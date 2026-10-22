using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WF = System.Windows.Forms;

namespace Pupitre
{
    /// <summary>Cartes mentales : modèles, marqueurs, mesure, disposition, dessin, vue, imports / exports.</summary>
    public partial class MindWindow
    {
        // ---- modèles, structures, noms ------------------------------------------------------------------------------

        /// <summary>Modèle de feuille : structure, style et sujets de départ.</summary>
        class MapTemplate { public string Id, Name, Desc; public int Layout, LineStyle, Theme, Shape, Palette; public string[] Children; }

        static readonly MapTemplate[] Templates =
        {
            new MapTemplate { Id = "mind", Name = "Carte mentale", Desc = "Branches réparties des deux côtés du sujet central, lignes courbes : idées, synthèses.", Layout = 0, LineStyle = 0, Theme = 4, Palette = 6, Shape = 1 },
            new MapTemplate { Id = "logic", Name = "Schéma logique", Desc = "Tout se déroule vers la droite, lignes coudées : plans, analyses, décompositions.", Layout = 1, LineStyle = 2, Theme = 4, Palette = 6, Shape = 1 },
            new MapTemplate { Id = "org", Name = "Organigramme", Desc = "Hiérarchie de haut en bas, cases rectangulaires : équipes, structures, processus.", Layout = 2, LineStyle = 2, Theme = 4, Palette = 6, Shape = 2 },
            new MapTemplate { Id = "tree", Name = "Arborescence", Desc = "Liste indentée niveau par niveau, comme un explorateur : plans de documents, dossiers.", Layout = 4, LineStyle = 2, Theme = 4, Palette = 6, Shape = 4 },
            new MapTemplate { Id = "timeline", Name = "Chronologie", Desc = "Étapes le long d'un axe horizontal, détails au-dessus et en dessous : plannings, historiques.", Layout = 5, LineStyle = 1, Theme = 4, Palette = 6, Shape = 3 },
            new MapTemplate { Id = "vtimeline", Name = "Chronologie verticale", Desc = "Étapes de haut en bas le long d'un axe vertical, détails à gauche et à droite.", Layout = 8, LineStyle = 2, Theme = 4, Palette = 6, Shape = 3 },
            new MapTemplate { Id = "fishbone", Name = "Arête de poisson", Desc = "Causes et sous-causes en arêtes vers un effet (Ishikawa) : analyse de problèmes.", Layout = 6, LineStyle = 1, Theme = 4, Palette = 6, Shape = 1 },
            new MapTemplate { Id = "brainstorm", Name = "Remue-méninges", Desc = "Carte bilatérale colorée, sujets en pilules : idées en vrac, à trier ensuite.", Layout = 0, LineStyle = 0, Theme = 2, Shape = 3 },
            new MapTemplate { Id = "project", Name = "Plan de projet", Desc = "Schéma logique pré-rempli : objectifs, équipe, jalons, risques, budget.", Layout = 1, LineStyle = 2, Theme = 4, Palette = 6, Shape = 1, Children = new[] { "Objectifs", "Équipe", "Jalons", "Risques", "Budget" } },
            new MapTemplate { Id = "meeting", Name = "Compte rendu de réunion", Desc = "Pré-rempli, thème clair : ordre du jour, décisions, actions, prochaine réunion.", Layout = 1, LineStyle = 2, Theme = 1, Shape = 1, Children = new[] { "Ordre du jour", "Décisions", "Actions", "Prochaine réunion" } },
            new MapTemplate { Id = "swot", Name = "Forces / faiblesses (SWOT)", Desc = "Carte bilatérale pré-remplie : forces, opportunités, faiblesses, menaces.", Layout = 0, LineStyle = 1, Theme = 4, Palette = 6, Shape = 2, Children = new[] { "Forces", "Opportunités", "Faiblesses", "Menaces" } },
            new MapTemplate { Id = "week", Name = "Semaine", Desc = "Chronologie pré-remplie du lundi au vendredi : organisation de la semaine.", Layout = 5, LineStyle = 1, Theme = 4, Palette = 6, Shape = 3, Children = new[] { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi" } },
            new MapTemplate { Id = "orgup", Name = "Organigramme inversé", Desc = "Hiérarchie de bas en haut : objectifs qui remontent vers une finalité.", Layout = 7, LineStyle = 2, Theme = 4, Palette = 6, Shape = 2 },
        };

        static readonly string[] StructureNames = { "Carte mentale (bilatérale)", "Schéma logique à droite", "Organigramme (vers le bas)", "Schéma logique à gauche", "Arborescence (liste indentée)", "Chronologie horizontale", "Arête de poisson (Ishikawa)", "Organigramme (vers le haut)", "Chronologie verticale" };
        static readonly string[] ShapeNames = { "Celle de la carte", "Arrondi", "Rectangle", "Pilule", "Texte seul", "Ellipse", "Souligné" };
        static readonly string[] MapShapeNames = { "Arrondi", "Rectangle", "Pilule", "Texte seul", "Ellipse", "Souligné" };
        static readonly string[] SizeNames = { "Normale", "Grande", "Petite" };
        static readonly string[] ThemeNames = { "Sombre (verre)", "Clair", "Coloré", "Classique XMind", "Arc-en-ciel XMind" };
        static readonly string[] PaletteNames = { "Pupitre", "Océan", "Forêt", "Crépuscule", "Pastel", "Ardoise", "Arc-en-ciel" };
        static readonly string[] LineNames = { "Courbes", "Droites", "Coudées" };
        static readonly string[] WidthNames = { "Fines", "Normales", "Épaisses" };
        static readonly string[] SpacingNames = { "Compact", "Normal", "Aéré" };
        static readonly string[] NumberNames = { "Aucune", "Chiffres (1, 2, 3)", "Lettres (a, b, c)", "Romains (i, ii, iii)" };
        static readonly string[][] Palettes =
        {
            null,
            new[] { "#1E88E5", "#00ACC1", "#26A69A", "#5C6BC0", "#42A5F5", "#0097A7", "#3949AB", "#039BE5" },
            new[] { "#2E7D32", "#689F38", "#00897B", "#558B2F", "#43A047", "#7CB342", "#26A69A", "#33691E" },
            new[] { "#E53935", "#FB8C00", "#D81B60", "#F4511E", "#8E24AA", "#FFB300", "#C2185B", "#F06292" },
            new[] { "#F28B82", "#FBBC04", "#81C995", "#AECBFA", "#FF8BCB", "#A7FFEB", "#FDD663", "#D7AEFB" },
            new[] { "#607D8B", "#78909C", "#546E7A", "#90A4AE", "#455A64", "#B0BEC5", "#37474F", "#8D9DA6" },
            new[] { "#F0463C", "#F5A33F", "#F3CC2E", "#1FB573", "#3A6BF2", "#4E3FB5", "#E84393", "#15B5C8" },
        };
        const double TreeIndent = 26;

        // ---- marqueurs (groupes XMind : priorité, avancement, drapeaux, étoiles, flèches, symboles, mois, jours) ----

        class MarkerDef { public string Id, Group, Glyph, Color, Name; public bool Chip; public MarkerDef(string id, string group, string glyph, string color, string name, bool chip) { Id = id; Group = group; Glyph = glyph; Color = color; Name = name; Chip = chip; } }

        static readonly string[] MarkerGroups = { "Priorité", "Avancement", "Drapeaux", "Étoiles", "Flèches", "Symboles", "Mois", "Jours" };
        static readonly MarkerDef[] MarkerDefs = BuildMarkers();

        static MarkerDef[] BuildMarkers()
        {
            var l = new List<MarkerDef>();
            string[] pc = { "#E53935", "#FB8C00", "#FDD835", "#43A047", "#1E88E5", "#8E24AA", "#78909C" };
            string[] pg = { "①", "②", "③", "④", "⑤", "⑥", "⑦" };
            for (int i = 0; i < 7; i++) l.Add(new MarkerDef("p" + (i + 1), "Priorité", pg[i], pc[i], "Priorité " + (i + 1), false));
            string[] tg = { "○", "◔", "◑", "◕", "●" }; string[] tn = { "À faire (0 %)", "Entamé (25 %)", "À mi-chemin (50 %)", "Bien avancé (75 %)", "Terminé (100 %)" };
            for (int i = 0; i < 5; i++) l.Add(new MarkerDef("t" + (i * 25), "Avancement", tg[i], i == 4 ? "#43A047" : "#5B8DEF", tn[i], false));
            string[] cn = { "red", "orange", "yellow", "green", "blue", "purple" }; string[] cf = { "rouge", "orange", "jaune", "vert", "bleu", "violet" }; string[] cc = { "#E53935", "#FB8C00", "#FDD835", "#43A047", "#1E88E5", "#8E24AA" };
            for (int i = 0; i < 6; i++) l.Add(new MarkerDef("flag-" + cn[i], "Drapeaux", "⚑", cc[i], "Drapeau " + cf[i], false));
            for (int i = 0; i < 6; i++) l.Add(new MarkerDef("star-" + cn[i], "Étoiles", "★", cc[i], "Étoile " + cf[i], false));
            l.Add(new MarkerDef("arrow-up", "Flèches", "↑", null, "Haut", false));
            l.Add(new MarkerDef("arrow-down", "Flèches", "↓", null, "Bas", false));
            l.Add(new MarkerDef("arrow-left", "Flèches", "←", null, "Gauche", false));
            l.Add(new MarkerDef("arrow-right", "Flèches", "→", null, "Droite", false));
            l.Add(new MarkerDef("arrow-ne", "Flèches", "↗", "#43A047", "En hausse", false));
            l.Add(new MarkerDef("arrow-se", "Flèches", "↘", "#E53935", "En baisse", false));
            l.Add(new MarkerDef("arrow-refresh", "Flèches", "↻", null, "À refaire", false));
            l.Add(new MarkerDef("sym-check", "Symboles", "✔", "#43A047", "Validé", false));
            l.Add(new MarkerDef("sym-cross", "Symboles", "✖", "#E53935", "Refusé", false));
            l.Add(new MarkerDef("sym-excl", "Symboles", "!", "#E53935", "Important", false));
            l.Add(new MarkerDef("sym-question", "Symboles", "?", "#1E88E5", "Question", false));
            l.Add(new MarkerDef("sym-info", "Symboles", "ℹ", "#1E88E5", "Information", false));
            l.Add(new MarkerDef("sym-idea", "Symboles", "💡", null, "Idée", false));
            l.Add(new MarkerDef("sym-warn", "Symboles", "⚠", "#FB8C00", "Attention", false));
            l.Add(new MarkerDef("sym-heart", "Symboles", "♥", "#EC407A", "Favori", false));
            l.Add(new MarkerDef("sym-mail", "Symboles", "✉", null, "Courriel", false));
            l.Add(new MarkerDef("sym-clip", "Symboles", "📎", null, "Pièce jointe", false));
            l.Add(new MarkerDef("sym-lock", "Symboles", "🔒", null, "Confidentiel", false));
            l.Add(new MarkerDef("sym-phone", "Symboles", "☎", null, "Appel", false));
            string[] months = { "janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc." };
            string[] monthNames = { "Janvier", "Février", "Mars", "Avril", "Mai", "Juin", "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre" };
            for (int i = 0; i < 12; i++) l.Add(new MarkerDef("month-" + (i + 1), "Mois", months[i], null, monthNames[i], true));
            string[] days = { "lun.", "mar.", "mer.", "jeu.", "ven.", "sam.", "dim." }; string[] dayNames = { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };
            for (int i = 0; i < 7; i++) l.Add(new MarkerDef("day-" + (i + 1), "Jours", days[i], null, dayNames[i], true));
            return l.ToArray();
        }

        static MarkerDef MarkerById(string id) { return MarkerDefs.FirstOrDefault(d => d.Id == id); }

        // ---- style effectif ---------------------------------------------------------------------------------------------

        int ShapeOf(MindNode n) { return ShapeOf(n, boxes.ContainsKey(n) ? boxes[n].Depth : 1); }
        /// <summary>Forme effective ; dans le thème Classique, les sous-sujets sans forme propre sont en texte souligné (XMind).</summary>
        int ShapeOf(MindNode n, int depth)
        {
            if (n.Shape != 0) return n.Shape;
            if (map.Theme == 3 && depth >= 2 && map.Shape <= 1) return 6;
            return map.Shape != 0 ? map.Shape : 1;
        }
        bool LightTheme { get { return map.Theme == 1 || map.Theme == 3 || map.Theme == 4; } }
        static readonly Color Accent = Color.FromRgb(0x5B, 0x8D, 0xEF);

        Color PaletteColor(int i)
        {
            if (map.Mono) return Accent;
            if (map.Palette <= 0 || map.Palette >= Palettes.Length) return TaskColors.Get(TaskColors.Indices[i % TaskColors.Indices.Length]);
            var p = Palettes[map.Palette];
            return Palette.ParseHex(p[i % p.Length], Accent);
        }

        Color BranchColor(MindNode n, int indexAtLevel1)
        {
            if (n.Color < 0) return Palette.ParseHex(n.CustomColor, Palette.Get(1));
            if (n.Color > 0) return TaskColors.Get(n.Color);
            var p = ParentOf(n);
            if (p == null) return (n == RootNode) ? Accent : PaletteColor(indexAtLevel1);
            if (p == RootNode) return PaletteColor(indexAtLevel1);
            Box pb; return boxes.TryGetValue(p, out pb) ? pb.Color : Accent;
        }

        double NodeFontSize(MindNode n, int depth)
        {
            double b = n.Size == 1 ? 16 : (n.Size == 2 ? 11 : 13);
            if (depth == 0) b += 3;
            return b;
        }

        static Color Lighten(Color c, double f) { return Color.FromRgb((byte)(c.R + (255 - c.R) * f), (byte)(c.G + (255 - c.G) * f), (byte)(c.B + (255 - c.B) * f)); }
        static Color Darken(Color c, double f) { return Color.FromRgb((byte)(c.R * (1 - f)), (byte)(c.G * (1 - f)), (byte)(c.B * (1 - f))); }
        static readonly Color Ink = Color.FromRgb(0x20, 0x24, 0x2C);

        /// <summary>Fond, bordure et texte d'un sujet selon le thème de la carte.</summary>
        void NodeBrushes(Box b, out Brush fill, out Brush border, out Brush text, out double borderWidth)
        {
            var col = b.Color;
            int shape = ShapeOf(b.N, b.Depth);
            bool textOnly = shape == 4 || shape == 6;
            switch (map.Theme)
            {
                case 1: // clair : pastels sans bordure, texte sombre
                    if (b.Depth == 0) { fill = Theme.Brush(0xFF, col); text = Brushes.White; border = Brushes.Transparent; borderWidth = 0; }
                    else if (b.Depth == 1) { fill = Theme.Brush(0xFF, Lighten(col, 0.78)); text = Theme.Brush(0xFF, Darken(col, 0.5)); border = Brushes.Transparent; borderWidth = 0; }
                    else { fill = Brushes.White; text = Theme.Brush(0xFF, Ink); border = Theme.Brush(0xFF, Lighten(col, 0.55)); borderWidth = 1; }
                    break;
                case 3: // classique XMind 2020 : fond blanc, centre sombre, sujets principaux à bordure colorée, sous-sujets soulignés
                    if (b.Depth == 0) { fill = Theme.Brush(0xFF, Color.FromRgb(0x2D, 0x3B, 0x55)); text = Brushes.White; border = Brushes.Transparent; borderWidth = 0; }
                    else if (b.Depth == 1) { fill = Brushes.White; text = Theme.Brush(0xFF, Ink); border = Theme.Brush(0xFF, col); borderWidth = 2; }
                    else { fill = Brushes.White; text = Theme.Brush(0xFF, Ink); border = Theme.Brush(0xFF, col); borderWidth = 1; }
                    break;
                case 4: // arc-en-ciel XMind : centre bleu nuit, sujets principaux en aplats de la branche, sous-sujets en pastel
                    if (b.Depth == 0) { fill = Theme.Brush(0xFF, Color.FromRgb(0x0F, 0x1A, 0x3C)); text = Brushes.White; border = Brushes.Transparent; borderWidth = 0; }
                    else if (b.Depth == 1) { fill = Theme.Brush(0xFF, col); text = Brushes.White; border = Brushes.Transparent; borderWidth = 0; }
                    else { fill = Theme.Brush(0xFF, Lighten(col, 0.8)); text = Theme.Brush(0xFF, Ink); border = Brushes.Transparent; borderWidth = 0; }
                    break;
                case 2: // coloré : aplats
                    fill = Theme.Brush(0xFF, b.Depth == 0 ? Darken(col, 0.25) : (b.Depth == 1 ? col : Lighten(col, 0.22))); text = Brushes.White; border = Brushes.Transparent; borderWidth = 0;
                    if (b.Depth >= 2) text = Theme.Brush(0xFF, Color.FromRgb(0x14, 0x16, 0x1C));
                    break;
                default: // sombre : sujets principaux pleins, secondaires en aplat léger
                    if (b.Depth == 0) { fill = Theme.Brush(0xFF, col); text = Brushes.White; border = Brushes.Transparent; borderWidth = 0; }
                    else if (b.Depth == 1) { fill = Theme.Brush(0xFF, Darken(col, 0.08)); text = Brushes.White; border = Brushes.Transparent; borderWidth = 0; }
                    else { fill = Theme.Brush(0x30, col); text = Theme.Res<Brush>("Text"); border = Brushes.Transparent; borderWidth = 0; }
                    break;
            }
            if (textOnly)
            {
                fill = Brushes.Transparent; borderWidth = shape == 6 ? 2 : 0;
                border = shape == 6 ? Theme.Brush(0xFF, col) : Brushes.Transparent;
                if (map.Theme == 3 || map.Theme == 4) text = Theme.Brush(0xFF, b.Depth == 0 ? Color.FromRgb(0x2D, 0x3B, 0x55) : Ink);
                else if (LightTheme) text = Theme.Brush(0xFF, b.Depth == 0 ? Darken(col, 0.3) : Ink);
                else text = b.Depth == 0 ? Theme.Brush(0xFF, Lighten(col, 0.7)) : (b.Depth == 1 ? Theme.Brush(0xFF, Lighten(col, 0.6)) : Theme.Res<Brush>("Text"));
            }
        }

        Brush LineBrush(Box child) { return Theme.Brush(LightTheme ? (byte)0xFF : (byte)0xD0, LightTheme ? Lighten(child.Color, 0.1) : child.Color); }

        double LineThickness(Box child)
        {
            double w = map.LineWidth == 0 ? 1.2 : (map.LineWidth == 2 ? 3.4 : 2.0);
            if (!map.Tapered) return w;
            return child.Depth <= 1 ? w * 1.8 : (child.Depth == 2 ? w * 1.2 : w * 0.85);
        }

        double SpineThickness { get { return map.LineWidth == 0 ? 2.4 : (map.LineWidth == 2 ? 5 : 3.4); } }

        bool HorizontalEdges { get { return map.Layout == 0 || map.Layout == 1 || map.Layout == 3 || map.Layout == 8; } }
        /// <summary>Arborescence, chronologie, arête : les niveaux profonds sont empilés verticalement avec un retrait.</summary>
        bool StackedLayout { get { return map.Layout == 4 || map.Layout == 5 || map.Layout == 6; } }

        // ---- mesure ------------------------------------------------------------------------------------------------------

        Size MeasureText(string text, double fs, FontWeight weight, FontStyle style, double maxWidth)
        {
            var ft = new FormattedText(text.Length == 0 ? "…" : text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily, style, weight, FontStretches.Normal), fs, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip) { MaxTextWidth = maxWidth, Trimming = TextTrimming.None };
            return new Size(Math.Min(ft.WidthIncludingTrailingWhitespace, maxWidth), ft.Height);
        }

        double ChipWidth(string text) { return MeasureText(text, 9.5, FontWeights.SemiBold, FontStyles.Normal, 120).Width + 10; }

        void MeasureAll()
        {
            boxes.Clear(); parentOf.Clear(); byId.Clear();
            MeasureRec(RootNode, 0, null, false);
            if (focus == null) foreach (var f in map.Floating) MeasureRec(f, 1, null, true);
            // couleurs de branche : chaque sujet principal (et flottant) prend sa teinte, transmise à ses sous-sujets
            int k = 0;
            foreach (var ch in RootNode.Children)
            {
                if (!boxes.ContainsKey(ch)) continue;
                boxes[ch].Color = BranchColor(ch, k); k++;
                foreach (var d in All(ch).Skip(1)) if (boxes.ContainsKey(d)) boxes[d].Color = BranchColor(d, k);
            }
            boxes[RootNode].Color = BranchColor(RootNode, 0);
            if (focus == null)
                foreach (var f in map.Floating)
                {
                    if (!boxes.ContainsKey(f)) continue;
                    boxes[f].Color = BranchColor(f, k); k++;
                    foreach (var d in All(f).Skip(1)) if (boxes.ContainsKey(d)) boxes[d].Color = BranchColor(d, k);
                }
        }

        void MeasureRec(MindNode n, int depth, MindNode parent, bool floating)
        {
            if (parent != null) parentOf[n] = parent;
            byId[n.Id] = n;
            double fs = NodeFontSize(n, depth);
            var ts = MeasureText(DisplayText(n), fs, (n.Bold || depth == 0) ? FontWeights.SemiBold : FontWeights.Normal, n.Italic ? FontStyles.Italic : FontStyles.Normal, 240);
            int shape = ShapeOf(n, depth);
            bool textOnly = shape == 4 || shape == 6;
            double padX = textOnly ? 4 : (depth == 0 ? 18 : 12), padY = textOnly ? 2 : (depth == 0 ? 10 : 6);
            if (shape == 5) padX += 10;
            double extra = 0;
            if (n.Todo != 0) extra += fs + 6;
            foreach (var id in n.Markers) { var d = MarkerById(id); if (d != null) extra += d.Chip ? ChipWidth(d.Glyph) + 4 : fs + 6; }
            if (!string.IsNullOrWhiteSpace(n.Link)) extra += 17;
            if (!string.IsNullOrWhiteSpace(n.Note)) extra += 14;
            double rowW = ts.Width + extra, labelsW = 0, labelsH = 0;
            if (n.Labels.Count > 0) { foreach (var l in n.Labels) labelsW += ChipWidth(l) + 4; labelsW -= 4; labelsH = 18; }
            var b = new Box { N = n, Depth = depth, Floating = floating, W = Math.Max(Math.Max(rowW, labelsW) + padX * 2, depth == 0 ? 90 : 40), H = ts.Height + (labelsH > 0 ? labelsH + 3 : 0) + padY * 2, TextW = ts.Width, Extra = extra, PadX = padX, PadY = padY, LabelsH = labelsH, MinW = depth == 0 ? 90 : 40 };
            if (editing == n) b.W = Math.Max(b.W, 60 + extra + padX * 2);
            boxes[n] = b;
            if (!n.Collapsed) foreach (var ch in n.Children) MeasureRec(ch, depth + 1, n, floating);
            else foreach (var ch in n.Children) RegisterHidden(ch, n);
        }

        void RegisterHidden(MindNode n, MindNode parent) { parentOf[n] = parent; foreach (var ch in n.Children) RegisterHidden(ch, n); }

        static string Alpha(int i) { string s = ""; while (i > 0) { i--; s = (char)('a' + i % 26) + s; i /= 26; } return s; }
        static string Roman(int i)
        {
            int[] v = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 }; string[] r = { "m", "cm", "d", "cd", "c", "xc", "l", "xl", "x", "ix", "v", "iv", "i" };
            var sb = new StringBuilder();
            for (int k = 0; k < v.Length && i > 0; k++) while (i >= v[k]) { sb.Append(r[k]); i -= v[k]; }
            return sb.ToString();
        }

