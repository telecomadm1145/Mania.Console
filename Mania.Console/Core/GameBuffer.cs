using System.Text;
using Color = System.ValueTuple<byte, byte, byte>;

namespace Osu.Console.Core
{
    public class GameBuffer
    {
        private (char Char, (Color bg, Color fg) Color)[][] Buffer = Array.Empty<(char Char, (Color bg, Color fg) Color)[]>();
        public int Width { get; set; } = 0;
        public int Height { get; set; } = 0;
        public void InitConsole()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                NativeApi.EnableANSI();
#pragma warning disable CA1416 // 验证平台兼容性
                System.Console.BufferHeight = System.Console.WindowHeight;
                System.Console.BufferWidth = System.Console.WindowWidth;
#pragma warning restore CA1416 // 验证平台兼容性
            }
            System.Console.Write("\u001b[?25l");
            Width = System.Console.BufferWidth;
            Height = System.Console.BufferHeight;
        }
        public void ResizeBuffer()
        {
            Buffer = Enumerable.Range(0, Height).Select(x => new (char Char, (Color bg, Color fg) Color)[Width]).ToArray();
        }
        private StringBuilder i_buf = new();
        public async void PushFrame()
        {
            System.Console.CursorLeft = 0;
            System.Console.CursorTop = 0;
            int i = 0;
            Color? LastFg = null;
            Color? LastBg = null;
            foreach (var line in Buffer)
            {
                foreach (var pixel in line)
                {
                    if (pixel.Char != '\0')
                    {
                        if (LastFg != pixel.Color.fg)
                        {
                            i_buf.Append("\u001B[38;2;");
                            i_buf.Append(pixel.Color.fg.Item1);
                            i_buf.Append(';');
                            i_buf.Append(pixel.Color.fg.Item2);
                            i_buf.Append(';');
                            i_buf.Append(pixel.Color.fg.Item3);
                            i_buf.Append('m');
                            LastFg = pixel.Color.fg;
                        }
                        if (LastBg != pixel.Color.bg)
                        {
                            i_buf.Append("\u001b[48;2;");
                            i_buf.Append(pixel.Color.bg.Item1);
                            i_buf.Append(';');
                            i_buf.Append(pixel.Color.bg.Item2);
                            i_buf.Append(';');
                            i_buf.Append(pixel.Color.bg.Item3);
                            i_buf.Append('m');
                            LastBg = pixel.Color.bg;
                        }
                        i_buf.Append(pixel.Char);
                    }
                    else
                    {
                        if ((LastBg.HasValue && LastBg != default(Color)))
                        {
                            i_buf.Append("\u001b[48;2;0;0;0m");
                            LastBg = null;
                        }
                        i_buf.Append(' ');
                    }
                }
                i++;
                if (i != Buffer.Length)
                    i_buf.Append('\n');
            }
            await System.Console.Out.WriteAsync(i_buf.ToString());
            i_buf.Length = 0;
        }
        public void Clear()
        {
            foreach (var line in Buffer)
            {
                for (int i = 0; i < line.Length; i++)
                {
                    line[i] = default;
                }
            }
        }
        public const double Ratio = 1.7;
        public void DrawEplise(Func<char> fill, (Color bg, Color fg) clr, int x, int y, int a, int b)
        {
            int max = (int)(Math.Max(a, b) * Ratio);
            for (double i = -max; i < max; i++)
            {
                for (double j = -max; j < max; j++)
                {
                    if (Math.Abs(Math.Sqrt(i * i + j * j * Ratio * Ratio) - a) < 0.56) // 近似
                    {
                        TrySetPixel((fill(), clr), (int)(x + i), y + (int)j);
                    }
                }
            }
        }
        public void FillEplise(Func<char> fill, (Color bg, Color fg) clr, int x, int y, int a, int b)
        {
            int max = (int)(Math.Max(a, b) * Ratio);
            for (double i = -max; i < max; i++)
            {
                for (double j = -max; j < max; j++)
                {
                    if (Math.Sqrt(i * i + j * j * Ratio * Ratio) <= a) // 近似
                    {
                        TrySetPixel((fill(), clr), (int)(x + i), y + (int)j);
                    }
                }
            }
        }
        public void DrawLineHorizontal((char chr, (Color bg, Color fg)) data, int x, int y1, int y2)
        {
            if (y1 > y2)
                (y2, y1) = (y1, y2);
            for (int j = y1; j <= y2; j++)
            {
                TrySetPixel(data, x, j);
            }
        }
        public void DrawLineVertical((char chr, (Color bg, Color fg)) data, int x1, int x2,int y)
        {
            if (x1 > x2)
                (x2, x1) = (x1, x2);
            for (int i = x1; i <= x2; i++)
            {
                TrySetPixel(data, i, y);
            }
        }
        public void DrawLine((char chr,(Color bg,Color fg)) data, int x1, int y1, int x2, int y2)
        {
            if (y1 > y2)
                (y2, y1) = (y1, y2);
            if (x1 > x2)
                (x2, x1) = (x1, x2);
            var dx = x2 - x1;
            var dy = y2 - y1;
            if (dx == 0)
            {
                DrawLineHorizontal(data, x1, y1, y2);
                return;
            }
            if (dy == 0)
            {
                DrawLineVertical(data, x1, x2, y1);
                return;
            }
            var ratio = (double)dy / dx;
            var cons = y2 - x2 * ratio;
            for (double i = x1; i <= x2; i++)
            {
                for (double j = y1; j <= y2; j++)
                {
                    if(Math.Abs((i * ratio + cons) - j) <= 0.5)
                    {
                        TrySetPixel(data, (int)i, (int)j);
                    }
                }
            }
        }
        public void FillRect((char chr, (Color bg, Color fg)) data, (int left, int top, int right, int bottom) rect)
        {
            if (rect.left > rect.right)
                (rect.left,rect.right) = (rect.right,rect.left);
            for (int i = rect.left; i < rect.right; i++)
            {
                DrawLineHorizontal(data, i, rect.top, rect.bottom);
            }
        }
        public void DrawRect((char chr, (Color bg, Color fg)) data,(int left,int top,int right,int bottom) rect)
        {
            DrawLine(data,rect.left,rect.top,rect.right,rect.top);
            DrawLine(data, rect.right, rect.top, rect.right, rect.bottom);
            DrawLine(data, rect.left, rect.top, rect.left, rect.bottom);
            DrawLine(data, rect.left, rect.bottom, rect.right, rect.bottom);
        }
        public void DrawString(string str, Color clr, int x, int y)
        {
            int i = 0;
            foreach (char chr in str)
            {
                if (chr == '\n')
                {
                    y++;
                    i = 0;
                    continue;
                }
                var v = TryGetPixel(x + i, y);
                TrySetPixel((chr, (v?.Color.bg ?? new Color(), clr)), x + i, y);
                i++;
            }
        }
        public void DrawString(string str, (Color bg, Color fg) clr, int x, int y)
        {
            int i = 0;
            foreach (char chr in str)
            {
                if (chr == '\n')
                {
                    y++;
                    i = 0;
                    continue;
                }
                TrySetPixel((chr, clr), x + i, y);
                i++;
            }
        }
        public void SetPixel((char Char, (Color bg, Color fg) Color) data, int x, int y)
        {
            if (x < 0 || x > Width)
                throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y > Height)
                throw new ArgumentOutOfRangeException(nameof(y));
            if (data.Char > 127)
            {
                data.Char = '?';
            }
            Buffer[y][x] = data;
        }
        public bool TrySetPixel((char Char, (Color bg, Color fg) Color) data, int x, int y)
        {
            if (x < 0 || x >= Width)
                return false;
            if (y < 0 || y >= Height)
                return false;
            if (data.Char > 127)
            {
                data.Char = '?';
            }
            Buffer[y][x] = data;
            return true;
        }
        public (char Char, (Color bg, Color fg) Color) GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width)
                throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException(nameof(y));
            return Buffer[y][x];
        }
        public (char Char, (Color bg, Color fg) Color)? TryGetPixel(int x, int y)
        {
            if (x < 0 || x > Width)
                return null;
            if (y < 0 || y > Height)
                return null;
            return Buffer[y][x];
        }
    }
}
