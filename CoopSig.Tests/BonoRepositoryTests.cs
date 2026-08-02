using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoopSig.Data;
using CoopSig.Models;
using CoopSig.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoopSig.Tests
{
    /// <summary>
    /// Pruebas de BonoRepository.
    ///
    /// El orden cronológico se prueba sin tocar la base: es lógica pura y es
    /// donde está el riesgo real, porque el mes está guardado como texto.
    /// Las pruebas de integración requieren una COPIA de la base en
    /// TestData\base_test.mdb (nunca la original) y se marcan inconclusas si
    /// no está, en lugar de fallar.
    /// </summary>
    [TestClass]
    public class BonoRepositoryTests
    {
        private static readonly string RutaBaseTest =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "base_test.mdb");

        [TestMethod]
        public void OrdenCronologico_DiciembreVaAntesQueEneroDelAnioSiguiente()
        {
            // Ordenar el texto pondría ABRIL primero y DICIEMBRE cuarto. Este
            // es el error que el orden en memoria existe para evitar.
            var bonos = new List<Bono>
            {
                CrearBono("ABRIL", "2019"),
                CrearBono("ENERO", "2020"),
                CrearBono("DICIEMBRE", "2019"),
                CrearBono("SETIEMBRE", "2019")
            };

            OrdenarDelMasRecienteAlMasAntiguo(bonos);

            CollectionAssert.AreEqual(
                new[] { "ENERO 2020", "DICIEMBRE 2019", "SETIEMBRE 2019", "ABRIL 2019" },
                bonos.Select(b => b.PeriodoDescripto).ToArray());
        }

        [TestMethod]
        public void OrdenCronologico_PeriodoIlegible_QuedaAlFinal()
        {
            var bonos = new List<Bono>
            {
                CrearBono("BASURA", "2019"),
                CrearBono("ENERO", "2020")
            };

            OrdenarDelMasRecienteAlMasAntiguo(bonos);

            Assert.AreEqual("ENERO 2020", bonos[0].PeriodoDescripto);
        }

        [TestMethod]
        public void Bono_CalcularUsaLaFormulaVerificada()
        {
            // El modelo delega en CalculoBono; esto verifica que el cableado
            // esté hecho y que no se calcule por otro lado.
            var bono = new Bono { Horas = 228m, ValorHora = 40m };

            Assert.AreEqual(8937.60m, bono.Calcular().Neto);
        }

        [TestMethod]
        public void ObtenerPorDocumento_DevuelveSoloBonosDeEsaPersona()
        {
            OmitirSiNoHayBaseDePrueba();

            var repositorio = new BonoRepository();
            var bonos = repositorio.ObtenerPorDocumento(38475547);

            Assert.IsTrue(bonos.All(b => b.Documento == 38475547));
        }

        [TestMethod]
        public void ObtenerPorDocumento_DevuelveDelMasRecienteAlMasAntiguo()
        {
            OmitirSiNoHayBaseDePrueba();

            var repositorio = new BonoRepository();
            var bonos = repositorio.ObtenerPorDocumento(38475547);

            for (var i = 1; i < bonos.Count; i++)
            {
                var comparacion = Periodo.CompararDescendente(
                    bonos[i - 1].PeriodoMes, bonos[i - 1].PeriodoAnio,
                    bonos[i].PeriodoMes, bonos[i].PeriodoAnio);

                Assert.IsTrue(comparacion <= 0, "El listado no quedó en orden cronológico.");
            }
        }

        [TestMethod]
        public void ObtenerPorDocumento_DocumentoInexistente_DevuelveListaVacia()
        {
            OmitirSiNoHayBaseDePrueba();

            var repositorio = new BonoRepository();

            Assert.AreEqual(0, repositorio.ObtenerPorDocumento(1).Count);
        }

        private static Bono CrearBono(string mes, string anio)
        {
            return new Bono { PeriodoMes = mes, PeriodoAnio = anio };
        }

        /// <summary>
        /// Misma comparación que aplica ObtenerPorDocumento. Se repite acá a
        /// propósito: si alguien invierte el orden en el repositorio, el test
        /// tiene que fallar en vez de acompañar el cambio.
        /// </summary>
        private static void OrdenarDelMasRecienteAlMasAntiguo(List<Bono> bonos)
        {
            bonos.Sort((izquierdo, derecho) => Periodo.CompararDescendente(
                izquierdo.PeriodoMes, izquierdo.PeriodoAnio,
                derecho.PeriodoMes, derecho.PeriodoAnio));
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
