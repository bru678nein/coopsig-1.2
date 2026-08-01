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
        /// Estados civiles realmente cargados en el padrón. Este campo no tiene
        /// tabla de catálogo, así que la lista sale de los propios datos en vez
        /// de una lista fija escrita a mano: si mañana aparece "Divorciado" o
        /// "Separado", figura sin tocar el programa.
        /// </summary>
        public List<string> ObtenerEstadosCiviles()
        {
            return Consultar(SelectValoresEnUso("EstadoCivil") + " ORDER BY 1");
        }

        /// <summary>
        /// Sexos realmente cargados en el padrón. Igual que EstadoCivil: sin
        /// tabla de catálogo, la lista sale de los datos. Así se respeta la
        /// convención que ya use la base ("M"/"F", "MASCULINO"/"FEMENINO" o la
        /// que sea) en vez de imponerle una nueva desde el programa.
        /// </summary>
        public List<string> ObtenerSexos()
        {
            return Consultar(SelectValoresEnUso("Sexo") + " ORDER BY 1");
        }

        /// <summary>
        /// Une los nombres de la tabla de catálogo con los valores distintos
        /// ya usados en Asociados, para que ningún valor histórico desaparezca
        /// del listado aunque no esté dado de alta en el catálogo.
        /// </summary>
        private static List<string> ObtenerValoresDistintosUnidos(string tablaCatalogo, string columnaAsociados)
        {
            try
            {
                return Consultar(
                    "SELECT Nombre FROM " + tablaCatalogo +
                    " UNION " + SelectValoresEnUso(columnaAsociados) +
                    " ORDER BY 1");
            }
            catch (OleDbException)
            {
                // Esta base puede no tener la tabla de catálogo, o tenerla con
                // otro nombre de columna. Los valores ya cargados en Asociados
                // alcanzan por sí solos (Decisión de mapeo #3, plan.md): sin
                // este repliegue las listas quedan vacías y, al ser
                // DropDownList, el alta y la edición se vuelven imposibles.
                // Si la falla fuese de conexión, esta segunda consulta también
                // falla y el error se propaga igual.
                return Consultar(SelectValoresEnUso(columnaAsociados) + " ORDER BY 1");
            }
        }

        /// <summary>
        /// Valores realmente en uso en el padrón, sin repetir y sin vacíos.
        /// </summary>
        private static string SelectValoresEnUso(string columnaAsociados)
        {
            return "SELECT DISTINCT " + columnaAsociados + " FROM Asociados" +
                   " WHERE " + columnaAsociados + " IS NOT NULL AND " + columnaAsociados + " <> ''";
        }

        private static List<string> Consultar(string sql)
        {
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
