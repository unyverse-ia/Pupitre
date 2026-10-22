using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using WF = System.Windows.Forms;

namespace Pupitre
{
    /// <summary>Fenêtre invisible qui reçoit les raccourcis globaux et les changements d'affichage.</summary>
    class MessageWindow : Window
    {
        readonly Controller c;
        public IntPtr Hwnd;
        public MessageWindow(Controller ctl)
        {
            c = ctl;
            WindowStyle = WindowStyle.None; ShowInTaskbar = false; Width = 1; Height = 1; Left = -10000; Top = -10000; ShowActivated = false;
            new WindowInteropHelper(this).EnsureHandle();
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(Hwnd).AddHook(delegate(IntPtr h, int msg, IntPtr w, IntPtr l, ref bool handled)
            {
                if (msg == Native.WM_HOTKEY) { c.OnHotkey((int)(w.ToInt64() & 0xFFFF)); handled = true; }
                else if (msg == Native.WM_DISPLAYCHANGE || msg == Native.WM_SETTINGCHANGE) c.OnDisplayChange();
                else if (msg == Native.WM_QUERYENDSESSION || msg == Native.WM_ENDSESSION) c.FlushAll(true);
                return IntPtr.Zero;
            });
        }
    }

    /// <summary>Chef d'orchestre : réglages, disposition multi-écrans, cadres, notes, tâches, raccourcis, zone de notification.</summary>
    public class Controller : IDisposable
    {
        public Settings S;
        public Layout L;
        public TasksVm Tasks;
        public readonly List<CadreWindow> Cadres = new List<CadreWindow>();
        public TasksWindow TasksWin;
        public NotesWindow NotesWin;
        public MindWindow MindWin;
        public IntPtr DesktopHost;
        public bool AllHidden;
        public bool Exiting;
        readonly Application app;
        TrayIcon tray;
        MessageWindow msgWin;
        DispatcherTimer watchdog, clock;
        FileSystemWatcher desktopWatcher;
        Debouncer layoutDeb, displayDeb;
        SettingsWindow settingsWin;
        QuickAddWindow quick;
        string reminderDay, screenSignature, handledSignature;
        bool applyingProfile;
        bool weHidIcons;
        readonly Dictionary<string, DateTime> pendingSort = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        DispatcherTimer sortTimer;

        public string SettingsPath { get { return Path.Combine(App.DataDir, "settings.json"); } }
        public string LayoutPath { get { return Path.Combine(App.DataDir, "layout.json"); } }
        public string TasksPath { get { return Path.Combine(App.DataDir, "tasks.json"); } }
        public string ArchivePath { get { return Path.Combine(App.DataDir, "archive.json"); } }
        public string MindPath { get { return Path.Combine(App.DataDir, "cartes.json"); } }

        public IEnumerable<DesktopWindow> DesktopWindows
        {
            get
            {
                foreach (var w in Cadres) yield return w;
                if (NotesWin != null) yield return NotesWin;
                if (MindWin != null) yield return MindWin;
                if (TasksWin != null) yield return TasksWin;
            }
        }

        public Controller(Application application) { app = application; }

        System.Threading.EventWaitHandle settingsSignal;

        /// <summary>Signal « ouvrir les paramètres » envoyé par une seconde instance lancée avec --parametres.</summary>
        void ListenSettingsSignal()
        {
            try { settingsSignal = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, "Local\\Pupitre.OpenSettings"); }
            catch (Exception ex) { Log.Error(ex, "signal paramètres"); return; }
            var t = new System.Threading.Thread(delegate()
            {
                while (!Exiting)
                {
                    try { if (settingsSignal.WaitOne(500)) Application.Current.Dispatcher.BeginInvoke(new Action(OpenSettings)); }
                    catch { return; }
                }
            });
            t.IsBackground = true; t.Name = "Pupitre.SettingsSignal"; t.Start();
        }

