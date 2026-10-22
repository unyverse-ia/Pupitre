using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using WF = System.Windows.Forms;

namespace Pupitre
{
    /// <summary>Capture rapide depuis n'importe quelle application (raccourci global) : nouvelle thématique dans une catégorie, ou tâche dans une thématique.</summary>
    public class QuickAddWindow : Window
    {
        readonly Controller c;
        TextBox box;
        TextBlock status, serviceLabel;
        Button catBtn, themeBtn, serviceBtn;
        TaskVm cat, theme;
        string serviceId;
        DispatcherTimer closeTimer;
        bool menuOpen, closing;
        static string lastCatId;

        public QuickAddWindow(Controller ctl)
        {
            c = ctl;
            Title = App.Name + " – Saisie rapide";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.Height;
            Width = 660;
            WindowStartupLocation = WindowStartupLocation.Manual;
            FontFamily = Theme.Font;
            UseLayoutRounding = true;

            var cats = c.Tasks.Categories();
            cat = cats.FirstOrDefault(x => x.Id == lastCatId) ?? cats.FirstOrDefault();

            var panel = new StackPanel();
            var head = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            var close = Theme.IconButton("IcoClose", "Fermer (Échap)", 13);
            close.Click += delegate { SafeClose(); };
            DockPanel.SetDock(close, Dock.Right);
            var icon = Theme.Icon("IcoCheckSquare", 15, Theme.Res<Brush>("Accent"));
            icon.Margin = new Thickness(2, 0, 8, 0);
            DockPanel.SetDock(icon, Dock.Left);
            head.Children.Add(close);
            head.Children.Add(icon);
            head.Children.Add(new TextBlock { Text = "Saisie rapide", Foreground = Theme.Res<Brush>("TextMuted"), FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(head);

            box = new TextBox { Style = Theme.Res<Style>("GhostTextBox"), FontSize = 18, Tag = "Nouvelle tâche…", Padding = new Thickness(4, 6, 4, 6) };
            box.KeyDown += BoxKey;
            panel.Children.Add(box);

            var opts = new WrapPanel { Margin = new Thickness(2, 4, 0, 0) };
            opts.Children.Add(Lbl("Catégorie"));
            catBtn = new Button { Style = Theme.Res<Style>("ChipBtn"), Margin = new Thickness(0, 0, 12, 0) };
            catBtn.Click += delegate { ShowCatMenu(); };
            opts.Children.Add(catBtn);
            opts.Children.Add(Lbl("Dans"));
            themeBtn = new Button { Style = Theme.Res<Style>("ChipBtn"), Margin = new Thickness(0, 0, 12, 0) };
            themeBtn.Click += delegate { ShowThemeMenu(); };
            opts.Children.Add(themeBtn);
            serviceLabel = Lbl("Service");
            opts.Children.Add(serviceLabel);
            serviceBtn = new Button { Style = Theme.Res<Style>("ChipBtn"), Content = "Aucun ▾" };
            serviceBtn.Click += delegate { ShowServiceMenu(); };
            opts.Children.Add(serviceBtn);
            panel.Children.Add(opts);

            panel.Children.Add(new System.Windows.Shapes.Rectangle { Height = 1, Margin = new Thickness(0, 8, 0, 8), Fill = Theme.Res<Brush>("Line") });
            panel.Children.Add(new TextBlock
            {
                Text = "Entrée : ajouter   ·   Échap : fermer        !  priorité importante        @demain  @lundi  @15/11  @+3  : échéance",
                Foreground = Theme.Res<Brush>("TextFaint"), FontSize = 11, TextWrapping = TextWrapping.Wrap
            });
            status = new TextBlock { Foreground = Theme.Res<Brush>("Ok"), FontSize = 12, Margin = new Thickness(0, 6, 0, 0), Visibility = Visibility.Collapsed };
            panel.Children.Add(status);

            Content = new Border
            {
                Background = Theme.Argb(0xF61C1D22),
                BorderBrush = Theme.Res<Brush>("MenuBorder"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16, 12, 16, 14),
                Margin = new Thickness(22),
                Child = panel,
                Effect = new DropShadowEffect { BlurRadius = 28, ShadowDepth = 4, Opacity = 0.6, Color = Colors.Black }
            };
            RefreshChips();

            Loaded += delegate
            {
                var wa = WF.Screen.PrimaryScreen.WorkingArea;
                double scale = 1.0;
                var src = PresentationSource.FromVisual(this);
                if (src != null && src.CompositionTarget != null) scale = 1.0 / src.CompositionTarget.TransformToDevice.M11;
                Left = wa.Left * scale + (wa.Width * scale - Width) / 2;
                Top = wa.Top * scale + wa.Height * scale * 0.2;
                Native.ForceForeground(new WindowInteropHelper(this).Handle);
                Activate();
                box.Focus();
                Keyboard.Focus(box);
            };
            Deactivated += delegate { if (!closing && closeTimer == null && !menuOpen) SafeClose(); };
        }

        /// <summary>Fermeture unique, même si la fenêtre est désactivée pendant sa fermeture.</summary>
        void SafeClose()
        {
            if (closing) return;
            closing = true;
            try { Close(); } catch { }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e) { closing = true; base.OnClosing(e); }

        static TextBlock Lbl(string text)
        {
            return new TextBlock { Text = text, Foreground = Theme.Res<Brush>("TextFaint"), FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) };
        }

        void RefreshChips()
        {
            catBtn.Content = (cat != null ? Trim(cat.Title) : "Nouvelle catégorie « Général »") + " ▾";
            themeBtn.Content = (theme != null ? Trim(theme.Title) : "Nouvelle tâche") + " ▾";
            box.Tag = theme != null ? "Nouvelle sous-tâche dans « " + Trim(theme.Title) + " »…" : "Nouvelle tâche…";
            bool svcVisible = theme == null;
            serviceLabel.Visibility = svcVisible ? Visibility.Visible : Visibility.Collapsed;
            serviceBtn.Visibility = svcVisible ? Visibility.Visible : Visibility.Collapsed;
            var s = c.Tasks.ServiceById(serviceId);
            serviceBtn.Content = (s != null ? s.Name : "Aucun") + " ▾";
        }

        void ShowCatMenu()
        {
            var menu = new ContextMenu();
            foreach (var k in c.Tasks.Categories())
            {
                var target = k;
                var mi = Mk(k.Title, cat == k, delegate { cat = target; theme = null; RefreshChips(); });
                mi.Icon = new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(5), Background = TaskColors.Solid(k.Color, k.CustomColor) };
                menu.Items.Add(mi);
            }
            if (menu.Items.Count > 0) menu.Items.Add(new Separator());
            menu.Items.Add(Mk("Nouvelle catégorie…", false, delegate
            {
                menuOpen = true;
                var name = Dialogs.Prompt(this, "Nouvelle catégorie", "Nom de la catégorie", "", false);
                menuOpen = false;
                if (!string.IsNullOrWhiteSpace(name)) { cat = c.Tasks.AddCategory(name); theme = null; RefreshChips(); }
                Activate(); box.Focus();
            }));
            Open(menu, catBtn);
        }

