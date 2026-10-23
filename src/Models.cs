using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows.Media;

namespace Pupitre
{
    [DataContract]
    public class Settings
    {
        [DataMember] public bool StartWithWindows;
        [DataMember] public bool LockLayout;
        [DataMember] public bool Snap;
        [DataMember] public bool HideDesktopIcons;
        [DataMember] public bool ShowExtensions;
        [DataMember] public int IconSize;         // 0 petites, 1 moyennes, 2 grandes
        [DataMember] public uint QuickMods, QuickVk;   // raccourci : nouvelle tâche
        [DataMember] public uint ToggleMods, ToggleVk; // raccourci : afficher / masquer
        [DataMember] public bool DailyReminder;
        [DataMember] public bool AutoSort;
        [DataMember] public bool TasksVisible;
        [DataMember] public bool NotesVisible;
        [DataMember] public bool MindVisible;
        [DataMember] public bool MindStyleOpen;
        [DataMember] public bool Welcomed;
        [DataMember] public string Services;      // services / pôles, séparés par des virgules
        [DataMember] public bool NoAutoDock;      // cadres : pas de fusion automatique quand un cadre est déposé contre un autre

        public Settings() { Defaults(); }
        [OnDeserializing] void OnDeserializing(StreamingContext c) { Defaults(); }
        void Defaults()
        {
            Snap = true; ShowExtensions = false; IconSize = 1;
            QuickMods = Native.MOD_CONTROL | Native.MOD_ALT; QuickVk = 0x4E;   // N
            ToggleMods = Native.MOD_CONTROL | Native.MOD_ALT; ToggleVk = 0x42; // B
            DailyReminder = true; AutoSort = true; TasksVisible = true; NotesVisible = true; MindVisible = true; MindStyleOpen = true;
            Services = "Direction, Commercial, Administratif, Technique, Marketing";
        }

        public List<string> ServiceList()
        {
            var list = new List<string>();
            foreach (var s in (Services ?? "").Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string t = s.Trim();
                if (t.Length > 0 && !list.Contains(t)) list.Add(t);
            }
            return list;
        }
    }

    [DataContract]
    public class CadreInfo
    {
        [DataMember] public string Id;
        [DataMember] public string Name;
        [DataMember] public string Folder;
        [DataMember] public int Color;        // index dans Palette (0 = sombre neutre)
        [DataMember] public int Opacity;      // -1 = défaut (60), sinon 0..100
        [DataMember] public string CustomColor; // #RRGGBB si Color == -1
        [DataMember] public double X, Y, W, H;
        [DataMember] public string Screen;    // écran d'appartenance (multi-écrans) et position relative à son espace de travail
        [DataMember] public double RelX, RelY;
        [DataMember] public bool Locked;      // position et taille figées (ce panneau seulement)
        [DataMember] public bool Rolled;
        [DataMember] public double RolledH;
        [DataMember] public bool Visible;
        [DataMember] public int IconSize;     // -1 = défaut global
        [DataMember] public string Sort;      // name | type | date
        [DataMember] public bool IsDesktop;   // cadre « Bureau »
        [DataMember] public string Rules;     // tri automatique : *.pdf; *.docx
        [DataMember] public bool HideFolder;
        [DataMember] public List<string> Seen; // fichiers récents déjà consultés (plus marqués « nouveau »)
        [DataMember] public string Under;     // fusion : identifiant du cadre juste au-dessus dans la colonne (null = indépendant ou sommet)

        public CadreInfo() { Defaults(); }
        [OnDeserializing] void OnDeserializing(StreamingContext c) { Defaults(); }
        void Defaults() { Visible = true; IconSize = -1; Opacity = -1; Sort = "name"; HideFolder = true; Color = 0; Seen = new List<string>(); }
    }

    [DataContract]
    public class PanelInfo
    {
        [DataMember] public double X, Y, W, H;
        [DataMember] public bool Rolled;
        [DataMember] public double RolledH;
        [DataMember] public bool Set;
        [DataMember] public int Color;
        [DataMember] public int Opacity;
        [DataMember] public string CustomColor;
        [DataMember] public string Screen;
        [DataMember] public double RelX, RelY;
        [DataMember] public bool Locked;      // position et taille figées (ce panneau seulement)

        public PanelInfo() { Defaults(); }
        [OnDeserializing] void OnDeserializing(StreamingContext c) { Defaults(); }
        void Defaults() { Color = 1; Opacity = -1; }
    }

