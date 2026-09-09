using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PlakaTanimaliOtoparkOtomasyonu.Services;

public class PlakaOnIslemeServisi
{
    public List<PlakaIslenmisGoruntu> OnIsle(Image kaynak, Rectangle? roi = null)
    {
        using var kirpilmis = roi.HasValue ? Kirp(kaynak, roi.Value) : BitmapHazirla(kaynak);
        using var buyutulmus = YenidenBoyutlandir(kirpilmis);

        var gri = BitmaptenGriDiziAl(buyutulmus, out var width, out var height);
        var kontrast = KontrastArtir(gri);
        var blur = GaussianBlur(kontrast, width, height);
        var global = Threshold(blur);
        var adaptive = AdaptiveThreshold(blur, width, height);
        var globalMorfoloji = MorfolojikKapatma(global, width, height);
        var adaptiveMorfoloji = MorfolojikKapatma(adaptive, width, height);

        return new List<PlakaIslenmisGoruntu>
        {
            new("GlobalThreshold", GriDizidenBitmap(globalMorfoloji, width, height)),
            new("AdaptiveThreshold", GriDizidenBitmap(adaptiveMorfoloji, width, height))
        };
    }

    public Bitmap RoiKirp(Image kaynak)
    {
        return Kirp(kaynak, KameraServisi.PlakaCercevesiHesapla(kaynak.Size));
    }