        void ShowThemeMenu()
        {
            var menu = new ContextMenu();
            menu.Items.Add(Mk("Nouvelle tâche", theme == null, delegate { theme = null; RefreshChips(); }));
            if (cat != null)
            {
                var themes = c.Tasks.OpenThemes(cat);
                if (themes.Count > 0) menu.Items.Add(new Separator());
                foreach (var t in themes)
                {
                    var target = t;
                    menu.Items.Add(Mk(t.Title + "   (ajouter une sous-tâche)", theme == t, delegate { theme = target; RefreshChips(); }));
                }
            }
            Open(menu, themeBtn);
        }

        void ShowServiceMenu()
        {
            var menu = new ContextMenu();
            menu.Items.Add(Mk("Aucun", serviceId == null, delegate { serviceId = null; RefreshChips(); }));
            foreach (var s in c.Tasks.Services)
            {
                var svc = s;
                var mi = Mk(svc.Name, serviceId == svc.Id, delegate { serviceId = svc.Id; RefreshChips(); });
                mi.Icon = new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(5), Background = TaskColors.Solid(svc.Color, svc.CustomColor) };
                menu.Items.Add(mi);
            }
            Open(menu, serviceBtn);
        }

        static string Trim(string s) { return s.Length > 28 ? s.Substring(0, 27) + "…" : s; }

        static MenuItem Mk(string header, bool isChecked, Action action)
        {
            var mi = new MenuItem { Header = header, IsChecked = isChecked };
            mi.Click += delegate { action(); };
            return mi;
        }

        void Open(ContextMenu menu, UIElement target)
        {
            menuOpen = true;
            menu.PlacementTarget = target;
            menu.Placement = PlacementMode.Bottom;
            menu.Closed += delegate { if (menuOpen) { menuOpen = false; Activate(); box.Focus(); } };
            menu.IsOpen = true;
        }

        void BoxKey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { SafeClose(); e.Handled = true; }
            else if (e.Key == Key.Enter)
            {
                string text = box.Text.Trim();
                if (text.Length == 0) return;
                if (cat == null) cat = c.Tasks.AddCategory("Général");
                lastCatId = cat.Id;
                string msg;
                if (theme != null)
                {
                    var k = c.Tasks.AddTask(theme, text);
                    msg = "✓  Sous-tâche ajoutée dans « " + Trim(theme.Title) + " » : " + k.Title;
                }
                else
                {
                    var t = c.Tasks.AddTheme(cat, text, serviceId);
                    msg = "✓  Tâche « " + t.Title + " » créée dans « " + Trim(cat.Title) + " »"
                        + (t.HasService ? "   ·   " + t.SvcName : "")
                        + (t.Flag ? "   ·   importante" : "")
                        + (t.DueVisible ? "   ·   " + t.DueText : "");
                }
                box.Clear();
                status.Text = msg;
                status.Visibility = Visibility.Visible;
                closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1100) };
                closeTimer.Tick += delegate { closeTimer.Stop(); SafeClose(); };
                closeTimer.Start();
                e.Handled = true;
            }
        }
    }
}
