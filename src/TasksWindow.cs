using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace Pupitre
{
    /// <summary>Masque un élément quand la largeur liée est sous un seuil (en-têtes de catégorie des panneaux étroits).</summary>
    public class NarrowConverter : System.Windows.Data.IValueConverter
    {
        public double Threshold { get; set; }
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double w = value is double ? (double)value : 0;
            return w > 0 && w < Threshold ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) { return Binding.DoNothing; }
    }

    /// <summary>
    /// Panneau Tâches façon « Suivi de projets » : catégories (cartes colorées) → thématiques (badge service, statut,
    /// priorité, commentaires, échéance) → tâches (cases à cocher). Filtre par service, recherche, glisser en direct.
    /// </summary>
    public class TasksWindow : DesktopWindow
    {
        readonly TasksVm vm;
        ItemsControl cats;
        TextBox searchBox;
        TextBlock footer, emptyText, badgeText;
        Button archiveBtn, filterBtn, addCatBtn, manageBtn;
        Border badge, searchRow;
        Debouncer searchDeb;
        // glisser en direct
        TaskVm dragVm;
        ItemsControl dragHost;
        FrameworkElement dragRow;
        Point dragStart, grabInRow;
        bool dragging;
        readonly DragGhost ghost = new DragGhost();
        Popup commentsPopup;

        const string BodyXaml = @"<DockPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:local='clr-namespace:Pupitre;assembly=Pupitre' LastChildFill='True'>
  <DockPanel.Resources>
    <BooleanToVisibilityConverter x:Key='B2V'/>
    <local:NarrowConverter x:Key='Narrow' Threshold='330'/>
    <local:NarrowConverter x:Key='VeryNarrow' Threshold='250'/>
    <Style x:Key='RowBorder' TargetType='{x:Type Border}'>
      <Setter Property='Background' Value='Transparent'/>
      <Style.Triggers>
        <Trigger Property='IsMouseOver' Value='True'>
          <Setter Property='Background' Value='{DynamicResource Hover}'/>
        </Trigger>
        <DataTrigger Binding='{Binding IsDragging}' Value='True'>
          <Setter Property='Background' Value='{DynamicResource Selected}'/>
        </DataTrigger>
      </Style.Triggers>
    </Style>
    <Style x:Key='HoverReveal' TargetType='{x:Type Border}'>
      <Setter Property='Opacity' Value='0'/>
      <Style.Triggers>
        <DataTrigger Binding='{Binding IsMouseOver, RelativeSource={RelativeSource AncestorType={x:Type Border}}}' Value='True'>
          <Setter Property='Opacity' Value='1'/>
        </DataTrigger>
      </Style.Triggers>
    </Style>

    <DataTemplate x:Key='TaskTpl'>
      <Border Tag='row' Style='{StaticResource RowBorder}' Margin='0,0,0,1' Padding='2,1,2,1' CornerRadius='6' Opacity='{Binding RowOpacity}'>
        <DockPanel MinHeight='24'>
          <Border Tag='grip' DockPanel.Dock='Left' Background='Transparent' Padding='2,3,3,3' VerticalAlignment='Center' Cursor='SizeNS' Visibility='{Binding GripVisibility}' ToolTip='Glisser pour changer l&apos;ordre'>
            <Viewbox Width='7' Height='11' Opacity='0.35'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoGrip}' StrokeThickness='3.4' TextElement.Foreground='{DynamicResource TextMuted}'/></Viewbox>
          </Border>
          <ToggleButton DockPanel.Dock='Left' Style='{DynamicResource CheckCircle}' Width='20' Height='20' Background='{DynamicResource Ok}' IsChecked='{Binding IsDone, Mode=TwoWay}' VerticalAlignment='Center' Margin='0,0,6,0' ToolTip='Sous-tâche réalisée / à faire'/>
          <Border DockPanel.Dock='Right' Style='{StaticResource HoverReveal}' VerticalAlignment='Center'>
            <Button Tag='del' Style='{DynamicResource IconBtnSmall}' Width='18' Height='18' ToolTip='Supprimer la sous-tâche'>
              <Viewbox Width='10' Height='10'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoClose}' StrokeThickness='2.4'/></Viewbox>
            </Button>
          </Border>
          <TextBlock DockPanel.Dock='Right' Text='{Binding DoneAtText}' FontSize='10.5' Foreground='{DynamicResource TextFaint}' VerticalAlignment='Center' Margin='6,0,2,0'/>
          <Grid VerticalAlignment='Center'>
            <TextBlock Tag='title' Text='{Binding Title}' TextTrimming='CharacterEllipsis' FontSize='12.5' Foreground='{DynamicResource Text}' TextDecorations='{Binding Strike}' Cursor='IBeam' ToolTip='Cliquer pour modifier' Visibility='{Binding NotEditing, Converter={StaticResource B2V}}'/>
            <TextBox Uid='editor' Style='{DynamicResource GhostTextBox}' Text='{Binding Title, UpdateSourceTrigger=PropertyChanged}' FontSize='12.5' Padding='0' Margin='-2,0,0,0' Visibility='{Binding IsEditing, Converter={StaticResource B2V}}'/>
          </Grid>
        </DockPanel>
      </Border>
    </DataTemplate>

    <DataTemplate x:Key='ThemeTpl'>
      <Border Tag='row' Style='{StaticResource RowBorder}' Margin='0,0,0,2' Padding='4,3,6,4' CornerRadius='8' Opacity='{Binding RowOpacity}'>
        <StackPanel>
          <DockPanel MinHeight='24'>
            <Border Tag='grip' DockPanel.Dock='Left' Background='Transparent' Padding='2,4,3,4' VerticalAlignment='Center' Cursor='SizeNS' Visibility='{Binding GripVisibility}' ToolTip='Glisser pour changer l&apos;ordre'>
              <Viewbox Width='7' Height='11' Opacity='0.35'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoGrip}' StrokeThickness='3.4' TextElement.Foreground='{DynamicResource TextMuted}'/></Viewbox>
            </Border>
            <Button Tag='toggle' DockPanel.Dock='Left' Style='{DynamicResource IconBtnSmall}' Width='18' Height='18' VerticalAlignment='Center' Margin='0,0,2,0' ToolTip='Déplier / replier les sous-tâches'>
              <Viewbox Width='10' Height='10'><Path Style='{DynamicResource Ico}' Data='{Binding ChevronData}' StrokeThickness='2.4'/></Viewbox>
            </Button>
            <StackPanel DockPanel.Dock='Right' Orientation='Horizontal' VerticalAlignment='Center'>
              <Button Tag='flag' Style='{DynamicResource IconBtnSmall}' Width='20' Height='20' ToolTip='{Binding FlagTip}'>
                <Viewbox Width='12' Height='12'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoFlag}' Stroke='{Binding FlagBrush}' Fill='{Binding FlagFill}'/></Viewbox>
              </Button>
              <Button Tag='cm' Style='{DynamicResource IconBtnSmall}' Width='22' Height='20' ToolTip='Commentaires et historique'>
                <Grid>
                  <Viewbox Width='13' Height='13'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoComment}' Stroke='{Binding CommentBrush}'/></Viewbox>
                  <Border CornerRadius='6' Background='{DynamicResource Accent}' MinWidth='12' Height='12' HorizontalAlignment='Right' VerticalAlignment='Top' Margin='0,-5,-6,0' Visibility='{Binding HasComments, Converter={StaticResource B2V}}'>
                    <TextBlock Text='{Binding CommentCount}' FontSize='8.5' FontWeight='Bold' Foreground='White' HorizontalAlignment='Center' VerticalAlignment='Center' Margin='2,0,2,0'/>
                  </Border>
                </Grid>
              </Button>
              <Button Tag='more' Style='{DynamicResource IconBtnSmall}' Width='20' Height='20' ToolTip='Options de la tâche'>
                <Viewbox Width='14' Height='14'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoDots}'/></Viewbox>
              </Button>
            </StackPanel>
            <Grid VerticalAlignment='Center'>
              <TextBlock Tag='title' Text='{Binding Title}' TextTrimming='CharacterEllipsis' FontSize='13' FontWeight='SemiBold' Foreground='{DynamicResource Text}' TextDecorations='{Binding Strike}' Cursor='IBeam' ToolTip='Cliquer pour modifier' Visibility='{Binding NotEditing, Converter={StaticResource B2V}}'/>
              <TextBox Uid='editor' Style='{DynamicResource GhostTextBox}' Text='{Binding Title, UpdateSourceTrigger=PropertyChanged}' FontSize='13' FontWeight='SemiBold' Padding='0' Margin='-2,0,0,0' Visibility='{Binding IsEditing, Converter={StaticResource B2V}}'/>
            </Grid>
          </DockPanel>
          <WrapPanel Margin='20,2,0,0'>
            <Button Tag='svc' Style='{DynamicResource ChipColored}' Background='{Binding SvcBg}' Foreground='{Binding SvcFg}' Content='{Binding SvcName}' FontWeight='SemiBold' ToolTip='Service rattaché (cliquer pour changer)' Margin='0,0,4,2'/>
            <Button Tag='status' Style='{DynamicResource ChipColored}' Background='{Binding StBg}' Foreground='{Binding StFg}' ToolTip='Statut (cliquer pour changer)' Margin='0,0,4,2'>
              <StackPanel Orientation='Horizontal'>
                <Border Width='6' Height='6' CornerRadius='3' Background='{Binding StDot}' Margin='0,0,5,0' VerticalAlignment='Center'/>
                <TextBlock Text='{Binding StLabel}'/>
              </StackPanel>
            </Button>
            <Button Tag='due' Style='{DynamicResource ChipColored}' Background='{Binding DueBg}' Foreground='{Binding DueFg}' Visibility='{Binding DueVisible, Converter={StaticResource B2V}}' ToolTip='Échéance' Margin='0,0,4,2'>
              <StackPanel Orientation='Horizontal'>
                <Viewbox Width='11' Height='11' Margin='0,0,4,0'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoCalendar}'/></Viewbox>
                <TextBlock Text='{Binding DueText}'/>
              </StackPanel>
            </Button>
            <TextBlock Text='{Binding ProgressText}' FontSize='10.5' Foreground='{DynamicResource TextFaint}' VerticalAlignment='Center' Margin='3,0,0,2' Visibility='{Binding ProgressVisible, Converter={StaticResource B2V}}' ToolTip='Tâches réalisées / total'/>
          </WrapPanel>
          <TextBlock Text='{Binding LastComment}' FontSize='11' FontStyle='Italic' Foreground='{DynamicResource TextFaint}' TextTrimming='CharacterEllipsis' Margin='20,2,0,0' Visibility='{Binding HasComments, Converter={StaticResource B2V}}' ToolTip='{Binding LastComment}'/>
          <StackPanel Margin='18,4,0,0' Visibility='{Binding IsOpen, Converter={StaticResource B2V}}'>
            <ItemsControl Tag='tasks' ItemsSource='{Binding ActiveTasks}' ItemTemplate='{StaticResource TaskTpl}' Focusable='False'/>
            <Button Tag='donetoggle' Style='{DynamicResource LinkBtn}' Content='{Binding DoneTasksLabel}' Visibility='{Binding HasDoneTasks, Converter={StaticResource B2V}}' HorizontalAlignment='Left' FontSize='10.5' Margin='4,1,0,1'/>
            <ItemsControl ItemsSource='{Binding DoneTasks}' ItemTemplate='{StaticResource TaskTpl}' Focusable='False' Visibility='{Binding DoneOpen, Converter={StaticResource B2V}}'/>
            <DockPanel Margin='2,1,0,0' Height='24'>
              <Viewbox DockPanel.Dock='Left' Width='11' Height='11' Margin='5,0,8,0' VerticalAlignment='Center'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoPlus}' StrokeThickness='2.4' TextElement.Foreground='{DynamicResource TextFaint}'/></Viewbox>
              <TextBox Uid='newtask' Style='{DynamicResource GhostTextBox}' Tag='Ajouter une sous-tâche…' FontSize='12.5' Padding='0' VerticalAlignment='Center' ToolTip='Entrée pour ajouter'/>
            </DockPanel>
          </StackPanel>
        </StackPanel>
      </Border>
    </DataTemplate>
  </DockPanel.Resources>

  <DockPanel DockPanel.Dock='Top' Margin='10,6,10,4'>
    <Button x:Name='AddCat' DockPanel.Dock='Right' Style='{DynamicResource ChipColored}' Background='{DynamicResource Accent}' Foreground='White' FontWeight='SemiBold' FontSize='11.5' ToolTip='Nouvelle catégorie' Margin='6,0,0,0'>
      <StackPanel Orientation='Horizontal'>
        <Viewbox Width='10' Height='10' Margin='0,0,5,0'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoPlus}' StrokeThickness='2.8' Stroke='White'/></Viewbox>
        <TextBlock Text='Catégorie'/>
      </StackPanel>
    </Button>
    <Button x:Name='Manage' DockPanel.Dock='Right' Style='{DynamicResource IconBtnSmall}' Width='24' Height='24' ToolTip='Services et catégories : noms, couleurs' Margin='4,0,0,0'>
      <Viewbox Width='14' Height='14'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoTag}'/></Viewbox>
    </Button>
    <Button x:Name='FilterBtn' Style='{DynamicResource ChipBtn}' Content='Tous les services ▾' HorizontalAlignment='Left' ToolTip='Filtrer par service'/>
  </DockPanel>
  <Border x:Name='SearchRow' DockPanel.Dock='Top' Margin='10,0,10,4' Visibility='Collapsed'>
    <TextBox x:Name='SearchBox' Style='{DynamicResource FieldTextBox}' Tag='Rechercher (tâches, sous-tâches, commentaires)…'/>
  </Border>
  <DockPanel DockPanel.Dock='Bottom' Margin='14,4,14,9'>
    <Button x:Name='Archive' DockPanel.Dock='Right' Style='{DynamicResource LinkBtn}' Content='Archiver les terminés' VerticalAlignment='Center'/>
    <TextBlock x:Name='Footer' Foreground='{DynamicResource TextFaint}' FontSize='11' VerticalAlignment='Center' TextTrimming='CharacterEllipsis'/>
  </DockPanel>
  <Grid>
    <ScrollViewer VerticalScrollBarVisibility='Auto' HorizontalScrollBarVisibility='Disabled' Padding='6,0,6,0' Focusable='False'>
      <ItemsControl x:Name='Cats' Tag='cats' Focusable='False' Margin='4,2,2,2'>
        <ItemsControl.ItemTemplate>
          <DataTemplate>
            <Border Tag='cat' Margin='0,0,0,8' CornerRadius='10' Background='{DynamicResource FieldBg}' BorderBrush='{DynamicResource Line}' BorderThickness='1' Opacity='{Binding RowOpacity}'>
              <Grid>
                <Grid.ColumnDefinitions>
                  <ColumnDefinition Width='3'/>
                  <ColumnDefinition Width='*'/>
                </Grid.ColumnDefinitions>
                <Border CornerRadius='9,0,0,9' Background='{Binding ColorBrush}'/>
                <StackPanel Grid.Column='1'>
                  <Border Tag='cathead' Background='{Binding HeadBg}' CornerRadius='{Binding HeadRadius}' Cursor='Hand'>
                    <DockPanel MinHeight='34' Margin='4,0,4,0'>
                      <Border Tag='grip' DockPanel.Dock='Left' Background='Transparent' Padding='3,4,4,4' VerticalAlignment='Center' Cursor='SizeNS' Visibility='{Binding GripVisibility}' ToolTip='Glisser pour changer l&apos;ordre des catégories'>
                        <Viewbox Width='7' Height='11' Opacity='0.4'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoGrip}' StrokeThickness='3.4' TextElement.Foreground='{DynamicResource TextMuted}'/></Viewbox>
                      </Border>
                      <Button Tag='toggle' DockPanel.Dock='Left' Style='{DynamicResource IconBtnSmall}' Width='20' Height='20' VerticalAlignment='Center' Foreground='{Binding NameBrush}' ToolTip='Replier / déplier'>
                        <Viewbox Width='11' Height='11'><Path Style='{DynamicResource Ico}' Data='{Binding ChevronData}' StrokeThickness='2.6'/></Viewbox>
                      </Button>
                      <Button Tag='addtheme' DockPanel.Dock='Right' Style='{DynamicResource ChipBtn}' ToolTip='Ajouter une tâche' Margin='4,0,0,0' VerticalAlignment='Center'>
                        <StackPanel Orientation='Horizontal'>
                          <Viewbox Width='10' Height='10' Margin='0,0,4,0'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoPlus}' StrokeThickness='2.6'/></Viewbox>
                          <TextBlock Text='Tâche' Visibility='{Binding ActualWidth, ElementName=Cats, Converter={StaticResource VeryNarrow}}'/>
                        </StackPanel>
                      </Button>
                      <Button Tag='more' DockPanel.Dock='Right' Style='{DynamicResource IconBtnSmall}' Width='20' Height='20' VerticalAlignment='Center' ToolTip='Options de la catégorie'>
                        <Viewbox Width='14' Height='14'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoDots}'/></Viewbox>
                      </Button>
                      <TextBlock DockPanel.Dock='Right' Text='{Binding CountText}' FontSize='11' Foreground='{DynamicResource TextMuted}' VerticalAlignment='Center' Margin='8,0,4,0' Visibility='{Binding ActualWidth, ElementName=Cats, Converter={StaticResource Narrow}}'/>
                      <Grid VerticalAlignment='Center' Margin='2,0,0,0'>
                        <TextBlock Tag='title' Text='{Binding Title}' FontWeight='Bold' FontSize='13' Foreground='{Binding NameBrush}' TextTrimming='CharacterEllipsis' ToolTip='Double-clic pour renommer' Visibility='{Binding NotEditing, Converter={StaticResource B2V}}'/>
                        <TextBox Uid='editor' Style='{DynamicResource GhostTextBox}' Text='{Binding Title, UpdateSourceTrigger=PropertyChanged}' FontSize='13' FontWeight='Bold' Padding='0' Margin='-2,0,0,0' Visibility='{Binding IsEditing, Converter={StaticResource B2V}}'/>
                      </Grid>
                    </DockPanel>
                  </Border>
                  <StackPanel Margin='4,4,4,2' Visibility='{Binding IsOpen, Converter={StaticResource B2V}}'>
                    <ItemsControl Tag='themes' ItemsSource='{Binding Themes}' ItemTemplate='{StaticResource ThemeTpl}' Focusable='False'/>
                    <Border Margin='4,2,2,4' Visibility='{Binding Adding, Converter={StaticResource B2V}}'>
                      <TextBox Uid='newtheme' Style='{DynamicResource FieldTextBox}' Tag='Nom de la tâche…  (Entrée pour créer, Échap pour annuler)'/>
                    </Border>
                    <TextBlock Text='Aucune tâche : bouton « + Tâche ».' FontSize='11.5' Foreground='{DynamicResource TextFaint}' Margin='8,4,0,6' Visibility='{Binding IsEmpty, Converter={StaticResource B2V}}'/>
                    <Button Tag='donethemes' Style='{DynamicResource LinkBtn}' Content='{Binding DoneThemesLabel}' Visibility='{Binding HasDoneThemes, Converter={StaticResource B2V}}' HorizontalAlignment='Left' FontSize='10.5' Margin='6,2,0,2'/>
                    <ItemsControl ItemsSource='{Binding DoneThemes}' ItemTemplate='{StaticResource ThemeTpl}' Focusable='False' Visibility='{Binding DoneOpen, Converter={StaticResource B2V}}'/>
                  </StackPanel>
                </StackPanel>
              </Grid>
            </Border>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>
    </ScrollViewer>
    <TextBlock x:Name='EmptyText' Foreground='{DynamicResource TextFaint}' HorizontalAlignment='Center' VerticalAlignment='Center' FontSize='12' Visibility='Collapsed' IsHitTestVisible='False' TextAlignment='Center' TextWrapping='Wrap' Margin='24'/>
  </Grid>