        public void Start()
        {
            ListenSettingsSignal();
            S = Json.Load<Settings>(SettingsPath);
            L = Json.Load<Layout>(LayoutPath);
            L.Version = 3;
            if (L.NotesPanel == null) L.NotesPanel = new PanelInfo { Color = 7 };
            if (L.MindPanel == null) L.MindPanel = new PanelInfo { Color = 5 };
            var store = Json.Load<TaskStore>(TasksPath);
            S.StartWithWindows = Startup.IsEnabled();
            Tasks = new TasksVm(this, store);
            Tasks.CountsChanged += OnCounts;
            layoutDeb = new Debouncer(600, delegate { UpdateAutoProfile(); Json.Save(LayoutPath, L); });
            displayDeb = new Debouncer(1200, ReplaceAll);

            DesktopHost = Native.FindDesktopHost();
            msgWin = new MessageWindow(this);
            RegisterHotkeys();
            screenSignature = ScreenSignature();

            if (L.Cadres.Count == 0) CreateDefaultLayout();
            foreach (var info in L.Cadres.ToList()) CreateCadreWindow(info);
            TasksWin = new TasksWindow(this);
            if (S.TasksVisible) ShowTasksWindow();
            NotesWin = new NotesWindow(this);
            if (S.NotesVisible) ShowNotesWindow();
            MindWin = new MindWindow(this);
            if (S.MindVisible) ShowMindWindow();
            InitProfiles();

            tray = new TrayIcon(this);
            watchdog = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            watchdog.Tick += delegate { CheckDesktopHost(); CheckScreens(); };
            watchdog.Start();
            clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            clock.Tick += delegate { Tick(); };
            clock.Start();
            sortTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
            sortTimer.Tick += delegate { ProcessPendingSort(); };
            SetupDesktopWatcher();
            OnCounts();
            app.SessionEnding += delegate { FlushAll(true); };

            if (!S.Welcomed)
            {
                var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
                t.Tick += delegate { t.Stop(); ShowWelcome(); };
                t.Start();
            }
            else
            {
                if (S.HideDesktopIcons) { DesktopIcons.SetHidden(true); weHidIcons = true; }
                var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
                t.Tick += delegate { t.Stop(); DailyReminder(); };
                t.Start();
            }
        }

        void ShowWelcome()
        {
            var w = new WelcomeDialog(this);
            bool ok = w.ShowDialog() == true;
            S.Welcomed = true;
            S.HideDesktopIcons = ok && w.HideIcons.IsChecked == true;
            if (S.HideDesktopIcons) { DesktopIcons.SetHidden(true); weHidIcons = true; }
            SaveSettings();
        }

        // ---- Écrans : placement mémorisé par écran, repli quand un écran manque -----------------------------

