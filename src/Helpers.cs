using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Pupitre
{
    internal static class App
    {
        public const string Name = "Pupitre";
        public static string DataDir { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Name); } }
        public static string Desktop { get { return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); } }
        public static string PublicDesktop { get { return Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory); } }
        public static string ExePath { get { return System.Reflection.Assembly.GetExecutingAssembly().Location; } }

        public static string SamePath(string a, string b)
        {
            return string.Equals(Norm(a), Norm(b), StringComparison.OrdinalIgnoreCase) ? Norm(a) : null;
        }
        public static bool PathEquals(string a, string b) { return SamePath(a, b) != null; }
        public static string Norm(string p)
        {
            if (string.IsNullOrEmpty(p)) return "";
            try { return Path.GetFullPath(p).TrimEnd('\\', '/'); } catch { return p.TrimEnd('\\', '/'); }
        }
    }

    internal static class Log
    {
        static bool shown;
        public static void Error(Exception ex, string context)
        {
            try
            {
                Directory.CreateDirectory(App.DataDir);
                File.AppendAllText(Path.Combine(App.DataDir, "erreurs.log"),
                    DateTime.Now.ToString("s") + "  [" + context + "]  " + ex + Environment.NewLine + Environment.NewLine);
            }
            catch { }
        }
        public static void Fatal(Exception ex)
        {
            Error(ex, "non géré");
            if (shown) return;
            shown = true;
            MessageBox.Show(App.Name + " a rencontré une erreur (détails dans " + App.DataDir + "\\erreurs.log) :\n\n" + ex.Message, App.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>Sérialisation JSON (DataContractJsonSerializer, inclus dans .NET), avec écriture atomique.</summary>
    internal static class Json
    {
        public static T Load<T>(string path) where T : class, new()
        {
            try
            {
                if (!File.Exists(path)) return new T();
                var bytes = File.ReadAllBytes(path);
                int start = (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) ? 3 : 0; // BOM (fichier édité avec le Bloc-notes)
                if (bytes.Length - start == 0) return new T();
                using (var ms = new MemoryStream(bytes, start, bytes.Length - start))
                {
                    var ser = new DataContractJsonSerializer(typeof(T));
                    var obj = ser.ReadObject(ms) as T;
                    return obj ?? new T();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "lecture " + Path.GetFileName(path));
                try { File.Copy(path, path + ".corrompu-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"), true); } catch { }
                return new T();
            }
        }

        public static void Save<T>(string path, T obj)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string tmp = path + ".tmp";
                using (var fs = File.Create(tmp))
                {
                    var ser = new DataContractJsonSerializer(typeof(T));
                    ser.WriteObject(fs, obj);
                }
                if (File.Exists(path)) File.Replace(tmp, path, null); else File.Move(tmp, path);
            }
            catch (Exception ex) { Log.Error(ex, "écriture " + Path.GetFileName(path)); }
        }
    }

    /// <summary>Lancement avec Windows (HKCU\...\Run), écrit uniquement sur demande de l'utilisateur.</summary>
    internal static class Startup
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public static bool IsEnabled()
        {
            try { using (var k = Registry.CurrentUser.OpenSubKey(RunKey)) return k != null && k.GetValue(App.Name) != null; }
            catch { return false; }
        }
        public static void Set(bool enabled)
        {
            try
            {
                using (var k = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (k == null) return;
                    if (enabled) k.SetValue(App.Name, "\"" + App.ExePath + "\"");
                    else if (k.GetValue(App.Name) != null) k.DeleteValue(App.Name, false);
                }
            }
            catch (Exception ex) { Log.Error(ex, "démarrage"); }
        }
    }

    internal static class DesktopIcons
    {
        public static bool AreHidden()
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    if (k == null) return false;
                    var v = k.GetValue("HideIcons");
                    return v is int && (int)v == 1;
                }
            }
            catch { return false; }
        }
        /// <summary>Bascule l'affichage des icônes du bureau puis vérifie (l'Explorateur applique la commande de façon asynchrone).</summary>
        /// <summary>Même bascule, mais attend le résultat : utilisée quand l'application s'arrête (fermeture de session, plantage).</summary>
        public static void SetHiddenNow(bool hidden)
        {
            for (int attempt = 0; attempt < 3 && AreHidden() != hidden; attempt++)
            {
                Native.ToggleDesktopIcons();
                for (int i = 0; i < 10 && AreHidden() != hidden; i++) System.Threading.Thread.Sleep(50);
            }
        }

        public static void SetHidden(bool hidden)
        {
            if (AreHidden() == hidden) return;
            Native.ToggleDesktopIcons();
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
            int tries = 0;
            t.Tick += delegate
            {
                if (AreHidden() == hidden || ++tries >= 3) { t.Stop(); return; }
                Native.ToggleDesktopIcons();
            };
            t.Start();
        }
    }

    /// <summary>Opérations sur les fichiers via le shell (corbeille, annulation Ctrl+Z dans l'Explorateur, conflits gérés par Windows).</summary>
    internal static class FileOps
    {
        static string Multi(IEnumerable<string> paths)
        {
            var sb = new StringBuilder();
            foreach (var p in paths) { sb.Append(p); sb.Append('\0'); }
            sb.Append('\0');
            return sb.ToString();
        }

        public static bool Transfer(IEnumerable<string> paths, string targetFolder, bool copy, bool silent, IntPtr owner)
        {
            var op = new Native.SHFILEOPSTRUCT();
            op.hwnd = owner;
            op.wFunc = copy ? Native.FO_COPY : Native.FO_MOVE;
            op.pFrom = Multi(paths);
            op.pTo = targetFolder + "\0\0";
            op.fFlags = (ushort)(Native.FOF_ALLOWUNDO | Native.FOF_NOCONFIRMMKDIR | (silent ? (Native.FOF_SILENT | Native.FOF_NOCONFIRMATION | Native.FOF_RENAMEONCOLLISION | Native.FOF_NOERRORUI) : 0));
            int r = Native.SHFileOperation(ref op);
            return r == 0 && !op.fAnyOperationsAborted;
        }

        /// <summary>Ouvre un élément virtuel du bureau (Corbeille, Ce PC…) dans l'Explorateur.</summary>
        public static void OpenSpecial(string parsingName)
        {
            try { Process.Start(new ProcessStartInfo("explorer.exe", parsingName) { UseShellExecute = true }); } catch { }
        }

        /// <summary>Vide la Corbeille avec la confirmation standard de Windows.</summary>
        public static void EmptyRecycleBin(IntPtr owner)
        {
            try { Native.SHEmptyRecycleBin(owner, null, 0); } catch (Exception ex) { Log.Error(ex, "vider la corbeille"); }
        }

        public static bool RecycleBinEmpty()
        {
            try
            {
                var q = new Native.SHQUERYRBINFO { cbSize = Marshal.SizeOf(typeof(Native.SHQUERYRBINFO)) };
                return Native.SHQueryRecycleBin(null, ref q) == 0 && q.i64NumItems == 0;
            }
            catch { return false; }
        }

        public static bool Recycle(IEnumerable<string> paths, IntPtr owner)
        {
            var op = new Native.SHFILEOPSTRUCT();
            op.hwnd = owner;
            op.wFunc = Native.FO_DELETE;
            op.pFrom = Multi(paths);
            op.fFlags = (ushort)(Native.FOF_ALLOWUNDO);
            int r = Native.SHFileOperation(ref op);
            return r == 0 && !op.fAnyOperationsAborted;
        }

        public static bool Rename(string path, string newName, IntPtr owner)
        {
            try
            {
                string target = Path.Combine(Path.GetDirectoryName(path), newName);
                if (App.PathEquals(path, target)) return true;
                if (Directory.Exists(path)) Directory.Move(path, target); else File.Move(path, target);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Impossible de renommer : " + ex.Message, App.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        public static void Open(string path)
        {
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("Impossible d'ouvrir « " + Path.GetFileName(path) + " » : " + ex.Message, App.Name, MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        public static void OpenWith(string path)
        {
            try { Process.Start(new ProcessStartInfo("rundll32.exe", "shell32.dll,OpenAs_RunDLL " + path) { UseShellExecute = true }); } catch { }
        }

        public static void ShowInExplorer(string path)
        {
            try
            {
                if (Directory.Exists(path) && !File.Exists(path) && path.EndsWith("\\")) Process.Start("explorer.exe", "\"" + path + "\"");
                else Process.Start("explorer.exe", "/select,\"" + path + "\"");
            }
            catch { }
        }

        public static void OpenFolder(string folder)
        {
            try { Process.Start(new ProcessStartInfo("explorer.exe", "\"" + folder + "\"") { UseShellExecute = true }); } catch { }
        }

        public static void Properties(string path, IntPtr owner)
        {
            var info = new Native.SHELLEXECUTEINFO();
            info.cbSize = Marshal.SizeOf(info);
            info.fMask = Native.SEE_MASK_INVOKEIDLIST;
            info.hwnd = owner;
            info.lpVerb = "properties";
            info.lpFile = path;
            info.nShow = 5;
            Native.ShellExecuteEx(ref info);
        }

        /// <summary>Icônes système que Windows affiche sur le bureau (Corbeille, Ce PC, Réseau…), selon le réglage de l'utilisateur.</summary>
    internal static class DesktopSpecials
    {
        public const string RecycleBin = "::{645FF040-5081-101B-9F08-00AA002F954E}";
        static readonly string[] Others = { "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}", "::{59031a47-3f72-44a7-89c5-5595fe6b30ee}", "::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", "::{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}" };

        /// <summary>Corbeille d'abord (sauf si masquée par l'utilisateur), puis les autres éléments cochés dans « Paramètres des icônes du bureau ».</summary>
        public static List<string> Shown()
        {
            var list = new List<string>();
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel"))
                {
                    if (!Hidden(k, RecycleBin, false)) list.Add(RecycleBin);
                    foreach (var o in Others) if (!Hidden(k, o, true)) list.Add(o);
                }
            }
            catch { if (list.Count == 0) list.Add(RecycleBin); }
            return list;
        }

        static bool Hidden(Microsoft.Win32.RegistryKey k, string parsing, bool defaultHidden)
        {
            if (k == null) return defaultHidden;
            object v = k.GetValue(parsing.Substring(2));
            if (v is int) return (int)v != 0;
            return defaultHidden;
        }
    }

    /// <summary>Nom unique dans un dossier : « nom (2).ext » si nécessaire.</summary>
        public static string UniqueName(string folder, string name)
        {
            string candidate = Path.Combine(folder, name);
            if (!File.Exists(candidate) && !Directory.Exists(candidate)) return name;
            string stem = Path.GetFileNameWithoutExtension(name), ext = Path.GetExtension(name);
            for (int i = 2; i < 1000; i++)
            {
                string n = stem + " (" + i + ")" + ext;
                candidate = Path.Combine(folder, n);
                if (!File.Exists(candidate) && !Directory.Exists(candidate)) return n;
            }
            return name;
        }

        public static bool MatchesRules(string fileName, string rules)
        {
            if (string.IsNullOrEmpty(rules)) return false;
            foreach (var raw in rules.Split(new[] { ';', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string pat = raw.Trim();
                if (pat.Length == 0) continue;
                if (!pat.Contains("*") && !pat.Contains("?")) pat = pat.StartsWith(".") ? "*" + pat : "*" + pat + "*";
                string rx = "^" + System.Text.RegularExpressions.Regex.Escape(pat).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                if (System.Text.RegularExpressions.Regex.IsMatch(fileName, rx, System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return true;
            }
            return false;
        }
    }

    /// <summary>Regroupe des appels rapprochés en un seul (sauvegardes, rafraîchissements).</summary>
    internal class Debouncer
    {
        readonly DispatcherTimer timer;
        readonly Action action;
        public Debouncer(int ms, Action act)
        {
            action = act;
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
            timer.Tick += delegate { timer.Stop(); action(); };
        }
        public void Trigger() { timer.Stop(); timer.Start(); }
        public void Flush() { if (timer.IsEnabled) { timer.Stop(); action(); } }
    }
}