</DockPanel>";

        const string EntryXaml = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <DataTemplate.Resources>
    <BooleanToVisibilityConverter x:Key='B2V'/>
  </DataTemplate.Resources>
  <Border Tag='entry' Margin='0,0,0,5' Padding='{Binding Pad}' CornerRadius='8' Background='{Binding Bg}'>
    <DockPanel>
      <StackPanel DockPanel.Dock='Right' Orientation='Horizontal' VerticalAlignment='Top' Margin='6,-2,-4,0' Visibility='{Binding CanEdit, Converter={StaticResource B2V}}'>
        <StackPanel.Style>
          <Style TargetType='{x:Type StackPanel}'>
            <Setter Property='Opacity' Value='0'/>
            <Style.Triggers>
              <DataTrigger Binding='{Binding IsMouseOver, RelativeSource={RelativeSource AncestorType={x:Type Border}}}' Value='True'>
                <Setter Property='Opacity' Value='1'/>
              </DataTrigger>
              <DataTrigger Binding='{Binding IsEditing}' Value='True'>
                <Setter Property='Opacity' Value='0'/>
              </DataTrigger>
            </Style.Triggers>
          </Style>
        </StackPanel.Style>
        <Button Tag='cedit' Style='{DynamicResource IconBtnSmall}' Width='18' Height='18' ToolTip='Modifier le commentaire'>
          <Viewbox Width='11' Height='11'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoPencil}'/></Viewbox>
        </Button>
        <Button Tag='cdel' Style='{DynamicResource IconBtnSmall}' Width='18' Height='18' ToolTip='Supprimer le commentaire'>
          <Viewbox Width='11' Height='11'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoTrash}'/></Viewbox>
        </Button>
      </StackPanel>
      <StackPanel>
        <TextBlock Tag='ctext' Text='{Binding Text}' TextWrapping='Wrap' FontSize='12' FontStyle='{Binding Style}' Foreground='{Binding Fg}' Visibility='{Binding NotEditing, Converter={StaticResource B2V}}' Cursor='IBeam' ToolTip='Cliquer pour modifier'/>
        <TextBox Uid='ceditor' Style='{DynamicResource GhostTextBox}' Text='{Binding Text, UpdateSourceTrigger=PropertyChanged}' FontSize='12' AcceptsReturn='True' TextWrapping='Wrap' Padding='0' Margin='-2,0,0,0' Visibility='{Binding IsEditing, Converter={StaticResource B2V}}' ToolTip='Entrée : enregistrer · Maj+Entrée : saut de ligne · Échap : annuler'/>
        <TextBlock Text='{Binding AtText}' FontSize='10' Foreground='{DynamicResource TextFaint}' Margin='0,2,0,0'/>
      </StackPanel>
    </DockPanel>
  </Border>
