using System.Data;
using Microsoft.Data.SqlClient;
using PlakaTanimaliOtoparkOtomasyonu.DataAccess;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;

namespace PlakaTanimaliOtoparkOtomasyonu.Forms;

public class FrmRaporlar : Form
{
    private readonly ParkKaydiRepository _parkKaydiRepository = new();
    private readonly DateTimePicker _dtpBaslangic = new();
    private readonly DateTimePicker _dtpBitis = new();
    private readonly DataGridView _grid = new();
    private readonly Label _lblToplam = new();
    private readonly Label _lblKayitSayisi = new();
    private readonly Label _lblAktifSayisi = new();

    public FrmRaporlar()
    {
        Text = "Raporlar";
        BackColor = TemaYardimcisi.ArkaPlan;
        ForeColor = TemaYardimcisi.Metin;
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Font;

        Controls.Add(EkranOlustur());
        Load += (_, _) => RaporGetir();
    }

    private Control EkranOlustur()
    {
        var container = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            BackColor = TemaYardimcisi.ArkaPlan
        };

        var baslik = TemaYardimcisi.Baslik("Raporlar", 22);
        baslik.Dock = DockStyle.Top;

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 68,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = TemaYardimcisi.ArkaPlan
        };

        _dtpBaslangic.Width = 180;
        _dtpBaslangic.Format = DateTimePickerFormat.Short;
        _dtpBaslangic.Value = DateTime.Today;
        _dtpBitis.Width = 180;
        _dtpBitis.Format = DateTimePickerFormat.Short;
        _dtpBitis.Value = DateTime.Today;

        toolbar.Controls.Add(Etiket("Baslangic"));
        toolbar.Controls.Add(_dtpBaslangic);
        toolbar.Controls.Add(Etiket("Bitis"));
        toolbar.Controls.Add(_dtpBitis);

        var btnRapor = TemaYardimcisi.Buton("Rapor Getir");
        btnRapor.Width = 130;
        btnRapor.Click += (_, _) => RaporGetir();
        toolbar.Controls.Add(btnRapor);

        var ozetPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 86,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = TemaYardimcisi.ArkaPlan,
            Padding = new Padding(0, 8, 0, 0)
        };
        ozetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        ozetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        ozetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        ozetPanel.Controls.Add(OzetKarti("Toplam Gelir", _lblToplam, TemaYardimcisi.Yesil), 0, 0);
        ozetPanel.Controls.Add(OzetKarti("Toplam Kayit", _lblKayitSayisi, TemaYardimcisi.Mavi), 1, 0);
        ozetPanel.Controls.Add(OzetKarti("Aktif Kayit", _lblAktifSayisi, TemaYardimcisi.Sari), 2, 0);

        TemaYardimcisi.GridHazirla(_grid);
        _grid.Dock = DockStyle.Fill;

        container.Controls.Add(_grid);
        container.Controls.Add(ozetPanel);
        container.Controls.Add(toolbar);
        container.Controls.Add(baslik);
        return container;
    }

    private void RaporGetir()
    {
        try
        {
            var baslangic = _dtpBaslangic.Value.Date;
            var bitis = _dtpBitis.Value.Date.AddDays(1);
            var table = _parkKaydiRepository.RaporGetir(baslangic, bitis);
            _grid.DataSource = table;
            _lblToplam.Text = $"{KazancTopla(table):C2}";
            _lblKayitSayisi.Text = table.Rows.Count.ToString();
            _lblAktifSayisi.Text = table.Rows
                .Cast<DataRow>()
                .Count(row => string.Equals(
                    Convert.ToString(row["Durum"]),
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
                .ToString();
        }
        catch (SqlException ex)
        {
            MessageBox.Show($"Rapor yuklenemedi. SQL scriptlerini kontrol edin.\n\n{ex.Message}", "Veritabani", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Rapor yuklenemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Control OzetKarti(string baslik, Label degerLabel, Color renk)
    {
        var panel = TemaYardimcisi.Kart();
        panel.Dock = DockStyle.Fill;
        panel.Margin = new Padding(0, 0, 10, 0);
        panel.Padding = new Padding(14, 10, 14, 10);

        degerLabel.Dock = DockStyle.Fill;
        degerLabel.ForeColor = Color.White;
        degerLabel.Font = new Font("Segoe UI Semibold", 16F);
        degerLabel.TextAlign = ContentAlignment.MiddleLeft;

        panel.Controls.Add(degerLabel);
        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = baslik,
            ForeColor = TemaYardimcisi.IkincilMetin,
            Font = new Font("Segoe UI", 9.5F),
            TextAlign = ContentAlignment.MiddleLeft
        });
        panel.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 5, BackColor = renk });
        return panel;
    }

    private static decimal KazancTopla(DataTable table)
    {
        decimal toplam = 0;
        foreach (DataRow row in table.Rows)
        {
            toplam += row["Toplam Ucret"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Toplam Ucret"]);
        }

        return toplam;
    }

    private static Label Etiket(string text)
    {
        return new Label
        {
            AutoSize = false,
            Width = 82,
            Height = 36,
            Text = text,
            ForeColor = TemaYardimcisi.IkincilMetin,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }
}
