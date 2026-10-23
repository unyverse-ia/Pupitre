using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Pupitre
{
    /// <summary>Paramètres généraux (brouillon + Enregistrer). Couleur, opacité et verrouillage se règlent sur chaque panneau.</summary>
    public class SettingsWindow : DarkDialog
    {
        readonly Controller c;
        readonly Settings d;
        CheckBox cbStart, cbSnap, cbHide, cbExt, cbAuto, cbRemind, cbTasks, cbNotes, cbMind, cbLock, cbDock;
        RadioButton[] iconSize = new RadioButton[3];
        StackPanel cadreList, windowList, profileList;
        TextBlock status, startupNote;

        public SettingsWindow(Controller ctl) : base(null, "Paramètres", 540)
        {
            c = ctl;
            d = new Settings();
            Copy(c.S, d);
            Activated += delegate { SyncLive(); };

            Section("Général");
            var g1 = new UniformGrid { Columns = 2 };
            cbStart = Opt(g1, "Lancer avec Windows", d.StartWithWindows);
            cbSnap = Opt(g1, "Aimanter les panneaux", d.Snap);
            cbHide = Opt(g1, "Masquer les icônes du bureau", d.HideDesktopIcons);
            cbExt = Opt(g1, "Afficher les extensions", d.ShowExtensions);
            cbAuto = Opt(g1, "Tri automatique (règles des cadres)", d.AutoSort);
            cbLock = Opt(g1, "Verrouiller tous les panneaux", d.LockLayout);
            cbDock = Opt(g1, "Fusion des cadres au dépôt", !d.NoAutoDock);
            cbDock.ToolTip = "Un cadre déposé contre le bas (ou le haut) d'un autre s'y colle en colonne";
            Panel.Children.Add(g1);
            Note("« Masquer les icônes du bureau » : le cadre « Bureau » les affiche à la place ; elles reviennent quand " + App.Name + " est fermé.");
            startupNote = Note(Startup.Describe());
            var row2 = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
            row2.Children.Add(Lbl("Taille des icônes par défaut"));
            var wp2 = new WrapPanel();
            for (int i = 0; i < 3; i++) { iconSize[i] = Chip(IconSizes.Names[i], "isz", d.IconSize == i); wp2.Children.Add(iconSize[i]); }
            row2.Children.Add(wp2);
            Panel.Children.Add(row2);
            Note("Couleur, opacité et verrou d\u2019un panneau : bouton « Paramètres du cadre » dans sa barre de titre.");

            Section("Panneaux");
            var g2 = new UniformGrid { Columns = 2 };
            cbTasks = Opt(g2, "Panneau Tâches", d.TasksVisible);
            cbNotes = Opt(g2, "Panneau Notes", d.NotesVisible);
            cbMind = Opt(g2, "Panneau Cartes mentales", d.MindVisible);
            Panel.Children.Add(g2);

            Section("Tâches");
            cbRemind = Check("Rappel quotidien des tâches du jour", d.DailyReminder);
            Note("Raccourcis : " + Native.HotkeyText(d.QuickMods, d.QuickVk) + " saisie rapide depuis n'importe où · " + Native.HotkeyText(d.ToggleMods, d.ToggleVk) + " afficher / masquer les panneaux.");
            Note("Services (badges colorés) et catégories : paramètres du panneau Tâches (icône engrenage de sa barre de titre, ou menu « … »).");
            Section("Dispositions par configuration d'écrans");
            Note("Chaque configuration d'écrans (bureau à trois écrans, portable seul…) mémorise sa propre disposition, appliquée automatiquement quand les écrans changent. Vous pouvez aussi enregistrer des instantanés nommés et les restaurer à tout moment.");
            profileList = new StackPanel();
            Panel.Children.Add(profileList);
            RefreshProfiles();
            var snap = new Button { Style = Theme.Res<Style>("SBtn"), Content = "Enregistrer la disposition actuelle…", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
            snap.Click += delegate
            {
                var name = Dialogs.Prompt(this, "Instantané de disposition", "Nom (ex : Bureau 3 écrans)", Controller.ScreensDescription(), false);
                if (!string.IsNullOrWhiteSpace(name)) { c.SaveSnapshot(name); RefreshProfiles(); }
            };
            Panel.Children.Add(snap);

            Section("Cadres");
            cadreList = new StackPanel();
            Panel.Children.Add(cadreList);
            RefreshCadres();
            var add = new Button { Style = Theme.Res<Style>("SBtn"), Content = "Nouveau cadre…", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
            add.Click += delegate { c.NewCadre(this); RefreshCadres(); };
            var fusionBtn = new Button { Style = Theme.Res<Style>("SBtn"), Content = "Fusion des cadres…", Margin = new Thickness(8, 8, 0, 0), ToolTip = "Colonnes de cadres collés : organiser par glisser-déposer" };
            fusionBtn.Click += delegate { new FusionDialog(this, c).ShowDialog(); RefreshCadres(); };
            var addRow = new WrapPanel();
            addRow.Children.Add(add); addRow.Children.Add(fusionBtn);
            Panel.Children.Add(addRow);

            Section("Fenêtres : retrouver un cadre ou un panneau");
            Panel.Children.Add(new TextBlock { Text = "Si une fenêtre a été déplacée hors de l'écran ou agrandie à l'excès : « Ramener à l'écran » la replace entièrement dans son écran, « Taille par défaut » lui redonne une taille raisonnable.", Foreground = Theme.Res<Brush>("SMuted"), FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 4) });
            windowList = new StackPanel();
            Panel.Children.Add(windowList);
            RefreshWindows();
            var fitAll = new Button { Style = Theme.Res<Style>("SBtn"), Content = "Ramener toutes les fenêtres à l'écran", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
            fitAll.Click += delegate { c.FitAllOnScreen(); RefreshWindows(); };
            Panel.Children.Add(fitAll);

            status = new TextBlock { Foreground = Theme.Res<Brush>("SError"), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0), Visibility = Visibility.Collapsed };
            Panel.Children.Add(status);
            var infoRow = new DockPanel { Margin = new Thickness(0, 12, 0, 0) };
            var data = new Button { Style = Theme.Res<Style>("SBtn"), Content = "Dossier des données", Padding = new Thickness(10, 3, 10, 4), ToolTip = App.DataDir + "\nsettings.json, layout.json, tasks.json, archive.json" };
            data.Click += delegate { c.OpenDataFolder(); };
            DockPanel.SetDock(data, Dock.Right);
            infoRow.Children.Add(data);
            infoRow.Children.Add(new TextBlock { Text = "100 % hors ligne · aucune connexion réseau, aucune télémétrie.", Foreground = Theme.Res<Brush>("SMuted"), FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
            Panel.Children.Add(infoRow);

            var quit = new Button { Style = Theme.Res<Style>("SBtn"), Content = "Quitter " + App.Name, ToolTip = "Ferme complètement l'application", HorizontalAlignment = HorizontalAlignment.Left };
            quit.Click += delegate { c.Quit(); };
            DockPanel.SetDock(quit, Dock.Left);
            Footer.Children.Add(quit);
            OkBtn.Content = "Enregistrer";
            CancelBtn.Content = "Fermer";
            Closed += delegate { c.SettingsClosed(); };

            MakeScrollable();
        }

        void Section(string title)
        {
            Panel.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 13, Margin = new Thickness(0, 12, 0, 4) });
            Panel.Children.Add(new System.Windows.Shapes.Rectangle { Height = 1, Fill = Theme.Res<Brush>("SLine"), Margin = new Thickness(0, 0, 0, 2) });
        }

        TextBlock Lbl(string text)
        {
            var tb = new TextBlock { Text = text, Foreground = Theme.Res<Brush>("SMuted"), FontSize = 12, Width = 200, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            DockPanel.SetDock(tb, Dock.Left);
            return tb;
        }

        CheckBox Opt(Panel parent, string text, bool value)
        {
            var cb = new CheckBox { Style = Theme.Res<Style>("SCheck"), Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }, IsChecked = value, Margin = new Thickness(0, 6, 8, 2), FontSize = 12.5 };
            parent.Children.Add(cb);
            return cb;
        }

        void RefreshProfiles()
        {
            profileList.Children.Clear();
            string current = Controller.ProfileSignature();
            if (c.L.Profiles.Count == 0) profileList.Children.Add(new TextBlock { Text = "Aucune disposition mémorisée.", Foreground = Theme.Res<Brush>("SMuted"), FontSize = 12, Margin = new Thickness(0, 6, 0, 0) });
            foreach (var p0 in c.L.Profiles.OrderByDescending(p => p.Auto).ThenBy(p => p.Name))
            {
                var p = p0;
                var row = new DockPanel { Margin = new Thickness(0, 5, 0, 0) };
                var del = new Button { Style = Theme.Res<Style>("SBtn"), Content = "Supprimer", Padding = new Thickness(10, 3, 10, 4), Margin = new Thickness(6, 0, 0, 0) };
                del.Click += delegate { if (Dialogs.Confirm(this, "Supprimer la disposition", "« " + p.Name + " » sera oubliée.", "Supprimer", true)) { c.DeleteProfile(p); RefreshProfiles(); } };
                DockPanel.SetDock(del, Dock.Right);
                var restore = new Button { Style = Theme.Res<Style>("SBtnAccent"), Content = "Restaurer", Padding = new Thickness(10, 3, 10, 4), Margin = new Thickness(6, 0, 0, 0) };
                restore.Click += delegate { c.ApplyProfile(p); SyncLive(); };
                DockPanel.SetDock(restore, Dock.Right);
                DateTime d;
                string when = DateTime.TryParse(p.Updated, null, System.Globalization.DateTimeStyles.RoundtripKind, out d) ? d.ToString("dd/MM HH:mm") : "";
                string kind = p.Auto ? "automatique" : "instantané";
                if (p.Signature == current) kind += " · écrans actuels";
                var desc = new TextBlock { Text = p.Screens + "  ·  " + kind + (when.Length > 0 ? "  ·  " + when : ""), Foreground = Theme.Res<Brush>("SMuted"), FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(8, 0, 0, 0), ToolTip = p.Screens };
                DockPanel.SetDock(desc, Dock.Right);
                var name = new TextBox { Style = Theme.Res<Style>("SFieldBox"), Text = p.Name, Width = 170, Padding = new Thickness(8, 3, 8, 4), FontWeight = p.Signature == current ? FontWeights.SemiBold : FontWeights.Normal };
                name.LostFocus += delegate { c.RenameProfile(p, name.Text); };
                row.Children.Add(del); row.Children.Add(restore); row.Children.Add(desc); row.Children.Add(name);
                profileList.Children.Add(row);
            }
        }

        /// <summary>Une ligne par fenêtre (cadres puis panneaux) : nom, position et taille, deux actions de secours.</summary>
        void RefreshWindows()
        {
            windowList.Children.Clear();
            foreach (var w0 in c.DesktopWindows)
            {
                var w = w0;
                var row = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
                var reset = Theme.IconButton("IcoRefresh", "Taille par défaut", 14);
                reset.Margin = new Thickness(4, 0, 0, 0);
                reset.Click += delegate { w.ResetSize(); RefreshWindows(); };
                var fit = Theme.IconButton("IcoInbox", "Ramener à l'écran", 14);
                fit.Click += delegate { w.FitOnScreen(); RefreshWindows(); };
                DockPanel.SetDock(reset, Dock.Right); DockPanel.SetDock(fit, Dock.Right);
                var cadre = w as CadreWindow;
                var dot = new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(5), Background = cadre != null ? Theme.Brush(0xFF, Palette.Tint(cadre.Info.Color, cadre.Info.CustomColor)) : Theme.Res<Brush>("Accent"), BorderBrush = Theme.Argb(0x66FFFFFF), BorderThickness = new Thickness(1), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
                DockPanel.SetDock(dot, Dock.Left);
                var nameTb = new TextBlock { Text = w.PanelTitle, VerticalAlignment = VerticalAlignment.Center, Width = 150, TextTrimming = TextTrimming.CharacterEllipsis };
                DockPanel.SetDock(nameTb, Dock.Left);
                string where = (int)Math.Round(w.Left) + ", " + (int)Math.Round(w.Top) + "  ·  " + (int)Math.Round(w.Width) + " × " + (int)Math.Round(w.IsRolled ? w.RestoreHeight : w.Height) + (w.IsRolled ? "  ·  enroulé" : "") + (w.IsVisible ? "" : "  ·  masqué");
                var info = new TextBlock { Text = where, Foreground = Theme.Res<Brush>("SMuted"), FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = "Position (x, y) et taille en unités d'affichage" };
                row.Children.Add(reset); row.Children.Add(fit); row.Children.Add(dot); row.Children.Add(nameTb); row.Children.Add(info);
                windowList.Children.Add(row);
            }
        }

        void RefreshCadres()
        {
            cadreList.Children.Clear();
            foreach (var w in c.Cadres)
            {
                var cw = w;
                var row = new DockPanel { Margin = new Thickness(0, 5, 0, 0) };
                var edit = new Button { Style = Theme.Res<Style>("SBtn"), Content = "Modifier…", Padding = new Thickness(10, 3, 10, 4), Margin = new Thickness(6, 0, 0, 0) };
                edit.Click += delegate { c.EditCadre(cw); RefreshCadres(); };
                DockPanel.SetDock(edit, Dock.Right);
                var dot = new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(5), Background = Theme.Brush(0xFF, Palette.Tint(cw.Info.Color, cw.Info.CustomColor)), BorderBrush = Theme.Argb(0x66FFFFFF), BorderThickness = new Thickness(1), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 8, 0) };
                DockPanel.SetDock(dot, Dock.Left);
                var nameTb = new TextBlock { Text = cw.Info.Name, VerticalAlignment = VerticalAlignment.Center, Width = 150, TextTrimming = TextTrimming.CharacterEllipsis };
                DockPanel.SetDock(nameTb, Dock.Left);
                var path = new TextBlock { Text = cw.Folder, Foreground = Theme.Res<Brush>("SMuted"), FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = cw.Folder };
                row.Children.Add(edit); row.Children.Add(dot); row.Children.Add(nameTb); row.Children.Add(path);
                cadreList.Children.Add(row);
            }
        }

        static void Copy(Settings from, Settings to)
        {
            to.StartWithWindows = from.StartWithWindows; to.LockLayout = from.LockLayout; to.Snap = from.Snap; to.HideDesktopIcons = from.HideDesktopIcons;
            to.ShowExtensions = from.ShowExtensions; to.IconSize = from.IconSize; to.QuickMods = from.QuickMods; to.QuickVk = from.QuickVk;
            to.ToggleMods = from.ToggleMods; to.ToggleVk = from.ToggleVk; to.DailyReminder = from.DailyReminder; to.AutoSort = from.AutoSort; to.TasksVisible = from.TasksVisible;
            to.NotesVisible = from.NotesVisible; to.MindVisible = from.MindVisible; to.MindStyleOpen = from.MindStyleOpen; to.MindTab = from.MindTab; to.Welcomed = from.Welcomed; to.Services = from.Services; to.NoAutoDock = from.NoAutoDock;
        }

        protected override bool Validate()
        {
            // seuls les réglages de cette fenêtre sont écrits : ce qui a changé entre-temps ailleurs (volet Style des cartes,
            // zone de notification, bouton « Restaurer » d'une disposition) est conservé
            var s = c.S;
            s.StartWithWindows = cbStart.IsChecked == true; s.Snap = cbSnap.IsChecked == true;
            s.HideDesktopIcons = cbHide.IsChecked == true; s.ShowExtensions = cbExt.IsChecked == true; s.AutoSort = cbAuto.IsChecked == true; s.LockLayout = cbLock.IsChecked == true;
            s.TasksVisible = cbTasks.IsChecked == true; s.NotesVisible = cbNotes.IsChecked == true; s.MindVisible = cbMind.IsChecked == true; s.DailyReminder = cbRemind.IsChecked == true;
            s.NoAutoDock = cbDock.IsChecked != true;
            for (int i = 0; i < iconSize.Length; i++) if (iconSize[i].IsChecked == true) s.IconSize = i;
            if (!c.ApplySettings()) c.Toast("Un raccourci clavier est indisponible (déjà utilisé par une autre application) ; les autres réglages sont enregistrés.");
            startupNote.Text = Startup.Describe();
            return true;
        }

        /// <summary>Cases qui reflètent un état modifiable ailleurs (zone de notification, « Restaurer ») : relues depuis les réglages.</summary>
        void SyncLive()
        {
            cbTasks.IsChecked = c.S.TasksVisible; cbNotes.IsChecked = c.S.NotesVisible; cbMind.IsChecked = c.S.MindVisible; cbLock.IsChecked = c.S.LockLayout;
        }
    }
}