        /// <summary>Numéro d'un sujet selon le style de la feuille (vide si non numéroté).</summary>
        string NumberLabel(MindNode n)
        {
            if (map.NumberStyle == 0 || n == RootNode) return "";
            var p = ParentOf(n);
            if (p == null) return "";
            int i = p.Children.IndexOf(n) + 1;
            string s = map.NumberStyle == 2 ? Alpha(i) : (map.NumberStyle == 3 ? Roman(i) : i.ToString());
            if (map.Tiered && p != RootNode) { string pp = NumberLabel(p); if (pp.Length > 0) s = pp + "." + s; }
            return s;
        }

        string DisplayText(MindNode n)
        {
            if (editing == n && editor != null) return editor.Text.Length == 0 ? "Nouveau sujet" : editor.Text;
            string t = n.Text ?? "";
            string num = NumberLabel(n);
            if (num.Length > 0) t = num + (num.Contains(".") ? "  " : ". ") + t;
            return t;
        }

        // ---- disposition -------------------------------------------------------------------------------------------------

        double GapH { get { return map.Spacing == 0 ? 34 : (map.Spacing == 2 ? 76 : 52); } }
        double GapV { get { return map.Spacing == 0 ? 8 : (map.Spacing == 2 ? 26 : 14); } }

        void PlaceAll()
        {
            var root = boxes[RootNode];
            root.X = -root.W / 2; root.Y = -root.H / 2; root.Side = 0;
            if (!RootNode.Collapsed)
                switch (map.Layout)
                {
                    case 2: PlaceVertical(RootNode, 1); break;
                    case 7: PlaceVertical(RootNode, -1); break;
                    case 4: { double y = root.Y + root.H + GapV * 1.5; PlaceStack(RootNode, root.X + TreeIndent, ref y, 1); break; }
                    case 5: PlaceTimeline(); break;
                    case 6: PlaceFishbone(); break;
                    case 8: PlaceVerticalTimeline(); break;
                    default:
                        {
                            var right = new List<MindNode>(); var left = new List<MindNode>();
                            for (int i = 0; i < RootNode.Children.Count; i++)
                            {
                                var ch = RootNode.Children[i];
                                bool goLeft = map.Layout == 3 || (map.Layout == 0 && (ch.Side == -1 || (ch.Side == 0 && i % 2 == 1)));
                                (goLeft ? left : right).Add(ch);
                            }
                            PlaceSide(right, 1); PlaceSide(left, -1);
                            break;
                        }
                }
            if (focus == null)
                foreach (var f in map.Floating)
                {
                    Box fb; if (!boxes.TryGetValue(f, out fb)) continue;
                    PlaceBranch(f, 1, f.X, f.Y + fb.H / 2);
                }
        }

