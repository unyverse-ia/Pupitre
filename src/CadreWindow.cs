using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace Pupitre
{
    /// <summary>Élément de fichier affiché dans un cadre.</summary>
    public class FileItemVm : INotifyPropertyChanged
    {
        public readonly string Path;
        public readonly string Name;
        public readonly bool IsFolder;
        public readonly DateTime Created;
        ImageSource icon;
        bool selected, isNew;
        int size;
        bool showExt;
        string tip;

        public FileItemVm(string path, string name, bool isFolder, int size, bool showExt, DateTime created)
        {
            Path = path; Name = name; IsFolder = isFolder; this.size = size; this.showExt = showExt; Created = created;
        }

        /// <summary>Élément virtuel du bureau (Corbeille, Ce PC…) : Path = nom d'analyse « ::{CLSID} ».</summary>
        public bool IsSpecial { get { return Path.StartsWith("::{"); } }
        public bool IsRecycleBin { get { return string.Equals(Path, FileOps.DesktopSpecials.RecycleBin, StringComparison.OrdinalIgnoreCase); } }
        public void SetSpecialName(string displayName) { if (!string.IsNullOrEmpty(displayName) && displayName != specialName) { specialName = displayName; tip = null; Notify("Label"); Notify("Tip"); } }
        string specialName;

        public string Label
        {
            get
            {
                if (IsSpecial) return specialName ?? (IsRecycleBin ? "Corbeille" : Name);
                if (IsFolder) return Name;
                string ext = System.IO.Path.GetExtension(Name).ToLowerInvariant();
                if (ext == ".lnk" || ext == ".url" || ext == ".appref-ms") return System.IO.Path.GetFileNameWithoutExtension(Name);
                if (!showExt && ext.Length > 0 && Name.Length > ext.Length) return Name.Substring(0, Name.Length - ext.Length);
                return Name;
            }
        }

        public string Tip
        {
            get
            {
                if (tip == null && IsSpecial) tip = Label + "\n" + (IsRecycleBin ? "Double-clic pour ouvrir · glisser des fichiers dessus pour les y mettre" : "Élément système du bureau");
                if (tip == null)
                {
                    try
                    {
                        var dt = IsFolder ? Directory.GetLastWriteTime(Path) : File.GetLastWriteTime(Path);
                        string size = IsFolder ? "Dossier" : Human(new FileInfo(Path).Length);
                        tip = Name + "\n" + size + " · modifié le " + dt.ToString("dd/MM/yyyy HH:mm") + (isNew ? "\nNouveau" : "");
                    }
                    catch { tip = Name; }
                }
                return tip;
            }
        }

        static string Human(long bytes)
        {
            if (bytes < 1024) return bytes + " o";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0") + " Ko";
            if (bytes < 1024L * 1024 * 1024) return (bytes / 1048576.0).ToString("0.#") + " Mo";
            return (bytes / 1073741824.0).ToString("0.##") + " Go";
        }

        public double CellW { get { return IconSizes.CellW(size); } }
        public double CellH { get { return IconSizes.CellH(size); } }
        public double IconPx { get { return IconSizes.Px(size); } }
        public int Size { get { return size; } }

        public ImageSource Icon { get { return icon; } set { icon = value; Notify("Icon"); } }
        public bool IsSelected { get { return selected; } set { if (selected != value) { selected = value; Notify("IsSelected"); } } }
        public bool IsNew { get { return isNew; } set { if (isNew != value) { isNew = value; tip = null; Notify("IsNew"); Notify("Tip"); } } }

        public event PropertyChangedEventHandler PropertyChanged;
        void Notify(string n) { var h = PropertyChanged; if (h != null) h(this, new PropertyChangedEventArgs(n)); }
    }

    /// <summary>Un cadre : la vue d'un dossier (ou du bureau) sous forme de grille d'icônes, avec glisser-déposer.</summary>
    public class CadreWindow : DesktopWindow
    {
        public readonly CadreInfo Info;
        readonly ObservableCollection<FileItemVm> items = new ObservableCollection<FileItemVm>();
        ItemsControl list;
        Border empty, dropHint, newBadge;
        TextBlock countText, emptyText, newText;
        FileSystemWatcher watcher, watcher2;
        readonly Debouncer refreshDeb;
        FileItemVm dragCandidate;
        Point dragStart;
        bool dragging;
        int iconLoadCursor;
        static readonly TimeSpan NewWindow = TimeSpan.FromHours(48);

        const string BodyXaml = @"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Grid.Resources>
    <BooleanToVisibilityConverter x:Key='B2V'/>
  </Grid.Resources>
  <ScrollViewer x:Name='Scroll' VerticalScrollBarVisibility='Auto' HorizontalScrollBarVisibility='Disabled' Padding='6,4,6,6' Focusable='False' Background='Transparent'>
    <ItemsControl x:Name='List' Focusable='False' Background='Transparent'>
      <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
          <WrapPanel Orientation='Horizontal'/>
        </ItemsPanelTemplate>
      </ItemsControl.ItemsPanel>
      <ItemsControl.ItemTemplate>
        <DataTemplate>
          <Border Tag='tile' Width='{Binding CellW}' Height='{Binding CellH}' Margin='1' CornerRadius='8' ToolTip='{Binding Tip}' ToolTipService.InitialShowDelay='700'>
            <Border.Style>
              <Style TargetType='{x:Type Border}'>
                <Setter Property='Background' Value='Transparent'/>
                <Style.Triggers>
                  <Trigger Property='IsMouseOver' Value='True'>
                    <Setter Property='Background' Value='{DynamicResource Hover}'/>
                  </Trigger>
                  <DataTrigger Binding='{Binding IsSelected}' Value='True'>
                    <Setter Property='Background' Value='{DynamicResource Selected}'/>
                  </DataTrigger>
                </Style.Triggers>
              </Style>
            </Border.Style>
            <Grid>
              <StackPanel VerticalAlignment='Top' Margin='3,7,3,2'>
                <Image Source='{Binding Icon}' Width='{Binding IconPx}' Height='{Binding IconPx}' RenderOptions.BitmapScalingMode='HighQuality' SnapsToDevicePixels='True' UseLayoutRounding='True'/>
                <TextBlock Text='{Binding Label}' TextAlignment='Center' TextWrapping='Wrap' TextTrimming='CharacterEllipsis' FontSize='12' Foreground='{DynamicResource Text}' Margin='0,5,0,0' MaxHeight='32' ClipToBounds='True' LineHeight='16' LineStackingStrategy='BlockLineHeight'/>
              </StackPanel>
              <Border Width='9' Height='9' CornerRadius='5' Background='{DynamicResource Accent}' BorderBrush='#80000000' BorderThickness='1' HorizontalAlignment='Right' VerticalAlignment='Top' Margin='0,5,8,0' Visibility='{Binding IsNew, Converter={StaticResource B2V}}' ToolTip='Nouveau'/>
            </Grid>
          </Border>
        </DataTemplate>
      </ItemsControl.ItemTemplate>
    </ItemsControl>
  </ScrollViewer>
  <Border x:Name='Empty' Visibility='Collapsed' IsHitTestVisible='False' Margin='14,10,14,14' BorderBrush='{DynamicResource Line}' BorderThickness='1.5' CornerRadius='10'>
    <StackPanel VerticalAlignment='Center' HorizontalAlignment='Center' Margin='10'>
      <Viewbox Width='26' Height='26' Margin='0,0,0,8' HorizontalAlignment='Center'>
        <Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoInbox}' TextElement.Foreground='{DynamicResource TextFaint}' StrokeThickness='1.6'/>
      </Viewbox>
      <TextBlock x:Name='EmptyText' Text='Glissez des fichiers ou des raccourcis ici' Foreground='{DynamicResource TextFaint}' FontSize='12' TextAlignment='Center' TextWrapping='Wrap'/>
    </StackPanel>
  </Border>
  <Border x:Name='DropHint' Visibility='Collapsed' IsHitTestVisible='False' Background='#265B8DEF' BorderBrush='{DynamicResource Accent}' BorderThickness='2' CornerRadius='10' Margin='4'/>
</Grid>";

        public CadreWindow(Controller ctl, CadreInfo info) : base(ctl)
        {
            Info = info;
            AddStandardActions();
            countText = new TextBlock { FontSize = 11, Foreground = Theme.Res<Brush>("TextFaint"), VerticalAlignment = VerticalAlignment.Center };
            TitleExtra.Children.Add(countText);
            newBadge = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(6, 0, 6, 1), Background = Theme.Res<Brush>("Accent"), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Collapsed, Cursor = Cursors.Hand, ToolTip = "Éléments récents (clic : tout marquer comme vu)" };
            newText = new TextBlock { FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
            newBadge.Child = newText;
            newBadge.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { MarkAllSeen(); e.Handled = true; };
            TitleExtra.Children.Add(newBadge);

            var body = (Grid)XamlReader.Parse(BodyXaml);
            list = (ItemsControl)body.FindName("List");
            empty = (Border)body.FindName("Empty");
            emptyText = (TextBlock)body.FindName("EmptyText");
            dropHint = (Border)body.FindName("DropHint");
            list.ItemsSource = items;
            Body.Content = body;

            list.PreviewMouseLeftButtonDown += ListMouseDown;
            list.PreviewMouseMove += ListMouseMove;
            list.PreviewMouseLeftButtonUp += delegate { dragCandidate = null; };
            list.MouseRightButtonUp += ListRightUp;
            PreviewKeyDown += OnKey;
            Root.AllowDrop = true;
            Root.DragEnter += OnDragOver;
            Root.DragOver += OnDragOver;
            Root.DragLeave += delegate { dropHint.Visibility = Visibility.Collapsed; };
            Root.Drop += OnDrop;
            Deactivated += delegate { ClearSelection(); };

            refreshDeb = new Debouncer(350, Refresh);
            ApplyInfo();
            SetupWatchers();
        }

        public string Folder { get { return Info.IsDesktop ? App.Desktop : Info.Folder; } }
        public int IconSize { get { return Info.IconSize < 0 ? c.S.IconSize : Info.IconSize; } }
        public int Count { get { return items.Count; } }

        /// <summary>Applique nom, couleur, opacité, taille d'icônes, tri (après modification des réglages du cadre).</summary>
        public void ApplyInfo()
        {
            SetTitle(Info.Name);
            SetLook(Info.Color, Info.Opacity, Info.CustomColor);
            MinWidth = IconSizes.CellW(IconSize) + 26;   // une icône de large au minimum
            if (Width < MinWidth) Width = MinWidth;
            emptyText.Text = Info.IsDesktop ? "Votre bureau est vide" : "Glissez des fichiers ou des raccourcis ici";
            Refresh();
        }

        // ---- Contenu ---------------------------------------------------------------------------------

        public void Refresh()
        {
            var entries = new List<FileItemVm>();
            try
            {
                if (Directory.Exists(Folder))
                    foreach (var p in Directory.GetFileSystemEntries(Folder)) AddEntry(entries, p);
                if (Info.IsDesktop && Directory.Exists(App.PublicDesktop))
                    foreach (var p in Directory.GetFileSystemEntries(App.PublicDesktop)) AddEntry(entries, p);
            }
            catch (Exception ex) { Log.Error(ex, "lecture du cadre " + Info.Name); }
            Sort(entries);
            // icônes système du bureau Windows (Corbeille, Ce PC…) en tête du cadre Bureau
            if (Info.IsDesktop)
            {
                var specials = FileOps.DesktopSpecials.Shown();
                for (int i = specials.Count - 1; i >= 0; i--) entries.Insert(0, new FileItemVm(specials[i], specials[i] == FileOps.DesktopSpecials.RecycleBin ? "Corbeille" : "Élément système", true, IconSize, c.S.ShowExtensions, DateTime.MinValue));
            }

            var selected = new HashSet<string>(items.Where(i => i.IsSelected).Select(i => i.Path), StringComparer.OrdinalIgnoreCase);
            var oldIcons = items.ToDictionary(i => i.Path + "|" + i.Size, i => i.Icon, StringComparer.OrdinalIgnoreCase);
            var now = DateTime.Now;
            items.Clear();
            int fresh = 0;
            foreach (var e in entries)
            {
                e.IsSelected = selected.Contains(e.Path);
                e.IsNew = now - e.Created < NewWindow && !Info.Seen.Contains(e.Path, StringComparer.OrdinalIgnoreCase);
                if (e.IsNew) fresh++;
                ImageSource ic;
                if (!e.IsSpecial && oldIcons.TryGetValue(e.Path + "|" + e.Size, out ic)) e.Icon = ic;   // les icônes système sont relues (corbeille pleine / vide)
                items.Add(e);
            }
            // nettoyage des chemins « vus » qui n'existent plus ou ne sont plus récents
            var present = new HashSet<string>(entries.Where(x => now - x.Created < NewWindow).Select(x => x.Path), StringComparer.OrdinalIgnoreCase);
            if (Info.Seen.RemoveAll(p => !present.Contains(p)) > 0) c.SaveLayoutSoon();

            empty.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            countText.Text = items.Count == 0 ? "" : items.Count.ToString();
            newText.Text = fresh == 1 ? "1 nouveau" : fresh + " nouveaux";
            newBadge.Visibility = fresh > 0 ? Visibility.Visible : Visibility.Collapsed;
            iconLoadCursor = 0;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(LoadIcons));
        }

        void AddEntry(List<FileItemVm> target, string path)
        {
            string name = Path.GetFileName(path);
            if (name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) return;
            FileAttributes a;
            try { a = File.GetAttributes(path); } catch { return; }
            if ((a & FileAttributes.Hidden) != 0 || (a & FileAttributes.System) != 0) return;
            bool isDir = (a & FileAttributes.Directory) != 0;
            DateTime created;
            try { created = isDir ? Directory.GetCreationTime(path) : File.GetCreationTime(path); } catch { created = DateTime.MinValue; }
            target.Add(new FileItemVm(path, name, isDir, IconSize, c.S.ShowExtensions, created));
        }

        void Sort(List<FileItemVm> entries)
        {
            Comparison<FileItemVm> cmp;
            switch (Info.Sort)
            {
                case "type":
                    cmp = (x, y) => { int d = string.Compare(Path.GetExtension(x.Name), Path.GetExtension(y.Name), StringComparison.CurrentCultureIgnoreCase); return d != 0 ? d : string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase); };
                    break;
                case "date":
                    cmp = (x, y) => Date(y.Path).CompareTo(Date(x.Path));
                    break;
                default:
                    cmp = (x, y) => string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
                    break;
            }
            entries.Sort((x, y) => { if (x.IsFolder != y.IsFolder) return x.IsFolder ? -1 : 1; return cmp(x, y); });
        }

        static DateTime Date(string p) { try { return File.GetLastWriteTime(p); } catch { return DateTime.MinValue; } }

        void LoadIcons()
        {
            int n = 0;
            while (iconLoadCursor < items.Count && n < 6)
            {
                var it = items[iconLoadCursor++];
                if (it.IsSpecial) { string dn; it.Icon = ShellIcons.GetSpecial(it.Path, (int)it.IconPx, out dn); it.SetSpecialName(dn); n++; }
                else if (it.Icon == null) { it.Icon = ShellIcons.Get(it.Path, (int)it.IconPx, it.IsFolder); n++; }
            }
            if (iconLoadCursor < items.Count) Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(LoadIcons));
        }

        void MarkSeen(FileItemVm it)
        {
            if (!it.IsNew) return;
            it.IsNew = false;
            if (!Info.Seen.Contains(it.Path, StringComparer.OrdinalIgnoreCase)) Info.Seen.Add(it.Path);
            int fresh = items.Count(x => x.IsNew);
            newText.Text = fresh == 1 ? "1 nouveau" : fresh + " nouveaux";
            newBadge.Visibility = fresh > 0 ? Visibility.Visible : Visibility.Collapsed;
            c.SaveLayoutSoon();
        }

        void MarkAllSeen()
        {
            foreach (var it in items.Where(x => x.IsNew).ToList()) MarkSeen(it);
        }

        public void SetupWatchers()
        {
            DisposeWatchers();
            watcher = MakeWatcher(Folder);
            if (Info.IsDesktop) watcher2 = MakeWatcher(App.PublicDesktop);
        }

        FileSystemWatcher MakeWatcher(string folder)
        {
            try
            {
                if (!Directory.Exists(folder)) return null;
                var w = new FileSystemWatcher(folder) { NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Attributes | NotifyFilters.LastWrite, IncludeSubdirectories = false };
                FileSystemEventHandler h = delegate { Dispatcher.BeginInvoke(new Action(refreshDeb.Trigger)); };
                w.Created += h; w.Deleted += h; w.Changed += h;
                w.Renamed += delegate { Dispatcher.BeginInvoke(new Action(refreshDeb.Trigger)); };
                w.EnableRaisingEvents = true;
                return w;
            }
            catch (Exception ex) { Log.Error(ex, "surveillance " + folder); return null; }
        }

        void DisposeWatchers()
        {
            if (watcher != null) { watcher.EnableRaisingEvents = false; watcher.Dispose(); watcher = null; }
            if (watcher2 != null) { watcher2.EnableRaisingEvents = false; watcher2.Dispose(); watcher2 = null; }
        }

        protected override void OnClosed(EventArgs e)
        {
            DisposeWatchers();
            base.OnClosed(e);
        }

        // ---- Sélection, souris, clavier --------------------------------------------------------------

        static FileItemVm HitItem(object source)
        {
            var d = source as DependencyObject;
            while (d != null)
            {
                var fe = d as FrameworkElement;
                if (fe != null && "tile".Equals(fe.Tag)) return fe.DataContext as FileItemVm;
                d = (d is Visual || d is System.Windows.Media.Media3D.Visual3D) ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
            }
            return null;
        }

        void ListMouseDown(object sender, MouseButtonEventArgs e)
        {
            var it = HitItem(e.OriginalSource);
            Focus();
            if (it == null) { if (!Ctrl) ClearSelection(); return; }
            MarkSeen(it);
            if (e.ClickCount == 2) { dragCandidate = null; Open(it); e.Handled = true; return; }
            if (Ctrl) it.IsSelected = !it.IsSelected;
            else if (!it.IsSelected) { ClearSelection(); it.IsSelected = true; }
            dragCandidate = it;
            dragStart = e.GetPosition(this);
        }

        void ListMouseMove(object sender, MouseEventArgs e)
        {
            if (dragCandidate == null || dragging || e.LeftButton != MouseButtonState.Pressed) return;
            var d = e.GetPosition(this) - dragStart;
            if (Math.Abs(d.X) > 6 || Math.Abs(d.Y) > 6) StartDrag();
        }

        void ListRightUp(object sender, MouseButtonEventArgs e)
        {
            var it = HitItem(e.OriginalSource);
            if (it != null)
            {
                if (!it.IsSelected) { ClearSelection(); it.IsSelected = true; }
                ShowItemMenu(it);
            }
            else ShowContentMenu();
            e.Handled = true;
        }

        void OnKey(object sender, KeyEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox) return;
            var sel = Selected();
            switch (e.Key)
            {
                case Key.Escape: ClearSelection(); break;
                case Key.Delete: if (sel.Count > 0) Recycle(sel); break;
                case Key.F2: if (sel.Count == 1 && !sel[0].IsSpecial) Rename(sel[0]); break;
                case Key.Enter: foreach (var s in sel) Open(s); break;
                case Key.F5: Refresh(); break;
                case Key.A: if (Ctrl) { foreach (var i in items) i.IsSelected = true; } else return; break;
                case Key.C: if (Ctrl && sel.Count > 0) CopyToClipboard(sel); else return; break;
                case Key.V: if (Ctrl) PasteFromClipboard(); else return; break;
                default: return;
            }
            e.Handled = true;
        }

        static bool Ctrl { get { return (Keyboard.Modifiers & ModifierKeys.Control) != 0; } }
        List<FileItemVm> Selected() { return items.Where(i => i.IsSelected).ToList(); }
        public void ClearSelection() { foreach (var i in items) i.IsSelected = false; }

        void Open(FileItemVm it) { if (it.IsSpecial) FileOps.OpenSpecial(it.Path); else FileOps.Open(it.Path); }

        void CopyToClipboard(List<FileItemVm> sel)
        {
            try
            {
                var col = new System.Collections.Specialized.StringCollection();
                foreach (var s in sel) if (!s.IsSpecial) col.Add(s.Path);
                if (col.Count == 0) return;
                Clipboard.SetFileDropList(col);
            }
            catch (Exception ex) { Log.Error(ex, "presse-papiers"); }
        }

        void PasteFromClipboard()
        {
            try
            {
                if (!Clipboard.ContainsFileDropList()) return;
                var paths = new List<string>();
                foreach (var p in Clipboard.GetFileDropList()) if (!App.PathEquals(Path.GetDirectoryName(p), Folder)) paths.Add(p);
                if (paths.Count > 0) FileOps.Transfer(paths, Folder, true, false, Hwnd);
                refreshDeb.Trigger();
            }
            catch (Exception ex) { Log.Error(ex, "coller"); }
        }

        void Recycle(List<FileItemVm> sel)
        {
            var paths = sel.Where(s => !s.IsSpecial).Select(s => s.Path).ToList();
            if (paths.Count == 0) return;
            FileOps.Recycle(paths, Hwnd);
            refreshDeb.Trigger();
        }

        void EmptyRecycleBin()
        {
            FileOps.EmptyRecycleBin(Hwnd);
            refreshDeb.Trigger();
            foreach (var w in c.Cadres) if (w.Info.IsDesktop) w.RefreshSoon();
        }

        void Rename(FileItemVm it)
        {
            string name = Dialogs.Prompt(this, "Renommer", "Nouveau nom", it.Name, true);
            if (string.IsNullOrEmpty(name) || name == it.Name) return;
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) { Dialogs.Info(this, "Nom invalide", "Ce nom contient des caractères interdits."); return; }
            FileOps.Rename(it.Path, name, Hwnd);
            refreshDeb.Trigger();
        }

        void NewFolder()
        {
            string name = Dialogs.Prompt(this, "Nouveau dossier", "Nom du dossier", "Nouveau dossier", false);
            if (string.IsNullOrEmpty(name)) return;
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) { Dialogs.Info(this, "Nom invalide", "Ce nom contient des caractères interdits."); return; }
            try { Directory.CreateDirectory(Path.Combine(Folder, FileOps.UniqueName(Folder, name))); }
            catch (Exception ex) { Dialogs.Info(this, "Impossible de créer le dossier", ex.Message); }
            refreshDeb.Trigger();
        }

        // ---- Glisser-déposer -------------------------------------------------------------------------

        void StartDrag()
        {
            var paths = Selected().Where(s => !s.IsSpecial).Select(s => s.Path).ToArray();
            if (paths.Length == 0) return;
            dragging = true;
            try
            {
                var data = new DataObject(DataFormats.FileDrop, paths);
                DragDrop.DoDragDrop(list, data, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
            }
            catch (Exception ex) { Log.Error(ex, "glisser"); }
            finally { dragging = false; dragCandidate = null; }
            refreshDeb.Trigger();
        }

        string[] DroppedPaths(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return null;
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths == null || paths.Length == 0) return null;
            var list2 = new List<string>();
            foreach (var p in paths) if (!App.PathEquals(Path.GetDirectoryName(p), Folder) && !App.PathEquals(p, Folder)) list2.Add(p);
            return list2.ToArray();
        }

        /// <summary>Chemins déposés sur la tuile Corbeille (depuis ce cadre, un autre cadre ou l'Explorateur), sinon null.</summary>
        string[] RecycleTarget(DragEventArgs e)
        {
            var it = HitItem(e.OriginalSource);
            if (it == null || !it.IsRecycleBin || !e.Data.GetDataPresent(DataFormats.FileDrop)) return null;
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths == null) return null;
            var list2 = paths.Where(p => !p.StartsWith("::{")).ToArray();
            return list2.Length > 0 ? list2 : null;
        }

        void OnDragOver(object sender, DragEventArgs e)
        {
            var toBin = RecycleTarget(e);
            if (toBin != null) { e.Effects = DragDropEffects.Move; dropHint.Visibility = Visibility.Collapsed; e.Handled = true; return; }
            var paths = DroppedPaths(e);
            if (paths == null || paths.Length == 0) { e.Effects = DragDropEffects.None; dropHint.Visibility = Visibility.Collapsed; }
            else
            {
                bool copy = (e.KeyStates & DragDropKeyStates.ControlKey) != 0 || (e.AllowedEffects & DragDropEffects.Move) == 0;
                e.Effects = copy ? DragDropEffects.Copy : DragDropEffects.Move;
                dropHint.Visibility = Visibility.Visible;
            }
            e.Handled = true;
        }

        void OnDrop(object sender, DragEventArgs e)
        {
            dropHint.Visibility = Visibility.Collapsed;
            var toBin = RecycleTarget(e);
            if (toBin != null)
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    FileOps.Recycle(toBin, Hwnd);
                    refreshDeb.Trigger();
                    foreach (var other in c.Cadres) if (other != this) other.RefreshSoon();
                }));
                return;
            }
            var paths = DroppedPaths(e);
            if (paths == null || paths.Length == 0) return;
            bool copy = (e.KeyStates & DragDropKeyStates.ControlKey) != 0 || (e.AllowedEffects & DragDropEffects.Move) == 0;
            e.Effects = copy ? DragDropEffects.Copy : DragDropEffects.Move;
            e.Handled = true;
            Dispatcher.BeginInvoke(new Action(delegate
            {
                FileOps.Transfer(paths, Folder, copy, false, Hwnd);
                // ce qui arrive par glisser-déposer a été vu : pas de marque « nouveau »
                foreach (var p in paths) { string dest = Path.Combine(Folder, Path.GetFileName(p)); if (!Info.Seen.Contains(dest, StringComparer.OrdinalIgnoreCase)) Info.Seen.Add(dest); }
                refreshDeb.Trigger();
                foreach (var other in c.Cadres) if (other != this) other.RefreshSoon();
            }));
        }

        public void RefreshSoon() { refreshDeb.Trigger(); }

        // ---- Menus -----------------------------------------------------------------------------------

        void ShowItemMenu(FileItemVm it)
        {
            var sel = Selected();
            var menu = new ContextMenu();
            if (it.IsSpecial)
            {
                menu.Items.Add(Item("Ouvrir", "IcoExternal", delegate { Open(it); }));
                if (it.IsRecycleBin)
                {
                    var emptyItem = Item("Vider la Corbeille…", "IcoTrash", EmptyRecycleBin);
                    emptyItem.IsEnabled = !FileOps.RecycleBinEmpty();
                    menu.Items.Add(emptyItem);
                }
                menu.PlacementTarget = this;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                menu.IsOpen = true;
                return;
            }
            sel = sel.Where(s => !s.IsSpecial).ToList();
            if (sel.Count == 0) return;
            menu.Items.Add(Item(sel.Count > 1 ? "Ouvrir (" + sel.Count + ")" : "Ouvrir", "IcoExternal", delegate { foreach (var s in sel) Open(s); }));
            if (sel.Count == 1 && !it.IsFolder) menu.Items.Add(Item("Ouvrir avec…", null, delegate { FileOps.OpenWith(it.Path); }));
            menu.Items.Add(Item("Afficher dans l'Explorateur", "IcoFolderOpen", delegate { FileOps.ShowInExplorer(it.Path); }));
            menu.Items.Add(new Separator());
            var others = c.Cadres.Where(w => w != this && w.Info.Visible).ToList();
            if (others.Count > 0)
            {
                var mv = new MenuItem { Header = "Déplacer vers", Icon = Theme.Icon("IcoFolder", 14) };
                foreach (var other in others)
                {
                    var target = other;
                    mv.Items.Add(Item(target.Info.Name, null, delegate
                    {
                        FileOps.Transfer(sel.Select(s => s.Path), target.Folder, false, false, Hwnd);
                        refreshDeb.Trigger(); target.RefreshSoon();
                    }));
                }
                menu.Items.Add(mv);
            }
            menu.Items.Add(Item("Copier", "IcoCopy", delegate { CopyToClipboard(sel); }));
            menu.Items.Add(Item("Copier le chemin", null, delegate { try { Clipboard.SetText(string.Join(Environment.NewLine, sel.Select(s => s.Path).ToArray())); } catch { } }));
            if (sel.Count == 1) menu.Items.Add(Item("Renommer…", "IcoPencil", delegate { Rename(it); }));
            var del = Item(sel.Count > 1 ? "Supprimer (" + sel.Count + ")" : "Supprimer", "IcoTrash", delegate { Recycle(sel); });
            del.InputGestureText = "Suppr";
            menu.Items.Add(del);
            if (sel.Count == 1) { menu.Items.Add(new Separator()); menu.Items.Add(Item("Propriétés", "IcoInfo", delegate { FileOps.Properties(it.Path, Hwnd); })); }
            menu.PlacementTarget = this;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            menu.IsOpen = true;
        }

        /// <summary>Clic droit sur le fond du cadre : actions sur le contenu (le réglage du panneau passe par la barre de titre).</summary>
        void ShowContentMenu()
        {
            var menu = new ContextMenu();
            menu.Items.Add(Item("Nouveau dossier…", "IcoFolder", NewFolder));
            menu.Items.Add(Item("Coller", "IcoCopy", PasteFromClipboard));
            menu.Items.Add(Item("Actualiser", "IcoRefresh", Refresh));
            menu.Items.Add(Item("Ouvrir dans l'Explorateur", "IcoFolderOpen", delegate { FileOps.OpenFolder(Folder); }));
            if (items.Any(x => x.IsNew)) menu.Items.Add(Item("Marquer les nouveaux comme vus", "IcoEye", MarkAllSeen));
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Nouvelle tâche…", "IcoCheckSquare", delegate { c.QuickAdd(); }));
            menu.Items.Add(Item("Nouveau cadre…", "IcoGridLayout", delegate { c.NewCadre(this); }));
            menu.Items.Add(Item("Nouvelle note", "IcoNotes", delegate { c.NewNote(this); }));
            menu.Items.Add(Item("Nouvelle carte mentale…", "IcoGridLayout", delegate { c.NewMindMap(this); }));
            menu.PlacementTarget = this;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            menu.IsOpen = true;
        }

        protected override void OnOpenSettings() { c.EditCadre(this); }
        protected override bool AllowTopResize { get { return !Fusion.IsBelow(c, this); } }

        /// <summary>Menu du titre : fusion avec la colonne d'un autre cadre (ou cadre indépendant).</summary>
        protected override void BuildMenu(ContextMenu menu)
        {
            var fusion = new MenuItem { Header = "Fusionner avec un cadre", Icon = Theme.Icon("IcoGridLayout", 14) };
            bool below = Fusion.IsBelow(c, this);
            fusion.Items.Add(Check("Aucune fusion (cadre indépendant)", !below, delegate { Fusion.Attach(c, this, null); }));
            foreach (var t0 in Fusion.Targets(c, this))
            {
                var t = t0;
                fusion.Items.Add(Check("Sous « " + t.Info.Name + " »", below && Fusion.Top(c, this) == t, delegate { Fusion.Attach(c, this, t); }));
            }
            menu.Items.Add(fusion);
        }
        protected override bool OwnLocked { get { return Info.Locked; } }
        protected override void OnRenamed(string name) { Info.Name = name; SetTitle(name); c.SaveLayoutSoon(); }

        void SetIconSize(int v) { Info.IconSize = v; c.SaveLayoutSoon(); Refresh(); }
        void SetSort(string v) { Info.Sort = v; c.SaveLayoutSoon(); Refresh(); }

        protected override void SaveRect()
        {
            Info.X = Left; Info.Y = Top; Info.W = Width; Info.H = RestoreHeight; Info.Rolled = IsRolled; Info.RolledH = RestoreHeight;
            string sc; double rx, ry; CurrentScreen(out sc, out rx, out ry); Info.Screen = sc; Info.RelX = rx; Info.RelY = ry;
            c.SaveLayoutSoon();
        }
    }
}
