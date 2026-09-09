using Microsoft.Data.SqlClient;
using PlakaTanimaliOtoparkOtomasyonu.DataAccess;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;
using PlakaTanimaliOtoparkOtomasyonu.Models;

namespace PlakaTanimaliOtoparkOtomasyonu.Forms;

public class FrmAyarlar : Form
{
    private readonly TarifeRepository _tarifeRepository = new();
    private readonly TextBox _txtTarifeAdi = TemaYardimcisi.TextBoxHazirla("Tarife adi");
    private readonly NumericUpDown _numIlkSaat = NumericOlustur();
    private readonly NumericUpDown _numSonrakiSaat = NumericOlustur();
    private readonly NumericUpDown _numGunlukMaks = NumericOlustur();
    private readonly DataGridView _grid = new();

    public FrmAyarlar()
    {
        Text = "Ayarlar";
        BackColor = TemaYardimcisi.ArkaPlan;
        ForeColor = TemaYardimcisi.Metin;
        Font = new Font("Segoe UI", 10F);

        Controls.Add(EkranOlustur());
        Load += (_, _) => TarifeleriYukle();
    }

    private Control EkranOlustur()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8),
            BackColor = TemaYardimcisi.ArkaPlan
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));

        var formKart = TemaYardimcisi.Kart();
        formKart.Dock = DockStyle.Fill;

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = TemaYardimcisi.Panel
        };

        stack.Controls.Add(TemaYardimcisi.Baslik("Tarife Ayarlari", 20));
        stack.Controls.Add(Etiket("Tarife Adi"));
        _txtTarifeAdi.Width = 320;
        stack.Controls.Add(_txtTarifeAdi);
        stack.Controls.Add(Etiket("Ilk Saat Ucreti"));
        stack.Controls.Add(_numIlkSaat);
        stack.Controls.Add(Etiket("Sonraki Saat Ucreti"));
        stack.Controls.Add(_numSonrakiSaat);
        stack.Controls.Add(Etiket("Gunluk Maksimum Ucret"));
        stack.Controls.Add(_numGunlukMaks);

        var btnKaydet = TemaYardimcisi.Buton("Aktif Tarife Olarak Kaydet", TemaYardimcisi.Yesil);
        btnKaydet.Width = 320;
        btnKaydet.Click += (_, _) => TarifeKaydet();
        stack.Controls.Add(btnKaydet);

        formKart.Controls.Add(stack);

        var listeKart = TemaYardimcisi.Kart();
        listeKart.Dock = DockStyle.Fill;
        var baslik = TemaYardimcisi.Baslik("Tarife Gecmisi", 20);
        baslik.Dock = DockStyle.Top;

        TemaYardimcisi.GridHazirla(_grid);
        _grid.Dock = DockStyle.Fill;

        listeKart.Controls.Add(_grid);
        listeKart.Controls.Add(baslik);
        layout.Controls.Add(formKart, 0, 0);
        layout.Controls.Add(listeKart, 1, 0);
        return layout;
    }

    private void TarifeleriYukle()
    {
        try
        {
            var aktif = _tarifeRepository.AktifTarifeGetir();
            if (aktif is not null)
            {
                _txtTarifeAdi.Text = aktif.TarifeAdi;
                _numIlkSaat.Value = aktif.IlkSaatUcreti;
                _numSonrakiSaat.Value = aktif.SonrakiSaatUcreti;
                _numGunlukMaks.Value = aktif.GunlukMaksimumUcret;
            }

            _grid.DataSource = _tarifeRepository.Listele();
        }
        catch (SqlException ex)
        {
            MessageBox.Show($"Tarifeler yuklenemedi. SQL scriptlerini kontrol edin.\n\n{ex.Message}", "Veritabani", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Tarifeler yuklenemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void TarifeKaydet()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_txtTarifeAdi.Text))
            {
                MessageBox.Show("Tarife adi girin.", "Tarife", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var tarife = new Tarife
            {
                TarifeAdi = _txtTarifeAdi.Text.Trim(),
                IlkSaatUcreti = _numIlkSaat.Value,
                SonrakiSaatUcreti = _numSonrakiSaat.Value,
                GunlukMaksimumUcret = _numGunlukMaks.Value,
                AktifMi = true
            };

            _tarifeRepository.AktifTarifeKaydet(tarife);
            MessageBox.Show("Tarife kaydedildi.", "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
            TarifeleriYukle();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Tarife kaydedilemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Label Etiket(string text)
    {
        return new Label
        {
            AutoSize = false,
            Width = 320,
            Height = 28,
            Text = text,
            ForeColor = TemaYardimcisi.IkincilMetin,
            Font = new Font("Segoe UI Semibold", 9F)
        };
    }

    private static NumericUpDown NumericOlustur()
    {
        return new NumericUpDown
        {
            Width = 320,
            Height = 36,
            Minimum = 0,
            Maximum = 100000,
            DecimalPlaces = 2,
            Increment = 5,
            BackColor = TemaYardimcisi.PanelKoyu,
            ForeColor = TemaYardimcisi.Metin,
            Font = new Font("Segoe UI", 11F)
        };
    }
}
