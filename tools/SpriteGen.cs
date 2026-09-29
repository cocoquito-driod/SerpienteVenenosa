using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

public static class SpriteGen
{
    // ---- Geometria compartida con el mod (mantener sincronizado con SerpentGeometry.cs) ----
    public static PointF HeadPivot = new PointF(46, 78);
    public static PointF BodyAnchorOffset = new PointF(29, 0);
    public static PointF BodyAnchorDirection = new PointF(1f, 0.25f);
    public static float BodyAnchorGap = 8f;
    public static float Spacing = 24f; public static double MaxBend = 0.38;
    public const int BodyW = 88, BodyH = 34, BodyCore = 24;
    public static PointF BodyPivot = new PointF(44, 17);
    public const int TailW = 64, TailH = 80;
    public static PointF TailPivot = new PointF(32, 15);

    static Color[] G; static Color Outline; static Color[] Red, Yel, Blu;
    static StringBuilder log = new StringBuilder();

    class Canvas
    {
        public int W, H; public Color[] P;
        public Canvas(int w, int h) { W = w; H = h; P = new Color[w * h]; for (int i = 0; i < P.Length; i++) P[i] = Color.FromArgb(0, 0, 0, 0); }
        public bool In(int x, int y) { return x >= 0 && y >= 0 && x < W && y < H; }
        public Color Get(int x, int y) { return In(x, y) ? P[y * W + x] : Color.FromArgb(0, 0, 0, 0); }
        public void Set(int x, int y, Color c) { if (In(x, y)) P[y * W + x] = c; }
        public bool Op(int x, int y) { return Get(x, y).A > 0; }
        public void OutlinePass(Color o)
        {
            Color[] copy = (Color[])P.Clone();
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                if (copy[y * W + x].A == 0) continue;
                bool edge = false;
                int[] dx = { 1, -1, 0, 0 }; int[] dy = { 0, 0, 1, -1 };
                for (int k = 0; k < 4; k++) { int nx = x + dx[k], ny = y + dy[k]; if (!In(nx, ny) || copy[ny * W + nx].A == 0) edge = true; }
                if (edge) P[y * W + x] = o;
            }
        }
        public void Over(Canvas top, int ox, int oy)
        {
            for (int y = 0; y < top.H; y++) for (int x = 0; x < top.W; x++) { Color c = top.Get(x, y); if (c.A > 0) Set(x + ox, y + oy, Blend(c, Get(x + ox, y + oy))); }
        }
        public Bitmap ToBitmap(int s)
        {
            Bitmap b = new Bitmap(W * s, H * s, PixelFormat.Format32bppArgb);
            for (int y = 0; y < H * s; y++) for (int x = 0; x < W * s; x++) b.SetPixel(x, y, P[(y / s) * W + (x / s)]);
            return b;
        }
        public void Save(string path, int s) { Directory.CreateDirectory(Path.GetDirectoryName(path)); using (Bitmap b = ToBitmap(s)) b.Save(path, ImageFormat.Png); log.AppendLine("saved " + path + " (" + (W * s) + "x" + (H * s) + ")"); }
    }

    static Color Blend(Color top, Color bottom)
    {
        if (top.A == 255 || bottom.A == 0) return top;
        double a = top.A / 255.0, b = bottom.A / 255.0; double oa = a + b * (1 - a);
        Func<int, int, int> mix = delegate(int t, int u) { return (int)((t * a + u * b * (1 - a)) / oa); };
        return Color.FromArgb((int)(oa * 255), mix(top.R, bottom.R), mix(top.G, bottom.G), mix(top.B, bottom.B));
    }
    static int Mod(int a, int m) { int r = a % m; return r < 0 ? r + m : r; }
    static double Lum(Color c) { return 0.299 * c.R + 0.587 * c.G + 0.114 * c.B; }
    static Color Shade(Color c, double f) { return Color.FromArgb(c.A, Clamp((int)(c.R * f)), Clamp((int)(c.G * f)), Clamp((int)(c.B * f))); }
    static int Clamp(int v) { return v < 0 ? 0 : v > 255 ? 255 : v; }

    static char Cls(Color c)
    {
        if (c.A < 40) return '.';
        int r = c.R, g = c.G, bl = c.B; int mx = Math.Max(r, Math.Max(g, bl)), mn = Math.Min(r, Math.Min(g, bl));
        if (mx < 50) return 'D';
        if (mn > 190) return 'W';
        if (r > 150 && g > 120 && bl < 90) return 'Y';
        if (r > 130 && g < 90) return 'R';
        if (bl > 110 && bl > r + 20) return 'B';
        if (g >= r && g >= bl) return 'G';
        return 'o';
    }

    static Color[] Ramp(List<Color> list, double[] qs)
    {
        list.Sort(delegate(Color a, Color b) { return Lum(a).CompareTo(Lum(b)); });
        Color[] r = new Color[qs.Length];
        int win = Math.Max(1, list.Count / 30);
        for (int i = 0; i < qs.Length; i++)
        {
            int idx = (int)(qs[i] * (list.Count - 1));
            int lo = Math.Max(0, idx - win), hi = Math.Min(list.Count - 1, idx + win);
            long sr = 0, sg = 0, sb = 0; int n = 0;
            for (int k = lo; k <= hi; k++) { sr += list[k].R; sg += list[k].G; sb += list[k].B; n++; }
            r[i] = Color.FromArgb(255, (int)(sr / n), (int)(sg / n), (int)(sb / n));
        }
        return r;
    }

    static void ExtractPalette(Bitmap src)
    {
        List<Color> g = new List<Color>(), d = new List<Color>(), rr = new List<Color>(), yy = new List<Color>(), bb = new List<Color>();
        for (int y = 0; y < src.Height; y++) for (int x = 0; x < src.Width; x++)
        {
            Color c = src.GetPixel(x, y); char k = Cls(c);
            if (k == 'G') g.Add(c); else if (k == 'D') d.Add(c); else if (k == 'R') rr.Add(c); else if (k == 'Y') yy.Add(c); else if (k == 'B') bb.Add(c);
        }
        G = Ramp(g, new double[] { 0.06, 0.25, 0.5, 0.75, 0.94 });
        Outline = Shade(G[0], 0.5);
        Red = Ramp(rr, new double[] { 0.15, 0.5, 0.85 });
        Yel = Ramp(yy, new double[] { 0.15, 0.5, 0.85 });
        Blu = Ramp(bb, new double[] { 0.15, 0.5, 0.85 });
        // Las rampas extraidas quedan muy juntas: se reespacian a partir del tono medio.
        Red = new Color[] { Shade(Red[1], 0.7), Red[1], Color.FromArgb(255, 0xC8, 0x4A, 0x3E) };
        Yel = new Color[] { Shade(Yel[1], 0.78), Yel[1], Yel[2] };
        Blu = new Color[] { Shade(Blu[1], 0.72), Blu[1], Blu[2] };
        log.AppendLine("G: " + Hex(G) + " outline " + Hex(new Color[] { Outline }));
        log.AppendLine("R: " + Hex(Red) + " Y: " + Hex(Yel) + " B: " + Hex(Blu));
    }
    static string Hex(Color[] cs) { StringBuilder s = new StringBuilder(); foreach (Color c in cs) s.Append(string.Format("#{0:X2}{1:X2}{2:X2} ", c.R, c.G, c.B)); return s.ToString(); }

    // ---- Sin cuello: debajo de esta linea (coordenadas de la imagen original) se borra la piel verde
    // y se conservan las plumas. El cuerpo se engancha detras de la cabeza, bajo la cresta. ----
    static double[] CutX = { 0, 96, 122, 150, 256 };
    static double[] CutY = { 214, 208, 190, 170, 170 };

    static double CutLine(double x)
    {
        for (int i = 1; i < CutX.Length; i++)
            if (x <= CutX[i]) return CutY[i - 1] + (CutY[i] - CutY[i - 1]) * (x - CutX[i - 1]) / (CutX[i] - CutX[i - 1]);
        return CutY[CutY.Length - 1];
    }

    static bool IsFeather(Color c)
    {
        char k = Cls(c);
        if (k == 'R' || k == 'Y' || k == 'B' || k == 'W') return true;
        return k == 'o' && c.G < Math.Max(c.R, c.B);
    }

    static Bitmap RemoveNeck(Bitmap src)
    {
        int w = src.Width, h = src.Height;
        Color[] p = new Color[w * h];
        bool[] zone = new bool[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { p[y * w + x] = src.GetPixel(x, y); zone[y * w + x] = y > CutLine(x); }
        Color clear = Color.FromArgb(0, 0, 0, 0);
        bool[] feather = new bool[w * h];
        for (int i = 0; i < p.Length; i++) feather[i] = p[i].A > 40 && IsFeather(p[i]);

        bool[] removed = new bool[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            int i = y * w + x;
            if (!zone[i] || p[i].A <= 40 || feather[i]) continue;
            bool nearFeather = false;
            if (Cls(p[i]) == 'D')
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                { int nx = x + dx, ny = y + dy; if (nx >= 0 && ny >= 0 && nx < w && ny < h && feather[ny * w + nx]) nearFeather = true; }
            if (!nearFeather) { p[i] = clear; removed[i] = true; }
        }
        // Restos sueltos dentro de la zona recortada.
        for (int pass = 0; pass < 2; pass++)
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (!zone[i] || p[i].A <= 40) continue;
                int n = 0;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                { int nx = x + dx, ny = y + dy; if ((dx != 0 || dy != 0) && nx >= 0 && ny >= 0 && nx < w && ny < h && p[ny * w + nx].A > 40) n++; }
                if (n < 3) { p[i] = clear; removed[i] = true; }
            }
        // La piel que quedo en el borde del corte recibe contorno oscuro.
        Color[] snap = (Color[])p.Clone();
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            int i = y * w + x;
            if (snap[i].A <= 40 || feather[i]) continue;
            bool edge = false;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            { int nx = x + dx, ny = y + dy; if (nx >= 0 && ny >= 0 && nx < w && ny < h && removed[ny * w + nx]) edge = true; }
            if (edge) p[i] = Outline;
        }
        Bitmap result = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) result.SetPixel(x, y, p[y * w + x]);
        return result;
    }

    // ---- Cabeza: reduccion 2:1 de la imagen original (ya sin cuello) ----
    static Canvas MakeHead(Bitmap original)
    {
        Bitmap src = RemoveNeck(original);
        Canvas cv = new Canvas(src.Width / 2, src.Height / 2);
        for (int y = 0; y < cv.H; y++) for (int x = 0; x < cv.W; x++)
        {
            double sa = 0, sr = 0, sg = 0, sb = 0;
            for (int j = 0; j < 2; j++) for (int i = 0; i < 2; i++)
            { Color c = src.GetPixel(x * 2 + i, y * 2 + j); double a = c.A / 255.0; sa += a; sr += c.R * a; sg += c.G * a; sb += c.B * a; }
            if (sa / 4 < 0.45) continue;
            cv.Set(x, y, Color.FromArgb(255, Clamp((int)(sr / sa)), Clamp((int)(sg / sa)), Clamp((int)(sb / sa))));
        }
        int fadeStart = cv.H - 12;
        for (int y = fadeStart; y < cv.H; y++)
        {
            double f = 1.0 - (y - fadeStart + 1) / 13.0;
            for (int x = 0; x < cv.W; x++) { Color c = cv.Get(x, y); if (c.A > 0) cv.Set(x, y, Color.FromArgb((int)(255 * f), c.R, c.G, c.B)); }
        }
        src.Dispose();
        return cv;
    }

    // ---- Escamas con sombreado cilindrico ----
    static Color Scale(double t, int x, int y)
    {
        double b = 1.0 - Math.Abs(t + 0.3) * 0.95;
        int idx = b > 0.78 ? 4 : b > 0.52 ? 3 : b > 0.28 ? 2 : b > 0.06 ? 1 : 0;
        int u = 3 * x + 4 * y, v = 3 * x - 4 * y + 300;
        if (Mod(u, 30) < 5 || Mod(v, 30) < 5) idx = Math.Max(0, idx - 1);
        else if (Mod(u, 30) < 10 && Mod(v, 30) < 10 && idx < 4) idx++;
        return G[idx];
    }

    static void Feather(Canvas cv, double bx, double by, double dx, double dy, double len, double wmax, Color[] ramp)
    {
        double n = Math.Sqrt(dx * dx + dy * dy); dx /= n; dy /= n;
        for (int y = 0; y < cv.H; y++) for (int x = 0; x < cv.W; x++)
        {
            double px = x + 0.5 - bx, py = y + 0.5 - by;
            double s = px * dx + py * dy, d = -px * dy + py * dx;
            if (s < 0 || s > len) continue;
            double f = s / len;
            double hw = wmax * Math.Pow(Math.Sin(Math.PI * Math.Pow(f, 0.75)), 0.6);
            if (Math.Abs(d) > hw) continue;
            int idx = d < 0 ? 2 : 1;
            if (Math.Abs(d) < 0.6 && f < 0.9) idx = 0;
            else if (Mod((int)(s * 0.9 + Math.Abs(d) * 1.3), 4) == 0) idx = Math.Max(0, idx - 1);
            cv.Set(x, y, ramp[idx]);
        }
    }

    static Canvas BodyFrame(bool feathered)
    {
        Canvas result = new Canvas(BodyW, BodyH);
        double cx = BodyW / 2.0;
        if (feathered)
        {
            Canvas fl = new Canvas(BodyW, BodyH);
            Color[][] cols = { Yel, Red, Blu };
            double[] by = { 9, 15, 21 }, len = { 19, 23, 18 }; double[] dy = { 0.25, 0.42, 0.62 };
            for (int k = 0; k < 3; k++)
            {
                Feather(fl, cx - BodyCore + 4, by[k], -1, dy[k], len[k], 3.6, cols[k]);
                Feather(fl, cx + BodyCore - 4, by[k], 1, dy[k], len[k], 3.6, cols[k]);
            }
            fl.OutlinePass(Outline);
            result.Over(fl, 0, 0);
        }
        Canvas body = new Canvas(BodyW, BodyH);
        for (int y = 0; y < BodyH; y++)
        {
            double v = (y + 0.5 - BodyH / 2.0) / (BodyH / 2.0);
            double hw = BodyCore * Math.Pow(1 - Math.Pow(Math.Abs(v), 4), 0.25);
            for (int x = 0; x < BodyW; x++)
            {
                double t = (x + 0.5 - cx) / hw; if (Math.Abs(t) > 1) continue;
                body.Set(x, y, Scale(t, x, y));
            }
        }
        Color[] snapshot = (Color[])body.P.Clone();
        Func<int, int, bool> empty = delegate(int x, int y) { return !body.In(x, y) || snapshot[y * BodyW + x].A == 0; };
        for (int y = 0; y < BodyH; y++) for (int x = 0; x < BodyW; x++)
        {
            if (snapshot[y * BodyW + x].A == 0) continue;
            if (empty(x - 1, y) || empty(x + 1, y) || empty(x, y + 1)) body.Set(x, y, Outline);
            else if (empty(x, y + 2) || empty(x, y + 3)) body.Set(x, y, Shade(snapshot[y * BodyW + x], 0.8));
        }
        result.Over(body, 0, 0);
        return result;
    }

    static Canvas Tail()
    {
        Canvas result = new Canvas(TailW, TailH);
        double cx = TailW / 2.0;
        Canvas fl = new Canvas(TailW, TailH);
        double[] ang = { -42, 42, -21, 21, 0 }; double[] len = { 30, 30, 38, 38, 46 };
        Color[][] cols = { Blu, Blu, Yel, Yel, Red };
        for (int k = 0; k < 5; k++)
        {
            double a = ang[k] * Math.PI / 180.0;
            Feather(fl, cx, 30, Math.Sin(a), Math.Cos(a), len[k], 5.2, cols[k]);
        }
        fl.OutlinePass(Outline);
        result.Over(fl, 0, 0);
        Canvas body = new Canvas(TailW, TailH);
        int tip = 44;
        for (int y = 0; y < tip; y++)
        {
            double hw = BodyCore * (1 - Math.Pow(y / (double)tip, 1.4)) - (y == 0 ? 2.5 : y == 1 ? 1.2 : y == 2 ? 0.5 : 0);
            if (hw < 0.8) continue;
            for (int x = 0; x < TailW; x++)
            {
                double t = (x + 0.5 - cx) / hw; if (Math.Abs(t) > 1) continue;
                body.Set(x, y, Scale(t, x, y));
            }
        }
        Color[] snap = (Color[])body.P.Clone();
        for (int y = 1; y < TailH; y++) for (int x = 0; x < TailW; x++)
        {
            if (snap[y * TailW + x].A == 0) continue;
            bool edge = x == 0 || snap[y * TailW + x - 1].A == 0 || x == TailW - 1 || snap[y * TailW + x + 1].A == 0 || y == TailH - 1 || snap[(y + 1) * TailW + x].A == 0;
            if (edge) body.Set(x, y, Outline);
        }
        result.Over(body, 0, 0);
        return result;
    }

    // ---- Proyectil de veneno (estilo 2x) ----
    static Canvas Venom()
    {
        Color dark = Color.FromArgb(255, 0x2E, 0x6B, 0x10), mid = Color.FromArgb(255, 0x62, 0xC0, 0x1E), light = Color.FromArgb(255, 0xA8, 0xEF, 0x3C), hi = Color.FromArgb(255, 0xEA, 0xFF, 0xB4), ol = Color.FromArgb(255, 0x1A, 0x3A, 0x0A);
        Canvas cv = new Canvas(12, 8);
        for (int y = 0; y < 8; y++) for (int x = 0; x < 12; x++)
        {
            double dx = x + 0.5 - 7.5, dy = y + 0.5 - 4;
            bool blob = dx * dx + dy * dy <= 3.6 * 3.6;
            bool tail = x >= 1 && x < 7 && Math.Abs(dy) <= (x - 1) * 0.45 + 0.3;
            if (!blob && !tail) continue;
            Color c = mid;
            if (dx + dy > 1.5) c = dark;
            if (dx + dy < -1.8) c = light;
            cv.Set(x, y, c);
        }
        cv.OutlinePass(ol);
        cv.Set(6, 2, hi); cv.Set(7, 2, hi); cv.Set(6, 3, light);
        return cv;
    }

    // ---- Icono del debuff Veneno de Serpiente: una gota verde (estilo 2x, 16x16 -> 32x32) ----
    static Canvas VenomBuffIcon()
    {
        Color ol = Color.FromArgb(255, 0x1A, 0x3A, 0x0A), dk = Color.FromArgb(255, 0x2E, 0x6B, 0x10), md = Color.FromArgb(255, 0x62, 0xC0, 0x1E), lt = Color.FromArgb(255, 0xA8, 0xEF, 0x3C), hi = Color.FromArgb(255, 0xEA, 0xFF, 0xB4);
        Canvas cv = new Canvas(16, 16);
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
        {
            double dx = x + 0.5 - 8, dy = y + 0.5 - 10;
            bool bulb = dx * dx + dy * dy <= 5.2 * 5.2;
            bool tip = y >= 1 && y < 10 && Math.Abs(dx) <= (y - 1) * 0.55;
            if (!bulb && !tip) continue;
            double l = -dx * 0.5 - dy * 0.35;
            cv.Set(x, y, l > 1.2 ? lt : l > -1.6 ? md : dk);
        }
        cv.OutlinePass(ol);
        cv.Set(6, 8, hi); cv.Set(6, 9, hi); cv.Set(7, 7, hi); cv.Set(9, 12, dk); cv.Set(10, 11, dk);
        return cv;
    }

    // ---- Iconos reducidos a partir de la imagen original ----
    static void SaveResized(Bitmap src, Rectangle crop, int size, string path, Color? bg)
    {
        using (Bitmap dst = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(dst))
        {
            if (bg.HasValue) g.Clear(bg.Value); else g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(src, new Rectangle(0, 0, size, size), crop, GraphicsUnit.Pixel);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            dst.Save(path, ImageFormat.Png);
        }
        log.AppendLine("saved " + path + " (" + size + "x" + size + ")");
    }

    // ---- Preview: simula la cadena igual que el mod ----
    static PointF Rot(PointF v, double r) { double c = Math.Cos(r), s = Math.Sin(r); return new PointF((float)(v.X * c - v.Y * s), (float)(v.X * s + v.Y * c)); }
    static void DrawSprite(Graphics g, Bitmap bmp, Rectangle srcRect, PointF pos, PointF pivot, double rot, bool flip)
    {
        g.ResetTransform();
        g.TranslateTransform(pos.X, pos.Y);
        g.RotateTransform((float)(rot * 180 / Math.PI));
        if (flip) g.ScaleTransform(-1, 1);
        g.DrawImage(bmp, new RectangleF(-pivot.X, -pivot.Y, srcRect.Width, srcRect.Height), srcRect, GraphicsUnit.Pixel);
    }

    static void Preview(Bitmap head, Bitmap body, Bitmap tail, string path, int mode)
    {
        int bodies = 14;
        PointF h = mode == 1 ? new PointF(150, 300) : mode == 2 ? new PointF(500, 600) : mode == 3 ? new PointF(700, 250) : new PointF(850, 300);
        int facing = mode == 1 ? 1 : -1; double headRot = 0;
        PointF[] seg = new PointF[bodies + 1]; double[] rot = new double[bodies + 1];
        for (int i = 0; i <= bodies; i++) seg[i] = new PointF(h.X, h.Y + 40 + i * Spacing);
        for (int t = 0; t < 140; t++)
        {
            PointF vel;
            if (mode == 0) vel = new PointF(-5f, (float)(5 * Math.Sin(t * 0.07)));
            else if (mode == 1) vel = new PointF(5f, (float)(5 * Math.Sin(t * 0.07)));
            else if (mode == 2) vel = new PointF((float)(6 * Math.Sin(t * 0.05)), -3.5f);
            else { double a = t < 60 ? Math.PI : Math.PI - (t - 60) * 0.06; vel = new PointF((float)(5 * Math.Cos(a)), (float)(5 * Math.Sin(a))); }
            h = new PointF(h.X + vel.X, h.Y + vel.Y);
            if (vel.X > 0.5f) facing = 1; else if (vel.X < -0.5f) facing = -1;
            double ang = Math.Atan2(vel.Y, vel.X);
            headRot = facing == 1 ? ang : ang + Math.PI;
            PointF nOff = new PointF(facing == 1 ? -BodyAnchorOffset.X : BodyAnchorOffset.X, BodyAnchorOffset.Y);
            PointF r = Rot(nOff, headRot); PointF neck = new PointF(h.X + r.X, h.Y + r.Y);
            PointF nd = Rot(new PointF(facing == 1 ? -BodyAnchorDirection.X : BodyAnchorDirection.X, BodyAnchorDirection.Y), headRot);
            double refAng = Math.Atan2(-nd.Y, -nd.X);
            PointF ahead = neck; float gap = BodyAnchorGap;
            for (int i = 0; i <= bodies; i++)
            {
                float ax = ahead.X - seg[i].X, ay = ahead.Y - seg[i].Y;
                double a = (ax * ax + ay * ay) < 0.0001 ? refAng : Math.Atan2(ay, ax);
                double diff = Math.IEEERemainder(a - refAng, Math.PI * 2);
                if (diff > MaxBend) diff = MaxBend; if (diff < -MaxBend) diff = -MaxBend;
                a = refAng + diff;
                seg[i] = new PointF((float)(ahead.X - Math.Cos(a) * gap), (float)(ahead.Y - Math.Sin(a) * gap));
                rot[i] = a + Math.PI / 2;
                ahead = seg[i]; refAng = a; gap = Spacing;
            }
        }
        using (Bitmap outB = new Bitmap(1000, 650, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(outB))
        {
            g.Clear(Color.FromArgb(255, 40, 60, 90));
            g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.Half;
            DrawSprite(g, tail, new Rectangle(0, 0, TailW, TailH), seg[bodies], TailPivot, rot[bodies], false);
            for (int i = bodies - 1; i >= 0; i--)
            {
                int frame = (i + 1) % 3 == 0 ? 1 : 0;
                DrawSprite(g, body, new Rectangle(0, frame * BodyH, BodyW, BodyH), seg[i], BodyPivot, rot[i], false);
            }
            PointF pv = HeadPivot;
            DrawSprite(g, head, new Rectangle(0, 0, head.Width, head.Height), h, pv, headRot, facing == 1);
            g.ResetTransform();
            g.FillEllipse(Brushes.Magenta, h.X - 3, h.Y - 3, 6, 6);
            outB.Save(path, ImageFormat.Png);
        }
        log.AppendLine("preview " + path);
    }

    public static string Run(string srcPath, string modDir, string previewDir)
    {
        Bitmap src = new Bitmap(srcPath);
        ExtractPalette(src);
        Canvas head = MakeHead(src);
        head.Save(Path.Combine(modDir, @"Content\NPCs\SerpentHead.png"), 1);

        Canvas sheet = new Canvas(BodyW, BodyH * 2);
        sheet.Over(BodyFrame(false), 0, 0);
        sheet.Over(BodyFrame(true), 0, BodyH);
        sheet.Save(Path.Combine(modDir, @"Content\NPCs\SerpentBody.png"), 1);
        Tail().Save(Path.Combine(modDir, @"Content\NPCs\SerpentTail.png"), 1);

        Venom().Save(Path.Combine(modDir, @"Content\Projectiles\VenomSpit.png"), 2);
        VenomBuffIcon().Save(Path.Combine(modDir, @"Content\Buffs\SerpentVenom.png"), 2);
        SaveResized(src, new Rectangle(0, 0, 256, 256), 80, Path.Combine(modDir, "icon.png"), Color.FromArgb(255, 0x1B, 0x2A, 0x1C));
        // SerpentEgg, SerpentFeather, SerpentForm y SerpentHead_Head_Boss estan dibujados a mano: no se generan.

        using (Bitmap hb = new Bitmap(Path.Combine(modDir, @"Content\NPCs\SerpentHead.png")))
        using (Bitmap bb = new Bitmap(Path.Combine(modDir, @"Content\NPCs\SerpentBody.png")))
        using (Bitmap tb = new Bitmap(Path.Combine(modDir, @"Content\NPCs\SerpentTail.png")))
        {
            Preview(hb, bb, tb, Path.Combine(previewDir, "preview_left.png"), 0);
            Preview(hb, bb, tb, Path.Combine(previewDir, "preview_right.png"), 1);
            Preview(hb, bb, tb, Path.Combine(previewDir, "preview_up.png"), 2);
            Preview(hb, bb, tb, Path.Combine(previewDir, "preview_turn.png"), 3);
        }
        src.Dispose();
        return log.ToString();
    }
}

