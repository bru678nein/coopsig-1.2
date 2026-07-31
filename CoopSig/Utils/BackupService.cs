using System;
using System.IO;
using System.Linq;
using CoopSig.Config;

namespace CoopSig.Utils
{
    /// <summary>
    /// Copia el archivo de base de datos a /Backups antes de abrir la
    /// aplicación (Constitución VII). Una falla de respaldo nunca bloquea
    /// el arranque: se informa en lenguaje llano y se permite continuar.
    /// </summary>
    public static class BackupService
    {
        private const string PrefijoArchivo = "base_";
        private const string FormatoFecha = "yyyyMMdd_HHmmss";

        public static ResultadoBackup EjecutarBackup()
        {
            try
            {
                var rutaOrigen = AppSettings.RutaBaseDatos;
                if (!File.Exists(rutaOrigen))
                {
                    return ResultadoBackup.Falla(
                        "No se encontró el archivo de base de datos en: " + rutaOrigen);
                }

                var carpetaBackups = AppSettings.CarpetaBackups;
                Directory.CreateDirectory(carpetaBackups);

                var extension = Path.GetExtension(rutaOrigen);
                var nombreArchivo = PrefijoArchivo + DateTime.Now.ToString(FormatoFecha) + extension;
                var rutaDestino = Path.Combine(carpetaBackups, nombreArchivo);

                File.Copy(rutaOrigen, rutaDestino, false);

                LimpiarBackupsAntiguos(carpetaBackups);

                return ResultadoBackup.Exito(rutaDestino);
            }
            catch (Exception ex)
            {
                return ResultadoBackup.Falla(
                    "No se pudo copiar la base de datos para el respaldo. Detalle: " + ex.Message);
            }
        }

        /// <summary>Conserva únicamente las últimas N copias configuradas (por defecto 30).</summary>
        private static void LimpiarBackupsAntiguos(string carpetaBackups)
        {
            var cantidadAConservar = AppSettings.CantidadBackupsAConservar;

            var archivos = new DirectoryInfo(carpetaBackups)
                .GetFiles(PrefijoArchivo + "*")
                .OrderByDescending(archivo => archivo.CreationTimeUtc)
                .ToList();

            for (var i = cantidadAConservar; i < archivos.Count; i++)
            {
                try
                {
                    archivos[i].Delete();
                }
                catch (IOException)
                {
                    // Un backup viejo que no se pudo borrar no debe interrumpir el arranque.
                }
            }
        }
    }

    /// <summary>Resultado de un intento de respaldo, con mensaje en lenguaje llano si falló.</summary>
    public class ResultadoBackup
    {
        public bool Exitoso { get; private set; }
        public string RutaArchivo { get; private set; }
        public string MensajeError { get; private set; }

        private ResultadoBackup(bool exitoso, string rutaArchivo, string mensajeError)
        {
            Exitoso = exitoso;
            RutaArchivo = rutaArchivo;
            MensajeError = mensajeError;
        }

        public static ResultadoBackup Exito(string rutaArchivo)
        {
            return new ResultadoBackup(true, rutaArchivo, null);
        }

        public static ResultadoBackup Falla(string mensajeError)
        {
            return new ResultadoBackup(false, null, mensajeError);
        }
    }
}
