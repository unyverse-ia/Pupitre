using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Pupitre
{
    /// <summary>Icônes de fichiers via le shell Windows (liste d'images système 32 / 48 / 256 px), avec cache.</summary>
    internal static class ShellIcons
    {
        static readonly Dictionary<string, ImageSource> cache = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        static readonly Guid IID_IImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");


        public static ImageSource Get(string path, int px, bool isFolder)
        {
            string ext = isFolder ? "" : Path.GetExtension(path).ToLowerInvariant();
            bool perFile = isFolder || ext == ".exe" || ext == ".lnk" || ext == ".ico" || ext == ".url" || ext == ".appref-ms" || ext == ".cur" || ext.Length == 0;
            string key = (perFile ? path : ext) + "|" + px;
            ImageSource img;
            if (cache.TryGetValue(key, out img)) return img;
            img = Load(path, px);
            if (img != null) cache[key] = img;
            return img;
        }

        /// <summary>Icône et nom affiché d'un élément virtuel du shell (« ::{CLSID} »). L'icône suit l'état (corbeille pleine ou vide).</summary>
        public static ImageSource GetSpecial(string parsingName, int px, out string displayName)
        {
            displayName = null;
            IntPtr pidl = IntPtr.Zero;
            try
            {
                uint attrs;
                if (Native.SHParseDisplayName(parsingName, IntPtr.Zero, out pidl, 0, out attrs) != 0 || pidl == IntPtr.Zero) return null;
                var info = new Native.SHFILEINFO();
                uint cb = (uint)Marshal.SizeOf(info);
                if (Native.SHGetFileInfoPidl(pidl, 0, ref info, cb, Native.SHGFI_PIDL | Native.SHGFI_SYSICONINDEX | Native.SHGFI_DISPLAYNAME) == IntPtr.Zero) return null;
                displayName = info.szDisplayName;
                string key = "sys|" + info.iIcon + "|" + px;
                ImageSource img;
                if (cache.TryGetValue(key, out img)) return img;
                img = FromIndex(info.iIcon, px);
                if (img != null) cache[key] = img;
                return img;
            }
            catch (Exception ex) { Log.Error(ex, "icône système " + parsingName); return null; }
            finally { if (pidl != IntPtr.Zero) Native.ILFree(pidl); }
        }

        static ImageSource FromIndex(int iIcon, int px)
        {
            IntPtr hIcon = IntPtr.Zero;
            try
            {
                int list = px <= 16 ? Native.SHIL_SMALL : (px <= 32 ? Native.SHIL_LARGE : (px <= 48 ? Native.SHIL_EXTRALARGE : Native.SHIL_JUMBO));
                Native.IImageList il;
                Guid iid = IID_IImageList;
                if (Native.SHGetImageList(list, ref iid, out il) == 0 && il != null)
                {
                    il.GetIcon(iIcon, 1, ref hIcon);
                    if (hIcon != IntPtr.Zero && list == Native.SHIL_JUMBO && IsPaddedJumbo(hIcon))
                    {
                        Native.DestroyIcon(hIcon); hIcon = IntPtr.Zero;
                        if (Native.SHGetImageList(Native.SHIL_EXTRALARGE, ref iid, out il) == 0 && il != null) il.GetIcon(iIcon, 1, ref hIcon);
                    }
                }
                if (hIcon == IntPtr.Zero) return null;
                var bs = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                bs.Freeze();
                return bs;
            }
            finally { if (hIcon != IntPtr.Zero) Native.DestroyIcon(hIcon); }
        }

        static ImageSource Load(string path, int px)
        {
            IntPtr hIcon = IntPtr.Zero;
            try
            {
                var info = new Native.SHFILEINFO();
                uint cb = (uint)Marshal.SizeOf(info);
                IntPtr res = Native.SHGetFileInfo(path, 0, ref info, cb, Native.SHGFI_SYSICONINDEX);
                if (res != IntPtr.Zero)
                {
                    int list = px <= 16 ? Native.SHIL_SMALL : (px <= 32 ? Native.SHIL_LARGE : (px <= 48 ? Native.SHIL_EXTRALARGE : Native.SHIL_JUMBO));
                    Native.IImageList il;
                    Guid iid = IID_IImageList;
                    if (Native.SHGetImageList(list, ref iid, out il) == 0 && il != null)
                    {
                        il.GetIcon(info.iIcon, 1, ref hIcon);
                        if (hIcon != IntPtr.Zero && list == Native.SHIL_JUMBO && IsPaddedJumbo(hIcon))
                        {
                            // Certaines icônes n'existent pas en 256 px : le shell renvoie une 48 px dans un cadre vide → on prend la 48 px.
                            Native.DestroyIcon(hIcon); hIcon = IntPtr.Zero;
                            if (Native.SHGetImageList(Native.SHIL_EXTRALARGE, ref iid, out il) == 0 && il != null) il.GetIcon(info.iIcon, 1, ref hIcon);
                        }
                    }
                }
                if (hIcon == IntPtr.Zero)
                {
                    var info2 = new Native.SHFILEINFO();
                    Native.SHGetFileInfo(path, 0, ref info2, cb, Native.SHGFI_ICON | Native.SHGFI_LARGEICON);
                    hIcon = info2.hIcon;
                }
                if (hIcon == IntPtr.Zero) return null;
                var bs = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                bs.Freeze();
                return bs;
            }
            catch (Exception ex) { Log.Error(ex, "icône " + path); return null; }
            finally { if (hIcon != IntPtr.Zero) Native.DestroyIcon(hIcon); }
        }

        /// <summary>Vrai si l'icône 256 px est en réalité une petite icône centrée (bordure entièrement transparente).</summary>
        static bool IsPaddedJumbo(IntPtr hIcon)
        {
            try
            {
                var bs = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                if (bs.PixelWidth < 200) return false;
                var conv = new FormatConvertedBitmap(bs, PixelFormats.Bgra32, null, 0);
                int w = conv.PixelWidth, h = conv.PixelHeight, stride = w * 4;
                var row = new byte[stride];
                // bande horizontale en haut (ligne 40) : si tout est transparent, l'icône est « rembourrée »
                conv.CopyPixels(new Int32Rect(0, h / 6, w, 1), row, stride, 0);
                for (int x = 0; x < w; x++) if (row[x * 4 + 3] != 0) return false;
                return true;
            }
            catch { return false; }
        }
    }
}