        double SubHeight(MindNode n)
        {
            var b = boxes[n];
            if (n.Collapsed || n.Children.Count == 0) return b.H;
            double total = 0;
            foreach (var ch in n.Children) total += SubHeight(ch);
            total += GapV * (n.Children.Count - 1);
            return Math.Max(b.H, total);
        }

        double SubWidth(MindNode n)
        {
            var b = boxes[n];
            if (n.Collapsed || n.Children.Count == 0) return b.W;
            double total = 0;
            foreach (var ch in n.Children) total += SubWidth(ch);
            total += GapV * 2 * (n.Children.Count - 1);
            return Math.Max(b.W, total);
        }

        void PlaceSide(List<MindNode> items, int side)
        {
            var root = boxes[RootNode];
            double total = 0;
            foreach (var ch in items) total += SubHeight(ch);
            total += GapV * Math.Max(0, items.Count - 1);
            double y = -total / 2;
            foreach (var ch in items)
            {
                double sh = SubHeight(ch);
                PlaceBranch(ch, side, side > 0 ? root.X + root.W + GapH : root.X - GapH, y + sh / 2);
                y += sh + GapV;
            }
        }

        void PlaceBranch(MindNode n, int side, double x, double centerY)
        {
            var b = boxes[n];
            b.Side = side;
            b.X = side > 0 ? x : x - b.W;
            b.Y = centerY - b.H / 2;
            if (n.Collapsed || n.Children.Count == 0) return;
            double total = 0;
            foreach (var ch in n.Children) total += SubHeight(ch);
            total += GapV * (n.Children.Count - 1);
            double y = centerY - total / 2;
            foreach (var ch in n.Children)
            {
                double sh = SubHeight(ch);
                PlaceBranch(ch, side, side > 0 ? b.X + b.W + GapH : b.X - GapH, y + sh / 2);
                y += sh + GapV;
            }
        }