</DataTemplate>";

        public TasksWindow(Controller ctl) : base(ctl)
        {
            vm = c.Tasks;
            SetTitle("Tâches");
            MinWidth = 240;

            badge = new Border { CornerRadius = new CornerRadius(9), Padding = new Thickness(7, 1, 7, 2), Background = Theme.Res<Brush>("Danger"), Visibility = Visibility.Collapsed, VerticalAlignment = VerticalAlignment.Center };
            badgeText = new TextBlock { FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
            badge.Child = badgeText;
            TitleExtra.Children.Add(badge);

            var searchBtn = Theme.IconButton("IcoSearch", "Rechercher", 15);
            searchBtn.Click += delegate { ToggleSearch(); };
            AddAction(searchBtn);
            var moreBtn = Theme.IconButton("IcoSort", "Copier la liste, archiver", 15);
            moreBtn.Click += delegate { ShowPanelMenu(); };
            AddAction(moreBtn);
            AddStandardActions();

            var body = (DockPanel)XamlReader.Parse(BodyXaml);
            cats = (ItemsControl)body.FindName("Cats");
            searchBox = (TextBox)body.FindName("SearchBox");
            searchRow = (Border)body.FindName("SearchRow");
            footer = (TextBlock)body.FindName("Footer");
            emptyText = (TextBlock)body.FindName("EmptyText");
            archiveBtn = (Button)body.FindName("Archive");
            filterBtn = (Button)body.FindName("FilterBtn");
            addCatBtn = (Button)body.FindName("AddCat");
            manageBtn = (Button)body.FindName("Manage");
            cats.ItemsSource = vm.Cats;
            Body.Content = body;

            searchDeb = new Debouncer(220, delegate { vm.SetSearch(searchBox.Text); UpdateCounts(); });
            searchBox.TextChanged += delegate { searchDeb.Trigger(); };
            searchBox.KeyDown += delegate(object s, KeyEventArgs e) { if (e.Key == Key.Escape) { ToggleSearch(); e.Handled = true; } };
            filterBtn.Click += delegate { ShowFilterMenu(); };
            addCatBtn.Click += delegate { NewCategory(); };
            manageBtn.Click += delegate { OpenManage(); };
            archiveBtn.Click += delegate { ArchiveDone(); };
            cats.PreviewMouseLeftButtonDown += ListDown;
            cats.PreviewMouseMove += ListMove;
            cats.PreviewMouseLeftButtonUp += ListUp;
            cats.LostMouseCapture += delegate { EndDrag(); };
            cats.PreviewKeyDown += ListKey;
            cats.MouseRightButtonUp += ListRightUp;
            cats.AddHandler(UIElement.LostFocusEvent, new RoutedEventHandler(ListLostFocus));
            cats.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(ListButtonClick));
            vm.Cats.CollectionChanged += delegate { UpdateEmpty(); };
            vm.CountsChanged += UpdateCounts;
            var look = c.L.Tasks;
            SetLook(look.Color, look.Opacity, look.CustomColor);
            UpdateCounts();
            UpdateEmpty();
        }

        // ---- barre d'outils ---------------------------------------------------------------------------

        void ToggleSearch()
        {
            bool show = searchRow.Visibility != Visibility.Visible;
            searchRow.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (show) { if (IsRolled) SetRolled(false, true); Activate(); searchBox.Focus(); Keyboard.Focus(searchBox); }
            else { searchBox.Text = ""; vm.SetSearch(""); UpdateCounts(); Focus(); }
        }

        public void NewCategory()
        {
            if (IsRolled) SetRolled(false, true);
            var name = Dialogs.Prompt(this, "Nouvelle catégorie", "Nom de la catégorie (ex : Création BI)", "", false);
            if (string.IsNullOrWhiteSpace(name)) return;
            var cat = vm.AddCategory(name);
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate { BeginAddTheme(cat); }));
        }

        void OpenManage()
        {
            var dlg = new TasksSettingsDialog(this, c, vm);
            dlg.ShowDialog();
            vm.Rebuild();
            UpdateCounts();
        }

        void ShowFilterMenu()
        {
            var menu = new ContextMenu();
            var head = new MenuItem { Header = "Services", IsEnabled = false, FontSize = 11 };
            menu.Items.Add(head);
            foreach (var s in vm.Services)
            {
                var svc = s;
                int n = vm.ServiceUsage(svc);
                var mi = new MenuItem { Header = svc.Name + "   ·   " + n, IsChecked = vm.ServiceFilter.Contains(svc.Id), StaysOpenOnClick = true, IsCheckable = true };
                mi.Icon = new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(5), Background = TaskColors.Solid(svc.Color, svc.CustomColor) };
                mi.Click += delegate { vm.ToggleServiceFilter(svc.Id); mi.IsChecked = vm.ServiceFilter.Contains(svc.Id); UpdateCounts(); };
                menu.Items.Add(mi);
            }
            if (vm.Services.Count == 0) menu.Items.Add(new MenuItem { Header = "Aucun service défini", IsEnabled = false });
            menu.Items.Add(new Separator());
            var clear = Item("Tous les services (effacer le filtre)", "IcoClose", delegate { vm.ClearFilter(); UpdateCounts(); });
            clear.IsEnabled = vm.ServiceFilter.Count > 0;
            menu.Items.Add(clear);
            menu.Items.Add(Item("Gérer les services…", "IcoTag", OpenManage));
            Open(menu, filterBtn);
        }

        void ShowPanelMenu()
        {
            var menu = new ContextMenu();
            menu.Items.Add(Item("Nouvelle catégorie…", "IcoPlus", NewCategory));
            menu.Items.Add(Item("Services et catégories…", "IcoTag", OpenManage));
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Copier le suivi (texte)", "IcoCopy", delegate { try { Clipboard.SetText(vm.ExportText()); c.Toast("Suivi copié dans le presse-papiers"); } catch { } }));
            menu.Items.Add(Item("Archiver les tâches terminées", "IcoCheckSquare", ArchiveDone));
            Open(menu, null);
        }

        void ArchiveDone()
        {
            if (vm.CountThemesDone == 0) { c.Toast("Aucune tâche terminée à archiver"); return; }
            if (!Dialogs.Confirm(this, "Archiver les terminés", vm.CountThemesDone + " tâche(s) terminée(s) et leurs sous-tâches seront déplacées dans archive.json (dossier des données).", "Archiver", false)) return;
            int n = vm.ArchiveDone();
            c.Toast(n + " tâche(s) archivée(s)");
        }

        void Open(ContextMenu menu, UIElement anchor)
        {
            if (anchor != null) { menu.PlacementTarget = anchor; menu.Placement = PlacementMode.Bottom; }
            else { menu.PlacementTarget = this; menu.Placement = PlacementMode.MousePoint; }
            menu.IsOpen = true;
        }

        // ---- compteurs, pied, vide --------------------------------------------------------------------

        void UpdateCounts()
        {
            int nf = vm.ServiceFilter.Count;
            if (nf == 0) filterBtn.Content = "Tous les services ▾";
            else if (nf == 1) { var s = vm.ServiceById(vm.ServiceFilter.First()); filterBtn.Content = (s != null ? s.Name : "Service") + " ▾"; }
            else filterBtn.Content = "Services · " + nf + " ▾";
            filterBtn.Foreground = nf > 0 ? Theme.Res<Brush>("Text") : Theme.Res<Brush>("TextMuted");

            var parts = new List<string>();
            parts.Add(vm.CountThemesDoing + " tâche" + (vm.CountThemesDoing > 1 ? "s" : "") + " en cours");
            if (vm.CountTasksTotal > 0) parts.Add(vm.CountTasksDone + "/" + vm.CountTasksTotal + " sous-tâches réalisées");
            if (vm.CountOverdue > 0) parts.Add(vm.CountOverdue + " en retard");
            else if (vm.CountToday > 0) parts.Add(vm.CountToday + " aujourd'hui");
            footer.Text = string.Join(" · ", parts.ToArray());
            archiveBtn.Visibility = vm.CountThemesDone > 0 ? Visibility.Visible : Visibility.Collapsed;
            archiveBtn.Content = "Archiver les terminés (" + vm.CountThemesDone + ")";

            if (vm.CountOverdue > 0) { badge.Background = Theme.Res<Brush>("Danger"); badgeText.Text = vm.CountOverdue.ToString(); badge.ToolTip = vm.CountOverdue + " tâche" + (vm.CountOverdue > 1 ? "s" : "") + " en retard (échéance dépassée)"; badge.Visibility = Visibility.Visible; }
            else if (vm.CountToday > 0) { badge.Background = Theme.Res<Brush>("Warn"); badgeText.Text = vm.CountToday.ToString(); badge.ToolTip = vm.CountToday + " tâche" + (vm.CountToday > 1 ? "s" : "") + " à échéance aujourd'hui"; badge.Visibility = Visibility.Visible; }
            else badge.Visibility = Visibility.Collapsed;
            UpdateEmpty();
        }

        void UpdateEmpty()
        {
            bool empty = vm.Cats.Count == 0;
            emptyText.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            if (!empty) return;
            emptyText.Text = vm.FilterActive ? "Aucune tâche pour ce filtre." : "Aucune catégorie.\nCréez-en une avec « + Catégorie », puis ajoutez vos tâches et leurs sous-tâches.";
        }

        // ---- menus ---------------------------------------------------------------------------------------

        MenuItem ColorMenu(string header, int current, string custom, Action<int, string> pick)
        {
            var m = new MenuItem { Header = header, Icon = Theme.Icon("IcoPalette", 14) };
            foreach (var i in TaskColors.Indices)
            {
                int v = i;
                var mi = Check(Palette.Names[i], current == i, delegate { pick(v, null); });
                mi.Icon = new Border { Width = 11, Height = 11, CornerRadius = new CornerRadius(6), Background = TaskColors.Solid(i) };
                m.Items.Add(mi);
            }
            m.Items.Add(new Separator());
            var cu = Check(current < 0 ? "Personnalisée (" + custom + ")…" : "Personnalisée…", current < 0, delegate
            {
                string hex = PickCustomColor(this, TaskColors.Of(current, custom));
                if (hex != null) pick(-1, hex);
            });
            cu.Icon = new Border { Width = 11, Height = 11, CornerRadius = new CornerRadius(6), Background = current < 0 ? TaskColors.Solid(-1, custom) : Theme.Argb(0x55FFFFFF) };
            m.Items.Add(cu);
            return m;
        }

        /// <summary>Palette Windows ; renvoie #RRGGBB ou null si annulé.</summary>
        public static string PickCustomColor(Window owner, Color initial)
        {
            using (var dlg = new System.Windows.Forms.ColorDialog { FullOpen = true, AnyColor = true, Color = System.Drawing.Color.FromArgb(initial.R, initial.G, initial.B) })
            {
                var h = owner != null ? new System.Windows.Interop.WindowInteropHelper(owner).Handle : IntPtr.Zero;
                var res = h != IntPtr.Zero ? dlg.ShowDialog(new Dialogs.Win32Owner(h)) : dlg.ShowDialog();
                if (res != System.Windows.Forms.DialogResult.OK) return null;
                return Palette.ToHex(Color.FromRgb(dlg.Color.R, dlg.Color.G, dlg.Color.B));
            }
        }

        void ShowCategoryMenu(TaskVm cat)
        {
            var menu = new ContextMenu();
            menu.Items.Add(Item("Nouvelle tâche", "IcoPlus", delegate { BeginAddTheme(cat); }));
            menu.Items.Add(Item("Renommer", "IcoPencil", delegate { BeginEditRow(cat); }));
            menu.Items.Add(ColorMenu("Couleur", cat.Color, cat.CustomColor, delegate(int i, string hex) { vm.SetCategoryColor(cat, i, hex); }));
            menu.Items.Add(Check(cat.IsOpen ? "Replier" : "Déplier", false, delegate { cat.IsOpen = !cat.IsOpen; }));
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Supprimer la catégorie…", "IcoTrash", delegate
            {
                int n = cat.Children.Count;
                if (Dialogs.Confirm(this, "Supprimer la catégorie", "« " + cat.Title + " »" + (n > 0 ? " et ses " + n + " tâche(s) avec leurs sous-tâches" : "") + " seront supprimées.", "Supprimer", true)) vm.Delete(cat);
            }));
            Open(menu, null);
        }

        MenuItem StatusItems(TaskVm t, MenuItem into)
        {
            for (int s = 0; s < 3; s++)
            {
                int v = s;
                var mi = Check(Stages.Labels[s], t.Stage == s, delegate { t.SetStage(v); });
                mi.Icon = new Border { Width = 9, Height = 9, CornerRadius = new CornerRadius(5), Background = Theme.Brush(0xFF, Stages.Dot(s)) };
                into.Items.Add(mi);
            }
            return into;
        }

        MenuItem ServiceItems(TaskVm t, MenuItem into)
        {
            into.Items.Add(Check("Aucun", !t.HasService, delegate { t.SetService(null); }));
            foreach (var s in vm.Services)
            {
                var svc = s;
                var mi = Check(svc.Name, t.ServiceId == svc.Id, delegate { t.SetService(svc.Id); });
                mi.Icon = new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(5), Background = TaskColors.Solid(svc.Color, svc.CustomColor) };
                into.Items.Add(mi);
            }
            into.Items.Add(new Separator());
            into.Items.Add(Item("Gérer les services…", "IcoTag", OpenManage));
            return into;
        }

        MenuItem DueItems(TaskVm t, MenuItem into)
        {
            var today = DateTime.Today;
            into.Items.Add(Item("Aujourd'hui", null, delegate { t.Due = today; }));
            into.Items.Add(Item("Demain", null, delegate { t.Due = today.AddDays(1); }));
            int toMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7; if (toMonday == 0) toMonday = 7;
            into.Items.Add(Item("Lundi prochain", null, delegate { t.Due = today.AddDays(toMonday); }));
            into.Items.Add(Item("Dans une semaine", null, delegate { t.Due = today.AddDays(7); }));
            into.Items.Add(Item("Date…", null, delegate
            {
                var s = Dialogs.Prompt(this, "Échéance", "Date (jj/mm/aaaa, demain, lundi, +3…)", t.Due.HasValue ? t.Due.Value.ToString("dd/MM/yyyy") : "", false);
                DateTime d;
                if (!string.IsNullOrEmpty(s) && QuickParse.TryDate(s, out d)) t.Due = d;
            }));
            into.Items.Add(new Separator());
            var none = Item("Aucune échéance", null, delegate { t.Due = null; });
            none.IsEnabled = t.DueVisible;
            into.Items.Add(none);
            return into;
        }

        void ShowThemeMenu(TaskVm t)
        {
            var menu = new ContextMenu();
            menu.Items.Add(Item("Ouvrir dans une fenêtre  (double-clic)", "IcoExternal", delegate { OpenDetail(t); }));
            menu.Items.Add(new Separator());
            menu.Items.Add(StatusItems(t, new MenuItem { Header = "Statut", Icon = Theme.Icon("IcoCheck", 14) }));
            menu.Items.Add(ServiceItems(t, new MenuItem { Header = "Service", Icon = Theme.Icon("IcoTag", 14) }));
            menu.Items.Add(Check("Priorité importante", t.Flag, delegate { t.Flag = !t.Flag; }));
            menu.Items.Add(DueItems(t, new MenuItem { Header = "Échéance", Icon = Theme.Icon("IcoCalendar", 14) }));
            menu.Items.Add(Item("Commentaires et historique…", "IcoComment", delegate { ShowComments(t, null); }));
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Ajouter une sous-tâche", "IcoPlus", delegate { FocusNewTask(t); }));
            menu.Items.Add(Item("Renommer", "IcoPencil", delegate { BeginEditRow(t); }));
            var move = new MenuItem { Header = "Déplacer vers la catégorie", Icon = Theme.Icon("IcoList", 14) };
            foreach (var cat in vm.Categories())
            {
                var target = cat;
                move.Items.Add(Check(cat.Title, t.Parent == cat, delegate { vm.MoveThemeTo(t, target); }));
            }
            menu.Items.Add(move);
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Supprimer la tâche…", "IcoTrash", delegate
            {
                int n = t.Children.Count;
                if (n == 0 || Dialogs.Confirm(this, "Supprimer la tâche", "« " + t.Title + " » et ses " + n + " sous-tâche(s) seront supprimées.", "Supprimer", true)) vm.Delete(t);
            }));
            Open(menu, null);
        }

        void ShowTaskMenu(TaskVm t)
        {
            var menu = new ContextMenu();
            menu.Items.Add(Item("Ouvrir la tâche dans une fenêtre  (double-clic)", "IcoExternal", delegate { OpenDetail(t); }));
            menu.Items.Add(Item("Renommer", "IcoPencil", delegate { BeginEditRow(t); }));
            menu.Items.Add(Check("Réalisée", t.IsDone, delegate { t.IsDone = !t.IsDone; }));
            menu.Items.Add(new Separator());
            menu.Items.Add(Item("Supprimer", "IcoTrash", delegate { vm.Delete(t); }));
            Open(menu, null);
        }

        void ShowSingle(TaskVm t, string which, UIElement anchor)
        {
            var menu = new ContextMenu();
            var holder = new MenuItem();
            if (which == "svc") ServiceItems(t, holder); else if (which == "status") StatusItems(t, holder); else DueItems(t, holder);
            var items = new List<object>();
            foreach (var it in holder.Items) items.Add(it);
            holder.Items.Clear();
            foreach (var it in items) menu.Items.Add(it);
            Open(menu, anchor);
        }

        // ---- commentaires -----------------------------------------------------------------------------

        void ShowComments(TaskVm t, UIElement anchor)
        {
            if (commentsPopup != null && commentsPopup.IsOpen) commentsPopup.IsOpen = false;
            TextBox box; ScrollViewer scroll;
            var root = CommentsBlock(t, false, delegate { commentsPopup.IsOpen = false; }, out box, out scroll);
            root.Width = 300;
            var head = new TextBlock { Text = "Commentaires · " + t.Title, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Theme.Res<Brush>("TextMuted"), TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 0, 0, 8) };
            DockPanel.SetDock(head, Dock.Top);
            root.Children.Insert(0, head);

            var border = new Border
            {
                Background = Theme.Res<Brush>("MenuBg"),
                BorderBrush = Theme.Res<Brush>("MenuBorder"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 12),
                Margin = new Thickness(8),
                Child = root,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 14, ShadowDepth = 2, Opacity = 0.45 }
            };
            commentsPopup = new Popup { PlacementTarget = anchor ?? (UIElement)this, Placement = anchor != null ? PlacementMode.Bottom : PlacementMode.MousePoint, StaysOpen = false, AllowsTransparency = true, PopupAnimation = PopupAnimation.Fade, Child = border, HorizontalOffset = anchor != null ? -220 : 0 };
            Activate();
            commentsPopup.IsOpen = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate { scroll.ScrollToEnd(); box.Focus(); Keyboard.Focus(box); }));
        }

        /// <summary>
        /// Historique d'une tâche : commentaires modifiables en place, événements journalisés (lecture seule), champ d'ajout.
        /// fill : occupe toute la hauteur disponible (fiche) ; sinon hauteur bornée (bulle du panneau).
        /// </summary>
        DockPanel CommentsBlock(TaskVm t, bool fill, Action escape, out TextBox boxOut, out ScrollViewer scrollOut)
        {
            var root = new DockPanel { LastChildFill = true };
            var input = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
            var send = new Button { Style = Theme.Res<Style>("ChipColored"), Background = Theme.Res<Brush>("Accent"), Foreground = Brushes.White, FontWeight = FontWeights.SemiBold, Content = "Envoyer", Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom, Padding = new Thickness(2) };
            DockPanel.SetDock(send, Dock.Right);
            var box = new TextBox { Style = Theme.Res<Style>("FieldTextBox"), Tag = "Ajouter un commentaire…  (Entrée pour envoyer)", AcceptsReturn = false, TextWrapping = TextWrapping.Wrap, MaxHeight = 80, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Action sendNow = delegate
            {
                string text = box.Text.Trim();
                if (text.Length == 0) return;
                t.AddComment(text, false);
                box.Clear();
                box.Focus();
            };
            send.Click += delegate { sendNow(); };
            box.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0) { sendNow(); e.Handled = true; }
                else if (e.Key == Key.Escape) { escape(); e.Handled = true; }
            };
            input.Children.Add(send);
            input.Children.Add(box);
            DockPanel.SetDock(input, Dock.Bottom);
            root.Children.Add(input);

            var list = new ItemsControl { ItemsSource = t.Entries, ItemTemplate = (DataTemplate)XamlReader.Parse(EntryXaml), Focusable = false };
            // modification en place d'un commentaire : clic sur le texte ou crayon ; corbeille pour supprimer
            Action<CommentVm> beginEdit = delegate(CommentVm cm)
            {
                cm.BeginEdit();
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate
                {
                    var container = list.ItemContainerGenerator.ContainerFromItem(cm) as FrameworkElement;
                    var editor = Ui.FindDescendant<TextBox>(container, x => x.Uid == "ceditor");
                    if (editor != null) { editor.Focus(); Keyboard.Focus(editor); editor.CaretIndex = editor.Text.Length; }
                }));
            };
            list.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(delegate(object s, RoutedEventArgs e)
            {
                var b = e.OriginalSource as ButtonBase;
                if (b == null) return;
                var cm = b.DataContext as CommentVm;
                string tag = b.Tag as string;
                if (cm == null || tag == null) return;
                if (tag == "cedit") beginEdit(cm);
                else if (tag == "cdel") t.RemoveComment(cm);
                e.Handled = true;
            }));
            list.PreviewMouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                var fe = Ui.HitTag(e.OriginalSource, "ctext", "row");
                var cm = fe != null ? fe.DataContext as CommentVm : null;
                if (cm == null || !cm.CanEdit) return;
                beginEdit(cm);
                e.Handled = true;
            };
            list.PreviewKeyDown += delegate(object s, KeyEventArgs e)
            {
                var tb = Keyboard.FocusedElement as TextBox;
                if (tb == null || tb.Uid != "ceditor") return;
                var cm = tb.DataContext as CommentVm;
                if (cm == null) return;
                if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0) { cm.EndEdit(true); box.Focus(); e.Handled = true; }
                else if (e.Key == Key.Escape) { cm.EndEdit(false); box.Focus(); e.Handled = true; }
            };
            list.AddHandler(UIElement.LostFocusEvent, new RoutedEventHandler(delegate(object s, RoutedEventArgs e)
            {
                var tb = e.OriginalSource as TextBox;
                if (tb == null || tb.Uid != "ceditor") return;
                var cm = tb.DataContext as CommentVm;
                if (cm != null && cm.IsEditing) cm.EndEdit(true);
            }));
            var scroll = new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Focusable = false };
            scroll.MaxHeight = fill ? Math.Max(220, SystemParameters.WorkArea.Height - 430) : 260;
            var empty = new TextBlock { Text = "Aucun commentaire pour l'instant. Les changements de statut sont journalisés ici.", FontSize = 11.5, Foreground = Theme.Res<Brush>("TextFaint"), TextWrapping = TextWrapping.Wrap, Visibility = t.Entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed };
            var grid = new Grid();
            grid.Children.Add(scroll);
            grid.Children.Add(empty);
            root.Children.Add(grid);
            NotifyCollectionChangedEventHandler changed = delegate { empty.Visibility = t.Entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed; scroll.ScrollToEnd(); };
            t.Entries.CollectionChanged += changed;
            root.Unloaded += delegate { t.Entries.CollectionChanged -= changed; };
            boxOut = box; scrollOut = scroll;
            return root;
        }

        // ---- fiche d'une tâche (fenêtre flottante) ------------------------------------------------------

        const string SubXaml = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <DockPanel MinHeight='26' Margin='0,0,0,1'>
    <ToggleButton DockPanel.Dock='Left' Style='{DynamicResource CheckCircle}' Width='20' Height='20' Background='{DynamicResource Ok}' IsChecked='{Binding IsDone, Mode=TwoWay}' VerticalAlignment='Center' Margin='0,0,8,0' ToolTip='Sous-tâche réalisée / à faire'/>
    <Button DockPanel.Dock='Right' Tag='del' Style='{DynamicResource IconBtnSmall}' Width='18' Height='18' VerticalAlignment='Center' ToolTip='Supprimer la sous-tâche'>
      <Viewbox Width='10' Height='10'><Path Style='{DynamicResource Ico}' Data='{DynamicResource IcoClose}' StrokeThickness='2.4'/></Viewbox>
    </Button>
    <TextBlock DockPanel.Dock='Right' Text='{Binding DoneAtText}' FontSize='10.5' Foreground='{DynamicResource TextFaint}' VerticalAlignment='Center' Margin='6,0,4,0'/>
    <TextBox Style='{DynamicResource GhostTextBox}' Text='{Binding Title, UpdateSourceTrigger=PropertyChanged}' FontSize='13' Padding='0' VerticalAlignment='Center' TextDecorations='{Binding Strike}' Opacity='{Binding RowOpacity}'/>
  </DockPanel>
