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
