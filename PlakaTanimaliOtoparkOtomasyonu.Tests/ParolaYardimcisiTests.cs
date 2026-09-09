using System.Security.Cryptography;
using System.Text;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;

namespace PlakaTanimaliOtoparkOtomasyonu.Tests;

public class ParolaYardimcisiTests
{
    [Fact]
    public void Hashle_DogruParolayiDogrular_YanlisParolayiReddeder()
    {
        const string sifre = "GucluYerelParola2026";

        var hash = ParolaYardimcisi.Hashle(sifre);

        Assert.StartsWith("PBKDF2-SHA256$", hash);
        Assert.DoesNotContain(sifre, hash);
        Assert.True(ParolaYardimcisi.Dogrula(sifre, hash));
        Assert.False(ParolaYardimcisi.Dogrula("yanlis-parola", hash));
        Assert.False(ParolaYardimcisi.YenidenHashlenmeli(hash));
    }

    [Fact]
    public void Hashle_HerParolaIcinRastgeleTuzKullanir()
    {
        const string sifre = "AyniParola2026";

        var ilk = ParolaYardimcisi.Hashle(sifre);
        var ikinci = ParolaYardimcisi.Hashle(sifre);

        Assert.NotEqual(ilk, ikinci);
        Assert.True(ParolaYardimcisi.Dogrula(sifre, ilk));
        Assert.True(ParolaYardimcisi.Dogrula(sifre, ikinci));
    }

    [Fact]
    public void Dogrula_LegacySha256KaydiniTanimlarVeYukseltmeIster()
    {
        const string sifre = "eski-parola";
        var legacyHash = Convert.ToHexString(SHA256.HashData(Encoding.Unicode.GetBytes(sifre)));

        Assert.True(ParolaYardimcisi.Dogrula(sifre, legacyHash));
        Assert.True(ParolaYardimcisi.YenidenHashlenmeli(legacyHash));
    }

    [Theory]
    [InlineData("OnIkiKarakter1", true)]
    [InlineData("yalnizcaharficerir", false)]
    [InlineData("kisa1", false)]
    public void GucluParolaMi_BasitBootstrapPolitikasiniUygular(string sifre, bool beklenen)
    {
        Assert.Equal(beklenen, ParolaYardimcisi.GucluParolaMi(sifre));
    }
}
