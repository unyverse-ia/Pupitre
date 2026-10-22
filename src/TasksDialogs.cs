using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Pupitre
{
    /// <summary>Services (badges) et catégories du suivi : nom, couleur, suppression. Les changements s'appliquent immédiatement.</summary>
    public class TasksSettingsDialog : DarkDialog
    {
        readonly Controller c;
        readonly TasksVm vm;
        readonly StackPanel svcList = new StackPanel(), catList = new StackPanel();

        public TasksSettingsDialog(Window owner, Controller ctl, TasksVm model) : base(owner, "Services et catégories", 470)
        {
            c = ctl; vm = model;
            Section("Services");
            Note("Un service par tâche, affiché en badge coloré et utilisé comme filtre. Cliquez sur une pastille pour changer la couleur, modifiez le nom directement dans le champ.");
            Panel.Children.Add(svcList);
            Panel.Children.Add(AddRow("Nouveau service…", delegate(string name) { if (vm.AddService(name) != null) RefreshServices(); }));

            Section("Catégories");
            Note("Les catégories regroupent les tâches. Supprimer une catégorie supprime aussi ses tâches et leurs sous-tâches.");
            Panel.Children.Add(catList);
            Panel.Children.Add(AddRow("Nouvelle catégorie…", delegate(string name) { vm.AddCategory(name); RefreshCategories(); }));

            OkBtn.Content = "Fermer";
            HideCancel();
            RefreshServices();
            RefreshCategories();
        }

        void Section(string title)
        {
            Panel.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 13, Margin = new Thickness(0, 12, 0, 4) });
            Panel.Children.Add(new System.Windows.Shapes.Rectangle { Height = 1, Fill = Theme.Res<Brush>("SLine"), Margin = new Thickness(0, 0, 0, 2) });
        }

        DockPanel AddRow(string hint, Action<string> add)
        {
            var row = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
            var btn = new Button { Style = Theme.Res<Style>("SBtnAccent"), Content = "Ajouter", Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(12, 4, 12, 5) };
            DockPanel.SetDock(btn, Dock.Right);
            var box = new TextBox { Style = Theme.Res<Style>("SFieldBox"), Tag = hint };
            Action commit = delegate { string s = box.Text.Trim(); if (s.Length == 0) return; add(s); box.Clear(); box.Focus(); };
            btn.Click += delegate { commit(); };
            box.KeyDown += delegate(object s, KeyEventArgs e) { if (e.Key == Key.Enter) { commit(); e.Handled = true; } };
            row.Children.Add(btn);
            row.Children.Add(box);
            return row;
        }

        /// <summary>Ligne : pastilles de couleur + palette libre, nom modifiable, usage, suppression.</summary>
        Border Row(string name, int color, string custom, string usage, Action<int, string> setColor, Action<string> rename, Action remove)
        {
            var dock = new DockPanel();
            var swatches = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            foreach (var i in TaskColors.Indices)
            {
                int v = i;
                var dot = new Border { Width = 15, Height = 15, CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 3, 0), Cursor = Cursors.Hand, ToolTip = Palette.Names[i],
                    Background = TaskColors.Solid(i), BorderThickness = new Thickness(2), BorderBrush = i == color ? Brushes.White : Brushes.Transparent };
                dot.MouseLeftButtonDown += delegate { setColor(v, null); };
                swatches.Children.Add(dot);
            }
            // dernière pastille : couleur libre (palette Windows) ; affiche la couleur personnalisée quand elle est active
            bool isCustom = color < 0;
            var pick = new Border { Width = 15, Height = 15, CornerRadius = new CornerRadius(8), Margin = new Thickness(2, 0, 3, 0), Cursor = Cursors.Hand, ToolTip = isCustom ? "Couleur personnalisée " + custom + " — cliquer pour changer" : "Couleur personnalisée…",
                Background = isCustom ? TaskColors.Solid(-1, custom) : Theme.Argb(0x30FFFFFF), BorderThickness = new Thickness(2), BorderBrush = isCustom ? Brushes.White : Brushes.Transparent };
            pick.Child = new Viewbox { Width = 9, Height = 9, Child = new System.Windows.Shapes.Path { Style = Theme.Res<Style>("Ico"), Data = Theme.Res<Geometry>("IcoPalette"), Stroke = Brushes.White, StrokeThickness = 2.2 } };
            pick.MouseLeftButtonDown += delegate
            {
                string hex = TasksWindow.PickCustomColor(this, TaskColors.Of(color, custom));
                if (hex != null) setColor(-1, hex);
            };
            swatches.Children.Add(pick);
            DockPanel.SetDock(swatches, Dock.Left);
            var del = Theme.IconButton("IcoTrash", "Supprimer", 13);
            del.Click += delegate { remove(); };
            DockPanel.SetDock(del, Dock.Right);
            var use = new TextBlock { Text = usage, Foreground = Theme.Res<Brush>("SMuted"), FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 6, 0) };
            DockPanel.SetDock(use, Dock.Right);
            var box = new TextBox { Style = Theme.Res<Style>("SFieldBox"), Text = name, FontWeight = FontWeights.SemiBold, Padding = new Thickness(8, 3, 8, 4), MinWidth = 120 };
            box.LostFocus += delegate { rename(box.Text); };
            box.KeyDown += delegate(object s, KeyEventArgs e) { if (e.Key == Key.Enter) { rename(box.Text); Focus(); e.Handled = true; } };
            dock.Children.Add(swatches);
            dock.Children.Add(del);
            dock.Children.Add(use);
            dock.Children.Add(box);
            return new Border { Child = dock, Padding = new Thickness(8, 5, 4, 5), Margin = new Thickness(0, 5, 0, 0), CornerRadius = new CornerRadius(9), Background = Theme.Res<Brush>("SField"), BorderBrush = Theme.Res<Brush>("SLine"), BorderThickness = new Thickness(1) };
        }

        void RefreshServices()
        {
            svcList.Children.Clear();
            if (vm.Services.Count == 0) svcList.Children.Add(new TextBlock { Text = "Aucun service. Ajoutez-en un ci-dessous.", Foreground = Theme.Res<Brush>("SMuted"), FontSize = 12, Margin = new Thickness(0, 6, 0, 0) });
            foreach (var s in vm.Services.ToList())
            {
                var svc = s;
                int n = vm.ServiceUsage(svc);
                svcList.Children.Add(Row(svc.Name, svc.Color, svc.CustomColor, n + (n > 1 ? " tâches" : " tâche"),
                    delegate(int i, string hex) { vm.SetServiceColor(svc, i, hex); RefreshServices(); },
                    delegate(string name) { vm.RenameService(svc, name); },
                    delegate
                    {
                        if (Dialogs.Confirm(this, "Supprimer le service", "« " + svc.Name + " » sera retiré des tâches qui l'utilisent.", "Supprimer", true)) { vm.RemoveService(svc); RefreshServices(); }
                    }));
            }
        }

        void RefreshCategories()
        {
            catList.Children.Clear();
            var cats = vm.Categories();
            if (cats.Count == 0) catList.Children.Add(new TextBlock { Text = "Aucune catégorie.", Foreground = Theme.Res<Brush>("SMuted"), FontSize = 12, Margin = new Thickness(0, 6, 0, 0) });
            foreach (var cat0 in cats)
            {
                var cat = cat0;
                int n = cat.Children.Count;
                catList.Children.Add(Row(cat.Title, cat.Color, cat.CustomColor, n + (n > 1 ? " tâches" : " tâche"),
                    delegate(int i, string hex) { vm.SetCategoryColor(cat, i, hex); RefreshCategories(); },
                    delegate(string name) { if (!string.IsNullOrWhiteSpace(name)) cat.Title = name.Trim(); },
                    delegate
                    {
                        if (Dialogs.Confirm(this, "Supprimer la catégorie", "« " + cat.Title + " »" + (n > 0 ? " et ses " + n + " tâche(s) avec leurs sous-tâches" : "") + " seront supprimées.", "Supprimer", true)) { vm.Delete(cat); RefreshCategories(); }
                    }));
            }
        }
    }
}
