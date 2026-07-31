using System;
using System.Configuration;
using System.IO;

namespace CoopSig.Config
{
    /// <summary>
    /// Lee la configuración externa (App.config). Es lo único que difiere
    /// entre las dos instalaciones independientes (Constitución VIII):
    /// se distribuye un único ejecutable idéntico.
    /// </summary>
    internal static class AppSettings
    {
        public static string RutaBaseDatos
        {
            get
            {
                var ruta = ConfigurationManager.AppSettings["RutaBaseDatos"];
                if (string.IsNullOrWhiteSpace(ruta))
                {
                    throw new InvalidOperationException(
                        "Falta configurar 'RutaBaseDatos' en App.config.");
                }
                return ruta;
            }
        }

        public static string CarpetaBackups
        {
            get
            {
                var carpeta = ConfigurationManager.AppSettings["CarpetaBackups"];
                if (string.IsNullOrWhiteSpace(carpeta))
                {
                    // Por defecto: subcarpeta "Backups" junto a la base de datos.
                    var carpetaBase = Path.GetDirectoryName(RutaBaseDatos);
                    carpeta = Path.Combine(
                        carpetaBase ?? AppDomain.CurrentDomain.BaseDirectory, "Backups");
                }
                return carpeta;
            }
        }

        public static int CantidadBackupsAConservar
        {
            get
            {
                var valor = ConfigurationManager.AppSettings["CantidadBackupsAConservar"];
                int cantidad;
                if (!int.TryParse(valor, out cantidad) || cantidad <= 0)
                {
                    cantidad = 30;
                }
                return cantidad;
            }
        }
    }
}
