using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

/// <summary>Fabrique plume.ico à partir d'un PNG (si fourni) ou d'un dessin vectoriel de repli. Aucune dépendance.</summary>
static class MakeIcon
{
    static int Main(string[] args)
    {
        string src = args.Length > 0 ? args[0] : null;
        string dst = args.Length > 1 ? args[1] : "plume.ico";
        Bitmap master = null;
        if (src != null && File.Exists(src))
        {
            try { master = new Bitmap(src); Console.WriteLine("Icone : source " + src); }
            catch { master = null; }
        }
        if (master == null)
        {
            master = DrawFallback(1024);
            Console.WriteLine("Icone : dessin de repli (aucun PNG fourni)");
        }
        int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 128, 256 };
        var images = new List<byte[]>();
        foreach (var s in sizes)
        {
            using (var bmp = Resize(master, s))
            {
                images.Add(s >= 64 ? Png(bmp) : Bmp(bmp));
            }
        }
        Write(dst, sizes, images);
        Console.WriteLine("Icone : " + dst);
        return 0;
    }

    static Bitmap Resize(Bitmap m, int s)
    {
        var b = new Bitmap(s, s, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(b))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.Clear(Color.Transparent);
            g.DrawImage(m, new Rectangle(0, 0, s, s), 0, 0, m.Width, m.Height, GraphicsUnit.Pixel);
        }
        return b;
    }

    static byte[] Png(Bitmap b)
    {
        using (var ms = new MemoryStream()) { b.Save(ms, ImageFormat.Png); return ms.ToArray(); }
    }

    // Entrée ICO classique : BITMAPINFOHEADER + pixels BGRA (bas en haut) + masque AND 1 bit.
    static byte[] Bmp(Bitmap b)
    {
        int w = b.Width, h = b.Height;
        int maskRow = (w + 31) / 32 * 4;
        var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);
        bw.Write(40); bw.Write(w); bw.Write(h * 2); bw.Write((short)1); bw.Write((short)32);
        bw.Write(0); bw.Write(w * h * 4 + maskRow * h); bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
        for (int y = h - 1; y >= 0; y--)
            for (int x = 0; x < w; x++)
            {
                var c = b.GetPixel(x, y);
                bw.Write(c.B); bw.Write(c.G); bw.Write(c.R); bw.Write(c.A);
            }
        for (int y = h - 1; y >= 0; y--)
        {
            var row = new byte[maskRow];
            for (int x = 0; x < w; x++)
                if (b.GetPixel(x, y).A < 128) row[x / 8] |= (byte)(0x80 >> (x % 8));
            bw.Write(row);
        }
        bw.Flush();
        return ms.ToArray();
    }

    static void Write(string path, int[] sizes, List<byte[]> imgs)
    {
        using (var fs = File.Create(path))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write((short)0); bw.Write((short)1); bw.Write((short)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                int s = sizes[i];
                bw.Write((byte)(s >= 256 ? 0 : s)); bw.Write((byte)(s >= 256 ? 0 : s));
                bw.Write((byte)0); bw.Write((byte)0);
                bw.Write((short)1); bw.Write((short)32);
                bw.Write(imgs[i].Length); bw.Write(offset);
                offset += imgs[i].Length;
            }
            foreach (var im in imgs) bw.Write(im);
        }
    }

    static Bitmap DrawFallback(int s)
    {
        var b = new Bitmap(s, s, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(b))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            float m = s * 0.05f;
            var rect = new RectangleF(m, m, s - 2 * m, s - 2 * m);
            using (var path = RoundRect(rect, s * 0.24f))
            using (var br = new SolidBrush(Color.FromArgb(0x14, 0x14, 0x16)))
                g.FillPath(br, path);
            using (var pen = new Pen(Color.FromArgb(0xFF, 0x6A, 0x3D), s * 0.11f))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                g.DrawBezier(pen,
                    new PointF(s * 0.24f, s * 0.72f), new PointF(s * 0.28f, s * 0.18f),
                    new PointF(s * 0.66f, s * 0.90f), new PointF(s * 0.76f, s * 0.30f));
            }
            float d = s * 0.085f;
            g.FillEllipse(Brushes.White, s * 0.76f - d / 2, s * 0.30f - d / 2, d, d);
        }
        return b;
    }

    static GraphicsPath RoundRect(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        float d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
