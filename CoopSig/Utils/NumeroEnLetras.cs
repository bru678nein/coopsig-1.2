using System;
using System.Collections.Generic;
using System.Globalization;

namespace CoopSig.Utils
{
    /// <summary>
    /// Convierte un importe a letras para el renglón "Recibí la cantidad de
    /// Pesos" del recibo.
    ///
    /// Reemplaza a la función VBA Enletras() que vive dentro de la base y que
    /// hoy imprime #Error, con lo cual los recibos salen sin el importe en
    /// letras. Acá se recalcula: no depende de que la base tenga las macros
    /// habilitadas ni de estar en una ubicación de confianza de Access.
    /// </summary>
    public static class NumeroEnLetras
    {
        private static readonly string[] Unidades =
        {
            "", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO",
            "NUEVE", "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE",
            "DIECISÉIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE", "VEINTE",
            "VEINTIUNO", "VEINTIDÓS", "VEINTITRÉS", "VEINTICUATRO", "VEINTICINCO",
            "VEINTISÉIS", "VEINTISIETE", "VEINTIOCHO", "VEINTINUEVE"
        };

        private static readonly string[] Decenas =
        {
            "", "", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA",
            "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"
        };

        private static readonly string[] Centenas =
        {
            "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS",
            "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"
        };

        /// <summary>
        /// Importe completo en letras, con los centavos como fracción sobre
        /// cien: 8.937,60 devuelve "OCHO MIL NOVECIENTOS TREINTA Y SIETE CON
        /// 60/100".
        ///
        /// Los centavos van en números y no en letras a propósito: es lo
        /// habitual en un recibo y evita renglones larguísimos por dos dígitos.
        /// </summary>
        public static string Convertir(decimal importe)
        {
            var negativo = importe < 0m;
            var absoluto = Math.Abs(Math.Round(importe, 2, MidpointRounding.AwayFromZero));

            var entero = (long)decimal.Truncate(absoluto);
            var centavos = (int)((absoluto - entero) * 100m);

            var texto = string.Format(
                CultureInfo.InvariantCulture,
                "{0} CON {1:00}/100",
                ConvertirEntero(entero),
                centavos);

            return negativo ? "MENOS " + texto : texto;
        }

        /// <summary>
        /// Parte entera en letras. Contempla hasta cientos de millones, muy por
        /// encima de cualquier bono real.
        /// </summary>
        public static string ConvertirEntero(long numero)
        {
            if (numero < 0)
            {
                return "MENOS " + ConvertirEntero(-numero);
            }
            if (numero == 0)
            {
                return "CERO";
            }

            var partes = new List<string>();

            var millones = numero / 1000000L;
            var resto = numero % 1000000L;

            if (millones > 0)
            {
                partes.Add(millones == 1
                    ? "UN MILLÓN"
                    : Apocopar(ConvertirEntero(millones)) + " MILLONES");
            }

            var miles = resto / 1000L;
            var unidades = resto % 1000L;

            if (miles > 0)
            {
                // "MIL", nunca "UN MIL". Y veintiuno, treinta y uno, etc. se
                // apocopan delante de mil: VEINTIÚN MIL, TREINTA Y UN MIL.
                partes.Add(miles == 1 ? "MIL" : Apocopar(Grupo((int)miles)) + " MIL");
            }

            if (unidades > 0)
            {
                partes.Add(Grupo((int)unidades));
            }

            return string.Join(" ", partes.ToArray());
        }

        /// <summary>Un grupo de 0 a 999.</summary>
        private static string Grupo(int numero)
        {
            if (numero == 0)
            {
                return string.Empty;
            }

            // "CIEN" exacto; a partir de 101 es "CIENTO".
            if (numero == 100)
            {
                return "CIEN";
            }

            var texto = Centenas[numero / 100];
            var resto = numero % 100;

            if (resto == 0)
            {
                return texto;
            }

            if (texto.Length > 0)
            {
                texto += " ";
            }

            if (resto < 30)
            {
                return texto + Unidades[resto];
            }

            texto += Decenas[resto / 10];
            var unidad = resto % 10;
            return unidad == 0 ? texto : texto + " Y " + Unidades[unidad];
        }

        /// <summary>
        /// Apócope de "uno" delante de un sustantivo masculino: veintiún mil,
        /// treinta y un mil, un millón. Decir "veintiuno mil" está mal.
        /// </summary>
        private static string Apocopar(string texto)
        {
            if (texto.EndsWith("VEINTIUNO", StringComparison.Ordinal))
            {
                return texto.Substring(0, texto.Length - "VEINTIUNO".Length) + "VEINTIÚN";
            }
            if (texto.EndsWith("UNO", StringComparison.Ordinal))
            {
                return texto.Substring(0, texto.Length - "UNO".Length) + "UN";
            }
            return texto;
        }
    }
}