        /// <summary>Organigramme : enfants en ligne sous (dir +1) ou au-dessus (dir -1) du parent.</summary>
        void PlaceVertical(MindNode n, int dir)
        {
            var b = boxes[n];
            b.Side = 0;
            if (n.Collapsed || n.Children.Count == 0) return;
            double total = 0;
            foreach (var ch in n.Children) total += SubWidth(ch);
            total += GapV * 2 * (n.Children.Count - 1);
            double x = b.X + b.W / 2 - total / 2;
            foreach (var ch in n.Children)
            {
                double sw = SubWidth(ch);
                var cb = boxes[ch];
                cb.X = x + sw / 2 - cb.W / 2;
                cb.Y = dir > 0 ? b.Y + b.H + GapH * 0.8 : b.Y - GapH * 0.8 - cb.H;
                PlaceVertical(ch, dir);
                x += sw + GapV * 2;
            }
        }

        /// <summary>Empile les sous-sujets de parent verticalement (dir : +1 vers le bas, -1 vers le haut) avec un retrait par niveau.</summary>
        void PlaceStack(MindNode parent, double x, ref double y, int dir)
        {
            if (parent.Collapsed) return;
            foreach (var ch in parent.Children)
            {
                var b = boxes[ch]; b.Side = dir; b.X = x;
                if (dir > 0) { b.Y = y; y += b.H + GapV; }
                else { y -= b.H; b.Y = y; y -= GapV; }
                PlaceStack(ch, x + TreeIndent, ref y, dir);
            }
        }

        double StackWidth(MindNode n, int depth)
        {
            double w = depth * TreeIndent + boxes[n].W;
            if (!n.Collapsed) foreach (var ch in n.Children) w = Math.Max(w, StackWidth(ch, depth + 1));
            return w;
        }

        void PlaceTimeline()
        {
            var root = boxes[RootNode];
            double x = root.X + root.W + GapH, stem = GapV * 2 + 12;
            for (int i = 0; i < RootNode.Children.Count; i++)
            {
                var ch = RootNode.Children[i]; var b = boxes[ch];
                int side = ch.Side != 0 ? ch.Side : (i % 2 == 0 ? 1 : -1);
                b.Side = side; b.X = x;
                if (side > 0) { b.Y = -stem - b.H; double y = b.Y - GapV; PlaceStack(ch, b.X + TreeIndent, ref y, -1); }
                else { b.Y = stem; double y = b.Y + b.H + GapV; PlaceStack(ch, b.X + TreeIndent, ref y, 1); }
                x += StackWidth(ch, 0) + GapH;
            }
        }

        void PlaceFishbone()
        {
            var root = boxes[RootNode];
            double x = root.X - GapH, rise = GapH * 1.3;
            for (int i = 0; i < RootNode.Children.Count; i++)
            {
                var ch = RootNode.Children[i]; var b = boxes[ch];
                int side = ch.Side != 0 ? ch.Side : (i % 2 == 0 ? 1 : -1);
                x -= StackWidth(ch, 0) + GapH;
                b.Side = side; b.X = x;
                if (side > 0) { b.Y = -rise - b.H; double y = b.Y - GapV; PlaceStack(ch, b.X + TreeIndent, ref y, -1); }
                else { b.Y = rise; double y = b.Y + b.H + GapV; PlaceStack(ch, b.X + TreeIndent, ref y, 1); }
            }
        }

        /// <summary>Chronologie verticale : axe sous le sujet central, étapes alternées à droite et à gauche, détails en branches horizontales.</summary>
        void PlaceVerticalTimeline()
        {
            var root = boxes[RootNode];
            double spineX = root.X + root.W / 2, y = root.Y + root.H + GapH * 0.8;
            for (int i = 0; i < RootNode.Children.Count; i++)
            {
                var ch = RootNode.Children[i];
                int side = ch.Side != 0 ? ch.Side : (i % 2 == 0 ? 1 : -1);
                double sh = SubHeight(ch);
                PlaceBranch(ch, side, side > 0 ? spineX + 28 : spineX - 28, y + sh / 2);
                y += sh + GapV;
            }
        }

        /// <summary>Où placer le « + » d'un sous-sujet, celui d'un sujet frère et la pastille de repli : 0 droite, 1 gauche, 2 bas, 3 haut.</summary>
        void Sides(Box b, out int child, out int sib)
        {
            sib = 2;
            if (b.Floating) { child = 0; return; }
            switch (map.Layout)
            {
                case 2: child = 2; sib = 0; break;
                case 7: child = 3; sib = 0; break;
                case 8: child = b.Depth == 0 ? 2 : (b.Side < 0 ? 1 : 0); break;
                case 4: case 5: child = 0; break;
                case 6: child = b.Depth == 0 ? 1 : 0; break;
                default: child = b.Depth == 0 ? (map.Layout == 3 ? 1 : 0) : (b.Side < 0 ? 1 : 0); break;
            }
        }

        static void PlaceBadge(FrameworkElement el, int side)
        {
            double off = el.Width / 2 + 3;
            el.HorizontalAlignment = side == 0 ? HorizontalAlignment.Right : (side == 1 ? HorizontalAlignment.Left : HorizontalAlignment.Center);
            el.VerticalAlignment = side == 2 ? VerticalAlignment.Bottom : (side == 3 ? VerticalAlignment.Top : VerticalAlignment.Center);
            el.Margin = side == 0 ? new Thickness(0, 0, -off, 0) : (side == 1 ? new Thickness(-off, 0, 0, 0) : (side == 2 ? new Thickness(0, 0, 0, -off) : new Thickness(0, -off, 0, 0)));
        }

        // ---- rendu ------------------------------------------------------------------------------------------------------

