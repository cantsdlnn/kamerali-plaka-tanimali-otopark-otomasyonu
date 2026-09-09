using PlakaTanimaliOtoparkOtomasyonu.DataAccess;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;
using PlakaTanimaliOtoparkOtomasyonu.Models;

namespace PlakaTanimaliOtoparkOtomasyonu.Services;

public class OtoparkServisi
{
    private readonly ParkKaydiRepository _parkKaydiRepository = new();
    private readonly TarifeRepository _tarifeRepository = new();
    private readonly UcretHesaplamaServisi _ucretHesaplamaServisi = new();

    public int AracGirisKaydet(string plaka, int parkYeriId, int kullaniciId)
    {
        var temizPlaka = PlakaDogrulamaYardimcisi.Temizle(plaka);
        if (!PlakaDogrulamaYardimcisi.TurkPlakasiMi(temizPlaka))
        {
            throw new InvalidOperationException("Plaka formati gecersiz. Ornek: 34ABC123");
        }

        return _parkKaydiRepository.GirisKaydet(temizPlaka, parkYeriId, kullaniciId);
    }

    public CikisHesapSonucu AktifKayitBulVeHesapla(string plaka)
    {
        var temizPlaka = PlakaDogrulamaYardimcisi.Temizle(plaka);
        var kayit = _parkKaydiRepository.AktifKaydiPlakayaGoreGetir(temizPlaka)
            ?? throw new InvalidOperationException("Bu plaka icin aktif park kaydi bulunamadi.");

        var tarife = _tarifeRepository.AktifTarifeGetir()
            ?? throw new InvalidOperationException("Aktif tarife bulunamadi. Ayarlar ekranindan tarife tanimlayin.");

        return _ucretHesaplamaServisi.Hesapla(kayit, tarife);
    }

    public void AracCikisKaydet(CikisHesapSonucu hesap, string odemeTipi, int kullaniciId)
    {
        if (hesap.ParkKaydi is null)
        {
            throw new InvalidOperationException("Cikis icin once aktif kayit bulunmali.");
        }

        _parkKaydiRepository.CikisKaydet(hesap.ParkKaydi.ParkKaydiId, hesap.Ucret, odemeTipi, kullaniciId);
    }
}