    private static Bitmap Kirp(Image kaynak, Rectangle roi)
    {
        var sinir = new Rectangle(0, 0, kaynak.Width, kaynak.Height);
        var guvenliRoi = Rectangle.Intersect(sinir, roi);
        if (guvenliRoi.Width <= 0 || guvenliRoi.Height <= 0)
        {
            guvenliRoi = sinir;
        }

        var hedef = new Bitmap(guvenliRoi.Width, guvenliRoi.Height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(hedef);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(kaynak, new Rectangle(0, 0, hedef.Width, hedef.Height), guvenliRoi, GraphicsUnit.Pixel);
        return hedef;
    }

    private static Bitmap BitmapHazirla(Image kaynak)
    {
        var hedef = new Bitmap(kaynak.Width, kaynak.Height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(hedef);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(kaynak, new Rectangle(0, 0, hedef.Width, hedef.Height));
        return hedef;
    }

    private static Bitmap YenidenBoyutlandir(Image kaynak)
    {
        const double scale = 3.0;
        var width = Math.Max(1, (int)(kaynak.Width * scale));
        var height = Math.Max(1, (int)(kaynak.Height * scale));
        var hedef = new Bitmap(width, height, PixelFormat.Format24bppRgb);

        using var graphics = Graphics.FromImage(hedef);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.DrawImage(kaynak, new Rectangle(0, 0, width, height));
        return hedef;
    }

    private static byte[] BitmaptenGriDiziAl(Bitmap bitmap, out int width, out int height)
    {
        width = bitmap.Width;
        height = bitmap.Height;
        var rect = new Rectangle(0, 0, width, height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

        try
        {
            var bytes = new byte[Math.Abs(data.Stride) * height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            var gray = new byte[width * height];

            for (var y = 0; y < height; y++)
            {
                var row = y * data.Stride;
                for (var x = 0; x < width; x++)
                {
                    var index = row + x * 3;
                    var b = bytes[index];
                    var g = bytes[index + 1];
                    var r = bytes[index + 2];
                    gray[y * width + x] = (byte)Math.Clamp((int)(r * 0.299 + g * 0.587 + b * 0.114), 0, 255);
                }
            }

            return gray;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static byte[] KontrastArtir(byte[] gray)
    {
        var output = new byte[gray.Length];
        const double factor = 1.65;

        for (var i = 0; i < gray.Length; i++)
        {
            output[i] = (byte)Math.Clamp((int)((gray[i] - 128) * factor + 128), 0, 255);
        }

        return output;
    }

    private static byte[] GaussianBlur(byte[] gray, int width, int height)
    {
        var output = new byte[gray.Length];
        int[] kernel = { 1, 2, 1, 2, 4, 2, 1, 2, 1 };

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var toplam = 0;
                var agirlik = 0;

                for (var ky = -1; ky <= 1; ky++)
                {
                    var py = Math.Clamp(y + ky, 0, height - 1);
                    for (var kx = -1; kx <= 1; kx++)
                    {
                        var px = Math.Clamp(x + kx, 0, width - 1);
                        var k = kernel[(ky + 1) * 3 + (kx + 1)];
                        toplam += gray[py * width + px] * k;
                        agirlik += k;
                    }
                }

                output[y * width + x] = (byte)(toplam / agirlik);
            }
        }

        return output;
    }

    private static byte[] Threshold(byte[] gray)
    {
        var threshold = OtsuThreshold(gray);
        var output = new byte[gray.Length];

        for (var i = 0; i < gray.Length; i++)
        {
            output[i] = gray[i] >= threshold ? (byte)255 : (byte)0;
        }

        return output;
    }

    private static byte[] AdaptiveThreshold(byte[] gray, int width, int height)
    {
        var output = new byte[gray.Length];
        var yaricap = 14;
        var integral = new long[(width + 1) * (height + 1)];

        for (var y = 1; y <= height; y++)
        {
            long rowSum = 0;
            for (var x = 1; x <= width; x++)
            {
                rowSum += gray[(y - 1) * width + (x - 1)];
                integral[y * (width + 1) + x] = integral[(y - 1) * (width + 1) + x] + rowSum;
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var x1 = Math.Max(0, x - yaricap);
                var y1 = Math.Max(0, y - yaricap);
                var x2 = Math.Min(width - 1, x + yaricap);
                var y2 = Math.Min(height - 1, y + yaricap);
                var count = (x2 - x1 + 1) * (y2 - y1 + 1);
                var sum = IntegralToplam(integral, width, x1, y1, x2, y2);
                var mean = sum / count;
                output[y * width + x] = gray[y * width + x] > mean - 8 ? (byte)255 : (byte)0;
            }
        }

        return output;
    }

    private static long IntegralToplam(long[] integral, int width, int x1, int y1, int x2, int y2)
    {
        var stride = width + 1;
        var ax = x1;
        var ay = y1;
        var bx = x2 + 1;
        var by = y2 + 1;
        return integral[by * stride + bx] - integral[ay * stride + bx] - integral[by * stride + ax] + integral[ay * stride + ax];
    }

    private static int OtsuThreshold(byte[] gray)
    {
        var histogram = new int[256];
        foreach (var value in gray)
        {
            histogram[value]++;
        }

        var total = gray.Length;
        var sum = 0D;
        for (var i = 0; i < 256; i++)
        {
            sum += i * histogram[i];
        }

        var sumB = 0D;
        var wB = 0;
        var maxVariance = 0D;
        var threshold = 128;

        for (var i = 0; i < 256; i++)
        {
            wB += histogram[i];
            if (wB == 0)
            {
                continue;
            }

            var wF = total - wB;
            if (wF == 0)
            {
                break;
            }

            sumB += i * histogram[i];
            var mB = sumB / wB;
            var mF = (sum - sumB) / wF;
            var variance = wB * wF * Math.Pow(mB - mF, 2);

            if (variance > maxVariance)
            {
                maxVariance = variance;
                threshold = i;
            }
        }

        return threshold;
    }

    private static byte[] MorfolojikKapatma(byte[] binary, int width, int height)
    {
        return Erode(Dilate(binary, width, height), width, height);
    }

    private static byte[] Dilate(byte[] binary, int width, int height)
    {
        var output = new byte[binary.Length];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var max = 0;
                for (var ky = -1; ky <= 1; ky++)
                {
                    var py = Math.Clamp(y + ky, 0, height - 1);
                    for (var kx = -1; kx <= 1; kx++)
                    {
                        var px = Math.Clamp(x + kx, 0, width - 1);
                        max = Math.Max(max, binary[py * width + px]);
                    }
                }

                output[y * width + x] = (byte)max;
            }
        }

        return output;
    }

    private static byte[] Erode(byte[] binary, int width, int height)
    {
        var output = new byte[binary.Length];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var min = 255;
                for (var ky = -1; ky <= 1; ky++)
                {
                    var py = Math.Clamp(y + ky, 0, height - 1);
                    for (var kx = -1; kx <= 1; kx++)
                    {
                        var px = Math.Clamp(x + kx, 0, width - 1);
                        min = Math.Min(min, binary[py * width + px]);
                    }
                }

                output[y * width + x] = (byte)min;
            }
        }

        return output;
    }

    private static Bitmap GriDizidenBitmap(byte[] gray, int width, int height)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        var rect = new Rectangle(0, 0, width, height);
        var data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

        try
        {
            var bytes = new byte[Math.Abs(data.Stride) * height];
            for (var y = 0; y < height; y++)
            {
                var row = y * data.Stride;
                for (var x = 0; x < width; x++)
                {
                    var value = gray[y * width + x];
                    var index = row + x * 3;
                    bytes[index] = value;
                    bytes[index + 1] = value;
                    bytes[index + 2] = value;
                }
            }

            Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return bitmap;
    }
}

public sealed class PlakaIslenmisGoruntu : IDisposable
{
    public PlakaIslenmisGoruntu(string tur, Bitmap goruntu)
    {
        Tur = tur;
        Goruntu = goruntu;
    }

    public string Tur { get; }
    public Bitmap Goruntu { get; }

    public void Dispose()
    {
        Goruntu.Dispose();
    }
}
