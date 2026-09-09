using System.Drawing.Imaging;
using PlakaTanimaliOtoparkOtomasyonu.DataAccess;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;
using PlakaTanimaliOtoparkOtomasyonu.Models;
using Tesseract;

namespace PlakaTanimaliOtoparkOtomasyonu.Services;

public class PlakaOkumaServisi
{
    private const string BasarisizMesaj = "Plaka net okunamadi, lutfen plakayi cerceveye duzgun yerlestiriniz veya manuel giriniz.";
    private const decimal MinimumOtomatikKabulGuveni = 45M;
    private const decimal DusukGuvenOneriEsigi = 70M;
    private const decimal PythonFormatDisiGuvenEsigi = 55M;

    private readonly PlakaOnIslemeServisi _plakaOnIslemeServisi = new();
    private readonly PythonPlakaOkumaServisi _pythonPlakaOkumaServisi = new();
    private readonly SablonPlakaOkumaServisi _sablonPlakaOkumaServisi = new();
    private readonly PlakaOkumaKaydiRepository _plakaOkumaKaydiRepository = new();
    private readonly AracRepository _aracRepository = new();

    public async Task<PlakaOkumaSonucu> KameradanOkuAsync(KameraServisi kameraServisi, string islemTuru, CancellationToken cancellationToken = default)
    {
        var kareler = await kameraServisi.KareleriYakalaAsync(15, 2000, cancellationToken);
        try
        {
            return await OkuKarelerdenAsync(kareler, islemTuru, roiKullan: true, resimdenOkuma: false, kaynakResimYolu: null, cancellationToken);
        }
        finally
        {
            foreach (var kare in kareler)
            {
                kare.Dispose();
            }
        }
    }

    public Task<PlakaOkumaSonucu> ResimdenOkuAsync(Image image, string islemTuru, string? kaynakResimYolu = null, CancellationToken cancellationToken = default)
    {
        return OkuKarelerdenAsync(new[] { image }, islemTuru, roiKullan: false, resimdenOkuma: true, kaynakResimYolu, cancellationToken);
    }

    public void ManuelOkumaLogla(string plaka, string islemTuru, string? resimYolu = null, string? okunanMetin = null)
    {
        var temiz = PlakaDogrulamaYardimcisi.Temizle(plaka);
        var duzeltilmis = PlakaDogrulamaYardimcisi.KarakterHatalariniDuzelt(temiz);
        KayitEkle(new PlakaOkumaSonucu
        {
            OkunanMetin = string.IsNullOrWhiteSpace(okunanMetin) ? temiz : okunanMetin,
            DuzeltilmisPlaka = duzeltilmis,
            GuvenOrani = 100,
            ResimYolu = resimYolu,
            IslemTuru = islemTuru,
            BasariliMi = PlakaDogrulamaYardimcisi.TurkPlakasiMi(duzeltilmis),
            Aciklama = "Kullanici tarafindan onaylanan veya manuel duzeltilen plaka."
        });
    }