</DataTemplate>";

        /// <summary>Double-clic sur une tâche ou l'une de ses sous-tâches : la fiche complète dans une fenêtre flottante.</summary>
        public void OpenDetail(TaskVm t)
        {
            if (t == null || t.IsCategory) return;
            if (t.IsTask) t = t.Parent;
            if (t == null || !t.IsTheme) return;
            var theme = t;
            DetailWindow.Show(this, "task", "task:" + theme.Id, theme.Title.Length > 0 ? theme.Title : "Tâche", delegate(DetailWindow w) { return BuildTaskDetail(theme, w); }, 600, 150);
        }

        static Button Chip(TaskVm t, string bg, string fg)
        {
            var b = new Button { Style = Theme.Res<Style>("ChipColored"), Margin = new Thickness(0, 0, 4, 2), DataContext = t };
            b.SetBinding(Button.BackgroundProperty, new Binding(bg));
            b.SetBinding(Button.ForegroundProperty, new Binding(fg));
            return b;
        }

        /// <summary>Contenu de la fiche : catégorie, titre, badges (mêmes menus que le panneau), sous-tâches, historique complet.</summary>
        FrameworkElement BuildTaskDetail(TaskVm t, DetailWindow w)
        {
            var top = new StackPanel();

            var crumb = new TextBlock { FontSize = 11.5, Margin = new Thickness(0, 0, 0, 4), TextTrimming = TextTrimming.CharacterEllipsis };
            Action crumbRefresh = delegate
            {
                crumb.Inlines.Clear();
                if (t.Parent != null) crumb.Inlines.Add(new Run(t.Parent.Title) { Foreground = t.Parent.NameBrush, FontWeight = FontWeights.SemiBold });
                DateTime created;
                if (DateTime.TryParse(t.Created, out created)) crumb.Inlines.Add(new Run((t.Parent != null ? "  ·  " : "") + "créée le " + created.ToString("dd/MM/yyyy")) { Foreground = Theme.Res<Brush>("TextFaint") });
            };
            crumbRefresh();
            top.Children.Add(crumb);

            var title = new TextBox { Style = Theme.Res<Style>("GhostTextBox"), FontSize = 17, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, AcceptsReturn = false, Padding = new Thickness(0, 2, 0, 2), Margin = new Thickness(0, 0, 0, 8), DataContext = t, Tag = "Titre de la tâche" };
            title.SetBinding(TextBox.TextProperty, new Binding("Title") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            title.KeyDown += delegate(object s, KeyEventArgs e) { if (e.Key == Key.Enter) { title.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)); e.Handled = true; } };
            top.Children.Add(title);

            // badges : statut, service, échéance, priorité — mêmes menus que dans le panneau
            var chips = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
            var status = Chip(t, "StBg", "StFg");
            var stRow = new StackPanel { Orientation = Orientation.Horizontal };
            var dot = new Border { Width = 7, Height = 7, CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center, DataContext = t };
            dot.SetBinding(Border.BackgroundProperty, new Binding("StDot"));
            var stText = new TextBlock { DataContext = t };
            stText.SetBinding(TextBlock.TextProperty, new Binding("StLabel"));
            stRow.Children.Add(dot); stRow.Children.Add(stText);
            status.Content = stRow; status.ToolTip = "Statut (cliquer pour changer)";
            status.Click += delegate { ShowSingle(t, "status", status); };
            chips.Children.Add(status);

            var svc = Chip(t, "SvcBg", "SvcFg");
            svc.SetBinding(Button.ContentProperty, new Binding("SvcName"));
            svc.FontWeight = FontWeights.SemiBold; svc.ToolTip = "Service rattaché (cliquer pour changer)";
            svc.Click += delegate { ShowSingle(t, "svc", svc); };
            chips.Children.Add(svc);

            var due = new Button { Style = Theme.Res<Style>("ChipColored"), Margin = new Thickness(0, 0, 4, 2), ToolTip = "Échéance (cliquer pour changer)" };
            var dueText = new TextBlock();
            due.Content = dueText;
            Action dueRefresh = delegate
            {
                bool has = t.DueVisible;
                dueText.Text = has ? "Échéance : " + t.DueText : "Pas d'échéance";
                due.Background = has ? t.DueBg : Theme.Argb(0x1EFFFFFF);
                due.Foreground = has ? t.DueFg : Theme.Res<Brush>("TextMuted");
            };
            dueRefresh();
            due.Click += delegate { ShowSingle(t, "due", due); };
            chips.Children.Add(due);

            var flag = new Button { Style = Theme.Res<Style>("ChipBtn"), Margin = new Thickness(0, 0, 4, 2) };
            var flagText = new TextBlock { Text = "⚑ Important", FontWeight = FontWeights.SemiBold, DataContext = t };
            flagText.SetBinding(TextBlock.ForegroundProperty, new Binding("FlagBrush"));
            flag.SetBinding(Button.ToolTipProperty, new Binding("FlagTip") { Source = t });
            flag.Content = flagText;
            flag.Click += delegate { t.Flag = !t.Flag; };
            chips.Children.Add(flag);
            top.Children.Add(chips);

            // sous-tâches : actives puis réalisées, ajout en bas
            var subs = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            var subHead = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
            var progress = new TextBlock { DataContext = t, FontSize = 11, Foreground = Theme.Res<Brush>("TextFaint"), VerticalAlignment = VerticalAlignment.Center };
            progress.SetBinding(TextBlock.TextProperty, new Binding("ProgressText"));
            DockPanel.SetDock(progress, Dock.Right);
            subHead.Children.Add(progress);
            subHead.Children.Add(new TextBlock { Text = "Sous-tâches", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Theme.Res<Brush>("TextMuted") });
            subs.Children.Add(subHead);
            var subTpl = (DataTemplate)XamlReader.Parse(SubXaml);
            var active = new ItemsControl { ItemsSource = t.ActiveTasks, ItemTemplate = subTpl, Focusable = false };
            var done = new ItemsControl { ItemsSource = t.DoneTasks, ItemTemplate = subTpl, Focusable = false, Margin = new Thickness(0, 2, 0, 0) };
            RoutedEventHandler subClick = delegate(object s, RoutedEventArgs e)
            {
                var b = e.OriginalSource as ButtonBase;
                var sub = b != null ? b.DataContext as TaskVm : null;
                if (sub == null || (b.Tag as string) != "del") return;
                vm.Delete(sub);
                e.Handled = true;
            };
            active.AddHandler(ButtonBase.ClickEvent, subClick);
            done.AddHandler(ButtonBase.ClickEvent, subClick);
            subs.Children.Add(active);
            subs.Children.Add(done);
            var add = new TextBox { Style = Theme.Res<Style>("FieldTextBox"), Tag = "Ajouter une sous-tâche…  (Entrée)", Margin = new Thickness(0, 4, 0, 0) };
            add.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key != Key.Enter) return;
                string text = add.Text.Trim();
                if (text.Length > 0) { vm.AddTask(t, text); add.Clear(); }
                e.Handled = true;
            };
            subs.Children.Add(add);

            // historique complet, sur toute la hauteur restante
            TextBox box; ScrollViewer scroll;
            var comments = CommentsBlock(t, true, delegate { w.Close(); }, out box, out scroll);
            var cHead = new TextBlock { Text = "Commentaires et historique", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Theme.Res<Brush>("TextMuted"), Margin = new Thickness(0, 0, 0, 4) };
            DockPanel.SetDock(cHead, Dock.Top);
            comments.Children.Insert(0, cHead);

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto, MaxHeight = 240 });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 40 });
            Grid.SetRow(top, 0); grid.Children.Add(top);
            var subScroll = new ScrollViewer { Content = subs, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Focusable = false };
            Grid.SetRow(subScroll, 1); grid.Children.Add(subScroll);
            Grid.SetRow(comments, 2); grid.Children.Add(comments);

            PropertyChangedEventHandler onProp = delegate(object s, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "Due" || e.PropertyName == "DueText" || e.PropertyName == "DueVisible") dueRefresh();
                else if (e.PropertyName == "Title") w.SetTitle(t.Title.Length > 0 ? t.Title : "Tâche");
            };
            t.PropertyChanged += onProp;
            Action onCounts = delegate { if (!vm.All.Contains(t)) { w.Close(); return; } crumbRefresh(); };
            vm.CountsChanged += onCounts;
            w.Closed += delegate { t.PropertyChanged -= onProp; vm.CountsChanged -= onCounts; };
            w.Loaded += delegate { w.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate { scroll.ScrollToEnd(); })); };
            return grid;
        }

        // ---- ajout en ligne, édition --------------------------------------------------------------------

        void BeginAddTheme(TaskVm cat)
        {
            if (cat == null) return;
            if (IsRolled) SetRolled(false, true);
            cat.IsOpen = true;
            cat.Adding = true;
            Activate();
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate
            {
                var container = cats.ItemContainerGenerator.ContainerFromItem(cat) as FrameworkElement;
                var tb = Ui.FindDescendant<TextBox>(container, x => x.Uid == "newtheme");
                if (tb != null) { tb.Focus(); Keyboard.Focus(tb); }
            }));
        }

        void FocusNewTask(TaskVm theme)
        {
            theme.IsOpen = true;
            Activate();
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate
            {
                var container = ContainerOf(theme);
                var tb = Ui.FindDescendant<TextBox>(container, x => x.Uid == "newtask");
                if (tb != null) { tb.Focus(); Keyboard.Focus(tb); }
            }));
        }

        FrameworkElement ContainerOf(TaskVm t)
        {
            if (t.IsCategory) return cats.ItemContainerGenerator.ContainerFromItem(t) as FrameworkElement;
            var parentC = ContainerOf(t.Parent);
            if (parentC == null) return null;
            var host = Ui.FindDescendant<ItemsControl>(parentC, ic => ic.Items.Contains(t));
            return host == null ? null : host.ItemContainerGenerator.ContainerFromItem(t) as FrameworkElement;
        }

        void BeginEditRow(TaskVm t)
        {
            t.BeginEdit();
            Activate();
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate
            {
                var container = ContainerOf(t);
                var editor = Ui.FindDescendant<TextBox>(container, tb => tb.Uid == "editor" && tb.DataContext == t);
                if (editor != null) { editor.Focus(); Keyboard.Focus(editor); editor.CaretIndex = editor.Text.Length; }
            }));
        }

        // ---- souris et clavier -------------------------------------------------------------------------

        void ListButtonClick(object sender, RoutedEventArgs e)
        {
            var b = e.OriginalSource as ButtonBase;
            if (b == null) return;
            var t = b.DataContext as TaskVm;
            string tag = b.Tag as string;
            if (t == null || tag == null) return;
            switch (tag)
            {
                case "toggle": t.IsOpen = !t.IsOpen; break;
                case "addtheme": BeginAddTheme(t); break;
                case "more": if (t.IsCategory) ShowCategoryMenu(t); else if (t.IsTheme) ShowThemeMenu(t); else ShowTaskMenu(t); break;
                case "flag": t.Flag = !t.Flag; break;
                case "cm": ShowComments(t, b); break;
                case "svc": ShowSingle(t, "svc", b); break;
                case "status": ShowSingle(t, "status", b); break;
                case "due": ShowSingle(t, "due", b); break;
                case "del": vm.Delete(t); break;
                case "donetoggle": case "donethemes": t.DoneOpen = !t.DoneOpen; break;
                default: return;
            }
            e.Handled = true;
        }

        void ListRightUp(object sender, MouseButtonEventArgs e)
        {
            var row = Ui.HitTag(e.OriginalSource, "row", "row");
            var t = row != null ? row.DataContext as TaskVm : null;
            if (t == null) { var head = Ui.HitTag(e.OriginalSource, "cathead", "row"); t = head != null ? head.DataContext as TaskVm : null; }
            if (t == null) return;
            if (t.IsCategory) ShowCategoryMenu(t); else if (t.IsTheme) ShowThemeMenu(t); else ShowTaskMenu(t);
            e.Handled = true;
        }

        void ListDown(object sender, MouseButtonEventArgs e)
        {
            var grip = Ui.HitTag(e.OriginalSource, "grip", "row");
            if (grip == null && e.ClickCount == 2 && Ui.FindAncestor<ButtonBase>(e.OriginalSource as DependencyObject) == null)
            {
                // double-clic sur une tâche ou une sous-tâche (hors boutons) : la fiche complète dans une fenêtre
                var row = Ui.HitTag(e.OriginalSource, "row", "row");
                var dt = row != null ? row.DataContext as TaskVm : null;
                if (dt != null && !dt.IsCategory)
                {
                    if (dt.IsEditing) dt.EndEdit(true);
                    OpenDetail(dt);
                    e.Handled = true;
                    return;
                }
            }
            if (grip != null)
            {
                dragVm = grip.DataContext as TaskVm;
                if (dragVm == null) return;
                dragHost = Ui.FindAncestor<ItemsControl>(grip);
                dragRow = Ui.HitTag(grip, "row", "row") ?? Ui.HitTag(grip, "cat", "row");
                grabInRow = dragRow != null ? e.GetPosition(dragRow) : new Point();
                dragStart = e.GetPosition(cats);
                dragging = false;
                Activate();             // avant la capture : l'activation d'une fenêtre inactive relâcherait la capture
                cats.CaptureMouse();
                e.Handled = true;
                return;
            }
            var title = Ui.HitTag(e.OriginalSource, "title", "row");
            if (title != null)
            {
                var t = title.DataContext as TaskVm;
                if (t == null) return;
                if (t.IsCategory) { if (e.ClickCount == 2) { BeginEditRow(t); e.Handled = true; } return; }
                if (t.IsDone) return;
                BeginEditRow(t);
                e.Handled = true;
            }
        }

        void ListUp(object sender, MouseButtonEventArgs e)
        {
            if (dragVm != null) { bool did = dragging; EndDrag(); if (did) { e.Handled = true; return; } return; }
            // clic simple sur l'en-tête d'une catégorie (hors boutons et champs) : replier / déplier
            if (Ui.FindAncestor<ButtonBase>(e.OriginalSource as DependencyObject) != null || Ui.FindAncestor<TextBox>(e.OriginalSource as DependencyObject) != null) return;
            var head = Ui.HitTag(e.OriginalSource, "cathead", "row");
            if (head == null || e.ClickCount != 1) return;
            var cat = head.DataContext as TaskVm;
            if (cat != null && !cat.IsEditing) cat.IsOpen = !cat.IsOpen;
        }

        /// <summary>Glisser en direct : l'élément change de place dès que le curseur dépasse le milieu du voisin.</summary>
        void ListMove(object sender, MouseEventArgs e)
        {
            if (dragVm == null || dragHost == null || e.LeftButton != MouseButtonState.Pressed) return;
            var p0 = e.GetPosition(cats);
            if (!dragging && Math.Abs(p0.Y - dragStart.Y) < 4) return;
            if (!dragging) { dragging = true; ghost.Start(this, dragRow, grabInRow); dragVm.IsDragging = true; }
            ghost.Move(DragGhost.ScreenDip(this, e));
            var p = e.GetPosition(dragHost);
            int cur = dragHost.Items.IndexOf(dragVm);
            if (cur < 0) return;
            if (cur > 0)
            {
                var above = dragHost.ItemContainerGenerator.ContainerFromIndex(cur - 1) as FrameworkElement;
                if (above != null)
                {
                    double mid = above.TransformToAncestor(dragHost).Transform(new Point(0, 0)).Y + above.ActualHeight / 2;
                    if (p.Y < mid) { vm.MoveWithin(dragVm, cur - 1); return; }
                }
            }
            if (cur < dragHost.Items.Count - 1)
            {
                var below = dragHost.ItemContainerGenerator.ContainerFromIndex(cur + 1) as FrameworkElement;
                if (below != null)
                {
                    double mid = below.TransformToAncestor(dragHost).Transform(new Point(0, 0)).Y + below.ActualHeight / 2;
                    if (p.Y > mid) vm.MoveWithin(dragVm, cur + 1);
                }
            }
        }

        void EndDrag()
        {
            ghost.End();
            if (dragVm != null) dragVm.IsDragging = false;
            dragVm = null; dragHost = null; dragRow = null; dragging = false;
            if (cats.IsMouseCaptured) cats.ReleaseMouseCapture();
        }

        void ListKey(object sender, KeyEventArgs e)
        {
            var tb = Keyboard.FocusedElement as TextBox;
            if (tb == null) return;
            var t = tb.DataContext as TaskVm;
            if (t == null) return;
            if (tb.Uid == "editor")
            {
                if (e.Key == Key.Enter) { t.EndEdit(true); Focus(); e.Handled = true; }
                else if (e.Key == Key.Escape) { t.EndEdit(false); Focus(); e.Handled = true; }
            }
            else if (tb.Uid == "newtask")
            {
                if (e.Key == Key.Enter) { string s = tb.Text.Trim(); if (s.Length > 0) { vm.AddTask(t, s); tb.Clear(); } e.Handled = true; }
                else if (e.Key == Key.Escape) { tb.Clear(); Focus(); e.Handled = true; }
            }
            else if (tb.Uid == "newtheme")
            {
                if (e.Key == Key.Enter)
                {
                    string s = tb.Text.Trim();
                    tb.Clear();
                    t.Adding = false;
                    if (s.Length > 0) vm.AddTheme(t, s, vm.ServiceFilter.Count == 1 ? vm.ServiceFilter.First() : null);
                    Focus();
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape) { tb.Clear(); t.Adding = false; Focus(); e.Handled = true; }
            }
        }

        void ListLostFocus(object sender, RoutedEventArgs e)
        {
            var tb = e.OriginalSource as TextBox;
            if (tb == null) return;
            var t = tb.DataContext as TaskVm;
            if (t == null) return;
            if (tb.Uid == "editor" && t.IsEditing) t.EndEdit(true);
            else if (tb.Uid == "newtheme" && t.Adding && tb.Text.Trim().Length == 0) t.Adding = false;
        }

        // ---- panneau --------------------------------------------------------------------------------------

        protected override bool CanRename { get { return false; } }
        protected override string SettingsTooltip { get { return "Paramètres du panneau (couleur, opacité, verrou)"; } }
        protected override Size DefaultSize { get { return new Size(380, 560); } }
        protected override void OnOpenSettings() { c.EditPanel(this, c.L.Tasks, "Tâches"); }
        protected override bool OwnLocked { get { return c.L.Tasks.Locked; } }

        protected override void SaveRect()
        {
            var t = c.L.Tasks;
            t.X = Left; t.Y = Top; t.W = Width; t.H = RestoreHeight; t.Rolled = IsRolled; t.RolledH = RestoreHeight; t.Set = true;
            string sc; double rx, ry; CurrentScreen(out sc, out rx, out ry); t.Screen = sc; t.RelX = rx; t.RelY = ry;
            c.SaveLayoutSoon();
        }
    }
}
