using System;
using System.Collections.Generic;
using System.Globalization;

namespace CoopSig.Utils
{
    /// <summary>
    /// Traduce entre el nombre de mes que guarda la base y su número de orden.
    ///
    /// La tabla Bono guarda el período como dos textos: PeriodoMes con el
    /// nombre del mes en mayúsculas ("ENERO") y PeriodoAño con cuatro dígitos
    /// ("2018"). Ordenar PeriodoMes alfabéticamente da ABRIL, AGOSTO,
    /// DICIEMBRE, ENERO..., así que cualquier orden o comparación cronológica
    /// tiene que pasar por acá primero.
    /// </summary>
    public static class Periodo
    {
        /// <summary>
        /// Grafía que usa la base, y la única que se escribe al guardar. La
        /// base tiene "SETIEMBRE" sin P: guardar "SEPTIEMBRE" haría que ese mes
        /// no coincida con ningún registro histórico, sin error ni aviso.
        /// </summary>
        private static readonly string[] NombresPorNumero =
        {
            "ENERO", "FEBRERO", "MARZO", "ABRIL", "MAYO", "JUNIO",
            "JULIO", "AGOSTO", "SETIEMBRE", "OCTUBRE", "NOVIEMBRE", "DICIEMBRE"
        };

        private static readonly Dictionary<string, int> NumeroPorNombre =
            ConstruirIndice();

        private static Dictionary<string, int> ConstruirIndice()
        {
            var indice = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < NombresPorNumero.Length; i++)
            {
                indice[NombresPorNumero[i]] = i + 1;
            }

            // Se acepta al leer, nunca se escribe: si alguien cargó alguna vez
            // la grafía con P, ese bono tiene que seguir siendo encontrable.
            indice["SEPTIEMBRE"] = 9;
            return indice;
        }

        /// <summary>Los doce meses en orden cronológico, como los guarda la base.</summary>
        public static IList<string> Meses()
        {
            return (string[])NombresPorNumero.Clone();
        }

        /// <summary>
        /// Nombre del mes tal como se guarda, o null si el número está fuera
        /// de rango.
        /// </summary>
        public static string NombreDeMes(int numeroDeMes)
        {
            if (numeroDeMes < 1 || numeroDeMes > 12)
            {
                return null;
            }
            return NombresPorNumero[numeroDeMes - 1];
        }

        /// <summary>
        /// Número de mes (1 a 12) a partir del nombre guardado, o null si el
        /// texto no corresponde a ningún mes conocido.
        /// </summary>
        public static int? NumeroDeMes(string nombreDeMes)
        {
            if (string.IsNullOrWhiteSpace(nombreDeMes))
            {
                return null;
            }

            int numero;
            return NumeroPorNombre.TryGetValue(nombreDeMes.Trim(), out numero)
                ? numero
                : (int?)null;
        }

        /// <summary>
        /// Clave numérica ordenable de un período, con el formato AAAAMM. Sirve
        /// para ordenar y comparar sin depender del texto. Devuelve null si el
        /// mes o el año no se pueden interpretar.
        /// </summary>
        public static int? ClaveDeOrden(string periodoMes, string periodoAnio)
        {
            var mes = NumeroDeMes(periodoMes);
            if (!mes.HasValue)
            {
                return null;
            }

            int anio;
            if (!int.TryParse(
                    (periodoAnio ?? string.Empty).Trim(),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out anio))
            {
                return null;
            }

            return anio * 100 + mes.Value;
        }

        /// <summary>
        /// Compara dos períodos cronológicamente. Los períodos que no se pueden
        /// interpretar quedan al final, para que un dato roto no se cuele como
        /// si fuera el más antiguo.
        /// </summary>
        public static int Comparar(
            string mesIzquierdo, string anioIzquierdo,
            string mesDerecho, string anioDerecho)
        {
            var izquierdo = ClaveDeOrden(mesIzquierdo, anioIzquierdo);
            var derecho = ClaveDeOrden(mesDerecho, anioDerecho);

            if (izquierdo.HasValue && derecho.HasValue)
            {
                return izquierdo.Value.CompareTo(derecho.Value);
            }
            if (izquierdo.HasValue)
            {
                return -1;
            }
            if (derecho.HasValue)
            {
                return 1;
            }
            return 0;
        }

        /// <summary>Período legible para mostrar en pantalla: "ENERO 2020".</summary>
        public static string Describir(string periodoMes, string periodoAnio)
        {
            return string.Format("{0} {1}", periodoMes, periodoAnio).Trim();
        }
    }
}
