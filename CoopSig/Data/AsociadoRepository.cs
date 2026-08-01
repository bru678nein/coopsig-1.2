using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using CoopSig.Config;
using CoopSig.Models;

namespace CoopSig.Data
{
    /// <summary>
    /// Acceso a datos de la tabla Asociados. Toda consulta usa parámetros
    /// OleDb, nunca concatenación de valores de usuario (Constitución VI).
    /// No existe ninguna operación DELETE (Constitución II): la baja es
    /// siempre lógica, escribiendo FechaBaja.
    /// </summary>
    public class AsociadoRepository
    {
        private const string Tabla = "Asociados";

        private const string Columnas =
            "Documento, Apellido, Nombre, CUIL, Digito, FechaNacimiento, Sexo, EstadoCivil," +
            " Direccion, Telefono, Notas, Servicio, Cargo, FechaIngreso, FechaBaja";

        /// <summary>
        /// Busca asociados por Documento (si el texto es enteramente numérico,
        /// coincidencia por prefijo) o por Apellido/Nombre (si contiene letras).
        /// Con texto vacío devuelve el padrón completo (HU-1).
        /// </summary>
        public List<Asociado> Buscar(string texto, bool incluirBajas)
        {
            texto = (texto ?? string.Empty).Trim();
            var esNumerico = EsBusquedaNumerica(texto);

            var sql = "SELECT " + Columnas + " FROM " + Tabla + " WHERE ";
            sql += esNumerico
                ? "CStr(Documento) LIKE ?"
                : "(Apellido LIKE ? OR Nombre LIKE ?)";

            if (!incluirBajas)
            {
                sql += " AND FechaBaja IS NULL";
            }

            sql += " ORDER BY Apellido, Nombre";

            using (var conexion = ConexionManager.CrearConexion())
            using (var comando = new OleDbCommand(sql, conexion))
            {
                if (esNumerico)
                {
                    comando.Parameters.AddWithValue("@documento", texto + "%");
                }
                else
                {
                    var patron = "%" + texto + "%";
                    comando.Parameters.AddWithValue("@apellido", patron);
                    comando.Parameters.AddWithValue("@nombre", patron);
                }

                conexion.Open();
                using (var lector = comando.ExecuteReader())
                {
                    var resultado = new List<Asociado>();
                    while (lector.Read())
                    {
                        resultado.Add(MapearAsociado(lector));
                    }
                    return resultado;
                }
            }
        }

        public Asociado ObtenerPorDocumento(long doc)
        {
            var sql = "SELECT " + Columnas + " FROM " + Tabla + " WHERE Documento = ?";

            using (var conexion = ConexionManager.CrearConexion())
            using (var comando = new OleDbCommand(sql, conexion))
            {
                comando.Parameters.AddWithValue("@documento", doc);
                conexion.Open();
                using (var lector = comando.ExecuteReader())
                {
                    return lector.Read() ? MapearAsociado(lector) : null;
                }
            }
        }

