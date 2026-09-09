namespace PlakaTanimaliOtoparkOtomasyonu;

using PlakaTanimaliOtoparkOtomasyonu.Forms;
using PlakaTanimaliOtoparkOtomasyonu.Helpers;
using PlakaTanimaliOtoparkOtomasyonu.Services;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        TemaYardimcisi.UygulamaTemasiniHazirla();
        KameraServisi.PlakaCercevesiYukle();

        using (var yukleme = new FrmYukleme())
        {
            yukleme.ShowDialog();
        }

        Application.Run(new FrmGiris());
    }
}