    private async Task<PlakaOkumaSonucu> OkuKarelerdenAsync(IEnumerable<Image> kareler, string islemTuru, bool roiKullan, bool resimdenOkuma, string? kaynakResimYolu, CancellationToken cancellationToken)
    {
        var kareListesi = kareler.ToList();
        var sonuc = new PlakaOkumaSonucu { IslemTuru = islemTuru };
        var adaylar = new List<PlakaOkumaAdayi>();
        var sablonAdaylari = new List<SablonPlakaOkumaSonucu>();
        var temsilciResimYolu = kaynakResimYolu;
        var kareNo = 0;

        try
        {
            if (temsilciResimYolu is null && kareListesi.FirstOrDefault() is { } ilkKare)
            {
                temsilciResimYolu = TemsilciResimKaydet(ilkKare, roiKullan);
            }
            sonuc.ResimYolu = temsilciResimYolu;

            await PythonSonucunuDeneAsync(sonuc, kareListesi, cancellationToken);
            if (sonuc.BasariliMi)
            {
                KayitEkle(sonuc);
                return sonuc;
            }

            if (!string.IsNullOrWhiteSpace(sonuc.PythonFormatDisiAday))
            {
                KayitEkle(sonuc);
                return sonuc;
            }

            using var ocr = TesseractMotoru.Olustur();

            foreach (var kare in kareListesi)
            {
                kareNo++;
                if (temsilciResimYolu is null)
                {
                    temsilciResimYolu = TemsilciResimKaydet(kare, roiKullan);
                }

                SablonAdayiEkle(kare, sablonAdaylari);

                var islenmisler = new List<PlakaIslenmisGoruntu>();
                try
                {
                    if (roiKullan)
                    {
                        islenmisler.AddRange(_plakaOnIslemeServisi.OnIsle(kare, KameraServisi.PlakaCercevesiHesapla(kare.Size)));
                    }
                    else
                    {
                        islenmisler.AddRange(_plakaOnIslemeServisi.OnIsle(kare));
                        if (resimdenOkuma)
                        {
                            islenmisler.AddRange(_plakaOnIslemeServisi.OnIsle(kare, KameraServisi.PlakaCercevesiHesapla(kare.Size)));
                        }
                    }

                    foreach (var islenmis in islenmisler)
                    {
                        var ocrSonucu = ocr.Oku(islenmis.Goruntu);
                        var temiz = PlakaDogrulamaYardimcisi.Temizle(ocrSonucu.Metin);
                        var duzeltilmis = PlakaDogrulamaYardimcisi.KarakterHatalariniDuzelt(temiz);
                        var gecerli = PlakaDogrulamaYardimcisi.TurkPlakasiMi(duzeltilmis);

                        adaylar.Add(new PlakaOkumaAdayi
                        {
                            HamMetin = ocrSonucu.Metin,
                            TemizMetin = temiz,
                            DuzeltilmisPlaka = duzeltilmis,
                            GuvenOrani = ocrSonucu.GuvenOrani,
                            GecerliMi = gecerli,
                            KareNo = kareNo,
                            OnIslemeTuru = islenmis.Tur
                        });
                    }
                }
                finally
                {
                    foreach (var islenmis in islenmisler)
                    {
                        islenmis.Dispose();
                    }
                }
            }

            sonuc.Adaylar.AddRange(adaylar);
            sonuc.ResimYolu = temsilciResimYolu;
            CokluSonucuSec(sonuc, adaylar);
            SablonSonucunuUygula(sonuc, sablonAdaylari);
            YakinPlakaOnerisiEkle(sonuc);
        }
        catch (Exception ex)
        {
            sonuc.BasariliMi = false;
            sonuc.ResimYolu = temsilciResimYolu;
            sonuc.Aciklama = $"{BasarisizMesaj} Detay: {ex.Message}";
            YakinPlakaOnerisiEkle(sonuc);
        }

        if (!sonuc.BasariliMi)
        {
            sonuc.Aciklama = string.IsNullOrWhiteSpace(sonuc.Aciklama) ? BasarisizMesaj : sonuc.Aciklama;
        }

        sonuc.NihaiSonuc = sonuc.BasariliMi ? sonuc.DuzeltilmisPlaka : sonuc.NihaiSonuc;

        KayitEkle(sonuc);
        return sonuc;
    }

    private async Task PythonSonucunuDeneAsync(PlakaOkumaSonucu sonuc, IReadOnlyList<Image> kareler, CancellationToken cancellationToken)
    {
        if (kareler.Count == 0)
        {
            return;
        }

        var pythonSonuclari = new List<PythonPlakaOkumaSonucu>();
        foreach (var kare in TemsilciKareleriSec(kareler))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var roi = _plakaOnIslemeServisi.RoiKirp(kare);
            var pythonSonucu = await _pythonPlakaOkumaServisi.OkuAsync(roi, cancellationToken);
            pythonSonuclari.Add(pythonSonucu);

            if (!pythonSonucu.ServisCalisiyorMu)
            {
                break;
            }
        }

