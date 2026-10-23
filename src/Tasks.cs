using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Pupitre
{
    public class RelayCommand : ICommand
    {
        readonly Action<object> run;
        public RelayCommand(Action<object> r) { run = r; }
        public bool CanExecute(object p) { return true; }
        public void Execute(object p) { run(p); }
        public event EventHandler CanExecuteChanged { add { } remove { } }
    }

    /// <summary>Syntaxe d'ajout rapide : « Indicateur surcharges ! @demain » → priorité importante, échéance.</summary>
    public static class QuickParse
    {
        static readonly string[] Days = { "dimanche", "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi" };
        static readonly string[] DaysShort = { "dim", "lun", "mar", "mer", "jeu", "ven", "sam" };

        public static void Apply(string text, TaskItem t)
        {
            var title = new List<string>();
            var tags = new List<string>();
            int bang = 0;
            DateTime? due = null;
            foreach (var raw in text.Split(' '))
            {
                string w = raw.Trim();
                if (w.Length == 0) continue;
                int trailing = 0;
                while (trailing < w.Length && w[w.Length - 1 - trailing] == '!') trailing++;
                if (trailing > 0)
                {
                    bang = Math.Max(bang, trailing);
                    w = w.Substring(0, w.Length - trailing);
                    if (w.Length == 0) continue;
                }
                if (w.Length > 1 && w[0] == '#') { tags.Add(w.Substring(1)); continue; }
                DateTime d;
                if (w.Length > 1 && w[0] == '@' && TryDate(w.Substring(1), out d)) { due = d; continue; }
                title.Add(w);
            }
            t.Title = string.Join(" ", title.ToArray()).Trim();
            if (t.Title.Length == 0) t.Title = text.Trim();
            if (bang > 0) { t.Flag = true; t.Priority = bang >= 2 ? 3 : 2; }
            if (tags.Count > 0) t.Tags = string.Join(" ", tags.ToArray());
            if (due.HasValue) t.Due = due.Value.ToString("yyyy-MM-dd");
        }

        public static bool TryDate(string s, out DateTime d)
        {
            var today = DateTime.Today;
            d = today;
            s = (s ?? "").Trim().ToLowerInvariant().TrimEnd('.', ',', ';');
            if (s.Length == 0) return false;
            switch (s)
            {
                case "auj": case "aujourd'hui": case "aujourdhui": case "today": d = today; return true;
                case "demain": case "dem": case "tomorrow": d = today.AddDays(1); return true;
                case "apres-demain": case "après-demain": case "apresdemain": d = today.AddDays(2); return true;
                case "semaine": case "sem": case "+1sem": d = today.AddDays(7); return true;
                case "hier": d = today.AddDays(-1); return true;
            }
            for (int i = 0; i < 7; i++)
            {
                if (s == Days[i] || s == DaysShort[i])
                {
                    int diff = (i - (int)today.DayOfWeek + 7) % 7;
                    if (diff == 0) diff = 7;
                    d = today.AddDays(diff);
                    return true;
                }
            }
            string num = s.TrimStart('+');
            if (num.EndsWith("j")) num = num.Substring(0, num.Length - 1);
            int n;
            if ((s.StartsWith("+") || s.EndsWith("j")) && int.TryParse(num, out n)) { d = today.AddDays(n); return true; }
            var parts = s.Split('/', '-', '.');
            if (parts.Length >= 2 && parts.Length <= 3)
            {
                int dd = 0, mm = 0, yy = today.Year;
                bool ok = int.TryParse(parts[0], out dd) && int.TryParse(parts[1], out mm);
                if (ok && parts.Length == 3) ok = int.TryParse(parts[2], out yy);
                if (ok)
                {
                    if (yy < 100) yy += 2000;
                    try
                    {
                        d = new DateTime(yy, mm, dd);
                        if (parts.Length == 2 && d < today) d = d.AddYears(1);
                        return true;
                    }
                    catch { }
                }
            }
            return false;
        }
    }

    /// <summary>Couleurs des services et des catégories : les 9 teintes de la palette de l'application.</summary>
    public static class TaskColors
    {
        public static readonly int[] Indices = { 1, 2, 3, 4, 5, 6, 7, 8 };
        public static int Norm(int i) { return i <= 0 || i >= Palette.Colors.Length ? 1 : i; }
        public static Color Get(int i) { return Palette.Get(Norm(i)); }
        /// <summary>Couleur effective : index de palette, ou couleur personnalisée (#RRGGBB) si l'index vaut -1.</summary>
        public static Color Of(int i, string custom) { return i < 0 ? Palette.ParseHex(custom, Get(1)) : Get(i); }
        public static Brush Solid(int i) { return Theme.Brush(0xFF, Get(i)); }
        public static Brush Solid(int i, string custom) { return Theme.Brush(0xFF, Of(i, custom)); }
        public static Brush Tint(int i, byte alpha) { return Theme.Brush(alpha, Get(i)); }
        public static Brush Tint(int i, string custom, byte alpha) { return Theme.Brush(alpha, Of(i, custom)); }
        /// <summary>Teinte éclaircie, lisible comme texte sur le verre sombre.</summary>
        public static Brush Light(int i) { return Light(i, null); }
        public static Brush Light(int i, string custom)
        {
            var c = Of(i, custom);
            return Theme.Brush(0xFF, Color.FromRgb((byte)Math.Min(255, c.R + 70), (byte)Math.Min(255, c.G + 70), (byte)Math.Min(255, c.B + 70)));
        }
        public static int Next(int count) { return Indices[count % Indices.Length]; }
    }

    /// <summary>Entrée de l'historique d'une thématique : commentaire modifiable en place, ou événement journalisé (lecture seule).</summary>
    public class CommentVm : INotifyPropertyChanged
    {
        public readonly CommentItem M;
        readonly TaskVm owner;
        bool editing;
        string backup;
        static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

        public CommentVm(TaskVm o, CommentItem m) { owner = o; M = m; }

        public string Text
        {
            get { return M.Text ?? ""; }
            set { if (M.Text != value) { M.Text = value; Notify("Text"); } }
        }
        public string AtText
        {
            get
            {
                DateTime d;
                return DateTime.TryParse(M.At, null, DateTimeStyles.RoundtripKind, out d) ? d.ToString("d MMM", Fr) + " · " + d.ToString("HH:mm") : "";
            }
        }
        public bool IsSystem { get { return M.Type == "system"; } }
        public bool CanEdit { get { return !IsSystem; } }
        public bool IsEditing { get { return editing; } set { if (editing != value) { editing = value; Notify("IsEditing"); Notify("NotEditing"); } } }
        public bool NotEditing { get { return !editing; } }
        public Brush Bg { get { return IsSystem ? Brushes.Transparent : Theme.Res<Brush>("FieldBg"); } }
        public Brush Fg { get { return IsSystem ? Theme.Res<Brush>("TextFaint") : Theme.Res<Brush>("Text"); } }
        public FontStyle Style { get { return IsSystem ? FontStyles.Italic : FontStyles.Normal; } }
        public Thickness Pad { get { return IsSystem ? new Thickness(6, 1, 6, 1) : new Thickness(9, 6, 9, 6); } }

        public void BeginEdit() { if (!CanEdit || editing) return; backup = Text; IsEditing = true; }
        public void EndEdit(bool commit)
        {
            if (!editing) return;
            if (!commit || Text.Trim().Length == 0) Text = backup;
            else Text = Text.Trim();
            IsEditing = false;
            if (Text != backup) owner.CommentEdited(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void Notify(string n) { var h = PropertyChanged; if (h != null) h(this, new PropertyChangedEventArgs(n)); }
    }

    /// <summary>Vue-modèle d'un élément du suivi : catégorie, thématique ou tâche.</summary>
    public class TaskVm : INotifyPropertyChanged
    {
        public readonly TaskItem M;
        readonly TasksVm owner;
        public TaskVm Parent;
        public List<TaskVm> Children = new List<TaskVm>();
        // catégorie : thématiques visibles ; thématique : tâches visibles (propriétés : WPF ne lie pas les champs)
        public ObservableCollection<TaskVm> Themes { get; private set; }
        public ObservableCollection<TaskVm> DoneThemes { get; private set; }
        public ObservableCollection<TaskVm> ActiveTasks { get; private set; }
        public ObservableCollection<TaskVm> DoneTasks { get; private set; }
        public ObservableCollection<CommentVm> Entries { get; private set; }
        bool editing, dragging, adding;
        string backup;
        static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

        public TaskVm(TasksVm o, TaskItem m)
        {
            owner = o; M = m;
            Themes = new ObservableCollection<TaskVm>(); DoneThemes = new ObservableCollection<TaskVm>();
            ActiveTasks = new ObservableCollection<TaskVm>(); DoneTasks = new ObservableCollection<TaskVm>();
            Entries = new ObservableCollection<CommentVm>();
            if (M.Comments == null) M.Comments = new List<CommentItem>();
            foreach (var c in M.Comments) Entries.Add(new CommentVm(this, c));
        }

        public string Id { get { return M.Id; } }
        public int Kind { get { return M.Kind; } }
        public double Order { get { return M.Order; } }
        public string Created { get { return M.Created ?? ""; } }
        public bool IsCategory { get { return M.Kind == 0; } }
        public bool IsTheme { get { return M.Kind == 1; } }
        public bool IsTask { get { return M.Kind == 2; } }

        // ---- commun ----
        public string Title
        {
            get { return M.Title ?? ""; }
            set { if (M.Title != value) { M.Title = value; Notify("Title"); owner.Changed(this, false); } }
        }
        public bool IsOpen
        {
            get { return !M.Collapsed || owner.FilterActive; }
            set { if (M.Collapsed == value) { M.Collapsed = !value; Notify("IsOpen"); Notify("ChevronData"); Notify("HeadRadius"); owner.Changed(this, false); } }
        }
        public bool DoneOpen
        {
            get { return M.DoneOpen; }
            set { if (M.DoneOpen != value) { M.DoneOpen = value; Notify("DoneOpen"); Notify("DoneThemesLabel"); Notify("DoneTasksLabel"); owner.Changed(this, false); } }
        }
        public Geometry ChevronData { get { return Theme.Res<Geometry>(IsOpen ? "IcoChevronDown" : "IcoChevronRight"); } }
        public bool IsEditing { get { return editing; } set { if (editing != value) { editing = value; Notify("IsEditing"); Notify("NotEditing"); } } }
        public bool NotEditing { get { return !editing; } }
        public bool IsDragging { get { return dragging; } set { if (dragging != value) { dragging = value; Notify("IsDragging"); Notify("RowOpacity"); } } }
        public bool Adding { get { return adding; } set { if (adding != value) { adding = value; Notify("Adding"); Notify("IsEmpty"); } } }
        public Visibility GripVisibility { get { return !owner.FilterActive && !IsDone ? Visibility.Visible : Visibility.Collapsed; } }

        public void BeginEdit() { backup = Title; IsEditing = true; }
        public void EndEdit(bool commit)
        {
            if (!IsEditing) return;
            if (!commit) Title = backup;
            if (string.IsNullOrWhiteSpace(Title))
            {
                if (string.IsNullOrWhiteSpace(backup)) { IsEditing = false; owner.Delete(this); return; }
                Title = backup;
            }
            IsEditing = false;
            owner.AfterEdit();
        }

        // ---- catégorie ----
        public int Color { get { return M.Color; } }
        public string CustomColor { get { return M.CustomColor; } }
        public Brush ColorBrush { get { return TaskColors.Solid(M.Color, M.CustomColor); } }
        public Brush HeadBg { get { return TaskColors.Tint(M.Color, M.CustomColor, 0x26); } }
        public Brush NameBrush { get { return TaskColors.Light(M.Color, M.CustomColor); } }
        public CornerRadius HeadRadius { get { return IsOpen ? new CornerRadius(0, 9, 0, 0) : new CornerRadius(0, 9, 9, 0); } }
        public string CountText
        {
            get
            {
                if (!IsCategory) return "";
                int open = Children.Count(t => !t.IsDone), done = Children.Count - open;
                string s = open + " en cours";
                if (done > 0) s += " · " + done + " terminée" + (done > 1 ? "s" : "");
                return s;
            }
        }
        public bool IsEmpty { get { return IsCategory && Children.Count == 0 && !adding; } }
        public bool HasDoneThemes { get { return DoneThemes.Count > 0; } }
        public string DoneThemesLabel { get { return (M.DoneOpen ? "▾  " : "▸  ") + "Tâches terminées · " + DoneThemes.Count; } }
        public void NotifyLook() { Notify("ColorBrush"); Notify("HeadBg"); Notify("NameBrush"); Notify("SvcName"); Notify("SvcBg"); Notify("SvcFg"); Notify("HasService"); }

        // ---- thématique : étape ----
        public int Stage { get { return owner.StageIndex(M); } }
        StageDef St { get { return owner.StageAt(Stage); } }
        public string StLabel { get { return St.Name; } }
        public Brush StDot { get { return TaskColors.Solid(St.Color, St.CustomColor); } }
        public Brush StBg { get { return TaskColors.Tint(St.Color, St.CustomColor, 0x3A); } }
        public Brush StFg { get { return TaskColors.Light(St.Color, St.CustomColor); } }
        public void NotifyStage() { Notify("Stage"); Notify("StLabel"); Notify("StDot"); Notify("StBg"); Notify("StFg"); }
        /// <summary>Change l'étape, journalise l'événement ; la dernière étape (terminé) ouvre la section « terminés » de la catégorie.</summary>
        public void SetStage(int s)
        {
            s = Math.Max(0, Math.Min(owner.LastStage, s));
            if (!IsTheme || Stage == s) return;
            bool done = s == owner.LastStage;
            var def = owner.StageAt(s);
            M.StageId = def.Id;
            M.Stage = !done && s > 0 ? 1 : 0;        // champs des versions antérieures, maintenus
            M.Status = done ? 2 : 0;
            M.Completed = done ? DateTime.Now.ToString("s") : null;
            AddComment("Étape → " + def.Name, true);
            if (done) { IsEditing = false; if (Parent != null) Parent.DoneOpen = true; }
            NotifyStage(); Notify("IsDone"); Notify("Strike"); Notify("RowOpacity"); Notify("DoneAtText"); Notify("GripVisibility"); Notify("DueFg"); Notify("DueBg");
            owner.Changed(this, true);
        }
        public string ServiceId { get { return M.ServiceId; } }
        public ServiceDef Svc { get { return owner.ServiceById(M.ServiceId); } }
        public bool HasService { get { return Svc != null; } }
        public string SvcName { get { var s = Svc; return s != null ? s.Name : "Service"; } }
        public Brush SvcBg { get { var s = Svc; return s != null ? TaskColors.Tint(s.Color, s.CustomColor, 0xD0) : Theme.Argb(0x16FFFFFF); } }
        public Brush SvcFg { get { return Svc != null ? Brushes.White : Theme.Res<Brush>("TextFaint"); } }
        public void SetService(string id)
        {
            if (M.ServiceId == id) return;
            M.ServiceId = id;
            Notify("ServiceId"); Notify("Svc"); Notify("HasService"); Notify("SvcName"); Notify("SvcBg"); Notify("SvcFg");
            owner.Changed(this, true);
        }
        public bool Flag
        {
            get { return M.Flag; }
            set { if (M.Flag != value) { M.Flag = value; M.Priority = value ? 2 : 1; Notify("Flag"); Notify("FlagBrush"); Notify("FlagFill"); Notify("FlagTip"); owner.Changed(this, false); } }
        }
        public Brush FlagBrush { get { return M.Flag ? Theme.Res<Brush>("Danger") : Theme.Res<Brush>("TextFaint"); } }
        public Brush FlagFill { get { return M.Flag ? Theme.Res<Brush>("Danger") : Brushes.Transparent; } }
        public string FlagTip { get { return M.Flag ? "Priorité importante — cliquer pour retirer" : "Marquer comme important"; } }

        public DateTime? Due
        {
            get
            {
                DateTime d;
                if (!string.IsNullOrEmpty(M.Due) && DateTime.TryParseExact(M.Due, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return d;
                return null;
            }
            set
            {
                string s = value.HasValue ? value.Value.ToString("yyyy-MM-dd") : null;
                if (M.Due == s) return;
                M.Due = s;
                RefreshDue();
                owner.Changed(this, true);
            }
        }
        public void RefreshDue() { Notify("Due"); Notify("DueText"); Notify("DueBg"); Notify("DueFg"); Notify("DueVisible"); }
        public bool DueVisible { get { return Due.HasValue; } }
        int DueState
        {
            get
            {
                if (!Due.HasValue) return 0;
                if (IsDone) return 5;
                int days = (Due.Value - DateTime.Today).Days;
                if (days < 0) return 1;
                if (days == 0) return 2;
                if (days <= 7) return 3;
                return 4;
            }
        }
        public string DueText
        {
            get
            {
                if (!Due.HasValue) return "";
                var due = Due.Value; var today = DateTime.Today;
                int days = (due - today).Days;
                if (days == 0) return "Aujourd'hui";
                if (days == 1) return "Demain";
                if (days == -1) return "Hier";
                string f = due.Year == today.Year ? due.ToString("d MMM", Fr) : due.ToString("d MMM yyyy", Fr);
                if (days < 0 && !IsDone) return "Retard · " + f;
                return f;
            }
        }
        public Brush DueFg
        {
            get
            {
                switch (DueState)
                {
                    case 1: return Theme.Res<Brush>("Danger");
                    case 2: return Theme.Res<Brush>("Warn");
                    case 3: return Theme.Res<Brush>("AccentHover");
                    default: return Theme.Res<Brush>("TextMuted");
                }
            }
        }
        public Brush DueBg
        {
            get
            {
                switch (DueState)
                {
                    case 1: return Theme.Brush(0x30, System.Windows.Media.Color.FromRgb(0xF2, 0x55, 0x4B));
                    case 2: return Theme.Brush(0x30, System.Windows.Media.Color.FromRgb(0xFF, 0xA6, 0x4D));
                    case 3: return Theme.Brush(0x2A, System.Windows.Media.Color.FromRgb(0x5B, 0x8D, 0xEF));
                    default: return Theme.Res<Brush>("Hover");
                }
            }
        }

        public int CommentCount { get { return M.Comments.Count(x => x.Type != "system"); } }
        public bool HasComments { get { return CommentCount > 0; } }
        public string LastComment { get { var c = M.Comments.LastOrDefault(x => x.Type != "system"); return c != null ? c.Text.Replace("\r", " ").Replace("\n", " ") : ""; } }
        public Brush CommentBrush { get { return HasComments ? Theme.Res<Brush>("TextMuted") : Theme.Res<Brush>("TextFaint"); } }
        public void AddComment(string text, bool system)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0) return;
            var c = new CommentItem { Id = Guid.NewGuid().ToString("N"), Type = system ? "system" : "comment", Text = text, At = DateTime.Now.ToString("s") };
            M.Comments.Add(c);
            Entries.Add(new CommentVm(this, c));
            NotifyComments();
            owner.Changed(this, false);
        }
        public void CommentEdited(CommentVm cm) { NotifyComments(); owner.Changed(this, false); }
        public void RemoveComment(CommentVm cm)
        {
            M.Comments.Remove(cm.M);
            Entries.Remove(cm);
            NotifyComments();
            owner.Changed(this, false);
        }
        void NotifyComments() { Notify("CommentCount"); Notify("HasComments"); Notify("LastComment"); Notify("CommentBrush"); }

        public string ProgressText { get { int n = Children.Count; return IsTheme && n > 0 ? Children.Count(k => k.IsDone) + "/" + n : ""; } }
        public bool ProgressVisible { get { return IsTheme && Children.Count > 0; } }
        public bool HasDoneTasks { get { return DoneTasks.Count > 0; } }
        public string DoneTasksLabel { get { int n = DoneTasks.Count; return (M.DoneOpen ? "▾  " : "▸  ") + n + (n > 1 ? " sous-tâches réalisées" : " sous-tâche réalisée"); } }

        // ---- tâche (et thématique terminée) ----
        public bool IsDone
        {
            get { return M.Status == 2; }
            set
            {
                if (IsDone == value) return;
                if (IsTheme) { SetStage(value ? owner.LastStage : owner.ActiveStage); return; }
                M.Status = value ? 2 : 0;
                M.Completed = value ? DateTime.Now.ToString("s") : null;
                if (value) IsEditing = false;
                Notify("IsDone"); Notify("Strike"); Notify("RowOpacity"); Notify("DoneAtText"); Notify("GripVisibility");
                if (value && Parent != null && Parent.IsTheme && Parent.Stage == 0) Parent.SetStage(owner.ActiveStage);
                owner.Changed(this, true);
            }
        }
        public string DoneAtText
        {
            get
            {
                DateTime d;
                if (!IsDone || string.IsNullOrEmpty(M.Completed) || !DateTime.TryParse(M.Completed, null, DateTimeStyles.RoundtripKind, out d)) return "";
                return d.ToString("d MMM", Fr);
            }
        }
        public TextDecorationCollection Strike { get { return IsDone ? TextDecorations.Strikethrough : null; } }
        public double RowOpacity { get { return dragging ? 0.28 : (IsDone ? 0.6 : 1.0); } }

        public void NotifyStructure()
        {
            Notify("CountText"); Notify("IsEmpty"); Notify("HasDoneThemes"); Notify("DoneThemesLabel"); Notify("ProgressText"); Notify("ProgressVisible"); Notify("HasDoneTasks"); Notify("DoneTasksLabel"); Notify("GripVisibility"); Notify("IsOpen"); Notify("ChevronData"); Notify("HeadRadius");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void Notify(string n) { var h = PropertyChanged; if (h != null) h(this, new PropertyChangedEventArgs(n)); }
    }

    /// <summary>
    /// Suivi de projets : catégories → thématiques (service, statut, priorité, commentaires, échéance) → tâches.
    /// Vocabulaire de l'interface : une « thématique » du code est affichée comme « tâche », une « tâche » du code comme « sous-tâche ».
    /// </summary>
    public class TasksVm
    {
        readonly Controller c;
        readonly TaskStore store;
        public readonly List<TaskVm> All = new List<TaskVm>();
        public readonly ObservableCollection<TaskVm> Cats = new ObservableCollection<TaskVm>();
        public readonly HashSet<string> ServiceFilter = new HashSet<string>();
        public string Search = "";
        readonly Debouncer saveDeb, rebuildDeb;
        public event Action CountsChanged;
        public int CountTodo, CountToday, CountOverdue, CountThemesOpen, CountThemesDoing, CountThemesDone, CountTasksTotal, CountTasksDone;

        public TasksVm(Controller ctl, TaskStore s)
        {
            c = ctl; store = s;
            bool migrated = Migrate() | MigrateV3() | MigrateV4();
            foreach (var t in store.Tasks)
            {
                if (string.IsNullOrEmpty(t.Id)) t.Id = Guid.NewGuid().ToString("N");
                All.Add(new TaskVm(this, t));
            }
            saveDeb = new Debouncer(700, Save);
            rebuildDeb = new Debouncer(380, Rebuild);
            Rebuild();
            if (migrated) saveDeb.Trigger();
        }

        public List<ServiceDef> Services { get { return store.Services; } }
        public bool FilterActive { get { return ServiceFilter.Count > 0 || Search.Length > 0; } }
        /// <summary>Catégories masquées par le filtre ou la recherche (aucune tâche correspondante).</summary>
        public int HiddenCategories { get; private set; }
        /// <summary>Filtre et recherche viennent d'être effacés (pour montrer une catégorie neuve) : la fenêtre se met à jour.</summary>
        public event Action FilterReset;

        /// <summary>Migration des fichiers antérieurs (projets → sous-tâches, services en texte) : catégorie « Général », thématiques, tâches, services nommés.</summary>
        bool Migrate()
        {
            if (store.Version >= 2) return false;
            var names = c.S.ServiceList();
            foreach (var t in store.Tasks) if (!string.IsNullOrEmpty(t.Service) && !names.Contains(t.Service, StringComparer.CurrentCultureIgnoreCase)) names.Add(t.Service);
            var map = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            for (int i = 0; i < names.Count; i++)
            {
                var sd = new ServiceDef { Id = Guid.NewGuid().ToString("N"), Name = names[i], Color = TaskColors.Next(i) };
                store.Services.Add(sd);
                map[sd.Name] = sd.Id;
            }
            if (store.Tasks.Count > 0)
            {
                var cat = new TaskItem { Id = Guid.NewGuid().ToString("N"), Kind = 0, Title = "Général", Color = 1, Created = DateTime.Now.ToString("s"), Order = 0 };
                foreach (var t in store.Tasks.Where(t => t.ParentId == null).ToList())
                {
                    t.Kind = 1;
                    t.ParentId = cat.Id;
                    if (!string.IsNullOrEmpty(t.Service) && map.ContainsKey(t.Service)) t.ServiceId = map[t.Service];
                    t.Flag = t.Priority >= 2;
                    bool anyDone = store.Tasks.Any(k => k.ParentId == t.Id && k.Status == 2);
                    t.Stage = t.Status == 2 ? 0 : (anyDone ? 1 : 0);
                }
                foreach (var t in store.Tasks) if (t.Kind == 0 && t.ParentId != null) t.Kind = 2;
                store.Tasks.Insert(0, cat);
            }
            store.Version = 2;
            MigrateV3();
            return true;
        }

        /// <summary>Complément de migration : une thématique sans service hérite du service le plus fréquent de ses tâches.</summary>
        bool MigrateV3()
        {
            if (store.Version >= 3) return false;
            foreach (var th in store.Tasks.Where(t => t.Kind == 1 && string.IsNullOrEmpty(t.ServiceId)))
            {
                var names = store.Tasks.Where(k => k.ParentId == th.Id && !string.IsNullOrEmpty(k.Service)).Select(k => k.Service.Trim()).ToList();
                if (string.IsNullOrEmpty(th.Service) && names.Count == 0) continue;
                string best = !string.IsNullOrEmpty(th.Service) ? th.Service.Trim() : names.GroupBy(n => n, StringComparer.CurrentCultureIgnoreCase).OrderByDescending(g => g.Count()).First().Key;
                var svc = store.Services.FirstOrDefault(x => string.Equals(x.Name, best, StringComparison.CurrentCultureIgnoreCase));
                if (svc == null) { svc = new ServiceDef { Id = Guid.NewGuid().ToString("N"), Name = best, Color = TaskColors.Next(store.Services.Count) }; store.Services.Add(svc); }
                th.ServiceId = svc.Id;
            }
            store.Version = 3;
            return true;
        }

        public ServiceDef ServiceById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return store.Services.FirstOrDefault(s => s.Id == id);
        }

        bool Match(TaskVm theme)
        {
            if (ServiceFilter.Count > 0 && (theme.ServiceId == null || !ServiceFilter.Contains(theme.ServiceId))) return false;
            if (Search.Length > 0)
            {
                var sb = new StringBuilder(theme.Title).Append(' ').Append(theme.SvcName);
                foreach (var k in theme.Children) sb.Append(' ').Append(k.Title);
                foreach (var cm in theme.M.Comments) sb.Append(' ').Append(cm.Text);
                if (sb.ToString().IndexOf(Search, StringComparison.CurrentCultureIgnoreCase) < 0) return false;
            }
            return true;
        }

        static List<TaskVm> Sorted(IEnumerable<TaskVm> items) { return items.OrderBy(t => t.Order).ThenBy(t => t.Created).ToList(); }

        static void Sync(ObservableCollection<TaskVm> target, List<TaskVm> desired)
        {
            if (target.Count == desired.Count)
            {
                bool same = true;
                for (int i = 0; i < desired.Count; i++) if (target[i] != desired[i]) { same = false; break; }
                if (same) return;
            }
            target.Clear();
            foreach (var d in desired) target.Add(d);
        }

        /// <summary>Relie parents et enfants, applique filtres et recherche, remplit les listes affichées.</summary>
        public void Rebuild()
        {
            if (All.Any(t => t.IsEditing)) { rebuildDeb.Trigger(); return; }
            var byId = new Dictionary<string, TaskVm>();
            foreach (var t in All) { byId[t.Id] = t; t.Children.Clear(); t.Parent = null; }
            TaskVm fallback = null;
            foreach (var t in All)
            {
                if (t.Kind == 0) { t.M.ParentId = null; continue; }
                TaskVm p;
                if (t.M.ParentId != null && byId.TryGetValue(t.M.ParentId, out p) && p.Kind == t.Kind - 1) { t.Parent = p; p.Children.Add(t); }
                else if (t.Kind == 1)
                {
                    if (fallback == null) fallback = All.FirstOrDefault(x => x.Kind == 0);
                    if (fallback == null) { fallback = NewItem(0, null, "Général"); fallback.M.Color = 1; byId[fallback.Id] = fallback; }
                    t.M.ParentId = fallback.Id; t.Parent = fallback; fallback.Children.Add(t);
                }
            }
            foreach (var t in All) t.Children = Sorted(t.Children);
            var cats = Sorted(All.Where(t => t.Kind == 0));
            var visible = new List<TaskVm>();
            foreach (var cat in cats)
            {
                var themes = cat.Children.Where(Match).ToList();
                Sync(cat.Themes, themes.Where(t => !t.IsDone).ToList());
                Sync(cat.DoneThemes, themes.Where(t => t.IsDone).OrderByDescending(t => t.M.Completed ?? "").ToList());
                foreach (var th in cat.Children)
                {
                    Sync(th.ActiveTasks, th.Children.Where(k => !k.IsDone).ToList());
                    Sync(th.DoneTasks, th.Children.Where(k => k.IsDone).OrderByDescending(k => k.M.Completed ?? "").ToList());
                    th.NotifyStructure();
                }
                cat.NotifyStructure();
                if (!FilterActive || themes.Count > 0) visible.Add(cat);
            }
            Sync(Cats, visible);
            HiddenCategories = cats.Count - visible.Count;
            Recount();
        }

        public void SetSearch(string s) { Search = (s ?? "").Trim(); Rebuild(); }
        public void ToggleServiceFilter(string id) { if (!ServiceFilter.Remove(id)) ServiceFilter.Add(id); Rebuild(); }
        public void ClearFilter() { ServiceFilter.Clear(); Rebuild(); }

        // ---- création ------------------------------------------------------------------------------

        TaskVm NewItem(int kind, TaskVm parent, string title)
        {
            var m = new TaskItem { Id = Guid.NewGuid().ToString("N"), Kind = kind, ParentId = parent == null ? null : parent.Id, Title = title, Created = DateTime.Now.ToString("s") };
            var siblings = All.Where(t => t.Kind == kind && t.M.ParentId == m.ParentId).ToList();
            m.Order = siblings.Count == 0 ? 0 : siblings.Max(t => t.Order) + 10;
            var vm = new TaskVm(this, m);
            All.Add(vm);
            store.Tasks.Add(m);
            return vm;
        }

        public TaskVm AddCategory(string name)
        {
            var vm = NewItem(0, null, name.Trim());
            vm.M.Color = TaskColors.Next(All.Count(t => t.Kind == 0) - 1);
            saveDeb.Trigger();
            // une catégorie neuve est vide : avec un filtre ou une recherche actifs elle resterait invisible, on les efface
            if (FilterActive) { ServiceFilter.Clear(); Search = ""; var h = FilterReset; if (h != null) h(); }
            Rebuild();
            return vm;
        }

        public TaskVm AddTheme(TaskVm cat, string text, string serviceId)
        {
            if (cat == null || !cat.IsCategory) return null;
            var vm = NewItem(1, cat, "");
            QuickParse.Apply(text, vm.M);
            if (!string.IsNullOrEmpty(serviceId)) vm.M.ServiceId = serviceId;
            cat.M.Collapsed = false;
            saveDeb.Trigger();
            Rebuild();
            return vm;
        }

        public TaskVm AddTask(TaskVm theme, string text)
        {
            if (theme == null || !theme.IsTheme) return null;
            var vm = NewItem(2, theme, text.Trim());
            theme.M.Collapsed = false;
            saveDeb.Trigger();
            Rebuild();
            return vm;
        }

        public void Delete(TaskVm t)
        {
            foreach (var k in All.Where(x => x.Parent == t).ToList()) Delete(k);
            All.Remove(t);
            store.Tasks.Remove(t.M);
            saveDeb.Trigger();
            Rebuild();
        }

        public void Changed(TaskVm t, bool rebuild)
        {
            saveDeb.Trigger();
            if (rebuild) rebuildDeb.Trigger(); else Recount();
        }

        public void AfterEdit() { rebuildDeb.Trigger(); }

        // ---- réorganisation ----------------------------------------------------------------------

        /// <summary>Liste affichée qui contient l'élément (pour le glisser en direct).</summary>
        public ObservableCollection<TaskVm> HostOf(TaskVm t)
        {
            if (t.IsCategory) return Cats;
            if (t.Parent == null) return null;
            return t.IsTheme ? t.Parent.Themes : t.Parent.ActiveTasks;
        }

        /// <summary>Déplace l'élément d'un cran dans sa liste (pendant le glisser) et mémorise le nouvel ordre.</summary>
        public void MoveWithin(TaskVm moved, int newIndex)
        {
            var coll = HostOf(moved);
            if (coll == null) return;
            int old = coll.IndexOf(moved);
            if (old < 0 || newIndex < 0 || newIndex >= coll.Count || old == newIndex) return;
            coll.Move(old, newIndex);
            for (int i = 0; i < coll.Count; i++) coll[i].M.Order = i * 10;
            if (moved.Parent != null) moved.Parent.Children = Sorted(moved.Parent.Children);
            saveDeb.Trigger();
        }

        public void MoveThemeTo(TaskVm theme, TaskVm cat)
        {
            if (!theme.IsTheme || cat == null || !cat.IsCategory || theme.Parent == cat) return;
            theme.M.ParentId = cat.Id;
            var siblings = All.Where(x => x.M.ParentId == cat.Id && x != theme).ToList();
            theme.M.Order = siblings.Count == 0 ? 0 : siblings.Max(x => x.Order) + 10;
            cat.M.Collapsed = false;
            saveDeb.Trigger();
            Rebuild();
        }

        // ---- étapes ---------------------------------------------------------------------------------

        public List<StageDef> StageList { get { return store.Stages; } }
        public int LastStage { get { return store.Stages.Count - 1; } }
        /// <summary>Étape « en cours » par défaut : la première intermédiaire, ou l'initiale s'il n'y en a pas.</summary>
        public int ActiveStage { get { return Math.Max(0, Math.Min(1, LastStage - 1)); } }
        public StageDef StageAt(int i) { return store.Stages[Math.Max(0, Math.Min(LastStage, i))]; }

        /// <summary>Index de l'étape d'une thématique ; sans identifiant (fichier antérieur), déduit de Status et Stage.</summary>
        public int StageIndex(TaskItem m)
        {
            if (!string.IsNullOrEmpty(m.StageId)) { int i = store.Stages.FindIndex(x => x.Id == m.StageId); if (i >= 0) return i; }
            return m.Status == 2 ? LastStage : (m.Stage == 1 ? ActiveStage : 0);
        }

        /// <summary>Étapes par défaut (celles des versions antérieures) quand le magasin n'en a pas.</summary>
        void EnsureStages()
        {
            if (store.Stages.Count >= 2) return;
            store.Stages.Clear();
            store.Stages.Add(new StageDef { Id = Guid.NewGuid().ToString("N"), Name = "Non commencé", Color = -1, CustomColor = "#94A3B8" });
            store.Stages.Add(new StageDef { Id = Guid.NewGuid().ToString("N"), Name = "En cours", Color = 1 });
            store.Stages.Add(new StageDef { Id = Guid.NewGuid().ToString("N"), Name = "Terminé", Color = 2 });
        }

        /// <summary>Version 4 : liste d'étapes dans le magasin, identifiant d'étape sur chaque thématique.</summary>
        bool MigrateV4()
        {
            bool changed = store.Stages.Count < 2;
            EnsureStages();
            foreach (var t in store.Tasks) if (t.Kind == 1 && string.IsNullOrEmpty(t.StageId)) { t.StageId = StageAt(StageIndex(t)).Id; changed = true; }
            if (store.Version < 4) { store.Version = 4; changed = true; }
            return changed;
        }

        /// <summary>Nouvelle étape intermédiaire, insérée juste avant « terminé ».</summary>
        public StageDef AddStage(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) return null;
            var s = new StageDef { Id = Guid.NewGuid().ToString("N"), Name = name, Color = TaskColors.Next(store.Stages.Count) };
            store.Stages.Insert(LastStage, s);
            StagesChanged();
            return s;
        }

        public void RenameStage(StageDef s, string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0 || s.Name == name) return;
            s.Name = name;
            StagesChanged();
        }

        public void SetStageColor(StageDef s, int color, string custom)
        {
            s.Color = color;
            if (custom != null) s.CustomColor = custom;
            StagesChanged();
        }

        /// <summary>Déplace une étape intermédiaire d'un cran ; la première et la dernière restent en place.</summary>
        public void MoveStage(StageDef s, int delta)
        {
            int i = store.Stages.IndexOf(s), j = i + delta;
            if (i <= 0 || i >= LastStage || j <= 0 || j >= LastStage) return;
            store.Stages.RemoveAt(i);
            store.Stages.Insert(j, s);
            StagesChanged();
        }

        /// <summary>Supprime une étape intermédiaire ; ses thématiques reviennent à l'étape précédente.</summary>
        public void RemoveStage(StageDef s)
        {
            int i = store.Stages.IndexOf(s);
            if (i <= 0 || i >= LastStage) return;
            var prev = store.Stages[i - 1];
            foreach (var t in All) if (t.IsTheme && t.M.StageId == s.Id) { t.M.StageId = prev.Id; t.M.Stage = i - 1 > 0 ? 1 : 0; }
            store.Stages.RemoveAt(i);
            StagesChanged();
        }

        public int StageUsage(StageDef s) { return All.Count(t => t.IsTheme && t.M.StageId == s.Id); }

        void StagesChanged()
        {
            foreach (var t in All) if (t.IsTheme) t.NotifyStage();
            saveDeb.Trigger();
            Recount();
        }

        // ---- services et catégories -----------------------------------------------------------------

        public ServiceDef AddService(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) return null;
            var s = new ServiceDef { Id = Guid.NewGuid().ToString("N"), Name = name, Color = TaskColors.Next(store.Services.Count) };
            store.Services.Add(s);
            saveDeb.Trigger();
            return s;
        }

        public void RenameService(ServiceDef s, string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0 || s.Name == name) return;
            s.Name = name;
            foreach (var t in All) if (t.ServiceId == s.Id) t.NotifyLook();
            saveDeb.Trigger();
        }

        public void SetServiceColor(ServiceDef s, int color, string custom)
        {
            s.Color = color;
            if (custom != null) s.CustomColor = custom;
            foreach (var t in All) if (t.ServiceId == s.Id) t.NotifyLook();
            saveDeb.Trigger();
        }

        public void RemoveService(ServiceDef s)
        {
            store.Services.Remove(s);
            ServiceFilter.Remove(s.Id);
            foreach (var t in All) if (t.ServiceId == s.Id) { t.M.ServiceId = null; t.NotifyLook(); }
            saveDeb.Trigger();
            Rebuild();
        }

        public int ServiceUsage(ServiceDef s) { return All.Count(t => t.IsTheme && t.ServiceId == s.Id); }

        public void SetCategoryColor(TaskVm cat, int color, string custom)
        {
            cat.M.Color = color;
            if (custom != null) cat.M.CustomColor = custom;
            cat.NotifyLook();
            saveDeb.Trigger();
        }

        public List<TaskVm> Categories() { return Sorted(All.Where(t => t.Kind == 0)); }
        public List<TaskVm> OpenThemes(TaskVm cat) { return Sorted(All.Where(t => t.Kind == 1 && t.Parent == cat && !t.IsDone)); }

        // ---- archivage, export ----------------------------------------------------------------------

        /// <summary>Déplace les thématiques terminées (et leurs tâches) dans archive.json.</summary>
        public int ArchiveDone()
        {
            var archive = Json.Load<TaskStore>(c.ArchivePath);
            var themes = All.Where(t => t.IsTheme && t.IsDone).ToList();
            int n = 0;
            foreach (var th in themes)
            {
                th.M.Completed = th.M.Completed ?? DateTime.Now.ToString("s");
                var cat = th.Parent;
                th.M.Notes = (cat != null ? cat.Title : "") ;   // catégorie d'origine, pour mémoire
                archive.Tasks.Add(th.M);
                foreach (var k in All.Where(x => x.Parent == th).ToList()) { archive.Tasks.Add(k.M); All.Remove(k); store.Tasks.Remove(k.M); }
                All.Remove(th);
                store.Tasks.Remove(th.M);
                n++;
            }
            if (n > 0) { archive.Version = 2; Json.Save(c.ArchivePath, archive); }
            saveDeb.Trigger();
            Rebuild();
            return n;
        }

        public string ExportText()
        {
            var sb = new StringBuilder();
            foreach (var cat in Categories())
            {
                sb.AppendLine("■ " + cat.Title.ToUpperInvariant());
                foreach (var th in cat.Children)
                {
                    var parts = new List<string>();
                    if (th.HasService) parts.Add(th.SvcName);
                    parts.Add(th.StLabel);
                    if (th.Flag) parts.Add("Important");
                    if (th.DueVisible) parts.Add(th.Due.Value.ToString("dd/MM/yyyy"));
                    sb.AppendLine("  " + (th.IsDone ? "✔ " : "● ") + th.Title + "  ·  " + string.Join(" · ", parts.ToArray()));
                    foreach (var k in th.Children) sb.AppendLine("      " + (k.IsDone ? "[x] " : "[ ] ") + k.Title);
                    var last = th.LastComment;
                    if (last.Length > 0) sb.AppendLine("      » " + last);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public void RefreshDates() { foreach (var t in All) if (t.IsTheme) t.RefreshDue(); Recount(); }

        void Recount()
        {
            var today = DateTime.Today;
            var themes = All.Where(t => t.IsTheme).ToList();
            CountThemesOpen = themes.Count(t => !t.IsDone);
            CountThemesDoing = themes.Count(t => !t.IsDone && t.Stage > 0);
            CountThemesDone = themes.Count(t => t.IsDone);
            CountTasksTotal = All.Count(t => t.IsTask);
            CountTasksDone = All.Count(t => t.IsTask && t.IsDone);
            CountTodo = CountThemesOpen;
            CountToday = themes.Count(t => !t.IsDone && t.Due.HasValue && t.Due.Value <= today);
            CountOverdue = themes.Count(t => !t.IsDone && t.Due.HasValue && t.Due.Value < today);
            var h = CountsChanged;
            if (h != null) h();
        }

        public void Save() { Json.Save(c.TasksPath, store); }
        public void Flush() { saveDeb.Flush(); }
    }
}
