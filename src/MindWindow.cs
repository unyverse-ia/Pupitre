using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Pupitre
{
    /// <summary>
    /// Panneau « Cartes mentales », calqué sur XMind : feuilles (cartes) créées à partir de modèles, neuf structures,
    /// sujet central / principaux / secondaires / flottants / parent, relations, limites, résumés, légendes,
    /// marqueurs par groupes, étiquettes, notes, liens, cases à faire, numérotation hiérarchique, thèmes et palettes,
    /// lignes effilées, styles rapides, pliage, recherche, branche seule, annuler / rétablir, export image et Markdown.
    /// Cette partie : état, barre d'outils, opérations sur le modèle, souris, clavier et menus.
    /// La disposition, le dessin, les marqueurs et les imports / exports sont dans MindWindowDraw.cs.
    /// </summary>
    public partial class MindWindow : DesktopWindow
    {
        readonly MindStore store;
        MindMap map;
        MindNode selected, focus;
        readonly Dictionary<MindNode, MindNode> parentOf = new Dictionary<MindNode, MindNode>();
        readonly Dictionary<MindNode, Box> boxes = new Dictionary<MindNode, Box>();
        readonly Dictionary<MindNode, Border> views = new Dictionary<MindNode, Border>();
        readonly Dictionary<string, MindNode> byId = new Dictionary<string, MindNode>();
        readonly List<string> undo = new List<string>(), redo = new List<string>();
        Canvas canvas;
        Grid viewport;
        Border canvasBg, gridBg;
        TextBlock hint, footer;
        Button mapBtn, focusBtn;
        TextBox editor;
        MindNode editing;
        Border editOuter, editWrap;
        Box editBox;
        bool editNew;
        readonly ScaleTransform scale = new ScaleTransform(1, 1);
        readonly TranslateTransform pan = new TranslateTransform(0, 0);
        readonly Debouncer saveDeb;
        // souris : panoramique, ou glisser d'un sujet (rattacher, détacher en flottant, déplacer un flottant)
        bool panning, dragging;
        Point mouseStart, panStart, grabInView;
        MindNode dragNode, dragOrigParent;
        string dragSnapshot;            // état avant le glisser : annulation (Échap) et pile d'annulation
        MindNode previewParent;         // aperçu en direct : où le sujet est provisoirement placé
        int previewIndex, previewSide;
        bool previewFloating, dragMoved;
        readonly DragGhost ghost = new DragGhost();
        bool fitPending;
        MindNode relFrom;                       // mode « relation » : sujet de départ, en attente du clic sur la cible
        string searchText = "";
        readonly List<MindNode> searchHits = new List<MindNode>();
        int searchIndex;

        class Box { public MindNode N; public double X, Y, W, H, TextW, Extra, PadX, PadY, LabelsH, MinW; public int Depth; public int Side; public Color Color; public bool Floating; }

        public MindWindow(Controller ctl) : base(ctl)
        {
            SetTitle("Cartes mentales");
            MinWidth = 260;
            store = Json.Load<MindStore>(c.MindPath);
            if (store.Maps.Count == 0) store.Maps.Add(NewMap("Ma première carte", Templates[0]));
            map = store.Maps.FirstOrDefault(m => m.Id == store.CurrentId) ?? store.Maps[0];
            store.CurrentId = map.Id;
            StabilizeSides();
            saveDeb = new Debouncer(700, delegate { map.Updated = Now(); Json.Save(c.MindPath, store); RefreshMapList(); });

            var fit = Theme.IconButton("IcoRefresh", "Ajuster la carte à la fenêtre  (Ctrl+0)", 15);
            fit.Click += delegate { Fit(); };
            AddAction(fit);
            var sideBtn = Theme.IconButton("IcoList", "Afficher / masquer le volet (liste des cartes, options de style)", 15);
            sideBtn.Click += delegate { ToggleStylePanel(); };
            AddAction(sideBtn);
            var full = Theme.IconButton("IcoExternal", "Plein écran : le panneau occupe tout l'écran ; même bouton pour revenir", 15);
            full.Click += delegate { ToggleFullScreen(); Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(Fit)); };
            AddAction(full);
            AddStandardActions();

            var body = new DockPanel { LastChildFill = true };
            // barre d'outils minimale : feuille, insérer, style (volet), exporter
            var bar = new DockPanel { Margin = new Thickness(10, 6, 10, 4), LastChildFill = false };
            mapBtn = Chip("", "Feuilles : changer, créer à partir d'un modèle, renommer, dupliquer, supprimer");
            mapBtn.Click += delegate { ShowMapList(); };
            bar.Children.Add(mapBtn);
            var insert = Chip("Insérer ▾", "Sujet, sujet flottant, relation, limite, résumé, légende, marqueur, étiquette, note, lien, case à faire");
            insert.Margin = new Thickness(6, 0, 4, 2);
            insert.Click += delegate { ShowInsertMenu(insert); };
            bar.Children.Add(insert);
            var export = Chip("Exporter ▾", "Image, presse-papiers, plan texte, Markdown ; importer un plan ou du Markdown");
            export.Click += delegate { ShowExportMenu(export); };
            bar.Children.Add(export);
            focusBtn = Chip("⤴ Toute la carte", "Quitter l'affichage d'une seule branche  (Ctrl+;)");
            focusBtn.Visibility = Visibility.Collapsed;
            focusBtn.Margin = new Thickness(8, 0, 0, 0);
            focusBtn.Click += delegate { SetFocus(null); };
            bar.Children.Add(focusBtn);
            DockPanel.SetDock(bar, Dock.Top);
            body.Children.Add(bar);

            footer = new TextBlock { Foreground = Theme.Res<Brush>("TextFaint"), FontSize = 11, Margin = new Thickness(14, 2, 14, 8), TextTrimming = TextTrimming.CharacterEllipsis };
            DockPanel.SetDock(footer, Dock.Bottom);
            body.Children.Add(footer);

            var side = BuildStylePanel();
            DockPanel.SetDock(side, Dock.Left);
            body.Children.Add(side);

            viewport = new Grid { ClipToBounds = true, Background = Brushes.Transparent, Focusable = true, Margin = new Thickness(0, 0, 6, 0), FocusVisualStyle = null };
            canvasBg = new Border { CornerRadius = new CornerRadius(10), Background = Brushes.Transparent };
            viewport.Children.Add(canvasBg);
            gridBg = new Border { CornerRadius = new CornerRadius(10), Background = DotGrid(Theme.Argb(0x14FFFFFF)), IsHitTestVisible = false };
            viewport.Children.Add(gridBg);
            canvas = new Canvas { Background = Brushes.Transparent };
            var tg = new TransformGroup(); tg.Children.Add(scale); tg.Children.Add(pan);
            canvas.RenderTransform = tg;
            viewport.Children.Add(canvas);
            overlay = new Canvas();
            overlay.Children.Add(BuildToolbar());
            viewport.Children.Add(overlay);
            editor = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(0), Margin = new Thickness(0), TextAlignment = TextAlignment.Center, MinWidth = 40, MaxWidth = 240, VerticalContentAlignment = VerticalAlignment.Center, FocusVisualStyle = null };
            editor.PreviewKeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0) { EndEdit(true); e.Handled = true; }
                else if (e.Key == Key.Escape) { EndEdit(false); e.Handled = true; }
                else if (e.Key == Key.Tab) { EndEdit(true); if (selected != null) AddChild(); e.Handled = true; }
            };
            editor.TextChanged += delegate { OnEditorTextChanged(); };
            hint = new TextBlock { Foreground = Theme.Res<Brush>("TextFaint"), FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(12, 0, 12, 8), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, IsHitTestVisible = false };
            viewport.Children.Add(hint);
            body.Children.Add(viewport);
            Body.Content = body;

            viewport.MouseLeftButtonDown += ViewportDown;
            viewport.MouseMove += ViewportMove;
            viewport.MouseLeftButtonUp += ViewportUp;
            viewport.LostMouseCapture += delegate { if (dragging) CancelDrag(); else EndMouse(); };
            viewport.MouseRightButtonUp += ViewportRight;
            viewport.MouseWheel += ViewportWheel;
            viewport.SizeChanged += delegate { if (fitPending) { fitPending = false; Fit(); } };
            SizeChanged += delegate { UpdateStyleVisibility(); };
            PreviewKeyDown += OnKey;
            PreviewTextInput += OnTextInput;
            Deactivated += delegate { if (editing != null) EndEdit(true); };

            var look = c.L.MindPanel;
            SetLook(look.Color, look.Opacity, look.CustomColor);
            selected = map.Root;
            scale.ScaleX = scale.ScaleY = Math.Max(0.3, Math.Min(3, map.Zoom <= 0 ? 1 : map.Zoom));
            pan.X = map.PanX; pan.Y = map.PanY;
            if (map.PanX == 0 && map.PanY == 0) fitPending = true;
            UpdateStyleVisibility();
            Render();
        }

        const string EmptyHint = "Cliquez le sujet central : un « + » à droite et un « + » à gauche ajoutent un sujet de ce côté (Tab : côté le moins chargé)  ·  Entrée : sujet frère  ·  tapez directement pour remplacer le texte, double-clic pour le modifier  ·  Suppr : supprimer  ·  la barre au-dessus du sujet donne couleur, forme, marqueurs, note, lien  ·  molette : zoom  ·  double-clic sur le fond : sujet flottant";

        /// <summary>Fond pointillé discret (grille de points), comme les outils de schéma modernes.</summary>
        static Brush DotGrid(Brush dot)
        {
            var b = new DrawingBrush { Drawing = new GeometryDrawing(dot, null, new EllipseGeometry(new Point(1.2, 1.2), 1.1, 1.1)), TileMode = TileMode.Tile, Viewport = new Rect(0, 0, 22, 22), ViewportUnits = BrushMappingMode.Absolute, Viewbox = new Rect(0, 0, 22, 22), ViewboxUnits = BrushMappingMode.Absolute };
            b.Freeze();
            return b;
        }

        static Button Chip(string text, string tip)
        {
            return new Button { Style = Theme.Res<Style>("ChipBtn"), Content = text, Margin = new Thickness(0, 0, 4, 2), ToolTip = tip };
        }

        static string NewId() { return Guid.NewGuid().ToString("N"); }
        static string Now() { return DateTime.Now.ToString("s"); }

        /// <summary>Sujet affiché comme racine : le sujet central, ou la branche seule en cours d'affichage.</summary>
        MindNode RootNode { get { return focus ?? map.Root; } }

        // ---- feuilles (cartes) -----------------------------------------------------------------------------------

        static MindMap NewMap(string name, MapTemplate t)
        {
            var m = new MindMap { Id = NewId(), Name = name, Created = Now(), Updated = Now(), Root = new MindNode { Id = NewId(), Text = "Sujet central" }, Zoom = 1, Shape = 1, Tapered = true };
            if (t != null)
            {
                m.Template = t.Id; m.Layout = t.Layout; m.LineStyle = t.LineStyle; m.Theme = t.Theme; m.Shape = t.Shape; m.Palette = t.Palette;
                if (t.Children != null) foreach (var ch in t.Children) m.Root.Children.Add(new MindNode { Id = NewId(), Text = ch });
            }
            return m;
        }

        void SelectMap(MindMap m)
        {
            if (m == null || m == map) return;
            if (editing != null) EndEdit(true);
            map = m; store.CurrentId = m.Id; undo.Clear(); redo.Clear(); focus = null; selected = m.Root; relFrom = null; searchText = ""; searchHits.Clear();
            StabilizeSides();
            scale.ScaleX = scale.ScaleY = Math.Max(0.3, Math.Min(3, m.Zoom <= 0 ? 1 : m.Zoom));
            pan.X = m.PanX; pan.Y = m.PanY;
            Render();
            if (m.PanX == 0 && m.PanY == 0) Fit();
            saveDeb.Trigger();
            RefreshMapList();
        }

        /// <summary>Nouvelle feuille : nom et modèle (structure, style, sujets de départ).</summary>
        public void NewMapInteractive()
        {
            if (IsRolled) SetRolled(false, true);
            var d = new TemplateDialog(this, "Carte " + (store.Maps.Count + 1));
            if (d.ShowDialog() != true) return;
            var m = NewMap(d.MapName, d.Chosen);
            store.Maps.Add(m);
            SelectMap(m);
            Activate();
            BeginEdit(m.Root);
        }

        void NewMapFromNode(MindNode n)
        {
            var root = MindJson.CloneNode(n);
            foreach (var d in All(root)) d.Id = NewId();
            root.Side = 0; root.Boundary = false; root.Summary = null; root.Collapsed = false;
            var m = new MindMap { Id = NewId(), Name = Label(n), Created = Now(), Updated = Now(), Root = root, Zoom = 1, Theme = map.Theme, Palette = map.Palette, Layout = map.Layout, LineStyle = map.LineStyle, LineWidth = map.LineWidth, Spacing = map.Spacing, Shape = map.Shape, NumberStyle = map.NumberStyle, Tiered = map.Tiered, Mono = map.Mono, Tapered = map.Tapered };
            store.Maps.Add(m);
            SelectMap(m);
            c.Toast("Nouvelle feuille « " + m.Name + " » créée à partir du sujet");
        }

        void DeleteMap(MindMap m)
        {
            if (!Dialogs.Confirm(this, "Supprimer la feuille", "« " + m.Name + " » et tous ses sujets seront supprimés.", "Supprimer", true)) return;
            int idx = store.Maps.IndexOf(m);
            store.Maps.Remove(m);
            if (store.Maps.Count == 0) store.Maps.Add(NewMap("Ma première carte", Templates[0]));
            if (m == map) SelectMap(store.Maps[Math.Max(0, Math.Min(idx, store.Maps.Count - 1))]);
            else { UpdateFooter(); saveDeb.Trigger(); }
        }

        // ---- modèle : parcours ------------------------------------------------------------------------------------

        static IEnumerable<MindNode> All(MindNode n)
        {
            yield return n;
            foreach (var ch in n.Children) foreach (var d in All(ch)) yield return d;
        }

        /// <summary>Tous les sujets de la feuille (central, branches, flottants).</summary>
        IEnumerable<MindNode> AllNodes()
        {
            foreach (var n in All(map.Root)) yield return n;
            foreach (var f in map.Floating) foreach (var n in All(f)) yield return n;
        }

        MindNode ParentOf(MindNode n) { MindNode p; return parentOf.TryGetValue(n, out p) ? p : null; }
        /// <summary>Parent d'après le modèle (la table parentOf ne couvre que la vue courante, p. ex. en affichage d'une seule branche).</summary>
        MindNode RealParentOf(MindNode n) { foreach (var a in AllNodes()) if (a.Children.Contains(n)) return a; return null; }
        /// <summary>Déplie un sujet et tous ses ancêtres : ce qu'on vient d'y ajouter doit être visible.</summary>
        void UnfoldAncestors(MindNode n) { for (var a = n; a != null; a = ParentOf(a) ?? RealParentOf(a)) a.Collapsed = false; }
        /// <summary>Après remplacement de la carte (annuler, rétablir, glisser annulé) : sélection, focus et résultats de recherche réaccrochés par identifiant.</summary>
        void RemapRefs(string selId, string focusId)
        {
            focus = focusId != null ? AllNodes().FirstOrDefault(n => n.Id == focusId) : null;
            selected = (selId != null ? AllNodes().FirstOrDefault(n => n.Id == selId) : null) ?? map.Root;
            var ids = searchHits.Select(h => h.Id).ToList();
            searchHits.Clear();
            foreach (var id in ids) { var h = AllNodes().FirstOrDefault(x => x.Id == id); if (h != null) searchHits.Add(h); }
            if (searchIndex >= searchHits.Count) searchIndex = 0;
        }

        bool IsDescendant(MindNode node, MindNode ancestor)
        {
            var p = ParentOf(node);
            while (p != null) { if (p == ancestor) return true; p = ParentOf(p); }
            return false;
        }

        static string Label(MindNode n) { return string.IsNullOrWhiteSpace(n.Text) ? "(sans titre)" : n.Text.Replace("\r", " ").Replace("\n", " "); }

        // ---- annuler / rétablir, enregistrement ---------------------------------------------------------------------

        void Snapshot()
        {
            undo.Add(MindJson.ToText(map));
            if (undo.Count > 50) undo.RemoveAt(0);
            redo.Clear();
        }

        void Restore(string json, List<string> pushTo)
        {
            pushTo.Add(MindJson.ToText(map));
            var restored = MindJson.FromText(json);
            restored.Zoom = map.Zoom; restored.PanX = map.PanX; restored.PanY = map.PanY;
            store.Maps[store.Maps.IndexOf(map)] = restored; map = restored;
            RemapRefs(selected != null ? selected.Id : null, focus != null ? focus.Id : null);
            Render();
            saveDeb.Trigger();
        }

        void Undo() { if (undo.Count == 0) return; var s = undo[undo.Count - 1]; undo.RemoveAt(undo.Count - 1); Restore(s, redo); }
        void Redo() { if (redo.Count == 0) return; var s = redo[redo.Count - 1]; redo.RemoveAt(redo.Count - 1); Restore(s, undo); }

        void Changed()
        {
            // relations orphelines (sujet supprimé) : retirées
            var ids = new HashSet<string>(AllNodes().Select(n => n.Id));
            map.Relations.RemoveAll(r => !ids.Contains(r.From) || !ids.Contains(r.To) || r.From == r.To);
            if (focus != null && !ids.Contains(focus.Id)) focus = null;
            saveDeb.Trigger();
            Render();
        }

        // ---- sujets ---------------------------------------------------------------------------------------------------

        public void AddChild() { AddChildSide(selected ?? map.Root, 0); }

        /// <summary>Côté d'un sujet principal : celui enregistré, sinon l'alternance droite / gauche de sa position.</summary>
        static int SideOf(MindNode ch, int index) { return ch.Side != 0 ? ch.Side : (index % 2 == 0 ? 1 : -1); }

        bool SidedLayout { get { return map.Layout == 0 || map.Layout == 5 || map.Layout == 6 || map.Layout == 8; } }

        /// <summary>Fige le côté des sujets principaux : ajouter ou retirer un sujet ne fait plus changer les autres de côté.</summary>
        void StabilizeSides()
        {
            if (map == null || map.Root == null) return;
            for (int i = 0; i < map.Root.Children.Count; i++) { var ch = map.Root.Children[i]; if (ch.Side == 0) ch.Side = SideOf(ch, i); }
        }

        /// <summary>Côté le moins chargé pour un nouveau sujet principal (droite à égalité), comme l'équilibrage automatique de XMind.</summary>
        int BalanceSide()
        {
            if (!SidedLayout) return 0;
            int right = 0, left = 0;
            for (int i = 0; i < RootNode.Children.Count; i++) { if (SideOf(RootNode.Children[i], i) > 0) right++; else left++; }
            return right <= left ? 1 : -1;
        }

        /// <summary>Nouveau sous-sujet ; pour le sujet central, side = 1 droite / au-dessus, -1 gauche / en dessous, 0 équilibré.</summary>
        public void AddChildSide(MindNode target, int side)
        {
            if (target == null) target = map.Root;
            Snapshot();
            var n = new MindNode { Id = NewId(), Text = "" };
            if (target == RootNode && SidedLayout) n.Side = side != 0 ? side : BalanceSide();
            target.Children.Add(n);
            UnfoldAncestors(target);
            selected = n;
            Changed();
            BeginEdit(n);
            editNew = true;
        }

        public void AddSibling()
        {
            var p = selected != null ? ParentOf(selected) : null;
            if (p == null) { AddChild(); return; }
            Snapshot();
            var n = new MindNode { Id = NewId(), Text = "", Side = selected.Side };
            p.Children.Insert(p.Children.IndexOf(selected) + 1, n);
            UnfoldAncestors(p);
            selected = n;
            Changed();
            BeginEdit(n);
            editNew = true;
        }

        /// <summary>Ctrl+Entrée : intercale un nouveau sujet entre le sujet sélectionné et son parent.</summary>
        void InsertParent()
        {
            var p = selected != null ? ParentOf(selected) : null;
            if (p == null) return;
            Snapshot();
            var np = new MindNode { Id = NewId(), Text = "", Side = selected.Side };
            p.Children[p.Children.IndexOf(selected)] = np;
            np.Children.Add(selected);
            UnfoldAncestors(p);
            selected.Side = 0;
            selected = np;
            Changed();
            BeginEdit(np);
        }

        /// <summary>Sujet flottant à un point du canevas (coordonnées carte).</summary>
        void AddFloating(Point at, bool edit)
        {
            if (focus != null) SetFocus(null);
            Snapshot();
            var n = new MindNode { Id = NewId(), Text = "", X = at.X, Y = at.Y };
            map.Floating.Add(n);
            selected = n;
            Changed();
            if (edit) { BeginEdit(n); editNew = true; }
        }

        /// <summary>Suppr : propose toujours la suppression du sujet sélectionné (et de sa branche).</summary>
        void DeleteSelected()
        {
            if (selected == null || selected == map.Root) return;
            int n = All(selected).Count() - 1;
            string what = "« " + Label(selected) + " »" + (n > 0 ? " et ses " + n + " sous-sujet" + (n > 1 ? "s" : "") : "");
            if (!Dialogs.Confirm(this, "Supprimer le sujet", "Supprimer " + what + " ?", "Supprimer", true)) return;
            Snapshot();
            if (selected == focus) focus = null;                        // branche affichée seule : retour à toute la carte
            var p = ParentOf(selected) ?? RealParentOf(selected);
            if (p == null) { map.Floating.Remove(selected); selected = map.Root; }
            else
            {
                int idx = p.Children.IndexOf(selected);
                p.Children.Remove(selected);
                selected = idx < p.Children.Count ? p.Children[idx] : (idx > 0 ? p.Children[idx - 1] : p);
            }
            Changed();
        }

        /// <summary>Ctrl+Suppr : supprime le sujet seul, ses sous-sujets remontent d'un niveau.</summary>
        void DeleteSingle()
        {
            if (selected == null || selected == map.Root) return;
            Snapshot();
            if (selected == focus) focus = null;
            var p = ParentOf(selected) ?? RealParentOf(selected);
            if (p == null)
            {
                map.Floating.Remove(selected);
                double y = selected.Y;
                foreach (var ch in selected.Children) { ch.X = selected.X + 20; ch.Y = y; y += 50; map.Floating.Add(ch); }
                selected = selected.Children.Count > 0 ? selected.Children[0] : map.Root;
            }
            else
            {
                int idx = p.Children.IndexOf(selected);
                p.Children.RemoveAt(idx);
                p.Children.InsertRange(idx, selected.Children);
                selected = selected.Children.Count > 0 ? selected.Children[0] : p;
            }
            Changed();
        }

        void DuplicateSelected()
        {
            if (selected == null || selected == map.Root) return;
            Snapshot();
            var copy = MindJson.CloneNode(selected);
            foreach (var d in All(copy)) d.Id = NewId();
            var p = ParentOf(selected) ?? RealParentOf(selected);
            if (p == null) { copy.X = selected.X + 30; copy.Y = selected.Y + 30; map.Floating.Add(copy); }
            else p.Children.Insert(p.Children.IndexOf(selected) + 1, copy);
            selected = copy;
            Changed();
        }

        void CopySelected()
        {
            if (selected == null) return;
            try { Clipboard.SetText(Outline(selected, 0)); c.Toast("Branche copiée (texte indenté)"); } catch { }
        }

        void PasteIntoSelected()
        {
            string text;
            try { text = Clipboard.ContainsText() ? Clipboard.GetText() : null; } catch { return; }
            if (string.IsNullOrWhiteSpace(text)) return;
            var into = selected ?? map.Root;
            Snapshot();
            ParseOutline(text, into, false);
            UnfoldAncestors(into);
            Changed();
        }

        void MoveSelected(int delta)
        {
            if (selected == null) return;
            var p = ParentOf(selected);
            if (p == null) return;
            int i = p.Children.IndexOf(selected), j = i + delta;
            if (j < 0 || j >= p.Children.Count) return;
            Snapshot();
            p.Children.RemoveAt(i); p.Children.Insert(j, selected);
            Changed();
        }

        /// <summary>Rang d'insertion parmi les sous-sujets visibles de parent, d'après le point lâché (et le côté pour le sujet central).</summary>
        int InsertIndex(MindNode parent, MindNode node, Point at, int side)
        {
            bool byX = map.Layout == 2 || map.Layout == 7 || (parent == RootNode && (map.Layout == 5 || map.Layout == 6));
            // les piles « vers le haut » (chronologies, arêtes de poisson) rangent le premier enfant le plus près du parent : ordre inversé
            var cands = new List<KeyValuePair<int, double>>();
            for (int i = 0; i < parent.Children.Count; i++)
            {
                var ch = parent.Children[i]; Box cb;
                if (ch == node || !boxes.TryGetValue(ch, out cb)) continue;
                if (parent == RootNode && side != 0 && SideOf(ch, i) != side) continue;
                cands.Add(new KeyValuePair<int, double>(i, byX ? cb.X + cb.W / 2 : cb.Y + cb.H / 2));
            }
            double v = byX ? at.X : at.Y;
            bool reversed = cands.Count >= 2 && cands[1].Value < cands[0].Value;
            foreach (var kv in cands) if (reversed ? v > kv.Value : v < kv.Value) return kv.Key;
            return parent.Children.Count;
        }

        void ToggleFold(MindNode n)
        {
            if (n == null || n.Children.Count == 0) return;
            Snapshot(); n.Collapsed = !n.Collapsed; Changed();
        }

        /// <summary>Ctrl+Alt+/ : replie tous les sujets principaux, ou déplie tout si certains sont repliés.</summary>
        void ToggleFoldAll()
        {
            Snapshot();
            bool any = RootNode.Children.Any(ch => ch.Collapsed);
            if (any) foreach (var n in All(RootNode)) n.Collapsed = false;
            else foreach (var ch in RootNode.Children) ch.Collapsed = ch.Children.Count > 0;
            Changed();
        }

        /// <summary>Ctrl+; : n'affiche que la branche du sujet (null : toute la carte).</summary>
        void SetFocus(MindNode n)
        {
            if (editing != null) EndEdit(true);
            if (n == map.Root) n = null;
            focus = n;
            selected = n ?? map.Root;
            Render();
            Fit();
        }

        void GoToRoot() { selected = RootNode; Render(); EnsureVisible(selected); }

        // ---- enrichissements : marqueurs, étiquettes, note, lien, case, légende, limite, résumé, relation --------

        void SetMarker(MindNode n, MarkerDef def)
        {
            Snapshot();
            bool had = n.Markers.Contains(def.Id);
            n.Markers.RemoveAll(id => { var d = MarkerById(id); return d != null && d.Group == def.Group; });
            if (!had) n.Markers.Add(def.Id);
            Changed();
        }

        void EditLabels(MindNode n)
        {
            var s = Dialogs.Prompt(this, "Étiquettes", "Étiquettes du sujet, séparées par des virgules", string.Join(", ", n.Labels), false);
            if (s == null) return;
            Snapshot();
            n.Labels = s.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().ToList();
            Changed();
        }

        void EditNote(MindNode n)
        {
            var t = Dialogs.PromptMultiline(this, "Note du sujet", "Texte affiché au survol du sujet (icône ≡ pour le modifier)", n.Note ?? "");
            if (t == null) return;
            Snapshot(); n.Note = t.Trim(); Changed();
        }

        void EditLink(MindNode n)
        {
            var s = Dialogs.Prompt(this, "Lien du sujet", "Adresse web, chemin d'un fichier ou d'un dossier (vide pour retirer)", n.Link ?? "", false);
            if (s == null) return;
            Snapshot(); n.Link = s.Trim().Length > 0 ? s.Trim() : null; Changed();
        }

        static void OpenLink(string link)
        {
            if (string.IsNullOrWhiteSpace(link)) return;
            try { Process.Start(new ProcessStartInfo(link) { UseShellExecute = true }); }
            catch (Exception ex) { Log.Error(ex, "ouverture du lien " + link); }
        }

        void EditCallout(MindNode n)
        {
            var t = Dialogs.PromptMultiline(this, "Légende du sujet", "Bulle affichée au-dessus du sujet (vide pour retirer)", n.Callout ?? "");
            if (t == null) return;
            Snapshot(); n.Callout = t.Trim().Length > 0 ? t.Trim() : null; Changed();
        }

        void EditSummary(MindNode n)
        {
            var t = Dialogs.Prompt(this, "Résumé des sous-sujets", "Texte de l'accolade qui regroupe les sous-sujets (vide pour retirer)", n.Summary ?? "", false);
            if (t == null) return;
            Snapshot(); n.Summary = t.Trim().Length > 0 ? t.Trim() : null; Changed();
        }

        void ToggleBoundary(MindNode n)
        {
            if (n == null || n == map.Root) return;
            Snapshot(); n.Boundary = !n.Boundary; if (!n.Boundary) n.BoundaryLabel = null; Changed();
        }

        void EditBoundaryLabel(MindNode n)
        {
            var t = Dialogs.Prompt(this, "Texte de la limite", "Intitulé affiché sur le cadre", n.BoundaryLabel ?? "", false);
            if (t == null) return;
            Snapshot(); n.Boundary = true; n.BoundaryLabel = t.Trim().Length > 0 ? t.Trim() : null; Changed();
        }

        void ToggleTodo(MindNode n)
        {
            Snapshot();
            n.Todo = n.Todo == 0 ? 1 : (n.Todo == 1 ? 2 : 1);
            Changed();
        }

        void BeginRelation(MindNode from)
        {
            if (from == null) return;
            relFrom = from;
            hint.Text = "Relation depuis « " + Label(from) + " » : cliquez le sujet cible  (Échap pour annuler)";
            hint.Visibility = Visibility.Visible;
            viewport.Cursor = Cursors.Cross;
        }

        void CancelRelation() { relFrom = null; viewport.Cursor = Cursors.Arrow; Render(); }

        void CreateRelation(MindNode a, MindNode b)
        {
            if (a == null || b == null || a == b) return;
            Snapshot();
            var r = new MindRelation { Id = NewId(), From = a.Id, To = b.Id, Label = "" };
            map.Relations.Add(r);
            Changed();
            var t = Dialogs.Prompt(this, "Relation", "Texte affiché sur la relation (facultatif)", "", false);
            if (t != null && t.Trim().Length > 0) { r.Label = t.Trim(); Changed(); }
        }

        void ShowRelationMenu(MindRelation r)
        {
            var menu = new ContextMenu();
            menu.Items.Add(Item("Modifier le texte…", "IcoPencil", delegate { var t = Dialogs.Prompt(this, "Relation", "Texte de la relation", r.Label ?? "", false); if (t != null) { Snapshot(); r.Label = t.Trim(); Changed(); } }));
            menu.Items.Add(Item("Inverser le sens", "IcoRefresh", delegate { Snapshot(); var tmp = r.From; r.From = r.To; r.To = tmp; Changed(); }));
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Supprimer la relation", "IcoTrash", delegate { Snapshot(); map.Relations.Remove(r); Changed(); }));
            Open(menu, null);
        }

        /// <summary>Styles rapides XMind : 0 par défaut, 1 important, 2 très important, 3 barré.</summary>
        void QuickStyle(MindNode n, int which)
        {
            Snapshot();
            switch (which)
            {
                case 1: n.Bold = true; n.Size = 0; n.Color = 3; n.Strike = false; break;
                case 2: n.Bold = true; n.Size = 1; n.Color = 8; n.Strike = false; break;
                case 3: n.Strike = true; n.Bold = false; break;
                default: n.Bold = false; n.Italic = false; n.Strike = false; n.Size = 0; n.Color = 0; n.CustomColor = null; n.Shape = 0; break;
            }
            Changed();
        }

        // ---- recherche ------------------------------------------------------------------------------------------------

        void Find()
        {
            var s = Dialogs.Prompt(this, "Rechercher", "Texte à trouver dans les sujets, étiquettes, notes et légendes (F3 : résultat suivant, Échap : effacer)", searchText, false);
            if (s == null) return;
            searchText = s.Trim();
            RunSearch();
        }

        void RunSearch()
        {
            searchHits.Clear(); searchIndex = 0;
            if (searchText.Length == 0) { Render(); return; }
            string q = searchText.ToLowerInvariant();
            foreach (var n in AllNodes())
            {
                bool hit = (n.Text ?? "").ToLowerInvariant().Contains(q) || (n.Note ?? "").ToLowerInvariant().Contains(q) || (n.Callout ?? "").ToLowerInvariant().Contains(q) || n.Labels.Any(l => l.ToLowerInvariant().Contains(q));
                if (hit) searchHits.Add(n);
            }
            if (searchHits.Count == 0) { c.Toast("Aucun sujet ne contient « " + searchText + " »"); Render(); return; }
            ShowHit();
        }

        void NextHit()
        {
            if (searchHits.Count == 0) { if (searchText.Length > 0) RunSearch(); return; }
            searchIndex = (searchIndex + 1) % searchHits.Count;
            ShowHit();
        }

        void ShowHit()
        {
            var n = searchHits[searchIndex];
            if (focus != null && !All(focus).Contains(n)) focus = null;
            Render();
            // déplie les ancêtres si le résultat est caché
            var p = ParentOf(n); bool unfolded = false;
            while (p != null) { if (p.Collapsed) { p.Collapsed = false; unfolded = true; } p = ParentOf(p); }
            selected = n;
            if (unfolded) saveDeb.Trigger();
            Render();
            EnsureVisible(n);
        }

        // ---- souris ---------------------------------------------------------------------------------------------------

        Point ToCanvas(Point viewportPoint) { return new Point((viewportPoint.X - pan.X) / scale.ScaleX, (viewportPoint.Y - pan.Y) / scale.ScaleY); }

        MindNode NodeAt(object source)
        {
            var fe = Ui.HitTag(source, "node");
            return fe != null ? fe.DataContext as MindNode : null;
        }

        void ViewportDown(object sender, MouseButtonEventArgs e)
        {
            if (editing != null) { if (Ui.FindAncestor<TextBox>(e.OriginalSource as DependencyObject) == editor) return; EndEdit(true); }
            Activate();
            viewport.Focus();
            var p = e.GetPosition(viewport);
            if (toolbar != null && e.OriginalSource is DependencyObject && toolbar.IsAncestorOf((DependencyObject)e.OriginalSource)) return;
            if (relFrom != null)
            {
                var target = NodeAt(e.OriginalSource);
                var from = relFrom;
                CancelRelation();
                if (target != null && target != from) CreateRelation(from, target);
                e.Handled = true;
                return;
            }
            var tagged = Ui.HitTag(e.OriginalSource, "add") ?? Ui.HitTag(e.OriginalSource, "addsib") ?? Ui.HitTag(e.OriginalSource, "expand") ?? Ui.HitTag(e.OriginalSource, "todo") ?? Ui.HitTag(e.OriginalSource, "link") ?? Ui.HitTag(e.OriginalSource, "note") ?? Ui.HitTag(e.OriginalSource, "callout") ?? Ui.HitTag(e.OriginalSource, "summary") ?? Ui.HitTag(e.OriginalSource, "boundary") ?? Ui.HitTag(e.OriginalSource, "rel");
            if (tagged != null)
            {
                string tag = tagged.Tag as string;
                var n = tagged.DataContext as MindNode;
                var r = tagged.DataContext as MindRelation;
                e.Handled = true;
                switch (tag)
                {
                    case "add": if (n != null) { selected = n; AddChildSide(n, tagged.Uid == "1" ? 1 : (tagged.Uid == "-1" ? -1 : 0)); } return;
                    case "addsib": if (n != null) { selected = n; AddSibling(); } return;
                    case "expand": if (n != null) { Snapshot(); n.Collapsed = false; selected = n; Changed(); } return;
                    case "todo": if (n != null) { selected = n; ToggleTodo(n); } return;
                    case "link": if (n != null) { selected = n; Render(); OpenLink(n.Link); } return;
                    case "note": if (n != null) { selected = n; Render(); EditNote(n); } return;
                    case "callout": if (n != null) { selected = n; Render(); if (e.ClickCount == 2) EditCallout(n); } return;
                    case "summary": if (n != null) { selected = n; Render(); if (e.ClickCount == 2) EditSummary(n); } return;
                    case "boundary": if (n != null) { selected = n; Render(); if (e.ClickCount == 2) EditBoundaryLabel(n); } return;
                    case "rel": if (r != null && e.ClickCount == 2) { var t = Dialogs.Prompt(this, "Relation", "Texte de la relation", r.Label ?? "", false); if (t != null) { Snapshot(); r.Label = t.Trim(); Changed(); } } return;
                }
                return;
            }
            var node = NodeAt(e.OriginalSource);
            if (node != null)
            {
                if (e.ClickCount == 2) { selected = node; Render(); BeginEdit(node); e.Handled = true; return; }
                selected = node; Render();
                dragNode = node; dragging = false; mouseStart = p;
                Border v; grabInView = views.TryGetValue(node, out v) ? e.GetPosition(v) : new Point();
                viewport.CaptureMouse();
                e.Handled = true;
                return;
            }
            if (e.ClickCount == 2) { AddFloating(ToCanvas(p), true); e.Handled = true; return; }
            panning = true; mouseStart = p; panStart = new Point(pan.X, pan.Y);
            viewport.CaptureMouse();
            viewport.Cursor = Cursors.SizeAll;
        }

        void ViewportMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(viewport);
            if (panning)
            {
                pan.X = panStart.X + (p.X - mouseStart.X); pan.Y = panStart.Y + (p.Y - mouseStart.Y);
                PlaceToolbar();
                return;
            }
            if (dragNode == null || e.LeftButton != MouseButtonState.Pressed) return;
            if (!dragging)
            {
                if (Math.Abs(p.X - mouseStart.X) < 6 && Math.Abs(p.Y - mouseStart.Y) < 6) return;
                if (dragNode == map.Root) return;
                dragging = true; dragMoved = false;
                dragSnapshot = MindJson.ToText(map);
                var p0 = ParentOf(dragNode);
                dragOrigParent = p0;
                previewParent = p0; previewIndex = p0 != null ? p0.Children.IndexOf(dragNode) : -1; previewSide = dragNode.Side; previewFloating = p0 == null;
                Border v; if (views.TryGetValue(dragNode, out v)) ghost.Start(this, v, new Point(grabInView.X * scale.ScaleX, grabInView.Y * scale.ScaleY), true);
                toolbar.Visibility = Visibility.Collapsed;
            }
            ghost.Move(DragGhost.ScreenDip(this, e));
            PreviewDrag(p);
        }

        /// <summary>
        /// Aperçu en direct (XMind) : le sujet est provisoirement déplacé dans le modèle à l'endroit où il serait lâché
        /// (sous le sujet survolé, de l'autre côté du centre, ou flottant) et la carte se réorganise aussitôt.
        /// </summary>
        void PreviewDrag(Point p)
        {
            var node = dragNode;
            if (node == null) return;
            var cp = ToCanvas(p);
            var hit = viewport.InputHitTest(p) as DependencyObject;
            var target = hit != null ? NodeAt(hit) : null;
            if (target == node || (target != null && IsDescendant(target, node))) target = null;
            // sur un sujet : rattachement ; sur le fond : un sujet principal (à l'origine ou actuellement) change de côté
            // ou de rang autour du centre, tout autre sujet devient flottant
            MindNode parent; int side = 0; bool floating = false;
            if (target != null) parent = target;
            else if (!previewFloating && (previewParent == RootNode || dragOrigParent == RootNode)) parent = RootNode;
            else { parent = null; floating = true; }
            Box tb;
            if (parent == RootNode && SidedLayout && boxes.TryGetValue(RootNode, out tb))
                side = (map.Layout == 5 || map.Layout == 6) ? (cp.Y < tb.Y + tb.H / 2 ? 1 : -1) : (cp.X < tb.X + tb.W / 2 ? -1 : 1);
            // retrait de la position provisoire courante, puis calcul du rang d'insertion
            var cur = ParentOf(node);
            if (floating)
            {
                var tl = ToCanvas(new Point(p.X - grabInView.X * scale.ScaleX, p.Y - grabInView.Y * scale.ScaleY));
                if (!previewFloating) { if (cur != null) cur.Children.Remove(node); else map.Floating.Remove(node); map.Floating.Add(node); node.Side = 0; previewFloating = true; previewParent = null; dragMoved = true; }
                if (Math.Abs(node.X - tl.X) > 2 || Math.Abs(node.Y - tl.Y) > 2) { node.X = tl.X; node.Y = tl.Y; dragMoved = true; Render(); }
                return;
            }
            int idx = InsertIndex(parent, node, cp, side);
            int curIdx = cur == parent ? parent.Children.IndexOf(node) : -1;
            if (curIdx >= 0 && idx > curIdx) idx--;
            if (!previewFloating && parent == previewParent && idx == previewIndex && side == previewSide) return;
            if (cur != null) cur.Children.Remove(node); else map.Floating.Remove(node);
            if (idx > parent.Children.Count) idx = parent.Children.Count;
            parent.Children.Insert(idx, node);
            parent.Collapsed = false;
            node.Side = side;
            previewParent = parent; previewIndex = idx; previewSide = side; previewFloating = false; dragMoved = true;
            Render();
        }

        /// <summary>Échap pendant un glisser : retour à l'état de départ.</summary>
        void CancelDrag()
        {
            if (!dragging) return;
            string snap = dragSnapshot;
            EndMouse();
            if (snap != null)
            {
                var restored = MindJson.FromText(snap);
                restored.Zoom = map.Zoom; restored.PanX = map.PanX; restored.PanY = map.PanY;
                store.Maps[store.Maps.IndexOf(map)] = restored; map = restored;
                RemapRefs(selected != null ? selected.Id : null, focus != null ? focus.Id : null);
                Render();
            }
        }

        void ViewportUp(object sender, MouseButtonEventArgs e)
        {
            bool did = dragging, moved = dragMoved; var node = dragNode; string snap = dragSnapshot;
            EndMouse();
            if (!did || node == null) return;
            // l'aperçu est déjà l'état final : il ne reste qu'à l'enregistrer (et à garder l'état de départ pour Ctrl+Z)
            if (moved && snap != null) { undo.Add(snap); if (undo.Count > 50) undo.RemoveAt(0); redo.Clear(); }
            selected = node;
            Changed();
            EnsureVisible(node);
        }

        void EndMouse()
        {
            ghost.End();
            if (panning) { panning = false; SaveView(); }
            viewport.Cursor = relFrom != null ? Cursors.Cross : Cursors.Arrow;
            dragNode = null; dragging = false; dragOrigParent = null; dragSnapshot = null; dragMoved = false; previewParent = null;
            if (viewport.IsMouseCaptured) viewport.ReleaseMouseCapture();
        }

        void ViewportRight(object sender, MouseButtonEventArgs e)
        {
            if (editing != null) EndEdit(true);
            if (relFrom != null) { CancelRelation(); e.Handled = true; return; }
            var rel = Ui.HitTag(e.OriginalSource, "rel");
            if (rel != null) { var r = rel.DataContext as MindRelation; if (r != null) ShowRelationMenu(r); e.Handled = true; return; }
            var node = NodeAt(e.OriginalSource);
            if (node != null) { selected = node; Render(); ShowNodeMenu(node); }
            else ShowBackgroundMenu(ToCanvas(e.GetPosition(viewport)));
            e.Handled = true;
        }

        void ViewportWheel(object sender, MouseWheelEventArgs e)
        {
            Zoom(e.Delta > 0 ? 1.12 : 1 / 1.12, e.GetPosition(viewport));
            e.Handled = true;
        }

        void Zoom(double factor, Point around)
        {
            double old = scale.ScaleX;
            double z = Math.Max(0.3, Math.Min(3, old * factor));
            pan.X = around.X - (around.X - pan.X) * z / old;
            pan.Y = around.Y - (around.Y - pan.Y) * z / old;
            scale.ScaleX = scale.ScaleY = z;
            SaveView();
        }

        // ---- édition en place ----------------------------------------------------------------------------------------

        /// <summary>Saisie dans le sujet lui-même : même forme et mêmes couleurs, taille qui suit le texte (MindNode, Whimsical).</summary>
        void BeginEdit(MindNode n)
        {
            if (n == null || !boxes.ContainsKey(n)) return;
            if (editing != null && editing != n) EndEdit(true);
            if (!boxes.ContainsKey(n)) return;
            editNew = false;
            editing = n;
            editor.Text = n.Text ?? "";
            Render();
            EnsureVisible(n);
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(delegate { if (editing == n) { editor.Focus(); Keyboard.Focus(editor); editor.SelectAll(); } }));
        }

        /// <summary>Taper directement sur un sujet sélectionné remplace son texte (XMind, Visio).</summary>
        void OnTextInput(object sender, TextCompositionEventArgs e)
        {
            if (editing != null || selected == null || relFrom != null) return;
            if (Keyboard.FocusedElement is TextBox) return;
            bool mc = (Keyboard.Modifiers & ModifierKeys.Control) != 0, ma = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
            if (mc != ma) return;   // Ctrl seul ou Alt seul : raccourci ; Ctrl+Alt (AltGr) : caractère légitime
            string t = e.Text;
            if (string.IsNullOrEmpty(t) || char.IsWhiteSpace(t[0]) || char.IsControl(t[0])) return;
            var n = selected;
            editing = n; editNew = false;
            editor.Text = t;
            Render();
            EnsureVisible(n);
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(delegate { if (editing == n) { editor.Focus(); Keyboard.Focus(editor); editor.CaretIndex = editor.Text.Length; } }));
            e.Handled = true;
        }

        /// <summary>Pendant la saisie, le sujet s'élargit avec son texte sans recalculer toute la carte.</summary>
        void OnEditorTextChanged()
        {
            if (editing == null || editOuter == null || editBox == null) return;
            var b = editBox;
            var sz = MeasureText(editor.Text.Length == 0 ? "Nouveau sujet" : editor.Text, NodeFontSize(b.N, b.Depth), (b.N.Bold || b.Depth == 0) ? FontWeights.SemiBold : FontWeights.Normal, b.N.Italic ? FontStyles.Italic : FontStyles.Normal, 240);
            double w = Math.Max(sz.Width + b.Extra + b.PadX * 2, b.MinW), h = sz.Height + (b.LabelsH > 0 ? b.LabelsH + 3 : 0) + b.PadY * 2;
            editor.MinWidth = Math.Max(40, sz.Width + 2);
            editOuter.Width = editWrap.Width = w; editOuter.Height = editWrap.Height = h;
            double left = b.X;
            if (HorizontalEdges && !b.Floating && b.Depth > 0 && b.Side < 0) left = b.X + b.W - w;
            else if (!HorizontalEdges && !StackedLayout) left = b.X + b.W / 2 - w / 2;
            Canvas.SetLeft(editWrap, left);
            var ph = editOuter.Tag as TextBlock;
            if (ph != null) ph.Visibility = editor.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        void EndEdit(bool commit)
        {
            if (editing == null) return;
            var n = editing;
            string t = editor.Text.Trim();
            bool wasNew = editNew;
            editing = null; editNew = false; editOuter = null; editWrap = null; editBox = null;
            var host = editor.Parent as Panel;
            if (host != null) host.Children.Remove(editor);
            bool empty = t.Length == 0, bare = n.Children.Count == 0 && n.Markers.Count == 0 && n.Labels.Count == 0 && string.IsNullOrWhiteSpace(n.Note) && string.IsNullOrWhiteSpace(n.Link);
            if (n != map.Root && bare && (empty && (commit || wasNew) || (!commit && wasNew)))
            {
                // sujet laissé vide : retiré sans confirmation (Whimsical, MindNode)
                if (!wasNew) Snapshot(); else if (undo.Count > 0) undo.RemoveAt(undo.Count - 1);   // sujet existant vidé : annulable
                var p = ParentOf(n) ?? RealParentOf(n);
                if (p != null) { int idx = p.Children.IndexOf(n); p.Children.Remove(n); selected = idx > 0 ? p.Children[idx - 1] : p; } else { map.Floating.Remove(n); selected = map.Root; }
                Changed();
            }
            else if (commit && t != (n.Text ?? "")) { if (!wasNew) Snapshot(); n.Text = t; Changed(); }
            else Render();
            viewport.Focus();
        }

        // ---- clavier (raccourcis XMind) -----------------------------------------------------------------------------

        void OnKey(object sender, KeyEventArgs e)
        {
            if (editing != null) return;
            if (Keyboard.FocusedElement is TextBox) return;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0, shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0, alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
            bool slash = key == Key.Oem2 || key == Key.OemQuestion || key == Key.Divide;
            if (ctrl && alt && !slash) return;   // AltGr (Ctrl+Alt sur clavier français) : caractère spécial, pas un raccourci
            switch (key)
            {
                case Key.Tab: AddChild(); break;
                case Key.Enter: if (ctrl) InsertParent(); else if (selected == RootNode || ParentOf(selected) == null) AddChild(); else AddSibling(); break;
                case Key.Space: case Key.F2: if (selected != null) BeginEdit(selected); break;
                case Key.Delete: case Key.Back: if (ctrl) DeleteSingle(); else DeleteSelected(); break;
                case Key.Escape: if (dragging) CancelDrag(); else if (relFrom != null) CancelRelation(); else if (searchText.Length > 0) { searchText = ""; searchHits.Clear(); Render(); } else return; break;
                case Key.F3: NextHit(); break;
                case Key.Z: if (ctrl && shift) Redo(); else if (ctrl) Undo(); else return; break;
                case Key.Y: if (ctrl) Redo(); else return; break;
                case Key.D: if (ctrl) DuplicateSelected(); else return; break;
                case Key.C: if (ctrl) CopySelected(); else return; break;
                case Key.V: if (ctrl) PasteIntoSelected(); else return; break;
                case Key.F: if (ctrl) Find(); else return; break;
                case Key.K: if (ctrl && selected != null) EditLink(selected); else return; break;
                case Key.N: if (ctrl && shift && selected != null) EditNote(selected); else return; break;
                case Key.B: if (ctrl) ToggleBoundary(selected); else return; break;
                case Key.R: if (ctrl && shift) BeginRelation(selected); else if (ctrl) GoToRoot(); else return; break;
                case Key.OemSemicolon: case Key.OemComma: case Key.OemPeriod: if (ctrl) SetFocus(focus != null ? null : selected); else return; break;
                case Key.D0: case Key.NumPad0: if (ctrl) Fit(); else return; break;
                case Key.OemPlus: case Key.Add: if (ctrl) Zoom(1.2, new Point(viewport.ActualWidth / 2, viewport.ActualHeight / 2)); else return; break;
                case Key.OemMinus: case Key.Subtract: if (ctrl) Zoom(1 / 1.2, new Point(viewport.ActualWidth / 2, viewport.ActualHeight / 2)); else return; break;
                case Key.Up: if (ctrl || alt) MoveSelected(-1); else Navigate(0, -1); break;
                case Key.Down: if (ctrl || alt) MoveSelected(1); else Navigate(0, 1); break;
                case Key.Left: Navigate(-1, 0); break;
                case Key.Right: Navigate(1, 0); break;
                default:
                    if (slash && ctrl && alt) ToggleFoldAll();
                    else if (slash && ctrl) ToggleFold(selected);
                    else return;
                    break;
            }
            e.Handled = true;
        }

        void Navigate(int dx, int dy)
        {
            if (selected == null) { selected = RootNode; Render(); return; }
            Box b; if (!boxes.TryGetValue(selected, out b)) return;
            MindNode next = null;
            if (dy != 0)
            {
                var p = ParentOf(selected);
                if (p != null) { int i = p.Children.IndexOf(selected) + dy; if (i >= 0 && i < p.Children.Count) next = p.Children[i]; }
            }
            else if (!HorizontalEdges || b.Floating)
            {
                if (dx > 0) { if (!selected.Collapsed && selected.Children.Count > 0) next = selected.Children[0]; }
                else next = ParentOf(selected);
            }
            else if (selected == RootNode) next = RootNode.Children.FirstOrDefault(ch => boxes.ContainsKey(ch) && boxes[ch].Side == dx);
            else
            {
                bool toChild = dx == b.Side;
                if (toChild) { if (!selected.Collapsed && selected.Children.Count > 0) next = selected.Children[selected.Children.Count / 2]; }
                else next = ParentOf(selected);
            }
            if (next != null) { selected = next; Render(); EnsureVisible(next); }
        }

        // ---- menus -----------------------------------------------------------------------------------------------------

        MenuItem Choice(string header, string[] names, int current, Action<int> pick)
        {
            var m = new MenuItem { Header = header };
            for (int i = 0; i < names.Length; i++) { int v = i; m.Items.Add(Check(names[i], current == i, delegate { pick(v); })); }
            return m;
        }

        void Open(ContextMenu menu, UIElement anchor)
        {
            if (anchor != null) { menu.PlacementTarget = anchor; menu.Placement = PlacementMode.Bottom; }
            else { menu.PlacementTarget = this; menu.Placement = PlacementMode.MousePoint; }
            menu.IsOpen = true;
        }

        static readonly int[] StructureOrder = { 0, 1, 3, 2, 7, 4, 5, 8, 6 };

        /// <summary>Entrées « Insérer » de XMind, appliquées au sujet sélectionné.</summary>
        void FillInsert(ItemsControl menu, MindNode n, bool withTopics)
        {
            if (withTopics)
            {
                menu.Items.Add(Item("Sous-sujet  (Tab ou « + »)", "IcoPlus", AddChild));
                if (n != map.Root && ParentOf(n) != null) menu.Items.Add(Item("Sujet frère  (Entrée)", "IcoPlus", AddSibling));
            }
            if (n != map.Root && ParentOf(n) != null) menu.Items.Add(Item("Sujet parent  (Ctrl+Entrée)", "IcoPlus", InsertParent));
            menu.Items.Add(Item("Sujet flottant  (double-clic sur le fond)", null, delegate { AddFloating(ToCanvas(new Point(viewport.ActualWidth / 2, viewport.ActualHeight / 2)), true); }));
            menu.Items.Add(new Separator());
            menu.Items.Add(MarkerMenu(n));
            menu.Items.Add(Item(n.Labels.Count > 0 ? "Étiquettes…  (" + string.Join(", ", n.Labels) + ")" : "Étiquettes…", "IcoTag", delegate { EditLabels(n); }));
            menu.Items.Add(Item(string.IsNullOrWhiteSpace(n.Note) ? "Note…  (Ctrl+Maj+N)" : "Modifier la note…  (Ctrl+Maj+N)", "IcoNotes", delegate { EditNote(n); }));
            if (string.IsNullOrWhiteSpace(n.Link)) menu.Items.Add(Item("Lien…  (Ctrl+K)", "IcoExternal", delegate { EditLink(n); }));
            else
            {
                var lm = new MenuItem { Header = "Lien  (" + n.Link + ")", Icon = Theme.Icon("IcoExternal", 14) };
                lm.Items.Add(Item("Ouvrir", "IcoExternal", delegate { OpenLink(n.Link); }));
                lm.Items.Add(Item("Modifier…  (Ctrl+K)", "IcoPencil", delegate { EditLink(n); }));
                lm.Items.Add(Item("Retirer", "IcoTrash", delegate { Snapshot(); n.Link = null; Changed(); }));
                menu.Items.Add(lm);
            }
            menu.Items.Add(Check("Case à faire", n.Todo != 0, delegate { Snapshot(); n.Todo = n.Todo == 0 ? 1 : 0; Changed(); }));
            menu.Items.Add(Item(string.IsNullOrWhiteSpace(n.Callout) ? "Légende…" : "Modifier la légende…", "IcoComment", delegate { EditCallout(n); }));
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Relation vers un autre sujet…  (Ctrl+Maj+R)", null, delegate { BeginRelation(n); }));
            if (n != map.Root)
            {
                menu.Items.Add(Check("Limite autour de la branche  (Ctrl+B)", n.Boundary, delegate { ToggleBoundary(n); }));
                if (n.Boundary) menu.Items.Add(Item("Texte de la limite…", "IcoPencil", delegate { EditBoundaryLabel(n); }));
            }
            if (n.Children.Count > 0) menu.Items.Add(Item(string.IsNullOrWhiteSpace(n.Summary) ? "Résumé des sous-sujets…" : "Modifier le résumé…", null, delegate { EditSummary(n); }));
        }

        void ShowInsertMenu(UIElement anchor)
        {
            var menu = new ContextMenu();
            FillInsert(menu, selected ?? map.Root, true);
            Open(menu, anchor);
        }

        void ShowNodeMenu(MindNode n)
        {
            var menu = new ContextMenu();
            menu.Items.Add(Item("Modifier le texte  (double-clic, ou tapez directement)", "IcoPencil", delegate { BeginEdit(n); }));
            menu.Items.Add(Item("Sous-sujet  (Tab ou « + »)", "IcoPlus", AddChild));
            if (n != map.Root && ParentOf(n) != null) menu.Items.Add(Item("Sujet frère  (Entrée)", "IcoPlus", AddSibling));
            menu.Items.Add(new Separator());
            var ins = new MenuItem { Header = "Insérer", Icon = Theme.Icon("IcoPlus", 14) };
            FillInsert(ins, n, false);
            menu.Items.Add(ins);
            var sty = new MenuItem { Header = "Style du sujet", Icon = Theme.Icon("IcoPalette", 14) };
            var quick = new MenuItem { Header = "Style rapide" };
            quick.Items.Add(Item("Très important", null, delegate { QuickStyle(n, 2); }));
            quick.Items.Add(Item("Important", null, delegate { QuickStyle(n, 1); }));
            quick.Items.Add(Item("Barré", null, delegate { QuickStyle(n, 3); }));
            quick.Items.Add(Item("Par défaut", null, delegate { QuickStyle(n, 0); }));
            sty.Items.Add(quick);
            var col = new MenuItem { Header = "Couleur de la branche" };
            col.Items.Add(Check(n == map.Root ? "Bleu Pupitre" : "Héritée / automatique", n.Color == 0, delegate { Snapshot(); n.Color = 0; Changed(); }));
            foreach (var i in TaskColors.Indices)
            {
                int v = i;
                var mi = Check(Palette.Names[i], n.Color == i, delegate { Snapshot(); n.Color = v; Changed(); });
                mi.Icon = new Border { Width = 11, Height = 11, CornerRadius = new CornerRadius(6), Background = TaskColors.Solid(i) };
                col.Items.Add(mi);
            }
            col.Items.Add(new Separator());
            col.Items.Add(Check(n.Color < 0 ? "Personnalisée (" + n.CustomColor + ")…" : "Personnalisée…", n.Color < 0, delegate
            {
                string hex = TasksWindow.PickCustomColor(this, boxes.ContainsKey(n) ? boxes[n].Color : Palette.Get(1));
                if (hex != null) { Snapshot(); n.Color = -1; n.CustomColor = hex; Changed(); }
            }));
            sty.Items.Add(col);
            sty.Items.Add(Choice("Forme", ShapeNames, n.Shape, delegate(int v) { Snapshot(); n.Shape = v; Changed(); }));
            sty.Items.Add(Choice("Taille du texte", SizeNames, n.Size, delegate(int v) { Snapshot(); n.Size = v; Changed(); }));
            sty.Items.Add(Check("Gras", n.Bold, delegate { Snapshot(); n.Bold = !n.Bold; Changed(); }));
            sty.Items.Add(Check("Italique", n.Italic, delegate { Snapshot(); n.Italic = !n.Italic; Changed(); }));
            sty.Items.Add(Check("Barré", n.Strike, delegate { Snapshot(); n.Strike = !n.Strike; Changed(); }));
            if (ParentOf(n) == RootNode && (map.Layout == 0 || map.Layout == 5 || map.Layout == 6 || map.Layout == 8))
            {
                bool upDown = map.Layout == 5 || map.Layout == 6;
                var side = new MenuItem { Header = upDown ? "Position par rapport à l'axe" : "Côté" };
                side.Items.Add(Check("Automatique", n.Side == 0, delegate { Snapshot(); n.Side = 0; Changed(); }));
                side.Items.Add(Check(upDown ? "Au-dessus" : "Droite", n.Side == 1, delegate { Snapshot(); n.Side = 1; Changed(); }));
                side.Items.Add(Check(upDown ? "En dessous" : "Gauche", n.Side == -1, delegate { Snapshot(); n.Side = -1; Changed(); }));
                sty.Items.Add(side);
            }
            menu.Items.Add(sty);
            var org = new MenuItem { Header = "Organiser", Icon = Theme.Icon("IcoList", 14) };
            if (n.Children.Count > 0) org.Items.Add(Item(n.Collapsed ? "Déplier  (Ctrl+/)" : "Replier  (Ctrl+/)", null, delegate { ToggleFold(n); }));
            if (n != map.Root && n.Children.Count > 0) org.Items.Add(Item(focus == n ? "Afficher toute la carte  (Ctrl+;)" : "Afficher cette branche seule  (Ctrl+;)", "IcoEye", delegate { SetFocus(focus == n ? null : n); }));
            if (n != map.Root && ParentOf(n) != null)
            {
                org.Items.Add(Item("Monter  (Alt+↑)", null, delegate { MoveSelected(-1); }));
                org.Items.Add(Item("Descendre  (Alt+↓)", null, delegate { MoveSelected(1); }));
            }
            org.Items.Add(Item("Nouvelle feuille à partir de ce sujet", "IcoGridLayout", delegate { NewMapFromNode(n); }));
            menu.Items.Add(org);
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Copier la branche  (Ctrl+C)", "IcoCopy", CopySelected));
            menu.Items.Add(Item("Coller comme sous-sujets  (Ctrl+V)", null, PasteIntoSelected));
            if (n != map.Root) menu.Items.Add(Item("Dupliquer  (Ctrl+D)", null, DuplicateSelected));
            if (n != map.Root)
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(Item("Supprimer…  (Suppr)", "IcoTrash", DeleteSelected));
                if (n.Children.Count > 0) menu.Items.Add(Item("Supprimer le sujet seul, garder ses sous-sujets  (Ctrl+Suppr)", null, DeleteSingle));
            }
            Open(menu, null);
        }

        void ShowBackgroundMenu(Point at)
        {
            var menu = new ContextMenu();
            if (undo.Count > 0) menu.Items.Add(Item("Annuler  (Ctrl+Z)", "IcoRefresh", Undo));
            if (redo.Count > 0) menu.Items.Add(Item("Rétablir  (Ctrl+Y)", "IcoRefresh", Redo));
            if (undo.Count > 0 || redo.Count > 0) menu.Items.Add(new Separator());
            menu.Items.Add(Item("Sujet flottant ici  (double-clic)", "IcoPlus", delegate { AddFloating(at, true); }));
            menu.Items.Add(Item("Sous-sujet du sujet central", "IcoPlus", delegate { selected = RootNode; AddChild(); }));
            menu.Items.Add(Item("Coller un plan ici (sous-sujets du sujet central)  (Ctrl+V)", null, delegate { selected = RootNode; PasteIntoSelected(); }));
            menu.Items.Add(new Separator());
            FillStyle(menu);
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Rechercher dans la carte…  (Ctrl+F, F3 : suivant)", "IcoSearch", Find));
            menu.Items.Add(Item("Ajuster à la fenêtre  (Ctrl+0)", "IcoRefresh", Fit));
            menu.Items.Add(Item("Aller au sujet central  (Ctrl+R)", null, GoToRoot));
            if (focus != null) menu.Items.Add(Item("Afficher toute la carte  (Ctrl+;)", "IcoEye", delegate { SetFocus(null); }));
            Open(menu, null);
        }

        void FillStyle(ItemsControl menu)
        {
            var structure = new MenuItem { Header = "Structure" };
            foreach (var i in StructureOrder) { int v = i; structure.Items.Add(Check(StructureNames[i], map.Layout == i, delegate { Snapshot(); map.Layout = v; Changed(); Fit(); })); }
            menu.Items.Add(structure);
            menu.Items.Add(Choice("Thème", ThemeNames, map.Theme, delegate(int v) { Snapshot(); map.Theme = v; Changed(); }));
            menu.Items.Add(Choice("Palette des branches", PaletteNames, map.Palette, delegate(int v) { Snapshot(); map.Palette = v; Changed(); }));
            menu.Items.Add(Check("Branches colorées (arc-en-ciel)", !map.Mono, delegate { Snapshot(); map.Mono = !map.Mono; Changed(); }));
            menu.Items.Add(Check("Lignes effilées", map.Tapered, delegate { Snapshot(); map.Tapered = !map.Tapered; Changed(); }));
            menu.Items.Add(Choice("Forme des sujets", MapShapeNames, Math.Max(0, map.Shape - 1), delegate(int v) { Snapshot(); map.Shape = v + 1; Changed(); }));
            menu.Items.Add(Choice("Lignes", LineNames, map.LineStyle, delegate(int v) { Snapshot(); map.LineStyle = v; Changed(); }));
            menu.Items.Add(Choice("Épaisseur des lignes", WidthNames, map.LineWidth, delegate(int v) { Snapshot(); map.LineWidth = v; Changed(); }));
            menu.Items.Add(Choice("Espacement", SpacingNames, map.Spacing, delegate(int v) { Snapshot(); map.Spacing = v; Changed(); }));
            var num = Choice("Numérotation", NumberNames, map.NumberStyle, delegate(int v) { Snapshot(); map.NumberStyle = v; Changed(); });
            num.Items.Add(new Separator());
            num.Items.Add(Check("Hiérarchique (1.2.1)", map.Tiered, delegate { Snapshot(); map.Tiered = !map.Tiered; Changed(); }));
            menu.Items.Add(num);
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Tout déplier", null, delegate { Snapshot(); foreach (var n in AllNodes()) n.Collapsed = false; Changed(); }));
            menu.Items.Add(Item("Replier au premier niveau  (Ctrl+Alt+/)", null, delegate { Snapshot(); foreach (var ch in RootNode.Children) ch.Collapsed = ch.Children.Count > 0; Changed(); }));
        }

        string OutlineAll()
        {
            var sb = new System.Text.StringBuilder(Outline(map.Root, 0));
            foreach (var f in map.Floating) sb.Append(Outline(f, 0));
            return sb.ToString();
        }

        string MarkdownAll()
        {
            var sb = new System.Text.StringBuilder(Markdown(map.Root));
            foreach (var f in map.Floating) { sb.AppendLine(); MarkdownRec(sb, f, 0); }
            return sb.ToString();
        }

        void ShowExportMenu(UIElement anchor)
        {
            var menu = new ContextMenu();
            menu.Items.Add(Item("Enregistrer en image (PNG)…", "IcoCopy", delegate { ExportPng(false); }));
            menu.Items.Add(Item("Copier l'image", "IcoCopy", delegate { ExportPng(true); }));
            menu.Items.Add(Item("Copier le plan (texte indenté)", "IcoList", delegate { try { Clipboard.SetText(OutlineAll()); c.Toast("Plan copié dans le presse-papiers"); } catch { } }));
            menu.Items.Add(Item("Copier en Markdown", "IcoList", delegate { try { Clipboard.SetText(MarkdownAll()); c.Toast("Markdown copié dans le presse-papiers"); } catch { } }));
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Importer un plan ou du Markdown…", "IcoPlus", ImportOutline));
            Open(menu, anchor);
        }

        // ---- panneau ---------------------------------------------------------------------------------------------------

        /// <summary>Enregistre tout de suite une modification en attente (sortie de l'application).</summary>
        public void Flush() { saveDeb.Flush(); }

        protected override bool CanRename { get { return false; } }
        protected override string SettingsTooltip { get { return "Paramètres du panneau (couleur, opacité, verrou)"; } }
        protected override Size DefaultSize { get { return new Size(760, 560); } }
        protected override void OnOpenSettings() { c.EditPanel(this, c.L.MindPanel, "Cartes mentales"); }
        protected override bool OwnLocked { get { return c.L.MindPanel.Locked; } }

        protected override void SaveRect()
        {
            var t = c.L.MindPanel;
            t.X = Left; t.Y = Top; t.W = Width; t.H = RestoreHeight; t.Rolled = IsRolled; t.RolledH = RestoreHeight; t.Set = true;
            string sc; double rx, ry; CurrentScreen(out sc, out rx, out ry); t.Screen = sc; t.RelX = rx; t.RelY = ry;
            c.SaveLayoutSoon();
        }
    }
}