    [DataContract]
    public class NoteInfo
    {
        [DataMember] public string Id;
        [DataMember] public string Title;
        [DataMember] public string Text;
        [DataMember] public int Color;
        [DataMember] public string Updated;   // dernière modification
        [DataMember] public bool Expanded;

        public NoteInfo() { Defaults(); }
        [OnDeserializing] void OnDeserializing(StreamingContext c) { Defaults(); }
        void Defaults() { Color = 7; Title = "Note"; }
    }

    /// <summary>Géométrie d'un panneau dans un profil de disposition.</summary>
    [DataContract]
    public class PanelGeom
    {
        [DataMember] public string Id;
        [DataMember] public double X, Y, W, H;
        [DataMember] public bool Rolled;
        [DataMember] public bool Visible;
        [DataMember] public string Screen;
        [DataMember] public double RelX, RelY;
    }

    /// <summary>Disposition mémorisée : automatique (une par configuration d'écrans) ou instantané nommé.</summary>
    [DataContract]
    public class LayoutProfile
    {
        [DataMember] public string Id;
        [DataMember] public string Name;
        [DataMember] public bool Auto;          // mise à jour en continu pour sa configuration d'écrans
        [DataMember] public string Signature;   // écrans (noms + géométrie) au moment de l'enregistrement
        [DataMember] public string Screens;     // description lisible
        [DataMember] public string Updated;
        [DataMember] public bool TasksVisible, NotesVisible, MindVisible;
        [DataMember] public List<PanelGeom> Cadres;
        [DataMember] public PanelGeom Tasks, Notes, Mind;

        public LayoutProfile() { Cadres = new List<PanelGeom>(); }
        [OnDeserializing] void OnDeserializing(StreamingContext c) { Cadres = new List<PanelGeom>(); }
    }

    [DataContract]
    public class Layout
    {
        [DataMember] public int Version;
        [DataMember] public List<CadreInfo> Cadres;
        [DataMember] public PanelInfo Tasks;
        [DataMember] public List<NoteInfo> Notes;
        [DataMember] public PanelInfo NotesPanel;
        [DataMember] public PanelInfo MindPanel;
        [DataMember] public List<LayoutProfile> Profiles;

        public Layout() { Defaults(); }
        [OnDeserializing] void OnDeserializing(StreamingContext c) { Defaults(); }
        void Defaults() { Cadres = new List<CadreInfo>(); Tasks = new PanelInfo(); Notes = new List<NoteInfo>(); NotesPanel = new PanelInfo { Color = 7 }; MindPanel = new PanelInfo { Color = 5 }; Profiles = new List<LayoutProfile>(); Version = 3; }
    }

    /// <summary>Service / pôle : badge coloré sur les thématiques, filtre.</summary>
    [DataContract]
    public class ServiceDef
    {
        [DataMember] public string Id;
        [DataMember] public string Name;
        [DataMember] public int Color;        // index de palette (1..9), -1 = couleur personnalisée
        [DataMember] public string CustomColor; // #RRGGBB
    }

    /// <summary>Étape du suivi d'une thématique. La première de la liste est l'état initial, la dernière vaut « terminé ».</summary>
    [DataContract]
    public class StageDef
    {
        [DataMember] public string Id;
        [DataMember] public string Name;
        [DataMember] public int Color;        // index de palette (1..9), -1 = couleur personnalisée
        [DataMember] public string CustomColor; // #RRGGBB
    }

    /// <summary>Entrée de l'historique d'une thématique : commentaire libre ou événement (« Étape → En cours »).</summary>
    [DataContract]
    public class CommentItem
    {
        [DataMember] public string Id;
        [DataMember] public string Type;      // "comment" ou "system"
        [DataMember] public string Text;
        [DataMember] public string At;        // ISO 8601
    }