        var servisSonucu = pythonSonuclari.FirstOrDefault(s => s.ServisCalisiyorMu) ?? pythonSonuclari.FirstOrDefault();
        if (servisSonucu is null)
        {
            return;
        }

        sonuc.PythonServisiCalisiyorMu = servisSonucu.ServisCalisiyorMu;
        sonuc.PythonMesaji = servisSonucu.Message;
        sonuc.PythonAdaylari = PythonAdaylariMetniOlustur(pythonSonuclari);

        if (!servisSonucu.ServisCalisiyorMu)
        {
            sonuc.Aciklama = servisSonucu.Message;
            return;
        }

        var secim = PythonAdayiniSec(pythonSonuclari);
        if (secim is null)
        {
            sonuc.PythonReddedilmeSebebi = "Python OCR aday uretmedi.";
            sonuc.Aciklama = string.IsNullOrWhiteSpace(servisSonucu.Message)
                ? "Python EasyOCR plaka okuyamadi. Tesseract yedek yontemi deneniyor."
                : $"{servisSonucu.Message}. Tesseract yedek yontemi deneniyor.";
            return;
        }

        sonuc.PythonSonucu = secim.Plaka;
        sonuc.PythonSecilenAday = secim.Plaka;
        sonuc.PythonGuvenOrani = secim.GuvenOrani;
        sonuc.PythonReddedilmeSebebi = secim.ReddedilmeSebebi;

        if (!secim.GecerliMi)
        {
            if (secim.GuvenOrani >= PythonFormatDisiGuvenEsigi)
            {
                sonuc.PythonFormatDisiAday = secim.Plaka;
                sonuc.PythonFormatDisiGuvenOrani = secim.GuvenOrani;
                sonuc.Aciklama = $"Python OCR plakayi okudu ancak sonuc Turk plaka formatina uymuyor: {secim.Plaka}";
                return;
            }

            sonuc.Aciklama = "Python EasyOCR sonucu dusuk guvenli veya format disi. Tesseract yedek yontemi deneniyor.";
            return;
        }

