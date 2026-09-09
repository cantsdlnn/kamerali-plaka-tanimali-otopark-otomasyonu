using PlakaTanimaliOtoparkOtomasyonu.Helpers;

namespace PlakaTanimaliOtoparkOtomasyonu.Tests;

public class PlakaDogrulamaYardimcisiTests
{
    [Theory]
    [InlineData("34 abc 123", "34ABC123")]
    [InlineData("06-xy-2026", "06XY2026")]
    [InlineData("", "")]
    public void Temizle_BoslukVeNoktalamaIsaretleriniKaldirir(string girdi, string beklenen)
    {
        Assert.Equal(beklenen, PlakaDogrulamaYardimcisi.Temizle(girdi));
    }

    [Theory]
    [InlineData("01A12")]
    [InlineData("34ABC123")]
    [InlineData("81ZZ9999")]
    public void TurkPlakasiMi_GecerliPlakalariKabulEder(string plaka)
    {
        Assert.True(PlakaDogrulamaYardimcisi.TurkPlakasiMi(plaka));
    }

    [Theory]
    [InlineData("00ABC123")]
    [InlineData("82ABC123")]
    [InlineData("34ABCD12")]
    [InlineData("ABC123")]
    public void TurkPlakasiMi_GecersizPlakalariReddeder(string plaka)
    {
        Assert.False(PlakaDogrulamaYardimcisi.TurkPlakasiMi(plaka));
    }

    [Fact]
    public void KarakterHatalariniDuzelt_BolgeyeGoreOcrHatalariniDuzeltir()
    {
        Assert.Equal("06ABC123", PlakaDogrulamaYardimcisi.KarakterHatalariniDuzelt("O6ABCI23"));
    }
}
