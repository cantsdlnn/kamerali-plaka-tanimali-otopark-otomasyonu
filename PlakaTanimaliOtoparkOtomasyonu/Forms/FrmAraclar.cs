using Microsoft.Data.SqlClient;
using PlakaTanimaliOtoparkOtomasyonu.DataAccess;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;
using PlakaTanimaliOtoparkOtomasyonu.Models;

namespace PlakaTanimaliOtoparkOtomasyonu.Forms;

public class FrmAraclar : Form
{
    private readonly AracRepository _aracRepository = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _txtArama = TemaYardimcisi.TextBoxHazirla("Ara");
    private readonly TextBox _txtPlaka = TemaYardimcisi.TextBoxHazirla("Plaka");
    private readonly TextBox _txtMarka = TemaYardimcisi.TextBoxHazirla("Marka");
    private readonly TextBox _txtModel = TemaYardimcisi.TextBoxHazirla("Model");
    private readonly TextBox _txtRenk = TemaYardimcisi.TextBoxHazirla("Renk");
    private readonly TextBox _txtSahipAdi = TemaYardimcisi.TextBoxHazirla("Sahip adi");
    private readonly TextBox _txtTelefon = TemaYardimcisi.TextBoxHazirla("Telefon");
    private Arac? _secilenArac;

    public FrmAraclar()
    {
        Text = "Araclar";
        BackColor = TemaYardimcisi.ArkaPlan;
        ForeColor = TemaYardimcisi.Metin;
        Font = new Font("Segoe UI", 10F);

        Controls.Add(EkranOlustur());
        Load += (_, _) => AraclariYukle();
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
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

        var listeKart = TemaYardimcisi.Kart();
        listeKart.Dock = DockStyle.Fill;

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = TemaYardimcisi.Panel
        };

        _txtArama.Width = 260;
        toolbar.Controls.Add(_txtArama);

        var btnAra = TemaYardimcisi.Buton("Ara");
        btnAra.Width = 90;
        btnAra.Click += (_, _) => AraclariYukle();
        toolbar.Controls.Add(btnAra);

        var btnTemizle = TemaYardimcisi.Buton("Temizle", TemaYardimcisi.PanelKoyu);
        btnTemizle.Width = 105;
        btnTemizle.Click += (_, _) =>
        {
            _txtArama.Clear();
            AraclariYukle();
        };
        toolbar.Controls.Add(btnTemizle);

        TemaYardimcisi.GridHazirla(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.SelectionChanged += Grid_SelectionChanged;

        var baslik = TemaYardimcisi.Baslik("Arac Kayitlari", 20);
        baslik.Dock = DockStyle.Top;

        listeKart.Controls.Add(_grid);
        listeKart.Controls.Add(toolbar);
        listeKart.Controls.Add(baslik);

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

        stack.Controls.Add(TemaYardimcisi.Baslik("Arac Bilgisi", 20));
        foreach (var control in new[] { _txtPlaka, _txtMarka, _txtModel, _txtRenk, _txtSahipAdi, _txtTelefon })
        {
            control.Width = 330;
            stack.Controls.Add(control);
            stack.Controls.Add(new Panel { Width = 1, Height = 8, BackColor = TemaYardimcisi.Panel });
        }
        _txtPlaka.CharacterCasing = CharacterCasing.Upper;

        var btnEkle = TemaYardimcisi.Buton("Ekle", TemaYardimcisi.Yesil);
        btnEkle.Width = 330;
        btnEkle.Click += (_, _) => AracEkle();
        stack.Controls.Add(btnEkle);

        var btnGuncelle = TemaYardimcisi.Buton("Guncelle", TemaYardimcisi.Mavi);
        btnGuncelle.Width = 330;
        btnGuncelle.Click += (_, _) => AracGuncelle();
        stack.Controls.Add(btnGuncelle);

        var btnYeni = TemaYardimcisi.Buton("Yeni Kayit", TemaYardimcisi.PanelKoyu);
        btnYeni.Width = 330;
        btnYeni.Click += (_, _) => FormuTemizle();
        stack.Controls.Add(btnYeni);

        formKart.Controls.Add(stack);
        layout.Controls.Add(listeKart, 0, 0);
        layout.Controls.Add(formKart, 1, 0);
        return layout;
    }

    private void AraclariYukle()
    {
        try
        {
            _grid.DataSource = _aracRepository.Listele(_txtArama.Text);
        }
        catch (SqlException ex)
        {
            MessageBox.Show($"Araclar yuklenemedi. SQL scriptlerini kontrol edin.\n\n{ex.Message}", "Veritabani", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Araclar yuklenemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AracEkle()
    {
        try
        {
            var arac = FormdanAracOlustur();
            if (!PlakaDogrulamaYardimcisi.TurkPlakasiMi(arac.Plaka))
            {
                MessageBox.Show("Gecerli bir Turk plakasi girin.", "Plaka", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _aracRepository.Ekle(arac);
            MessageBox.Show("Arac kaydi eklendi.", "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
            FormuTemizle();
            AraclariYukle();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Arac eklenemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AracGuncelle()
    {
        try
        {
            if (_secilenArac is null)
            {
                MessageBox.Show("Guncellenecek araci secin.", "Arac", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var arac = FormdanAracOlustur();
            arac.AracId = _secilenArac.AracId;
            _aracRepository.Guncelle(arac);
            MessageBox.Show("Arac kaydi guncellendi.", "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
            AraclariYukle();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Arac guncellenemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private Arac FormdanAracOlustur()
    {
        return new Arac
        {
            Plaka = PlakaDogrulamaYardimcisi.Temizle(_txtPlaka.Text),
            Marka = BosMuNull(_txtMarka.Text),
            Model = BosMuNull(_txtModel.Text),
            Renk = BosMuNull(_txtRenk.Text),
            SahipAdi = BosMuNull(_txtSahipAdi.Text),
            Telefon = BosMuNull(_txtTelefon.Text)
        };
    }

    private void Grid_SelectionChanged(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow?.DataBoundItem is not Arac arac)
        {
            return;
        }

        _secilenArac = arac;
        _txtPlaka.Text = arac.Plaka;
        _txtMarka.Text = arac.Marka;
        _txtModel.Text = arac.Model;
        _txtRenk.Text = arac.Renk;
        _txtSahipAdi.Text = arac.SahipAdi;
        _txtTelefon.Text = arac.Telefon;
    }

    private void FormuTemizle()
    {
        _secilenArac = null;
        foreach (var textBox in new[] { _txtPlaka, _txtMarka, _txtModel, _txtRenk, _txtSahipAdi, _txtTelefon })
        {
            textBox.Clear();
        }
    }

    private static string? BosMuNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
