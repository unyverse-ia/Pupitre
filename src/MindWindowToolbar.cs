using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Pupitre
{
    /// <summary>
    /// Cartes mentales : barre d'outils flottante au-dessus du sujet sélectionné (comme la barre contextuelle de
    /// Whimsical, Miro ou XMind), réduite à l'essentiel : couleur, gras, marqueur, note, lien, relation, menu complet, suppression.
    /// </summary>
    public partial class MindWindow
    {
        Canvas overlay;
        Border toolbar;
        Border colorDot;
        Popup popup;

        Border BuildToolbar()
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            colorDot = new Border { Width = 14, Height = 14, CornerRadius = new CornerRadius(7), Background = Theme.Brush(0xFF, Accent), BorderBrush = Theme.Argb(0x88FFFFFF), BorderThickness = new Thickness(1) };
            row.Children.Add(ToolBtn(colorDot, "Couleur de la branche", delegate(Button b) { ShowColorPopup(b); }));
            row.Children.Add(Sep());
            row.Children.Add(ToolBtn(new TextBlock { Text = "G", FontSize = 12.5, FontWeight = FontWeights.Bold, Foreground = Theme.Res<Brush>("Text") }, "Gras", delegate(Button b) { if (selected != null) { Snapshot(); selected.Bold = !selected.Bold; Changed(); } }));
            row.Children.Add(Sep());
            row.Children.Add(ToolBtn(Theme.Icon("IcoFlag", 14), "Marqueur (priorité, avancement, drapeaux, symboles…)", delegate(Button b) { if (selected != null) { var cm = MarkerContextMenu(selected); cm.PlacementTarget = b; cm.Placement = PlacementMode.Bottom; cm.IsOpen = true; } }));
            row.Children.Add(ToolBtn(Theme.Icon("IcoNotes", 14), "Note  (Ctrl+Maj+N)", delegate(Button b) { if (selected != null) EditNote(selected); }));
            row.Children.Add(ToolBtn(Theme.Icon("IcoExternal", 14), "Lien web, fichier ou dossier  (Ctrl+K)", delegate(Button b) { if (selected != null) EditLink(selected); }));
            row.Children.Add(Sep());
            row.Children.Add(ToolBtn(new TextBlock { Text = "⟶", FontSize = 13, Foreground = Theme.Res<Brush>("Text") }, "Relation vers un autre sujet : cliquez ensuite la cible  (Ctrl+Maj+R)", delegate(Button b) { BeginRelation(selected); }));
            row.Children.Add(Sep());
            row.Children.Add(ToolBtn(Theme.Icon("IcoDots", 14), "Toutes les options", delegate(Button b) { if (selected != null) ShowNodeMenu(selected); }));
            row.Children.Add(ToolBtn(Theme.Icon("IcoTrash", 14), "Supprimer le sujet  (Suppr)", delegate(Button b) { DeleteSelected(); }));
            toolbar = new Border
            {
                Background = Theme.Res<Brush>("MenuBg"),
                BorderBrush = Theme.Res<Brush>("MenuBorder"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(4, 2, 4, 2),
                Child = row,
                Visibility = Visibility.Collapsed,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 12, ShadowDepth = 2, Opacity = 0.45 }
            };
            return toolbar;
        }

        static Rectangle Sep() { return new Rectangle { Width = 1, Height = 16, Fill = Theme.Argb(0x33FFFFFF), Margin = new Thickness(3, 0, 3, 0), VerticalAlignment = VerticalAlignment.Center }; }

        Button ToolBtn(object content, string tip, Action<Button> click)
        {
            var b = new Button { Style = Theme.Res<Style>("IconBtnSmall"), Width = 26, Height = 24, Content = content, ToolTip = tip, Focusable = false };
            b.Click += delegate { Activate(); click(b); };
            return b;
        }

        /// <summary>Place la barre au-dessus du sujet sélectionné (ou en dessous s'il touche le haut), ou la masque.</summary>
        void PlaceToolbar()
        {
            if (toolbar == null) return;
            Box b;
            bool show = selected != null && editing == null && !dragging && !panning && relFrom == null && boxes.TryGetValue(selected, out b);
            if (!show) { toolbar.Visibility = Visibility.Collapsed; return; }
            b = boxes[selected];
            toolbar.Visibility = Visibility.Visible;
            if (colorDot != null) colorDot.Background = Theme.Brush(0xFF, b.Color);
            toolbar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double tw = toolbar.DesiredSize.Width, th = toolbar.DesiredSize.Height, z = scale.ScaleX;
            double cx = (b.X + b.W / 2) * z + pan.X;
            double top = b.Y * z + pan.Y - th - 12;
            if (!string.IsNullOrWhiteSpace(b.N.Callout)) top -= 52 * z;
            if (top < 4) top = (b.Y + b.H) * z + pan.Y + 18;
            double left = Math.Max(4, Math.Min(viewport.ActualWidth - tw - 4, cx - tw / 2));
            Canvas.SetLeft(toolbar, left); Canvas.SetTop(toolbar, top);
        }

        Popup OpenPopup(Button anchor, UIElement content)
        {
            if (popup != null) popup.IsOpen = false;
            popup = new Popup { PlacementTarget = anchor, Placement = PlacementMode.Bottom, StaysOpen = false, AllowsTransparency = true, PopupAnimation = PopupAnimation.Fade, VerticalOffset = 4 };
            popup.Child = new Border { Background = Theme.Res<Brush>("MenuBg"), BorderBrush = Theme.Res<Brush>("MenuBorder"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(8), Child = content, Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 12, ShadowDepth = 2, Opacity = 0.45 } };
            popup.IsOpen = true;
            return popup;
        }

        void ShowColorPopup(Button anchor)
        {
            var n = selected;
            if (n == null) return;
            var wrap = new WrapPanel { Width = 150 };
            wrap.Children.Add(Swatch(n.Color == 0, Theme.Argb(0x22FFFFFF), n == map.Root ? "Bleu Pupitre" : "Automatique (héritée de la branche)", "A", delegate { popup.IsOpen = false; Snapshot(); n.Color = 0; n.CustomColor = null; Changed(); }));
            foreach (var i in TaskColors.Indices) { int v = i; wrap.Children.Add(Swatch(n.Color == v, TaskColors.Solid(v), Palette.Names[v], null, delegate { popup.IsOpen = false; Snapshot(); n.Color = v; Changed(); })); }
            wrap.Children.Add(Swatch(n.Color < 0, n.Color < 0 ? Theme.Brush(0xFF, Palette.ParseHex(n.CustomColor, Accent)) : Theme.Argb(0x22FFFFFF), "Couleur personnalisée…", "+", delegate
            {
                popup.IsOpen = false;
                string hex = TasksWindow.PickCustomColor(this, boxes.ContainsKey(n) ? boxes[n].Color : Palette.Get(1));
                if (hex != null) { Snapshot(); n.Color = -1; n.CustomColor = hex; Changed(); }
            }));
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = "Couleur de la branche", FontSize = 10.5, Foreground = Theme.Res<Brush>("TextFaint"), Margin = new Thickness(0, 0, 0, 6) });
            sp.Children.Add(wrap);
            OpenPopup(anchor, sp);
        }

        /// <summary>Marqueurs par groupes ; choisir un marqueur déjà posé le retire.</summary>
        void FillMarkers(ItemsControl into, MindNode n)
        {
            foreach (var g in MarkerGroups)
            {
                var gm = new MenuItem { Header = g };
                foreach (var d0 in MarkerDefs.Where(d => d.Group == g))
                {
                    var d = d0;
                    gm.Items.Add(Check((d.Chip ? "" : d.Glyph + "  ") + d.Name, n.Markers.Contains(d.Id), delegate { SetMarker(n, d); }));
                }
                into.Items.Add(gm);
            }
            if (n.Markers.Count > 0) { into.Items.Add(new Separator()); into.Items.Add(Item("Retirer tous les marqueurs", null, delegate { Snapshot(); n.Markers.Clear(); Changed(); })); }
        }

        MenuItem MarkerMenu(MindNode n)
        {
            var mk = new MenuItem { Header = "Marqueur", Icon = Theme.Icon("IcoFlag", 14) };
            FillMarkers(mk, n);
            return mk;
        }

        ContextMenu MarkerContextMenu(MindNode n)
        {
            var cm = new ContextMenu();
            FillMarkers(cm, n);
            return cm;
        }

        // ---- feuilles : liste défilante avec filtre (lisible même avec des dizaines de feuilles) ------------------

        static int CountNodes(MindMap m)
        {
            int n = All(m.Root).Count();
            foreach (var f in m.Floating) n += All(f).Count();
            return n;
        }

        static string When(string iso)
        {
            DateTime d;
            if (string.IsNullOrEmpty(iso) || !DateTime.TryParse(iso, out d)) return "";
            return "modifiée le " + d.ToString("dd/MM") + " à " + d.ToString("HH:mm");
        }

        Button RowBtn(object content, string tip, bool enabled, Action click)
        {
            var b = new Button { Style = Theme.Res<Style>("IconBtnSmall"), Width = 22, Height = 22, Content = content, ToolTip = tip, IsEnabled = enabled, Focusable = false, Opacity = enabled ? 1 : 0.3 };
            b.Click += delegate { click(); };
            return b;
        }

        void ShowMapList()
        {
            var root = new StackPanel { Width = 330 };
            var filter = new TextBox { Style = Theme.Res<Style>("FieldTextBox"), Tag = "Rechercher une feuille…", Margin = new Thickness(0, 0, 0, 6) };
            root.Children.Add(filter);
            var list = new StackPanel();
            var scroll = new ScrollViewer { Content = list, MaxHeight = 340, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Focusable = false };
            root.Children.Add(scroll);
            var foot = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
            var add = new Button { Style = Theme.Res<Style>("ChipColored"), Background = Theme.Res<Brush>("Accent"), Foreground = Brushes.White, FontWeight = FontWeights.SemiBold, Content = "+ Nouvelle feuille…", ToolTip = "Créer une feuille à partir d'un modèle" };
            add.Click += delegate { popup.IsOpen = false; NewMapInteractive(); };
            DockPanel.SetDock(add, Dock.Right);
            var count = new TextBlock { Foreground = Theme.Res<Brush>("TextFaint"), FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
            foot.Children.Add(add); foot.Children.Add(count);
            root.Children.Add(foot);
            Action fill = null;
            fill = delegate
            {
                list.Children.Clear();
                string q = filter.Text.Trim().ToLowerInvariant();
                int shown = 0;
                for (int i = 0; i < store.Maps.Count; i++)
                {
                    var m = store.Maps[i]; int idx = i;
                    if (q.Length > 0 && (m.Name ?? "").ToLowerInvariant().IndexOf(q) < 0) continue;
                    shown++;
                    bool cur = m == map;
                    var row = new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 4, 2, 4), Background = cur ? Theme.Brush(0x33, Accent) : Brushes.Transparent, Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 4, 1) };
                    var dp = new DockPanel();
                    var acts = new StackPanel { Orientation = Orientation.Horizontal, Opacity = cur ? 1 : 0 };
                    acts.Children.Add(RowBtn(new TextBlock { Text = "▲", FontSize = 9, Foreground = Theme.Res<Brush>("TextMuted") }, "Monter", idx > 0, delegate { store.Maps.RemoveAt(idx); store.Maps.Insert(idx - 1, m); saveDeb.Trigger(); UpdateFooter(); fill(); }));
                    acts.Children.Add(RowBtn(new TextBlock { Text = "▼", FontSize = 9, Foreground = Theme.Res<Brush>("TextMuted") }, "Descendre", idx < store.Maps.Count - 1, delegate { store.Maps.RemoveAt(idx); store.Maps.Insert(idx + 1, m); saveDeb.Trigger(); UpdateFooter(); fill(); }));
                    acts.Children.Add(RowBtn(Theme.Icon("IcoPencil", 12), "Renommer", true, delegate
                    {
                        popup.IsOpen = false;
                        var n = Dialogs.Prompt(this, "Renommer la feuille", "Nom", m.Name, false);
                        if (!string.IsNullOrWhiteSpace(n)) { m.Name = n.Trim(); UpdateFooter(); saveDeb.Trigger(); }
                    }));
                    acts.Children.Add(RowBtn(Theme.Icon("IcoCopy", 12), "Dupliquer", true, delegate
                    {
                        popup.IsOpen = false;
                        var d = MindJson.Clone(m); d.Id = NewId(); d.Name = m.Name + " (copie)"; d.Created = Now();
                        store.Maps.Insert(idx + 1, d); SelectMap(d);
                    }));
                    acts.Children.Add(RowBtn(Theme.Icon("IcoTrash", 12), "Supprimer…", true, delegate { popup.IsOpen = false; DeleteMap(m); }));
                    DockPanel.SetDock(acts, Dock.Right);
                    var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    text.Children.Add(new TextBlock { Text = m.Name, FontWeight = cur ? FontWeights.SemiBold : FontWeights.Normal, Foreground = Theme.Res<Brush>("Text"), TextTrimming = TextTrimming.CharacterEllipsis });
                    text.Children.Add(new TextBlock { Text = CountNodes(m) + " sujet(s)  ·  " + When(m.Updated), FontSize = 10.5, Foreground = Theme.Res<Brush>("TextFaint"), TextTrimming = TextTrimming.CharacterEllipsis });
                    dp.Children.Add(acts); dp.Children.Add(text);
                    row.Child = dp;
                    bool isCur = cur;
                    row.MouseEnter += delegate { acts.Opacity = 1; if (!isCur) row.Background = Theme.Argb(0x14FFFFFF); };
                    row.MouseLeave += delegate { if (!isCur) { acts.Opacity = 0; row.Background = Brushes.Transparent; } };
                    row.MouseLeftButtonDown += delegate(object s2, MouseButtonEventArgs e)
                    {
                        if (Ui.FindAncestor<ButtonBase>(e.OriginalSource as DependencyObject) != null) return;
                        popup.IsOpen = false; SelectMap(m); e.Handled = true;
                    };
                    list.Children.Add(row);
                }
                count.Text = (q.Length > 0 ? shown + " / " : "") + store.Maps.Count + " feuille" + (store.Maps.Count > 1 ? "s" : "");
            };
            filter.TextChanged += delegate { fill(); };
            filter.PreviewKeyDown += delegate(object s2, KeyEventArgs e)
            {
                if (e.Key == Key.Enter)
                {
                    string q = filter.Text.Trim().ToLowerInvariant();
                    var first = store.Maps.FirstOrDefault(x => (x.Name ?? "").ToLowerInvariant().IndexOf(q) >= 0);
                    if (first != null) { popup.IsOpen = false; SelectMap(first); }
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape) { popup.IsOpen = false; e.Handled = true; }
            };
            fill();
            OpenPopup(mapBtn, root);
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(delegate { filter.Focus(); Keyboard.Focus(filter); }));
        }
    }
}
