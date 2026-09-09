using System.Data;
using Microsoft.Data.SqlClient;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;
using PlakaTanimaliOtoparkOtomasyonu.Models;

namespace PlakaTanimaliOtoparkOtomasyonu.DataAccess;

public class ParkKaydiRepository : BaseRepository
{
    public int GirisKaydet(string plaka, int parkYeriId, int kullaniciId)
    {
        using var connection = BaglantiOlustur();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var temizPlaka = PlakaDogrulamaYardimcisi.Temizle(plaka);
            var aracId = AracIdGetirVeyaOlustur(connection, transaction, temizPlaka);

            using (var activeCheck = new SqlCommand("""
                SELECT COUNT(1)
                FROM dbo.ParkKayitlari pk
                WHERE pk.AracId = @AracId AND pk.Status = N'Active';
                """, connection, transaction))
            {
                activeCheck.Parameters.AddWithValue("@AracId", aracId);
                if (Convert.ToInt32(activeCheck.ExecuteScalar()) > 0)
                {
                    throw new InvalidOperationException("Bu plaka icin aktif bir giris kaydi zaten var.");
                }
            }

            using (var spotCheck = new SqlCommand("""
                SELECT IsOccupied
                FROM dbo.ParkYerleri
                WHERE ParkYeriId = @ParkYeriId AND AktifMi = 1;
                """, connection, transaction))
            {
                spotCheck.Parameters.AddWithValue("@ParkYeriId", parkYeriId);
                var occupied = spotCheck.ExecuteScalar();
                if (occupied is null)
                {
                    throw new InvalidOperationException("Secilen park yeri bulunamadi.");
                }

                if (Convert.ToBoolean(occupied))
                {
                    throw new InvalidOperationException("Secilen park yeri dolu gorunuyor.");
                }
            }

            int kayitId;
            using (var insert = new SqlCommand("""
                INSERT INTO dbo.ParkKayitlari (AracId, ParkYeriId, GirisZamani, Status, CreatedByUserId)
                OUTPUT INSERTED.ParkKaydiId
                VALUES (@AracId, @ParkYeriId, SYSDATETIME(), N'Active', @CreatedByUserId);
                """, connection, transaction))
            {
                insert.Parameters.AddWithValue("@AracId", aracId);
                insert.Parameters.AddWithValue("@ParkYeriId", parkYeriId);
                insert.Parameters.AddWithValue("@CreatedByUserId", kullaniciId);
                kayitId = Convert.ToInt32(insert.ExecuteScalar());
            }

            using (var updateSpot = new SqlCommand("""
                UPDATE dbo.ParkYerleri
                SET IsOccupied = 1
                WHERE ParkYeriId = @ParkYeriId;
                """, connection, transaction))
            {
                updateSpot.Parameters.AddWithValue("@ParkYeriId", parkYeriId);
                updateSpot.ExecuteNonQuery();
            }

            transaction.Commit();
            return kayitId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void CikisKaydet(int parkKaydiId, decimal toplamUcret, string odemeTipi, int kullaniciId)
    {
        using var connection = BaglantiOlustur();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            int parkYeriId;
            using (var getRecord = new SqlCommand("""
                SELECT ParkYeriId
                FROM dbo.ParkKayitlari
                WHERE ParkKaydiId = @ParkKaydiId AND Status = N'Active';
                """, connection, transaction))
            {
                getRecord.Parameters.AddWithValue("@ParkKaydiId", parkKaydiId);
                var value = getRecord.ExecuteScalar();
                if (value is null)
                {
                    throw new InvalidOperationException("Aktif park kaydi bulunamadi.");
                }

                parkYeriId = Convert.ToInt32(value);
            }

            using (var updateRecord = new SqlCommand("""
                UPDATE dbo.ParkKayitlari
                SET Status = N'Completed',
                    CikisZamani = SYSDATETIME(),
                    TotalFee = @TotalFee,
                    CompletedByUserId = @CompletedByUserId
                WHERE ParkKaydiId = @ParkKaydiId;
                """, connection, transaction))
            {
                updateRecord.Parameters.AddWithValue("@ParkKaydiId", parkKaydiId);
                updateRecord.Parameters.AddWithValue("@TotalFee", toplamUcret);
                updateRecord.Parameters.AddWithValue("@CompletedByUserId", kullaniciId);
                updateRecord.ExecuteNonQuery();
            }

            using (var updateSpot = new SqlCommand("""
                UPDATE dbo.ParkYerleri
                SET IsOccupied = 0
                WHERE ParkYeriId = @ParkYeriId;
                """, connection, transaction))
            {
                updateSpot.Parameters.AddWithValue("@ParkYeriId", parkYeriId);
                updateSpot.ExecuteNonQuery();
            }

            using (var insertPayment = new SqlCommand("""
                INSERT INTO dbo.Odemeler (ParkKaydiId, Tutar, OdemeTipi, KullaniciId)
                VALUES (@ParkKaydiId, @Tutar, @OdemeTipi, @KullaniciId);
                """, connection, transaction))
            {
                insertPayment.Parameters.AddWithValue("@ParkKaydiId", parkKaydiId);
                insertPayment.Parameters.AddWithValue("@Tutar", toplamUcret);
                insertPayment.Parameters.AddWithValue("@OdemeTipi", odemeTipi);
                insertPayment.Parameters.AddWithValue("@KullaniciId", kullaniciId);
                insertPayment.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public ParkKaydi? AktifKaydiPlakayaGoreGetir(string plaka)
    {
        const string sql = """
            SELECT TOP 1 pk.ParkKaydiId, pk.AracId, pk.ParkYeriId, a.Plaka, py.Kod AS ParkYeriKod,
                   pk.GirisZamani, pk.CikisZamani, pk.Status, pk.TotalFee,
                   pk.CreatedByUserId, pk.CompletedByUserId, pk.OlusturmaTarihi
            FROM dbo.ParkKayitlari pk
            INNER JOIN dbo.Araclar a ON a.AracId = pk.AracId
            INNER JOIN dbo.ParkYerleri py ON py.ParkYeriId = pk.ParkYeriId
            WHERE a.Plaka = @Plaka AND pk.Status = N'Active'
            ORDER BY pk.GirisZamani DESC;
            """;

        var table = TabloGetir(sql, Parametre("@Plaka", PlakaDogrulamaYardimcisi.Temizle(plaka)));
        return table.Rows.Count == 0 ? null : Map(table.Rows[0]);
    }

    public List<ParkKaydi> AktifKayitlariGetir()
    {
        const string sql = """
            SELECT pk.ParkKaydiId, pk.AracId, pk.ParkYeriId, a.Plaka, py.Kod AS ParkYeriKod,
                   pk.GirisZamani, pk.CikisZamani, pk.Status, pk.TotalFee,
                   pk.CreatedByUserId, pk.CompletedByUserId, pk.OlusturmaTarihi
            FROM dbo.ParkKayitlari pk
            INNER JOIN dbo.Araclar a ON a.AracId = pk.AracId
            INNER JOIN dbo.ParkYerleri py ON py.ParkYeriId = pk.ParkYeriId
            WHERE pk.Status = N'Active'
            ORDER BY pk.GirisZamani DESC;
            """;

        return TabloGetir(sql).Rows.Cast<DataRow>().Select(Map).ToList();
    }

    public DataTable RaporGetir(DateTime baslangic, DateTime bitis)
    {
        const string sql = """
            SELECT pk.ParkKaydiId AS [Kayit No],
                   a.Plaka,
                   py.Kod AS [Park Yeri],
                   pk.GirisZamani AS [Giris Zamani],
                   pk.CikisZamani AS [Cikis Zamani],
                   DATEDIFF(MINUTE, pk.GirisZamani, ISNULL(pk.CikisZamani, SYSDATETIME())) AS [Sure Dakika],
                   pk.Status AS Durum,
                   ISNULL(pk.TotalFee, 0) AS [Toplam Ucret]
            FROM dbo.ParkKayitlari pk
            INNER JOIN dbo.Araclar a ON a.AracId = pk.AracId
            INNER JOIN dbo.ParkYerleri py ON py.ParkYeriId = pk.ParkYeriId
            WHERE pk.GirisZamani >= @Baslangic AND pk.GirisZamani < @Bitis
            ORDER BY pk.GirisZamani DESC;
            """;

        return TabloGetir(sql, Parametre("@Baslangic", baslangic), Parametre("@Bitis", bitis));
    }

    public DashboardOzet DashboardOzetGetir()
    {
        const string sql = """
            SELECT
                (SELECT COUNT(1) FROM dbo.ParkYerleri WHERE AktifMi = 1) AS ToplamKapasite,
                (SELECT COUNT(1) FROM dbo.ParkYerleri WHERE AktifMi = 1 AND IsOccupied = 1) AS DoluYer,
                (SELECT COUNT(1) FROM dbo.ParkKayitlari WHERE CAST(GirisZamani AS DATE) = CAST(SYSDATETIME() AS DATE)) AS GunlukGiris,
                (SELECT COUNT(1) FROM dbo.ParkKayitlari WHERE CAST(CikisZamani AS DATE) = CAST(SYSDATETIME() AS DATE)) AS GunlukCikis,
                (SELECT ISNULL(SUM(Tutar), 0) FROM dbo.Odemeler WHERE CAST(OdemeZamani AS DATE) = CAST(SYSDATETIME() AS DATE)) AS GunlukKazanc;
            """;

        var table = TabloGetir(sql);
        if (table.Rows.Count == 0)
        {
            return new DashboardOzet();
        }

        var row = table.Rows[0];
        return new DashboardOzet
        {
            ToplamKapasite = TamSayi(row, "ToplamKapasite"),
            DoluYer = TamSayi(row, "DoluYer"),
            GunlukGiris = TamSayi(row, "GunlukGiris"),
            GunlukCikis = TamSayi(row, "GunlukCikis"),
            GunlukKazanc = DecimalDeger(row, "GunlukKazanc")
        };
    }

    private static int AracIdGetirVeyaOlustur(SqlConnection connection, SqlTransaction transaction, string plaka)
    {
        using (var select = new SqlCommand("SELECT AracId FROM dbo.Araclar WHERE Plaka = @Plaka;", connection, transaction))
        {
            select.Parameters.AddWithValue("@Plaka", plaka);
            var existing = select.ExecuteScalar();
            if (existing is not null)
            {
                return Convert.ToInt32(existing);
            }
        }

        using var insert = new SqlCommand("""
            INSERT INTO dbo.Araclar (Plaka)
            OUTPUT INSERTED.AracId
            VALUES (@Plaka);
            """, connection, transaction);
        insert.Parameters.AddWithValue("@Plaka", plaka);
        return Convert.ToInt32(insert.ExecuteScalar());
    }

    private static ParkKaydi Map(DataRow row)
    {
        return new ParkKaydi
        {
            ParkKaydiId = TamSayi(row, "ParkKaydiId"),
            AracId = TamSayi(row, "AracId"),
            ParkYeriId = TamSayi(row, "ParkYeriId"),
            Plaka = Metin(row, "Plaka"),
            ParkYeriKod = Metin(row, "ParkYeriKod"),
            GirisZamani = Tarih(row, "GirisZamani"),
            CikisZamani = NullableTarih(row, "CikisZamani"),
            Status = Metin(row, "Status"),
            TotalFee = NullableDecimal(row, "TotalFee"),
            CreatedByUserId = TamSayi(row, "CreatedByUserId"),
            CompletedByUserId = row["CompletedByUserId"] == DBNull.Value ? null : TamSayi(row, "CompletedByUserId"),
            OlusturmaTarihi = Tarih(row, "OlusturmaTarihi")
        };
    }
}
