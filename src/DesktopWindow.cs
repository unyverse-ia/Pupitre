using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WF = System.Windows.Forms;

namespace Pupitre
{
    /// <summary>
    /// Fenêtre « collée au bureau » : verre teinté (couleur + opacité permanentes), coins arrondis, toujours sous les
    /// autres fenêtres, appartenant à la fenêtre du bureau (survit à Win+D), déplaçable par son titre, redimensionnable,
    /// aimantée, enroulable. Barre de titre : renommage au crayon, paramètres du cadre (couleur, opacité, verrou), enrouler, menu « + ».
    /// </summary>
    public abstract class DesktopWindow : Window
    {
        public const double TitleH = 36;
        protected readonly Controller c;
        public IntPtr Hwnd;
        protected Border Root, TitleBar;
        Grid outer;
        readonly List<System.Windows.Shapes.Rectangle> handles = new List<System.Windows.Shapes.Rectangle>();
        bool resizing; int resizeEdge; Native.POINT resizeStart; Native.RECT resizeRect;
        protected TextBlock TitleText;
        /// <summary>Nom affiché dans la barre de titre (cadre ou panneau).</summary>
        public string PanelTitle { get { return TitleText != null ? TitleText.Text : Title; } }
        protected StackPanel Actions, TitleExtra;
        protected ContentPresenter Body;
        protected Border AccentDot;
        TextBox titleEdit;
        Button renameBtn, rollBtn, menuBtn, settingsBtn;
        Viewbox lockMark;
        Border actionsHost;
        bool tightBar;                 // barre étroite : les boutons passent sous le titre
        const double ActionsH = 26;
        /// <summary>Hauteur courante de la barre de titre (une ou deux lignes).</summary>
        public double BarH { get { return TitleH + (tightBar ? ActionsH : 0); } }
        bool rolled, shownOnce, menuOpen;
        double restoreH;
        public bool IntentionalClose;
        protected bool SuppressSave;
        readonly Debouncer saveDeb;

        public bool IsRolled { get { return rolled; } }
        public double RestoreHeight { get { return rolled ? restoreH : Height; } }

