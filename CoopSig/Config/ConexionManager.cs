using System;
using System.Data.OleDb;
using Microsoft.Win32;

namespace CoopSig.Config
{
    /// <summary>
    /// Resuelve el proveedor OleDb disponible una única vez al arrancar,
    /// probando ACE.OLEDB.16.0 y luego 12.0, y cachea el resultado para
    /// toda la sesión. La ruta del .mdb sale de App.config (Constitución VIII).
    /// </summary>
    internal static class ConexionManager
    {
        private const string ProveedorPreferido = "Microsoft.ACE.OLEDB.16.0";
        private const string ProveedorAlternativo = "Microsoft.ACE.OLEDB.12.0";

        private static string _proveedorResuelto;
        private static readonly object CandadoResolucion = new object();

        public static OleDbConnection CrearConexion()
        {
            return new OleDbConnection(ObtenerConnectionString());
        }

        private static string ObtenerConnectionString()
        {
            var proveedor = ResolverProveedor();
            var rutaBase = AppSettings.RutaBaseDatos;
            return string.Format("Provider={0};Data Source={1};", proveedor, rutaBase);
        }

        private static string ResolverProveedor()
        {
            if (_proveedorResuelto != null)
            {
                return _proveedorResuelto;
            }

            lock (CandadoResolucion)
            {
                if (_proveedorResuelto != null)
                {
                    return _proveedorResuelto;
                }

                if (ProveedorRegistrado(ProveedorPreferido))
                {
                    _proveedorResuelto = ProveedorPreferido;
                }
                else if (ProveedorRegistrado(ProveedorAlternativo))
                {
                    _proveedorResuelto = ProveedorAlternativo;
                }
                else
                {
                    throw new InvalidOperationException(
                        "No se encontró un proveedor de datos de Access instalado " +
                        "(Microsoft.ACE.OLEDB.16.0 ni 12.0). Verifique que Office esté " +
                        "instalado en su versión de 64 bits en esta máquina.");
                }

                return _proveedorResuelto;
            }
        }

        /// <summary>
        /// Comprueba si el proveedor está registrado leyendo su ProgID en el
        /// registro. Evita el error engañoso "El proveedor no está registrado en
        /// el equipo local", que en realidad es desajuste de arquitectura, no
        /// ausencia de driver: como el proceso es x64, esta consulta ve la vista
        /// de 64 bits del registro y por lo tanto responde por la arquitectura
        /// correcta.
        ///
        /// No se usa OleDbEnumerator.GetElements() a propósito: enumera TODOS los
        /// proveedores OLE DB del equipo, y algunos (los de SQL Server) hacen
        /// descubrimiento por red al ser enumerados. Esa llamada COM puede
        /// bloquear el hilo de interfaz durante minutos y dispara el asistente de
        /// depuración 'ContextSwitchDeadlock'.
        /// </summary>
        private static bool ProveedorRegistrado(string nombreProveedor)
        {
            using (var clave = Registry.ClassesRoot.OpenSubKey(nombreProveedor))
            {
                return clave != null;
            }
        }
    }
}
