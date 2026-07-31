using System;

namespace CoopSig.Models
{
    /// <summary>
    /// Representa una fila de la tabla Asociados. El estado activo/inactivo
    /// no es una columna: se deriva de FechaBaja (Decisión de mapeo #1, plan.md).
    /// </summary>
    public class Asociado
    {
        public long Documento { get; set; }
        public string Apellido { get; set; }
        public string Nombre { get; set; }

        /// <summary>Prefijo del identificador fiscal (p. ej. 20, 23, 27). Opcional.</summary>
        public int? Cuil { get; set; }

        /// <summary>Dígito verificador del identificador fiscal. Opcional.</summary>
        public int? Digito { get; set; }

        public string Servicio { get; set; }
        public string Cargo { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public DateTime? FechaBaja { get; set; }

        /// <summary>Activo cuando no tiene fecha de baja registrada (HU-4).</summary>
        public bool Activo
        {
            get { return !FechaBaja.HasValue; }
        }

        /// <summary>
        /// Identificador fiscal completo reconstruido: Cuil + Documento + Digito.
        /// Devuelve null si falta el prefijo o el dígito — caso válido: solo se
        /// dispone del documento (Decisión de mapeo #2, plan.md).
        /// </summary>
        public string IdentificadorFiscal
        {
            get
            {
                if (!Cuil.HasValue || !Digito.HasValue)
                {
                    return null;
                }
                return string.Format("{0:00}-{1}-{2}", Cuil.Value, Documento, Digito.Value);
            }
        }

        public string NombreCompleto
        {
            get { return string.Format("{0}, {1}", Apellido, Nombre); }
        }
    }
}