    /// <summary>Élément du suivi : catégorie (Kind 0) → thématique (Kind 1) → tâche (Kind 2).</summary>
    [DataContract]
    public class TaskItem
    {
        [DataMember] public string Id;
        [DataMember] public string ParentId;  // null pour une catégorie
        [DataMember] public int Kind;         // 0 catégorie, 1 thématique, 2 tâche
        [DataMember] public string Title;
        [DataMember] public string Notes;
        [DataMember] public string Tags;
        [DataMember] public string Service;   // nom du service, lu par la migration des fichiers antérieurs
        [DataMember] public string ServiceId; // thématique : service rattaché
        [DataMember] public int Priority;     // lu par la migration des fichiers antérieurs
        [DataMember] public bool Flag;        // thématique : priorité importante
        [DataMember] public string Due;       // thématique : yyyy-MM-dd ou null
        [DataMember] public int Status;       // 0 à faire, 2 terminée (tâche ou thématique) ; maintenu pour les versions antérieures
        [DataMember] public int Stage;        // thématique : 0 initiale, 1 intermédiaire ; maintenu pour les versions antérieures
        [DataMember] public string StageId;   // thématique : étape courante (TaskStore.Stages)
        [DataMember] public int Color;        // catégorie : index de palette, -1 = couleur personnalisée
        [DataMember] public string CustomColor; // catégorie : #RRGGBB
        [DataMember] public string Created;
        [DataMember] public string Completed;
        [DataMember] public double Order;
        [DataMember] public bool Collapsed;   // catégorie / thématique repliée
        [DataMember] public bool DoneOpen;    // section « terminées » dépliée
        [DataMember] public List<CommentItem> Comments;

        public TaskItem() { Priority = 1; Comments = new List<CommentItem>(); }
        [OnDeserializing] void OnDeserializing(StreamingContext c) { Priority = 1; Comments = new List<CommentItem>(); }
        [OnDeserialized] void OnDeserialized(StreamingContext c) { if (Comments == null) Comments = new List<CommentItem>(); }
    }

    [DataContract]
    public class TaskStore
    {
        [DataMember] public int Version;
        [DataMember] public List<ServiceDef> Services;
        [DataMember] public List<StageDef> Stages;
        [DataMember] public List<TaskItem> Tasks;
        public TaskStore() { Defaults(); }
        [OnDeserializing] void OnDeserializing(StreamingContext c) { Defaults(); }
        void Defaults() { Tasks = new List<TaskItem>(); Services = new List<ServiceDef>(); Stages = new List<StageDef>(); }
    }

    public static class Palette
    {
        public static readonly string[] Names = { "Sombre", "Bleu", "Vert", "Orange", "Rose", "Violet", "Cyan", "Jaune", "Rouge", "Ardoise" };
        public static readonly Color[] Colors =
        {
            Color.FromRgb(0x9A, 0xA0, 0xAA), Color.FromRgb(0x5B, 0x8D, 0xEF), Color.FromRgb(0x3F, 0xBF, 0x7F), Color.FromRgb(0xFF, 0x8A, 0x3D),
            Color.FromRgb(0xE2, 0x5C, 0x9A), Color.FromRgb(0x9B, 0x6C, 0xF6), Color.FromRgb(0x38, 0xBD, 0xF8), Color.FromRgb(0xF5, 0xC5, 0x42),
            Color.FromRgb(0xF2, 0x55, 0x4B), Color.FromRgb(0x64, 0x74, 0x8B)
        };
        static readonly Color Base = Color.FromRgb(0x14, 0x15, 0x1A);

        public static Color Get(int i) { return Colors[i < 0 || i >= Colors.Length ? 0 : i]; }
        public static SolidColorBrush Brush(int i) { var b = new SolidColorBrush(Get(i)); b.Freeze(); return b; }

        public const int DefaultOpacity = 60;

        public static Color ParseHex(string hex, Color fallback)
        {
            try { if (!string.IsNullOrEmpty(hex)) return (Color)ColorConverter.ConvertFromString(hex); } catch { }
            return fallback;
        }

        public static string ToHex(Color c) { return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2"); }

        /// <summary>Teinte de fond : sombre neutre (index 0), couleur personnalisée telle quelle (index -1), sinon couleur assombrie pour garder le texte lisible.</summary>
        public static Color Tint(int i, string custom)
        {
            if (i < 0) return ParseHex(custom, Base);
            return Tint(i);
        }

        public static Color Tint(int i)
        {
            if (i <= 0 || i >= Colors.Length) return Base;
            var c = Colors[i];
            const double k = 0.58;
            return Color.FromRgb((byte)(c.R * k + Base.R * (1 - k)), (byte)(c.G * k + Base.G * (1 - k)), (byte)(c.B * k + Base.B * (1 - k)));
        }

        /// <summary>Couleur stable pour une étiquette de service.</summary>
    }

    public static class IconSizes
    {
        public static readonly string[] Names = { "Petites", "Moyennes", "Grandes" };
        public static int Px(int size) { return size <= 0 ? 32 : (size == 1 ? 48 : 64); }
        public static double CellW(int size) { return size <= 0 ? 80 : (size == 1 ? 98 : 118); }
        public static double CellH(int size) { return size <= 0 ? 82 : (size == 1 ? 100 : 118); }
    }
}
