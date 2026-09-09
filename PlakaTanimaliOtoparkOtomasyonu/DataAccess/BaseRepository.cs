using System.Data;
using Microsoft.Data.SqlClient;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;

namespace PlakaTanimaliOtoparkOtomasyonu.DataAccess;

public abstract class BaseRepository
{
    protected string ConnectionString { get; }

    protected BaseRepository()
    {
        ConnectionString = AppConfigYardimcisi.BaglantiCumlesiAl();
    }

    protected SqlConnection BaglantiOlustur() => new(ConnectionString);

    protected DataTable TabloGetir(string sql, params SqlParameter[] parameters)
    {
        using var connection = BaglantiOlustur();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        using var adapter = new SqlDataAdapter(command);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }

    protected int KomutCalistir(string sql, params SqlParameter[] parameters)
    {
        using var connection = BaglantiOlustur();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    protected object? DegerGetir(string sql, params SqlParameter[] parameters)
    {
        using var connection = BaglantiOlustur();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        connection.Open();
        var value = command.ExecuteScalar();
        return value == DBNull.Value ? null : value;
    }

    protected static SqlParameter Parametre(string name, object? value) => new(name, value ?? DBNull.Value);

    protected static string Metin(DataRow row, string column) =>
        row[column] == DBNull.Value ? string.Empty : Convert.ToString(row[column]) ?? string.Empty;

    protected static string? NullableMetin(DataRow row, string column) =>
        row[column] == DBNull.Value ? null : Convert.ToString(row[column]);

    protected static int TamSayi(DataRow row, string column) =>
        row[column] == DBNull.Value ? 0 : Convert.ToInt32(row[column]);

    protected static decimal DecimalDeger(DataRow row, string column) =>
        row[column] == DBNull.Value ? 0 : Convert.ToDecimal(row[column]);

    protected static decimal? NullableDecimal(DataRow row, string column) =>
        row[column] == DBNull.Value ? null : Convert.ToDecimal(row[column]);

    protected static bool BoolDeger(DataRow row, string column) =>
        row[column] != DBNull.Value && Convert.ToBoolean(row[column]);

    protected static DateTime Tarih(DataRow row, string column) =>
        row[column] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row[column]);

    protected static DateTime? NullableTarih(DataRow row, string column) =>
        row[column] == DBNull.Value ? null : Convert.ToDateTime(row[column]);
}