        public void Insertar(Asociado asociado)
        {
            // OleDb liga los parámetros por POSICIÓN, no por nombre: el orden de
            // los Add debe seguir exactamente el orden de las columnas.
            var sql =
                "INSERT INTO " + Tabla +
                " (Documento, Apellido, Nombre, CUIL, Digito, FechaNacimiento, Sexo, EstadoCivil," +
                " Direccion, Telefono, Notas, Servicio, Cargo, FechaIngreso, FechaBaja)" +
                " VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            using (var conexion = ConexionManager.CrearConexion())
            using (var comando = new OleDbCommand(sql, conexion))
            {
                comando.Parameters.AddWithValue("@documento", asociado.Documento);
                comando.Parameters.AddWithValue("@apellido", asociado.Apellido);
                comando.Parameters.AddWithValue("@nombre", asociado.Nombre);
                AgregarParametroNullable(comando, "@cuil", asociado.Cuil);
                AgregarParametroNullable(comando, "@digito", asociado.Digito);
                AgregarParametroNullable(comando, "@fechaNacimiento", asociado.FechaNacimiento);
                AgregarParametroNullable(comando, "@sexo", asociado.Sexo);
                AgregarParametroNullable(comando, "@estadoCivil", asociado.EstadoCivil);
                AgregarParametroNullable(comando, "@direccion", asociado.Direccion);
                AgregarParametroNullable(comando, "@telefono", asociado.Telefono);
                AgregarParametroTextoLargo(comando, "@notas", asociado.Notas);
                comando.Parameters.AddWithValue("@servicio", asociado.Servicio);
                AgregarParametroNullable(comando, "@cargo", asociado.Cargo);
                AgregarParametroNullable(comando, "@fechaIngreso", asociado.FechaIngreso);
                AgregarParametroNullable(comando, "@fechaBaja", asociado.FechaBaja);

                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }

        public void Actualizar(Asociado asociado)
        {
            // El parámetro del WHERE va último: OleDb liga por posición.
            var sql =
                "UPDATE " + Tabla +
                " SET Apellido = ?, Nombre = ?, CUIL = ?, Digito = ?, FechaNacimiento = ?," +
                " Sexo = ?, EstadoCivil = ?, Direccion = ?, Telefono = ?, Notas = ?," +
                " Servicio = ?, Cargo = ?, FechaIngreso = ?, FechaBaja = ? WHERE Documento = ?";

            using (var conexion = ConexionManager.CrearConexion())
            using (var comando = new OleDbCommand(sql, conexion))
            {
                comando.Parameters.AddWithValue("@apellido", asociado.Apellido);
                comando.Parameters.AddWithValue("@nombre", asociado.Nombre);
                AgregarParametroNullable(comando, "@cuil", asociado.Cuil);
                AgregarParametroNullable(comando, "@digito", asociado.Digito);
                AgregarParametroNullable(comando, "@fechaNacimiento", asociado.FechaNacimiento);
                AgregarParametroNullable(comando, "@sexo", asociado.Sexo);
                AgregarParametroNullable(comando, "@estadoCivil", asociado.EstadoCivil);
                AgregarParametroNullable(comando, "@direccion", asociado.Direccion);
                AgregarParametroNullable(comando, "@telefono", asociado.Telefono);
                AgregarParametroTextoLargo(comando, "@notas", asociado.Notas);
                comando.Parameters.AddWithValue("@servicio", asociado.Servicio);
                AgregarParametroNullable(comando, "@cargo", asociado.Cargo);
                AgregarParametroNullable(comando, "@fechaIngreso", asociado.FechaIngreso);
                AgregarParametroNullable(comando, "@fechaBaja", asociado.FechaBaja);
                comando.Parameters.AddWithValue("@documento", asociado.Documento);

                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Baja lógica: escribe FechaBaja con la fecha de hoy. Nunca DELETE
        /// (Constitución II). El registro sigue existiendo y consultable.
        /// </summary>
        public void DarDeBaja(long doc)
        {
            var sql = "UPDATE " + Tabla + " SET FechaBaja = ? WHERE Documento = ?";

            using (var conexion = ConexionManager.CrearConexion())
            using (var comando = new OleDbCommand(sql, conexion))
            {
                comando.Parameters.AddWithValue("@fechaBaja", DateTime.Today);
                comando.Parameters.AddWithValue("@documento", doc);
                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }

        /// <summary>Reactivación: limpia FechaBaja (HU-4).</summary>
        public void Reactivar(long doc)
        {
            var sql = "UPDATE " + Tabla + " SET FechaBaja = NULL WHERE Documento = ?";

            using (var conexion = ConexionManager.CrearConexion())
            using (var comando = new OleDbCommand(sql, conexion))
            {
                comando.Parameters.AddWithValue("@documento", doc);
                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }

        public bool ExisteDocumento(long doc)
        {
            var sql = "SELECT COUNT(*) FROM " + Tabla + " WHERE Documento = ?";

            using (var conexion = ConexionManager.CrearConexion())
            using (var comando = new OleDbCommand(sql, conexion))
            {
                comando.Parameters.AddWithValue("@documento", doc);
                conexion.Open();
                var cantidad = (int)comando.ExecuteScalar();
                return cantidad > 0;
            }
        }

        /// <summary>
        /// Clasifica el texto de búsqueda: enteramente numérico → Documento;
        /// contiene letras → Apellido/Nombre (HU-1).
        /// </summary>
        internal static bool EsBusquedaNumerica(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return false;
            }

            foreach (var c in texto)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determina si un asociado ya cargado en memoria coincide con el texto
        /// de búsqueda, aplicando la misma clasificación que Buscar(). Se usa
        /// para el filtrado en memoria del padrón (ver "Rendimiento", plan.md).
        /// </summary>
        public static bool Coincide(Asociado asociado, string texto)
        {
            texto = (texto ?? string.Empty).Trim();
            if (texto.Length == 0)
            {
                return true;
            }

            if (EsBusquedaNumerica(texto))
            {
                return asociado.Documento
                    .ToString(CultureInfo.InvariantCulture)
                    .StartsWith(texto, StringComparison.Ordinal);
            }

            var apellido = asociado.Apellido ?? string.Empty;
            var nombre = asociado.Nombre ?? string.Empty;
            return apellido.IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0
                || nombre.IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AgregarParametroNullable(OleDbCommand comando, string nombre, object valor)
        {
            comando.Parameters.AddWithValue(nombre, valor ?? DBNull.Value);
        }

        /// <summary>
        /// Parámetro para una columna "Texto largo" (Memo). Se declara el tipo a
        /// mano en lugar de usar AddWithValue: al inferirlo, ADO.NET lo trata
        /// como texto corto y recorta el contenido a 255 caracteres.
        /// </summary>
        private static void AgregarParametroTextoLargo(OleDbCommand comando, string nombre, string valor)
        {
            var parametro = comando.Parameters.Add(nombre, OleDbType.LongVarWChar);
            parametro.Value = string.IsNullOrEmpty(valor) ? (object)DBNull.Value : valor;
        }

        private static Asociado MapearAsociado(IDataRecord lector)
        {
            return new Asociado
            {
                Documento = Convert.ToInt64(lector["Documento"]),
                Apellido = lector["Apellido"] as string,
                Nombre = lector["Nombre"] as string,
                Cuil = LeerEnteroNullable(lector["CUIL"]),
                Digito = LeerEnteroNullable(lector["Digito"]),
                FechaNacimiento = LeerFechaNullable(lector["FechaNacimiento"]),
                Sexo = lector["Sexo"] as string,
                EstadoCivil = lector["EstadoCivil"] as string,
                Direccion = lector["Direccion"] as string,
                Telefono = lector["Telefono"] as string,
                Notas = lector["Notas"] as string,
                Servicio = lector["Servicio"] as string,
                Cargo = lector["Cargo"] as string,
                FechaIngreso = LeerFechaNullable(lector["FechaIngreso"]),
                FechaBaja = LeerFechaNullable(lector["FechaBaja"])
            };
        }

        private static int? LeerEnteroNullable(object valor)
        {
            return valor == null || valor == DBNull.Value ? (int?)null : Convert.ToInt32(valor);
        }

        private static DateTime? LeerFechaNullable(object valor)
        {
            return valor == null || valor == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(valor);
        }
    }
}
