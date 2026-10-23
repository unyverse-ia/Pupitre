using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Pupitre
{
    /// <summary>
    /// Cartes mentales : volet « Style » à gauche, façon panneau de format XMind. Chaque réglage est une vignette
    /// dessinée en vectoriel (structure, thème, palette, forme, lignes, épaisseur, espacement, numérotation), plus une
    /// section pour le sujet sélectionné (couleur, forme, taille, gras / italique / barré, styles rapides).
    /// </summary>
    public partial class MindWindow
    {
        Border stylePanel;
        StackPanel styleStack, nodeSection;
        readonly List<Thumb> thumbs = new List<Thumb>();
        const double ThumbW = 62, ThumbH = 44;

        class Thumb { public Border Box; public Canvas Cv; public string Kind; public int Value; public Action<Canvas, bool> Draw; }

        Border BuildStylePanel()
        {
            styleStack = new StackPanel { Margin = new Thickness(8, 4, 6, 12) };

            var structures = new WrapPanel();
            foreach (var i in StructureOrder) { int v = i; AddThumb(structures, "structure", v, ShortStructure(v), StructureNames[v], delegate(Canvas cv, bool sel) { DrawStructure(cv, v, sel); }, delegate { Snapshot(); map.Layout = v; Changed(); Fit(); }); }
            Section("Structure", structures);

            var themes = new WrapPanel();
            for (int i = 0; i < ThemeNames.Length; i++) { int v = i; AddThumb(themes, "theme", v, ThemeNames[v].Split(' ')[0], ThemeNames[v], delegate(Canvas cv, bool sel) { DrawTheme(cv, v); }, delegate { Snapshot(); map.Theme = v; Changed(); }); }
            Section("Thème", themes);

            var palettes = new WrapPanel();
            for (int i = 0; i < PaletteNames.Length; i++) { int v = i; AddThumb(palettes, "palette", v, PaletteNames[v], "Palette " + PaletteNames[v], delegate(Canvas cv, bool sel) { DrawPalette(cv, v); }, delegate { Snapshot(); map.Palette = v; Changed(); }); }
            var pToggles = new WrapPanel();
            pToggles.Children.Add(ToggleChip("mono", 0, "Branches colorées", delegate { Snapshot(); map.Mono = !map.Mono; Changed(); }));
            pToggles.Children.Add(ToggleChip("tapered", 1, "Lignes effilées", delegate { Snapshot(); map.Tapered = !map.Tapered; Changed(); }));
            var palBlock = new StackPanel(); palBlock.Children.Add(palettes); palBlock.Children.Add(pToggles);
            Section("Couleurs des branches", palBlock);

            var shapes = new WrapPanel();
            for (int i = 0; i < MapShapeNames.Length; i++) { int v = i + 1; AddThumb(shapes, "shape", v, MapShapeNames[i], "Forme des sujets : " + MapShapeNames[i], delegate(Canvas cv, bool sel) { DrawShape(cv, v, 34, 16); }, delegate { Snapshot(); map.Shape = v; Changed(); }); }
            Section("Forme des sujets", shapes);

            var lines = new WrapPanel();
            for (int i = 0; i < LineNames.Length; i++) { int v = i; AddThumb(lines, "line", v, LineNames[v], "Lignes " + LineNames[v].ToLowerInvariant(), delegate(Canvas cv, bool sel) { DrawLineStyle(cv, v); }, delegate { Snapshot(); map.LineStyle = v; Changed(); }); }
            for (int i = 0; i < WidthNames.Length; i++) { int v = i; AddThumb(lines, "width", v, WidthNames[v], "Épaisseur : " + WidthNames[v].ToLowerInvariant(), delegate(Canvas cv, bool sel) { DrawLineWidth(cv, v); }, delegate { Snapshot(); map.LineWidth = v; Changed(); }); }
            Section("Lignes", lines);

            var spacing = new WrapPanel();
            for (int i = 0; i < SpacingNames.Length; i++) { int v = i; AddThumb(spacing, "spacing", v, SpacingNames[v], "Espacement " + SpacingNames[v].ToLowerInvariant(), delegate(Canvas cv, bool sel) { DrawSpacing(cv, v); }, delegate { Snapshot(); map.Spacing = v; Changed(); }); }
            Section("Espacement", spacing);

            var numbers = new WrapPanel();
            string[] numSamples = { "—", "1 2 3", "a b c", "i ii iii" };
            for (int i = 0; i < NumberNames.Length; i++) { int v = i; AddThumb(numbers, "number", v, i == 0 ? "Aucune" : NumberNames[i].Split(' ')[0], NumberNames[i], delegate(Canvas cv, bool sel) { DrawText(cv, numSamples[v], 12); }, delegate { Snapshot(); map.NumberStyle = v; Changed(); }); }
            var nToggles = new WrapPanel();
            nToggles.Children.Add(ToggleChip("tiered", 1, "Hiérarchique (1.2.1)", delegate { Snapshot(); map.Tiered = !map.Tiered; Changed(); }));
            var numBlock = new StackPanel(); numBlock.Children.Add(numbers); numBlock.Children.Add(nToggles);
            Section("Numérotation", numBlock);

            nodeSection = new StackPanel();
            styleStack.Children.Add(nodeSection);

            optionsView = new ScrollViewer { Content = styleStack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Focusable = false };
            mapsView = BuildMapList();
            var tabs = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 8, 6, 2) };
            tabMaps = SideTab("Cartes", 0); tabOptions = SideTab("Options", 1);
            tabs.Children.Add(tabMaps); tabs.Children.Add(tabOptions);
            DockPanel.SetDock(tabs, Dock.Top);
            var views = new Grid(); views.Children.Add(mapsView); views.Children.Add(optionsView);
            var host = new DockPanel(); host.Children.Add(tabs); host.Children.Add(views);
            stylePanel = new Border { Width = 300, Background = Theme.Brush(0x2E, Colors.Black), BorderBrush = Theme.Res<Brush>("Line"), BorderThickness = new Thickness(0, 0, 1, 0), Child = host, Margin = new Thickness(0, 0, 6, 0), CornerRadius = new CornerRadius(0, 0, 0, 10) };
            SetSideTab(c.S.MindTab);
            return stylePanel;
        }

        static string ShortStructure(int layout)
        {
            switch (layout)
            {
                case 0: return "Carte mentale"; case 1: return "Logique →"; case 3: return "← Logique"; case 2: return "Organigramme"; case 7: return "Organigr. ↑";
                case 4: return "Arborescence"; case 5: return "Chronologie"; case 8: return "Chrono. verticale"; default: return "Arête";
            }
        }

        void Section(string title, UIElement content)
        {
            styleStack.Children.Add(new TextBlock { Text = title.ToUpperInvariant(), FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Theme.Res<Brush>("TextFaint"), Margin = new Thickness(3, 12, 0, 5) });
            styleStack.Children.Add(content);
        }

        FrameworkElement AddThumb(Panel into, string kind, int value, string label, string tip, Action<Canvas, bool> draw, Action click)
        {
            var cv = new Canvas { Width = ThumbW - 2, Height = ThumbH - 2, ClipToBounds = true };
            var box = new Border { Width = ThumbW, Height = ThumbH, CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Child = cv, Background = Theme.Argb(0x12FFFFFF), BorderBrush = Theme.Res<Brush>("Line"), SnapsToDevicePixels = true };
            var sp = new StackPanel { Width = 68, Margin = new Thickness(1, 1, 1, 3), Cursor = Cursors.Hand, ToolTip = tip, Background = Brushes.Transparent };
            sp.Children.Add(box);
            if (label != null) sp.Children.Add(new TextBlock { Text = label, FontSize = 9.5, Foreground = Theme.Res<Brush>("TextMuted"), HorizontalAlignment = HorizontalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 2, 0, 0), MaxWidth = 66 });
            sp.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { Activate(); click(); e.Handled = true; };
            sp.MouseEnter += delegate { if (box.BorderThickness.Left < 2) box.Background = Theme.Argb(0x26FFFFFF); };
            sp.MouseLeave += delegate { if (box.BorderThickness.Left < 2) box.Background = Theme.Argb(0x12FFFFFF); };
            thumbs.Add(new Thumb { Box = box, Cv = cv, Kind = kind, Value = value, Draw = draw });
            into.Children.Add(sp);
            return sp;
        }

        /// <summary>Puce à bascule (réglage booléen) ; value : 1 = actif quand le réglage est vrai, 0 = actif quand il est faux.</summary>
        Button ToggleChip(string kind, int value, string text, Action click)
        {
            var b = new Button { Style = Theme.Res<Style>("ChipBtn"), Content = text, Margin = new Thickness(1, 3, 3, 1), Tag = kind + ":" + value, FontSize = 11 };
            b.Click += delegate { click(); };
            return b;
        }

        int StyleValue(string kind)
        {
            switch (kind)
            {
                case "structure": return map.Layout; case "theme": return map.Theme; case "palette": return map.Palette; case "shape": return map.Shape == 0 ? 1 : map.Shape;
                case "line": return map.LineStyle; case "width": return map.LineWidth; case "spacing": return map.Spacing; case "number": return map.NumberStyle;
                case "mono": return map.Mono ? 1 : 0; case "tapered": return map.Tapered ? 1 : 0; case "tiered": return map.Tiered ? 1 : 0;
            }
            return -1;
        }

        FrameworkElement mapsView, optionsView;
        RadioButton tabMaps, tabOptions;

        RadioButton SideTab(string text, int index)
        {
            var rb = new RadioButton { Style = Theme.Res<Style>("SChip"), Content = text, GroupName = "mindside", Margin = new Thickness(0, 0, 6, 0) };
            rb.Checked += delegate { SetSideTab(index); };
            return rb;
        }

        /// <summary>Onglet du volet : 0 = liste des cartes (par défaut), 1 = options de style ; le choix est mémorisé.</summary>
        void SetSideTab(int index)
        {
            if (mapsView == null) return;
            mapsView.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
            optionsView.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
            if (index == 0 && tabMaps.IsChecked != true) tabMaps.IsChecked = true;
            if (index == 1 && tabOptions.IsChecked != true) tabOptions.IsChecked = true;
            if (c.S.MindTab != index) { c.S.MindTab = index; c.SaveSettings(); }
            if (index == 0) RefreshMapList();
        }

        void ToggleStylePanel()
        {
            c.S.MindStyleOpen = !c.S.MindStyleOpen;
            c.SaveSettings();
            UpdateStyleVisibility();
        }

        /// <summary>Le volet s'affiche dès qu'il est demandé ; dans un panneau étroit il se resserre au lieu de disparaître.</summary>
        void UpdateStyleVisibility()
        {
            if (stylePanel == null) return;
            stylePanel.Visibility = c.S.MindStyleOpen ? Visibility.Visible : Visibility.Collapsed;
            stylePanel.Width = double.IsNaN(ActualWidth) || ActualWidth < 10 ? 232 : Math.Min(300, Math.Max(170, ActualWidth * 0.42));
        }

        /// <summary>Met à jour les vignettes (sélection, thème) et reconstruit la section du sujet sélectionné.</summary>
        void RefreshStylePanel()
        {
            if (stylePanel == null) return;
            foreach (var t in thumbs)
            {
                bool sel = StyleValue(t.Kind) == t.Value;
                t.Box.BorderBrush = sel ? Theme.Res<Brush>("Accent") : Theme.Res<Brush>("Line");
                t.Box.BorderThickness = new Thickness(sel ? 2 : 1);
                t.Box.Background = sel ? Theme.Brush(0x3A, Accent) : Theme.Argb(0x12FFFFFF);
                t.Cv.Children.Clear();
                t.Draw(t.Cv, sel);
            }
            foreach (var b in styleStack.Children.OfType<StackPanel>().SelectMany(sp => sp.Children.OfType<WrapPanel>()).SelectMany(w => w.Children.OfType<Button>()).Where(x => x.Tag is string))
            {
                var parts = ((string)b.Tag).Split(':');
                bool on = StyleValue(parts[0]) == int.Parse(parts[1]);
                b.Style = Theme.Res<Style>(on ? "ChipColored" : "ChipBtn");
                if (on) { b.Background = Theme.Res<Brush>("Accent"); b.Foreground = Brushes.White; }
                else b.ClearValue(Control.BackgroundProperty);
                b.Content = (on ? "✓ " : "") + ((string)b.Tag == "mono:0" ? "Branches colorées" : ((string)b.Tag == "tapered:1" ? "Lignes effilées" : "Hiérarchique (1.2.1)"));
            }
            BuildNodeSection();
        }

        void BuildNodeSection()
        {
            nodeSection.Children.Clear();
            var n = selected;
            if (n == null) return;
            nodeSection.Children.Add(new TextBlock { Text = "SUJET SÉLECTIONNÉ", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Theme.Res<Brush>("TextFaint"), Margin = new Thickness(3, 14, 0, 2) });
            nodeSection.Children.Add(new TextBlock { Text = Label(n), FontSize = 11.5, Foreground = Theme.Res<Brush>("TextMuted"), TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(3, 0, 0, 6) });

            // couleur de branche : automatique, teintes, personnalisée
            var colors = new WrapPanel { Margin = new Thickness(2, 0, 0, 4) };
            colors.Children.Add(Swatch(n.Color == 0, Theme.Argb(0x22FFFFFF), n == map.Root ? "Bleu Pupitre" : "Automatique (héritée de la branche)", "A", delegate { Snapshot(); n.Color = 0; n.CustomColor = null; Changed(); }));
            foreach (var i in TaskColors.Indices) { int v = i; colors.Children.Add(Swatch(n.Color == v, TaskColors.Solid(v), Palette.Names[v], null, delegate { Snapshot(); n.Color = v; Changed(); })); }
            colors.Children.Add(Swatch(n.Color < 0, n.Color < 0 ? Theme.Brush(0xFF, Palette.ParseHex(n.CustomColor, Accent)) : Theme.Argb(0x22FFFFFF), "Couleur personnalisée…", "+", delegate
            {
                string hex = TasksWindow.PickCustomColor(this, boxes.ContainsKey(n) ? boxes[n].Color : Palette.Get(1));
                if (hex != null) { Snapshot(); n.Color = -1; n.CustomColor = hex; Changed(); }
            }));
            nodeSection.Children.Add(colors);

            // forme propre au sujet
            var shapes = new WrapPanel();
            for (int i = 0; i < ShapeNames.Length; i++)
            {
                int v = i;
                var cv = new Canvas { Width = 40, Height = 24, ClipToBounds = true };
                if (v == 0) DrawText(cv, "carte", 9); else DrawShape(cv, v, 26, 12);
                bool sel = n.Shape == v;
                var box = new Border { Width = 42, Height = 26, CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(sel ? 2 : 1), BorderBrush = sel ? Theme.Res<Brush>("Accent") : Theme.Res<Brush>("Line"), Background = sel ? Theme.Brush(0x3A, Accent) : Theme.Argb(0x12FFFFFF), Child = cv, Margin = new Thickness(1, 1, 2, 2), Cursor = Cursors.Hand, ToolTip = "Forme : " + ShapeNames[v] };
                box.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { Snapshot(); n.Shape = v; Changed(); e.Handled = true; };
                shapes.Children.Add(box);
            }
            nodeSection.Children.Add(shapes);

            // taille et styles de texte
            var text = new WrapPanel { Margin = new Thickness(0, 2, 0, 0) };
            string[] sizes = { "A normal", "A grand", "a petit" };
            for (int i = 0; i < 3; i++) { int v = i; text.Children.Add(SmallChip(sizes[v], n.Size == v, "Taille du texte : " + SizeNames[v].ToLowerInvariant(), delegate { Snapshot(); n.Size = v; Changed(); })); }
            text.Children.Add(SmallChip("G", n.Bold, "Gras", delegate { Snapshot(); n.Bold = !n.Bold; Changed(); }));
            text.Children.Add(SmallChip("I", n.Italic, "Italique", delegate { Snapshot(); n.Italic = !n.Italic; Changed(); }));
            text.Children.Add(SmallChip("S̶", n.Strike, "Barré", delegate { Snapshot(); n.Strike = !n.Strike; Changed(); }));
            nodeSection.Children.Add(text);

            var quick = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
            quick.Children.Add(SmallChip("Très important", false, "Style rapide : gras, grand, rouge", delegate { QuickStyle(n, 2); }));
            quick.Children.Add(SmallChip("Important", false, "Style rapide : gras, orange", delegate { QuickStyle(n, 1); }));
            quick.Children.Add(SmallChip("Par défaut", false, "Retire les styles du sujet", delegate { QuickStyle(n, 0); }));
            nodeSection.Children.Add(quick);
        }

        Border Swatch(bool selected, Brush fill, string tip, string glyph, Action click)
        {
            var b = new Border { Width = 20, Height = 20, CornerRadius = new CornerRadius(10), Background = fill, BorderBrush = selected ? Brushes.White : Theme.Argb(0x55FFFFFF), BorderThickness = new Thickness(selected ? 2.5 : 1), Margin = new Thickness(0, 0, 4, 4), Cursor = Cursors.Hand, ToolTip = tip };
            if (glyph != null) b.Child = new TextBlock { Text = glyph, FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Theme.Res<Brush>("Text"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { click(); e.Handled = true; };
            return b;
        }

        Button SmallChip(string text, bool on, string tip, Action click)
        {
            var b = new Button { Style = Theme.Res<Style>(on ? "ChipColored" : "ChipBtn"), Content = text, FontSize = 11, Margin = new Thickness(0, 0, 3, 3), ToolTip = tip, Padding = new Thickness(7, 2, 7, 3) };
            if (on) { b.Background = Theme.Res<Brush>("Accent"); b.Foreground = Brushes.White; }
            b.Click += delegate { click(); };
            return b;
        }

        // ---- vignettes vectorielles ------------------------------------------------------------------------------------

        Brush ThumbInk { get { return Theme.Res<Brush>("Text"); } }
        Brush ThumbMuted { get { return Theme.Argb(0xB0FFFFFF); } }

        static void ThumbRect(Canvas cv, double x, double y, double w, double h, Brush fill, double radius, Brush stroke)
        {
            var r = new Rectangle { Width = w, Height = h, RadiusX = radius, RadiusY = radius, Fill = fill, Stroke = stroke, StrokeThickness = stroke != null ? 1 : 0 };
            Canvas.SetLeft(r, x); Canvas.SetTop(r, y); cv.Children.Add(r);
        }

        static void Seg(Canvas cv, double x0, double y0, double x1, double y1, Brush stroke, double thick)
        {
            cv.Children.Add(new Line { X1 = x0, Y1 = y0, X2 = x1, Y2 = y1, Stroke = stroke, StrokeThickness = thick, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
        }

        void DrawStructure(Canvas cv, int layout, bool sel)
        {
            DrawStructureInto(cv, layout, Theme.Brush(0xFF, Accent), new Brush[] { Theme.Argb(0xB8FFFFFF) }, Theme.Argb(0x90FFFFFF));
        }

        /// <summary>Schéma d'une structure dans un repère 60 × 42 : couleur du centre, couleurs des sujets (cycliques), couleur des lignes.</summary>
        static void DrawStructureInto(Canvas cv, int layout, Brush root, Brush[] kids, Brush line)
        {
            int k = 0;
            Func<Brush> kid = delegate { return kids[(k++) % kids.Length]; };
            switch (layout)
            {
                case 0:
                    ThumbRect(cv, 22, 16, 16, 10, root, 3, null);
                    foreach (var p in new[] { new Point(46, 6), new Point(46, 27), new Point(4, 6), new Point(4, 27) }) { ThumbRect(cv, p.X, p.Y, 10, 7, kid(), 2, null); Seg(cv, 30, 21, p.X + 5, p.Y + 3.5, line, 1); }
                    break;
                case 1: case 3:
                    {
                        bool left = layout == 3;
                        ThumbRect(cv, left ? 43 : 3, 16, 14, 10, root, 3, null);
                        double rx = left ? 43 : 17, mx = left ? 34 : 26;
                        for (int i = 0; i < 3; i++) { double y = 4 + i * 14; ThumbRect(cv, left ? 12 : 34, y, 14, 7, kid(), 2, null); Seg(cv, rx, 21, mx, 21, line, 1); Seg(cv, mx, 21, mx, y + 3.5, line, 1); Seg(cv, mx, y + 3.5, left ? 26 : 34, y + 3.5, line, 1); }
                        break;
                    }
                case 2: case 7:
                    {
                        bool up = layout == 7;
                        ThumbRect(cv, 22, up ? 30 : 3, 16, 9, root, 3, null);
                        double ry = up ? 30 : 12, my = up ? 24 : 19, cy = up ? 6 : 27;
                        for (int i = 0; i < 3; i++) { double x = 4 + i * 18; ThumbRect(cv, x, cy, 14, 8, kid(), 2, null); Seg(cv, 30, ry, 30, my, line, 1); Seg(cv, 30, my, x + 7, my, line, 1); Seg(cv, x + 7, my, x + 7, up ? 14 : 27, line, 1); }
                        break;
                    }
                case 4:
                    ThumbRect(cv, 3, 3, 16, 9, root, 3, null);
                    ThumbRect(cv, 12, 16, 14, 6, kid(), 2, null); ThumbRect(cv, 12, 25, 14, 6, kid(), 2, null); ThumbRect(cv, 20, 34, 14, 6, kid(), 2, null);
                    Seg(cv, 7, 12, 7, 28, line, 1); Seg(cv, 7, 19, 12, 19, line, 1); Seg(cv, 7, 28, 12, 28, line, 1); Seg(cv, 15, 31, 15, 37, line, 1); Seg(cv, 15, 37, 20, 37, line, 1);
                    break;
                case 5:
                    Seg(cv, 3, 22, 57, 22, root, 2);
                    ThumbRect(cv, 8, 4, 12, 7, kid(), 2, null); Seg(cv, 14, 11, 14, 22, line, 1);
                    ThumbRect(cv, 24, 31, 12, 7, kid(), 2, null); Seg(cv, 30, 22, 30, 31, line, 1);
                    ThumbRect(cv, 40, 4, 12, 7, kid(), 2, null); Seg(cv, 46, 11, 46, 22, line, 1);
                    break;
                case 8:
                    Seg(cv, 30, 3, 30, 39, root, 2);
                    ThumbRect(cv, 36, 6, 14, 7, kid(), 2, null); Seg(cv, 30, 9.5, 36, 9.5, line, 1);
                    ThumbRect(cv, 10, 18, 14, 7, kid(), 2, null); Seg(cv, 24, 21.5, 30, 21.5, line, 1);
                    ThumbRect(cv, 36, 30, 14, 7, kid(), 2, null); Seg(cv, 30, 33.5, 36, 33.5, line, 1);
                    break;
                default:
                    Seg(cv, 3, 22, 45, 22, root, 2);
                    ThumbRect(cv, 45, 17, 13, 10, root, 3, null);
                    ThumbRect(cv, 8, 3, 12, 7, kid(), 2, null); Seg(cv, 14, 10, 20, 22, line, 1);
                    ThumbRect(cv, 20, 33, 12, 7, kid(), 2, null); Seg(cv, 26, 33, 32, 22, line, 1);
                    ThumbRect(cv, 32, 3, 12, 7, kid(), 2, null); Seg(cv, 38, 10, 44, 22, line, 1);
                    break;
            }
        }

        static readonly Dictionary<string, ImageSource> templateImages = new Dictionary<string, ImageSource>();

        /// <summary>Image de vignette incorporée dans l'exécutable (ressource templates.&lt;id&gt;.png), ou null.</summary>
        static ImageSource TemplateImage(string id)
        {
            ImageSource img;
            if (templateImages.TryGetValue(id, out img)) return img;
            img = null;
            try
            {
                var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("templates." + id + ".png");
                if (stream != null)
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit(); bmp.StreamSource = stream; bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; bmp.EndInit(); bmp.Freeze();
                    img = bmp;
                }
            }
            catch (Exception ex) { Log.Error(ex, "vignette " + id); }
            templateImages[id] = img;
            return img;
        }

        /// <summary>Vignette d'un modèle (galerie « Nouvelle feuille ») : fond et couleurs du thème, structure, palette des branches.</summary>
        static void DrawTemplateThumb(Canvas cv, MapTemplate t)
        {
            bool light = t.Theme == 1 || t.Theme == 3 || t.Theme == 4;
            ThumbRect(cv, 0, 0, cv.Width, cv.Height, light ? Theme.Brush(0xFF, Color.FromRgb(0xF6, 0xF7, 0xF9)) : Theme.Brush(0xFF, Color.FromRgb(0x24, 0x27, 0x30)), 8, null);
            var inner = new Canvas { Width = 60, Height = 42, RenderTransform = new ScaleTransform(cv.Width / 66.0, cv.Height / 46.0) };
            Canvas.SetLeft(inner, 3 * cv.Width / 66.0); Canvas.SetTop(inner, 2 * cv.Height / 46.0);
            Brush root = t.Theme == 3 ? Theme.Brush(0xFF, Color.FromRgb(0x2D, 0x3B, 0x55)) : (t.Theme == 4 ? Theme.Brush(0xFF, Color.FromRgb(0x0F, 0x1A, 0x3C)) : Theme.Brush(0xFF, Accent));
            var kids = new List<Brush>();
            for (int i = 0; i < 6; i++)
            {
                Color col = t.Palette <= 0 || t.Palette >= Palettes.Length ? TaskColors.Get(TaskColors.Indices[i]) : Palette.ParseHex(Palettes[t.Palette][i], Accent);
                if (t.Theme == 1) kids.Add(Theme.Brush(0xFF, Lighten(col, 0.55)));
                else if (t.Theme == 3) kids.Add(Theme.Brush(0xFF, Lighten(col, 0.35)));
                else kids.Add(Theme.Brush(0xFF, col));
            }
            var line = light ? Theme.Argb(0x66000000) : Theme.Argb(0x90FFFFFF);
            DrawStructureInto(inner, t.Layout, root, kids.ToArray(), line);
            cv.Children.Add(inner);
        }

        void DrawTheme(Canvas cv, int theme)
        {
            ThumbRect(cv, 0, 0, 60, 42, theme == 1 || theme == 3 || theme == 4 ? Theme.Brush(0xFF, Color.FromRgb(0xF6, 0xF7, 0xF9)) : Theme.Brush(0xFF, Color.FromRgb(0x24, 0x27, 0x30)), 7, null);
            var c1 = PaletteSample(0); var c2 = PaletteSample(1);
            ThumbRect(cv, 4, 15, 20, 12, theme == 3 ? Theme.Brush(0xFF, Color.FromRgb(0x2D, 0x3B, 0x55)) : (theme == 4 ? Theme.Brush(0xFF, Color.FromRgb(0x0F, 0x1A, 0x3C)) : Theme.Brush(0xFF, Accent)), 4, null);
            Seg(cv, 24, 21, 34, 11, Theme.Brush(0xFF, c1), 1.5); Seg(cv, 24, 21, 34, 31, Theme.Brush(0xFF, c2), 1.5);
            if (theme == 4) { ThumbRect(cv, 34, 6, 20, 10, Theme.Brush(0xFF, Color.FromRgb(0xF0, 0x46, 0x3C)), 3, null); ThumbRect(cv, 34, 26, 20, 10, Theme.Brush(0xFF, Color.FromRgb(0x3A, 0x6B, 0xF2)), 3, null); }
            else if (theme == 3) { ThumbRect(cv, 34, 6, 20, 10, Brushes.White, 3, Theme.Brush(0xFF, c1)); Seg(cv, 36, 34, 52, 34, Theme.Brush(0xFF, c2), 1.5); }
            else if (theme == 1) { ThumbRect(cv, 34, 6, 20, 10, Theme.Brush(0xFF, Lighten(c1, 0.8)), 3, Theme.Brush(0xFF, c1)); ThumbRect(cv, 34, 26, 20, 10, Theme.Brush(0xFF, Lighten(c2, 0.8)), 3, Theme.Brush(0xFF, c2)); }
            else if (theme == 2) { ThumbRect(cv, 34, 6, 20, 10, Theme.Brush(0xFF, c1), 3, null); ThumbRect(cv, 34, 26, 20, 10, Theme.Brush(0xFF, c2), 3, null); }
            else { ThumbRect(cv, 34, 6, 20, 10, Theme.Brush(0xFF, Darken(c1, 0.05)), 3, null); ThumbRect(cv, 34, 26, 20, 10, Theme.Brush(0xFF, Darken(c2, 0.05)), 3, null); }
        }

        Color PaletteSample(int i)
        {
            if (map.Palette <= 0 || map.Palette >= Palettes.Length) return TaskColors.Get(TaskColors.Indices[i % TaskColors.Indices.Length]);
            return Palette.ParseHex(Palettes[map.Palette][i % Palettes[map.Palette].Length], Accent);
        }

        void DrawPalette(Canvas cv, int palette)
        {
            for (int i = 0; i < 6; i++)
            {
                Color col = palette <= 0 ? TaskColors.Get(TaskColors.Indices[i]) : Palette.ParseHex(Palettes[palette][i], Accent);
                var e = new Ellipse { Width = 12, Height = 12, Fill = Theme.Brush(0xFF, col) };
                Canvas.SetLeft(e, 4 + (i % 3) * 18); Canvas.SetTop(e, 7 + (i / 3) * 16);
                cv.Children.Add(e);
            }
        }

        void DrawShape(Canvas cv, int shape, double w, double h)
        {
            double x = (cv.Width - w) / 2, y = (cv.Height - h) / 2;
            var fill = Theme.Brush(0xFF, Accent);
            var text = new TextBlock { Text = "Aa", FontSize = h * 0.55, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
            switch (shape)
            {
                case 2: ThumbRect(cv, x, y, w, h, fill, 2, null); break;
                case 3: ThumbRect(cv, x, y, w, h, fill, h / 2, null); break;
                case 4: text.Foreground = ThumbInk; break;
                case 5: { var e = new Ellipse { Width = w + 6, Height = h + 2, Fill = fill }; Canvas.SetLeft(e, x - 3); Canvas.SetTop(e, y - 1); cv.Children.Add(e); break; }
                case 6: text.Foreground = ThumbInk; Seg(cv, x + 4, y + h, x + w - 4, y + h, fill, 2); break;
                default: ThumbRect(cv, x, y, w, h, fill, 5, null); break;
            }
            text.Measure(new Size(200, 200));
            Canvas.SetLeft(text, cv.Width / 2 - text.DesiredSize.Width / 2); Canvas.SetTop(text, cv.Height / 2 - text.DesiredSize.Height / 2 - 1);
            cv.Children.Add(text);
        }

        void DrawLineStyle(Canvas cv, int style)
        {
            var ink = Theme.Brush(0xFF, Accent);
            var path = new Path { Stroke = ink, StrokeThickness = 2, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round };
            var g = new StreamGeometry();
            using (var ctx = g.Open())
            {
                ctx.BeginFigure(new Point(8, 32), false, false);
                if (style == 1) ctx.LineTo(new Point(52, 10), true, false);
                else if (style == 2) { ctx.LineTo(new Point(30, 32), true, false); ctx.LineTo(new Point(30, 10), true, false); ctx.LineTo(new Point(52, 10), true, false); }
                else ctx.BezierTo(new Point(30, 32), new Point(30, 10), new Point(52, 10), true, false);
            }
            g.Freeze(); path.Data = g; cv.Children.Add(path);
            foreach (var p in new[] { new Point(8, 32), new Point(52, 10) }) { var e = new Ellipse { Width = 6, Height = 6, Fill = ThumbInk }; Canvas.SetLeft(e, p.X - 3); Canvas.SetTop(e, p.Y - 3); cv.Children.Add(e); }
        }

        void DrawLineWidth(Canvas cv, int width)
        {
            double t = width == 0 ? 1 : (width == 2 ? 4 : 2.2);
            Seg(cv, 8, 21, 52, 21, Theme.Brush(0xFF, Accent), t);
        }

        void DrawSpacing(Canvas cv, int spacing)
        {
            double gap = spacing == 0 ? 3 : (spacing == 2 ? 11 : 6);
            var kid = Theme.Argb(0xB8FFFFFF);
            ThumbRect(cv, 6, 17, 12, 8, Theme.Brush(0xFF, Accent), 2, null);
            for (int i = 0; i < 3; i++) ThumbRect(cv, 30, 5 + i * (8 + gap), 22, 8, kid, 2, null);
        }

        void DrawText(Canvas cv, string text, double size)
        {
            var tb = new TextBlock { Text = text, FontSize = size, FontWeight = FontWeights.SemiBold, Foreground = ThumbInk };
            tb.Measure(new Size(200, 200));
            Canvas.SetLeft(tb, cv.Width / 2 - tb.DesiredSize.Width / 2); Canvas.SetTop(tb, cv.Height / 2 - tb.DesiredSize.Height / 2);
            cv.Children.Add(tb);
        }
    }
}
