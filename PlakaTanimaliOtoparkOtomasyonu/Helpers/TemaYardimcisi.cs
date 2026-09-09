namespace PlakaTanimaliOtoparkOtomasyonu.Helpers;

public static class TemaYardimcisi
{
    public static readonly Color ArkaPlan = UIHelper.ArkaPlan;
    public static readonly Color Panel = UIHelper.Panel;
    public static readonly Color PanelKoyu = UIHelper.PanelKoyu;
    public static readonly Color Cizgi = UIHelper.Cizgi;
    public static readonly Color Metin = UIHelper.Metin;
    public static readonly Color IkincilMetin = UIHelper.IkincilMetin;
    public static readonly Color Mavi = UIHelper.Mavi;
    public static readonly Color Yesil = UIHelper.Yesil;
    public static readonly Color Kirmizi = UIHelper.Kirmizi;
    public static readonly Color Sari = UIHelper.Sari;

    public static void UygulamaTemasiniHazirla()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
    }

    public static void FormuHazirla(Form form, string baslik)
    {
        UIHelper.FormHazirla(form, baslik);
    }

    public static Label Baslik(string text, int size = 18)
    {
        var label = UIHelper.Baslik(text, size);
        label.AutoSize = true;
        label.Dock = DockStyle.None;
        return label;
    }

    public static Label Aciklama(string text)
    {
        var label = UIHelper.Aciklama(text);
        label.AutoSize = true;
        return label;
    }

    public static Panel Kart()
    {
        return UIHelper.KartPanel();
    }

    public static TextBox TextBoxHazirla(string? placeholder = null)
    {
        return UIHelper.TextBox(placeholder);
    }

    public static ComboBox ComboHazirla()
    {
        return UIHelper.ComboBox();
    }

    public static RoundedButton Buton(string text, Color? renk = null)
    {
        return UIHelper.Buton(text, renk);
    }

    public static void GridHazirla(DataGridView grid)
    {
        UIHelper.GridHazirla(grid);
    }

    public static TableLayoutPanel FormGrid(int columns = 2)
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = columns,
            RowCount = 1,
            Padding = new Padding(18),
            BackColor = ArkaPlan
        };

        for (var i = 0; i < columns; i++)
        {
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
        }

        return table;
    }
}
