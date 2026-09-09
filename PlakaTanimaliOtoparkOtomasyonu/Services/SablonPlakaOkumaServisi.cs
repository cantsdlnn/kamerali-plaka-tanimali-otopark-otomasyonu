using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;
using Tesseract;

namespace PlakaTanimaliOtoparkOtomasyonu.Services;

public class SablonPlakaOkumaServisi
{
    private const int NormalGenislik = 40;
    private const int NormalYukseklik = 60;
    private const int MaksimumIslemeGenisligi = 920;
    private const decimal MinimumSablonGuveni = 30M;
    private static readonly char[] Rakamlar = "0123456789".ToCharArray();
    private static readonly char[] Harfler = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] TumKarakterler = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    private readonly Dictionary<char, SablonKarakter> _sablonlar;

    public SablonPlakaOkumaServisi()
    {
        var sablonKlasoru = Path.Combine(AppContext.BaseDirectory, "Templates");
        SablonKlasorunuHazirla(sablonKlasoru);
        _sablonlar = SablonlariYukle(sablonKlasoru);
    }

    public SablonPlakaOkumaSonucu Oku(Image roiGoruntusu)
    {
        try
        {
            using var hazir = GoruntuyuHazirla(roiGoruntusu);
            var gri = BitmaptenGriDiziAl(hazir, out var width, out var height);
            var kontrast = KontrastArtir(gri);
            var binary = OtsuIleIkiliyeCevir(kontrast, width, height);
            binary = GerekirseTersCevir(binary);
            binary = UzunCizgileriTemizle(binary, width, height);

            var projeksiyonKutulari = ProjeksiyonIleKarakterKutulariniBul(binary, width, height);
            var konturKutulari = KarakterKutulariniBul(binary, width, height);
            var segmentasyonTuru = projeksiyonKutulari.Count is >= 5 and <= 12
                ? "Projeksiyon"
                : SegmentasyonSkoru(projeksiyonKutulari) >= SegmentasyonSkoru(konturKutulari)
                    ? "Projeksiyon"
                    : "Kontur";
            var kutular = segmentasyonTuru == "Projeksiyon" ? projeksiyonKutulari : konturKutulari;
            if (kutular.Count == 0)
            {
                return SablonPlakaOkumaSonucu.Basarisiz("Sablon okuma icin karakter segmenti bulunamadi.");
            }

            var tekKarakterOcrAdayi = new[]
                {
                    ("Projeksiyon", projeksiyonKutulari),
                    ("Kontur", konturKutulari)
                }
                .Where(secenek => secenek.Item2.Count > 0)
                .Select(secenek => new
                {
                    Tur = secenek.Item1,
                    Karakterler = secenek.Item2
                        .OrderBy(k => k.Kutu.X)
                        .Select(k => new SegmentliKarakter(k, NormalizeEt(binary, width, height, k.Kutu)))
                        .ToList()
                })
                .Select(secenek => new
                {
                    secenek.Tur,
                    secenek.Karakterler,
                    Aday = TekKarakterOcrIleOku(secenek.Karakterler)
                })
                .Where(secenek => secenek.Aday is not null)
                .OrderByDescending(secenek => PlakaDogrulamaYardimcisi.TurkPlakasiMi(secenek.Aday!.DuzeltilmisPlaka))
                .ThenByDescending(secenek => secenek.Aday!.GuvenOrani + secenek.Aday.OkunanMetin.Length * 2M)
                .FirstOrDefault();

            if (tekKarakterOcrAdayi?.Aday is { } tekKarakterAday &&
                PlakaDogrulamaYardimcisi.TurkPlakasiMi(tekKarakterAday.DuzeltilmisPlaka))
            {
                return new SablonPlakaOkumaSonucu
                {
                    BasariliMi = tekKarakterAday.GuvenOrani >= MinimumSablonGuveni,
                    OkunanMetin = tekKarakterAday.OkunanMetin,
                    DuzeltilmisPlaka = tekKarakterAday.DuzeltilmisPlaka,
                    GuvenOrani = tekKarakterAday.GuvenOrani,
                    KarakterSayisi = tekKarakterOcrAdayi.Karakterler.Count,
                    ProjeksiyonKarakterSayisi = projeksiyonKutulari.Count,
                    KonturKarakterSayisi = konturKutulari.Count,
                    SegmentasyonTuru = $"{tekKarakterOcrAdayi.Tur}+TekKarakterOCR",
                    Aciklama = "Segmentasyon sonrasi karakterler beklenen tipe gore tek tek okundu."
                };
            }

            var karakterler = kutular
                .OrderBy(k => k.Kutu.X)
                .Select(k => new SegmentliKarakter(k, NormalizeEt(binary, width, height, k.Kutu)))
                .ToList();

            var enIyi = GecerliPlakaAdayiniSec(karakterler);
            if (enIyi is not null)
            {
                return new SablonPlakaOkumaSonucu
                {
                    BasariliMi = enIyi.GuvenOrani >= MinimumSablonGuveni,
                    OkunanMetin = enIyi.OkunanMetin,
                    DuzeltilmisPlaka = enIyi.DuzeltilmisPlaka,
                    GuvenOrani = enIyi.GuvenOrani,
                    KarakterSayisi = karakterler.Count,
                    ProjeksiyonKarakterSayisi = projeksiyonKutulari.Count,
                    KonturKarakterSayisi = konturKutulari.Count,
                    SegmentasyonTuru = segmentasyonTuru,
                    Aciklama = enIyi.GuvenOrani >= MinimumSablonGuveni
                        ? "Sablon eslestirme ile gecerli plaka bulundu."
                        : "Sablon eslestirme gecerli plaka buldu ancak guven dusuk."
                };
            }

            var serbestAday = SerbestAdayOlustur(karakterler);
            return new SablonPlakaOkumaSonucu
            {
                BasariliMi = false,
                OkunanMetin = serbestAday.OkunanMetin,
                DuzeltilmisPlaka = serbestAday.DuzeltilmisPlaka,
                GuvenOrani = serbestAday.GuvenOrani,
                KarakterSayisi = karakterler.Count,
                ProjeksiyonKarakterSayisi = projeksiyonKutulari.Count,
                KonturKarakterSayisi = konturKutulari.Count,
                SegmentasyonTuru = segmentasyonTuru,
                Aciklama = "Sablon eslestirme Turk plaka formatinda sonuc uretemedi."
            };
        }
        catch (Exception ex)
        {
            return SablonPlakaOkumaSonucu.Basarisiz($"Sablon okuma hatasi: {ex.Message}");
        }
    }

    private PlakaSablonAdayi? GecerliPlakaAdayiniSec(List<SegmentliKarakter> karakterler)
    {
        PlakaSablonAdayi? enIyi = null;
        var maksimumUzunluk = Math.Min(9, karakterler.Count);

        for (var baslangic = 0; baslangic < karakterler.Count; baslangic++)
        {
            for (var uzunluk = 5; uzunluk <= maksimumUzunluk && baslangic + uzunluk <= karakterler.Count; uzunluk++)
            {
                var parca = karakterler.Skip(baslangic).Take(uzunluk).ToList();
                for (var harfUzunlugu = 1; harfUzunlugu <= 3; harfUzunlugu++)
                {
                    var sonRakamUzunlugu = uzunluk - 2 - harfUzunlugu;
                    if (sonRakamUzunlugu is < 2 or > 4)
                    {
                        continue;
                    }

                    var aday = DeseneGoreOku(parca, harfUzunlugu);
                    if (!PlakaDogrulamaYardimcisi.TurkPlakasiMi(aday.DuzeltilmisPlaka))
                    {
                        continue;
                    }

                    var adaySecimSkoru = aday.GuvenOrani + uzunluk * 2M;
                    var enIyiSecimSkoru = enIyi is null ? decimal.MinValue : enIyi.GuvenOrani + enIyi.OkunanMetin.Length * 2M;
                    if (enIyi is null || adaySecimSkoru > enIyiSecimSkoru)
                    {
                        enIyi = aday;
                    }
                }
            }
        }

        return enIyi;
    }

    private PlakaSablonAdayi DeseneGoreOku(List<SegmentliKarakter> karakterler, int harfUzunlugu)
    {
        var sonuc = new char[karakterler.Count];
        var toplamSkor = 0D;

        for (var i = 0; i < karakterler.Count; i++)
        {
            var rakamBekleniyor = i < 2 || i >= 2 + harfUzunlugu;
            var eslesme = EnIyiEslesme(karakterler[i].NormalVeri, rakamBekleniyor ? Rakamlar : Harfler);
            sonuc[i] = BeklenenTureGoreDuzelt(eslesme.Karakter, rakamBekleniyor);
            toplamSkor += eslesme.Skor;
        }

        var okunan = new string(sonuc);
        var duzeltilmis = PlakaDogrulamaYardimcisi.KarakterHatalariniDuzelt(okunan);
        return new PlakaSablonAdayi(okunan, duzeltilmis, (decimal)Math.Round(toplamSkor / karakterler.Count, 2));
    }

    private PlakaSablonAdayi? TekKarakterOcrIleOku(List<SegmentliKarakter> karakterler)
    {
        if (karakterler.Count < 5)
        {
            return null;
        }

        try
        {
            using var engine = TekKarakterTesseractMotoruOlustur();
            var rakamOkumalari = karakterler
                .Select(k => TekKarakterOku(engine, k.NormalVeri, Rakamlar))
                .ToList();
            var harfOkumalari = karakterler
                .Select(k => TekKarakterOku(engine, k.NormalVeri, Harfler))
                .ToList();

            PlakaSablonAdayi? enIyi = null;
            var maksimumUzunluk = Math.Min(9, karakterler.Count);

            for (var baslangic = 0; baslangic < karakterler.Count; baslangic++)
            {
                for (var uzunluk = 5; uzunluk <= maksimumUzunluk && baslangic + uzunluk <= karakterler.Count; uzunluk++)
                {
                    for (var harfUzunlugu = 1; harfUzunlugu <= 3; harfUzunlugu++)
                    {
                        var sonRakamUzunlugu = uzunluk - 2 - harfUzunlugu;
                        if (sonRakamUzunlugu is < 2 or > 4)
                        {
                            continue;
                        }

                        var karakterSonuclari = new List<TekKarakterOcrSonucu>();
                        for (var i = 0; i < uzunluk; i++)
                        {
                            var kaynakIndex = baslangic + i;
                            var rakamBekleniyor = i < 2 || i >= 2 + harfUzunlugu;
                            var ocrSonucu = rakamBekleniyor ? rakamOkumalari[kaynakIndex] : harfOkumalari[kaynakIndex];
                            if (ocrSonucu.Karakter == '?')
                            {
                                var sablon = EnIyiEslesme(karakterler[kaynakIndex].NormalVeri, rakamBekleniyor ? Rakamlar : Harfler);
                                ocrSonucu = new TekKarakterOcrSonucu(sablon.Karakter, (decimal)sablon.Skor);
                            }

                            karakterSonuclari.Add(new TekKarakterOcrSonucu(
                                BeklenenTureGoreDuzelt(ocrSonucu.Karakter, rakamBekleniyor),
                                ocrSonucu.GuvenOrani));
                        }

                        var okunan = new string(karakterSonuclari.Select(k => k.Karakter).ToArray());
                        var duzeltilmis = PlakaDogrulamaYardimcisi.KarakterHatalariniDuzelt(okunan);
                        if (!PlakaDogrulamaYardimcisi.TurkPlakasiMi(duzeltilmis))
                        {
                            continue;
                        }

                        var guven = karakterSonuclari.Count == 0
                            ? 0
                            : karakterSonuclari.Average(k => k.GuvenOrani);
                        var aday = new PlakaSablonAdayi(okunan, duzeltilmis, Math.Round(guven, 2));
                        var adaySecimSkoru = aday.GuvenOrani + uzunluk * 2M;
                        var enIyiSecimSkoru = enIyi is null ? decimal.MinValue : enIyi.GuvenOrani + enIyi.OkunanMetin.Length * 2M;

                        if (enIyi is null || adaySecimSkoru > enIyiSecimSkoru)
                        {
                            enIyi = aday;
                        }
                    }
                }
            }

            return enIyi;
        }
        catch
        {
            return null;
        }
    }

    private static TesseractEngine TekKarakterTesseractMotoruOlustur()
    {
        var tessDataPath = TessDataPathAl();
        var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
        engine.SetVariable("preserve_interword_spaces", "0");
        return engine;
    }

    private static TekKarakterOcrSonucu TekKarakterOku(TesseractEngine engine, bool[] karakter, IReadOnlyCollection<char> izinliKarakterler)
    {
        var whitelist = new string(izinliKarakterler.ToArray());
        engine.SetVariable("tessedit_char_whitelist", whitelist);

        var imagePath = Path.Combine(Path.GetTempPath(), $"plaka_char_{Guid.NewGuid():N}.png");
        try
        {
            using (var bitmap = BuyukKarakterBitmapOlustur(karakter))
            {
                bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
            }

            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img, PageSegMode.SingleChar);
            var temiz = PlakaDogrulamaYardimcisi.Temizle(page.GetText());
            var secilen = temiz.FirstOrDefault(izinliKarakterler.Contains);
            if (secilen == default)
            {
                return new TekKarakterOcrSonucu('?', 0);
            }

            return new TekKarakterOcrSonucu(secilen, Math.Round((decimal)page.GetMeanConfidence() * 100M, 2));
        }
        finally
        {
            try
            {
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }
            }
            catch
            {
                // Gecici tek karakter dosyasi silinemezse okuma akisini bozmayiz.
            }
        }
    }

    private static Bitmap BuyukKarakterBitmapOlustur(bool[] karakter)
    {
        using var kucuk = BoolDizidenBitmap(karakter, NormalGenislik, NormalYukseklik);
        var buyuk = new Bitmap(NormalGenislik * 4, NormalYukseklik * 4, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(buyuk);
        graphics.Clear(Color.White);
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.DrawImage(kucuk, new Rectangle(0, 0, buyuk.Width, buyuk.Height));
        return buyuk;
    }

    private static string TessDataPathAl()
    {
        var adaylar = new[]
        {
            Path.Combine(Application.StartupPath, "tessdata"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata")
        };

        return adaylar.FirstOrDefault(Directory.Exists) ?? adaylar[0];
    }

    private PlakaSablonAdayi SerbestAdayOlustur(List<SegmentliKarakter> karakterler)
    {
        var okunacaklar = karakterler.Take(9).ToList();
        if (okunacaklar.Count == 0)
        {
            return new PlakaSablonAdayi(string.Empty, string.Empty, 0);
        }

        var sonuc = new char[okunacaklar.Count];
        var toplamSkor = 0D;
        for (var i = 0; i < okunacaklar.Count; i++)
        {
            var eslesme = EnIyiEslesme(okunacaklar[i].NormalVeri, TumKarakterler);
            sonuc[i] = eslesme.Karakter;
            toplamSkor += eslesme.Skor;
        }

        var okunan = new string(sonuc);
        var duzeltilmis = PlakaDogrulamaYardimcisi.KarakterHatalariniDuzelt(okunan);
        return new PlakaSablonAdayi(okunan, duzeltilmis, (decimal)Math.Round(toplamSkor / okunacaklar.Count, 2));
    }

    private SablonEslesme EnIyiEslesme(bool[] karakter, IReadOnlyCollection<char> izinliKarakterler)
    {
        var enIyiKarakter = '?';
        var enIyiSkor = 0D;

        foreach (var karakterAdi in izinliKarakterler)
        {
            if (!_sablonlar.TryGetValue(karakterAdi, out var sablon))
            {
                continue;
            }

            var skor = SablonSkoruHesapla(karakter, sablon.Veri);
            if (skor > enIyiSkor)
            {
                enIyiSkor = skor;
                enIyiKarakter = karakterAdi;
            }
        }

        return new SablonEslesme(enIyiKarakter, enIyiSkor);
    }

    private static double SablonSkoruHesapla(bool[] karakter, bool[] sablon)
    {
        var ortakOnPlan = 0;
        var birlesikOnPlan = 0;
        var ayniPiksel = 0;
        var karakterOnPlan = 0;
        var sablonOnPlan = 0;

        for (var i = 0; i < karakter.Length; i++)
        {
            if (karakter[i])
            {
                karakterOnPlan++;
            }

            if (sablon[i])
            {
                sablonOnPlan++;
            }

            if (karakter[i] && sablon[i])
            {
                ortakOnPlan++;
            }

            if (karakter[i] || sablon[i])
            {
                birlesikOnPlan++;
            }

            if (karakter[i] == sablon[i])
            {
                ayniPiksel++;
            }
        }

        var iou = birlesikOnPlan == 0 ? 0D : (double)ortakOnPlan / birlesikOnPlan;
        var aynilik = (double)ayniPiksel / karakter.Length;
        var maxOnPlan = Math.Max(karakterOnPlan, sablonOnPlan);
        var alanUyumu = maxOnPlan == 0 ? 0D : 1D - (double)Math.Abs(karakterOnPlan - sablonOnPlan) / maxOnPlan;

        return Math.Round((iou * 0.68D + aynilik * 0.22D + alanUyumu * 0.10D) * 100D, 2);
    }

    private static Bitmap GoruntuyuHazirla(Image kaynak)
    {
        var scale = kaynak.Width > MaksimumIslemeGenisligi
            ? (double)MaksimumIslemeGenisligi / kaynak.Width
            : 3D;

        var width = Math.Max(1, (int)Math.Round(kaynak.Width * scale));
        var height = Math.Max(1, (int)Math.Round(kaynak.Height * scale));
        var hedef = new Bitmap(width, height, PixelFormat.Format24bppRgb);

        using var graphics = Graphics.FromImage(hedef);
        graphics.Clear(Color.White);
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
        const double factor = 1.85D;

        for (var i = 0; i < gray.Length; i++)
        {
            output[i] = (byte)Math.Clamp((int)((gray[i] - 128) * factor + 128), 0, 255);
        }

        return output;
    }

    private static bool[] OtsuIleIkiliyeCevir(byte[] gray, int width, int height)
    {
        var threshold = Math.Clamp(Math.Max(OtsuThreshold(gray), 135), 90, 180);
        var output = new bool[width * height];

        for (var i = 0; i < gray.Length; i++)
        {
            output[i] = gray[i] < threshold;
        }

        return output;
    }

    private static bool[] GerekirseTersCevir(bool[] binary)
    {
        var foreground = binary.Count(v => v);
        if (foreground <= binary.Length * 0.55D)
        {
            return binary;
        }

        var ters = new bool[binary.Length];
        for (var i = 0; i < binary.Length; i++)
        {
            ters[i] = !binary[i];
        }

        return ters;
    }

    private static int SegmentasyonSkoru(List<KarakterKutusu> kutular)
    {
        if (kutular.Count == 0)
        {
            return 0;
        }

        var formatPuani = kutular.Count switch
        {
            >= 5 and <= 9 => 100,
            10 or 11 or 12 => 80,
            _ => 0
        };
        var sekizKaraktereYakinlik = Math.Max(0, 40 - Math.Abs(kutular.Count - 8) * 10);
        var alanPuani = Math.Min(30, kutular.Sum(k => k.PikselSayisi) / 800);
        return formatPuani + sekizKaraktereYakinlik + alanPuani;
    }

    private static bool[] UzunCizgileriTemizle(bool[] binary, int width, int height)
    {
        var output = (bool[])binary.Clone();

        for (var y = 0; y < height; y++)
        {
            var count = 0;
            for (var x = 0; x < width; x++)
            {
                if (output[y * width + x])
                {
                    count++;
                }
            }

            if (count < width * 0.58D)
            {
                continue;
            }

            for (var dy = -1; dy <= 1; dy++)
            {
                var py = Math.Clamp(y + dy, 0, height - 1);
                for (var x = 0; x < width; x++)
                {
                    output[py * width + x] = false;
                }
            }
        }

        for (var x = 0; x < width; x++)
        {
            var count = 0;
            for (var y = 0; y < height; y++)
            {
                if (output[y * width + x])
                {
                    count++;
                }
            }

            if (count < height * 0.82D)
            {
                continue;
            }

            for (var dx = -1; dx <= 1; dx++)
            {
                var px = Math.Clamp(x + dx, 0, width - 1);
                for (var y = 0; y < height; y++)
                {
                    output[y * width + px] = false;
                }
            }
        }

        return output;
    }

    private static List<KarakterKutusu> ProjeksiyonIleKarakterKutulariniBul(bool[] binary, int width, int height)
    {
        var band = MetinBandiniBul(binary, width, height);
        if (band.Height <= 0)
        {
            return new List<KarakterKutusu>();
        }

        var thresholdAdaylari = new[]
        {
            Math.Max(2, (int)(band.Height * 0.07D)),
            Math.Max(2, (int)(band.Height * 0.045D)),
            Math.Max(1, (int)(band.Height * 0.03D))
        };

        List<KarakterKutusu> enIyi = new();
        foreach (var threshold in thresholdAdaylari)
        {
            var kutular = ProjeksiyonIleKarakterKutulariniBul(binary, width, height, band, threshold);
            if (SegmentasyonSkoru(kutular) > SegmentasyonSkoru(enIyi))
            {
                enIyi = kutular;
            }
        }

        return enIyi;
    }

    private static Rectangle MetinBandiniBul(bool[] binary, int width, int height)
    {
        var rowCounts = new int[height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (binary[y * width + x])
                {
                    rowCounts[y]++;
                }
            }
        }

        var threshold = Math.Max(3, (int)(width * 0.025D));
        var enIyiBaslangic = 0;
        var enIyiBitis = height - 1;
        var enIyiSkor = 0;
        var yIndex = 0;

        while (yIndex < height)
        {
            while (yIndex < height && rowCounts[yIndex] < threshold)
            {
                yIndex++;
            }

            var baslangic = yIndex;
            var skor = 0;
            while (yIndex < height && rowCounts[yIndex] >= threshold)
            {
                skor += rowCounts[yIndex];
                yIndex++;
            }

            var bitis = yIndex - 1;
            var bandYukseklik = bitis - baslangic + 1;
            if (bandYukseklik >= height * 0.25D && skor > enIyiSkor)
            {
                enIyiSkor = skor;
                enIyiBaslangic = baslangic;
                enIyiBitis = bitis;
            }
        }

        if (enIyiSkor == 0)
        {
            return new Rectangle(0, 0, width, height);
        }

        enIyiBaslangic = Math.Max(0, enIyiBaslangic - 3);
        enIyiBitis = Math.Min(height - 1, enIyiBitis + 3);
        return new Rectangle(0, enIyiBaslangic, width, enIyiBitis - enIyiBaslangic + 1);
    }

    private static List<KarakterKutusu> ProjeksiyonIleKarakterKutulariniBul(
        bool[] binary,
        int width,
        int height,
        Rectangle band,
        int columnThreshold)
    {
        var columnCounts = new int[width];
        for (var x = 0; x < width; x++)
        {
            for (var y = band.Top; y < band.Bottom; y++)
            {
                if (binary[y * width + x])
                {
                    columnCounts[x]++;
                }
            }
        }

        columnCounts = SutunlariYumusat(columnCounts);
        var runs = new List<Rectangle>();
        var xIndex = 0;
        while (xIndex < width)
        {
            while (xIndex < width && columnCounts[xIndex] < columnThreshold)
            {
                xIndex++;
            }

            var baslangic = xIndex;
            while (xIndex < width && columnCounts[xIndex] >= columnThreshold)
            {
                xIndex++;
            }

            var bitis = xIndex - 1;
            if (bitis >= baslangic)
            {
                runs.Add(new Rectangle(baslangic, band.Y, bitis - baslangic + 1, band.Height));
            }
        }

        runs = YakinSutunParcalariniBirlestir(runs, Math.Max(2, (int)(width * 0.004D)));
        return runs
            .Select(run => KutuDaralt(binary, width, height, run))
            .Where(k => KutuKaraktereBenziyor(k, width, height))
            .OrderBy(k => k.Kutu.X)
            .Take(12)
            .ToList();
    }

    private static int[] SutunlariYumusat(int[] counts)
    {
        var output = new int[counts.Length];
        for (var i = 0; i < counts.Length; i++)
        {
            var toplam = 0;
            var adet = 0;
            for (var offset = -2; offset <= 2; offset++)
            {
                var index = Math.Clamp(i + offset, 0, counts.Length - 1);
                toplam += counts[index];
                adet++;
            }

            output[i] = toplam / adet;
        }

        return output;
    }

    private static List<Rectangle> YakinSutunParcalariniBirlestir(List<Rectangle> runs, int maksimumBosluk)
    {
        if (runs.Count <= 1)
        {
            return runs;
        }

        var sonuc = new List<Rectangle>();
        var aktif = runs[0];

        for (var i = 1; i < runs.Count; i++)
        {
            var siradaki = runs[i];
            var bosluk = siradaki.Left - aktif.Right;
            if (bosluk <= maksimumBosluk)
            {
                aktif = Rectangle.Union(aktif, siradaki);
                continue;
            }

            sonuc.Add(aktif);
            aktif = siradaki;
        }

        sonuc.Add(aktif);
        return sonuc;
    }

    private static KarakterKutusu KutuDaralt(bool[] binary, int width, int height, Rectangle alan)
    {
        var sinir = Rectangle.Intersect(new Rectangle(0, 0, width, height), alan);
        var minX = sinir.Right;
        var maxX = sinir.Left;
        var minY = sinir.Bottom;
        var maxY = sinir.Top;
        var count = 0;

        for (var y = sinir.Top; y < sinir.Bottom; y++)
        {
            for (var x = sinir.Left; x < sinir.Right; x++)
            {
                if (!binary[y * width + x])
                {
                    continue;
                }

                count++;
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }
        }

        if (count == 0)
        {
            return new KarakterKutusu(Rectangle.Empty, 0);
        }

        return new KarakterKutusu(new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1), count);
    }

    private static bool KutuKaraktereBenziyor(KarakterKutusu kutu, int width, int height)
    {
        if (kutu.Kutu.IsEmpty)
        {
            return false;
        }

        var boxWidth = kutu.Kutu.Width;
        var boxHeight = kutu.Kutu.Height;
        var density = (double)kutu.PikselSayisi / Math.Max(1, boxWidth * boxHeight);

        return boxWidth >= Math.Max(6, width * 0.014D) &&
               boxWidth <= width * 0.23D &&
               boxHeight >= height * 0.22D &&
               boxHeight <= height * 0.98D &&
               density is >= 0.04D and <= 0.90D;
    }

    private static List<KarakterKutusu> KarakterKutulariniBul(bool[] binary, int width, int height)
    {
        var visited = new bool[binary.Length];
        var kutular = new List<KarakterKutusu>();
        var minHeight = Math.Max(12, (int)(height * 0.30D));
        var maxHeight = (int)(height * 0.98D);
        var minWidth = Math.Max(4, (int)(width * 0.008D));
        var maxWidth = (int)(width * 0.24D);
        var minArea = Math.Max(25, (int)(width * height * 0.00035D));

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                if (!binary[index] || visited[index])
                {
                    continue;
                }

                var kutu = BilesenBul(binary, visited, width, height, x, y);
                var boxWidth = kutu.Kutu.Width;
                var boxHeight = kutu.Kutu.Height;
                var density = (double)kutu.PikselSayisi / Math.Max(1, boxWidth * boxHeight);

                if (boxWidth < minWidth ||
                    boxHeight < minHeight ||
                    boxWidth > maxWidth ||
                    boxHeight > maxHeight ||
                    kutu.PikselSayisi < minArea ||
                    density is < 0.06D or > 0.82D)
                {
                    continue;
                }

                kutular.Add(kutu);
            }
        }

        if (kutular.Count <= 1)
        {
            return kutular;
        }

        var ortancaYukseklik = kutular
            .Select(k => k.Kutu.Height)
            .OrderBy(v => v)
            .ElementAt(kutular.Count / 2);

        return kutular
            .Where(k => k.Kutu.Height >= ortancaYukseklik * 0.55D)
            .OrderBy(k => k.Kutu.X)
            .Take(14)
            .ToList();
    }

    private static KarakterKutusu BilesenBul(bool[] binary, bool[] visited, int width, int height, int startX, int startY)
    {
        var queue = new Queue<Point>();
        queue.Enqueue(new Point(startX, startY));
        visited[startY * width + startX] = true;

        var minX = startX;
        var maxX = startX;
        var minY = startY;
        var maxY = startY;
        var count = 0;

        while (queue.Count > 0)
        {
            var point = queue.Dequeue();
            count++;
            minX = Math.Min(minX, point.X);
            maxX = Math.Max(maxX, point.X);
            minY = Math.Min(minY, point.Y);
            maxY = Math.Max(maxY, point.Y);

            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    var nx = point.X + dx;
                    var ny = point.Y + dy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    {
                        continue;
                    }

                    var index = ny * width + nx;
                    if (!binary[index] || visited[index])
                    {
                        continue;
                    }

                    visited[index] = true;
                    queue.Enqueue(new Point(nx, ny));
                }
            }
        }

        return new KarakterKutusu(new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1), count);
    }

    private static bool[] NormalizeEt(bool[] binary, int width, int height, Rectangle kutu)
    {
        var paddingX = Math.Max(2, (int)(kutu.Width * 0.08D));
        var paddingY = Math.Max(2, (int)(kutu.Height * 0.06D));
        var source = Rectangle.Intersect(
            new Rectangle(0, 0, width, height),
            Rectangle.Inflate(kutu, paddingX, paddingY));

        using var crop = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(crop))
        {
            graphics.Clear(Color.White);
        }

        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var value = binary[(source.Y + y) * width + (source.X + x)] ? Color.Black : Color.White;
                crop.SetPixel(x, y, value);
            }
        }

        using var normalized = new Bitmap(NormalGenislik, NormalYukseklik, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(normalized))
        {
            graphics.Clear(Color.White);
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;

            var scale = Math.Min((NormalGenislik - 6D) / crop.Width, (NormalYukseklik - 6D) / crop.Height);
            var drawWidth = Math.Max(1, (int)Math.Round(crop.Width * scale));
            var drawHeight = Math.Max(1, (int)Math.Round(crop.Height * scale));
            var x = (NormalGenislik - drawWidth) / 2;
            var y = (NormalYukseklik - drawHeight) / 2;
            graphics.DrawImage(crop, new Rectangle(x, y, drawWidth, drawHeight));
        }

        return BitmaptenBoolDiziAl(normalized);
    }

    private static bool[] BitmaptenBoolDiziAl(Bitmap bitmap)
    {
        var gray = BitmaptenGriDiziAl(bitmap, out var width, out var height);
        var threshold = OtsuThreshold(gray);
        var output = new bool[width * height];

        for (var i = 0; i < gray.Length; i++)
        {
            output[i] = gray[i] < threshold;
        }

        return output;
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

    private static bool[] Dilate(bool[] binary, int width, int height)
    {
        var output = new bool[binary.Length];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var hasForeground = false;
                for (var ky = -1; ky <= 1 && !hasForeground; ky++)
                {
                    var py = Math.Clamp(y + ky, 0, height - 1);
                    for (var kx = -1; kx <= 1; kx++)
                    {
                        var px = Math.Clamp(x + kx, 0, width - 1);
                        if (binary[py * width + px])
                        {
                            hasForeground = true;
                            break;
                        }
                    }
                }

                output[y * width + x] = hasForeground;
            }
        }

        return output;
    }

    private static bool[] Erode(bool[] binary, int width, int height)
    {
        var output = new bool[binary.Length];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var allForeground = true;
                for (var ky = -1; ky <= 1 && allForeground; ky++)
                {
                    var py = Math.Clamp(y + ky, 0, height - 1);
                    for (var kx = -1; kx <= 1; kx++)
                    {
                        var px = Math.Clamp(x + kx, 0, width - 1);
                        if (!binary[py * width + px])
                        {
                            allForeground = false;
                            break;
                        }
                    }
                }

                output[y * width + x] = allForeground;
            }
        }

        return output;
    }

    private static char BeklenenTureGoreDuzelt(char karakter, bool rakamBekleniyor)
    {
        return rakamBekleniyor
            ? karakter switch
            {
                'O' => '0',
                'I' => '1',
                'S' => '5',
                'B' => '8',
                _ => karakter
            }
            : karakter switch
            {
                '0' => 'O',
                '1' => 'I',
                '5' => 'S',
                _ => karakter
            };
    }

    private static void SablonKlasorunuHazirla(string sablonKlasoru)
    {
        Directory.CreateDirectory(sablonKlasoru);
        foreach (var karakter in TumKarakterler)
        {
            var path = Path.Combine(sablonKlasoru, $"{karakter}.png");
            if (!File.Exists(path))
            {
                SablonDosyasiOlustur(path, karakter);
            }
        }
    }

    private static Dictionary<char, SablonKarakter> SablonlariYukle(string sablonKlasoru)
    {
        var sablonlar = new Dictionary<char, SablonKarakter>();
        foreach (var karakter in TumKarakterler)
        {
            var path = Path.Combine(sablonKlasoru, $"{karakter}.png");
            using var bitmap = new Bitmap(path);
            sablonlar[karakter] = new SablonKarakter(karakter, BitmaptenBoolDiziAl(bitmap));
        }

        return sablonlar;
    }

    private static void SablonDosyasiOlustur(string path, char karakter)
    {
        using var buyuk = new Bitmap(NormalGenislik * 4, NormalYukseklik * 4, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(buyuk))
        {
            graphics.Clear(Color.White);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            using var font = SablonFontuOlustur(karakter);
            using var brush = new SolidBrush(Color.Black);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoClip
            };

            var rect = new RectangleF(0, -12, buyuk.Width, buyuk.Height + 16);
            graphics.DrawString(karakter.ToString(), font, brush, rect, format);
        }

        var gri = BitmaptenGriDiziAl(buyuk, out var width, out var height);
        var binary = OtsuIleIkiliyeCevir(gri, width, height);
        var kutular = KarakterKutulariniBul(binary, width, height);
        var kutu = kutular.Count > 0
            ? kutular.OrderByDescending(k => k.Kutu.Width * k.Kutu.Height).First().Kutu
            : new Rectangle(0, 0, width, height);
        var normal = NormalizeEt(binary, width, height, kutu);
        using var kayit = BoolDizidenBitmap(normal, NormalGenislik, NormalYukseklik);
        kayit.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }

    private static Font SablonFontuOlustur(char karakter)
    {
        foreach (var aile in new[] { "Arial Black", "Segoe UI Black", "Impact", "Bahnschrift SemiBold" })
        {
            try
            {
                return new Font(aile, karakter == '1' ? 185F : 172F, FontStyle.Regular, GraphicsUnit.Pixel);
            }
            catch
            {
                // Font yoksa siradaki adaya gecilir.
            }
        }

        return new Font(FontFamily.GenericSansSerif, 172F, FontStyle.Bold, GraphicsUnit.Pixel);
    }

    private static Bitmap BoolDizidenBitmap(bool[] data, int width, int height)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                bitmap.SetPixel(x, y, data[y * width + x] ? Color.Black : Color.White);
            }
        }

        return bitmap;
    }

    private sealed record SablonKarakter(char Karakter, bool[] Veri);

    private sealed record KarakterKutusu(Rectangle Kutu, int PikselSayisi);

    private sealed record SegmentliKarakter(KarakterKutusu Kutu, bool[] NormalVeri);

    private sealed record SablonEslesme(char Karakter, double Skor);

    private sealed record TekKarakterOcrSonucu(char Karakter, decimal GuvenOrani);

    private sealed record PlakaSablonAdayi(string OkunanMetin, string DuzeltilmisPlaka, decimal GuvenOrani);
}

public class SablonPlakaOkumaSonucu
{
    public string OkunanMetin { get; set; } = string.Empty;
    public string DuzeltilmisPlaka { get; set; } = string.Empty;
    public decimal GuvenOrani { get; set; }
    public bool BasariliMi { get; set; }
    public int KarakterSayisi { get; set; }
    public int ProjeksiyonKarakterSayisi { get; set; }
    public int KonturKarakterSayisi { get; set; }
    public string SegmentasyonTuru { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;

    public static SablonPlakaOkumaSonucu Basarisiz(string aciklama)
    {
        return new SablonPlakaOkumaSonucu { BasariliMi = false, Aciklama = aciklama };
    }
}
