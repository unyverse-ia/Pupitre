using System;
using System.Reflection;
using WF = System.Windows.Forms;
using SD = System.Drawing;

namespace Pupitre
{
    /// <summary>Icône de la zone de notification.</summary>
    internal class TrayIcon : IDisposable
    {
        readonly Controller c;
        readonly WF.NotifyIcon ni;
        readonly WF.ToolStripMenuItem miToggle, miLock, miTasks, miNotes, miMind, miProfiles;

        public TrayIcon(Controller ctl)
        {
            c = ctl;
            ni = new WF.NotifyIcon();
            ni.Text = App.Name;
            ni.Icon = LoadIcon();
            var menu = new WF.ContextMenuStrip();
            var miQuick = new WF.ToolStripMenuItem("Nouvelle tâche…", null, (s, e) => c.QuickAdd());
            miQuick.Font = new SD.Font(miQuick.Font, SD.FontStyle.Bold);
            menu.Items.Add(miQuick);
            menu.Items.Add(new WF.ToolStripMenuItem("Nouveau cadre…", null, (s, e) => c.NewCadre(null)));
            menu.Items.Add(new WF.ToolStripMenuItem("Nouvelle note", null, (s, e) => c.NewNote(null)));
            menu.Items.Add(new WF.ToolStripMenuItem("Nouvelle carte mentale…", null, (s, e) => c.NewMindMap(null)));
            menu.Items.Add(new WF.ToolStripSeparator());
            miToggle = new WF.ToolStripMenuItem("Masquer les cadres", null, (s, e) => c.ToggleAll());
            menu.Items.Add(miToggle);
            miTasks = new WF.ToolStripMenuItem("Panneau Tâches", null, (s, e) => c.SetTasksVisible(!c.S.TasksVisible));
            menu.Items.Add(miTasks);
            miNotes = new WF.ToolStripMenuItem("Panneau Notes", null, (s, e) => c.SetNotesVisible(!c.S.NotesVisible));
            menu.Items.Add(miNotes);
            miMind = new WF.ToolStripMenuItem("Panneau Cartes mentales", null, (s, e) => c.SetMindVisible(!c.S.MindVisible));
            menu.Items.Add(miMind);
            miLock = new WF.ToolStripMenuItem("Verrouiller tous les panneaux", null, (s, e) => c.SetLocked(!c.S.LockLayout));
            menu.Items.Add(miLock);
            miProfiles = new WF.ToolStripMenuItem("Dispositions");
            menu.Items.Add(miProfiles);
            menu.Items.Add(new WF.ToolStripMenuItem("Ramener tous les panneaux à l'écran", null, (s, e) => c.FitAllOnScreen()));
            menu.Items.Add(new WF.ToolStripSeparator());
            menu.Items.Add(new WF.ToolStripMenuItem("Paramètres…", null, (s, e) => c.OpenSettings()));
            menu.Items.Add(new WF.ToolStripSeparator());
            menu.Items.Add(new WF.ToolStripMenuItem("Quitter " + App.Name, null, (s, e) => c.Quit()));
            menu.Opening += (s, e) =>
            {
                miToggle.Text = c.AllHidden ? "Afficher les cadres" : "Masquer les cadres";
                miLock.Checked = c.S.LockLayout;
                miTasks.Checked = c.S.TasksVisible;
                miNotes.Checked = c.S.NotesVisible;
                miMind.Checked = c.S.MindVisible;
                miProfiles.DropDownItems.Clear();
                string current = Controller.ProfileSignature();
                foreach (var p0 in c.L.Profiles)
                {
                    var p = p0;
                    var mi = new WF.ToolStripMenuItem((p.Auto ? "⟳ " : "") + p.Name + (p.Signature == current ? "   (écrans actuels)" : ""), null, (s2, e2) => c.ApplyProfile(p));
                    miProfiles.DropDownItems.Add(mi);
                }
                if (c.L.Profiles.Count > 0) miProfiles.DropDownItems.Add(new WF.ToolStripSeparator());
                miProfiles.DropDownItems.Add(new WF.ToolStripMenuItem("Enregistrer la disposition actuelle…", null, (s2, e2) =>
                {
                    var name = Dialogs.Prompt(null, "Instantané de disposition", "Nom (ex : Bureau 3 écrans)", Controller.ScreensDescription(), false);
                    if (!string.IsNullOrWhiteSpace(name)) { c.SaveSnapshot(name); c.Toast("Disposition « " + name.Trim() + " » enregistrée"); }
                }));
            };
            ni.ContextMenuStrip = menu;
            ni.MouseClick += (s, e) => { if (e.Button == WF.MouseButtons.Left) c.ToggleAll(); };
            ni.DoubleClick += (s, e) => c.QuickAdd();
            ni.Visible = true;
        }

        public void SetText(string text)
        {
            try { ni.Text = text.Length > 63 ? text.Substring(0, 63) : text; } catch { }
        }

        public void Balloon(string title, string text)
        {
            try { ni.ShowBalloonTip(6000, title, text, WF.ToolTipIcon.None); } catch { }
        }

        static SD.Icon LoadIcon()
        {
            try
            {
                var ic = SD.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
                if (ic != null) return ic;
            }
            catch { }
            return SD.SystemIcons.Application;
        }

        public void Dispose()
        {
            ni.Visible = false;
            ni.Dispose();
        }
    }
}
