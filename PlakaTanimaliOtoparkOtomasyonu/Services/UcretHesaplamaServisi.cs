using PlakaTanimaliOtoparkOtomasyonu.Models;

namespace PlakaTanimaliOtoparkOtomasyonu.Services;

public class UcretHesaplamaServisi
{
    public CikisHesapSonucu Hesapla(ParkKaydi kayit, Tarife tarife, DateTime? cikisZamani = null)
    {
        var cikis = cikisZamani ?? DateTime.Now;
        var sure = cikis - kayit.GirisZamani;
        if (sure.TotalMinutes < 1)
        {
            sure = TimeSpan.FromMinutes(1);
        }

        var saat = Math.Max(1, (int)Math.Ceiling(sure.TotalHours));
        var temelUcret = saat == 1
            ? tarife.IlkSaatUcreti
            : tarife.IlkSaatUcreti + ((saat - 1) * tarife.SonrakiSaatUcreti);

        var gunSayisi = Math.Max(1, (int)Math.Ceiling(saat / 24M));
        var maksimum = gunSayisi * tarife.GunlukMaksimumUcret;
        var ucret = Math.Min(temelUcret, maksimum);

        return new CikisHesapSonucu
        {
            ParkKaydi = kayit,
            Sure = sure,
            UcretlendirilenSaat = saat,
            Ucret = ucret
        };
    }
}
