using System.Text.RegularExpressions;

namespace PlakaTanimaliOtoparkOtomasyonu.Helpers;

public static partial class PlakaDogrulamaYardimcisi
{
    public static string Temizle(string? plaka)
    {
        if (string.IsNullOrWhiteSpace(plaka))
        {
            return string.Empty;
        }

        return Regex.Replace(plaka.ToUpperInvariant(), "[^0-9A-Z]", string.Empty).Trim();
    }

    public static bool TurkPlakasiMi(string? plaka)
    {
        var temiz = Temizle(plaka);
        return DemoPlakaModuAktifMi()
            ? DemoTurkPlakaRegex().IsMatch(temiz)
            : TurkPlakaRegex().IsMatch(temiz);
    }

    public static bool DemoPlakaModuAktifMi()
    {
        var value = AppConfigYardimcisi.AyarAl("DemoPlakaModu", "false");
        return bool.TryParse(value, out var aktif) && aktif;
    }

    public static string KarakterHatalariniDuzelt(string? metin)
    {
        var temiz = Temizle(metin);
        if (temiz.Length < 5)
        {
            return temiz;
        }

        if (TurkPlakasiMi(temiz))
        {
            return temiz;
        }

        var enIyi = string.Empty;
        var enAzDegisim = int.MaxValue;

        var maksimumHarfUzunlugu = DemoPlakaModuAktifMi() ? 4 : 3;
        for (var harfUzunlugu = 1; harfUzunlugu <= maksimumHarfUzunlugu; harfUzunlugu++)
        {
            for (var sayiUzunlugu = 2; sayiUzunlugu <= 4; sayiUzunlugu++)
            {
                var toplamUzunluk = 2 + harfUzunlugu + sayiUzunlugu;
                if (temiz.Length != toplamUzunluk)
                {
                    continue;
                }

                var aday = DuzeltBolgelereGore(temiz, harfUzunlugu, out var degisim);
                if (TurkPlakasiMi(aday) && degisim < enAzDegisim)
                {
                    enIyi = aday;
                    enAzDegisim = degisim;
                }
            }
        }

        return string.IsNullOrWhiteSpace(enIyi) ? temiz : enIyi;
    }

    private static string DuzeltBolgelereGore(string temiz, int harfUzunlugu, out int degisim)
    {
        var karakterler = temiz.ToCharArray();
        degisim = 0;

        for (var i = 0; i < karakterler.Length; i++)
        {
            var onceki = karakterler[i];
            if (i < 2)
            {
                karakterler[i] = SayiyaDuzelt(karakterler[i]);
            }
            else if (i < 2 + harfUzunlugu)
            {
                karakterler[i] = HarfeDuzelt(karakterler[i]);
            }
            else
            {
                karakterler[i] = SayiyaDuzelt(karakterler[i]);
            }

            if (karakterler[i] != onceki)
            {
                degisim++;
            }
        }

        return new string(karakterler);
    }

    private static char SayiyaDuzelt(char karakter)
    {
        return karakter switch
        {
            'O' => '0',
            'Q' => '0',
            'D' => '0',
            'I' => '1',
            'L' => '1',
            'S' => '5',
            'B' => '8',
            _ => karakter
        };
    }

    private static char HarfeDuzelt(char karakter)
    {
        return karakter switch
        {
            '0' => 'O',
            '1' => 'I',
            '8' => 'B',
            '5' => 'S',
            _ => karakter
        };
    }

    [GeneratedRegex(@"^(0[1-9]|[1-7][0-9]|8[01])[A-Z]{1,3}[0-9]{2,4}$", RegexOptions.Compiled)]
    private static partial Regex TurkPlakaRegex();

    [GeneratedRegex(@"^(0[1-9]|[1-7][0-9]|8[01])[A-Z]{1,4}[0-9]{2,4}$", RegexOptions.Compiled)]
    private static partial Regex DemoTurkPlakaRegex();
}
