using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using WF = System.Windows.Forms;

namespace Pupitre
{
    /// <summary>
    /// Fiche flottante : fenêtre sombre, non modale et redimensionnable, qui ouvre une note ou une tâche en grand
    /// sans toucher à la taille du panneau. Une seule fiche par élément ; elle se ferme avec Échap, la croix,
    /// ou quand son panneau est masqué.
    /// </summary>
    public class DetailWindow : Window
    {
        static readonly Dictionary<string, DetailWindow> open = new Dictionary<string, DetailWindow>();
        static readonly Dictionary<string, double> lastWidth = new Dictionary<string, double>();
        readonly string key, kind;
        readonly Window owner;
        readonly ContentPresenter body = new ContentPresenter();
        readonly TextBlock titleText;
        bool sizing, resized;
        Point sizeStart;
        Size sizeFrom;

        /// <summary>Ouvre la fiche identifiée par key (ou la ramène au premier plan) ; builder construit son contenu.</summary>
        public static DetailWindow Show(Window owner, string kind, string key, string title, Func<DetailWindow, UIElement> builder, double defW, double defH)
        {
            DetailWindow w;
            if (open.TryGetValue(key, out w)) { w.Activate(); return w; }
            w = new DetailWindow(owner, kind, key, title, defW, defH);
            w.body.Content = builder(w);
            open[key] = w;
            w.Show();
            w.Activate();
            return w;
        }

        public static void CloseFor(string key) { DetailWindow w; if (open.TryGetValue(key, out w)) w.Close(); }

        public void SetTitle(string t) { titleText.Text = t; Title = App.Name + " – " + t; }

        DetailWindow(Window owner, string kind, string key, string title, double defW, double defH)
        {
            this.owner = owner; this.kind = kind; this.key = key;
            Title = App.Name + " – " + title;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            FontFamily = Theme.Font;
            FontSize = 13;
            Foreground = Theme.Res<Brush>("Text");
            UseLayoutRounding = true;
            // hauteur ajustée au contenu (texte, sous-tâches, commentaires), bornée à l'écran ; largeur mémorisée si elle a été changée
            double w;
            if (!lastWidth.TryGetValue(kind, out w)) w = defW;
            Width = w + 28;
            SizeToContent = SizeToContent.Height;
            MinWidth = 340; MinHeight = Math.Min(defH, 150) + 28;
            MaxHeight = Math.Max(300, SystemParameters.WorkArea.Height - 40);
            if (owner != null && owner.IsVisible) { Owner = owner; WindowStartupLocation = WindowStartupLocation.CenterOwner; }
            else WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var head = new DockPanel { Margin = new Thickness(0, 0, 0, 8), Background = Brushes.Transparent, Cursor = Cursors.SizeAll };
            head.MouseLeftButtonDown += delegate { try { DragMove(); } catch { } };
            var close = Theme.IconButton("IcoClose", "Fermer  (Échap)", 13);
            close.Click += delegate { Close(); };
            DockPanel.SetDock(close, Dock.Right);
            titleText = new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 15, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            head.Children.Add(close);
            head.Children.Add(titleText);
            var dock = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(head, Dock.Top);
            dock.Children.Add(head);
            dock.Children.Add(body);

            var grid = new Grid();
            grid.Children.Add(dock);
            grid.Children.Add(Handle(Cursors.SizeWE, HorizontalAlignment.Right, VerticalAlignment.Stretch, 6, double.NaN, true, false));
            grid.Children.Add(Handle(Cursors.SizeNS, HorizontalAlignment.Stretch, VerticalAlignment.Bottom, double.NaN, 6, false, true));
            grid.Children.Add(Handle(Cursors.SizeNWSE, HorizontalAlignment.Right, VerticalAlignment.Bottom, 16, 16, true, true));
            Content = new Border
            {
                Background = Theme.Res<Brush>("MenuBg"),
                BorderBrush = Theme.Res<Brush>("MenuBorder"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 12, 16, 14),
                Margin = new Thickness(14),
                Child = grid,
                Effect = new DropShadowEffect { BlurRadius = 22, ShadowDepth = 3, Opacity = 0.55, Color = Colors.Black }
            };
            PreviewKeyDown += delegate(object s, KeyEventArgs e) { if (e.Key == Key.Escape) { Close(); e.Handled = true; } };
            Loaded += delegate { LimitToScreen(); ClampToScreen(); };
            SizeChanged += delegate { if (IsLoaded) ClampToScreen(); };
            if (owner != null) owner.IsVisibleChanged += OwnerVisibleChanged;
            Closed += delegate
            {
                open.Remove(key);
                if (resized) lastWidth[kind] = ActualWidth - 28;
                if (owner != null) owner.IsVisibleChanged -= OwnerVisibleChanged;
            };
        }

