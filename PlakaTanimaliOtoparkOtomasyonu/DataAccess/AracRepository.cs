using System.Data;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;
using PlakaTanimaliOtoparkOtomasyonu.Models;

namespace PlakaTanimaliOtoparkOtomasyonu.DataAccess;

public class AracRepository : BaseRepository
{
    public List<Arac> Listele(string? arama = null)
    {
        const string sql = """
            SELECT AracId, Plaka, Marka, Model, Renk, SahipAdi, Telefon, KayitTarihi
            FROM dbo.Araclar
            WHERE @Arama IS NULL
               OR Plaka LIKE @AramaLike
               OR Marka LIKE @AramaLike
               OR Model LIKE @AramaLike
               OR SahipAdi LIKE @AramaLike
            ORDER BY KayitTarihi DESC;
            """;

        var temizArama = string.IsNullOrWhiteSpace(arama) ? null : arama.Trim();
        var table = TabloGetir(
            sql,
            Parametre("@Arama", temizArama),
            Parametre("@AramaLike", temizArama is null ? null : $"%{temizArama}%"));

        return table.Rows.Cast<DataRow>().Select(Map).ToList();
    }

    public Arac? PlakayaGoreGetir(string plaka)
    {
        const string sql = """
            SELECT AracId, Plaka, Marka, Model, Renk, SahipAdi, Telefon, KayitTarihi
            FROM dbo.Araclar
            WHERE Plaka = @Plaka;
            """;

        var table = TabloGetir(sql, Parametre("@Plaka", PlakaDogrulamaYardimcisi.Temizle(plaka)));
        return table.Rows.Count == 0 ? null : Map(table.Rows[0]);
    }

    public List<string> TumPlakalariGetir()
    {
        const string sql = """
            SELECT DISTINCT Plaka
            FROM dbo.Araclar
            WHERE Plaka IS NOT NULL AND LTRIM(RTRIM(Plaka)) <> ''
            ORDER BY Plaka;
            """;

        var table = TabloGetir(sql);
        return table.Rows
            .Cast<DataRow>()
            .Select(row => PlakaDogrulamaYardimcisi.Temizle(Metin(row, "Plaka")))
            .Where(PlakaDogrulamaYardimcisi.TurkPlakasiMi)
            .Distinct()
            .ToList();
    }

    public int Ekle(Arac arac)
    {
        const string sql = """
            INSERT INTO dbo.Araclar (Plaka, Marka, Model, Renk, SahipAdi, Telefon)
            OUTPUT INSERTED.AracId
            VALUES (@Plaka, @Marka, @Model, @Renk, @SahipAdi, @Telefon);
            """;

        var id = DegerGetir(
            sql,
            Parametre("@Plaka", PlakaDogrulamaYardimcisi.Temizle(arac.Plaka)),
            Parametre("@Marka", arac.Marka),
            Parametre("@Model", arac.Model),
            Parametre("@Renk", arac.Renk),
            Parametre("@SahipAdi", arac.SahipAdi),
            Parametre("@Telefon", arac.Telefon));

        return Convert.ToInt32(id);
    }

    public void Guncelle(Arac arac)
    {
        const string sql = """
            UPDATE dbo.Araclar
            SET Marka = @Marka,
                Model = @Model,
                Renk = @Renk,
                SahipAdi = @SahipAdi,
                Telefon = @Telefon
            WHERE AracId = @AracId;
            """;

        KomutCalistir(
            sql,
            Parametre("@AracId", arac.AracId),
            Parametre("@Marka", arac.Marka),
            Parametre("@Model", arac.Model),
            Parametre("@Renk", arac.Renk),
            Parametre("@SahipAdi", arac.SahipAdi),
            Parametre("@Telefon", arac.Telefon));
    }

    private static Arac Map(DataRow row)
    {
        return new Arac
        {
            AracId = TamSayi(row, "AracId"),
            Plaka = Metin(row, "Plaka"),
            Marka = NullableMetin(row, "Marka"),
            Model = NullableMetin(row, "Model"),
            Renk = NullableMetin(row, "Renk"),
            SahipAdi = NullableMetin(row, "SahipAdi"),
            Telefon = NullableMetin(row, "Telefon"),
            KayitTarihi = Tarih(row, "KayitTarihi")
        };
    }
}
