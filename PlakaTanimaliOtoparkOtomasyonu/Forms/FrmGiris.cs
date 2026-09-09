using Microsoft.Data.SqlClient;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;
using PlakaTanimaliOtoparkOtomasyonu.Services;

namespace PlakaTanimaliOtoparkOtomasyonu.Forms;

public class FrmGiris : Form
{
    private readonly GirisServisi _girisServisi = new();
    private readonly TextBox _txtKullaniciAdi = TemaYardimcisi.TextBoxHazirla("Kullanici adi");
    private readonly TextBox _txtSifre = TemaYardimcisi.TextBoxHazirla("Sifre");
    private readonly Label _lblDurum = new();

    public FrmGiris()
    {
        TemaYardimcisi.FormuHazirla(this, "Giris");
        Size = new Size(980, 620);
        MinimumSize = new Size(980, 620);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = TemaYardimcisi.ArkaPlan
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));

        var brandPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = TemaYardimcisi.PanelKoyu,
            Padding = new Padding(52)
        };

        var brandStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = TemaYardimcisi.PanelKoyu
        };

        brandStack.Controls.Add(new Label
        {
            AutoSize = false,
            Width = 340,
            Height = 150,
            Text = "Plaka Tanimali\nOtopark Otomasyonu",
            Font = new Font("Segoe UI Semibold", 27F),
            ForeColor = Color.White
        });
        brandStack.Controls.Add(new Label
        {
            AutoSize = false,
            Width = 320,
            Height = 120,
            Text = "Kamera destekli plaka okuma, park yeri takibi, odeme ve rapor ekranlari tek panelde.",
            Font = new Font("Segoe UI", 12F),
            ForeColor = TemaYardimcisi.IkincilMetin
        });
        brandPanel.Controls.Add(brandStack);

        var loginPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(72, 70, 72, 72),
            BackColor = TemaYardimcisi.ArkaPlan
        };

        var formStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = TemaYardimcisi.ArkaPlan
        };

        formStack.Controls.Add(TemaYardimcisi.Baslik("Sisteme Giris", 24));
        formStack.Controls.Add(TemaYardimcisi.Aciklama("Yetkili kullanici bilgilerinizle devam edin."));

        _txtKullaniciAdi.Width = 320;
        _txtSifre.Width = 320;
        _txtSifre.UseSystemPasswordChar = true;

        formStack.Controls.Add(_txtKullaniciAdi);
        formStack.Controls.Add(Spacer(10));
        formStack.Controls.Add(_txtSifre);
        formStack.Controls.Add(Spacer(14));

        _lblDurum.AutoSize = false;
        _lblDurum.Width = 320;
        _lblDurum.Height = 44;
        _lblDurum.ForeColor = TemaYardimcisi.IkincilMetin;
        _lblDurum.TextAlign = ContentAlignment.MiddleLeft;
        _lblDurum.Font = new Font("Segoe UI", 9.5F);
        formStack.Controls.Add(_lblDurum);

        var btnGiris = TemaYardimcisi.Buton("Giris Yap");
        btnGiris.Width = 320;
        btnGiris.Height = 44;
        btnGiris.Click += BtnGiris_Click;
        formStack.Controls.Add(btnGiris);

        loginPanel.Controls.Add(formStack);
        layout.Controls.Add(brandPanel, 0, 0);
        layout.Controls.Add(loginPanel, 1, 0);
        Controls.Add(layout);

        AcceptButton = btnGiris;
    }

    private void BtnGiris_Click(object? sender, EventArgs e)
    {
        try
        {
            var kullanici = _girisServisi.GirisYap(_txtKullaniciAdi.Text, _txtSifre.Text);
            if (kullanici is null)
            {
                _lblDurum.ForeColor = TemaYardimcisi.Sari;
                _lblDurum.Text = "Kullanici adi veya sifre hatali.";
                return;
            }

            Hide();
            using var anaPanel = new FrmAnaPanel(kullanici);
            anaPanel.ShowDialog();
            Close();
        }
        catch (IlkKurulumGerekliException ex)
        {
            MessageBox.Show(ex.Message, "Ilk kurulum gerekli", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (SqlException ex)
        {
            MessageBox.Show($"SQL Server baglantisi kurulamadı.\n\nOnce SqlScripts klasorundeki scriptleri calistirin.\n\nDetay: {ex.Message}",
                "Veritabani hatasi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Giris sirasinda hata olustu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Control Spacer(int height) => new Panel { Width = 1, Height = height, BackColor = TemaYardimcisi.ArkaPlan };
}