        protected DesktopWindow(Controller ctl)
        {
            c = ctl;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = false;
            AllowsTransparency = true;      // transparence par pixel : la teinte et l'opacité sont dessinées par Pupitre, donc permanentes
            Background = Brushes.Transparent;
            MinWidth = 120;
            MinHeight = TitleH + 56;
            UseLayoutRounding = true;
            FontFamily = Theme.Font;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

            Root = new Border { CornerRadius = new CornerRadius(12), BorderThickness = new Thickness(1), BorderBrush = Theme.Argb(0x2AFFFFFF), SnapsToDevicePixels = true, Background = Brushes.Transparent };
            var dock = new DockPanel { LastChildFill = true };

            TitleBar = new Border { Height = TitleH, Background = Theme.Argb(0x22000000), CornerRadius = new CornerRadius(11, 11, 0, 0), Padding = new Thickness(12, 0, 5, 0), Cursor = Cursors.SizeAll, ClipToBounds = true };
            var tg = new Grid();
            tg.RowDefinitions.Add(new RowDefinition { Height = new GridLength(TitleH) });
            tg.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var leftGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Left, Height = TitleH };
            leftGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });   // pastille
            leftGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // titre / édition
            leftGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });   // crayon
            leftGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });   // verrou
            leftGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });   // badges
            AccentDot = new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 9, 0), VerticalAlignment = VerticalAlignment.Center };
            TitleText = new TextBlock { FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Theme.Res<Brush>("Text"), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 260 };
            titleEdit = new TextBox { Style = Theme.Res<Style>("GhostTextBox"), FontSize = 13, FontWeight = FontWeights.SemiBold, Visibility = Visibility.Collapsed, MinWidth = 90, MaxWidth = 260, Padding = new Thickness(2, 0, 2, 0), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(-3, 0, 0, 0) };
            titleEdit.KeyDown += TitleEditKey;
            titleEdit.LostKeyboardFocus += delegate { if (titleEdit.Visibility == Visibility.Visible) EndRename(true); };
            renameBtn = Theme.IconButton("IcoPencil", "Renommer", 12);
            renameBtn.Width = 22; renameBtn.Height = 22; renameBtn.Margin = new Thickness(4, 0, 0, 0); renameBtn.Opacity = 0;
            renameBtn.Click += delegate { BeginRename(); };
            lockMark = new Viewbox { Width = 11, Height = 11, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Collapsed, Opacity = 0.8 };
            lockMark.Child = new System.Windows.Shapes.Path { Style = Theme.Res<Style>("Ico"), Data = Theme.Res<Geometry>("IcoLock"), Stroke = Theme.Res<Brush>("Warn") };
            TitleExtra = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
            Grid.SetColumn(AccentDot, 0); Grid.SetColumn(TitleText, 1); Grid.SetColumn(titleEdit, 1); Grid.SetColumn(renameBtn, 2); Grid.SetColumn(lockMark, 3); Grid.SetColumn(TitleExtra, 4);
            leftGrid.Children.Add(AccentDot); leftGrid.Children.Add(TitleText); leftGrid.Children.Add(titleEdit); leftGrid.Children.Add(renameBtn); leftGrid.Children.Add(lockMark); leftGrid.Children.Add(TitleExtra);
            Actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Opacity = 0 };
            actionsHost = new Border { Child = Actions, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, CornerRadius = new CornerRadius(7), Padding = new Thickness(4, 0, 0, 0), Background = Brushes.Transparent, IsHitTestVisible = false };
            Grid.SetRow(leftGrid, 0); Grid.SetRow(actionsHost, 0);
            tg.Children.Add(leftGrid);
            tg.Children.Add(actionsHost);
            TitleBar.Child = tg;
            TitleExtra.SizeChanged += delegate { UpdateTitleLayout(); };
            TitleBar.MouseEnter += delegate { UpdateActionsVisibility(); };
            TitleBar.MouseLeave += delegate { UpdateActionsVisibility(); };
            TitleBar.MouseLeftButtonDown += TitleMouseDown;
            TitleBar.MouseRightButtonUp += delegate(object s, MouseButtonEventArgs e) { ShowMenu(); e.Handled = true; };
            Activated += delegate { UpdateActionsVisibility(); };
            Deactivated += delegate { UpdateActionsVisibility(); };
            DockPanel.SetDock(TitleBar, Dock.Top);
            dock.Children.Add(TitleBar);

            Body = new ContentPresenter { Margin = new Thickness(0, 0, 5, 0) };   // retrait = la barre de défilement reste cliquable à côté de la poignée de redimensionnement
            dock.Children.Add(Body);
            Root.Child = dock;
            outer = new Grid();
            outer.Children.Add(Root);
            AddHandle(HorizontalAlignment.Left, VerticalAlignment.Stretch, 5, double.NaN, Cursors.SizeWE, 1);
            AddHandle(HorizontalAlignment.Right, VerticalAlignment.Stretch, 5, double.NaN, Cursors.SizeWE, 2);
            AddHandle(HorizontalAlignment.Stretch, VerticalAlignment.Top, double.NaN, 5, Cursors.SizeNS, 4);
            AddHandle(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, double.NaN, 6, Cursors.SizeNS, 8);
            AddHandle(HorizontalAlignment.Left, VerticalAlignment.Top, 14, 14, Cursors.SizeNWSE, 5);
            AddHandle(HorizontalAlignment.Right, VerticalAlignment.Top, 14, 14, Cursors.SizeNESW, 6);
            AddHandle(HorizontalAlignment.Left, VerticalAlignment.Bottom, 14, 14, Cursors.SizeNESW, 9);
            AddHandle(HorizontalAlignment.Right, VerticalAlignment.Bottom, 14, 14, Cursors.SizeNWSE, 10);
            Content = outer;

            // boutons communs (ajoutés après ceux du sous-type via AddStandardActions)
            settingsBtn = Theme.IconButton("IcoSettings", SettingsTooltip, 15);
            settingsBtn.Click += delegate { OnOpenSettings(); };
            rollBtn = Theme.IconButton("IcoChevronUp", "Enrouler / dérouler  (double-clic sur le titre)", 14);
            rollBtn.Click += delegate { ToggleRoll(); };
            menuBtn = Theme.IconButton("IcoPlus", "Nouveau : tâche, cadre, note, carte mentale", 15);
            menuBtn.Click += delegate { ShowMenu(); };

            saveDeb = new Debouncer(500, delegate { if (shownOnce && !SuppressSave) SaveRect(); });
            LocationChanged += delegate { if (shownOnce && !SuppressSave) saveDeb.Trigger(); };
            SizeChanged += delegate { UpdateTitleLayout(); if (shownOnce && !SuppressSave) saveDeb.Trigger(); };
            Closed += delegate { if (!IntentionalClose) c.OnWindowLost(this); };
        }

        /// <summary>Alt+F4 ou toute fermeture non demandée par l'application est ignorée (le panneau fait partie du bureau).</summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!IntentionalClose && !c.Exiting) { e.Cancel = true; return; }
            base.OnClosing(e);
        }

        protected void AddAction(UIElement el) { Actions.Children.Add(el); }

        /// <summary>Paramètres du cadre (couleur, opacité, verrou…), enrouler, nouveau. Les paramètres généraux sont dans la zone de notification.</summary>
        protected void AddStandardActions()
        {
            Actions.Children.Add(settingsBtn);
            Actions.Children.Add(rollBtn);
            Actions.Children.Add(menuBtn);
            if (!CanRename) renameBtn.Visibility = Visibility.Collapsed;
        }

        protected virtual bool CanRename { get { return true; } }
        protected virtual string SettingsTooltip { get { return "Paramètres du cadre (couleur, opacité, verrou…)"; } }
        /// <summary>Verrou propre à ce panneau (réglé dans ses paramètres) ; le verrou général des paramètres s'y ajoute.</summary>
        protected virtual bool OwnLocked { get { return false; } }
        public bool IsLocked { get { return c.S.LockLayout || OwnLocked; } }
        protected abstract void OnOpenSettings();
        protected virtual void OnRenamed(string name) { }
        protected abstract void SaveRect();
        protected virtual void BuildMenu(ContextMenu menu) { }

        void UpdateActionsVisibility()
        {
            bool show = TitleBar.IsMouseOver || IsActive || menuOpen;
            double to = show ? 1 : 0;
            Actions.BeginAnimation(OpacityProperty, new DoubleAnimation(to, TimeSpan.FromMilliseconds(140)));
            actionsHost.IsHitTestVisible = show;
            if (CanRename) renameBtn.BeginAnimation(OpacityProperty, new DoubleAnimation(to, TimeSpan.FromMilliseconds(140)));
        }

        /// <summary>Largeur naturelle de la partie gauche du titre (pastille, texte, crayon, verrou, badges).</summary>
        double TitleNaturalWidth()
        {
            double text = 0;
            try
            {
                var ft = new FormattedText(TitleText.Text ?? "", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                    new Typeface(FontFamily, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal), 13, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                text = Math.Min(ft.WidthIncludingTrailingWhitespace, 260);
            }
            catch { text = 80; }
            double w = (AccentDot.Visibility == Visibility.Visible ? 17 : 0) + text + (CanRename ? 26 : 0) + (lockMark.Visibility == Visibility.Visible ? 17 : 0);
            if (TitleExtra.Children.Count > 0) w += TitleExtra.ActualWidth + 6;
            return w;
        }

        /// <summary>Responsive : si titre + boutons ne tiennent pas sur une ligne, les boutons passent sur une seconde ligne.</summary>
        void UpdateTitleLayout()
        {
            if (actionsHost == null || ActualWidth <= 0) return;
            double actionsW = Actions.Children.Count * 26 + 6;
            double avail = ActualWidth - 12 - 5 - 2;   // padding de la barre + bordure
            bool tight = TitleNaturalWidth() + actionsW + 10 > avail;
            if (tight == tightBar) return;
            tightBar = tight;
            Grid.SetRow(actionsHost, tight ? 1 : 0);
            actionsHost.Margin = tight ? new Thickness(0, 0, 0, 2) : new Thickness(0);
            TitleBar.Height = BarH;
            if (rolled) { MinHeight = BarH; Height = BarH; }
            else MinHeight = BarH + 56;
            UpdateActionsVisibility();
        }

        void TitleMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (titleEdit.Visibility == Visibility.Visible && titleEdit.IsMouseOver) return;
            if (e.ClickCount == 2) { ToggleRoll(); e.Handled = true; return; }
            Activate();
            if (IsLocked) return;
            try { DragMove(); } catch { }
        }

        // ---- Renommage en place --------------------------------------------------------------------

        public void BeginRename()
        {
            if (!CanRename) return;
            titleEdit.Text = TitleText.Text;
            TitleText.Visibility = Visibility.Collapsed;
            titleEdit.Visibility = Visibility.Visible;
            Activate();
            titleEdit.Focus();
            Keyboard.Focus(titleEdit);
            titleEdit.SelectAll();
        }

        void EndRename(bool commit)
        {
            if (titleEdit.Visibility != Visibility.Visible) return;
            string text = titleEdit.Text.Trim();
            titleEdit.Visibility = Visibility.Collapsed;
            TitleText.Visibility = Visibility.Visible;
            if (commit && text.Length > 0 && text != TitleText.Text) OnRenamed(text);
        }

        void TitleEditKey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { EndRename(true); Focus(); e.Handled = true; }
            else if (e.Key == Key.Escape) { EndRename(false); Focus(); e.Handled = true; }
        }

        // ---- Win32 ---------------------------------------------------------------------------------

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Hwnd = new WindowInteropHelper(this).Handle;
            var src = HwndSource.FromHwnd(Hwnd);
            if (src != null) src.AddHook(WndProc);
            Native.SetExStyle(Hwnd, Native.WS_EX_TOOLWINDOW, true);
            Native.SetExStyle(Hwnd, Native.WS_EX_APPWINDOW, false);
            Native.SetStyle(Hwnd, Native.WS_MAXIMIZEBOX | Native.WS_MINIMIZEBOX, false);
            ApplyAppearance();
            ApplyLock();
            AttachToDesktop();
        }

        IntPtr WndProc(IntPtr h, int msg, IntPtr w, IntPtr l, ref bool handled)
        {
            switch (msg)
            {
                case Native.WM_WINDOWPOSCHANGING:
                    {
                        var wp = (Native.WINDOWPOS)Marshal.PtrToStructure(l, typeof(Native.WINDOWPOS));
                        if ((wp.flags & Native.SWP_NOZORDER) == 0 && wp.hwndInsertAfter != Native.HWND_BOTTOM)
                        {
                            wp.hwndInsertAfter = Native.HWND_BOTTOM;
                            Marshal.StructureToPtr(wp, l, false);
                        }
                        break;
                    }
                case Native.WM_SYSCOMMAND:
                    {
                        int cmd = (int)(w.ToInt64() & 0xFFF0);
                        if (cmd == Native.SC_MAXIMIZE || cmd == Native.SC_MINIMIZE) { handled = true; return IntPtr.Zero; }
                        break;
                    }
                case Native.WM_NCLBUTTONDBLCLK:
                    handled = true;
                    return IntPtr.Zero;
                case Native.WM_ENTERSIZEMOVE:
                    {
                        // Point de prise : les positions suivantes sont recalculées depuis le curseur, jamais depuis
                        // le rectangle déjà aimanté (sinon Windows rebase le glisser et la fenêtre reste collée).
                        Native.POINT cur; Native.GetCursorPos(out cur);
                        Native.RECT wr; Native.GetWindowRect(Hwnd, out wr);
                        grabX = cur.X - wr.Left; grabY = cur.Y - wr.Top;
                        break;
                    }
                case Native.WM_MOVING:
                    if (c.S.Snap) { SnapMove(l); handled = true; return new IntPtr(1); }
                    break;
                case Native.WM_EXITSIZEMOVE:
                    saveDeb.Trigger();
                    break;
            }
            return IntPtr.Zero;
        }

        public void AttachToDesktop()
        {
            if (Hwnd == IntPtr.Zero) return;
            IntPtr host = c.DesktopHost;
            if (host != IntPtr.Zero && Native.IsWindow(host)) Native.SetWindowLongPtr(Hwnd, Native.GWLP_HWNDPARENT, host);
            Native.SendToBottom(Hwnd);
        }

        public double DpiScale()
        {
            var src = PresentationSource.FromVisual(this);
            return (src != null && src.CompositionTarget != null) ? src.CompositionTarget.TransformToDevice.M11 : 1.0;
        }

        // ---- Aimantation (bords d'écran et autres panneaux) ------------------------------------------

        int grabX, grabY;

        void Targets(out List<int> xs, out List<int> ys)
        {
            xs = new List<int>(); ys = new List<int>();
            Native.POINT p;
            Native.GetCursorPos(out p);
            var wa = WF.Screen.FromPoint(new System.Drawing.Point(p.X, p.Y)).WorkingArea;
            xs.Add(wa.Left); xs.Add(wa.Right); ys.Add(wa.Top); ys.Add(wa.Bottom);
            foreach (var other in c.DesktopWindows)
            {
                if (other == this || !other.IsVisible || other.Hwnd == IntPtr.Zero) continue;
                Native.RECT o;
                if (!Native.GetWindowRect(other.Hwnd, out o)) continue;
                xs.Add(o.Left); xs.Add(o.Right); ys.Add(o.Top); ys.Add(o.Bottom);
            }
        }

        void SnapMove(IntPtr lParam)
        {
            var r = (Native.RECT)Marshal.PtrToStructure(lParam, typeof(Native.RECT));
            int w = r.Width, hgt = r.Height;
            Native.POINT cur; Native.GetCursorPos(out cur);
            r.Left = cur.X - grabX; r.Top = cur.Y - grabY;
            int th = (int)Math.Round(9 * DpiScale());
            List<int> xs, ys; Targets(out xs, out ys);
            int dx = Pick(Shift(r.Left, xs, th), Shift(r.Left + w, xs, th));
            int dy = Pick(Shift(r.Top, ys, th), Shift(r.Top + hgt, ys, th));
            r.Left += dx; r.Top += dy;
            r.Right = r.Left + w; r.Bottom = r.Top + hgt;
            Marshal.StructureToPtr(r, lParam, false);
        }

        // ---- Redimensionnement par poignées (la boucle de dimensionnement de Windows ne s'applique pas aux fenêtres transparentes) ----

        void AddHandle(HorizontalAlignment ha, VerticalAlignment va, double w, double h, Cursor cursor, int edge)
        {
            // alpha 1 : invisible mais cliquable (un pixel entièrement transparent laisse passer le clic vers le bureau)
            var r = new System.Windows.Shapes.Rectangle { Fill = Theme.Argb(0x01000000), HorizontalAlignment = ha, VerticalAlignment = va, Cursor = cursor, Tag = edge };
            if (!double.IsNaN(w)) r.Width = w;
            if (!double.IsNaN(h)) r.Height = h;
            r.MouseLeftButtonDown += ResizeStart;
            r.MouseMove += ResizeMove;
            r.MouseLeftButtonUp += ResizeEnd;
            r.LostMouseCapture += delegate { resizing = false; };
            handles.Add(r);
            outer.Children.Add(r);
        }

        void ResizeStart(object sender, MouseButtonEventArgs e)
        {
            if (IsLocked || rolled) return;
            var r = (System.Windows.Shapes.Rectangle)sender;
            resizeEdge = (int)r.Tag;
            Native.GetCursorPos(out resizeStart);
            Native.GetWindowRect(Hwnd, out resizeRect);
            resizing = r.CaptureMouse();
            e.Handled = true;
        }

        void ResizeMove(object sender, MouseEventArgs e)
        {
            if (!resizing || e.LeftButton != MouseButtonState.Pressed) return;
            Native.POINT cur; Native.GetCursorPos(out cur);
            int dx = cur.X - resizeStart.X, dy = cur.Y - resizeStart.Y;
            var r = resizeRect;
            bool left = (resizeEdge & 1) != 0, right = (resizeEdge & 2) != 0, top = (resizeEdge & 4) != 0, bottom = (resizeEdge & 8) != 0;
            double scale = DpiScale();
            int th = c.S.Snap ? (int)Math.Round(9 * scale) : 0;
            List<int> xs, ys; Targets(out xs, out ys);
            int minW = (int)(MinWidth * scale), minH = (int)(MinHeight * scale);
            // au-delà du minimum, le bord s'arrête au minimum (et ne revient pas à la taille de départ)
            if (left) { int v = r.Left + dx; if (th > 0) v += Shift(v, xs, th); r.Left = Math.Min(v, r.Right - minW); }
            if (right) { int v = r.Right + dx; if (th > 0) v += Shift(v, xs, th); r.Right = Math.Max(v, r.Left + minW); }
            if (top) { int v = r.Top + dy; if (th > 0) v += Shift(v, ys, th); r.Top = Math.Min(v, r.Bottom - minH); }
            if (bottom) { int v = r.Bottom + dy; if (th > 0) v += Shift(v, ys, th); r.Bottom = Math.Max(v, r.Top + minH); }
            // jamais au-delà de l'espace de travail de l'écran sous le curseur : les bords restent toujours accessibles
            var wa = WF.Screen.FromPoint(new System.Drawing.Point(cur.X, cur.Y)).WorkingArea;
            if (left && r.Left < wa.Left) r.Left = wa.Left;
            if (top && r.Top < wa.Top) r.Top = wa.Top;
            if (right && r.Right > wa.Right) r.Right = wa.Right;
            if (bottom && r.Bottom > wa.Bottom) r.Bottom = wa.Bottom;
            Left = r.Left / scale; Top = r.Top / scale; Width = r.Width / scale; Height = r.Height / scale;
        }

        void ResizeEnd(object sender, MouseButtonEventArgs e)
        {
            if (!resizing) return;
            resizing = false;
            ((UIElement)sender).ReleaseMouseCapture();
            saveDeb.Trigger();
            e.Handled = true;
        }

        static int Shift(int v, List<int> targets, int th)
        {
            int best = 0, bestAbs = th + 1;
            foreach (var t in targets)
            {
                int d = t - v;
                if (Math.Abs(d) < bestAbs) { bestAbs = Math.Abs(d); best = d; }
            }
            return bestAbs <= th ? best : 0;
        }

        static int Pick(int a, int b)
        {
            if (a == 0) return b;
            if (b == 0) return a;
            return Math.Abs(a) <= Math.Abs(b) ? a : b;
        }

        // ---- Apparence : couleur + opacité du verre (permanentes) -------------------------------------

        protected int LookColor, LookOpacity = -1;
        protected string LookCustom;

        public int EffectiveOpacity { get { return LookOpacity < 0 ? Palette.DefaultOpacity : Math.Max(0, Math.Min(100, LookOpacity)); } }

        /// <summary>Couleur (index de palette, ou -1 = couleur personnalisée « custom » #RRGGBB) et opacité 0..100 (-1 = défaut).</summary>
        public void SetLook(int colorIndex, int opacity, string custom)
        {
            LookColor = colorIndex;
            LookOpacity = opacity;
            LookCustom = custom;
            ApplyAppearance();
        }

        public void ApplyAppearance()
        {
            int op = EffectiveOpacity;
            var tint = Palette.Tint(LookColor, LookCustom);
            byte alpha = (byte)Math.Round(op * 2.55);
            Root.Background = Theme.Brush(alpha, tint);
            Root.BorderBrush = Theme.Argb(op < 15 ? 0x18FFFFFFu : 0x2AFFFFFFu);
            TitleBar.Background = Theme.Brush((byte)(LookColor == 0 ? 0x22 : 0x2C), Color.FromRgb(0, 0, 0));
            var dot = LookColor < 0 ? Palette.ParseHex(LookCustom, Palette.Get(0)) : Palette.Get(LookColor);
            AccentDot.Background = Theme.Brush(0xFF, dot);
            AccentDot.Visibility = LookColor == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public void ApplyLock()
        {
            bool locked = IsLocked;
            foreach (var hd in handles) hd.IsHitTestVisible = !(locked || rolled);
            TitleBar.Cursor = locked ? Cursors.Arrow : Cursors.SizeAll;
            lockMark.Visibility = locked ? Visibility.Visible : Visibility.Collapsed;
            lockMark.ToolTip = c.S.LockLayout ? "Disposition verrouillée (paramètres généraux)" : "Panneau verrouillé : position et taille figées (voir ses paramètres)";
            UpdateTitleLayout();
        }

        public void SetTitle(string text) { TitleText.Text = text; Title = App.Name + " – " + text; UpdateTitleLayout(); }

        public void ToggleRoll() { SetRolled(!rolled, true); }

        public void SetRolled(bool on, bool animate)
        {
            if (on == rolled) return;
            rolled = on;
            SuppressSave = true;
            if (on)
            {
                restoreH = Height;
                MinHeight = BarH;
                Body.Visibility = Visibility.Collapsed;
                TitleBar.CornerRadius = new CornerRadius(11);
                ApplyLock();
                AnimateHeight(BarH, animate, delegate { SuppressSave = false; saveDeb.Trigger(); });
            }
            else
            {
                double target = Math.Max(restoreH, BarH + 56);
                TitleBar.CornerRadius = new CornerRadius(11, 11, 0, 0);
                AnimateHeight(target, animate, delegate
                {
                    MinHeight = BarH + 56;
                    Body.Visibility = Visibility.Visible;
                    ApplyLock();
                    SuppressSave = false;
                    saveDeb.Trigger();
                });
            }
            ((Viewbox)rollBtn.Content).Child = new System.Windows.Shapes.Path { Style = Theme.Res<Style>("Ico"), Data = Theme.Res<Geometry>(on ? "IcoChevronDown" : "IcoChevronUp") };
        }

        void AnimateHeight(double target, bool animate, Action done)
        {
            BeginAnimation(HeightProperty, null);
            if (!animate || !IsVisible) { Height = target; if (done != null) done(); return; }
            var a = new DoubleAnimation(Height, target, TimeSpan.FromMilliseconds(170)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.Stop };
            a.Completed += delegate { Height = target; if (done != null) done(); };
            BeginAnimation(HeightProperty, a);
        }

        /// <summary>Taille raisonnable du panneau, utilisée par « Taille par défaut ».</summary>
        protected virtual Size DefaultSize { get { return new Size(460, 380); } }

        /// <summary>Espace de travail (unités WPF) de l'écran qui contient le centre du panneau.</summary>
        Rect WorkAreaDip()
        {
            double scale = DpiScale();
            var center = new System.Drawing.Point((int)((Left + Width / 2) * scale), (int)((Top + Height / 2) * scale));
            var wa = WF.Screen.FromPoint(center).WorkingArea;
            return new Rect(wa.Left / scale, wa.Top / scale, wa.Width / scale, wa.Height / scale);
        }

        /// <summary>Remet le panneau à sa taille par défaut, entièrement visible sur son écran.</summary>
        public void ResetSize()
        {
            if (rolled) SetRolled(false, false);
            var wa = WorkAreaDip();
            Width = Math.Max(MinWidth, Math.Min(DefaultSize.Width, wa.Width - 16));
            Height = Math.Max(MinHeight, Math.Min(DefaultSize.Height, wa.Height - 16));
            FitOnScreen();
        }

        Rect? beforeFull;

        /// <summary>Le panneau occupe-t-il tout l'espace de travail de son écran ?</summary>
        public bool IsFullScreen
        {
            get { var wa = WorkAreaDip(); return Math.Abs(Left - wa.Left) < 3 && Math.Abs(Top - wa.Top) < 3 && Math.Abs(Width - wa.Width) < 3 && Math.Abs(Height - wa.Height) < 3; }
        }

        /// <summary>Plein écran (tout l'espace de travail) et retour à la taille précédente ; sans mémoire du précédent, taille par défaut.</summary>
        public void ToggleFullScreen()
        {
            if (IsLocked) return;
            if (rolled) SetRolled(false, false);
            if (IsFullScreen)
            {
                if (beforeFull.HasValue) { var r = beforeFull.Value; Left = r.X; Top = r.Y; Width = r.Width; Height = r.Height; beforeFull = null; FitOnScreen(); }
                else ResetSize();
                return;
            }
            beforeFull = new Rect(Left, Top, Width, Height);
            var wa = WorkAreaDip();
            Left = wa.Left; Top = wa.Top; Width = wa.Width; Height = wa.Height;
            saveDeb.Trigger();
        }

        /// <summary>Ramène le panneau entièrement dans l'espace de travail de son écran (réduit si besoin).</summary>
        public void FitOnScreen()
        {
            var wa = WorkAreaDip();
            if (Width > wa.Width) Width = Math.Max(MinWidth, wa.Width);
            if (!rolled && Height > wa.Height) Height = Math.Max(MinHeight, wa.Height);
            if (Left + Width > wa.Right) Left = wa.Right - Width;
            if (Top + Height > wa.Bottom) Top = wa.Bottom - Height;
            if (Left < wa.Left) Left = wa.Left;
            if (Top < wa.Top) Top = wa.Top;
            saveDeb.Trigger();
        }

        static Rect ClampDip(Rect r, Rect wa)
        {
            double w = Math.Min(r.Width, wa.Width), h = Math.Min(r.Height, wa.Height);
            double x = Math.Max(wa.Left, Math.Min(r.X, wa.Right - w)), y = Math.Max(wa.Top, Math.Min(r.Y, wa.Bottom - h));
            return new Rect(x, y, w, h);
        }

        public void ShowAt(double x, double y, double w, double h)
        {
            SuppressSave = true;
            var r = ValidRect(x, y, w, h);
            Left = r.X; Top = r.Y; Width = r.Width; Height = r.Height;
            Root.Opacity = 0;
            Show();
            shownOnce = true;
            SuppressSave = false;
            Root.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        }

        /// <summary>Écran qui contient le centre du panneau, et position relative à son espace de travail (unités WPF).</summary>
        protected void CurrentScreen(out string screen, out double relX, out double relY)
        {
            double scale = DpiScale();
            var center = new System.Drawing.Point((int)((Left + Width / 2) * scale), (int)((Top + Height / 2) * scale));
            var sc = WF.Screen.FromPoint(center);
            screen = sc.DeviceName;
            relX = Left - sc.WorkingArea.Left / scale;
            relY = Top - sc.WorkingArea.Top / scale;
        }

        /// <summary>Repositionne sans réenregistrer (changement d'écrans) ; la hauteur enroulée est conservée.</summary>
        public void PlaceSilently(Rect r)
        {
            SuppressSave = true;
            Left = r.X; Top = r.Y; Width = r.Width;
            if (!rolled) Height = r.Height; else restoreH = r.Height;
            SuppressSave = false;
        }

        public void ShowAt(Rect r) { ShowAt(r.X, r.Y, r.Width, r.Height); }

        Rect ValidRect(double x, double y, double w, double h)
        {
            double scale = 1.0;
            try { scale = 1.0 / DpiScale(); } catch { }
            var prim = WF.Screen.PrimaryScreen.WorkingArea;
            if (w < MinWidth) w = Math.Max(MinWidth, 320);
            if (h < TitleH) h = Math.Max(MinHeight, 240);
            if (double.IsNaN(x) || double.IsNaN(y) || (x == 0 && y == 0 && w == 0))
                return new Rect(prim.Left * scale + 24, prim.Top * scale + 24, w, h);
            var rect = new Rect(x, y, w, h);
            foreach (var s in WF.Screen.AllScreens)
            {
                var wa = new Rect(s.WorkingArea.Left * scale, s.WorkingArea.Top * scale, s.WorkingArea.Width * scale, s.WorkingArea.Height * scale);
                var inter = Rect.Intersect(rect, wa);
                if (inter != Rect.Empty && inter.Width >= 60 && inter.Height >= TitleH) return ClampDip(rect, wa);
            }
            return new Rect(prim.Left * scale + 24, prim.Top * scale + 24, w, h);
        }

        // ---- Menu « Nouveau » -------------------------------------------------------------------------

        public void ShowMenu()
        {
            var menu = new ContextMenu();
            menu.Items.Add(Item("Nouvelle tâche…", "IcoCheckSquare", delegate { c.QuickAdd(); }));
            menu.Items.Add(Item("Nouveau cadre…", "IcoGridLayout", delegate { c.NewCadre(this); }));
            menu.Items.Add(Item("Nouvelle note", "IcoNotes", delegate { c.NewNote(this); }));
            menu.Items.Add(Item("Nouvelle carte mentale…", "IcoGridLayout", delegate { c.NewMindMap(this); }));
            BuildMenu(menu);
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Taille par défaut", "IcoRefresh", ResetSize));
            menu.Items.Add(Item("Ramener à l'écran", null, FitOnScreen));
            menu.PlacementTarget = this;
            menu.Placement = PlacementMode.MousePoint;
            menuOpen = true;
            UpdateActionsVisibility();
            menu.Closed += delegate { menuOpen = false; UpdateActionsVisibility(); };
            menu.IsOpen = true;
        }

        protected static MenuItem Item(string header, string icon, Action action)
        {
            var mi = new MenuItem { Header = header };
            if (icon != null) mi.Icon = Theme.Icon(icon, 14);
            if (action != null) mi.Click += delegate { action(); };
            return mi;
        }

        protected static MenuItem Check(string header, bool isChecked, Action action)
        {
            var mi = new MenuItem { Header = header, IsChecked = isChecked, StaysOpenOnClick = false };
            mi.Click += delegate { action(); };
            return mi;
        }
    }
}
