namespace CoopSig.Models
{
    /// <summary>
    /// Entrada del catálogo de Servicios, unida con los valores ya en uso en
    /// Asociados (hay 2 servicios en uso que no figuran en el catálogo y deben
    /// seguir apareciendo — Decisión de mapeo #3, plan.md).
    /// </summary>
    public class Servicio
    {
        public string Nombre { get; set; }

        public override string ToString()
        {
            return Nombre;
        }
    }
}
