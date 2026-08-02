using CoopSig.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoopSig.Tests
{
    /// <summary>
    /// Pruebas de la traducción de períodos. El caso que más importa es el de
    /// SETIEMBRE: la base lo escribe sin P, y una tabla de meses escrita de
    /// memoria haría desaparecer ese mes entero de cualquier filtro, sin
    /// error y sin aviso.
    /// </summary>
    [TestClass]
    public class PeriodoTests
    {
        [TestMethod]
        public void NombreDeMes_Septiembre_SeEscribeSinPeComoLaBase()
        {
            Assert.AreEqual("SETIEMBRE", Periodo.NombreDeMes(9));
        }

        [TestMethod]
        public void NumeroDeMes_GrafiaConPe_SeAceptaAlLeer()
        {
            // No se escribe nunca, pero si algún registro histórico la tiene,
            // ese bono tiene que seguir siendo encontrable.
            Assert.AreEqual(9, Periodo.NumeroDeMes("SEPTIEMBRE"));
            Assert.AreEqual(9, Periodo.NumeroDeMes("SETIEMBRE"));
        }

        [TestMethod]
        public void NumeroDeMes_ConEspaciosYMinusculas_LosTolera()
        {
            Assert.AreEqual(1, Periodo.NumeroDeMes("  enero "));
        }

        [TestMethod]
        public void NumeroDeMes_TextoQueNoEsUnMes_DevuelveNulo()
        {
            Assert.IsNull(Periodo.NumeroDeMes("CUALQUIERA"));
            Assert.IsNull(Periodo.NumeroDeMes(""));
            Assert.IsNull(Periodo.NumeroDeMes(null));
        }

        [TestMethod]
        public void ClaveDeOrden_ArmaAnioMesEnUnEntero()
        {
            Assert.AreEqual(202001, Periodo.ClaveDeOrden("ENERO", "2020"));
            Assert.AreEqual(201812, Periodo.ClaveDeOrden("DICIEMBRE", "2018"));
        }

        [TestMethod]
        public void Comparar_DiciembreEsPosteriorAEnero_AunqueAlfabeticamenteNo()
        {
            // Ordenar el texto daría DICIEMBRE antes que ENERO. Este es el
            // error que la clase existe para evitar.
            Assert.IsTrue(Periodo.Comparar("DICIEMBRE", "2018", "ENERO", "2018") > 0);
        }

        [TestMethod]
        public void Comparar_AnioPesaMasQueElMes()
        {
            Assert.IsTrue(Periodo.Comparar("ENERO", "2020", "DICIEMBRE", "2019") > 0);
        }

        [TestMethod]
        public void Comparar_PeriodoIlegible_QuedaAlFinalYNoAlPrincipio()
        {
            // Un dato roto no debe colarse como si fuera el más antiguo.
            Assert.IsTrue(Periodo.Comparar("BASURA", "2020", "ENERO", "2020") > 0);
        }

        [TestMethod]
        public void Meses_DevuelveLosDoceEnOrdenCronologico()
        {
            var meses = Periodo.Meses();

            Assert.AreEqual(12, meses.Count);
            Assert.AreEqual("ENERO", meses[0]);
            Assert.AreEqual("DICIEMBRE", meses[11]);
        }

        [TestMethod]
        public void Meses_NoDevuelveLaListaInterna()
        {
            // Si devolviera el arreglo interno, quien lo reciba podría
            // renombrar un mes para todo el programa.
            Periodo.Meses()[0] = "PISOTEADO";

            Assert.AreEqual("ENERO", Periodo.NombreDeMes(1));
        }
    }
}
