using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// <summary>Boîte de dialogue sombre, sans bordure système, centrée sur sa fenêtre parente.</summary>
    public class DarkDialog : Window
    {
        protected readonly StackPanel Panel = new StackPanel();
        protected readonly DockPanel Footer = new DockPanel { Margin = new Thickness(0, 16, 0, 0) };
        protected Button OkBtn, CancelBtn;
        protected TextBlock TitleText;
        bool modal;

        /// <summary>Termine la boîte : DialogResult si elle est modale (ShowDialog), simple fermeture sinon (Show).</summary>
        protected void Finish(bool result)
        {
            if (modal) { try { DialogResult = result; return; } catch { } }
            Close();
        }

        public new bool? ShowDialog()
        {
            modal = true;
            return base.ShowDialog();
        }

        public DarkDialog(Window owner, string title, double width)
        {
            Title = App.Name + " – " + title;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.Height;
            Width = width + 36;
            FontFamily = Theme.Font;
            FontSize = 13;
            Foreground = Theme.Res<Brush>("SText");
            UseLayoutRounding = true;
            if (owner != null && owner.IsVisible) { Owner = owner; WindowStartupLocation = WindowStartupLocation.CenterOwner; }
            else WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var outer = new StackPanel();
            var head = new DockPanel { Margin = new Thickness(0, 0, 0, 10), Background = Brushes.Transparent, Cursor = Cursors.SizeAll };
            head.MouseLeftButtonDown += delegate { try { DragMove(); } catch { } };
            var close = Theme.IconButton("IcoClose", "Fermer", 13);
            close.Click += delegate { Finish(false); };
            DockPanel.SetDock(close, Dock.Right);
            TitleText = new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 15, VerticalAlignment = VerticalAlignment.Center };
            head.Children.Add(close);
            head.Children.Add(TitleText);
            outer.Children.Add(head);
            outer.Children.Add(Panel);

            CancelBtn = new Button { Style = Theme.Res<Style>("SBtn"), Content = "Annuler", Margin = new Thickness(0, 0, 8, 0) };
            CancelBtn.Click += delegate { Finish(false); };
            OkBtn = new Button { Style = Theme.Res<Style>("SBtnAccent"), Content = "OK", IsDefault = true };
            OkBtn.Click += delegate { if (Validate()) Finish(true); };
            DockPanel.SetDock(OkBtn, Dock.Right);
            DockPanel.SetDock(CancelBtn, Dock.Right);
            Footer.Children.Add(OkBtn);
            Footer.Children.Add(CancelBtn);
            outer.Children.Add(Footer);

            Content = new Border
            {
                Background = Theme.Res<Brush>("SBg"),
                BorderBrush = Theme.Res<Brush>("SLine"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(18, 14, 18, 16),
                Margin = new Thickness(18),
                Child = outer,
                Effect = new DropShadowEffect { BlurRadius = 24, ShadowDepth = 3, Opacity = 0.55, Color = Colors.Black }
            };
            PreviewKeyDown += delegate(object s, KeyEventArgs e) { if (e.Key == Key.Escape) { Finish(false); e.Handled = true; } };
        }

        protected virtual bool Validate() { return true; }

        public StackPanel PanelPublic { get { return Panel; } }
        public void CommitPublic() { if (Validate()) Finish(true); }
        public void SetOk(string text, bool danger)
        {
            OkBtn.Content = text;
            if (danger) OkBtn.Style = Theme.Res<Style>("SBtnDanger");
        }
        public void HideCancel() { CancelBtn.Visibility = Visibility.Collapsed; }

        /// <summary>Place les réglages dans une zone défilante bornée à l'écran : la boîte ne dépasse jamais, les boutons du bas restent visibles.</summary>
        protected void MakeScrollable()
        {
            var outer = Panel.Parent as StackPanel;
            if (outer == null) return;
            int idx = outer.Children.IndexOf(Panel);
            if (idx < 0) return;
            outer.Children.RemoveAt(idx);
            var sv = new ScrollViewer { Content = Panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Focusable = false, Padding = new Thickness(0, 0, 8, 0), MaxHeight = Math.Max(320, SystemParameters.WorkArea.Height - 220) };
            outer.Children.Insert(idx, sv);
        }

        // ---- aides de mise en page ----
        protected TextBlock Label(string text)
        {
            var tb = new TextBlock { Text = text, Foreground = Theme.Res<Brush>("SMuted"), FontSize = 12, Margin = new Thickness(0, 10, 0, 4) };
            Panel.Children.Add(tb);
            return tb;
        }
        protected TextBlock Note(string text)
        {
            var tb = new TextBlock { Text = text, Foreground = Theme.Res<Brush>("SMuted"), FontSize = 11.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) };
            Panel.Children.Add(tb);
            return tb;
        }
        protected TextBox Field(string text, string hint)
        {
            var tb = new TextBox { Style = Theme.Res<Style>("SFieldBox"), Text = text ?? "", Tag = hint };
            Panel.Children.Add(tb);
            return tb;
        }
        protected CheckBox Check(string text, bool value)
        {
            var cb = new CheckBox { Style = Theme.Res<Style>("SCheck"), Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }, IsChecked = value, Margin = new Thickness(0, 10, 0, 0) };
            Panel.Children.Add(cb);
            return cb;
        }
        protected RadioButton Chip(string text, string group, bool isChecked)
        {
            return new RadioButton { Style = Theme.Res<Style>("SChip"), Content = text, GroupName = group, IsChecked = isChecked };
        }
    }

    /// <summary>
    /// Bloc « Apparence » commun aux paramètres d'un cadre et d'un panneau : pastilles, couleur personnalisée (palette Windows),
    /// opacité 0..100 au curseur, verrou. Les changements s'appliquent en direct sur la fenêtre ; Annuler les retire.
    /// </summary>
    public class LookEditor
    {
        readonly DesktopWindow target;
        readonly int color0, opacity0; readonly string custom0;
        public int Color; public string Custom; public int Opacity; public bool Locked;
        readonly ColorPicker picker;
        readonly Border swatch;
        readonly Button customBtn;
        readonly TextBlock opValue;
        public readonly Slider Slider;
        public readonly CheckBox LockBox;

        public LookEditor(StackPanel panel, DesktopWindow w, int color, string custom, int opacity, bool locked)
        {
            target = w; color0 = color; custom0 = custom; opacity0 = opacity;
            Color = color; Custom = custom; Opacity = opacity < 0 ? Palette.DefaultOpacity : opacity; Locked = locked;

            panel.Children.Add(new TextBlock { Text = "Couleur", Foreground = Theme.Res<Brush>("SMuted"), FontSize = 12, Margin = new Thickness(0, 10, 0, 4) });
            picker = new ColorPicker(color);
            picker.Changed += delegate { Color = picker.Selected; RefreshCustom(); Preview(); };
            panel.Children.Add(picker);
            var row = new DockPanel { Margin = new Thickness(0, 2, 0, 0) };
            swatch = new Border { Width = 22, Height = 22, CornerRadius = new CornerRadius(11), BorderThickness = new Thickness(2), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), Cursor = Cursors.Hand, ToolTip = "Couleur personnalisée" };
            swatch.Child = new Ellipse { Width = 14, Height = 14, Stroke = Theme.Argb(0x66FFFFFF), StrokeThickness = 1 };
            swatch.MouseLeftButtonDown += delegate { if (!string.IsNullOrEmpty(Custom)) { Color = -1; picker.Selected = -1; RefreshCustom(); Preview(); } else PickCustom(); };
            DockPanel.SetDock(swatch, Dock.Left);
            customBtn = new Button { Style = Theme.Res<Style>("SBtn"), Content = "Couleur personnalisée…", Padding = new Thickness(10, 3, 10, 4), HorizontalAlignment = HorizontalAlignment.Left, ToolTip = "Choisir n'importe quelle couleur (palette Windows)" };
            customBtn.Click += delegate { PickCustom(); };
            row.Children.Add(swatch);
            row.Children.Add(customBtn);
            panel.Children.Add(row);
            RefreshCustom();

            var head = new DockPanel { Margin = new Thickness(0, 12, 0, 2) };
            opValue = new TextBlock { Text = Opacity + " %", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(opValue, Dock.Right);
            head.Children.Add(opValue);
            head.Children.Add(new TextBlock { Text = "Opacité du fond", Foreground = Theme.Res<Brush>("SMuted"), FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(head);
            Slider = new Slider { Style = Theme.Res<Style>("DarkSlider"), Minimum = 0, Maximum = 100, Value = Opacity, IsSnapToTickEnabled = true, TickFrequency = 1, IsMoveToPointEnabled = true, Margin = new Thickness(0, 0, 0, 2), ToolTip = "0 % : transparent · 100 % : opaque" };
            Slider.ValueChanged += delegate { Opacity = (int)Math.Round(Slider.Value); opValue.Text = Opacity + " %"; Preview(); };
            panel.Children.Add(Slider);

            LockBox = new CheckBox { Style = Theme.Res<Style>("SCheck"), Content = new TextBlock { Text = "Verrouiller ce panneau (position et taille figées)", TextWrapping = TextWrapping.Wrap }, IsChecked = locked, Margin = new Thickness(0, 10, 0, 0) };
            LockBox.Checked += delegate { Locked = true; };
            LockBox.Unchecked += delegate { Locked = false; };
            panel.Children.Add(LockBox);

            panel.Children.Add(new TextBlock { Text = "Taille et position", Foreground = Theme.Res<Brush>("SMuted"), FontSize = 12, Margin = new Thickness(0, 12, 0, 4) });
            var sizeRow = new WrapPanel();
            var reset = new Button { Style = Theme.Res<Style>("SBtn"), Content = "Taille par défaut", Padding = new Thickness(10, 3, 10, 4), Margin = new Thickness(0, 0, 8, 4), ToolTip = "Remet le panneau à une taille raisonnable, entièrement visible sur son écran" };
            reset.Click += delegate { if (target != null) target.ResetSize(); };
            var fit = new Button { Style = Theme.Res<Style>("SBtn"), Content = "Ramener à l'écran", Padding = new Thickness(10, 3, 10, 4), Margin = new Thickness(0, 0, 0, 4), ToolTip = "Déplace et réduit le panneau pour qu'il tienne dans l'écran, bords accessibles" };
            fit.Click += delegate { if (target != null) target.FitOnScreen(); };
            sizeRow.Children.Add(reset); sizeRow.Children.Add(fit);
            panel.Children.Add(sizeRow);
        }

        void RefreshCustom()
        {
            bool has = !string.IsNullOrEmpty(Custom);
            var col = Palette.ParseHex(Custom, System.Windows.Media.Color.FromRgb(0x5B, 0x8D, 0xEF));
            ((Ellipse)swatch.Child).Fill = has ? Theme.Brush(0xFF, col) : Theme.Argb(0x33FFFFFF);
            swatch.BorderBrush = Color < 0 ? Brushes.White : Brushes.Transparent;
            customBtn.Content = Color < 0 ? "Couleur personnalisée " + (Custom ?? "") + "  (changer…)" : (has ? "Reprendre " + Custom + "…" : "Couleur personnalisée…");
        }

        void PickCustom()
        {
            var initial = Palette.ParseHex(Custom, Palette.Tint(Color < 0 ? 0 : Color));
            using (var dlg = new WF.ColorDialog { FullOpen = true, AnyColor = true, Color = System.Drawing.Color.FromArgb(initial.R, initial.G, initial.B) })
            {
                // propriétaire : la boîte de paramètres elle-même (toujours au premier plan), pas le panneau collé au bureau
                var host = Window.GetWindow(picker);
                IntPtr h = host != null ? new WindowInteropHelper(host).Handle : IntPtr.Zero;
                var res = h != IntPtr.Zero ? dlg.ShowDialog(new Dialogs.Win32Owner(h)) : dlg.ShowDialog();
                if (res != WF.DialogResult.OK) return;
                Custom = Palette.ToHex(System.Windows.Media.Color.FromRgb(dlg.Color.R, dlg.Color.G, dlg.Color.B));
                Color = -1; picker.Selected = -1;
                RefreshCustom(); Preview();
            }
        }

        void Preview() { if (target != null) target.SetLook(Color, Opacity, Custom); }

        /// <summary>Annulation : la fenêtre reprend son apparence d'avant la boîte.</summary>
        public void Revert() { if (target != null) target.SetLook(color0, opacity0, custom0); }
    }

    /// <summary>Paramètres d'un panneau (Tâches, Notes) : apparence et verrou.</summary>
    public class PanelSettingsDialog : DarkDialog
    {
        readonly PanelInfo info;
        readonly LookEditor look;
        bool saved;

        public PanelSettingsDialog(DesktopWindow w, Controller c, PanelInfo p, string what) : base(w, "Paramètres du panneau " + what, 420)
        {
            info = p;
            look = new LookEditor(Panel, w, p.Color, p.CustomColor, p.Opacity, p.Locked);
            Note("Position, taille et apparence sont mémorisées par écran et restaurées au prochain démarrage.");
            OkBtn.Content = "Enregistrer";
            Closed += delegate { if (!saved) look.Revert(); };
        }

        protected override bool Validate()
        {
            info.Color = look.Color; info.CustomColor = look.Custom; info.Opacity = look.Opacity; info.Locked = look.Locked;
            saved = true;
            return true;
        }
    }

    /// <summary>Sélecteur de couleur d'accent (pastilles).</summary>
    public class ColorPicker : WrapPanel
    {
        readonly Border[] dots = new Border[Palette.Colors.Length];
        int selected;
        public int Selected { get { return selected; } set { selected = value; Refresh(); } }
        public event Action Changed;

        public ColorPicker(int initial)
        {
            selected = initial;
            for (int i = 0; i < dots.Length; i++)
            {
                int v = i;
                var dot = new Border { Width = 22, Height = 22, CornerRadius = new CornerRadius(11), Margin = new Thickness(0, 0, 8, 4), Cursor = Cursors.Hand, ToolTip = Palette.Names[i], BorderThickness = new Thickness(2), BorderBrush = Brushes.Transparent };
                dot.Child = new Ellipse { Width = 14, Height = 14, Fill = Theme.Brush(0xFF, i == 0 ? Palette.Tint(0) : Palette.Get(i)), Stroke = Theme.Argb(0x66FFFFFF), StrokeThickness = 1 };
                dot.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { selected = v; Refresh(); var h = Changed; if (h != null) h(); e.Handled = true; };
                dots[i] = dot;
                Children.Add(dot);
            }
            Refresh();
        }

        void Refresh()
        {
            for (int i = 0; i < dots.Length; i++) dots[i].BorderBrush = i == selected ? Brushes.White : Brushes.Transparent;
        }
    }

    internal static class Dialogs
    {
        public static string Prompt(Window owner, string title, string label, string initial, bool selectStem)
        {
            var d = new DarkDialog(owner, title, 380);
            var tb = new TextBox { Style = Theme.Res<Style>("SFieldBox"), Text = initial ?? "", Tag = label };
            d.Loaded += delegate
            {
                tb.Focus();
                int dot = selectStem ? tb.Text.LastIndexOf('.') : -1;
                tb.Select(0, dot > 0 ? dot : tb.Text.Length);
            };
            var p = d.PanelPublic;
            var lab = new TextBlock { Text = label, Foreground = Theme.Res<Brush>("SMuted"), FontSize = 12, Margin = new Thickness(0, 4, 0, 4) };
            p.Children.Add(lab);
            p.Children.Add(tb);
            return d.ShowDialog() == true ? tb.Text.Trim() : null;
        }

        public static string PromptMultiline(Window owner, string title, string label, string initial)
        {
            var d = new DarkDialog(owner, title, 480);
            var tb = new TextBox { Style = Theme.Res<Style>("SFieldBox"), Text = initial ?? "", AcceptsReturn = true, AcceptsTab = true, TextWrapping = TextWrapping.Wrap, MinHeight = 140, MaxHeight = 360, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalContentAlignment = VerticalAlignment.Top };
            d.Loaded += delegate { tb.Focus(); tb.CaretIndex = tb.Text.Length; };
            var p = d.PanelPublic;
            p.Children.Add(new TextBlock { Text = label, Foreground = Theme.Res<Brush>("SMuted"), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 6) });
            p.Children.Add(tb);
            p.Children.Add(new TextBlock { Text = "Ctrl+Entrée pour valider", Foreground = Theme.Res<Brush>("SMuted"), FontSize = 11, Margin = new Thickness(0, 6, 0, 0) });
            tb.PreviewKeyDown += delegate(object s, KeyEventArgs e) { if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) != 0) { d.SetOk("OK", false); d.CommitPublic(); e.Handled = true; } };
            return d.ShowDialog() == true ? tb.Text : null;
        }

        public static bool Confirm(Window owner, string title, string message, string okText, bool danger)
        {
            var d = new DarkDialog(owner, title, 400);
            d.PanelPublic.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0), FontSize = 13 });
            d.SetOk(okText, danger);
            return d.ShowDialog() == true;
        }

        public static void Info(Window owner, string title, string message)
        {
            var d = new DarkDialog(owner, title, 400);
            d.PanelPublic.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0), FontSize = 13 });
            d.SetOk("OK", false);
            d.HideCancel();
            d.ShowDialog();
        }

        public static string BrowseFolder(Window owner, string initial, string description)
        {
            using (var dlg = new WF.FolderBrowserDialog())
            {
                dlg.Description = description;
                dlg.ShowNewFolderButton = true;
                if (!string.IsNullOrEmpty(initial) && Directory.Exists(initial)) dlg.SelectedPath = initial;
                var h = owner != null ? new WindowInteropHelper(owner).Handle : IntPtr.Zero;
                var res = h != IntPtr.Zero ? dlg.ShowDialog(new Win32Owner(h)) : dlg.ShowDialog();
                return res == WF.DialogResult.OK ? dlg.SelectedPath : null;
            }
        }

        internal class Win32Owner : WF.IWin32Window
        {
            readonly IntPtr h;
            public Win32Owner(IntPtr handle) { h = handle; }
            public IntPtr Handle { get { return h; } }
        }
    }

    /// <summary>Création d'un cadre : nom, dossier (nouveau dossier caché du bureau ou dossier existant), couleur.</summary>
    public class NewCadreDialog : DarkDialog
    {
        readonly TextBox name, path;
        readonly RadioButton newFolder, existing;
        readonly ColorPicker colors;
        readonly Controller c;
        public CadreInfo Result;

        public NewCadreDialog(Window owner, Controller ctl) : base(owner, "Nouveau cadre", 440)
        {
            c = ctl;
            Label("Nom du cadre");
            name = Field("", "Ex. Projets, Outils, À trier…");
            Label("Contenu");
            var modes = new WrapPanel();
            newFolder = Chip("Nouveau dossier sur le bureau (caché)", "mode", true);
            existing = Chip("Dossier existant…", "mode", false);
            modes.Children.Add(newFolder); modes.Children.Add(existing);
            Panel.Children.Add(modes);
            var row = new DockPanel { Margin = new Thickness(0, 4, 0, 0), Visibility = Visibility.Collapsed };
            var browse = new Button { Style = Theme.Res<Style>("SBtn"), Content = "Parcourir…", Margin = new Thickness(6, 0, 0, 0) };
            DockPanel.SetDock(browse, Dock.Right);
            path = new TextBox { Style = Theme.Res<Style>("SFieldBox"), Tag = "Chemin du dossier" };
            row.Children.Add(browse); row.Children.Add(path);
            Panel.Children.Add(row);
            browse.Click += delegate { var f = Dialogs.BrowseFolder(this, path.Text, "Dossier affiché par le cadre"); if (f != null) { path.Text = f; if (name.Text.Trim().Length == 0) name.Text = System.IO.Path.GetFileName(f); } };
            existing.Checked += delegate { row.Visibility = Visibility.Visible; };
            newFolder.Checked += delegate { row.Visibility = Visibility.Collapsed; };
            Note("Un cadre affiche le contenu d'un dossier : glissez-y des fichiers depuis le bureau, un autre cadre ou l'Explorateur.");
            Label("Couleur");
            colors = new ColorPicker(1);
            Panel.Children.Add(colors);
            OkBtn.Content = "Créer le cadre";
            Loaded += delegate { name.Focus(); };
        }

        protected override bool Validate()
        {
            string n = name.Text.Trim();
            if (n.Length == 0) { name.Focus(); return false; }
            var info = new CadreInfo { Id = Guid.NewGuid().ToString("N"), Name = n, Color = colors.Selected };
            if (existing.IsChecked == true)
            {
                if (!Directory.Exists(path.Text.Trim())) { Dialogs.Info(this, "Dossier introuvable", "Choisissez un dossier existant."); return false; }
                info.Folder = App.Norm(path.Text.Trim());
                info.HideFolder = false;
            }
            else
            {
                if (n.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0) { Dialogs.Info(this, "Nom invalide", "Le nom contient des caractères interdits pour un dossier."); return false; }
                string folder = System.IO.Path.Combine(App.Desktop, FileOps.UniqueName(App.Desktop, n));
                try
                {
                    Directory.CreateDirectory(folder);
                    File.SetAttributes(folder, File.GetAttributes(folder) | FileAttributes.Hidden);
                }
                catch (Exception ex) { Dialogs.Info(this, "Impossible de créer le dossier", ex.Message); return false; }
                info.Folder = folder;
                info.HideFolder = true;
            }
            Result = info;
            return true;
        }
    }

    /// <summary>Réglages d'un cadre existant.</summary>
    public class CadreSettingsDialog : DarkDialog
    {
        readonly CadreWindow cadre;
        readonly Controller c;
        readonly TextBox name, rules, folderBox;
        readonly LookEditor look;
        readonly RadioButton[] sizes = new RadioButton[4];
        readonly RadioButton[] sorts = new RadioButton[3];
        readonly CheckBox hide;
        readonly RadioButton[] fusion;
        readonly List<CadreWindow> fusionTargets;
        public string FusionTarget;     // identifiant du sommet de colonne choisi (null : cadre indépendant)
        public bool FusionChanged;
        public bool Deleted;
        bool saved;

        public CadreSettingsDialog(CadreWindow w, Controller ctl) : base(w, "Paramètres du cadre", 460)
        {
            cadre = w; c = ctl;
            var info = w.Info;
            Label("Nom");
            name = Field(info.Name, "");
            Label("Dossier");
            var row = new DockPanel();
            var open = new Button { Style = Theme.Res<Style>("SBtn"), Content = "Ouvrir", Margin = new Thickness(6, 0, 0, 0) };
            open.Click += delegate { FileOps.OpenFolder(w.Folder); };
            DockPanel.SetDock(open, Dock.Right);
            folderBox = new TextBox { Style = Theme.Res<Style>("SKey"), Text = w.Folder, ToolTip = w.Folder };
            if (!info.IsDesktop)
            {
                var change = new Button { Style = Theme.Res<Style>("SBtn"), Content = "Changer…", Margin = new Thickness(6, 0, 0, 0) };
                change.Click += delegate { var f = Dialogs.BrowseFolder(this, w.Folder, "Dossier affiché par le cadre"); if (f != null) folderBox.Text = f; };
                DockPanel.SetDock(change, Dock.Right);
                row.Children.Add(change);
            }
            row.Children.Add(open);
            row.Children.Add(folderBox);
            Panel.Children.Add(row);
            if (info.IsDesktop) Note("Ce cadre affiche les icônes de votre bureau Windows (et du bureau public).");

            look = new LookEditor(Panel, w, info.Color, info.CustomColor, info.Opacity, info.Locked);

            Label("Taille des icônes");
            var sp = new WrapPanel();
            sizes[0] = Chip("Par défaut", "size", info.IconSize < 0);
            sp.Children.Add(sizes[0]);
            for (int i = 0; i < 3; i++) { sizes[i + 1] = Chip(IconSizes.Names[i], "size", info.IconSize == i); sp.Children.Add(sizes[i + 1]); }
            Panel.Children.Add(sp);

            Label("Tri");
            var so = new WrapPanel();
            sorts[0] = Chip("Nom", "sort", info.Sort == "name"); sorts[1] = Chip("Type", "sort", info.Sort == "type"); sorts[2] = Chip("Date", "sort", info.Sort == "date");
            foreach (var s in sorts) so.Children.Add(s);
            Panel.Children.Add(so);

            Label("Fusion avec d'autres cadres");
            fusionTargets = Fusion.Targets(ctl, w);
            var top = Fusion.IsBelow(ctl, w) ? Fusion.Top(ctl, w) : null;
            var fp = new WrapPanel();
            fusion = new RadioButton[fusionTargets.Count + 1];
            fusion[0] = Chip("Aucune (cadre indépendant)", "fusion", top == null);
            fp.Children.Add(fusion[0]);
            for (int i = 0; i < fusionTargets.Count; i++) { fusion[i + 1] = Chip("Sous « " + fusionTargets[i].Info.Name + " »", "fusion", top == fusionTargets[i]); fp.Children.Add(fusion[i + 1]); }
            Panel.Children.Add(fp);
            Note("Les cadres fusionnés forment une colonne : même largeur, collés l'un sous l'autre, déplacés ensemble ; chacun se replie sur son titre (double-clic) et se déplie. Les panneaux Tâches, Notes et Cartes mentales restent indépendants.");

            if (!info.IsDesktop)
            {
                Label("Tri automatique : fichiers du bureau à ranger ici");
                rules = Field(info.Rules, "Ex. *.pdf; *.docx; facture*");
                Note("Quand un fichier correspondant apparaît sur le bureau, il est déplacé dans ce cadre (option « Tri automatique » des paramètres).");
                bool underDesktop = App.Norm(w.Folder).StartsWith(App.Norm(App.Desktop) + "\\", StringComparison.OrdinalIgnoreCase);
                hide = Check("Cacher ce dossier sur le bureau Windows", info.HideFolder);
                hide.Visibility = underDesktop ? Visibility.Visible : Visibility.Collapsed;
            }

            if (!info.IsDesktop)
            {
                var del = new Button { Style = Theme.Res<Style>("SBtnDanger"), Content = "Supprimer le cadre", HorizontalAlignment = HorizontalAlignment.Left };
                del.Click += delegate
                {
                    if (Dialogs.Confirm(this, "Supprimer le cadre", "Le cadre « " + info.Name + " » sera retiré du bureau.\nSon dossier et les fichiers qu'il contient ne sont pas supprimés :\n" + w.Folder, "Supprimer le cadre", true))
                    {
                        Deleted = true;
                        Finish(false);
                    }
                };
                DockPanel.SetDock(del, Dock.Left);
                Footer.Children.Add(del);
            }
            OkBtn.Content = "Enregistrer";
            Closed += delegate { if (!saved) look.Revert(); };
        }

        protected override bool Validate()
        {
            var info = cadre.Info;
            string n = name.Text.Trim();
            if (n.Length == 0) { name.Focus(); return false; }
            info.Name = n;
            info.Color = look.Color; info.CustomColor = look.Custom; info.Opacity = look.Opacity; info.Locked = look.Locked;
            info.IconSize = sizes[0].IsChecked == true ? -1 : (sizes[1].IsChecked == true ? 0 : (sizes[2].IsChecked == true ? 1 : 2));
            info.Sort = sorts[1].IsChecked == true ? "type" : (sorts[2].IsChecked == true ? "date" : "name");
            var curTop = Fusion.IsBelow(c, cadre) ? Fusion.Top(c, cadre) : null;
            CadreWindow chosen = null;
            for (int i = 0; i < fusionTargets.Count; i++) if (fusion[i + 1].IsChecked == true) chosen = fusionTargets[i];
            FusionTarget = chosen != null ? chosen.Info.Id : null;
            FusionChanged = chosen != curTop;
            if (!info.IsDesktop)
            {
                string f = App.Norm(folderBox.Text.Trim());
                if (!Directory.Exists(f)) { Dialogs.Info(this, "Dossier introuvable", "Choisissez un dossier existant."); return false; }
                info.Folder = f;
                info.Rules = rules.Text.Trim();
                if (hide.Visibility == Visibility.Visible)
                {
                    info.HideFolder = hide.IsChecked == true;
                    try
                    {
                        var a = File.GetAttributes(f);
                        File.SetAttributes(f, info.HideFolder ? (a | FileAttributes.Hidden) : (a & ~FileAttributes.Hidden));
                    }
                    catch { }
                }
            }
            saved = true;
            return true;
        }
    }

    /// <summary>Premier lancement.</summary>
    public class WelcomeDialog : DarkDialog
    {
        public CheckBox HideIcons;
        public WelcomeDialog(Controller c) : base(null, "Bienvenue dans " + App.Name, 470)
        {
            Row("IcoGridLayout", "Des cadres sur votre bureau", "Chaque cadre affiche un dossier. Glissez-y vos fichiers et raccourcis depuis le cadre « Bureau », un autre cadre ou l'Explorateur. Double-clic sur un titre pour enrouler, clic droit pour les options.");
            Row("IcoCheckSquare", "Vos tâches, toujours sous la main", "Le panneau Tâches note vos demandes : priorités, échéances, étiquettes. Depuis n'importe quelle application, " + Native.HotkeyText(c.S.QuickMods, c.S.QuickVk) + " ouvre la saisie rapide.");
            Row("IcoEye", "Un bureau dégagé", "Les cadres restent sous vos fenêtres et survivent à Win+D. Tout se règle depuis l'icône " + App.Name + " de la zone de notification.");
            HideIcons = Check("Masquer les icônes Windows du bureau (le cadre « Bureau » les affiche à la place)", true);
            Note("Réversible à tout moment dans les paramètres. Les icônes réapparaissent quand " + App.Name + " est fermé.");
            OkBtn.Content = "Commencer";
            HideCancel();
        }

        void Row(string icon, string title, string text)
        {
            var g = new DockPanel { Margin = new Thickness(0, 10, 0, 0) };
            var ic = Theme.Icon(icon, 20, Theme.Res<Brush>("Accent"));
            ic.VerticalAlignment = VerticalAlignment.Top;
            ic.Margin = new Thickness(0, 2, 12, 0);
            DockPanel.SetDock(ic, Dock.Left);
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 13 });
            sp.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Foreground = Theme.Res<Brush>("SMuted"), FontSize = 12, Margin = new Thickness(0, 2, 0, 0) });
            g.Children.Add(ic); g.Children.Add(sp);
            Panel.Children.Add(g);
        }
    }
}
