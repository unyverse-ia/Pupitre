using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Pupitre
{
    /// <summary>Appels Win32 : fenêtres collées au bureau, icônes et éléments du shell, opérations fichiers, raccourcis globaux.</summary>
    internal static class Native
    {
        public const int GWL_EXSTYLE = -20, GWL_STYLE = -16, GWLP_HWNDPARENT = -8;
        public const int WS_EX_TOOLWINDOW = 0x80, WS_EX_APPWINDOW = 0x40000;
        public const int WS_MAXIMIZEBOX = 0x10000, WS_MINIMIZEBOX = 0x20000;
        public const int WM_WINDOWPOSCHANGING = 0x46, WM_NCLBUTTONDBLCLK = 0xA3, WM_MOVING = 0x216, WM_HOTKEY = 0x312, WM_DISPLAYCHANGE = 0x7E,
                         WM_COMMAND = 0x111, WM_SYSCOMMAND = 0x112, WM_EXITSIZEMOVE = 0x232, WM_ENTERSIZEMOVE = 0x231, WM_SETTINGCHANGE = 0x1A,
                         WM_QUERYENDSESSION = 0x11, WM_ENDSESSION = 0x16;
        public const int SC_MAXIMIZE = 0xF030, SC_MINIMIZE = 0xF020;
        public const uint SWP_NOSIZE = 1, SWP_NOMOVE = 2, SWP_NOZORDER = 4, SWP_NOACTIVATE = 0x10;
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public const uint MOD_ALT = 1, MOD_CONTROL = 2, MOD_SHIFT = 4, MOD_WIN = 8, MOD_NOREPEAT = 0x4000;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
            public int Width { get { return Right - Left; } }
            public int Height { get { return Bottom - Top; } }
        }
        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] public struct WINDOWPOS { public IntPtr hwnd, hwndInsertAfter; public int x, y, cx, cy; public uint flags; }

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string title);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string title);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder sb, int n);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr SendMessageTimeout(IntPtr h, int msg, IntPtr w, IntPtr l, uint flags, uint timeout, out IntPtr result);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr h);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetKeyNameText(int lParam, StringBuilder s, int n);
        [DllImport("user32.dll")] static extern uint MapVirtualKey(uint code, uint type);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
        [DllImport("user32.dll")] static extern bool AttachThreadInput(uint a, uint b, bool attach);
        [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr64(IntPtr h, int i);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr64(IntPtr h, int i, IntPtr v);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] static extern int GetWindowLong32(IntPtr h, int i);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] static extern int SetWindowLong32(IntPtr h, int i, int v);

        public static IntPtr GetWindowLongPtr(IntPtr h, int i) { return IntPtr.Size == 8 ? GetWindowLongPtr64(h, i) : new IntPtr(GetWindowLong32(h, i)); }
        public static IntPtr SetWindowLongPtr(IntPtr h, int i, IntPtr v) { return IntPtr.Size == 8 ? SetWindowLongPtr64(h, i, v) : new IntPtr(SetWindowLong32(h, i, v.ToInt32())); }

        public static string ClassName(IntPtr h)
        {
            var sb = new StringBuilder(128);
            GetClassName(h, sb, 128);
            return sb.ToString();
        }

        public static void SetExStyle(IntPtr h, int flag, bool on)
        {
            int s = (int)GetWindowLongPtr(h, GWL_EXSTYLE);
            int n = on ? (s | flag) : (s & ~flag);
            if (n != s) SetWindowLongPtr(h, GWL_EXSTYLE, new IntPtr(n));
        }

        public static void SetStyle(IntPtr h, int flag, bool on)
        {
            int s = (int)GetWindowLongPtr(h, GWL_STYLE);
            int n = on ? (s | flag) : (s & ~flag);
            if (n != s) SetWindowLongPtr(h, GWL_STYLE, new IntPtr(n));
        }

        public static void SendToBottom(IntPtr h)
        {
            SetWindowPos(h, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        /// <summary>Passe une fenêtre au premier plan même depuis un processus qui n'a pas le focus (saisie rapide).</summary>
        public static void ForceForeground(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            IntPtr fg = GetForegroundWindow();
            if (fg == hwnd) return;
            uint cur = GetCurrentThreadId();
            uint other = fg == IntPtr.Zero ? 0 : GetWindowThreadProcessId(fg, IntPtr.Zero);
            bool attached = other != 0 && other != cur && AttachThreadInput(cur, other, true);
            SetForegroundWindow(hwnd);
            if (attached) AttachThreadInput(cur, other, false);
        }

        // ---- Bureau : fenêtre hôte des icônes (Progman ou WorkerW contenant SHELLDLL_DefView) ----

        public static IntPtr FindDesktopHost()
        {
            IntPtr progman = FindWindow("Progman", null);
            if (progman != IntPtr.Zero && FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero) return progman;
            IntPtr found = IntPtr.Zero;
            EnumWindows(delegate(IntPtr h, IntPtr l)
            {
                if (ClassName(h) == "WorkerW" && FindWindowEx(h, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero) { found = h; return false; }
                return true;
            }, IntPtr.Zero);
            return found != IntPtr.Zero ? found : progman;
        }

        public static IntPtr FindDefView()
        {
            IntPtr host = FindDesktopHost();
            return host == IntPtr.Zero ? IntPtr.Zero : FindWindowEx(host, IntPtr.Zero, "SHELLDLL_DefView", null);
        }

        /// <summary>Bascule « Afficher les éléments du bureau » (même commande que le menu contextuel du bureau).</summary>
        public static bool ToggleDesktopIcons()
        {
            IntPtr dv = FindDefView();
            if (dv == IntPtr.Zero) return false;
            IntPtr res;
            SendMessageTimeout(dv, WM_COMMAND, new IntPtr(0x7402), IntPtr.Zero, 0x0002, 1000, out res);
            return true;
        }

        // ---- Shell : icônes et éléments virtuels (Corbeille, Ce PC…) ----

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon; public int iIcon; public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
        }
        public const uint SHGFI_ICON = 0x100, SHGFI_LARGEICON = 0, SHGFI_SYSICONINDEX = 0x4000, SHGFI_DISPLAYNAME = 0x200, SHGFI_PIDL = 0x8;
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr SHGetFileInfo(string path, uint attrs, ref SHFILEINFO info, uint cb, uint flags);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHGetFileInfoW")] public static extern IntPtr SHGetFileInfoPidl(IntPtr pidl, uint attrs, ref SHFILEINFO info, uint cb, uint flags);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern int SHParseDisplayName(string name, IntPtr bindingContext, out IntPtr pidl, uint sfgaoIn, out uint psfgaoOut);
        [DllImport("shell32.dll")] public static extern void ILFree(IntPtr pidl);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern int SHEmptyRecycleBin(IntPtr hwnd, string root, uint flags);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern int SHQueryRecycleBin(string root, ref SHQUERYRBINFO info);
        [StructLayout(LayoutKind.Sequential)] public struct SHQUERYRBINFO { public int cbSize; public long i64Size; public long i64NumItems; }
        [DllImport("shell32.dll")] public static extern int SHGetImageList(int list, ref Guid riid, out IImageList ppv);
        public const int SHIL_LARGE = 0, SHIL_SMALL = 1, SHIL_EXTRALARGE = 2, SHIL_JUMBO = 4;

        [ComImport, Guid("46EB5926-582E-4017-9FDF-E8998DAA0950"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IImageList
        {
            [PreserveSig] int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);
            [PreserveSig] int ReplaceIcon(int i, IntPtr hicon, ref int pi);
            [PreserveSig] int SetOverlayImage(int iImage, int iOverlay);
            [PreserveSig] int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
            [PreserveSig] int AddMasked(IntPtr hbmImage, int crMask, ref int pi);
            [PreserveSig] int Draw(IntPtr pimldp);
            [PreserveSig] int Remove(int i);
            [PreserveSig] int GetIcon(int i, int flags, ref IntPtr picon);
        }

        // ---- Shell : opérations fichiers (corbeille, déplacer, copier) ----

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd; public uint wFunc; public string pFrom; public string pTo; public ushort fFlags;
            [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted; public IntPtr hNameMappings; public string lpszProgressTitle;
        }
        public const uint FO_MOVE = 1, FO_COPY = 2, FO_DELETE = 3;
        public const ushort FOF_SILENT = 4, FOF_RENAMEONCOLLISION = 8, FOF_NOCONFIRMATION = 0x10, FOF_ALLOWUNDO = 0x40, FOF_NOCONFIRMMKDIR = 0x200, FOF_NOERRORUI = 0x400;
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern int SHFileOperation(ref SHFILEOPSTRUCT op);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHELLEXECUTEINFO
        {
            public int cbSize; public uint fMask; public IntPtr hwnd; public string lpVerb; public string lpFile; public string lpParameters; public string lpDirectory;
            public int nShow; public IntPtr hInstApp; public IntPtr lpIDList; public string lpClass; public IntPtr hkeyClass; public uint dwHotKey; public IntPtr hIcon; public IntPtr hProcess;
        }
        public const uint SEE_MASK_INVOKEIDLIST = 0x0C;
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO info);

        // ---- Raccourcis : libellés ----

        public static string KeyName(int vk)
        {
            if (vk <= 0) return "—";
            switch (vk)
            {
                case 0x11: case 0xA2: case 0xA3: return "Ctrl";
                case 0x10: case 0xA0: case 0xA1: return "Maj";
                case 0x12: case 0xA4: case 0xA5: return "Alt";
                case 0x5B: case 0x5C: return "Win";
                case 0x20: return "Espace";
                case 0x1B: return "Échap";
            }
            uint sc = MapVirtualKey((uint)vk, 0);
            var sb = new StringBuilder(64);
            if (sc != 0 && GetKeyNameText((int)(sc << 16), sb, 64) > 0) return sb.ToString();
            return "Touche " + vk;
        }

        public static string HotkeyText(uint mods, uint vk)
        {
            if (vk == 0) return "Aucun";
            var parts = new System.Collections.Generic.List<string>();
            if ((mods & MOD_CONTROL) != 0) parts.Add("Ctrl");
            if ((mods & MOD_ALT) != 0) parts.Add("Alt");
            if ((mods & MOD_SHIFT) != 0) parts.Add("Maj");
            if ((mods & MOD_WIN) != 0) parts.Add("Win");
            parts.Add(KeyName((int)vk));
            return string.Join(" + ", parts.ToArray());
        }
    }
}
