using System.Data;
using Microsoft.Data.SqlClient;
using PlakaTanimaliOtoparkOtomasyonu.Models;

namespace PlakaTanimaliOtoparkOtomasyonu.DataAccess;

public class KullaniciRepository : BaseRepository
{
    public Kullanici? KullaniciAdinaGoreGetir(string kullaniciAdi)
    {
        const string sql = """
            SELECT KullaniciId, KullaniciAdi, SifreHash, AdSoyad, Rol, AktifMi, OlusturmaTarihi
            FROM dbo.Kullanicilar
            WHERE KullaniciAdi = @KullaniciAdi AND AktifMi = 1;
            """;

        var table = TabloGetir(sql, Parametre("@KullaniciAdi", kullaniciAdi));
        return table.Rows.Count == 0 ? null : Map(table.Rows[0]);
    }

    public bool KullaniciVarMi()
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Kullanicilar) THEN 1 ELSE 0 END;";
        return Convert.ToInt32(DegerGetir(sql) ?? 0) == 1;
    }

    public bool IlkAdminiOlustur(string kullaniciAdi, string sifreHash, string adSoyad)
    {
        using var connection = BaglantiOlustur();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            using var command = new SqlCommand("""
                IF NOT EXISTS (SELECT 1 FROM dbo.Kullanicilar WITH (UPDLOCK, HOLDLOCK))
                BEGIN
                    INSERT INTO dbo.Kullanicilar
                        (KullaniciAdi, SifreHash, AdSoyad, Rol, AktifMi)
                    VALUES
                        (@KullaniciAdi, @SifreHash, @AdSoyad, N'Admin', 1);
                    SELECT CAST(1 AS bit);
                END
                ELSE
                BEGIN
                    SELECT CAST(0 AS bit);
                END;
                """, connection, transaction);

            command.Parameters.Add(Parametre("@KullaniciAdi", kullaniciAdi));
            command.Parameters.Add(Parametre("@SifreHash", sifreHash));
            command.Parameters.Add(Parametre("@AdSoyad", adSoyad));

            var olusturuldu = Convert.ToBoolean(command.ExecuteScalar());
            transaction.Commit();
            return olusturuldu;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool SifreHashGuncelle(int kullaniciId, string eskiHash, string yeniHash)
    {
        const string sql = """
            UPDATE dbo.Kullanicilar
            SET SifreHash = @YeniHash
            WHERE KullaniciId = @KullaniciId AND SifreHash = @EskiHash;
            """;

        return KomutCalistir(
            sql,
            Parametre("@YeniHash", yeniHash),
            Parametre("@KullaniciId", kullaniciId),
            Parametre("@EskiHash", eskiHash)) == 1;
    }

    public List<Kullanici> TumunuGetir()
    {
        const string sql = """
            SELECT KullaniciId, KullaniciAdi, SifreHash, AdSoyad, Rol, AktifMi, OlusturmaTarihi
            FROM dbo.Kullanicilar
            ORDER BY AdSoyad;
            """;

        return TabloGetir(sql).Rows.Cast<DataRow>().Select(Map).ToList();
    }

    private static Kullanici Map(DataRow row)
    {
        return new Kullanici
        {
            KullaniciId = TamSayi(row, "KullaniciId"),
            KullaniciAdi = Metin(row, "KullaniciAdi"),
            SifreHash = Metin(row, "SifreHash"),
            AdSoyad = Metin(row, "AdSoyad"),
            Rol = Metin(row, "Rol"),
            AktifMi = BoolDeger(row, "AktifMi"),
            OlusturmaTarihi = Tarih(row, "OlusturmaTarihi")
        };
    }
}