        void Render()
        {
            MeasureAll();
            PlaceAll();
            Draw(canvas, true);
            canvasBg.Background = LightTheme ? Theme.Brush(0xFF, Color.FromRgb(0xF6, 0xF7, 0xF9)) : Brushes.Transparent;
            gridBg.Background = DotGrid(LightTheme ? Theme.Argb(0x16000000) : Theme.Argb(0x14FFFFFF));
            if (relFrom == null)
            {
                hint.Text = EmptyHint;
                hint.Visibility = RootNode.Children.Count == 0 && (focus != null || map.Floating.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
            }
            hint.Foreground = LightTheme ? Theme.Brush(0xFF, Color.FromRgb(0x94, 0xA3, 0xB8)) : Theme.Res<Brush>("TextFaint");
            focusBtn.Visibility = focus != null ? Visibility.Visible : Visibility.Collapsed;
            UpdateFooter();
            RefreshStylePanel();
            PlaceToolbar();
        }

        void UpdateFooter()
        {
            int n = AllNodes().Count();
            mapBtn.Content = map.Name + " ▾";
            string s = n + " sujet" + (n > 1 ? "s" : "") + "  ·  " + (int)Math.Round(scale.ScaleX * 100) + " %";
            if (store.Maps.Count > 1) s += "  ·  feuille " + (store.Maps.IndexOf(map) + 1) + "/" + store.Maps.Count;
            if (focus != null) s += "  ·  branche « " + Label(focus) + " » seule";
            if (searchText.Length > 0) s += "  ·  " + searchHits.Count + " résultat" + (searchHits.Count > 1 ? "s" : "") + " pour « " + searchText + " »";
            footer.Text = s;
        }

        Rect SubtreeRect(MindNode n)
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var d in All(n)) { Box b; if (!boxes.TryGetValue(d, out b)) continue; minX = Math.Min(minX, b.X); minY = Math.Min(minY, b.Y); maxX = Math.Max(maxX, b.X + b.W); maxY = Math.Max(maxY, b.Y + b.H); }
            return minX == double.MaxValue ? Rect.Empty : new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>Dessine la carte dans un canevas (le panneau, ou un canevas hors écran pour l'export).</summary>
        void Draw(Canvas target, bool interactive)
        {
            target.Children.Clear();
            if (interactive) views.Clear();
            // limites (derrière tout)
            foreach (var b in boxes.Values.Where(x => x.N.Boundary && x.Depth > 0 || (x.N.Boundary && x.Floating)).ToList()) DrawBoundary(target, b);
            // axe de la chronologie / colonne vertébrale de l'arête
            if ((map.Layout == 5 || map.Layout == 6 || map.Layout == 8) && !RootNode.Collapsed && RootNode.Children.Count > 0)
            {
                var root = boxes[RootNode];
                var main = RootNode.Children.Where(ch => boxes.ContainsKey(ch)).Select(ch => boxes[ch]).ToList();
                if (main.Count > 0)
                {
                    Line spine;
                    if (map.Layout == 5) spine = new Line { X1 = root.X + root.W, Y1 = root.Y + root.H / 2, X2 = main.Max(b => b.X + Math.Min(24, b.W / 2)) + 24, Y2 = root.Y + root.H / 2 };
                    else if (map.Layout == 6) spine = new Line { X1 = main.Min(b => b.X + b.W / 2) - 24, Y1 = root.Y + root.H / 2, X2 = root.X, Y2 = root.Y + root.H / 2 };
                    else spine = new Line { X1 = root.X + root.W / 2, Y1 = root.Y + root.H, X2 = root.X + root.W / 2, Y2 = main.Max(b => b.Y + b.H / 2) + 24 };
                    spine.Stroke = Theme.Brush(0xE0, root.Color); spine.StrokeThickness = SpineThickness; spine.StrokeStartLineCap = PenLineCap.Round; spine.StrokeEndLineCap = PenLineCap.Round; spine.IsHitTestVisible = false;
                    target.Children.Add(spine);
                }
            }
            foreach (var b in boxes.Values)
            {
                var p = ParentOf(b.N);
                if (p == null || !boxes.ContainsKey(p)) continue;
                target.Children.Add(Edge(boxes[p], b));
            }
            foreach (var b in boxes.Values.Where(x => !string.IsNullOrWhiteSpace(x.N.Summary) && !x.N.Collapsed && x.N.Children.Count > 0).ToList()) DrawSummary(target, b);
            foreach (var b in boxes.Values.OrderBy(x => x.Depth))
            {
                var v = NodeView(b, interactive);
                Canvas.SetLeft(v, b.X); Canvas.SetTop(v, b.Y);
                target.Children.Add(v);
                if (interactive) views[b.N] = v;
            }
            foreach (var b in boxes.Values.Where(x => !string.IsNullOrWhiteSpace(x.N.Callout)).ToList()) DrawCallout(target, b);
            foreach (var r in map.Relations) DrawRelation(target, r);
        }

        Path Edge(Box pb, Box cb)
        {
            var path = new Path { Stroke = LineBrush(cb), StrokeThickness = LineThickness(cb), StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round, IsHitTestVisible = false };
            var g = new StreamGeometry();
            using (var ctx = g.Open())
            {
                if (cb.Floating) EdgeHorizontal(ctx, pb, cb);
                else if (map.Layout == 2) Curve(ctx, pb.X + pb.W / 2, pb.Y + pb.H, cb.X + cb.W / 2, cb.Y, true);
                else if (map.Layout == 7) Curve(ctx, pb.X + pb.W / 2, pb.Y, cb.X + cb.W / 2, cb.Y + cb.H, true);
                else if (map.Layout == 5 && pb.Depth == 0) EdgeStem(ctx, pb, cb);
                else if (map.Layout == 8 && pb.Depth == 0) EdgeStemH(ctx, pb, cb);
                else if (map.Layout == 6 && pb.Depth == 0) EdgeBone(ctx, pb, cb);
                else if (StackedLayout) EdgeStack(ctx, pb, cb);
                else EdgeHorizontal(ctx, pb, cb);
            }
            g.Freeze();
            path.Data = g;
            if (pb.Depth == 0 && HorizontalEdges) Panel.SetZIndex(path, -1);
            return path;
        }

        void Curve(StreamGeometryContext ctx, double x0, double y0, double x1, double y1, bool vertical)
        {
            ctx.BeginFigure(new Point(x0, y0), false, false);
            if (map.LineStyle == 1) ctx.LineTo(new Point(x1, y1), true, false);
            else if (map.LineStyle == 2)
            {
                if (vertical) { double my = (y0 + y1) / 2; ctx.LineTo(new Point(x0, my), true, false); ctx.LineTo(new Point(x1, my), true, false); ctx.LineTo(new Point(x1, y1), true, false); }
                else { double mx = (x0 + x1) / 2; ctx.LineTo(new Point(mx, y0), true, false); ctx.LineTo(new Point(mx, y1), true, false); ctx.LineTo(new Point(x1, y1), true, false); }
            }
            else
            {
                if (vertical) ctx.BezierTo(new Point(x0, (y0 + y1) / 2), new Point(x1, (y0 + y1) / 2), new Point(x1, y1), true, false);
                else ctx.BezierTo(new Point((x0 + x1) / 2, y0), new Point((x0 + x1) / 2, y1), new Point(x1, y1), true, false);
            }
        }

        void EdgeHorizontal(StreamGeometryContext ctx, Box pb, Box cb)
        {
            double x0, y0, x1, y1;
            if (cb.Side >= 0) { x0 = pb.X + pb.W; y0 = pb.Y + pb.H / 2; x1 = cb.X; y1 = cb.Y + cb.H / 2; }
            else { x0 = pb.X; y0 = pb.Y + pb.H / 2; x1 = cb.X + cb.W; y1 = cb.Y + cb.H / 2; }
            if (pb.Depth == 0 && !pb.Floating) { x0 = pb.X + pb.W / 2; y0 = pb.Y + pb.H / 2; }
            Curve(ctx, x0, y0, x1, y1, false);
        }

        /// <summary>Niveaux empilés : coude depuis le bord bas (ou haut) du parent jusqu'au bord gauche de l'enfant.</summary>
        static void EdgeStack(StreamGeometryContext ctx, Box pb, Box cb)
        {
            double x0 = pb.X + Math.Min(12, pb.W / 2), y0 = cb.Side >= 0 ? pb.Y + pb.H : pb.Y;
            double y1 = cb.Y + cb.H / 2;
            ctx.BeginFigure(new Point(x0, y0), false, false);
            ctx.LineTo(new Point(x0, y1), true, false);
            ctx.LineTo(new Point(cb.X, y1), true, false);
        }

        /// <summary>Chronologie horizontale : tige verticale entre l'axe et l'étape.</summary>
        static void EdgeStem(StreamGeometryContext ctx, Box pb, Box cb)
        {
            double x = cb.X + Math.Min(24, cb.W / 2);
            double y0 = pb.Y + pb.H / 2, y1 = cb.Side > 0 ? cb.Y + cb.H : cb.Y;
            ctx.BeginFigure(new Point(x, y0), false, false);
            ctx.LineTo(new Point(x, y1), true, false);
        }

        /// <summary>Chronologie verticale : tige horizontale entre l'axe et l'étape.</summary>
        static void EdgeStemH(StreamGeometryContext ctx, Box pb, Box cb)
        {
            double x0 = pb.X + pb.W / 2, y = cb.Y + cb.H / 2;
            double x1 = cb.Side >= 0 ? cb.X : cb.X + cb.W;
            ctx.BeginFigure(new Point(x0, y), false, false);
            ctx.LineTo(new Point(x1, y), true, false);
        }

        /// <summary>Arête de poisson : diagonale de la cause vers l'axe, inclinée vers la tête.</summary>
        static void EdgeBone(StreamGeometryContext ctx, Box pb, Box cb)
        {
            double x0 = cb.X + cb.W / 2, y0 = cb.Side > 0 ? cb.Y + cb.H : cb.Y;
            double y1 = pb.Y + pb.H / 2, x1 = x0 + Math.Abs(y1 - y0) * 0.6;
            ctx.BeginFigure(new Point(x0, y0), false, false);
            ctx.LineTo(new Point(x1, y1), true, false);
        }

        Border NodeView(Box b, bool interactive)
        {
            Brush fill, border, text; double bw;
            NodeBrushes(b, out fill, out border, out text, out bw);
            var n = b.N;
            bool sel = interactive && n == selected, hit = interactive && searchHits.Contains(n);
            int shape = ShapeOf(n, b.Depth);
            var outer = new Border
            {
                Width = b.W,
                Height = b.H,
                Background = fill,
                BorderBrush = sel ? Theme.Res<Brush>("Accent") : (hit ? Theme.Res<Brush>("Warn") : border),
                BorderThickness = shape == 6 ? new Thickness(0, 0, 0, sel || hit ? 3 : 2) : new Thickness(sel || hit ? Math.Max(2, bw) : bw),
                CornerRadius = shape == 2 ? new CornerRadius(4) : (shape == 3 ? new CornerRadius(b.H / 2) : (shape == 5 ? new CornerRadius(b.H) : (shape == 4 || shape == 6 ? new CornerRadius(0) : new CornerRadius(b.Depth == 0 ? 12 : 9)))),
                SnapsToDevicePixels = true
            };
            if (sel) outer.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 14, ShadowDepth = 0, Opacity = 0.7, Color = Accent };
            else if (b.Depth == 0) outer.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 16, ShadowDepth = 3, Opacity = 0.35, Color = Colors.Black };
            double fs = NodeFontSize(n, b.Depth);
            var col = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            if (n.Todo != 0)
                row.Children.Add(new TextBlock { Text = n.Todo == 2 ? "☑" : "☐", FontSize = fs + 1, Foreground = n.Todo == 2 ? Theme.Res<Brush>("Ok") : text, Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center, Tag = "todo", DataContext = n, Cursor = Cursors.Hand, ToolTip = n.Todo == 2 ? "Fait — cliquer pour remettre à faire" : "À faire — cliquer pour marquer fait" });
            foreach (var id in n.Markers)
            {
                var d = MarkerById(id);
                if (d == null) continue;
                if (d.Chip) row.Children.Add(new Border { CornerRadius = new CornerRadius(4), Background = Theme.Brush(0x55, b.Color), BorderBrush = Theme.Brush(0xAA, b.Color), BorderThickness = new Thickness(1), Padding = new Thickness(4, 0, 4, 1), Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center, ToolTip = d.Name, Child = new TextBlock { Text = d.Glyph, FontSize = 9.5, FontWeight = FontWeights.SemiBold, Foreground = text } });
                else row.Children.Add(new TextBlock { Text = d.Glyph, FontSize = fs, Foreground = d.Color != null ? Theme.Brush(0xFF, Palette.ParseHex(d.Color, Accent)) : text, Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center, ToolTip = d.Group + " : " + d.Name });
            }
            string label = DisplayText(n);
            if (interactive && editing == n)
            {
                // saisie dans le sujet : la zone de texte prend la police et la couleur du sujet
                var host = editor.Parent as Panel;
                if (host != null) host.Children.Remove(editor);
                editor.FontSize = fs; editor.FontWeight = (n.Bold || b.Depth == 0) ? FontWeights.SemiBold : FontWeights.Normal; editor.FontStyle = n.Italic ? FontStyles.Italic : FontStyles.Normal;
                editor.Foreground = text; editor.CaretBrush = text; editor.SelectionBrush = Theme.Brush(0x66, Accent);
                editor.MinWidth = Math.Max(40, b.TextW + 2);
                var ph = new TextBlock { Text = "Nouveau sujet", FontSize = fs, Foreground = text, Opacity = 0.4, IsHitTestVisible = false, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Visibility = editor.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed };
                var g = new Grid { VerticalAlignment = VerticalAlignment.Center };
                g.Children.Add(ph); g.Children.Add(editor);
                row.Children.Add(g);
                outer.Tag = ph;
            }
            else
            {
                var tb = new TextBlock { Text = label.Length == 0 ? "…" : label, FontSize = fs, FontWeight = (n.Bold || b.Depth == 0) ? FontWeights.SemiBold : FontWeights.Normal, FontStyle = n.Italic ? FontStyles.Italic : FontStyles.Normal, Foreground = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 240, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Opacity = label.Length == 0 ? 0.45 : 1 };
                if (n.Strike || n.Todo == 2) tb.TextDecorations = TextDecorations.Strikethrough;
                row.Children.Add(tb);
            }
            if (!string.IsNullOrWhiteSpace(n.Link)) row.Children.Add(new TextBlock { Text = "🔗", FontSize = fs - 2, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Tag = "link", DataContext = n, Cursor = Cursors.Hand, ToolTip = "Ouvrir : " + n.Link });
            if (!string.IsNullOrWhiteSpace(n.Note)) row.Children.Add(new TextBlock { Text = "≡", FontSize = fs - 1, Foreground = text, Opacity = 0.75, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Tag = "note", DataContext = n, Cursor = Cursors.Hand, ToolTip = n.Note });
            col.Children.Add(row);
            if (n.Labels.Count > 0)
            {
                var chips = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 3, 0, 0) };
                foreach (var l in n.Labels)
                    chips.Children.Add(new Border { CornerRadius = new CornerRadius(7), Background = Theme.Brush(LightTheme ? (byte)0x22 : (byte)0x44, b.Color), BorderBrush = Theme.Brush(0x99, b.Color), BorderThickness = new Thickness(1), Padding = new Thickness(5, 0, 5, 1), Margin = new Thickness(0, 0, 4, 0), Child = new TextBlock { Text = l, FontSize = 9.5, FontWeight = FontWeights.SemiBold, Foreground = text } });
                col.Children.Add(chips);
            }
            outer.Child = col;

            var wrap = new Border { Tag = "node", DataContext = n, Background = Brushes.Transparent, Width = b.W, Height = b.H, Cursor = interactive ? Cursors.Hand : Cursors.Arrow };
            if (!string.IsNullOrWhiteSpace(n.Note) && string.IsNullOrWhiteSpace(n.Link)) wrap.ToolTip = n.Note;
            if (interactive && editing == n) { editOuter = outer; editWrap = wrap; editBox = b; }
            if (interactive && dragging && n == dragNode) wrap.Opacity = 0.45;
            if (interactive && !sel && !hit && editing != n)
            {
                // survol : contour discret, comme dans MindNode ou Whimsical
                var normal = outer.BorderBrush; var normalW = outer.BorderThickness;
                wrap.MouseEnter += delegate { if (!dragging) { outer.BorderBrush = Theme.Brush(0x99, Accent); outer.BorderThickness = new Thickness(Math.Max(1.5, normalW.Left)); } };
                wrap.MouseLeave += delegate { outer.BorderBrush = normal; outer.BorderThickness = normalW; };
            }
            var dock = new Grid();
            dock.Children.Add(outer);
            int childSide, sibSide;
            Sides(b, out childSide, out sibSide);
            if (n.Collapsed && n.Children.Count > 0)
            {
                var badge = CollapsedBadge(n, b);
                PlaceBadge(badge, childSide);
                dock.Children.Add(badge);
            }
            else if (sel && editing != n)
            {
                // « + » du sujet sélectionné : sous-sujet sur le côté extérieur, sujet frère en dessous (comme XMind)
                if (n == RootNode && !b.Floating && (map.Layout == 0 || map.Layout == 5 || map.Layout == 6 || map.Layout == 8))
                {
                    // sujet central : un « + » de chaque côté, le sujet est créé du côté cliqué (XMind)
                    bool upDown = map.Layout == 5 || map.Layout == 6;
                    var a1 = AddButton(n, b, "add", upDown ? "Ajouter un sujet au-dessus de l'axe" : "Ajouter un sujet à droite  (Tab : côté le moins chargé)"); a1.Uid = "1"; PlaceBadge(a1, upDown ? 3 : 0); dock.Children.Add(a1);
                    var a2 = AddButton(n, b, "add", upDown ? "Ajouter un sujet en dessous de l'axe" : "Ajouter un sujet à gauche"); a2.Uid = "-1"; PlaceBadge(a2, upDown ? 2 : 1); dock.Children.Add(a2);
                }
                else
                {
                    var add = AddButton(n, b, "add", "Ajouter un sous-sujet  (Tab)");
                    PlaceBadge(add, childSide);
                    dock.Children.Add(add);
                }
                if (n != map.Root && ParentOf(n) != null)
                {
                    var sib = AddButton(n, b, "addsib", "Ajouter un sujet frère  (Entrée)");
                    PlaceBadge(sib, sibSide);
                    dock.Children.Add(sib);
                }
            }
            wrap.Child = dock;
            return wrap;
        }

        /// <summary>Petit « + » rond au bord du sujet sélectionné.</summary>
        static Border AddButton(MindNode n, Box b, string tag, string tip)
        {
            var add = new Border { Width = 18, Height = 18, CornerRadius = new CornerRadius(9), Background = Theme.Brush(0xFF, b.Color), BorderBrush = Brushes.White, BorderThickness = new Thickness(1.2), Tag = tag, DataContext = n, Cursor = Cursors.Hand, ToolTip = tip, Opacity = tag == "add" ? 1 : 0.8 };
            add.Child = new TextBlock { Text = "+", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, -2, 0, 0) };
            return add;
        }

        /// <summary>Pastille « +n » d'un sujet replié : clic pour déplier.</summary>
        static Border CollapsedBadge(MindNode n, Box b)
        {
            var badge = new Border { Width = 20, Height = 20, CornerRadius = new CornerRadius(10), Background = Theme.Brush(0xFF, b.Color), BorderBrush = Brushes.White, BorderThickness = new Thickness(1), Tag = "expand", DataContext = n, Cursor = Cursors.Hand, ToolTip = "Déplier (" + (All(n).Count() - 1) + " sous-sujet(s))  (Ctrl+/)" };
            badge.Child = new TextBlock { Text = "+" + n.Children.Count, FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            return badge;
        }

        void DrawBoundary(Canvas target, Box b)
        {
            var r = SubtreeRect(b.N);
            if (r.IsEmpty) return;
            bool labeled = !string.IsNullOrWhiteSpace(b.N.BoundaryLabel);
            r.Inflate(10, 10);
            if (labeled) { r.Y -= 8; r.Height += 8; }
            var rect = new Rectangle { Width = r.Width, Height = r.Height, RadiusX = 12, RadiusY = 12, Fill = Theme.Brush(0x16, b.Color), Stroke = Theme.Brush(0xB0, b.Color), StrokeThickness = 1.2, StrokeDashArray = new DoubleCollection { 5, 3 }, Tag = "boundary", DataContext = b.N, Cursor = Cursors.Hand };
            Canvas.SetLeft(rect, r.X); Canvas.SetTop(rect, r.Y);
            target.Children.Add(rect);
            if (labeled)
            {
                var lbl = new Border { Background = Theme.Brush(0xFF, b.Color), CornerRadius = new CornerRadius(6), Padding = new Thickness(6, 1, 6, 2), Tag = "boundary", DataContext = b.N, Cursor = Cursors.Hand, ToolTip = "Double-clic pour modifier le texte de la limite", Child = new TextBlock { Text = b.N.BoundaryLabel, FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White } };
                Canvas.SetLeft(lbl, r.X + 10); Canvas.SetTop(lbl, r.Y - 9);
                target.Children.Add(lbl);
            }
        }

        void DrawSummary(Canvas target, Box b)
        {
            var kids = b.N.Children.Where(boxes.ContainsKey).Select(ch => boxes[ch]).ToList();
            if (kids.Count == 0) return;
            double minX = kids.Min(k => k.X), maxX = kids.Max(k => k.X + k.W), minY = kids.Min(k => k.Y), maxY = kids.Max(k => k.Y + k.H);
            var stroke = Theme.Brush(0xD0, b.Color);
            var sz = MeasureText(b.N.Summary, 11, FontWeights.SemiBold, FontStyles.Normal, 160);
            var path = new Path { Stroke = stroke, StrokeThickness = 1.6, StrokeLineJoin = PenLineJoin.Round, IsHitTestVisible = false };
            var g = new StreamGeometry();
            double lx, ly;
            bool vertical = map.Layout == 2 || map.Layout == 7;
            using (var ctx = g.Open())
            {
                if (!vertical)
                {
                    int s = (StackedLayout || b.Floating) ? 1 : (kids[0].Side < 0 ? -1 : 1);
                    double x = s > 0 ? maxX + 8 : minX - 8, mid = (minY + maxY) / 2;
                    ctx.BeginFigure(new Point(x, minY), false, false); ctx.LineTo(new Point(x + 8 * s, minY), true, false); ctx.LineTo(new Point(x + 8 * s, maxY), true, false); ctx.LineTo(new Point(x, maxY), true, false);
                    ctx.BeginFigure(new Point(x + 8 * s, mid), false, false); ctx.LineTo(new Point(x + 16 * s, mid), true, false);
                    lx = s > 0 ? x + 20 : x - 20 - (sz.Width + 14); ly = mid - (sz.Height + 6) / 2;
                }
                else
                {
                    int s = map.Layout == 7 ? -1 : 1;
                    double y = s > 0 ? maxY + 8 : minY - 8, mid = (minX + maxX) / 2;
                    ctx.BeginFigure(new Point(minX, y), false, false); ctx.LineTo(new Point(minX, y + 8 * s), true, false); ctx.LineTo(new Point(maxX, y + 8 * s), true, false); ctx.LineTo(new Point(maxX, y), true, false);
                    ctx.BeginFigure(new Point(mid, y + 8 * s), false, false); ctx.LineTo(new Point(mid, y + 16 * s), true, false);
                    lx = mid - (sz.Width + 14) / 2; ly = s > 0 ? y + 20 : y - 20 - (sz.Height + 6);
                }
            }
            g.Freeze(); path.Data = g;
            target.Children.Add(path);
            var lbl = new Border { Background = Theme.Brush(0x33, b.Color), BorderBrush = stroke, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(6, 2, 6, 2), Tag = "summary", DataContext = b.N, Cursor = Cursors.Hand, ToolTip = "Résumé — double-clic pour modifier", Child = new TextBlock { Text = b.N.Summary, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = LightTheme ? Theme.Brush(0xFF, Ink) : Theme.Res<Brush>("Text"), TextWrapping = TextWrapping.Wrap, MaxWidth = 160 } };
            Canvas.SetLeft(lbl, lx); Canvas.SetTop(lbl, ly);
            target.Children.Add(lbl);
        }

        void DrawCallout(Canvas target, Box b)
        {
            var sz = MeasureText(b.N.Callout, 11, FontWeights.Normal, FontStyles.Normal, 200);
            double w = sz.Width + 16, h = sz.Height + 10;
            double x = b.X + b.W / 2 - w / 2, y = b.Y - h - 9;
            var bubble = new Border { Width = w, Background = Theme.Brush(0xFF, Color.FromRgb(0xFF, 0xF3, 0xC4)), BorderBrush = Theme.Brush(0xFF, Color.FromRgb(0xE0, 0xB8, 0x4A)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 4, 8, 4), Tag = "callout", DataContext = b.N, Cursor = Cursors.Hand, ToolTip = "Légende — double-clic pour modifier", Child = new TextBlock { Text = b.N.Callout, FontSize = 11, Foreground = Theme.Brush(0xFF, Color.FromRgb(0x3A, 0x2F, 0x00)), TextWrapping = TextWrapping.Wrap, MaxWidth = 200 } };
            Canvas.SetLeft(bubble, x); Canvas.SetTop(bubble, y);
            target.Children.Add(bubble);
            var tip = new Polygon { Points = new PointCollection { new Point(b.X + b.W / 2 - 5, y + h - 1), new Point(b.X + b.W / 2 + 5, y + h - 1), new Point(b.X + b.W / 2, y + h + 7) }, Fill = Theme.Brush(0xFF, Color.FromRgb(0xFF, 0xF3, 0xC4)), Stroke = Theme.Brush(0xFF, Color.FromRgb(0xE0, 0xB8, 0x4A)), StrokeThickness = 1, IsHitTestVisible = false };
            target.Children.Add(tip);
        }

        static Point EdgePoint(Box box, Point toward)
        {
            double cx = box.X + box.W / 2, cy = box.Y + box.H / 2, dx = toward.X - cx, dy = toward.Y - cy;
            if (Math.Abs(dx) < 0.01 && Math.Abs(dy) < 0.01) return new Point(cx, cy);
            double sx = Math.Abs(dx) > 0.01 ? (box.W / 2) / Math.Abs(dx) : double.MaxValue, sy = Math.Abs(dy) > 0.01 ? (box.H / 2) / Math.Abs(dy) : double.MaxValue;
            double s = Math.Min(sx, sy);
            return new Point(cx + dx * s, cy + dy * s);
        }

        void DrawRelation(Canvas target, MindRelation r)
        {
            MindNode na, nb; Box a, b;
            if (!byId.TryGetValue(r.From, out na) || !byId.TryGetValue(r.To, out nb) || !boxes.TryGetValue(na, out a) || !boxes.TryGetValue(nb, out b)) return;
            var ca = new Point(a.X + a.W / 2, a.Y + a.H / 2); var cb = new Point(b.X + b.W / 2, b.Y + b.H / 2);
            var p0 = EdgePoint(a, cb); var p2 = EdgePoint(b, ca);
            var d = p2 - p0; double len = d.Length;
            if (len < 2) return;
            var nrm = new Vector(-d.Y / len, d.X / len);
            var ctrl = new Point((p0.X + p2.X) / 2 + nrm.X * Math.Min(60, len * 0.25), (p0.Y + p2.Y) / 2 + nrm.Y * Math.Min(60, len * 0.25));
            var stroke = LightTheme ? Theme.Brush(0xFF, Darken(a.Color, 0.1)) : Theme.Brush(0xE0, Lighten(a.Color, 0.2));
            var path = new Path { Stroke = stroke, StrokeThickness = 1.6, StrokeDashArray = new DoubleCollection { 5, 3 }, StrokeStartLineCap = PenLineCap.Round, IsHitTestVisible = false };
            var g = new StreamGeometry();
            using (var ctx = g.Open()) { ctx.BeginFigure(p0, false, false); ctx.QuadraticBezierTo(ctrl, p2, true, false); }
            g.Freeze(); path.Data = g;
            target.Children.Add(path);
            var t = p2 - ctrl; if (t.Length < 0.01) t = d; t.Normalize();
            var n2 = new Vector(-t.Y, t.X);
            target.Children.Add(new Polygon { Points = new PointCollection { p2, p2 - t * 10 + n2 * 4.5, p2 - t * 10 - n2 * 4.5 }, Fill = stroke, IsHitTestVisible = false });
            string text = string.IsNullOrWhiteSpace(r.Label) ? "…" : r.Label;
            var sz = MeasureText(text, 11, FontWeights.Normal, FontStyles.Normal, 160);
            var m = new Point(0.25 * p0.X + 0.5 * ctrl.X + 0.25 * p2.X, 0.25 * p0.Y + 0.5 * ctrl.Y + 0.25 * p2.Y);
            var lbl = new Border { Background = LightTheme ? Brushes.White : Theme.Res<Brush>("MenuBg"), BorderBrush = stroke, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(6, 1, 6, 2), Tag = "rel", DataContext = r, Cursor = Cursors.Hand, ToolTip = "Relation — double-clic : texte, clic droit : inverser ou supprimer", Child = new TextBlock { Text = text, FontSize = 11, Foreground = LightTheme ? Theme.Brush(0xFF, Ink) : Theme.Res<Brush>("Text"), Opacity = string.IsNullOrWhiteSpace(r.Label) ? 0.6 : 1, TextWrapping = TextWrapping.Wrap, MaxWidth = 160 } };
            Canvas.SetLeft(lbl, m.X - (sz.Width + 14) / 2); Canvas.SetTop(lbl, m.Y - (sz.Height + 5) / 2);
            target.Children.Add(lbl);
        }

        // ---- vue : zoom, ajustement -------------------------------------------------------------------------------------

        Rect Bounds()
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var b in boxes.Values)
            {
                minX = Math.Min(minX, b.X); minY = Math.Min(minY, b.Y - (string.IsNullOrWhiteSpace(b.N.Callout) ? 0 : 44)); maxX = Math.Max(maxX, b.X + b.W); maxY = Math.Max(maxY, b.Y + b.H);
                if (b.N.Boundary) { minX = Math.Min(minX, b.X - 12); maxX = Math.Max(maxX, b.X + b.W + 12); }
            }
            if (minX == double.MaxValue) return new Rect(0, 0, 10, 10);
            return new Rect(minX - 14, minY - 14, maxX - minX + 28, maxY - minY + 28);
        }

        public void Fit()
        {
            if (viewport.ActualWidth < 10 || viewport.ActualHeight < 10) { fitPending = true; return; }
            var r = Bounds();
            double pad = 24;
            double z = Math.Min((viewport.ActualWidth - pad * 2) / Math.Max(1, r.Width), (viewport.ActualHeight - pad * 2) / Math.Max(1, r.Height));
            z = Math.Max(0.3, Math.Min(1.25, z));
            scale.ScaleX = scale.ScaleY = z;
            pan.X = viewport.ActualWidth / 2 - (r.X + r.Width / 2) * z;
            pan.Y = viewport.ActualHeight / 2 - (r.Y + r.Height / 2) * z;
            SaveView();
        }

        void SaveView() { map.Zoom = scale.ScaleX; map.PanX = pan.X; map.PanY = pan.Y; UpdateFooter(); saveDeb.Trigger(); PlaceToolbar(); }

        /// <summary>Décale la vue au minimum pour que le sujet soit entièrement visible.</summary>
        void EnsureVisible(MindNode n)
        {
            Box b;
            if (n == null || !boxes.TryGetValue(n, out b)) return;
            double vw = viewport.ActualWidth, vh = viewport.ActualHeight;
            if (vw < 10 || vh < 10) return;
            double z = scale.ScaleX, pad = 16;
            double x0 = b.X * z + pan.X, y0 = b.Y * z + pan.Y, x1 = x0 + b.W * z, y1 = y0 + b.H * z;
            double dx = 0, dy = 0;
            if (x1 > vw - pad) dx = vw - pad - x1;
            if (x0 + dx < pad) dx = pad - x0;
            if (y1 > vh - pad) dy = vh - pad - y1;
            if (y0 + dy < pad) dy = pad - y0;
            if (dx != 0 || dy != 0) { pan.X += dx; pan.Y += dy; SaveView(); }
        }

        // ---- plan texte, Markdown, image ------------------------------------------------------------------------------

        static string Outline(MindNode n, int depth)
        {
            var sb = new StringBuilder();
            sb.Append(new string(' ', depth * 4)).Append(depth == 0 ? "" : "- ").AppendLine(Label(n));
            foreach (var ch in n.Children) sb.Append(Outline(ch, depth + 1));
            return sb.ToString();
        }

        /// <summary>Markdown façon XMind : titres pour les trois premiers niveaux, listes imbriquées ensuite ; notes en paragraphes.</summary>
        static string Markdown(MindNode root)
        {
            var sb = new StringBuilder();
            MarkdownRec(sb, root, 0);
            return sb.ToString();
        }

        static void MarkdownRec(StringBuilder sb, MindNode n, int depth)
        {
            string t = Label(n);
            if (n.Todo != 0 && depth > 2) t = (n.Todo == 2 ? "[x] " : "[ ] ") + t;
            if (depth <= 2) { sb.Append(new string('#', depth + 1)).Append(' ').AppendLine(t); if (!string.IsNullOrWhiteSpace(n.Note)) sb.AppendLine().AppendLine(n.Note); sb.AppendLine(); }
            else sb.Append(new string(' ', (depth - 3) * 2)).Append("- ").AppendLine(t);
            foreach (var ch in n.Children) MarkdownRec(sb, ch, depth + 1);
            if (depth == 2 && n.Children.Count > 0) sb.AppendLine();
        }

        static readonly Regex NumberPrefix = new Regex(@"^\d+[.)]\s+");

        /// <summary>
        /// Lit un plan indenté (espaces, tabulations, « - ») ou du Markdown (titres #, listes) sous into ;
        /// rootFromFirst : la première ligne devient le texte de into (sujet central).
        /// </summary>
        static void ParseOutline(string text, MindNode into, bool rootFromFirst)
        {
            var lines = text.Replace("\r", "").Split('\n');
            int step = int.MaxValue;
            foreach (var raw in lines) { int ind = 0; foreach (char ch in raw) { if (ch == ' ') ind++; else if (ch == '\t') ind += 4; else break; } if (ind > 0) step = Math.Min(step, ind); }
            if (step == int.MaxValue) step = 4;
            var stack = new List<KeyValuePair<int, MindNode>>();
            int headingLevel = -1;
            bool first = rootFromFirst;
            foreach (var raw in lines)
            {
                if (raw.Trim().Length == 0) continue;
                string t = raw.TrimStart(' ', '\t');
                int level; string label; bool heading = false; int todo = 0;
                int hashes = 0; while (hashes < t.Length && t[hashes] == '#') hashes++;
                if (hashes > 0 && hashes <= 6 && hashes < t.Length && t[hashes] == ' ') { level = hashes - 1; label = t.Substring(hashes).Trim(); headingLevel = level; heading = true; }
                else
                {
                    int indent = 0; foreach (char ch in raw) { if (ch == ' ') indent++; else if (ch == '\t') indent += 4; else break; }
                    label = t.TrimStart('-', '*', '+', '•', ' ').Trim();
                    if (label.StartsWith("[ ] ")) { todo = 1; label = label.Substring(4).Trim(); } else if (label.StartsWith("[x] ") || label.StartsWith("[X] ")) { todo = 2; label = label.Substring(4).Trim(); }
                    label = NumberPrefix.Replace(label, "");
                    level = headingLevel + 1 + indent / step;
                }
                if (label.Length == 0) continue;
                if (first) { into.Text = label; stack.Clear(); stack.Add(new KeyValuePair<int, MindNode>(-1, into)); first = false; if (!heading) headingLevel = -1; continue; }
                while (stack.Count > 0 && stack[stack.Count - 1].Key >= level) stack.RemoveAt(stack.Count - 1);
                var parent = stack.Count > 0 ? stack[stack.Count - 1].Value : into;
                var node = new MindNode { Id = NewId(), Text = label, Todo = todo };
                parent.Children.Add(node);
                stack.Add(new KeyValuePair<int, MindNode>(level, node));
            }
        }

        void ImportOutline()
        {
            var text = Dialogs.PromptMultiline(this, "Importer un plan ou du Markdown", "Une ligne par sujet : l'indentation (espaces, tabulations, « - ») ou les titres Markdown (#, ##, ###) donnent le niveau ; la première ligne devient le sujet central si la feuille est vide", "");
            if (string.IsNullOrWhiteSpace(text)) return;
            Snapshot();
            bool emptyMap = map.Root.Children.Count == 0 && (string.IsNullOrWhiteSpace(map.Root.Text) || map.Root.Text == "Sujet central");
            ParseOutline(text, map.Root, emptyMap);
            Changed();
            Fit();
        }

        void ExportPng(bool toClipboard)
        {
            try
            {
                var r = Bounds();
                double pad = 40;
                var off = new Canvas { Width = r.Width + pad * 2, Height = r.Height + pad * 2, Background = Brushes.Transparent };
                Draw(off, false);
                off.RenderTransform = new TranslateTransform(pad - r.X, pad - r.Y);
                var host = new Grid { Width = off.Width, Height = off.Height, Background = LightTheme ? Theme.Brush(0xFF, Color.FromRgb(0xF6, 0xF7, 0xF9)) : Theme.Brush(0xFF, Color.FromRgb(0x1E, 0x20, 0x28)), ClipToBounds = true };
                host.Children.Add(off);
                host.Measure(new Size(host.Width, host.Height));
                host.Arrange(new Rect(0, 0, host.Width, host.Height));
                host.UpdateLayout();
                double dpi = 144;
                var bmp = new RenderTargetBitmap((int)Math.Ceiling(host.Width * dpi / 96), (int)Math.Ceiling(host.Height * dpi / 96), dpi, dpi, PixelFormats.Pbgra32);
                bmp.Render(host);
                if (toClipboard) { Clipboard.SetImage(bmp); c.Toast("Image de la carte copiée dans le presse-papiers"); return; }
                using (var dlg = new WF.SaveFileDialog { Title = "Enregistrer la carte en image", Filter = "Image PNG (*.png)|*.png", FileName = SafeName(map.Name) + ".png", InitialDirectory = App.Desktop })
                {
                    if (dlg.ShowDialog(new Dialogs.Win32Owner(Hwnd)) != WF.DialogResult.OK) return;
                    var enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(bmp));
                    using (var fs = System.IO.File.Create(dlg.FileName)) enc.Save(fs);
                    c.Toast("Carte enregistrée : " + System.IO.Path.GetFileName(dlg.FileName));
                }
            }
            catch (Exception ex) { Log.Error(ex, "export de la carte"); c.Toast("Export impossible (voir erreurs.log)"); }
        }

        static string SafeName(string s)
        {
            foreach (var ch in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_');
            return s.Trim().Length == 0 ? "carte" : s.Trim();
        }

        // ---- boîte « Nouvelle feuille » : nom et galerie de modèles ------------------------------------------------------

        class TemplateDialog : DarkDialog
        {
            readonly TextBox name;
            readonly List<Border> cards = new List<Border>();
            public MapTemplate Chosen;
            public string MapName { get { return name.Text.Trim(); } }

            public TemplateDialog(Window owner, string defaultName) : base(owner, "Nouvelle feuille", 540)
            {
                Chosen = Templates[0];
                Panel.Children.Add(new TextBlock { Text = "Nom de la feuille", Foreground = Theme.Res<Brush>("SMuted"), FontSize = 12, Margin = new Thickness(0, 4, 0, 4) });
                name = new TextBox { Style = Theme.Res<Style>("SFieldBox"), Text = defaultName };
                Panel.Children.Add(name);
                Panel.Children.Add(new TextBlock { Text = "Modèle (double-clic pour créer directement)", Foreground = Theme.Res<Brush>("SMuted"), FontSize = 12, Margin = new Thickness(0, 12, 0, 4) });
                var grid = new UniformGrid { Columns = 2 };
                foreach (var t0 in Templates)
                {
                    var t = t0;
                    var card = new Border { CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Padding = new Thickness(8, 8, 8, 8), Margin = new Thickness(3), Cursor = Cursors.Hand, Tag = t };
                    var sp = new StackPanel();
                    var img = TemplateImage(t.Id);
                    if (img != null)
                        sp.Children.Add(new Rectangle { Width = 238, Height = 118, RadiusX = 8, RadiusY = 8, Fill = new ImageBrush(img) { Stretch = Stretch.UniformToFill }, Margin = new Thickness(0, 0, 0, 6), SnapsToDevicePixels = true });
                    else
                    {
                        var thumb = new Canvas { Width = 238, Height = 118, ClipToBounds = true, Margin = new Thickness(0, 0, 0, 6) };
                        DrawTemplateThumb(thumb, t);
                        sp.Children.Add(new Border { CornerRadius = new CornerRadius(8), Child = thumb, Background = Brushes.Transparent, ClipToBounds = true });
                    }
                    sp.Children.Add(new TextBlock { Text = t.Name, FontWeight = FontWeights.SemiBold, FontSize = 13 });
                    sp.Children.Add(new TextBlock { Text = t.Desc, FontSize = 11, Foreground = Theme.Res<Brush>("SMuted"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });
                    card.Child = sp;
                    card.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                    {
                        Chosen = t; Refresh();
                        if (e.ClickCount == 2 && Validate()) Finish(true);
                    };
                    cards.Add(card);
                    grid.Children.Add(card);
                }
                Panel.Children.Add(new ScrollViewer { Content = grid, MaxHeight = Math.Max(240, SystemParameters.WorkArea.Height - 360), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Focusable = false });
                SetOk("Créer", false);
                Loaded += delegate { name.Focus(); name.SelectAll(); };
                Refresh();
            }

            void Refresh()
            {
                foreach (var card in cards)
                {
                    bool sel = card.Tag == Chosen;
                    card.Background = Theme.Res<Brush>(sel ? "SHover" : "SField");
                    card.BorderBrush = sel ? Theme.Res<Brush>("Accent") : Theme.Res<Brush>("SLine");
                    card.BorderThickness = new Thickness(sel ? 2 : 1);
                }
            }

            protected override bool Validate() { return MapName.Length > 0; }
        }
    }
}
