using PlakaTanimaliOtoparkOtomasyonu.DataAccess;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;
using PlakaTanimaliOtoparkOtomasyonu.Models;

namespace PlakaTanimaliOtoparkOtomasyonu.Services;

public class GirisServisi
{
    private readonly KullaniciRepository _kullaniciRepository = new();
    private readonly IlkKurulumServisi _ilkKurulumServisi;

    public GirisServisi()
    {
        _ilkKurulumServisi = new IlkKurulumServisi(_kullaniciRepository);
    }

    public Kullanici? GirisYap(string kullaniciAdi, string sifre)
    {
        if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrEmpty(sifre))
        {
            return null;
        }

        _ilkKurulumServisi.IlkAdminiGerekirseOlustur();

        var kullanici = _kullaniciRepository.KullaniciAdinaGoreGetir(kullaniciAdi.Trim());
        if (kullanici is null || !ParolaYardimcisi.Dogrula(sifre, kullanici.SifreHash))
        {
            return null;
        }

        if (ParolaYardimcisi.YenidenHashlenmeli(kullanici.SifreHash))
        {
            var yeniHash = ParolaYardimcisi.Hashle(sifre);
            if (_kullaniciRepository.SifreHashGuncelle(kullanici.KullaniciId, kullanici.SifreHash, yeniHash))
            {
                kullanici.SifreHash = yeniHash;
            }
        }

        return kullanici;
    }
}
