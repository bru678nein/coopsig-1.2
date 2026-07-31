using System;
using System.IO;
using System.Linq;
using CoopSig.Data;
using CoopSig.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoopSig.Tests
{
    /// <summary>
    /// Pruebas de AsociadoRepository.Buscar (tasks.md 5.2): clasificación
    /// numérico/alfabético (HU-1).
    ///
    /// Las pruebas de clasificación pura (EsBusquedaNumerica, Coincide) no
    /// tocan la base y corren siempre. Las pruebas de integración contra
    /// Buscar() requieren una COPIA de la base en TestData\base_test.mdb
    /// (nunca la original — Constitución I) y se marcan inconclusas si no
    /// está presente, en lugar de fallar.
    /// </summary>
    [TestClass]
    public class AsociadoRepositoryTests
    {
        private static readonly string RutaBaseTest =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "base_test.mdb");

        [TestMethod]
        public void EsBusquedaNumerica_SoloDigitos_EsVerdadero()
        {
            Assert.IsTrue(AsociadoRepository.EsBusquedaNumerica("30123456"));
        }

        [TestMethod]
        public void EsBusquedaNumerica_ConLetras_EsFalso()
        {
            Assert.IsFalse(AsociadoRepository.EsBusquedaNumerica("Gonzalez"));
        }

        [TestMethod]
        public void EsBusquedaNumerica_Vacio_EsFalso()
        {
            Assert.IsFalse(AsociadoRepository.EsBusquedaNumerica(string.Empty));
        }

        [TestMethod]
        public void EsBusquedaNumerica_MixtoNumeroYLetra_EsFalso()
        {
            Assert.IsFalse(AsociadoRepository.EsBusquedaNumerica("30123456G"));
        }

        [TestMethod]
        public void Coincide_DocumentoPorPrefijo_Coincide()
        {
            var asociado = new Asociado { Documento = 30123456, Apellido = "Gomez", Nombre = "Ana" };
            Assert.IsTrue(AsociadoRepository.Coincide(asociado, "301234"));
        }

        [TestMethod]
        public void Coincide_ApellidoParcialSinDistincionDeMayusculas_Coincide()
        {
            var asociado = new Asociado { Documento = 1, Apellido = "González", Nombre = "Juan" };
            Assert.IsTrue(AsociadoRepository.Coincide(asociado, "gonz"));
        }

        [TestMethod]
        public void Coincide_TextoQueNoCoincide_NoCoincide()
        {
            var asociado = new Asociado { Documento = 1, Apellido = "González", Nombre = "Juan" };
            Assert.IsFalse(AsociadoRepository.Coincide(asociado, "Perez"));
        }

        [TestMethod]
        public void Coincide_TextoVacio_CoincideConCualquiera()
        {
            var asociado = new Asociado { Documento = 1, Apellido = "González", Nombre = "Juan" };
            Assert.IsTrue(AsociadoRepository.Coincide(asociado, string.Empty));
        }

        [TestMethod]
        public void Buscar_TextoNumerico_FiltraPorDocumentoPrefijo()
        {
            OmitirSiNoHayBaseDePrueba();

            var repositorio = new AsociadoRepository();
            var resultado = repositorio.Buscar("301", true);

            Assert.IsTrue(resultado.All(a => a.Documento.ToString().StartsWith("301")));
        }

        [TestMethod]
        public void Buscar_SinIncluirBajas_ExcluyeInactivos()
        {
            OmitirSiNoHayBaseDePrueba();

            var repositorio = new AsociadoRepository();
            var resultado = repositorio.Buscar(string.Empty, false);

            Assert.IsTrue(resultado.All(a => a.Activo));
        }

        private static void OmitirSiNoHayBaseDePrueba()
        {
            if (!File.Exists(RutaBaseTest))
            {
                Assert.Inconclusive(
                    "Falta la base de prueba en TestData\\base_test.mdb. Copie una base " +
                    "de prueba (nunca la original) antes de ejecutar esta prueba en Windows. " +
                    "Ver TestData\\LEEME.txt.");
            }
        }
    }
}
