using CoopSig.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoopSig.Tests
{
    /// <summary>
    /// Pruebas del importe en letras del recibo. Reemplaza a la función VBA
    /// Enletras() de la base, que hoy imprime #Error.
    ///
    /// Los casos raros del castellano —CIEN contra CIENTO, MIL sin UN delante,
    /// y el apócope de VEINTIÚN— son los que se escriben mal cuando alguien
    /// arma esta función de memoria.
    /// </summary>
    [TestClass]
    public class NumeroEnLetrasTests
    {
        [TestMethod]
        public void Convertir_ReciboReal_CoincideConElImporte()
        {
            // VILLEGAS, ENERO 2020: neto verificado contra el recibo impreso.
            Assert.AreEqual(
                "OCHO MIL NOVECIENTOS TREINTA Y SIETE CON 60/100",
                NumeroEnLetras.Convertir(8937.60m));
        }

        [TestMethod]
        public void Convertir_BonoConBasicoAlto_SeEscribeCompleto()
        {
            Assert.AreEqual(
                "SEISCIENTOS DOCE MIL DOSCIENTOS CUARENTA Y CUATRO CON 90/100",
                NumeroEnLetras.Convertir(612244.90m));
        }

        [TestMethod]
        public void Convertir_SinCentavos_IgualLosMuestra()
        {
            // El recibo lleva siempre la fracción, aunque sea 00/100: así no se
            // puede agregar nada después del número.
            Assert.AreEqual("SEISCIENTOS MIL CON 00/100", NumeroEnLetras.Convertir(600000m));
        }

        [TestMethod]
        public void Convertir_SoloCentavos_DiceCero()
        {
            Assert.AreEqual("CERO CON 05/100", NumeroEnLetras.Convertir(0.05m));
        }

        [TestMethod]
        public void Convertir_Negativo_LoDice()
        {
            Assert.AreEqual("MENOS CIEN CON 00/100", NumeroEnLetras.Convertir(-100m));
        }

        [TestMethod]
        public void ConvertirEntero_CienExacto_EsCienYNoCiento()
        {
            Assert.AreEqual("CIEN", NumeroEnLetras.ConvertirEntero(100));
            Assert.AreEqual("CIENTO UNO", NumeroEnLetras.ConvertirEntero(101));
            Assert.AreEqual("CIEN MIL", NumeroEnLetras.ConvertirEntero(100000));
        }

        [TestMethod]
        public void ConvertirEntero_MilSolo_NoLlevaUnAdelante()
        {
            // "UN MIL" está mal; "DOS MIL" está bien.
            Assert.AreEqual("MIL", NumeroEnLetras.ConvertirEntero(1000));
            Assert.AreEqual("MIL UNO", NumeroEnLetras.ConvertirEntero(1001));
            Assert.AreEqual("DOS MIL", NumeroEnLetras.ConvertirEntero(2000));
        }

        [TestMethod]
        public void ConvertirEntero_ApocopeDelanteDeMil()
        {
            // "VEINTIUNO MIL" está mal.
            Assert.AreEqual("VEINTIÚN MIL", NumeroEnLetras.ConvertirEntero(21000));
            Assert.AreEqual("TREINTA Y UN MIL", NumeroEnLetras.ConvertirEntero(31000));
        }

        [TestMethod]
        public void ConvertirEntero_Millones()
        {
            Assert.AreEqual("UN MILLÓN", NumeroEnLetras.ConvertirEntero(1000000));
            Assert.AreEqual("DOS MILLONES", NumeroEnLetras.ConvertirEntero(2000000));
        }

        [TestMethod]
        public void ConvertirEntero_DecenasIrregulares()
        {
            Assert.AreEqual("QUINCE", NumeroEnLetras.ConvertirEntero(15));
            Assert.AreEqual("DIECISÉIS", NumeroEnLetras.ConvertirEntero(16));
            Assert.AreEqual("VEINTIUNO", NumeroEnLetras.ConvertirEntero(21));
            Assert.AreEqual("TREINTA Y UNO", NumeroEnLetras.ConvertirEntero(31));
        }

        [TestMethod]
        public void ConvertirEntero_Cero()
        {
            Assert.AreEqual("CERO", NumeroEnLetras.ConvertirEntero(0));
        }
    }
}
