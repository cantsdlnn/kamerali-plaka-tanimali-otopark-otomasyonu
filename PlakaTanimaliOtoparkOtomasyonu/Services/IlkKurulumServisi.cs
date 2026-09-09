using PlakaTanimaliOtoparkOtomasyonu.DataAccess;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;

namespace PlakaTanimaliOtoparkOtomasyonu.Services;

public sealed class IlkKurulumServisi
{
    public const string KullaniciAdiDegiskeni = "OTOPARK_BOOTSTRAP_ADMIN_USER";
    public const string ParolaDegiskeni = "OTOPARK_BOOTSTRAP_ADMIN_PASSWORD";
    public const string AdSoyadDegiskeni = "OTOPARK_BOOTSTRAP_ADMIN_NAME";

    private readonly KullaniciRepository _kullaniciRepository;

    public IlkKurulumServisi(KullaniciRepository kullaniciRepository)
    {
        _kullaniciRepository = kullaniciRepository;
    }

    public void IlkAdminiGerekirseOlustur()
    {
        if (_kullaniciRepository.KullaniciVarMi())
        {
            return;
        }

        var kullaniciAdi = Environment.GetEnvironmentVariable(KullaniciAdiDegiskeni)?.Trim();
        var sifre = Environment.GetEnvironmentVariable(ParolaDegiskeni);
        var adSoyad = Environment.GetEnvironmentVariable(AdSoyadDegiskeni)?.Trim();

        if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrEmpty(sifre))
        {
            throw new IlkKurulumGerekliException(
                $"Ilk yonetici hesabi henuz olusturulmadi. {KullaniciAdiDegiskeni} ve " +
                $"{ParolaDegiskeni} ortam degiskenlerini ayarlayip uygulamayi yeniden baslatin.");
        }

        if (kullaniciAdi.Length > 50)
        {
            throw new IlkKurulumGerekliException("Ilk yonetici kullanici adi en fazla 50 karakter olabilir.");
        }

        if (!ParolaYardimcisi.GucluParolaMi(sifre))
        {
            throw new IlkKurulumGerekliException(
                $"{ParolaDegiskeni} en az 12 karakter olmali ve hem harf hem rakam icermelidir.");
        }

        adSoyad = string.IsNullOrWhiteSpace(adSoyad) ? "Yerel Yonetici" : adSoyad;
        if (adSoyad.Length > 100)
        {
            throw new IlkKurulumGerekliException("Ilk yonetici adi en fazla 100 karakter olabilir.");
        }

        _kullaniciRepository.IlkAdminiOlustur(kullaniciAdi, ParolaYardimcisi.Hashle(sifre), adSoyad);
    }
}

public sealed class IlkKurulumGerekliException : InvalidOperationException
{
    public IlkKurulumGerekliException(string message) : base(message)
    {
    }
}
