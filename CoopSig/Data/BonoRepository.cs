using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using CoopSig.Config;
using CoopSig.Models;
using CoopSig.Utils;

namespace CoopSig.Data
{
    /// <summary>
    /// Acceso a datos de la tabla Bono. Toda consulta usa parámetros OleDb,
    /// nunca concatenación de valores de usuario.
    ///
    /// A diferencia de Asociados, acá NO se trae la tabla entera a memoria:
    /// son unos 31.000 bonos contra 4.200 asociados. Siempre se consulta
    /// filtrado, normalmente por documento.
    ///
    /// No existe ninguna operación DELETE: un bono no se borra. Hoy la oficina
    /// lo anula escribiendo texto en Comentario, porque la tabla no tiene
    /// columna Anulado.
    /// </summary>
    public class BonoRepository
    {
        private const string Tabla = "Bono";

        /// <summary>
        /// PeriodoAño va entre corchetes en todo el SQL: lleva eñe y sin los
        /// corchetes Access puede no reconocer el identificador, que es el
        /// error que ya nos costó una tarde con la tabla de catálogo.
        /// </summary>
        private const string Columnas =
            "Id, Fecha, PeriodoMes, [PeriodoAño], Documento, CUIL, Digito, Nombre, Apellido," +
            " Servicio, Horas, ValorHora, Basico, Mutual, Anticipo, Otros, Comentario, OtrosComentario";

        /// <summary>
        /// Bonos de una persona, del más reciente al más antiguo.
        ///
        /// El orden se hace en memoria y no con ORDER BY: el mes está guardado
        /// como texto, así que la base ordenaría ABRIL, AGOSTO, DICIEMBRE,
        /// ENERO. Periodo traduce el nombre a número antes de comparar.
        /// </summary>
        public List<Bono> ObtenerPorDocumento(long documento)
        {
            var sql = "SELECT " + Columnas + " FROM " + Tabla + " WHERE Documento = ?";

            var bonos = Consultar(sql, comando =>
                comando.Parameters.AddWithValue("@documento", documento));

            bonos.Sort((izquierdo, derecho) => Periodo.CompararDescendente(
                izquierdo.PeriodoMes, izquierdo.PeriodoAnio,
                derecho.PeriodoMes, derecho.PeriodoAnio));

            return bonos;
        }

        public Bono ObtenerPorId(int id)
        {
            var sql = "SELECT " + Columnas + " FROM " + Tabla + " WHERE Id = ?";

            var bonos = Consultar(sql, comando =>
                comando.Parameters.AddWithValue("@id", id));

            return bonos.Count > 0 ? bonos[0] : null;
        }

        /// <summary>
        /// Bonos ya cargados a esa persona para ese período. Se usa para avisar
        /// antes de grabar uno nuevo: la base admite varios, pero casi siempre
        /// es una carga repetida por error.
        /// </summary>
        public List<Bono> ObtenerPorDocumentoYPeriodo(
            long documento, string periodoMes, string periodoAnio)
        {
            var sql = "SELECT " + Columnas + " FROM " + Tabla +
                      " WHERE Documento = ? AND PeriodoMes = ? AND [PeriodoAño] = ?";

            return Consultar(sql, comando =>
            {
                comando.Parameters.AddWithValue("@documento", documento);
                comando.Parameters.AddWithValue("@periodoMes", periodoMes ?? string.Empty);
                comando.Parameters.AddWithValue("@periodoAnio", periodoAnio ?? string.Empty);
            });
        }

        /// <summary>
        /// Inserta el bono y devuelve el Id que le asignó Access. Id no se
        /// escribe: es autonumérico.
        /// </summary>
        public int Insertar(Bono bono)
        {
            // OleDb liga los parámetros por POSICIÓN, no por nombre. Son 17
            // columnas, 17 marcadores y 17 parámetros, en el mismo orden. Si
            // se agrega una columna y se olvida un signo de pregunta, no falla:
            // guarda el teléfono en el campo de dirección y se descubre meses
            // después.
            var sql =
                "INSERT INTO " + Tabla +
                " (Fecha, PeriodoMes, [PeriodoAño], Documento, CUIL, Digito, Nombre, Apellido," +
                " Servicio, Horas, ValorHora, Basico, Mutual, Anticipo, Otros, Comentario, OtrosComentario)" +
                " VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            using (var conexion = ConexionManager.CrearConexion())
            using (var comando = new OleDbCommand(sql, conexion))
            {
                AgregarParametrosDeBono(comando, bono);

                conexion.Open();
                comando.ExecuteNonQuery();

                // @@IDENTITY tiene que consultarse sobre LA MISMA conexión:
                // sobre otra devuelve null o el identificador de otra sesión.
                using (var comandoIdentidad = new OleDbCommand("SELECT @@IDENTITY", conexion))
                {
                    return Convert.ToInt32(comandoIdentidad.ExecuteScalar());
                }
            }
        }

