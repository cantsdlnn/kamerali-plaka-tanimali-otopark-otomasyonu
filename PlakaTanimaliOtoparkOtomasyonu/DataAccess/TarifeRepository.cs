using System.Data;
using PlakaTanimaliOtoparkOtomasyonu.Models;

namespace PlakaTanimaliOtoparkOtomasyonu.DataAccess;

public class TarifeRepository : BaseRepository
{
    public Tarife? AktifTarifeGetir()
    {
        const string sql = """
            SELECT TOP 1 TarifeId, TarifeAdi, IlkSaatUcreti, SonrakiSaatUcreti, GunlukMaksimumUcret, AktifMi, GuncellemeTarihi
            FROM dbo.Tarifeler
            WHERE AktifMi = 1
            ORDER BY GuncellemeTarihi DESC;
            """;

        var table = TabloGetir(sql);
        return table.Rows.Count == 0 ? null : Map(table.Rows[0]);
    }

    public List<Tarife> Listele()
    {
        const string sql = """
            SELECT TarifeId, TarifeAdi, IlkSaatUcreti, SonrakiSaatUcreti, GunlukMaksimumUcret, AktifMi, GuncellemeTarihi
            FROM dbo.Tarifeler
            ORDER BY GuncellemeTarihi DESC;
            """;

        return TabloGetir(sql).Rows.Cast<DataRow>().Select(Map).ToList();
    }

    public int AktifTarifeKaydet(Tarife tarife)
    {
        const string sql = """
            UPDATE dbo.Tarifeler SET AktifMi = 0;

            INSERT INTO dbo.Tarifeler (TarifeAdi, IlkSaatUcreti, SonrakiSaatUcreti, GunlukMaksimumUcret, AktifMi)
            OUTPUT INSERTED.TarifeId
            VALUES (@TarifeAdi, @IlkSaatUcreti, @SonrakiSaatUcreti, @GunlukMaksimumUcret, 1);
            """;

        var id = DegerGetir(
            sql,
            Parametre("@TarifeAdi", tarife.TarifeAdi),
            Parametre("@IlkSaatUcreti", tarife.IlkSaatUcreti),
            Parametre("@SonrakiSaatUcreti", tarife.SonrakiSaatUcreti),
            Parametre("@GunlukMaksimumUcret", tarife.GunlukMaksimumUcret));

        return Convert.ToInt32(id);
    }

    private static Tarife Map(DataRow row)
    {
        return new Tarife
        {
            TarifeId = TamSayi(row, "TarifeId"),
            TarifeAdi = Metin(row, "TarifeAdi"),
            IlkSaatUcreti = DecimalDeger(row, "IlkSaatUcreti"),
            SonrakiSaatUcreti = DecimalDeger(row, "SonrakiSaatUcreti"),
            GunlukMaksimumUcret = DecimalDeger(row, "GunlukMaksimumUcret"),
            AktifMi = BoolDeger(row, "AktifMi"),
            GuncellemeTarihi = Tarih(row, "GuncellemeTarihi")
        };
    }
}
