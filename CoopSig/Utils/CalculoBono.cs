using System;

namespace CoopSig.Utils
{
    /// <summary>
    /// Cálculo del bono, verificado contra el informe de Access y contra los
    /// registros históricos (relevamiento §12, R1):
    ///
    ///   Haberes          = Basico + Horas × ValorHora
    ///   Ley 20337        = Haberes × 2%
    ///   Total Descuentos = Mutual + Anticipo + Otros + Ley 20337
    ///   Neto a Cobrar    = Haberes − Total Descuentos
    ///
    /// El 2% NO se guarda en la tabla: se recalcula cada vez, igual que hace
    /// hoy el informe. Todo lo demás sí queda congelado en la fila del bono,
    /// para que un bono viejo reimpreso muestre los valores de su época.
    /// </summary>
    public static class CalculoBono
    {
        /// <summary>
        /// Aporte de Ley 20337 sobre los haberes. Es el único porcentaje del
        /// cálculo y se aplica sobre el total, básico incluido.
        /// </summary>
        public const decimal PorcentajeLey20337 = 0.02m;

        public static ResultadoBono Calcular(
            decimal horas,
            decimal valorHora,
            decimal basico,
            decimal mutual,
            decimal anticipo,
            decimal otros)
        {
            var totalHoras = Redondear(horas * valorHora);
            var haberes = Redondear(basico + totalHoras);
            var ley20337 = Redondear(haberes * PorcentajeLey20337);

            // Los descuentos se suman ya redondeados, y el neto se calcula a
            // partir de esa suma. Así el recibo cierra con una calculadora en
            // la mano: si se sumaran los valores sin redondear, el total
            // impreso podría diferir un centavo de la suma de sus renglones.
            var totalDescuentos = Redondear(mutual) + Redondear(anticipo)
                                  + Redondear(otros) + ley20337;

            return new ResultadoBono(
                totalHoras, haberes, ley20337, totalDescuentos,
                haberes - totalDescuentos);
        }

        /// <summary>
        /// Redondeo a centavos. Se usa AwayFromZero y no el redondeo bancario
        /// que trae .NET por defecto: con MidpointRounding.ToEven, 0,125 daría
        /// 0,12 y no 0,13, que no es lo que espera nadie mirando un recibo.
        /// </summary>
        private static decimal Redondear(decimal valor)
        {
            return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>
    /// Resultado del cálculo, con los subtotales que el recibo imprime por
    /// separado. Se devuelven todos y no solo el neto porque el recibo los
    /// muestra renglón por renglón y tienen que coincidir con él.
    /// </summary>
    public class ResultadoBono
    {
        public ResultadoBono(
            decimal totalHoras, decimal haberes, decimal ley20337,
            decimal totalDescuentos, decimal neto)
        {
            TotalHoras = totalHoras;
            Haberes = haberes;
            Ley20337 = ley20337;
            TotalDescuentos = totalDescuentos;
            Neto = neto;
        }

        /// <summary>Horas × ValorHora.</summary>
        public decimal TotalHoras { get; private set; }

        /// <summary>Básico + horas. En el recibo figura como "Total Excedentes Repartibles".</summary>
        public decimal Haberes { get; private set; }

        /// <summary>Aporte del 2% sobre los haberes. Calculado, nunca guardado.</summary>
        public decimal Ley20337 { get; private set; }

        public decimal TotalDescuentos { get; private set; }

        /// <summary>Lo que efectivamente cobra la persona.</summary>
        public decimal Neto { get; private set; }

        /// <summary>
        /// Un neto negativo significa que los descuentos superan a los haberes
        /// — típicamente un anticipo mayor que el bono del período. El cálculo
        /// no lo impide: es la pantalla la que tiene que avisar antes de
        /// grabar, porque qué hacer en ese caso es una decisión de la oficina.
        /// </summary>
        public bool EsNegativo
        {
            get { return Neto < 0m; }
        }
    }
}
