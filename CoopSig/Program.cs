using System;
using System.Windows.Forms;
using CoopSig.Forms;
using CoopSig.Utils;

namespace CoopSig
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // El respaldo corre antes de abrir el formulario principal
            // (Constitución VII). Una falla de respaldo nunca bloquea el arranque.
            EjecutarBackupInicial();

            Application.Run(new FrmPrincipal());
        }

        private static void EjecutarBackupInicial()
        {
            try
            {
                var resultado = BackupService.EjecutarBackup();
                if (!resultado.Exitoso)
                {
                    AvisarFallaDeBackup(resultado.MensajeError);
                }
            }
            catch (Exception ex)
            {
                // Cualquier falla inesperada del respaldo se informa en lenguaje
                // llano y la aplicación continúa (Constitución VII).
                AvisarFallaDeBackup(ex.Message);
            }
        }

        private static void AvisarFallaDeBackup(string detalle)
        {
            MessageBox.Show(
                "No se pudo generar la copia de seguridad de la base de datos." +
                Environment.NewLine + Environment.NewLine +
                detalle +
                Environment.NewLine + Environment.NewLine +
                "La aplicación va a continuar funcionando normalmente.",
                "Aviso de respaldo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
