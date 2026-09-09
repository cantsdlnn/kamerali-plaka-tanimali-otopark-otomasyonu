using Microsoft.Data.SqlClient;
using PlakaTanimaliOtoparkOtomasyonu.DataAccess;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;
using PlakaTanimaliOtoparkOtomasyonu.Models;

namespace PlakaTanimaliOtoparkOtomasyonu.Forms;

public class FrmParkYerleri : Form
{
    private readonly bool _sadeceAktifKayitlar;
    private readonly ParkYeriRepository _parkYeriRepository = new();
    private readonly ParkKaydiRepository _parkKaydiRepository = new();
    private readonly DataGridView _grid = new();
    private readonly FlowLayoutPanel _parkKartlari = new();

    public FrmParkYerleri(bool sadeceAktifKayitlar)
    {
        _sadeceAktifKayitlar = sadeceAktifKayitlar;
        Text = sadeceAktifKayitlar ? "Aktif Kayitlar" : "Park Yerleri";
        BackColor = TemaYardimcisi.ArkaPlan;
        ForeColor = TemaYardimcisi.Metin;
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Font;

        Controls.Add(EkranOlustur());
        Load += (_, _) => VerileriYukle();
    }

    private Control EkranOlustur()
    {
        var container = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            BackColor = TemaYardimcisi.ArkaPlan
        };

        var baslik = TemaYardimcisi.Baslik(_sadeceAktifKayitlar ? "Aktif Kayitlar" : "Park Yerleri", 22);
        baslik.Dock = DockStyle.Top;

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = TemaYardimcisi.ArkaPlan
        };

        var btnYenile = TemaYardimcisi.Buton("Yenile");
        btnYenile.Width = 120;
        btnYenile.Click += (_, _) => VerileriYukle();
        toolbar.Controls.Add(btnYenile);

        TemaYardimcisi.GridHazirla(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.DataError += (_, e) => e.ThrowException = false;

        _parkKartlari.Dock = DockStyle.Fill;
        _parkKartlari.AutoScroll = true;
        _parkKartlari.WrapContents = true;
        _parkKartlari.FlowDirection = FlowDirection.LeftToRight;
        _parkKartlari.BackColor = TemaYardimcisi.ArkaPlan;
        _parkKartlari.Padding = new Padding(2, 8, 2, 2);

        container.Controls.Add(_grid);
        container.Controls.Add(_parkKartlari);
        container.Controls.Add(toolbar);
        container.Controls.Add(baslik);
        return container;
    }

    private void VerileriYukle()
    {
        try
        {
            if (_sadeceAktifKayitlar)
            {
                _parkKartlari.Visible = false;
                _grid.Visible = true;
                _grid.DataSource = _parkKaydiRepository.AktifKayitlariGetir()
                    .Select(kayit => new
                    {
                        KayitNo = kayit.ParkKaydiId,
                        kayit.Plaka,
                        ParkYeri = kayit.ParkYeriKod,
                        GirisZamani = kayit.GirisZamani,
                        Durum = kayit.Status == "Active" ? "Aktif" : "Tamamlandi"
                    })
                    .ToList();
            }
            else
            {
                _grid.Visible = false;
                _parkKartlari.Visible = true;
                ParkKartlariniDoldur(_parkYeriRepository.Listele());
            }
        }
        catch (SqlException ex)
        {
            MessageBox.Show($"Veriler yuklenemedi. SQL scriptlerini kontrol edin.\n\n{ex.Message}", "Veritabani", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Veriler yuklenemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ParkKartlariniDoldur(IEnumerable<ParkYeri> parkYerleri)
    {
        _parkKartlari.SuspendLayout();
        _parkKartlari.Controls.Clear();

        foreach (var parkYeri in parkYerleri)
        {
            _parkKartlari.Controls.Add(ParkKartiOlustur(parkYeri));
        }

        _parkKartlari.ResumeLayout();
    }

    private static Control ParkKartiOlustur(ParkYeri parkYeri)
    {
        var dolu = parkYeri.IsOccupied;
        var panel = new Panel
        {
            Width = 176,
            Height = 128,
            Margin = new Padding(8),
            Padding = new Padding(14, 12, 14, 12),
            BackColor = dolu ? Color.FromArgb(127, 29, 29) : Color.FromArgb(20, 83, 45)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = parkYeri.Kod,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 21F),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = $"{parkYeri.Kat} / {parkYeri.Bolge}",
            ForeColor = TemaYardimcisi.IkincilMetin,
            Font = new Font("Segoe UI", 9F),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        }, 0, 1);

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = dolu ? "DOLU" : "BOS",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10F),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 2);

        panel.Controls.Add(layout);

        return panel;
    }
}
