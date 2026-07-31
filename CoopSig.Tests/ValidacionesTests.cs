using CoopSig.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoopSig.Tests
{
    /// <summary>
    /// Pruebas de Validaciones (tasks.md 5.1): unicidad de Documento,
    /// coherencia de CUIL, y el caso válido de CUIL vacío (HU-3).
    /// No requieren base de datos: son lógica pura.
    /// </summary>
    [TestClass]
    public class ValidacionesTests
    {
        [TestMethod]
        public void ValidarCuil_SinCuilNiDigito_EsValido()
        {
            var resultado = Validaciones.ValidarCuil(30123456, null, null);
            Assert.IsTrue(resultado.EsValido);
        }

        [TestMethod]
        public void ValidarCuil_SoloCuilSinDigito_NoEsValido()
        {
            var resultado = Validaciones.ValidarCuil(30123456, 20, null);
            Assert.IsFalse(resultado.EsValido);
        }

        [TestMethod]
        public void ValidarCuil_SoloDigitoSinCuil_NoEsValido()
        {
            var resultado = Validaciones.ValidarCuil(30123456, null, 5);
            Assert.IsFalse(resultado.EsValido);
        }

        [TestMethod]
        public void EsDigitoVerificadorValido_CoherenteConocido_EsVerdadero()
        {
            // 20-30123456-3: dígito verificador calculado a mano con el
            // algoritmo módulo 11 (multiplicadores 5,4,3,2,7,6,5,4,3,2).
            Assert.IsTrue(Validaciones.EsDigitoVerificadorValido(20, 30123456, 3));
        }

        [TestMethod]
        public void ValidarCuil_DigitoIncoherente_NoEsValido()
        {
            var resultado = Validaciones.ValidarCuil(30123456, 20, 9);
            Assert.IsFalse(resultado.EsValido);
        }

        [TestMethod]
        public void EsCampoObligatorioCompleto_Vacio_EsFalso()
        {
            Assert.IsFalse(Validaciones.EsCampoObligatorioCompleto(""));
            Assert.IsFalse(Validaciones.EsCampoObligatorioCompleto("   "));
            Assert.IsFalse(Validaciones.EsCampoObligatorioCompleto(null));
        }

        [TestMethod]
        public void EsCampoObligatorioCompleto_ConTexto_EsVerdadero()
        {
            Assert.IsTrue(Validaciones.EsCampoObligatorioCompleto("Pérez"));
        }
    }
}
