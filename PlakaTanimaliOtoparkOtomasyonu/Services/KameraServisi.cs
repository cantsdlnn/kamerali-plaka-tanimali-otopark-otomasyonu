using System.Reflection;
using System.Drawing.Drawing2D;
using System.Globalization;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;

namespace PlakaTanimaliOtoparkOtomasyonu.Services;

public class KameraServisi : IDisposable
{
    private static readonly RectangleF VarsayilanPlakaCerceveOrani = new(0.14F, 0.39F, 0.72F, 0.22F);
    private static RectangleF _plakaCerceveOrani = VarsayilanPlakaCerceveOrani;

    private object? _videoCapture;
    private object? _frame;
    private System.Windows.Forms.Timer? _timer;
    private PictureBox? _preview;
    private Bitmap? _sonGoruntu;
    private Type? _videoCaptureType;
    private Type? _matType;
    private readonly object _kilit = new();

    public bool Calisiyor => _videoCapture is not null && IsOpened();

    public static RectangleF PlakaCerceveOrani => _plakaCerceveOrani;

    public static void PlakaCercevesiAyarla(float x, float y, float width, float height)
    {
        width = Math.Clamp(width, 0.25F, 0.95F);
        height = Math.Clamp(height, 0.10F, 0.60F);
        x = Math.Clamp(x, 0F, 1F - width);
        y = Math.Clamp(y, 0F, 1F - height);
        _plakaCerceveOrani = new RectangleF(x, y, width, height);
    }

    public static void PlakaCercevesiAyarla(RectangleF oran)
    {
        PlakaCercevesiAyarla(oran.X, oran.Y, oran.Width, oran.Height);
    }

    public static void PlakaCercevesiSifirla()
    {
        _plakaCerceveOrani = VarsayilanPlakaCerceveOrani;
    }

