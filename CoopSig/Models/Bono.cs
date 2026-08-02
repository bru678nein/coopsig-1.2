using System;
using CoopSig.Utils;

namespace CoopSig.Models
{
    /// <summary>
    /// Representa una fila de la tabla Bono (singular). Access rotula su clave
    /// como "Id de pago": internamente al bono se le dice pago, y el menú del
    /// sistema anterior lo llamaba "Pagos".
    ///
    /// El bono NO apunta al asociado por clave foránea: se copia adentro su
    /// documento, nombre, apellido, servicio, CUIL y dígito. Es deliberado
    /// (relevamiento §12, R2). Si la persona cambia de servicio en 2027, el
    /// bono de 2019 tiene que seguir mostrando el servicio que tenía en 2019.
    /// El bono congela quién era la persona, no solo cuánto cobró.
    /// </summary>
    public class Bono
    {
        public int Id { get; set; }

        /// <summary>
        /// Fecha de carga del bono. NO indica el período: en los datos
        /// históricos se contradicen (hay bonos de DICIEMBRE 2018 con fecha
        /// 10/1/2018). Nunca usarla para filtrar ni agrupar por período.
        /// </summary>
        public DateTime? Fecha { get; set; }

        /// <summary>Nombre del mes en mayúsculas, como lo guarda la base: "ENERO".</summary>
        public string PeriodoMes { get; set; }

        /// <summary>Año de cuatro dígitos, guardado como texto. Columna PeriodoAño.</summary>
        public string PeriodoAnio { get; set; }

        public long Documento { get; set; }

        /// <summary>Prefijo fiscal. En esta tabla es texto, no número como en Asociados.</summary>
        public string Cuil { get; set; }

        /// <summary>Dígito verificador. Texto en esta tabla.</summary>
        public string Digito { get; set; }

        public string Nombre { get; set; }
        public string Apellido { get; set; }

        /// <summary>Servicio al momento del bono. Se elige en el bono, no se hereda del asociado.</summary>
        public string Servicio { get; set; }

        public decimal Horas { get; set; }
        public decimal ValorHora { get; set; }

        /// <summary>Importe fijo. Se SUMA a las horas, no las reemplaza.</summary>
        public decimal Basico { get; set; }

        /// <summary>Descuento. En el recibo aparece rotulado como "Seguro".</summary>
        public decimal Mutual { get; set; }

        /// <summary>Descuento del anticipo ya cobrado por la persona.</summary>
        public decimal Anticipo { get; set; }

        /// <summary>Otros descuentos. Su detalle va en OtrosComentario.</summary>
        public decimal Otros { get; set; }

        /// <summary>
        /// Concepto del bono: "RETRIBUCION", "PREMIO". También es donde se
        /// anota a mano que un bono quedó anulado, porque la tabla no tiene
        /// columna Anulado — ver el pendiente de anulación en el relevamiento.
        /// </summary>
        public string Comentario { get; set; }

        public string OtrosComentario { get; set; }

        public string NombreCompleto
        {
            get { return string.Format("{0}, {1}", Apellido, Nombre); }
        }

        public string PeriodoDescripto
        {
            get { return Periodo.Describir(PeriodoMes, PeriodoAnio); }
        }

        /// <summary>
        /// Totales del bono según la fórmula verificada. Se recalculan siempre
        /// a partir de los importes guardados: el neto y el 2% no se persisten
        /// en ninguna columna.
        /// </summary>
        public ResultadoBono Calcular()
        {
            return CalculoBono.Calcular(
                Horas, ValorHora, Basico, Mutual, Anticipo, Otros);
        }
    }
}
