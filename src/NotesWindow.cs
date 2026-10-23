using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace Pupitre
{
    /// <summary>Une note de la bibliothèque.</summary>
    public class NoteVm : INotifyPropertyChanged
    {
        public readonly NoteInfo M;
        readonly NotesWindow owner;
        static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");
        bool dragging;

        public NoteVm(NotesWindow o, NoteInfo m) { owner = o; M = m; }

        public string Title
        {
            get { return M.Title ?? ""; }
            set { if (M.Title != value) { M.Title = value; Touch(); Notify("Title"); } }
        }
        public string Text
        {
            get { return M.Text ?? ""; }
            set { if (M.Text != value) { M.Text = value; Touch(); Notify("Text"); } }
        }
        public int Color
        {
            get { return M.Color; }
            set { if (M.Color != value) { M.Color = value; Notify("Color"); Notify("StripeBrush"); Notify("CardBg"); owner.SaveSoon(); } }
        }
        /// <summary>Repliée : seule la ligne de titre reste visible.</summary>
        public bool IsExpanded
        {
            get { return M.Expanded; }
            set { if (M.Expanded != value) { M.Expanded = value; Notify("IsExpanded"); Notify("ChevronData"); Notify("ChevronTip"); owner.SaveSoon(); } }
        }
        bool textEditing;
        /// <summary>Texte en cours de saisie (zone de texte) ; sinon rendu lecture avec liens et cases à cocher.</summary>
        public bool IsTextEditing
        {
            get { return textEditing; }
            set { if (textEditing != value) { textEditing = value; Notify("IsTextEditing"); Notify("NotTextEditing"); } }
        }
        public bool NotTextEditing { get { return !textEditing; } }
        public bool IsDragging
        {
            get { return dragging; }
            set { if (dragging != value) { dragging = value; Notify("IsDragging"); Notify("CardBorder"); Notify("CardOpacity"); } }
        }
        public Brush CardBorder { get { return dragging ? Theme.Res<Brush>("Accent") : Theme.Res<Brush>("Line"); } }
        public double CardOpacity { get { return dragging ? 0.28 : 1.0; } }

        void Touch() { M.Updated = DateTime.Now.ToString("s"); Notify("UpdatedText"); owner.SaveSoon(); }

        public Geometry ChevronData { get { return Theme.Res<Geometry>(M.Expanded ? "IcoChevronUp" : "IcoChevronDown"); } }
        public string ChevronTip { get { return M.Expanded ? "Replier (ne garder que le titre)" : "Déplier"; } }
        public Brush StripeBrush { get { return Palette.Brush(M.Color); } }
        public Brush CardBg { get { return Theme.Brush(0x24, Palette.Get(M.Color)); } }
        public string UpdatedText
        {
            get
            {
                DateTime d;
                if (string.IsNullOrEmpty(M.Updated) || !DateTime.TryParse(M.Updated, null, DateTimeStyles.RoundtripKind, out d)) return "";
                var today = DateTime.Today;
                if (d.Date == today) return "aujourd'hui " + d.ToString("HH:mm");
                if (d.Date == today.AddDays(-1)) return "hier " + d.ToString("HH:mm");
                return d.ToString("d MMM", Fr) + (d.Year != today.Year ? " " + d.Year : "");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void Notify(string n) { var h = PropertyChanged; if (h != null) h(this, new PropertyChangedEventArgs(n)); }
    }

    /// <summary>Panneau unique « Notes » : bibliothèque de notes réordonnables, bouton d'ajout mis en avant.</summary>
    public class NotesWindow : DesktopWindow
    {
        readonly ObservableCollection<NoteVm> notes = new ObservableCollection<NoteVm>();
        ItemsControl list;
        TextBlock emptyText, badgeText;
        Border badge;
        Button addBtn;
        readonly Debouncer saveDeb;
        // glisser en direct pour réordonner (poignée à gauche du titre)
        NoteVm dragVm;
        FrameworkElement dragCard;
        Point dragStart, grabInCard;
        bool reordering;
        readonly DragGhost ghost = new DragGhost();

        const string BodyXaml = @"<DockPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:local='clr-namespace:Pupitre;assembly=Pupitre' LastChildFill='True'>
  <DockPanel.Resources>
    <BooleanToVisibilityConverter x:Key='B2V'/>
  </DockPanel.Resources>
  <Button x:Name='Add' DockPanel.Dock='Top' Style='{DynamicResource AccentBtn}' Margin='10,8,10,6' HorizontalAlignment='Stretch' ToolTip='Ajouter une note'>
    <StackPanel Orientation='Horizontal'>
      <Viewbox Width='15' Height='15' Margin='0,0,8,0'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoPlus}' StrokeThickness='2.6'/></Viewbox>
      <TextBlock Text='Nouvelle note'/>
    </StackPanel>
  </Button>
  <Grid>
    <ScrollViewer VerticalScrollBarVisibility='Auto' HorizontalScrollBarVisibility='Disabled' Padding='0,0,4,0' Focusable='False'>
      <StackPanel>
        <ItemsControl x:Name='List' Focusable='False'>
          <ItemsControl.ItemTemplate>
            <DataTemplate>
              <StackPanel>
                <Border Tag='card' Margin='8,0,6,8' CornerRadius='10' Background='{Binding CardBg}' BorderBrush='{Binding CardBorder}' BorderThickness='1' Opacity='{Binding CardOpacity}'>
                  <Grid>
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width='4'/>
                      <ColumnDefinition Width='*'/>
                    </Grid.ColumnDefinitions>
                    <Border CornerRadius='10,0,0,10' Background='{Binding StripeBrush}'/>
                    <StackPanel Grid.Column='1' Margin='6,6,6,7'>
                      <DockPanel>
                        <Border Tag='grip' DockPanel.Dock='Left' Background='Transparent' Padding='3,4,5,4' VerticalAlignment='Center' Cursor='SizeNS' ToolTip='Glisser pour changer l&apos;ordre'>
                          <Viewbox Width='8' Height='13' Opacity='0.4'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoGrip}' StrokeThickness='3.4' TextElement.Foreground='{DynamicResource TextMuted}'/></Viewbox>
                        </Border>
                        <StackPanel DockPanel.Dock='Right' Orientation='Horizontal' VerticalAlignment='Center'>
                          <Button Uid='color' Style='{DynamicResource IconBtnSmall}' ToolTip='Couleur'>
                            <Border Width='12' Height='12' CornerRadius='6' Background='{Binding StripeBrush}' BorderBrush='#66FFFFFF' BorderThickness='1'/>
                          </Button>
                          <Button Uid='delete' Style='{DynamicResource IconBtnSmall}' ToolTip='Supprimer cette note'>
                            <Viewbox Width='13' Height='13'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoTrash}'/></Viewbox>
                          </Button>
                          <Button Uid='expand' Style='{DynamicResource IconBtnSmall}' ToolTip='{Binding ChevronTip}'>
                            <Viewbox Width='13' Height='13'><Path Style='{DynamicResource Ico}' Data='{Binding ChevronData}'/></Viewbox>
                          </Button>
                        </StackPanel>
                        <TextBox Uid='title' Style='{DynamicResource GhostTextBox}' FontWeight='SemiBold' FontSize='13' Text='{Binding Title, UpdateSourceTrigger=PropertyChanged}' Tag='Titre' Padding='0,2,0,2' TextWrapping='Wrap' VerticalAlignment='Center'/>
                      </DockPanel>
                      <Grid Margin='16,2,0,0' Visibility='{Binding IsExpanded, Converter={StaticResource B2V}}'>
                        <ScrollViewer MaxHeight='420' VerticalScrollBarVisibility='Auto' HorizontalScrollBarVisibility='Disabled' Focusable='False' Visibility='{Binding NotTextEditing, Converter={StaticResource B2V}}'>
                          <local:NoteTextView Uid='view' Text='{Binding Text, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}' ToolTip='Cliquer pour modifier le texte'/>
                        </ScrollViewer>
                        <TextBox Uid='text' Style='{DynamicResource GhostTextBox}' Text='{Binding Text, UpdateSourceTrigger=PropertyChanged}' AcceptsReturn='True' AcceptsTab='True' TextWrapping='Wrap' MaxHeight='420' VerticalScrollBarVisibility='Auto' VerticalContentAlignment='Top' Tag='Écrire ici…   ([ ] pour une case à cocher)' FontSize='12.5' Padding='0' Visibility='{Binding IsTextEditing, Converter={StaticResource B2V}}' ToolTip='Échap ou clic ailleurs : fin de la saisie'/>
                      </Grid>
                      <TextBlock Text='{Binding UpdatedText}' Foreground='{DynamicResource TextFaint}' FontSize='10.5' Margin='16,4,0,0' Visibility='{Binding IsExpanded, Converter={StaticResource B2V}}'/>
                    </StackPanel>
                  </Grid>
                </Border>
              </StackPanel>
            </DataTemplate>
          </ItemsControl.ItemTemplate>
        </ItemsControl>
      </StackPanel>
    </ScrollViewer>
    <TextBlock x:Name='EmptyText' Text='Aucune note. Cliquez sur « Nouvelle note ».' Foreground='{DynamicResource TextFaint}' HorizontalAlignment='Center' VerticalAlignment='Center' FontSize='12' TextWrapping='Wrap' TextAlignment='Center' Margin='24' IsHitTestVisible='False'/>
  </Grid>
</DockPanel>";

        public NotesWindow(Controller ctl) : base(ctl)
        {
            SetTitle("Notes");
            MinWidth = 200;
            badge = new Border { CornerRadius = new CornerRadius(9), Padding = new Thickness(7, 1, 7, 2), Background = Theme.Argb(0x33FFFFFF), Visibility = Visibility.Collapsed, VerticalAlignment = VerticalAlignment.Center };
            badgeText = new TextBlock { FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
            badge.Child = badgeText;
            TitleExtra.Children.Add(badge);
            AddStandardActions();

            var body = (DockPanel)XamlReader.Parse(BodyXaml);
            addBtn = (Button)body.FindName("Add");
            list = (ItemsControl)body.FindName("List");
            emptyText = (TextBlock)body.FindName("EmptyText");
            list.ItemsSource = notes;
            Body.Content = body;

            addBtn.Click += delegate { AddNote(); };
            list.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(CardButton));
            // vue lecture → saisie : clic sur le texte, Tab depuis le titre ; retour : Échap ou perte du focus
            list.AddHandler(NoteTextView.EditRequestedEvent, new RoutedEventHandler(delegate(object s, RoutedEventArgs e)
            {
                var fe = e.OriginalSource as FrameworkElement;
                var vm = fe != null ? fe.DataContext as NoteVm : null;
                if (vm == null) return;
                BeginTextEdit(vm);
                e.Handled = true;
            }));
            list.AddHandler(UIElement.LostFocusEvent, new RoutedEventHandler(delegate(object s, RoutedEventArgs e)
            {
                var tb = e.OriginalSource as TextBox;
                if (tb == null || tb.Uid != "text") return;
                var vm = tb.DataContext as NoteVm;
                if (vm != null) vm.IsTextEditing = false;
            }));
            list.PreviewKeyDown += delegate(object s, KeyEventArgs e)
            {
                var tb = Keyboard.FocusedElement as TextBox;
                if (tb == null) return;
                var vm = tb.DataContext as NoteVm;
                if (vm == null) return;
                if (tb.Uid == "title" && e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) == 0) { BeginTextEdit(vm); e.Handled = true; }
                else if (tb.Uid == "text" && e.Key == Key.Escape) { vm.IsTextEditing = false; Focus(); e.Handled = true; }
            };
            list.PreviewMouseLeftButtonDown += ListDown;
            list.PreviewMouseMove += ListMove;
            list.PreviewMouseLeftButtonUp += ListUp;
            list.LostMouseCapture += delegate { EndDrag(); };
            saveDeb = new Debouncer(700, delegate { c.SaveLayoutSoon(); });

            // l'ordre du fichier est l'ordre d'affichage (réordonnable à la poignée)
            foreach (var n in c.L.Notes)
            {
                if (string.IsNullOrEmpty(n.Id)) n.Id = Guid.NewGuid().ToString("N");
                notes.Add(new NoteVm(this, n));
            }
            var look = c.L.NotesPanel;
            SetLook(look.Color, look.Opacity, look.CustomColor);
            UpdateCounts();
        }

        protected override bool CanRename { get { return false; } }
        protected override string SettingsTooltip { get { return "Paramètres du panneau (couleur, opacité, verrou)"; } }
        protected override Size DefaultSize { get { return new Size(416, 470); } }
        protected override void OnOpenSettings() { c.EditPanel(this, c.L.NotesPanel, "Notes"); }
        protected override bool OwnLocked { get { return c.L.NotesPanel.Locked; } }

        public void SaveSoon() { saveDeb.Trigger(); }

        public int Count { get { return notes.Count; } }

        /// <summary>Nouvelle note en tête de liste, dépliée, titre en édition.</summary>
        public void AddNote()
        {
            if (IsRolled) SetRolled(false, true);
            var m = new NoteInfo { Id = Guid.NewGuid().ToString("N"), Title = "", Text = "", Color = NextColor(), Updated = DateTime.Now.ToString("s"), Expanded = true };
            c.L.Notes.Insert(0, m);
            var vm = new NoteVm(this, m);
            notes.Insert(0, vm);
            UpdateCounts();
            c.SaveLayoutSoon();
            Activate();
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate
            {
                var cp = list.ItemContainerGenerator.ContainerFromItem(vm) as FrameworkElement;
                var tb = Ui.FindDescendant<TextBox>(cp, t => t.Uid == "title");
                if (tb != null) { tb.Focus(); Keyboard.Focus(tb); }
            }));
        }

        void BeginTextEdit(NoteVm vm)
        {
            if (!vm.IsExpanded) vm.IsExpanded = true;
            vm.IsTextEditing = true;
            Activate();
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate
            {
                var cp = list.ItemContainerGenerator.ContainerFromItem(vm) as FrameworkElement;
                var tb = Ui.FindDescendant<TextBox>(cp, t => t.Uid == "text");
                if (tb != null) { tb.Focus(); Keyboard.Focus(tb); tb.CaretIndex = tb.Text.Length; }
            }));
        }

        int NextColor()
        {
            int[] order = { 7, 1, 2, 3, 5, 6, 4 };
            return order[notes.Count % order.Length];
        }

        void CardButton(object sender, RoutedEventArgs e)
        {
            var b = e.OriginalSource as Button;
            if (b == null) return;
            var vm = b.DataContext as NoteVm;
            if (vm == null) return;
            switch (b.Uid)
            {
                case "expand": vm.IsExpanded = !vm.IsExpanded; e.Handled = true; break;
                case "delete":
                    if ((vm.Text.Trim().Length == 0 && vm.Title.Trim().Length == 0) || Dialogs.Confirm(this, "Supprimer la note", "« " + (vm.Title.Length > 0 ? vm.Title : "Sans titre") + " » sera supprimée.", "Supprimer", true))
                    {
                        DetailWindow.CloseFor("note:" + vm.M.Id);
                        notes.Remove(vm); c.L.Notes.Remove(vm.M); c.SaveLayoutSoon(); UpdateCounts();
                    }
                    e.Handled = true;
                    break;
                case "color": ShowColorMenu(vm, b); e.Handled = true; break;
            }
        }

        void ShowColorMenu(NoteVm vm, UIElement anchor)
        {
            var menu = new ContextMenu();
            for (int i = 0; i < Palette.Names.Length; i++)
            {
                int v = i;
                var mi = Check(Palette.Names[i], vm.Color == i, delegate { vm.Color = v; });
                mi.Icon = new Border { Width = 12, Height = 12, CornerRadius = new CornerRadius(6), Background = Palette.Brush(i), BorderBrush = Theme.Argb(0x55FFFFFF), BorderThickness = new Thickness(1) };
                menu.Items.Add(mi);
            }
            menu.PlacementTarget = anchor; menu.Placement = PlacementMode.Bottom; menu.IsOpen = true;
        }

        /// <summary>Double-clic sur une carte : la note en grand dans une fenêtre flottante, modifiable (enregistrement automatique).</summary>
        public void OpenDetail(NoteVm vm)
        {
            if (vm == null) return;
            var note = vm;
            DetailWindow.Show(this, "note", "note:" + note.M.Id, note.Title.Length > 0 ? note.Title : "Note", delegate(DetailWindow w)
            {
                var root = new DockPanel { LastChildFill = true };
                var head = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
                var color = new Button { Style = Theme.Res<Style>("IconBtnSmall"), ToolTip = "Couleur", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
                var swatch = new Border { Width = 14, Height = 14, CornerRadius = new CornerRadius(7), BorderBrush = Theme.Argb(0x66FFFFFF), BorderThickness = new Thickness(1), DataContext = note };
                swatch.SetBinding(Border.BackgroundProperty, new Binding("StripeBrush"));
                color.Content = swatch;
                color.Click += delegate { ShowColorMenu(note, color); };
                DockPanel.SetDock(color, Dock.Left);
                head.Children.Add(color);
                var title = new TextBox { Style = Theme.Res<Style>("GhostTextBox"), FontSize = 17, FontWeight = FontWeights.SemiBold, Tag = "Titre", Padding = new Thickness(0, 2, 0, 2), VerticalAlignment = VerticalAlignment.Center, DataContext = note };
                title.SetBinding(TextBox.TextProperty, new Binding("Title") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                head.Children.Add(title);
                DockPanel.SetDock(head, Dock.Top);
                root.Children.Add(head);
                var foot = new TextBlock { FontSize = 10.5, Foreground = Theme.Res<Brush>("TextFaint"), Margin = new Thickness(0, 6, 0, 0), DataContext = note };
                foot.SetBinding(TextBlock.TextProperty, new Binding("UpdatedText"));
                DockPanel.SetDock(foot, Dock.Bottom);
                root.Children.Add(foot);
                var text = new TextBox { Style = Theme.Res<Style>("GhostTextBox"), Tag = "Texte de la note…", AcceptsReturn = true, AcceptsTab = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, FontSize = 13.5, Padding = new Thickness(2), VerticalContentAlignment = VerticalAlignment.Top, DataContext = note, MinHeight = 72, MaxHeight = Math.Max(200, SystemParameters.WorkArea.Height - 260) };
                text.SetBinding(TextBox.TextProperty, new Binding("Text") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                root.Children.Add(text);
                PropertyChangedEventHandler onProp = delegate(object s, PropertyChangedEventArgs e) { if (e.PropertyName == "Title") w.SetTitle(note.Title.Length > 0 ? note.Title : "Note"); };
                note.PropertyChanged += onProp;
                w.Closed += delegate { note.PropertyChanged -= onProp; };
                w.Loaded += delegate { text.Focus(); text.CaretIndex = text.Text.Length; };
                return root;
            }, 540, 150);
        }

        // ---- Réordonnancement à la poignée --------------------------------------------------------------

        void ListDown(object sender, MouseButtonEventArgs e)
        {
            var grip = Ui.HitTag(e.OriginalSource, "grip");
            if (grip == null)
            {
                // double-clic sur une carte (hors boutons et cases) : la note en grand dans une fenêtre
                if (e.ClickCount != 2 || Ui.FindAncestor<ButtonBase>(e.OriginalSource as DependencyObject) != null) return;
                var card = Ui.HitTag(e.OriginalSource, "card");
                var nv = card != null ? card.DataContext as NoteVm : null;
                if (nv == null) return;
                nv.IsTextEditing = false;
                OpenDetail(nv);
                e.Handled = true;
                return;
            }
            dragVm = grip.DataContext as NoteVm;
            if (dragVm == null) return;
            dragCard = Ui.HitTag(grip, "card");
            grabInCard = dragCard != null ? e.GetPosition(dragCard) : new Point();
            dragStart = e.GetPosition(list);
            reordering = false;
            Activate();                 // avant la capture : l'activation d'une fenêtre inactive relâcherait la capture
            list.CaptureMouse();
            e.Handled = true;
        }

        /// <summary>Glisser en direct : la note change de place dès que le curseur dépasse le milieu de sa voisine.</summary>
        void ListMove(object sender, MouseEventArgs e)
        {
            if (dragVm == null || e.LeftButton != MouseButtonState.Pressed) return;
            var p = e.GetPosition(list);
            if (!reordering && Math.Abs(p.Y - dragStart.Y) < 4) return;
            if (!reordering) { reordering = true; ghost.Start(this, dragCard, grabInCard); dragVm.IsDragging = true; }
            ghost.Move(DragGhost.ScreenDip(this, e));
            int cur = notes.IndexOf(dragVm);
            if (cur < 0) return;
            if (cur > 0)
            {
                var above = list.ItemContainerGenerator.ContainerFromIndex(cur - 1) as FrameworkElement;
                if (above != null)
                {
                    double mid = above.TransformToAncestor(list).Transform(new Point(0, 0)).Y + above.ActualHeight / 2;
                    if (p.Y < mid) { MoveNote(cur, cur - 1); return; }
                }
            }
            if (cur < notes.Count - 1)
            {
                var below = list.ItemContainerGenerator.ContainerFromIndex(cur + 1) as FrameworkElement;
                if (below != null)
                {
                    double mid = below.TransformToAncestor(list).Transform(new Point(0, 0)).Y + below.ActualHeight / 2;
                    if (p.Y > mid) MoveNote(cur, cur + 1);
                }
            }
        }

        void ListUp(object sender, MouseButtonEventArgs e)
        {
            if (dragVm == null) return;
            bool did = reordering;
            EndDrag();
            if (did) e.Handled = true;
        }

        void EndDrag()
        {
            ghost.End();
            if (dragVm != null) dragVm.IsDragging = false;
            dragVm = null; dragCard = null; reordering = false;
            if (list.IsMouseCaptured) list.ReleaseMouseCapture();
        }

        /// <summary>Déplace une note d'un cran ; le fichier suit le même ordre.</summary>
        void MoveNote(int from, int to)
        {
            if (from < 0 || to < 0 || from >= notes.Count || to >= notes.Count || from == to) return;
            notes.Move(from, to);
            var m = c.L.Notes[from];
            c.L.Notes.RemoveAt(from);
            c.L.Notes.Insert(to, m);
            c.SaveLayoutSoon();
        }

        void UpdateCounts()
        {
            int n = notes.Count;
            emptyText.Visibility = n == 0 ? Visibility.Visible : Visibility.Collapsed;
            badgeText.Text = n.ToString();
            badge.Visibility = n > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        protected override void SaveRect()
        {
            var t = c.L.NotesPanel;
            t.X = Left; t.Y = Top; t.W = Width; t.H = RestoreHeight; t.Rolled = IsRolled; t.RolledH = RestoreHeight; t.Set = true;
            string sc; double rx, ry; CurrentScreen(out sc, out rx, out ry); t.Screen = sc; t.RelX = rx; t.RelY = ry;
            c.SaveLayoutSoon();
        }
    }
}
