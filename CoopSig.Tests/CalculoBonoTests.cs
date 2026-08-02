using CoopSig.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoopSig.Tests
{
    /// <summary>
    /// Pruebas del cálculo del bono. Los dos primeros casos no son inventados:
    /// son registros reales de la base y sus importes salieron del recibo
    /// impreso por el sistema actual. Si alguien altera la fórmula, esos dos
    /// tests avisan antes de que se imprima un recibo mal.
    /// </summary>
    [TestClass]
    public class CalculoBonoTests
    {
        [TestMethod]
        public void Calcular_ReciboRealSoloHoras_CoincideConElImpreso()
        {
            // VILLEGAS, CAROLINA ELIZABETH — OSEP VIGILANCIA — ENERO 2020.
            // 228 horas a $40. Recibo impreso: haberes $9.120,00,
            // Ley 20337 $182,40, neto a cobrar $8.937,60.
            var resultado = CalculoBono.Calcular(228m, 40m, 0m, 0m, 0m, 0m);

            Assert.AreEqual(9120.00m, resultado.Haberes);
            Assert.AreEqual(182.40m, resultado.Ley20337);
            Assert.AreEqual(182.40m, resultado.TotalDescuentos);
            Assert.AreEqual(8937.60m, resultado.Neto);
        }

        [TestMethod]
        public void Calcular_SoloBasicoSinHoras_DescuentaElDosPorCiento()
        {
            // Bono real de COOPERATIVA SIG: sin horas, básico $612.244,90.
            // Ese básico se fijó hacia atrás desde un neto redondo de
            // $600.000 (600.000 ÷ 0,98), lo que confirma que el 2% se aplica
            // también sobre el básico y no solo sobre las horas.
            var resultado = CalculoBono.Calcular(0m, 0m, 612244.90m, 0m, 0m, 0m);

            Assert.AreEqual(612244.90m, resultado.Haberes);
            Assert.AreEqual(12244.90m, resultado.Ley20337);
            Assert.AreEqual(600000.00m, resultado.Neto);
        }

        [TestMethod]
        public void Calcular_BasicoYHoras_ElDosPorCientoSaleDeLaSuma()
        {
            // El caso que distingue las dos hipótesis que estuvieron en duda:
            // si el 2% saliera solo de las horas daría $100,00 y el neto
            // sería otro. Sale de los haberes completos: $8.000 × 0,02 = $160.
            var resultado = CalculoBono.Calcular(100m, 50m, 3000m, 0m, 0m, 0m);

            Assert.AreEqual(5000m, resultado.TotalHoras);
            Assert.AreEqual(8000m, resultado.Haberes);
            Assert.AreEqual(160m, resultado.Ley20337);
            Assert.AreEqual(7840m, resultado.Neto);
        }

        [TestMethod]
        public void Calcular_ConTodosLosDescuentos_LosSumaAlDosPorCiento()
        {
            // Mutual (rotulada "Seguro" en el recibo), anticipo y otros se
            // suman al 2%: 200 + 160 + 500 + 1.000 = 1.860.
            var resultado = CalculoBono.Calcular(
                100m, 50m, 3000m, mutual: 200m, anticipo: 500m, otros: 1000m);

            Assert.AreEqual(160m, resultado.Ley20337);
            Assert.AreEqual(1860m, resultado.TotalDescuentos);
            Assert.AreEqual(6140m, resultado.Neto);
        }

        [TestMethod]
        public void Calcular_BonoVacio_NoRompeYDaCero()
        {
            var resultado = CalculoBono.Calcular(0m, 0m, 0m, 0m, 0m, 0m);

            Assert.AreEqual(0m, resultado.Haberes);
            Assert.AreEqual(0m, resultado.Ley20337);
            Assert.AreEqual(0m, resultado.Neto);
            Assert.IsFalse(resultado.EsNegativo);
        }

        [TestMethod]
        public void Calcular_AnticipoMayorQueElBono_DaNetoNegativo()
        {
            // El cálculo no lo impide: informa. Qué hacer cuando el anticipo
            // supera al bono es una decisión de la oficina, y le corresponde
            // avisar a la pantalla antes de grabar.
            var resultado = CalculoBono.Calcular(
                10m, 100m, 0m, mutual: 0m, anticipo: 5000m, otros: 0m);

            Assert.IsTrue(resultado.EsNegativo);
            Assert.AreEqual(-4020m, resultado.Neto);
        }

        [TestMethod]
        public void Calcular_ImportesConCentavos_RedondeaAlejandoseDelCero()
        {
            // 0,125 con el redondeo bancario de .NET daría 0,12. Sobre un
            // recibo eso se lee como un error de un centavo.
            var resultado = CalculoBono.Calcular(0m, 0m, 6.25m, 0m, 0m, 0m);

            Assert.AreEqual(0.13m, resultado.Ley20337);
            Assert.AreEqual(6.12m, resultado.Neto);
        }

        [TestMethod]
        public void Calcular_ElReciboCierraConCalculadora()
        {
            // Los renglones impresos tienen que sumar exactamente el total
            // impreso. Si los descuentos se sumaran sin redondear, el neto
            // podría diferir un centavo de la resta a mano.
            var resultado = CalculoBono.Calcular(
                163m, 47.33m, 1234.56m, mutual: 250m, anticipo: 333.33m, otros: 77.77m);

            Assert.AreEqual(
                resultado.Haberes - resultado.TotalDescuentos, resultado.Neto);
            Assert.AreEqual(
                250m + 333.33m + 77.77m + resultado.Ley20337,
                resultado.TotalDescuentos);
        }
    }
}
