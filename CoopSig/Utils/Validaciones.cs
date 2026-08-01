using System.Text;
using CoopSig.Data;
using CoopSig.Models;

namespace CoopSig.Utils
{
    /// <summary>
    /// Reglas de integridad validadas en código antes de escribir, nunca
    /// confiando en la base (Constitución VI).
    /// </summary>
    public class Validaciones
    {
        private readonly AsociadoRepository _repositorio;

        public Validaciones(AsociadoRepository repositorio)
        {
            _repositorio = repositorio;
        }

        /// <summary>
        /// Busca si ya existe un asociado con ese documento, para poder avisar
        /// de quién es antes de guardar (HU-3). Devuelve null si no existe.
        /// </summary>
        public Asociado BuscarDuplicadoDocumento(long documento)
        {
            return _repositorio.ObtenerPorDocumento(documento);
        }

        /// <summary>
        /// Coherencia del identificador fiscal (CUIL + Digito). Se omite si
        /// viene vacío: es un caso válido tener solo el documento (HU-3).
        /// </summary>
        public static ResultadoValidacion ValidarCuil(long documento, int? cuil, int? digito)
        {
            if (!cuil.HasValue && !digito.HasValue)
            {
                return ResultadoValidacion.Ok();
            }

            if (!cuil.HasValue || !digito.HasValue)
            {
                return ResultadoValidacion.Error(
                    "Si carga el identificador fiscal, debe completar tanto el prefijo como el dígito verificador.");
            }

            if (!EsDigitoVerificadorValido(cuil.Value, documento, digito.Value))
            {
                return ResultadoValidacion.Error(
                    "El dígito verificador del identificador fiscal no coincide. Verifique los datos.");
            }

            return ResultadoValidacion.Ok();
        }

        /// <summary>
        /// Algoritmo estándar de verificación de CUIL/CUIT argentino (módulo 11)
        /// sobre el número reconstruido Cuil + Documento.
        /// </summary>
        internal static bool EsDigitoVerificadorValido(int cuil, long documento, int digitoVerificador)
        {
            var numero = string.Format("{0:00}{1:00000000}", cuil, documento);

            int[] multiplicadores = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
            var suma = 0;
            for (var i = 0; i < multiplicadores.Length; i++)
            {
                suma += (numero[i] - '0') * multiplicadores[i];
            }

            var resto = suma % 11;
            var digitoCalculado = 11 - resto;
            if (digitoCalculado == 11)
            {
                digitoCalculado = 0;
            }
            else if (digitoCalculado == 10)
            {
                digitoCalculado = 9;
            }

            return digitoCalculado == digitoVerificador;
        }

        public static bool EsCampoObligatorioCompleto(string valor)
        {
            return !string.IsNullOrWhiteSpace(valor);
        }

        /// <summary>
        /// Interpreta un mismo campo escrito como CUIT o como DNI. Se ignoran
        /// guiones, puntos y espacios, así que "20-12345678-3", "20123456783"
        /// y "12345678" son todas entradas válidas.
        ///
        /// Con 11 dígitos se parte en prefijo (2) + documento (8) + verificador
        /// (1). Con 8 o menos se toma como documento solo, que es un caso
        /// legítimo: hay asociados de los que únicamente se tiene el DNI.
        ///
        /// El documento sigue siendo la clave del registro en ambos casos, así
        /// que cargar el CUIT de alguien ya existente no crea un duplicado.
        /// </summary>
        public static ResultadoValidacion ParsearDocumentoOCuit(
            string texto, out long documento, out int? cuil, out int? digito)
        {
            documento = 0;
            cuil = null;
            digito = null;

            var soloDigitos = SoloDigitos(texto);
            if (soloDigitos.Length == 0)
            {
                return ResultadoValidacion.Error("Ingrese el CUIT o el DNI del asociado.");
            }

            if (soloDigitos.Length == 11)
            {
                cuil = int.Parse(soloDigitos.Substring(0, 2));
                documento = long.Parse(soloDigitos.Substring(2, 8));
                digito = int.Parse(soloDigitos.Substring(10, 1));
                return ResultadoValidacion.Ok();
            }

            if (soloDigitos.Length <= 8)
            {
                documento = long.Parse(soloDigitos);
                if (documento <= 0)
                {
                    return ResultadoValidacion.Error("El documento debe ser mayor que cero.");
                }
                return ResultadoValidacion.Ok();
            }

            return ResultadoValidacion.Error(
                "El valor ingresado no es un CUIT (11 dígitos) ni un DNI (hasta 8 dígitos).");
        }

        private static string SoloDigitos(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return string.Empty;
            }

            var limpio = new StringBuilder(texto.Length);
            foreach (var caracter in texto)
            {
                if (char.IsDigit(caracter))
                {
                    limpio.Append(caracter);
                }
            }
            return limpio.ToString();
        }
    }

    /// <summary>Resultado de una validación: éxito, o error con mensaje en lenguaje llano.</summary>
    public class ResultadoValidacion
    {
        public bool EsValido { get; private set; }
        public string Mensaje { get; private set; }

        private ResultadoValidacion(bool esValido, string mensaje)
        {
            EsValido = esValido;
            Mensaje = mensaje;
        }

        public static ResultadoValidacion Ok()
        {
            return new ResultadoValidacion(true, null);
        }

        public static ResultadoValidacion Error(string mensaje)
        {
            return new ResultadoValidacion(false, mensaje);
        }
    }
}