        var duzeltilmis = PlakaDogrulamaYardimcisi.KarakterHatalariniDuzelt(secim.Plaka);
        sonuc.OkunanMetin = secim.Plaka;
        sonuc.DuzeltilmisPlaka = duzeltilmis;
        sonuc.GuvenOrani = secim.GuvenOrani;
        sonuc.BasariliMi = true;
        sonuc.NihaiSonuc = duzeltilmis;
        sonuc.Aciklama = $"Python EasyOCR sonucu secildi. Kaynak: {secim.Kaynak}.";
    }

    private static IEnumerable<Image> TemsilciKareleriSec(IReadOnlyList<Image> kareler)
    {
        if (kareler.Count <= 3)
        {
            return kareler;
        }

        var indeksler = new[] { kareler.Count / 2, kareler.Count - 1, 0 }
            .Distinct()
            .Where(index => index >= 0 && index < kareler.Count);

        return indeksler.Select(index => kareler[index]);
    }

    private static PythonAdaySecimi? PythonAdayiniSec(IEnumerable<PythonPlakaOkumaSonucu> sonuclar)
    {
        var adaylar = PythonAdaylariniOlustur(sonuclar).ToList();
        if (adaylar.Count == 0)
        {
            return null;
        }

        var gecerli = adaylar
            .Where(a => PlakaDogrulamaYardimcisi.TurkPlakasiMi(a.Plaka))
            .GroupBy(a => a.Plaka)
            .Select(group => new PythonAdaySecimi(
                group.Key,
                group.Max(a => a.GuvenOrani),
                true,
                group.OrderByDescending(a => a.GuvenOrani).First().Kaynak,
                string.Empty,
                group.Count()))
            .OrderByDescending(a => a.Tekrar)
            .ThenByDescending(a => a.GuvenOrani)
            .FirstOrDefault();

        if (gecerli is not null)
        {
            return gecerli;
        }

        return adaylar
            .GroupBy(a => a.Plaka)
            .Select(group => new PythonAdaySecimi(
                group.Key,
                group.Max(a => a.GuvenOrani),
                false,
                group.OrderByDescending(a => a.GuvenOrani).First().Kaynak,
                "Turk plaka regex formatina uymuyor.",
                group.Count()))
            .OrderByDescending(a => a.Tekrar)
            .ThenByDescending(a => a.GuvenOrani)
            .FirstOrDefault();
    }

    private static IEnumerable<PythonAdaySecimi> PythonAdaylariniOlustur(IEnumerable<PythonPlakaOkumaSonucu> sonuclar)
    {
        foreach (var sonuc in sonuclar)
        {
            var anaPlaka = PlakaDogrulamaYardimcisi.KarakterHatalariniDuzelt(sonuc.Plate);
            if (!string.IsNullOrWhiteSpace(anaPlaka))
            {
                yield return new PythonAdaySecimi(
                    anaPlaka,
                    Math.Round(sonuc.Confidence * 100M, 2),
                    PlakaDogrulamaYardimcisi.TurkPlakasiMi(anaPlaka),
                    string.IsNullOrWhiteSpace(sonuc.SourceMethod) ? sonuc.Method : sonuc.SourceMethod,
                    string.Empty,
                    1);
            }

            foreach (var aday in sonuc.Candidates)
            {
                var plaka = PlakaDogrulamaYardimcisi.KarakterHatalariniDuzelt(
                    string.IsNullOrWhiteSpace(aday.Plate) ? aday.Text : aday.Plate);
                if (string.IsNullOrWhiteSpace(plaka))
                {
                    continue;
                }

                yield return new PythonAdaySecimi(
                    plaka,
                    Math.Round(aday.Confidence * 100M, 2),
                    PlakaDogrulamaYardimcisi.TurkPlakasiMi(plaka),
                    aday.Method,
                    string.Empty,
                    1);
            }
        }
    }

    private static string PythonAdaylariMetniOlustur(IEnumerable<PythonPlakaOkumaSonucu> sonuclar)
    {
        return string.Join(", ",
            PythonAdaylariniOlustur(sonuclar)
                .GroupBy(aday => aday.Plaka)
                .Select(group => group.OrderByDescending(aday => aday.GuvenOrani).First())
                .OrderByDescending(aday => aday.GecerliMi)
                .ThenByDescending(aday => aday.GuvenOrani)
                .Take(8)
                .Select(aday => $"{aday.Plaka} %{aday.GuvenOrani:N0}{(aday.GecerliMi ? "" : " format disi")}"));
    }

    private static void CokluSonucuSec(PlakaOkumaSonucu sonuc, List<PlakaOkumaAdayi> adaylar)
    {
        var gecerliAdaylar = adaylar
            .Where(a => a.GecerliMi && a.GuvenOrani >= MinimumOtomatikKabulGuveni)
            .ToList();
        if (gecerliAdaylar.Count == 0)
        {
            var enIyiGecersiz = adaylar
                .OrderByDescending(a => a.GuvenOrani)
                .FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.TemizMetin));

            if (enIyiGecersiz is not null)
            {
                sonuc.OkunanMetin = enIyiGecersiz.HamMetin;
                sonuc.DuzeltilmisPlaka = enIyiGecersiz.DuzeltilmisPlaka;
                sonuc.GuvenOrani = enIyiGecersiz.GuvenOrani;
                sonuc.TesseractSonucu = string.IsNullOrWhiteSpace(enIyiGecersiz.DuzeltilmisPlaka)
                    ? enIyiGecersiz.TemizMetin
                    : enIyiGecersiz.DuzeltilmisPlaka;
                sonuc.TesseractGuvenOrani = enIyiGecersiz.GuvenOrani;
            }

            sonuc.BasariliMi = false;
            sonuc.Aciklama = adaylar.Any(a => a.GecerliMi)
                ? $"OCR sonucu dusuk guvenli. {BasarisizMesaj}"
                : BasarisizMesaj;
            return;
        }

        var secilen = gecerliAdaylar
            .GroupBy(a => a.DuzeltilmisPlaka)
            .Select(group => new
            {
                Plaka = group.Key,
                Tekrar = group.Count(),
                EnYuksekGuven = group.Max(a => a.GuvenOrani),
                EnIyiAday = group.OrderByDescending(a => a.GuvenOrani).First()
            })
            .OrderByDescending(x => x.Tekrar)
            .ThenByDescending(x => x.EnYuksekGuven)
            .First();

        sonuc.OkunanMetin = secilen.EnIyiAday.HamMetin;
        sonuc.DuzeltilmisPlaka = secilen.Plaka;
        sonuc.GuvenOrani = secilen.EnYuksekGuven;
        sonuc.BasariliMi = true;
        sonuc.TesseractSonucu = secilen.Plaka;
        sonuc.TesseractGuvenOrani = secilen.EnYuksekGuven;
        sonuc.NihaiSonuc = secilen.Plaka;
        sonuc.Aciklama = $"{secilen.Tekrar} OCR adayindan cogunluk oylamasi ile secildi.";
    }

    private void SablonAdayiEkle(Image kare, List<SablonPlakaOkumaSonucu> sablonAdaylari)
    {
        try
        {
            using var roi = _plakaOnIslemeServisi.RoiKirp(kare);
            var sablonSonucu = _sablonPlakaOkumaServisi.Oku(roi);
            if (!string.IsNullOrWhiteSpace(sablonSonucu.DuzeltilmisPlaka) ||
                !string.IsNullOrWhiteSpace(sablonSonucu.OkunanMetin) ||
                sablonSonucu.KarakterSayisi > 0)
            {
                sablonAdaylari.Add(sablonSonucu);
            }
        }
        catch
        {
            // Sablon okuma ikinci yoldur; hata alinirsa Tesseract ve manuel giris akisi devam eder.
        }
    }

    private static void SablonSonucunuUygula(PlakaOkumaSonucu sonuc, List<SablonPlakaOkumaSonucu> sablonAdaylari)
    {
        if (sablonAdaylari.Count == 0)
        {
            return;
        }

        var secilen = sablonAdaylari
            .Where(s => !string.IsNullOrWhiteSpace(s.DuzeltilmisPlaka))
            .GroupBy(s => s.DuzeltilmisPlaka)
            .Select(group => new
            {
                Plaka = group.Key,
                Tekrar = group.Count(),
                EnYuksekGuven = group.Max(s => s.GuvenOrani),
                EnIyiAday = group.OrderByDescending(s => s.GuvenOrani).First()
            })
            .OrderByDescending(x => x.Tekrar)
            .ThenByDescending(x => x.EnYuksekGuven)
            .FirstOrDefault();

        if (secilen is null)
        {
            return;
        }

        sonuc.SablonSonucu = secilen.Plaka;
        sonuc.SablonGuvenOrani = secilen.EnYuksekGuven;

        var sablonGecerli = secilen.EnIyiAday.BasariliMi &&
                            PlakaDogrulamaYardimcisi.TurkPlakasiMi(secilen.Plaka);
        if (!sablonGecerli)
        {
            return;
        }

        var tesseractDusukGuvenli = !sonuc.BasariliMi || sonuc.GuvenOrani < DusukGuvenOneriEsigi;
        var sablonDahaGuvenli = !sonuc.BasariliMi || secilen.EnYuksekGuven >= sonuc.GuvenOrani - 5M;

        if (tesseractDusukGuvenli && sablonDahaGuvenli)
        {
            sonuc.OkunanMetin = secilen.EnIyiAday.OkunanMetin;
            sonuc.DuzeltilmisPlaka = secilen.Plaka;
            sonuc.GuvenOrani = secilen.EnYuksekGuven;
            sonuc.BasariliMi = true;
            sonuc.NihaiSonuc = secilen.Plaka;
            sonuc.Aciklama = $"Tesseract dusuk guvenli oldugu icin sablon eslestirme sonucu secildi. Tekrar: {secilen.Tekrar}.";
            return;
        }

        if (!sonuc.BasariliMi && !string.IsNullOrWhiteSpace(secilen.Plaka))
        {
            sonuc.DuzeltilmisPlaka = secilen.Plaka;
            sonuc.GuvenOrani = Math.Max(sonuc.GuvenOrani, secilen.EnYuksekGuven);
            sonuc.NihaiSonuc = secilen.Plaka;
        }
    }

    private void YakinPlakaOnerisiEkle(PlakaOkumaSonucu sonuc)
    {
        if (sonuc.BasariliMi && sonuc.GuvenOrani >= DusukGuvenOneriEsigi)
        {
            return;
        }

        var adaylar = OneriAdaylariniOlustur(sonuc).ToList();
        if (adaylar.Count == 0)
        {
            return;
        }

        try
        {
            var plakalar = _aracRepository.TumPlakalariGetir();
            var enYakin = plakalar
                .SelectMany(plaka => adaylar.Select(aday => new
                {
                    Plaka = plaka,
                    Aday = aday,
                    Mesafe = LevenshteinMesafesi(aday, plaka)
                }))
                .OrderBy(x => x.Mesafe)
                .ThenBy(x => Math.Abs(x.Plaka.Length - x.Aday.Length))
                .FirstOrDefault();

            if (enYakin is null || adaylar.Any(a => string.Equals(enYakin.Plaka, a, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var izinliMesafe = enYakin.Aday.Length >= 7 ? 3 : 4;
            if (enYakin.Mesafe <= izinliMesafe)
            {
                sonuc.YakinPlakaOnerisi = enYakin.Plaka;
                sonuc.YakinPlakaMesafesi = enYakin.Mesafe;
                sonuc.Aciklama = string.IsNullOrWhiteSpace(sonuc.Aciklama)
                    ? $"OCR sonucu dusuk guvenli. {enYakin.Plaka} plakasi olabilir."
                    : $"{sonuc.Aciklama} En yakin kayitli plaka: {enYakin.Plaka}.";
            }
        }
        catch
        {
            // Veritabani onerisi yardimci ozelliktir; baglanti hatasi plaka okuma akisini durdurmaz.
        }
    }

    private static IEnumerable<string> OneriAdaylariniOlustur(PlakaOkumaSonucu sonuc)
    {
        var kaynaklar = new[]
        {
            sonuc.NihaiSonuc,
            sonuc.DuzeltilmisPlaka,
            sonuc.PythonSonucu,
            sonuc.SablonSonucu,
            sonuc.TesseractSonucu,
            sonuc.OkunanMetin
        };

        var adaylar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kaynak in kaynaklar)
        {
            var temiz = PlakaDogrulamaYardimcisi.Temizle(kaynak);
            if (temiz.Length >= 5)
            {
                adaylar.Add(temiz);
            }

            var gorsel = GorselRakamVaryanti(temiz);
            if (gorsel.Length >= 5)
            {
                adaylar.Add(gorsel);
            }
        }

        return adaylar;
    }

    private static string GorselRakamVaryanti(string metin)
    {
        var temiz = PlakaDogrulamaYardimcisi.Temizle(metin);
        var karakterler = temiz.Select(karakter => karakter switch
        {
            'O' or 'Q' or 'D' => '0',
            'I' or 'L' => '4',
            'R' => '3',
            'Z' => '2',
            'S' => '5',
            'U' or 'A' => '4',
            'H' or 'T' => '7',
            'B' => '8',
            _ => karakter
        });

        return new string(karakterler.ToArray());
    }

    private static int LevenshteinMesafesi(string kaynak, string hedef)
    {
        if (kaynak.Length == 0)
        {
            return hedef.Length;
        }

        if (hedef.Length == 0)
        {
            return kaynak.Length;
        }

        var onceki = new int[hedef.Length + 1];
        var simdiki = new int[hedef.Length + 1];

        for (var j = 0; j <= hedef.Length; j++)
        {
            onceki[j] = j;
        }

        for (var i = 1; i <= kaynak.Length; i++)
        {
            simdiki[0] = i;
            for (var j = 1; j <= hedef.Length; j++)
            {
                var maliyet = kaynak[i - 1] == hedef[j - 1] ? 0 : 1;
                simdiki[j] = Math.Min(
                    Math.Min(simdiki[j - 1] + 1, onceki[j] + 1),
                    onceki[j - 1] + maliyet);
            }

            (onceki, simdiki) = (simdiki, onceki);
        }

        return onceki[hedef.Length];
    }

    private void KayitEkle(PlakaOkumaSonucu sonuc)
    {
        try
        {
            _plakaOkumaKaydiRepository.Ekle(new PlakaOkumaKaydi
            {
                OkunanMetin = sonuc.OkunanMetin,
                DuzeltilmisPlaka = string.IsNullOrWhiteSpace(sonuc.DuzeltilmisPlaka) ? null : sonuc.DuzeltilmisPlaka,
                GuvenOrani = sonuc.GuvenOrani,
                ResimYolu = sonuc.ResimYolu,
                IslemTuru = sonuc.IslemTuru,
                BasariliMi = sonuc.BasariliMi,
                Aciklama = sonuc.Aciklama
            });
        }
        catch
        {
            // Demo akisi veritabani logu yazilamasa bile OCR sonucunu kullaniciya gostermeye devam eder.
        }
    }

    private string? TemsilciResimKaydet(Image image, bool roiKullan)
    {
        try
        {
            using var kaydedilecek = roiKullan ? _plakaOnIslemeServisi.RoiKirp(image) : new Bitmap(image);
            var klasor = Path.Combine(AppContext.BaseDirectory, "Resources");
            Directory.CreateDirectory(klasor);
            var yol = Path.Combine(klasor, $"plaka_okuma_{DateTime.Now:yyyyMMdd_HHmmss_fff}.jpg");
            kaydedilecek.Save(yol, System.Drawing.Imaging.ImageFormat.Jpeg);
            return yol;
        }
        catch
        {
            return null;
        }
    }

    private sealed class TesseractMotoru : IDisposable
    {
        private readonly TesseractEngine _engine;

        private TesseractMotoru(TesseractEngine engine)
        {
            _engine = engine;
        }

        public static TesseractMotoru Olustur()
        {
            var tessDataPath = TessDataPathAl();
            EngDilDosyasiniDogrula(tessDataPath);

            var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
            engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
            engine.SetVariable("preserve_interword_spaces", "0");

            return new TesseractMotoru(engine);
        }

        public (string Metin, decimal GuvenOrani) Oku(Bitmap bitmap)
        {
            var imagePath = Path.Combine(Path.GetTempPath(), $"plaka_ocr_{Guid.NewGuid():N}.png");

            try
            {
                bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);

                using var img = Pix.LoadFromFile(imagePath);
                using var page = _engine.Process(img, PageSegMode.SingleLine);

                var text = page.GetText()?.Trim() ?? string.Empty;
                var confidence = Math.Round((decimal)page.GetMeanConfidence() * 100M, 2);
                return (text, confidence);
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
                    // Gecici OCR dosyasi silinemezse demo akisini durdurmayiz.
                }
            }
        }

        public void Dispose()
        {
            _engine.Dispose();
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

        private static void EngDilDosyasiniDogrula(string tessDataPath)
        {
            var engPath = Path.Combine(tessDataPath, "eng.traineddata");
            if (!File.Exists(engPath))
            {
                throw new FileNotFoundException(
                    $"Tesseract OCR icin eng.traineddata dosyasi bulunamadi. Lutfen dosyayi su klasore ekleyin: {tessDataPath}",
                    engPath);
            }
        }
    }

    private sealed record PythonAdaySecimi(
        string Plaka,
        decimal GuvenOrani,
        bool GecerliMi,
        string Kaynak,
        string ReddedilmeSebebi,
        int Tekrar);
}
