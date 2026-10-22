using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Pupitre
{
    /// <summary>
    /// Rendu « lecture » du texte d'une note : adresses web et chemins de fichiers cliquables, lignes « [ ] » / « [x] »
    /// affichées comme des cases à cocher (le clic modifie le texte). Un clic ailleurs demande le passage en édition.
    /// </summary>
    public class NoteTextView : StackPanel
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(NoteTextView),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));
        public static readonly RoutedEvent EditRequestedEvent = EventManager.RegisterRoutedEvent("EditRequested", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NoteTextView));

        // liens : http(s), www., file:, chemins Windows (entre guillemets pour les espaces) et chemins réseau
        static readonly Regex LinkRx = new Regex(@"(https?://[^\s<>""']+|www\.[^\s<>""']+|file:///?[^\s<>""']+|""[A-Za-z]:\\[^""]+""|""\\\\[^""]+""|[A-Za-z]:\\[^\s<>""|]+|\\\\[^\s<>""|]+)", RegexOptions.Compiled);
        static readonly Regex CheckRx = new Regex(@"^(\s*(?:[-*•]\s+)?)\[( |x|X)\]\s?(.*)$", RegexOptions.Compiled);
        static readonly Regex LineSplit = new Regex(@"(\r\n|\r|\n)", RegexOptions.Compiled);
        bool updating;

        public string Text { get { return (string)GetValue(TextProperty); } set { SetValue(TextProperty, value); } }
        public event RoutedEventHandler EditRequested { add { AddHandler(EditRequestedEvent, value); } remove { RemoveHandler(EditRequestedEvent, value); } }

        public NoteTextView()
        {
            Background = Brushes.Transparent;
            Cursor = Cursors.IBeam;
            MouseLeftButtonUp += OnClick;
            Render();
        }

        static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var v = (NoteTextView)d;
            if (!v.updating) v.Render();
        }

        void OnClick(object sender, MouseButtonEventArgs e)
        {
            // les cases et les liens gèrent leur propre clic ; ailleurs : passage en édition
            RaiseEvent(new RoutedEventArgs(EditRequestedEvent, this));
        }

        void Render()
        {
            Children.Clear();
            string text = Text ?? "";
            if (text.Trim().Length == 0)
            {
                Children.Add(new TextBlock { Text = "Écrire ici…   ([ ] pour une case à cocher, liens et chemins cliquables)", FontSize = 12, FontStyle = FontStyles.Italic, Foreground = Theme.Res<Brush>("TextFaint"), TextWrapping = TextWrapping.Wrap });
                return;
            }
            var parts = LineSplit.Split(text);
            for (int i = 0; i < parts.Length; i += 2)
            {
                string line = parts[i];
                int lineIndex = i;
                if (line.Trim().Length == 0) { Children.Add(new Border { Height = 7 }); continue; }
                var m = CheckRx.Match(line);
                if (m.Success)
                {
                    bool done = m.Groups[2].Value != " ";
                    var row = new DockPanel { Margin = new Thickness(0, 1, 0, 1) };
                    var cb = new ToggleButton { Style = Theme.Res<Style>("CheckCircle"), Width = 18, Height = 18, Background = Theme.Res<Brush>("Ok"), IsChecked = done, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 1, 6, 0), ToolTip = done ? "Fait — cliquer pour rouvrir" : "À faire — cliquer pour cocher" };
                    cb.Click += delegate { Toggle(lineIndex); };
                    DockPanel.SetDock(cb, Dock.Left);
                    var tb = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12.5, Foreground = done ? Theme.Res<Brush>("TextMuted") : Theme.Res<Brush>("Text"), VerticalAlignment = VerticalAlignment.Center };
                    if (done) tb.TextDecorations = TextDecorations.Strikethrough;
                    Linkify(tb, m.Groups[3].Value);
                    row.Children.Add(cb);
                    row.Children.Add(tb);
                    Children.Add(row);
                }
                else
                {
                    var tb = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12.5, Foreground = Theme.Res<Brush>("Text") };
                    Linkify(tb, line);
                    Children.Add(tb);
                }
            }
        }

        void Linkify(TextBlock tb, string s)
        {
            int pos = 0;
            foreach (Match m in LinkRx.Matches(s))
            {
                if (m.Index > pos) tb.Inlines.Add(new Run(s.Substring(pos, m.Index - pos)));
                string raw = m.Value;
                string target = raw.Trim('"');
                // ponctuation finale fréquente après un lien
                while (target.Length > 1 && ".,;:)".IndexOf(target[target.Length - 1]) >= 0) { target = target.Substring(0, target.Length - 1); }
                string shown = raw.Substring(0, raw.Length - (raw.Length - target.Length - (raw.Length - raw.Trim('"').Length)));
                var hl = new Hyperlink(new Run(target)) { Foreground = Theme.Res<Brush>("AccentHover"), TextDecorations = null, Cursor = Cursors.Hand, ToolTip = "Ouvrir " + target };
                hl.MouseEnter += delegate { hl.TextDecorations = TextDecorations.Underline; };
                hl.MouseLeave += delegate { hl.TextDecorations = null; };
                string t = target;
                hl.Click += delegate(object sender, RoutedEventArgs e) { Open(t); e.Handled = true; };
                tb.Inlines.Add(hl);
                string tail = raw.Trim('"').Substring(target.Length);
                if (tail.Length > 0) tb.Inlines.Add(new Run(tail));
                pos = m.Index + m.Length;
            }
            if (pos < s.Length) tb.Inlines.Add(new Run(s.Substring(pos)));
        }

        static void Open(string target)
        {
            try
            {
                string t = target;
                if (t.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) t = "http://" + t;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(t) { UseShellExecute = true });
            }
            catch (Exception ex) { Log.Error(ex, "ouvrir le lien " + target); }
        }

        /// <summary>Coche ou décoche la ligne : le texte est réécrit, la liaison remonte vers la note.</summary>
        void Toggle(int partIndex)
        {
            var parts = LineSplit.Split(Text ?? "");
            if (partIndex >= parts.Length) return;
            var m = CheckRx.Match(parts[partIndex]);
            if (!m.Success) return;
            bool done = m.Groups[2].Value != " ";
            parts[partIndex] = m.Groups[1].Value + (done ? "[ ] " : "[x] ") + m.Groups[3].Value;
            updating = true;
            try { SetCurrentValue(TextProperty, string.Join("", parts)); }
            finally { updating = false; }
            Render();
        }
    }
}