        public static double Scale()
        {
            try { using (var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero)) return 96.0 / g.DpiX; } catch { return 1.0; }
        }

        static Rect Dip(System.Drawing.Rectangle r, double scale)
        {
            return new Rect(r.Left * scale, r.Top * scale, r.Width * scale, r.Height * scale);
        }

        static Rect Clamp(Rect r, Rect wa, double minW, double minH)
        {
            double w = Math.Max(minW, Math.Min(r.Width, wa.Width)), h = Math.Max(minH, Math.Min(r.Height, wa.Height));
            double x = Math.Max(wa.Left, Math.Min(r.X, wa.Right - w)), y = Math.Max(wa.Top, Math.Min(r.Y, wa.Bottom - h));
            return new Rect(x, y, w, h);
        }

        /// <summary>
        /// Position d'un panneau : sur son écran d'origine (position relative à l'espace de travail) s'il est présent ;
        /// sinon à sa position absolue si elle tombe sur un écran existant ; sinon sur l'écran principal, même position relative.
        /// Les valeurs mémorisées ne sont pas modifiées : quand l'écran revient, le panneau y retourne.
        /// </summary>
        public Rect Resolve(string screen, double relX, double relY, double x, double y, double w, double h)
        {
            double scale = Scale();
            if (w < 120) w = 360;
            if (h < 60) h = 260;
            var screens = WF.Screen.AllScreens;
            if (!string.IsNullOrEmpty(screen))
            {
                foreach (var sc in screens)
                    if (sc.DeviceName == screen)
                    {
                        var wa = Dip(sc.WorkingArea, scale);
                        return Clamp(new Rect(wa.Left + relX, wa.Top + relY, w, h), wa, 120, 60);
                    }
            }
            var abs = new Rect(x, y, w, h);
            foreach (var sc in screens)
            {
                var wa = Dip(sc.WorkingArea, scale);
                var inter = Rect.Intersect(abs, wa);
                if (inter != Rect.Empty && inter.Width >= 80 && inter.Height >= 40) return Clamp(abs, wa, 120, 60);
            }
            var prim = Dip(WF.Screen.PrimaryScreen.WorkingArea, scale);
            if (string.IsNullOrEmpty(screen)) return Clamp(abs, prim, 120, 60);
            return Clamp(new Rect(prim.Left + relX, prim.Top + relY, w, h), prim, 120, 60);
        }

        Rect ResolveCadre(CadreInfo i) { return Resolve(i.Screen, i.RelX, i.RelY, i.X, i.Y, i.W, i.H); }
        Rect ResolvePanel(PanelInfo p) { return Resolve(p.Screen, p.RelX, p.RelY, p.X, p.Y, p.W, p.H); }

        static string ScreenSignature()
        {
            var sb = new StringBuilder();
            foreach (var s in WF.Screen.AllScreens) sb.Append(s.DeviceName).Append(':').Append(s.Bounds).Append(':').Append(s.WorkingArea).Append(';');
            return sb.ToString();
        }

        void CheckScreens()
        {
            if (Exiting) return;
            string sig = ScreenSignature();
            if (sig != screenSignature) { screenSignature = sig; displayDeb.Trigger(); }
        }

        public void OnDisplayChange()
        {
            if (Exiting) return;
            displayDeb.Trigger();
        }

        /// <summary>
        /// Après un changement d'écrans : si une disposition automatique existe pour cette configuration, elle est appliquée ;
        /// sinon chaque panneau retrouve sa place (ou se replie sur un écran présent) et la configuration est mémorisée.
        /// </summary>
        void ReplaceAll()
        {
            if (Exiting) return;
            screenSignature = ScreenSignature();
            string sig = ProfileSignature();
            DesktopHost = Native.FindDesktopHost();
            var profile = L.Profiles.FirstOrDefault(p => p.Auto && p.Signature == sig);
            if (profile != null)
            {
                ApplyProfile(profile);
                handledSignature = sig;
                Toast("Écrans : « " + profile.Name + " » — disposition appliquée");
            }
            else
            {
                foreach (var w in Cadres) if (w.IsVisible) w.PlaceSilently(ResolveCadre(w.Info));
                if (TasksWin != null && TasksWin.IsVisible) TasksWin.PlaceSilently(ResolvePanel(L.Tasks));
                if (NotesWin != null && NotesWin.IsVisible) NotesWin.PlaceSilently(ResolvePanel(L.NotesPanel));
                if (MindWin != null && MindWin.IsVisible) MindWin.PlaceSilently(ResolvePanel(L.MindPanel));
                handledSignature = sig;
                var created = CaptureProfile(ScreensDescription(), true);
                Toast("Nouvelle configuration d'écrans « " + created.Name + " » : sa disposition sera mémorisée");
            }
            foreach (var w in DesktopWindows) if (w.IsVisible) w.AttachToDesktop();
        }

        // ---- Profils de disposition (une disposition automatique par configuration d'écrans + instantanés nommés) ----

        /// <summary>Empreinte de la configuration d'écrans : noms et géométrie, triés (l'espace de travail, qui varie avec la barre des tâches, est ignoré).</summary>
        public static string ProfileSignature()
        {
            var parts = WF.Screen.AllScreens.Select(s => s.DeviceName + "=" + s.Bounds.X + "," + s.Bounds.Y + "," + s.Bounds.Width + "x" + s.Bounds.Height + (s.Primary ? "*" : "")).OrderBy(x => x).ToArray();
            return string.Join(";", parts);
        }

        public static string ScreensDescription()
        {
            var screens = WF.Screen.AllScreens.OrderByDescending(s => s.Primary).ThenBy(s => s.Bounds.X).ToList();
            var parts = screens.Select(s => s.Bounds.Width + "×" + s.Bounds.Height + (s.Primary ? " (principal)" : "")).ToArray();
            return screens.Count + (screens.Count > 1 ? " écrans · " : " écran · ") + string.Join(", ", parts);
        }

        static PanelGeom Geom(string id, double x, double y, double w, double h, bool rolled, bool visible, string screen, double rx, double ry)
        {
            return new PanelGeom { Id = id, X = x, Y = y, W = w, H = h, Rolled = rolled, Visible = visible, Screen = screen, RelX = rx, RelY = ry };
        }

        void FillProfile(LayoutProfile p)
        {
            p.Cadres = L.Cadres.Select(ci => Geom(ci.Id, ci.X, ci.Y, ci.W, ci.H, ci.Rolled, ci.Visible, ci.Screen, ci.RelX, ci.RelY)).ToList();
            var t = L.Tasks; p.Tasks = Geom("tasks", t.X, t.Y, t.W, t.H, t.Rolled, S.TasksVisible, t.Screen, t.RelX, t.RelY);
            var n = L.NotesPanel; p.Notes = Geom("notes", n.X, n.Y, n.W, n.H, n.Rolled, S.NotesVisible, n.Screen, n.RelX, n.RelY);
            var m = L.MindPanel; p.Mind = Geom("mind", m.X, m.Y, m.W, m.H, m.Rolled, S.MindVisible, m.Screen, m.RelX, m.RelY);
            p.TasksVisible = S.TasksVisible; p.NotesVisible = S.NotesVisible; p.MindVisible = S.MindVisible;
            p.Updated = DateTime.Now.ToString("s");
        }

        public LayoutProfile CaptureProfile(string name, bool auto)
        {
            var p = new LayoutProfile { Id = Guid.NewGuid().ToString("N"), Name = name, Auto = auto, Signature = ProfileSignature(), Screens = ScreensDescription() };
            FillProfile(p);
            L.Profiles.Add(p);
            SaveLayoutSoon();
            return p;
        }

        /// <summary>Instantané nommé de la disposition actuelle (restaurable à tout moment).</summary>
        public LayoutProfile SaveSnapshot(string name) { return CaptureProfile(name, false); }

        public void RenameProfile(LayoutProfile p, string name) { if (!string.IsNullOrWhiteSpace(name)) { p.Name = name.Trim(); SaveLayoutSoon(); } }
        public void DeleteProfile(LayoutProfile p) { L.Profiles.Remove(p); SaveLayoutSoon(); }

        /// <summary>La disposition automatique de la configuration courante suit les déplacements, tant que les écrans n'ont pas changé.</summary>
        void UpdateAutoProfile()
        {
            if (applyingProfile || Exiting) return;
            string sig = ProfileSignature();
            if (sig != handledSignature) return;     // écrans en cours de changement : on ne touche à rien
            var p = L.Profiles.FirstOrDefault(x => x.Auto && x.Signature == sig);
            if (p == null) p = CaptureProfileSilently(sig);
            FillProfile(p);
        }

        LayoutProfile CaptureProfileSilently(string sig)
        {
            var p = new LayoutProfile { Id = Guid.NewGuid().ToString("N"), Name = ScreensDescription(), Auto = true, Signature = sig, Screens = ScreensDescription() };
            L.Profiles.Add(p);
            return p;
        }

        void InitProfiles()
        {
            string sig = ProfileSignature();
            var p = L.Profiles.FirstOrDefault(x => x.Auto && x.Signature == sig);
            if (p != null) ApplyProfile(p);
            handledSignature = sig;
            if (p == null) { var created = CaptureProfileSilently(sig); FillProfile(created); SaveLayoutSoon(); }
        }

        /// <summary>Applique une disposition : géométrie, enroulement et visibilité de chaque panneau (ramenés dans les écrans présents).</summary>
        public void ApplyProfile(LayoutProfile p)
        {
            applyingProfile = true;
            try
            {
                foreach (var g in p.Cadres)
                {
                    var ci = L.Cadres.FirstOrDefault(x => x.Id == g.Id);
                    if (ci == null) continue;
                    ci.X = g.X; ci.Y = g.Y; ci.W = g.W; ci.H = g.H; ci.Rolled = g.Rolled; ci.RolledH = g.H; ci.Visible = g.Visible; ci.Screen = g.Screen; ci.RelX = g.RelX; ci.RelY = g.RelY;
                    var w = Cadres.FirstOrDefault(x => x.Info == ci);
                    if (w == null) continue;
                    if (ci.Visible && !AllHidden)
                    {
                        if (!w.IsVisible) w.ShowAt(ResolveCadre(ci)); else { w.SetRolled(false, false); w.PlaceSilently(ResolveCadre(ci)); }
                        if (ci.Rolled) w.SetRolled(true, false);
                    }
                    else if (!ci.Visible && w.IsVisible) w.Hide();
                }
                ApplyPanelGeom(p.Tasks, L.Tasks);
                ApplyPanelGeom(p.Notes, L.NotesPanel);
                if (p.Mind != null) { ApplyPanelGeom(p.Mind, L.MindPanel); S.MindVisible = p.MindVisible; }
                S.TasksVisible = p.TasksVisible; S.NotesVisible = p.NotesVisible;
                SaveSettings();
                if (TasksWin != null)
                {
                    if (S.TasksVisible && !AllHidden) { if (!TasksWin.IsVisible) ShowTasksWindow(); else { TasksWin.SetRolled(false, false); TasksWin.PlaceSilently(ResolvePanel(L.Tasks)); if (L.Tasks.Rolled) TasksWin.SetRolled(true, false); } }
                    else if (TasksWin.IsVisible) TasksWin.Hide();
                }
                if (NotesWin != null)
                {
                    if (S.NotesVisible && !AllHidden) { if (!NotesWin.IsVisible) ShowNotesWindow(); else { NotesWin.SetRolled(false, false); NotesWin.PlaceSilently(ResolvePanel(L.NotesPanel)); if (L.NotesPanel.Rolled) NotesWin.SetRolled(true, false); } }
                    else if (NotesWin.IsVisible) NotesWin.Hide();
                }
                if (MindWin != null)
                {
                    if (S.MindVisible && !AllHidden) { if (!MindWin.IsVisible) ShowMindWindow(); else { MindWin.SetRolled(false, false); MindWin.PlaceSilently(ResolvePanel(L.MindPanel)); if (L.MindPanel.Rolled) MindWin.SetRolled(true, false); } }
                    else if (MindWin.IsVisible) MindWin.Hide();
                }
                foreach (var w in DesktopWindows) if (w.IsVisible) w.AttachToDesktop();
            }
            catch (Exception ex) { Log.Error(ex, "application d'une disposition"); }
            finally { applyingProfile = false; }
            SaveLayoutSoon();
        }

        static void ApplyPanelGeom(PanelGeom g, PanelInfo t)
        {
            if (g == null) return;
            t.X = g.X; t.Y = g.Y; t.W = g.W; t.H = g.H; t.Rolled = g.Rolled; t.RolledH = g.H; t.Screen = g.Screen; t.RelX = g.RelX; t.RelY = g.RelY; t.Set = true;
        }

        // ---- Disposition ------------------------------------------------------------------------------

        void CreateDefaultLayout()
        {
            var wa = WF.Screen.PrimaryScreen.WorkingArea;
            double scale = Scale();
            double W = wa.Width * scale, H = wa.Height * scale, X = wa.Left * scale, Y = wa.Top * scale;
            double cadreW = Math.Min(460, W * 0.3), cadreH = Math.Min(330, (H - 72) / 2);
            L.Cadres.Add(new CadreInfo { Id = "desktop", Name = "Bureau", IsDesktop = true, Color = 0, X = X + 24, Y = Y + 24, W = cadreW, H = cadreH, Folder = App.Desktop });
            string folder = Path.Combine(App.Desktop, "Raccourcis");
            try { Directory.CreateDirectory(folder); File.SetAttributes(folder, File.GetAttributes(folder) | FileAttributes.Hidden); } catch { }
            L.Cadres.Add(new CadreInfo { Id = Guid.NewGuid().ToString("N"), Name = "Raccourcis", Folder = folder, Color = 1, X = X + 24, Y = Y + 24 + cadreH + 16, W = cadreW, H = Math.Max(200, H - cadreH - 72), HideFolder = true });
            double tw = Math.Min(420, W * 0.28);
            L.Tasks = new PanelInfo { X = X + W - tw - 24, Y = Y + 24, W = tw, H = Math.Min(520, H * 0.55), Set = true, Color = 1, Opacity = -1 };
            L.NotesPanel = new PanelInfo { X = X + W - tw - 24, Y = Y + 24 + Math.Min(520, H * 0.55) + 16, W = tw, H = Math.Max(200, H - Math.Min(520, H * 0.55) - 72), Set = true, Color = 7, Opacity = -1 };
            layoutDeb.Trigger();
        }

        CadreWindow CreateCadreWindow(CadreInfo info)
        {
            var w = new CadreWindow(this, info);
            Cadres.Add(w);
            if (info.Visible && !AllHidden)
            {
                w.ShowAt(ResolveCadre(info));
                if (info.Rolled) w.SetRolled(true, false);
            }
            return w;
        }

        void ShowTasksWindow()
        {
            var t = L.Tasks;
            if (!t.Set)
            {
                var wa = Dip(WF.Screen.PrimaryScreen.WorkingArea, Scale());
                t.X = wa.Right - 440; t.Y = wa.Top + 24; t.W = 416; t.H = Math.Min(520, wa.Height - 48); t.Set = true;
            }
            TasksWin.ShowAt(ResolvePanel(t));
            if (t.Rolled) TasksWin.SetRolled(true, false);
        }

        void ShowMindWindow()
        {
            var t = L.MindPanel;
            if (!t.Set)
            {
                var wa = Dip(WF.Screen.PrimaryScreen.WorkingArea, Scale());
                t.X = wa.Left + wa.Width / 2 - 260; t.Y = wa.Top + 24; t.W = 520; t.H = Math.Min(420, wa.Height - 48); t.Set = true;
            }
            MindWin.ShowAt(ResolvePanel(t));
            if (t.Rolled) MindWin.SetRolled(true, false);
        }

        /// <summary>Nouvelle carte mentale (le panneau est affiché si besoin).</summary>
        public void NewMindMap(Window owner)
        {
            if (MindWin == null) return;
            if (!S.MindVisible) SetMindVisible(true);
            if (AllHidden) ToggleAll();
            MindWin.NewMapInteractive();
        }

        public void SetMindVisible(bool visible)
        {
            S.MindVisible = visible;
            SaveSettings();
            if (MindWin == null) return;
            if (visible && !AllHidden) { if (!MindWin.IsVisible) ShowMindWindow(); }
            else MindWin.Hide();
        }

        void ShowNotesWindow()
        {
            var t = L.NotesPanel;
            if (!t.Set)
            {
                var wa = Dip(WF.Screen.PrimaryScreen.WorkingArea, Scale());
                t.X = wa.Right - 440; t.Y = wa.Top + 560; t.W = 416; t.H = Math.Max(200, wa.Bottom - t.Y - 24); t.Set = true;
            }
            NotesWin.ShowAt(ResolvePanel(t));
            if (t.Rolled) NotesWin.SetRolled(true, false);
        }

        public void SaveLayoutSoon() { if (!Exiting) layoutDeb.Trigger(); }
        public void SaveSettings() { Json.Save(SettingsPath, S); }

        void PlaceNearCursor(out double x, out double y, double w, double h)
        {
            Native.POINT p; Native.GetCursorPos(out p);
            var wa = Dip(WF.Screen.FromPoint(new System.Drawing.Point(p.X, p.Y)).WorkingArea, Scale());
            double scale = Scale();
            x = Math.Max(wa.Left, Math.Min(p.X * scale - 60, wa.Right - w - 10));
            y = Math.Max(wa.Top, Math.Min(p.Y * scale - 20, wa.Bottom - h - 10));
        }

        public void NewCadre(Window owner)
        {
            var dlg = new NewCadreDialog(owner, this);
            if (dlg.ShowDialog() != true || dlg.Result == null) return;
            var info = dlg.Result;
            info.W = 360; info.H = 260;
            double x, y; PlaceNearCursor(out x, out y, info.W, info.H);
            info.X = x; info.Y = y;
            L.Cadres.Add(info);
            CreateCadreWindow(info);
            SaveLayoutSoon();
        }

        /// <summary>Nouvelle note dans la bibliothèque (le panneau est affiché si besoin).</summary>
        public void NewNote(Window owner)
        {
            if (NotesWin == null) return;
            if (!S.NotesVisible) SetNotesVisible(true);
            if (AllHidden) ToggleAll();
            NotesWin.AddNote();
        }

        public void EditCadre(CadreWindow w)
        {
            var dlg = new CadreSettingsDialog(w, this);
            bool ok = dlg.ShowDialog() == true;
            if (dlg.Deleted) { DeleteCadre(w); return; }
            if (!ok) return;
            w.ApplyInfo();
            w.ApplyLock();
            w.SetupWatchers();
            SaveLayoutSoon();
        }

        /// <summary>Paramètres d'un panneau Tâches ou Notes (apparence, verrou).</summary>
        public void EditPanel(DesktopWindow w, PanelInfo p, string what)
        {
            var dlg = new PanelSettingsDialog(w, this, p, what);
            if (dlg.ShowDialog() != true) return;
            w.SetLook(p.Color, p.Opacity, p.CustomColor);
            w.ApplyLock();
            SaveLayoutSoon();
        }

        public void DeleteCadre(CadreWindow w)
        {
            if (w.Info.IsDesktop) return;
            w.IntentionalClose = true;
            Cadres.Remove(w);
            L.Cadres.Remove(w.Info);
            try { w.Close(); } catch { }
            SaveLayoutSoon();
        }

        public void OnWindowLost(DesktopWindow w)
        {
            if (Exiting) return;
            Log.Error(new Exception("fenêtre perdue : " + w.Title), "watchdog");
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
            t.Tick += delegate
            {
                t.Stop();
                DesktopHost = Native.FindDesktopHost();
                var cw = w as CadreWindow;
                if (cw != null) { Cadres.Remove(cw); if (L.Cadres.Contains(cw.Info)) CreateCadreWindow(cw.Info); }
                else if (w == TasksWin) { TasksWin = new TasksWindow(this); if (S.TasksVisible && !AllHidden) ShowTasksWindow(); }
                else if (w == NotesWin) { NotesWin = new NotesWindow(this); if (S.NotesVisible && !AllHidden) ShowNotesWindow(); }
                else if (w == MindWin) { MindWin = new MindWindow(this); if (S.MindVisible && !AllHidden) ShowMindWindow(); }
            };
            t.Start();
        }

        void CheckDesktopHost()
        {
            if (Exiting) return;
            IntPtr h = Native.FindDesktopHost();
            if (h == IntPtr.Zero) return;
            if (h != DesktopHost || !Native.IsWindow(DesktopHost))
            {
                DesktopHost = h;
                foreach (var w in DesktopWindows) w.AttachToDesktop();
                if (S.HideDesktopIcons && !DesktopIcons.AreHidden()) { DesktopIcons.SetHidden(true); weHidIcons = true; }
            }
        }

        // ---- Visibilité, verrouillage, réglages --------------------------------------------------------

        /// <summary>Tous les panneaux visibles ramenés entièrement dans l'écran (zone de notification).</summary>
        public void FitAllOnScreen() { foreach (var w in DesktopWindows) if (w.IsVisible) w.FitOnScreen(); }

        public void ToggleAll()
        {
            AllHidden = !AllHidden;
            foreach (var w in Cadres) { if (AllHidden) w.Hide(); else if (w.Info.Visible) w.Show(); }
            if (TasksWin != null) { if (AllHidden) TasksWin.Hide(); else if (S.TasksVisible) TasksWin.Show(); }
            if (NotesWin != null) { if (AllHidden) NotesWin.Hide(); else if (S.NotesVisible) NotesWin.Show(); }
            if (MindWin != null) { if (AllHidden) MindWin.Hide(); else if (S.MindVisible) MindWin.Show(); }
            foreach (var w in DesktopWindows) if (w.IsVisible) w.AttachToDesktop();
        }

        public void SetTasksVisible(bool visible)
        {
            S.TasksVisible = visible;
            SaveSettings();
            if (TasksWin == null) return;
            if (visible && !AllHidden) { if (!TasksWin.IsVisible) ShowTasksWindow(); }
            else TasksWin.Hide();
        }

        public void SetNotesVisible(bool visible)
        {
            S.NotesVisible = visible;
            SaveSettings();
            if (NotesWin == null) return;
            if (visible && !AllHidden) { if (!NotesWin.IsVisible) ShowNotesWindow(); }
            else NotesWin.Hide();
        }

        public void SetLocked(bool locked)
        {
            S.LockLayout = locked;
            SaveSettings();
            foreach (var w in DesktopWindows) w.ApplyLock();
        }

        public void OpenSettings()
        {
            if (settingsWin != null) { settingsWin.Activate(); return; }
            settingsWin = new SettingsWindow(this);
            settingsWin.Show();
        }
        public void SettingsClosed() { settingsWin = null; }

        public bool ApplySettings()
        {
            bool ok = RegisterHotkeys();
            if (Startup.IsEnabled() != S.StartWithWindows) Startup.Set(S.StartWithWindows);
            if (S.HideDesktopIcons) { DesktopIcons.SetHidden(true); weHidIcons = true; }
            else if (weHidIcons) { DesktopIcons.SetHidden(false); weHidIcons = false; }
            foreach (var w in DesktopWindows) { w.ApplyAppearance(); w.ApplyLock(); }
            foreach (var w in Cadres) w.ApplyInfo();
            if (TasksWin != null)
            {
                if (S.TasksVisible && !AllHidden && !TasksWin.IsVisible) ShowTasksWindow();
                else if (!S.TasksVisible && TasksWin.IsVisible) TasksWin.Hide();
            }
            if (NotesWin != null)
            {
                if (S.NotesVisible && !AllHidden && !NotesWin.IsVisible) ShowNotesWindow();
                else if (!S.NotesVisible && NotesWin.IsVisible) NotesWin.Hide();
            }
            if (MindWin != null)
            {
                if (S.MindVisible && !AllHidden && !MindWin.IsVisible) ShowMindWindow();
                else if (!S.MindVisible && MindWin.IsVisible) MindWin.Hide();
            }
            Tasks.Rebuild();
            SetupDesktopWatcher();
            SaveSettings();
            return ok;
        }

        // ---- Raccourcis globaux ---------------------------------------------------------------------------

        bool RegisterHotkeys()
        {
            if (msgWin == null || msgWin.Hwnd == IntPtr.Zero) return true;
            Native.UnregisterHotKey(msgWin.Hwnd, 1);
            Native.UnregisterHotKey(msgWin.Hwnd, 2);
            bool ok = true;
            if (S.QuickVk != 0 && !Native.RegisterHotKey(msgWin.Hwnd, 1, S.QuickMods | Native.MOD_NOREPEAT, S.QuickVk)) ok = false;
            if (S.ToggleVk != 0 && !Native.RegisterHotKey(msgWin.Hwnd, 2, S.ToggleMods | Native.MOD_NOREPEAT, S.ToggleVk)) ok = false;
            return ok;
        }

        public void OnHotkey(int id)
        {
            if (id == 1) QuickAdd();
            else if (id == 2) ToggleAll();
        }

        public void QuickAdd()
        {
            if (quick != null) { try { quick.Activate(); } catch { } return; }
            quick = new QuickAddWindow(this);
            quick.Closed += delegate { quick = null; };
            quick.Show();
        }

        public void Toast(string message)
        {
            if (tray != null) tray.Balloon(App.Name, message);
        }

        public void OpenDataFolder()
        {
            try { Directory.CreateDirectory(App.DataDir); FileOps.OpenFolder(App.DataDir); } catch { }
        }

        // ---- Tri automatique du bureau ---------------------------------------------------------------------

        void SetupDesktopWatcher()
        {
            if (desktopWatcher != null) { desktopWatcher.EnableRaisingEvents = false; desktopWatcher.Dispose(); desktopWatcher = null; }
            if (!S.AutoSort) return;
            try
            {
                desktopWatcher = new FileSystemWatcher(App.Desktop) { NotifyFilter = NotifyFilters.FileName, IncludeSubdirectories = false };
                desktopWatcher.Created += delegate(object s, FileSystemEventArgs e) { app.Dispatcher.BeginInvoke(new Action(delegate { QueueSort(e.FullPath); })); };
                desktopWatcher.Renamed += delegate(object s, RenamedEventArgs e) { app.Dispatcher.BeginInvoke(new Action(delegate { QueueSort(e.FullPath); })); };
                desktopWatcher.EnableRaisingEvents = true;
            }
            catch (Exception ex) { Log.Error(ex, "surveillance du bureau"); }
        }

        void QueueSort(string path)
        {
            pendingSort[path] = DateTime.Now;
            sortTimer.Stop();
            sortTimer.Start();
        }

        void ProcessPendingSort()
        {
            sortTimer.Stop();
            var paths = pendingSort.Keys.ToList();
            pendingSort.Clear();
            foreach (var p in paths)
            {
                try
                {
                    if (!File.Exists(p)) continue;
                    string name = Path.GetFileName(p);
                    if (name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".crdownload", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".part", StringComparison.OrdinalIgnoreCase)) continue;
                    var target = Cadres.FirstOrDefault(w => !w.Info.IsDesktop && FileOps.MatchesRules(name, w.Info.Rules) && Directory.Exists(w.Folder));
                    if (target == null) continue;
                    if (FileOps.Transfer(new[] { p }, target.Folder, false, true, IntPtr.Zero))
                    {
                        target.RefreshSoon();
                        foreach (var w in Cadres) if (w.Info.IsDesktop) w.RefreshSoon();
                        tray.Balloon("Rangé dans « " + target.Info.Name + " »", name);
                    }
                }
                catch (Exception ex) { Log.Error(ex, "tri automatique"); }
            }
        }

        // ---- Tâches : compteurs, rappel ----------------------------------------------------------------------

        void OnCounts()
        {
            if (tray == null) return;
            string t = App.Name;
            if (Tasks.CountOverdue > 0) t += " — " + Tasks.CountOverdue + " en retard";
            else if (Tasks.CountToday > 0) t += " — " + Tasks.CountToday + " aujourd'hui";
            else if (Tasks.CountTodo > 0) t += " — " + Tasks.CountTodo + " à faire";
            tray.SetText(t);
        }

        void Tick()
        {
            string day = DateTime.Today.ToString("yyyy-MM-dd");
            if (day != reminderDay && reminderDay != null)
            {
                Tasks.RefreshDates();
                DailyReminder();
            }
            reminderDay = day;
        }

        void DailyReminder()
        {
            reminderDay = DateTime.Today.ToString("yyyy-MM-dd");
            if (!S.DailyReminder || tray == null) return;
            if (Tasks.CountToday == 0) return;
            string msg = Tasks.CountToday + " tâche" + (Tasks.CountToday > 1 ? "s" : "") + " à échéance aujourd'hui";
            if (Tasks.CountOverdue > 0) msg += ", dont " + Tasks.CountOverdue + " en retard";
            var first = Tasks.All.Where(t => t.Kind == 1 && !t.IsDone && t.Due.HasValue && t.Due.Value <= DateTime.Today).OrderBy(t => t.Due).ThenByDescending(t => t.Flag).FirstOrDefault();
            if (first != null) msg += "\n• " + first.Title;
            tray.Balloon("Échéances du jour", msg);
        }

        // ---- Sortie ------------------------------------------------------------------------------------------

        /// <summary>Écrit tout immédiatement (extinction de Windows, fermeture de session, Quitter).</summary>
        public void FlushAll(bool restoreIcons)
        {
            try
            {
                if (layoutDeb != null) layoutDeb.Flush();
                if (Tasks != null) Tasks.Save();
                if (L != null) Json.Save(LayoutPath, L);
                if (S != null) SaveSettings();
                if (restoreIcons && weHidIcons) { DesktopIcons.SetHiddenNow(false); weHidIcons = false; }
            }
            catch (Exception ex) { Log.Error(ex, "sauvegarde"); }
        }

        /// <summary>Plantage irrécupérable : les icônes du bureau sont réaffichées avant la fin du processus.</summary>
        public void OnCrash() { try { if (weHidIcons) DesktopIcons.SetHiddenNow(false); } catch { } }

        public void Quit()
        {
            if (Exiting) return;
            Exiting = true;
            FlushAll(true);
            Dispose();
            app.Shutdown();
        }

        public void Dispose()
        {
            try { if (settingsSignal != null) { settingsSignal.Close(); settingsSignal = null; } } catch { }
            if (watchdog != null) watchdog.Stop();
            if (clock != null) clock.Stop();
            if (desktopWatcher != null) { desktopWatcher.EnableRaisingEvents = false; desktopWatcher.Dispose(); desktopWatcher = null; }
            if (msgWin != null && msgWin.Hwnd != IntPtr.Zero) { Native.UnregisterHotKey(msgWin.Hwnd, 1); Native.UnregisterHotKey(msgWin.Hwnd, 2); }
            if (tray != null) { tray.Dispose(); tray = null; }
        }
    }
}
