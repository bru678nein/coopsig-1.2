using System.Collections.Generic;
using System.Data.OleDb;
using CoopSig.Config;
using CoopSig.Models;

namespace CoopSig.Data
{
    /// <summary>
    /// Lee los catálogos Servicio y Cargo, uniéndolos con los valores ya
    /// presentes en Asociados: hay valores en uso que no figuran en el
    /// catálogo y deben seguir apareciendo (Decisión de mapeo #3, plan.md).
    /// </summary>
    public class CatalogoRepository
    {
        public List<Servicio> ObtenerServicios()
        {
            var resultado = new List<Servicio>();
            foreach (var nombre in ObtenerValoresDistintosUnidos("Servicio", "Servicio"))
            {
                resultado.Add(new Servicio { Nombre = nombre });
            }
            return resultado;
        }

        public List<Cargo> ObtenerCargos()
        {
            var resultado = new List<Cargo>();
            foreach (var nombre in ObtenerValoresDistintosUnidos("Cargo", "Cargo"))
            {
                resultado.Add(new Cargo { Nombre = nombre });
            }
            return resultado;
        }

        /// <summary>
        /// Une los nombres de la tabla de catálogo con los valores distintos
        /// ya usados en Asociados, para que ningún valor histórico desaparezca
        /// del listado aunque no esté dado de alta en el catálogo.
        /// </summary>
        private static List<string> ObtenerValoresDistintosUnidos(string tablaCatalogo, string columnaAsociados)
        {
            var sql =
                "SELECT Nombre FROM " + tablaCatalogo +
                " UNION SELECT " + columnaAsociados + " FROM Asociados" +
                " WHERE " + columnaAsociados + " IS NOT NULL AND " + columnaAsociados + " <> ''" +
                " ORDER BY 1";

            var resultado = new List<string>();
            using (var conexion = ConexionManager.CrearConexion())
            using (var comando = new OleDbCommand(sql, conexion))
            {
                conexion.Open();
                using (var lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        if (!lector.IsDBNull(0))
                        {
                            resultado.Add(lector.GetString(0));
                        }
                    }
                }
            }
            return resultado;
        }
    }
}
