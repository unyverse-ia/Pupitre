using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Pupitre
{
    /// <summary>
    /// Tableau des colonnes de cadres fusionnés : une carte par colonne (fenêtre principale en tête, fenêtres empilées dessous)
    /// et une zone « Indépendant ». Chaque fenêtre se glisse sur une colonne (collée en bas), sur une ligne (insérée à cette
    /// place) ou dans la zone libre. Les changements s'appliquent immédiatement sur le bureau.
    /// </summary>
    public class FusionBoard : Grid
    {
        readonly Controller c;
        readonly CadreWindow current;
        readonly StackPanel list = new StackPanel();
        readonly Canvas overlay = new Canvas { IsHitTestVisible = false };
        Border ghost, hilite;
        CadreWindow dragWin;
        Point dragStart;
        bool dragging;

        public FusionBoard(Controller ctl, CadreWindow highlight)
        {
            c = ctl; current = highlight;
            Children.Add(list);
            Children.Add(overlay);
            Background = Brushes.Transparent;
            PreviewMouseLeftButtonDown += Down;
            PreviewMouseMove += Move;
            PreviewMouseLeftButtonUp += Up;
            LostMouseCapture += delegate { if (dragging) EndDrag(); };
            Build();
        }

        // ---- construction --------------------------------------------------------------------------------------

        public void Build()
        {
            list.Children.Clear();
            var tops = c.Cadres.Where(w => Fusion.Above(c, w) == null).ToList();
            foreach (var top in tops) list.Children.Add(Column(top));
            list.Children.Add(FreeZone());
        }

        Border Column(CadreWindow top)
        {
            var members = Fusion.Chain(c, top);
            var body = new StackPanel();
            for (int i = 0; i < members.Count; i++) body.Children.Add(Row(members[i], i == 0, members.Count));
            var card = new Border
            {
                Tag = "col:" + top.Info.Id, Child = body, Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(6, 6, 6, 8),
                CornerRadius = new CornerRadius(10), Background = Theme.Res<Brush>("SField"), BorderBrush = Theme.Res<Brush>("SLine"), BorderThickness = new Thickness(1)
            };
            body.Children.Add(new TextBlock { Text = members.Count > 1 ? "Déposer ici : en bas de la colonne" : "Déposer ici : sous cette fenêtre", FontSize = 10.5, Foreground = Theme.Res<Brush>("SMuted"), Margin = new Thickness(26, 2, 0, 0) });
            return card;
        }

        Border Row(CadreWindow w, bool isTop, int count)
        {
            var dock = new DockPanel { Margin = new Thickness(isTop ? 0 : 18, 0, 0, 0) };
            var grip = new TextBlock { Text = "⋮⋮", FontSize = 12, Foreground = Theme.Res<Brush>("SMuted"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), Cursor = Cursors.SizeAll };
            DockPanel.SetDock(grip, Dock.Left);
            var info = w.Info;
            var dot = new Border { Width = 9, Height = 9, CornerRadius = new CornerRadius(5), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0),
                Background = info.Color < 0 ? Theme.Brush(0xFF, Palette.ParseHex(info.CustomColor, Palette.Get(1))) : Palette.Brush(info.Color) };
            DockPanel.SetDock(dot, Dock.Left);
            string role = isTop ? (count > 1 ? "fenêtre principale · " + (count - 1) + " dessous" : "indépendante") : (w.IsRolled ? "repliée" : "");
            if (!info.Visible) role = (role.Length > 0 ? role + " · " : "") + "masquée";
            var roleText = new TextBlock { Text = role, FontSize = 11, Foreground = Theme.Res<Brush>("SMuted"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
            DockPanel.SetDock(roleText, Dock.Right);
            var name = new TextBlock { Text = info.Name, FontWeight = isTop ? FontWeights.SemiBold : FontWeights.Normal, FontSize = 13, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = w == current ? Theme.Res<Brush>("Accent") : Theme.Res<Brush>("SText") };
            dock.Children.Add(grip); dock.Children.Add(dot); dock.Children.Add(roleText); dock.Children.Add(name);
            return new Border { Tag = w, Child = dock, Padding = new Thickness(6, 5, 6, 5), Margin = new Thickness(0, 0, 0, 2), CornerRadius = new CornerRadius(7), Background = Brushes.Transparent, Cursor = Cursors.Hand };
        }

        Border FreeZone()
        {
            var tb = new TextBlock { Text = "Déposer ici : fenêtre indépendante (hors de toute colonne)", FontSize = 12, Foreground = Theme.Res<Brush>("SMuted"), HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center };
            return new Border { Tag = "free", Child = tb, Padding = new Thickness(10, 10, 10, 10), CornerRadius = new CornerRadius(10), BorderBrush = Theme.Res<Brush>("SLine"), BorderThickness = new Thickness(1), Background = Brushes.Transparent };
        }

        // ---- glisser-déposer -----------------------------------------------------------------------------------

        static Border RowUnder(DependencyObject d, out object tag)
        {
            tag = null;
            while (d != null)
            {
                var b = d as Border;
                if (b != null && b.Tag != null && (b.Tag is CadreWindow || b.Tag is string)) { tag = b.Tag; return b; }
                d = VisualTreeHelper.GetParent(d);
            }
            return null;
        }

        Border TargetAt(Point p, out object tag)
        {
            tag = null;
            var hit = VisualTreeHelper.HitTest(list, p);
            return hit == null ? null : RowUnder(hit.VisualHit, out tag);
        }

        void Down(object sender, MouseButtonEventArgs e)
        {
            object tag; var row = RowUnder(e.OriginalSource as DependencyObject, out tag);
            dragWin = tag as CadreWindow;
            if (dragWin == null) return;
            dragStart = e.GetPosition(this);
        }

        void Move(object sender, MouseEventArgs e)
        {
            if (dragWin == null || e.LeftButton != MouseButtonState.Pressed) return;
            var p = e.GetPosition(this);
            if (!dragging)
            {
                if (Math.Abs(p.X - dragStart.X) < 4 && Math.Abs(p.Y - dragStart.Y) < 4) return;
                dragging = true;
                ghost = new Border { Padding = new Thickness(10, 5, 10, 6), CornerRadius = new CornerRadius(7), Background = Theme.Res<Brush>("Accent"), Opacity = 0.9, IsHitTestVisible = false,
                    Child = new TextBlock { Text = dragWin.Info.Name, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold } };
                overlay.Children.Add(ghost);
                CaptureMouse();                     // relance un MouseMove de façon réentrante : le fantôme doit déjà exister
            }
            if (ghost == null) return;
            Canvas.SetLeft(ghost, p.X + 12); Canvas.SetTop(ghost, p.Y - 14);
            object tag; var target = TargetAt(e.GetPosition(list), out tag);
            if (hilite != null && hilite != target) hilite.Background = hilite.Tag is string && (string)hilite.Tag == "free" ? Brushes.Transparent : (hilite.Tag is CadreWindow ? Brushes.Transparent : Theme.Res<Brush>("SField"));
            if (target != null && target != hilite) target.Background = Theme.Brush(0x44, Palette.Get(1));
            hilite = target;
        }

        void Up(object sender, MouseButtonEventArgs e)
        {
            if (!dragging) { dragWin = null; return; }
            object tag; var target = TargetAt(e.GetPosition(list), out tag);
            var w = dragWin;
            EndDrag();
            if (target == null || w == null) return;
            var onRow = tag as CadreWindow;
            string s = tag as string;
            if (onRow != null)
            {
                if (onRow == w) return;
                var top = Fusion.Top(c, onRow);
                var members = Fusion.Chain(c, top).Where(m => m != w).ToList();
                Fusion.Place(c, w, top, members.IndexOf(onRow));          // inséré à la place de la ligne visée (au-dessus d'elle)
            }
            else if (s != null && s.StartsWith("col:"))
            {
                var top = Fusion.ById(c, s.Substring(4));
                if (top != null) Fusion.Place(c, w, top, int.MaxValue);   // en bas de la colonne
            }
            else if (s == "free") Fusion.Place(c, w, null, 0);
            Build();
        }

        void EndDrag()
        {
            dragging = false; dragWin = null;
            if (ghost != null) { overlay.Children.Remove(ghost); ghost = null; }
            if (hilite != null) { Build(); hilite = null; }
            if (IsMouseCaptured) ReleaseMouseCapture();
        }
    }
}
