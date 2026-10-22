using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace Pupitre
{
    /// <summary>
    /// Fantôme d'un élément en cours de glisser : une image de l'élément « détaché » suit la souris (Popup non cliquable),
    /// pendant que sa place dans la liste est tenue par l'élément d'origine estompé.
    /// </summary>
    public class DragGhost
    {
        const double Pad = 24;   // marge pour l'ombre portée
        Popup popup;
        Point grab;
        bool open;

        public bool IsOpen { get { return open; } }

        public void Start(Window owner, FrameworkElement source, Point grabInSource) { Start(owner, source, grabInSource, false); }

        /// <summary>bare : l'élément a déjà sa propre forme (sujet de carte) : pas de carte sombre autour, juste l'ombre.</summary>
        public void Start(Window owner, FrameworkElement source, Point grabInSource, bool bare)
        {
            End();
            if (source == null || source.ActualWidth < 1 || source.ActualHeight < 1) return;
            grab = grabInSource;
            double scale = 1.0;
            var src = PresentationSource.FromVisual(owner);
            if (src != null && src.CompositionTarget != null) scale = src.CompositionTarget.TransformToDevice.M11;
            RenderTargetBitmap rtb;
            try
            {
                rtb = new RenderTargetBitmap((int)Math.Ceiling(source.ActualWidth * scale), (int)Math.Ceiling(source.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
                // rendu via un pinceau visuel : indépendant de la position de l'élément dans son parent (Canvas)
                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen()) dc.DrawRectangle(new VisualBrush(source) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top }, null, new Rect(0, 0, source.ActualWidth, source.ActualHeight));
                rtb.Render(dv);
                rtb.Freeze();
            }
            catch (Exception ex) { Log.Error(ex, "fantôme de glisser"); return; }
            var img = new Image { Source = rtb, Width = source.ActualWidth, Height = source.ActualHeight, Stretch = Stretch.Fill };
            var card = new Border
            {
                Child = img,
                Background = bare ? Brushes.Transparent : Theme.Argb(0xF41E2028),
                BorderBrush = bare ? Brushes.Transparent : Theme.Res<Brush>("Accent"),
                BorderThickness = new Thickness(bare ? 0 : 1),
                Opacity = bare ? 0.85 : 1,
                CornerRadius = new CornerRadius(9),
                ClipToBounds = false,
                Margin = new Thickness(Pad),
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new TransformGroup { Children = { new ScaleTransform(1.03, 1.03), new RotateTransform(1.2) } },
                Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 7, Opacity = 0.65, Color = Colors.Black }
            };
            popup = new Popup { Child = card, AllowsTransparency = true, Placement = PlacementMode.Absolute, StaysOpen = true, PopupAnimation = PopupAnimation.None, IsHitTestVisible = false, Focusable = false };
            open = true;
        }

        /// <summary>Position de la souris à l'écran, en unités WPF.</summary>
        public void Move(Point screenDip)
        {
            if (!open || popup == null) return;
            popup.HorizontalOffset = screenDip.X - grab.X - Pad;
            popup.VerticalOffset = screenDip.Y - grab.Y - Pad;
            if (!popup.IsOpen) popup.IsOpen = true;
        }

        public void End()
        {
            if (popup != null) { popup.IsOpen = false; popup = null; }
            open = false;
        }

        public static Point ScreenDip(Window w, System.Windows.Input.MouseEventArgs e)
        {
            var p = w.PointToScreen(e.GetPosition(w));
            double scale = 1.0;
            var src = PresentationSource.FromVisual(w);
            if (src != null && src.CompositionTarget != null) scale = src.CompositionTarget.TransformToDevice.M11;
            return new Point(p.X / scale, p.Y / scale);
        }
    }
}