        public void Actualizar(Bono bono)
        {
            // El parámetro del WHERE va último: OleDb liga por posición.
            var sql =
                "UPDATE " + Tabla +
                " SET Fecha = ?, PeriodoMes = ?, [PeriodoAño] = ?, Documento = ?, CUIL = ?," +
                " Digito = ?, Nombre = ?, Apellido = ?, Servicio = ?, Horas = ?, ValorHora = ?," +
                " Basico = ?, Mutual = ?, Anticipo = ?, Otros = ?, Comentario = ?," +
                " OtrosComentario = ? WHERE Id = ?";

            using (var conexion = ConexionManager.CrearConexion())
            using (var comando = new OleDbCommand(sql, conexion))
            {
                AgregarParametrosDeBono(comando, bono);
                comando.Parameters.AddWithValue("@id", bono.Id);

                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Los 17 parámetros del bono en el orden exacto de las columnas.
        /// Centralizado para que INSERT y UPDATE no puedan desincronizarse.
        /// </summary>
        private static void AgregarParametrosDeBono(OleDbCommand comando, Bono bono)
        {
            AgregarNullable(comando, "@fecha", bono.Fecha);
            comando.Parameters.AddWithValue("@periodoMes", TextoOVacio(bono.PeriodoMes));
            comando.Parameters.AddWithValue("@periodoAnio", TextoOVacio(bono.PeriodoAnio));
            comando.Parameters.AddWithValue("@documento", bono.Documento);
            AgregarNullable(comando, "@cuil", bono.Cuil);
            AgregarNullable(comando, "@digito", bono.Digito);
            comando.Parameters.AddWithValue("@nombre", TextoOVacio(bono.Nombre));
            comando.Parameters.AddWithValue("@apellido", TextoOVacio(bono.Apellido));
            comando.Parameters.AddWithValue("@servicio", TextoOVacio(bono.Servicio));
            comando.Parameters.AddWithValue("@horas", ParaGuardar(bono.Horas));
            comando.Parameters.AddWithValue("@valorHora", ParaGuardar(bono.ValorHora));
            comando.Parameters.AddWithValue("@basico", ParaGuardar(bono.Basico));
            comando.Parameters.AddWithValue("@mutual", ParaGuardar(bono.Mutual));
            comando.Parameters.AddWithValue("@anticipo", ParaGuardar(bono.Anticipo));
            comando.Parameters.AddWithValue("@otros", ParaGuardar(bono.Otros));
            AgregarNullable(comando, "@comentario", bono.Comentario);
            AgregarNullable(comando, "@otrosComentario", bono.OtrosComentario);
        }

        private static List<Bono> Consultar(string sql, Action<OleDbCommand> agregarParametros)
        {
            using (var conexion = ConexionManager.CrearConexion())
            using (var comando = new OleDbCommand(sql, conexion))
            {
                agregarParametros(comando);

                conexion.Open();
                using (var lector = comando.ExecuteReader())
                {
                    var resultado = new List<Bono>();
                    while (lector.Read())
                    {
                        resultado.Add(MapearBono(lector));
                    }
                    return resultado;
                }
            }
        }

        private static Bono MapearBono(IDataRecord lector)
        {
            return new Bono
            {
                Id = Convert.ToInt32(lector["Id"]),
                Fecha = LeerFechaNullable(lector["Fecha"]),
                PeriodoMes = lector["PeriodoMes"] as string,
                PeriodoAnio = lector["PeriodoAño"] as string,
                Documento = Convert.ToInt64(lector["Documento"]),
                Cuil = lector["CUIL"] as string,
                Digito = lector["Digito"] as string,
                Nombre = lector["Nombre"] as string,
                Apellido = lector["Apellido"] as string,
                Servicio = lector["Servicio"] as string,
                Horas = LeerImporte(lector["Horas"]),
                ValorHora = LeerImporte(lector["ValorHora"]),
                Basico = LeerImporte(lector["Basico"]),
                Mutual = LeerImporte(lector["Mutual"]),
                Anticipo = LeerImporte(lector["Anticipo"]),
                Otros = LeerImporte(lector["Otros"]),
                Comentario = lector["Comentario"] as string,
                OtrosComentario = lector["OtrosComentario"] as string
            };
        }

        /// <summary>
        /// Los importes se leen a decimal aunque Access los guarde como número
        /// de punto flotante: sumar plata en double acumula errores de centavos
        /// que aparecen meses después y no se pueden explicar.
        /// Un importe nulo es cero, no un dato faltante.
        /// </summary>
        private static decimal LeerImporte(object valor)
        {
            return valor == null || valor == DBNull.Value ? 0m : Convert.ToDecimal(valor);
        }

        private static DateTime? LeerFechaNullable(object valor)
        {
            return valor == null || valor == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(valor);
        }

        private static void AgregarNullable(OleDbCommand comando, string nombre, object valor)
        {
            comando.Parameters.AddWithValue(nombre, valor ?? DBNull.Value);
        }

        private static object TextoOVacio(string valor)
        {
            return valor ?? string.Empty;
        }

        /// <summary>
        /// Convierte el importe a double para escribirlo. Las columnas de
        /// importe son Número de punto flotante en Access, y al pasarle un
        /// decimal el proveedor lo declara como NUMERIC: ese desajuste de tipo
        /// puede terminar en "Tipo de datos no coincide en la expresión de
        /// criterios". Convertir acá deja el desajuste en un solo lugar.
        ///
        /// El cálculo sigue siendo en decimal de punta a punta; el double vive
        /// únicamente en el borde de escritura, que es como está la base.
        /// </summary>
        private static double ParaGuardar(decimal importe)
        {
            return (double)importe;
        }
    }
}