    public static void PlakaCercevesiYukle()
    {
        try
        {
            var path = RoiAyarDosyasiYolu();
            if (!File.Exists(path))
            {
                return;
            }

            var parts = File.ReadAllText(path)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length != 4)
            {
                return;
            }

            var values = parts
                .Select(p => float.Parse(p, CultureInfo.InvariantCulture))
                .ToArray();

            PlakaCercevesiAyarla(values[0], values[1], values[2], values[3]);
        }
        catch
        {
            _plakaCerceveOrani = VarsayilanPlakaCerceveOrani;
        }
    }

    public static void PlakaCercevesiKaydet()
    {
        var path = RoiAyarDosyasiYolu();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var oran = _plakaCerceveOrani;
        var text = string.Join(";",
            oran.X.ToString(CultureInfo.InvariantCulture),
            oran.Y.ToString(CultureInfo.InvariantCulture),
            oran.Width.ToString(CultureInfo.InvariantCulture),
            oran.Height.ToString(CultureInfo.InvariantCulture));
        File.WriteAllText(path, text);
    }

    public static void PlakaCercevesiKaydet(RectangleF oran)
    {
        PlakaCercevesiAyarla(oran);
        PlakaCercevesiKaydet();
    }

    public static bool PictureBoxNoktasiniGoruntuOraninaCevir(PictureBox pictureBox, Point point, out PointF oran)
    {
        oran = PointF.Empty;
        if (pictureBox.Image is null)
        {
            return false;
        }

        var alan = ZoomGoruntuAlaniHesapla(pictureBox);
        if (!alan.Contains(point))
        {
            return false;
        }

        var x = (point.X - alan.X) / alan.Width;
        var y = (point.Y - alan.Y) / alan.Height;
        oran = new PointF(Math.Clamp(x, 0F, 1F), Math.Clamp(y, 0F, 1F));
        return true;
    }

    public static RectangleF OranlardanDikdortgen(PointF start, PointF end)
    {
        var x = Math.Min(start.X, end.X);
        var y = Math.Min(start.Y, end.Y);
        var width = Math.Abs(start.X - end.X);
        var height = Math.Abs(start.Y - end.Y);
        return new RectangleF(x, y, width, height);
    }

    public static RectangleF OranDikdortgeniniPictureBoxDikdortgenineCevir(PictureBox pictureBox, RectangleF oran)
    {
        var alan = ZoomGoruntuAlaniHesapla(pictureBox);
        return new RectangleF(
            alan.X + alan.Width * oran.X,
            alan.Y + alan.Height * oran.Y,
            alan.Width * oran.Width,
            alan.Height * oran.Height);
    }

    public void Baslat(PictureBox preview)
    {
        Durdur();

        var kameraIndexText = AppConfigYardimcisi.AyarAl("CameraIndex", "0");
        _ = int.TryParse(kameraIndexText, out var kameraIndex);

        _preview = preview;
        var openCv = Assembly.Load("OpenCvSharp");
        _videoCaptureType = openCv.GetType("OpenCvSharp.VideoCapture")
            ?? throw new InvalidOperationException("OpenCvSharp.VideoCapture tipi bulunamadi.");
        _matType = openCv.GetType("OpenCvSharp.Mat")
            ?? throw new InvalidOperationException("OpenCvSharp.Mat tipi bulunamadi.");

        _frame = Activator.CreateInstance(_matType);
        _videoCapture = VideoCaptureOlustur(openCv, kameraIndex);

        if (!IsOpened())
        {
            Durdur();
            throw new InvalidOperationException("Kamera acilamadi. Kamera baglantisini veya CameraIndex ayarini kontrol edin.");
        }

        _timer = new System.Windows.Forms.Timer { Interval = 33 };
        _timer.Tick += (_, _) => KareOku();
        _timer.Start();
    }

    public void Durdur()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;

        InvokeIfExists(_videoCapture, "Release");
        (_videoCapture as IDisposable)?.Dispose();
        _videoCapture = null;

        (_frame as IDisposable)?.Dispose();
        _frame = null;
    }

    public Image? FotoCek()
    {
        lock (_kilit)
        {
            return _sonGoruntu is null ? null : new Bitmap(_sonGoruntu);
        }
    }

    public async Task<List<Image>> KareleriYakalaAsync(int kareSayisi = 15, int sureMilisaniye = 2000, CancellationToken cancellationToken = default)
    {
        var kareler = new List<Image>();
        var aralik = Math.Max(50, sureMilisaniye / Math.Max(kareSayisi, 1));

        for (var i = 0; i < kareSayisi; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var kare = FotoCek();
            if (kare is not null)
            {
                kareler.Add(kare);
            }

            if (i < kareSayisi - 1)
            {
                await Task.Delay(aralik, cancellationToken);
            }
        }

        return kareler;
    }

    public static Rectangle PlakaCercevesiHesapla(Size goruntuBoyutu)
    {
        var oran = _plakaCerceveOrani;
        return new Rectangle(
            (int)(goruntuBoyutu.Width * oran.X),
            (int)(goruntuBoyutu.Height * oran.Y),
            (int)(goruntuBoyutu.Width * oran.Width),
            (int)(goruntuBoyutu.Height * oran.Height));
    }

    public static void PlakaCercevesiCiz(PictureBox pictureBox, Graphics graphics)
    {
        PlakaCercevesiCiz(pictureBox, graphics, null, false);
    }

    public static void PlakaCercevesiCiz(PictureBox pictureBox, Graphics graphics, RectangleF? geciciOran, bool secimAktif)
    {
        if (pictureBox.Image is null || pictureBox.ClientSize.Width <= 0 || pictureBox.ClientSize.Height <= 0)
        {
            return;
        }

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var oran = geciciOran ?? _plakaCerceveOrani;
        var goruntuAlani = ZoomGoruntuAlaniHesapla(pictureBox);
        var cerceve = new RectangleF(
            goruntuAlani.X + goruntuAlani.Width * oran.X,
            goruntuAlani.Y + goruntuAlani.Height * oran.Y,
            goruntuAlani.Width * oran.Width,
            goruntuAlani.Height * oran.Height);

        using var arkaPlan = new SolidBrush(Color.FromArgb(65, 0, 0, 0));
        using var kalem = new Pen(secimAktif ? Color.FromArgb(96, 165, 250) : Color.FromArgb(252, 211, 77), 3F) { DashStyle = DashStyle.Dash };
        using var icKalem = new Pen(Color.FromArgb(34, 197, 94), 1.5F);
        using var font = new Font("Segoe UI Semibold", 9F);

        graphics.FillRectangle(arkaPlan, goruntuAlani.X, goruntuAlani.Y, goruntuAlani.Width, cerceve.Y - goruntuAlani.Y);
        graphics.FillRectangle(arkaPlan, goruntuAlani.X, cerceve.Bottom, goruntuAlani.Width, goruntuAlani.Bottom - cerceve.Bottom);
        graphics.FillRectangle(arkaPlan, goruntuAlani.X, cerceve.Y, cerceve.X - goruntuAlani.X, cerceve.Height);
        graphics.FillRectangle(arkaPlan, cerceve.Right, cerceve.Y, goruntuAlani.Right - cerceve.Right, cerceve.Height);
        graphics.DrawRectangle(kalem, cerceve.X, cerceve.Y, cerceve.Width, cerceve.Height);
        graphics.DrawRectangle(icKalem, cerceve.X + 5, cerceve.Y + 5, cerceve.Width - 10, cerceve.Height - 10);

        var yaziAlani = new RectangleF(cerceve.X, cerceve.Bottom + 8, cerceve.Width, 24);
        var yazi = secimAktif ? "ROI secimi aktif: mouse ile alan cizin" : "Plakayi bu cerceveye hizalayin";
        TextRenderer.DrawText(graphics, yazi, font,
            Rectangle.Round(yaziAlani), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private void KareOku()
    {
        if (_videoCapture is null || _frame is null || _preview is null)
        {
            return;
        }

        if (!ReadFrame() || FrameEmpty())
        {
            return;
        }

        var bitmap = MatToBitmap(_frame);
        var eskiPreview = _preview.Image;
        _preview.Image = bitmap;
        eskiPreview?.Dispose();

        lock (_kilit)
        {
            _sonGoruntu?.Dispose();
            _sonGoruntu = new Bitmap(bitmap);
        }

        _preview.Invalidate();
    }

    private bool IsOpened()
    {
        var method = _videoCaptureType?.GetMethod("IsOpened", Type.EmptyTypes);
        return method?.Invoke(_videoCapture, null) is bool result && result;
    }

    private object VideoCaptureOlustur(Assembly openCv, int kameraIndex)
    {
        var apiEnumType = openCv.GetType("OpenCvSharp.VideoCaptureAPIs");
        var anyApi = ApiDegeriAl(apiEnumType);

        var parametresiz = _videoCaptureType?.GetConstructor(Type.EmptyTypes);
        if (parametresiz is not null)
        {
            var capture = parametresiz.Invoke(null);
            if (KamerayiAc(capture, kameraIndex, apiEnumType, anyApi))
            {
                return capture;
            }

            (capture as IDisposable)?.Dispose();
        }

        var intConstructor = _videoCaptureType?.GetConstructor(new[] { typeof(int) });
        if (intConstructor is not null)
        {
            return intConstructor.Invoke(new object[] { kameraIndex });
        }

        if (apiEnumType is not null && anyApi is not null)
        {
            var intApiConstructor = _videoCaptureType?.GetConstructor(new[] { typeof(int), apiEnumType });
            if (intApiConstructor is not null)
            {
                return intApiConstructor.Invoke(new[] { (object)kameraIndex, anyApi });
            }
        }

        throw new InvalidOperationException("OpenCvSharp.VideoCapture icin desteklenen kamera acma metodu bulunamadi.");
    }

    private bool KamerayiAc(object capture, int kameraIndex, Type? apiEnumType, object? anyApi)
    {
        var openInt = _videoCaptureType?.GetMethod("Open", new[] { typeof(int) });
        if (openInt?.Invoke(capture, new object[] { kameraIndex }) is bool opened && opened)
        {
            return true;
        }

        if (apiEnumType is not null && anyApi is not null)
        {
            var openIntApi = _videoCaptureType?.GetMethod("Open", new[] { typeof(int), apiEnumType });
            if (openIntApi?.Invoke(capture, new[] { (object)kameraIndex, anyApi }) is bool openedWithApi && openedWithApi)
            {
                return true;
            }
        }

        return false;
    }

    private static object? ApiDegeriAl(Type? apiEnumType)
    {
        if (apiEnumType is null)
        {
            return null;
        }

        foreach (var name in new[] { "ANY", "Any", "CAP_ANY" })
        {
            if (Enum.GetNames(apiEnumType).Contains(name))
            {
                return Enum.Parse(apiEnumType, name);
            }
        }

        return Enum.ToObject(apiEnumType, 0);
    }

    private bool ReadFrame()
    {
        var method = _videoCaptureType?.GetMethod("Read", new[] { _matType! });
        return method?.Invoke(_videoCapture, new[] { _frame }) is bool result && result;
    }

    private bool FrameEmpty()
    {
        var method = _matType?.GetMethod("Empty", Type.EmptyTypes);
        return method?.Invoke(_frame, null) is bool result && result;
    }

    private static Bitmap MatToBitmap(object mat)
    {
        var method = mat.GetType()
            .GetMethods()
            .FirstOrDefault(m => m.Name == "ToBytes" && m.GetParameters().Length >= 1 && m.GetParameters()[0].ParameterType == typeof(string));

        if (method is null)
        {
            throw new InvalidOperationException("OpenCvSharp Mat.ToBytes metodu bulunamadi.");
        }

        var parameters = method.GetParameters();
        object?[] args;
        if (parameters.Length == 1)
        {
            args = new object?[] { ".bmp" };
        }
        else
        {
            var arrayType = parameters[1].ParameterType.GetElementType() ?? typeof(object);
            args = new object?[] { ".bmp", Array.CreateInstance(arrayType, 0) };
        }

        var bytes = method.Invoke(mat, args) as byte[]
            ?? throw new InvalidOperationException("OpenCvSharp goruntu byte dizisi uretmedi.");

        using var stream = new MemoryStream(bytes);
        return new Bitmap(stream);
    }

    private static void InvokeIfExists(object? target, string methodName)
    {
        target?.GetType().GetMethod(methodName, Type.EmptyTypes)?.Invoke(target, null);
    }

    private static string RoiAyarDosyasiYolu()
    {
        var klasor = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PlakaTanimaliOtoparkOtomasyonu");
        return Path.Combine(klasor, "roi.settings");
    }

    private static RectangleF ZoomGoruntuAlaniHesapla(PictureBox pictureBox)
    {
        var image = pictureBox.Image!;
        var boxWidth = pictureBox.ClientSize.Width;
        var boxHeight = pictureBox.ClientSize.Height;
        var imageRatio = (float)image.Width / image.Height;
        var boxRatio = (float)boxWidth / boxHeight;

        if (imageRatio > boxRatio)
        {
            var height = boxWidth / imageRatio;
            return new RectangleF(0, (boxHeight - height) / 2F, boxWidth, height);
        }

        var width = boxHeight * imageRatio;
        return new RectangleF((boxWidth - width) / 2F, 0, width, boxHeight);
    }

    public void Dispose()
    {
        Durdur();
        lock (_kilit)
        {
            _sonGoruntu?.Dispose();
            _sonGoruntu = null;
        }
    }
}
