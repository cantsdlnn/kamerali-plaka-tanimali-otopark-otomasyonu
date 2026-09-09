using System.Data;
using PlakaTanimaliOtoparkOtomasyonu.Models;

namespace PlakaTanimaliOtoparkOtomasyonu.DataAccess;

public class ParkYeriRepository : BaseRepository
{
    public List<ParkYeri> Listele()
    {
        const string sql = """
            SELECT ParkYeriId, Kod, Kat, Bolge, IsOccupied, AktifMi
            FROM dbo.ParkYerleri
            ORDER BY Kod;
            """;

        return TabloGetir(sql).Rows.Cast<DataRow>().Select(Map).ToList();
    }

    public List<ParkYeri> BosYerleriGetir()
    {
        const string sql = """
            SELECT ParkYeriId, Kod, Kat, Bolge, IsOccupied, AktifMi
            FROM dbo.ParkYerleri
            WHERE IsOccupied = 0 AND AktifMi = 1
            ORDER BY Kod;
            """;

        return TabloGetir(sql).Rows.Cast<DataRow>().Select(Map).ToList();
    }

    public ParkYeri? IlkBosYeriGetir()
    {
        const string sql = """
            SELECT TOP 1 ParkYeriId, Kod, Kat, Bolge, IsOccupied, AktifMi
            FROM dbo.ParkYerleri
            WHERE IsOccupied = 0 AND AktifMi = 1
            ORDER BY Kod;
            """;

        var table = TabloGetir(sql);
        return table.Rows.Count == 0 ? null : Map(table.Rows[0]);
    }

    public void DurumGuncelle(int parkYeriId, bool doluMu)
    {
        const string sql = "UPDATE dbo.ParkYerleri SET IsOccupied = @IsOccupied WHERE ParkYeriId = @ParkYeriId;";
        KomutCalistir(sql, Parametre("@IsOccupied", doluMu), Parametre("@ParkYeriId", parkYeriId));
    }

    private static ParkYeri Map(DataRow row)
    {
        return new ParkYeri
        {
            ParkYeriId = TamSayi(row, "ParkYeriId"),
            Kod = Metin(row, "Kod"),
            Kat = Metin(row, "Kat"),
            Bolge = Metin(row, "Bolge"),
            IsOccupied = BoolDeger(row, "IsOccupied"),
            AktifMi = BoolDeger(row, "AktifMi")
        };
    }
}
