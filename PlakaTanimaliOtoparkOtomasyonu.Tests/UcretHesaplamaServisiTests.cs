using PlakaTanimaliOtoparkOtomasyonu.Models;
using PlakaTanimaliOtoparkOtomasyonu.Services;

namespace PlakaTanimaliOtoparkOtomasyonu.Tests;

public class UcretHesaplamaServisiTests
{
    private readonly UcretHesaplamaServisi _servis = new();

    private static Tarife TarifeOlustur() => new()
    {
        IlkSaatUcreti = 50M,
        SonrakiSaatUcreti = 25M,
        GunlukMaksimumUcret = 250M
    };

    [Fact]
    public void Hesapla_BirSaattenAzSureyiBirSaatOlarakUcretlendirir()
    {
        var giris = new DateTime(2026, 9, 9, 10, 0, 0);
        var kayit = new ParkKaydi { GirisZamani = giris };

        var sonuc = _servis.Hesapla(kayit, TarifeOlustur(), giris.AddMinutes(20));

        Assert.Equal(1, sonuc.UcretlendirilenSaat);
        Assert.Equal(50M, sonuc.Ucret);
    }

    [Fact]
    public void Hesapla_KismiSaatiYukariYuvarlar()
    {
        var giris = new DateTime(2026, 9, 9, 10, 0, 0);
        var kayit = new ParkKaydi { GirisZamani = giris };

        var sonuc = _servis.Hesapla(kayit, TarifeOlustur(), giris.AddMinutes(61));

        Assert.Equal(2, sonuc.UcretlendirilenSaat);
        Assert.Equal(75M, sonuc.Ucret);
    }

    [Fact]
    public void Hesapla_BirGunlukUstSiniriUygular()
    {
        var giris = new DateTime(2026, 9, 9, 10, 0, 0);
        var kayit = new ParkKaydi { GirisZamani = giris };

        var sonuc = _servis.Hesapla(kayit, TarifeOlustur(), giris.AddHours(24));

        Assert.Equal(24, sonuc.UcretlendirilenSaat);
        Assert.Equal(250M, sonuc.Ucret);
    }

    [Fact]
    public void Hesapla_IkinciGunIcinUstSiniriArtirir()
    {
        var giris = new DateTime(2026, 9, 9, 10, 0, 0);
        var kayit = new ParkKaydi { GirisZamani = giris };

        var sonuc = _servis.Hesapla(kayit, TarifeOlustur(), giris.AddHours(25));

        Assert.Equal(25, sonuc.UcretlendirilenSaat);
        Assert.Equal(500M, sonuc.Ucret);
    }
}