        void OwnerVisibleChanged(object s, DependencyPropertyChangedEventArgs e) { if (!(bool)e.NewValue) Close(); }

        /// <summary>Poignée de redimensionnement quasi invisible sur le bord droit, le bord bas ou le coin.</summary>
        Rectangle Handle(Cursor cursor, HorizontalAlignment ha, VerticalAlignment va, double w, double h, bool dx, bool dy)
        {
            var r = new Rectangle { Fill = Theme.Brush(0x01, Colors.White), Cursor = cursor, HorizontalAlignment = ha, VerticalAlignment = va, Margin = new Thickness(-16, -12, -16, -14) };
            if (!double.IsNaN(w)) r.Width = w;
            if (!double.IsNaN(h)) r.Height = h;
            r.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (dy && SizeToContent != SizeToContent.Manual) { double cur = ActualHeight; SizeToContent = SizeToContent.Manual; Height = cur; }
                sizing = true; resized = true; sizeStart = e.GetPosition(this); sizeFrom = new Size(ActualWidth, ActualHeight);
                r.CaptureMouse(); e.Handled = true;
            };
            r.MouseMove += delegate(object s, MouseEventArgs e)
            {
                if (!sizing) return;
                var p = e.GetPosition(this);
                if (dx) Width = Math.Max(MinWidth, sizeFrom.Width + (p.X - sizeStart.X));
                if (dy) Height = Math.Max(MinHeight, sizeFrom.Height + (p.Y - sizeStart.Y));
            };
            r.MouseLeftButtonUp += delegate { if (sizing) { sizing = false; r.ReleaseMouseCapture(); } };
            r.LostMouseCapture += delegate { sizing = false; };
            return r;
        }

        /// <summary>Garde la fiche entièrement visible sur l'écran où elle s'ouvre (le panneau est souvent en bas de l'écran).</summary>
        /// <summary>Hauteur maximale d'après l'écran qui affiche la fiche (pas seulement l'écran principal).</summary>
        void LimitToScreen()
        {
            try
            {
                var wa = WF.Screen.FromHandle(new WindowInteropHelper(this).Handle).WorkingArea;
                var src = PresentationSource.FromVisual(this);
                double scale = (src != null && src.CompositionTarget != null) ? src.CompositionTarget.TransformToDevice.M22 : 1.0;
                MaxHeight = Math.Max(300, wa.Height / scale - 40);
            }
            catch { }
        }

        void ClampToScreen()
        {
            try
            {
                var wa = WF.Screen.FromHandle(new WindowInteropHelper(this).Handle).WorkingArea;
                var src = PresentationSource.FromVisual(this);
                if (src == null || src.CompositionTarget == null) return;
                var m = src.CompositionTarget.TransformFromDevice;
                var tl = m.Transform(new Point(wa.Left, wa.Top));
                var br = m.Transform(new Point(wa.Right, wa.Bottom));
                double w = ActualWidth, h = ActualHeight;
                if (Left + w > br.X) Left = br.X - w;
                if (Top + h > br.Y) Top = br.Y - h;
                if (Left < tl.X) Left = tl.X;
                if (Top < tl.Y) Top = tl.Y;
            }
            catch { }
        }
    }
}
