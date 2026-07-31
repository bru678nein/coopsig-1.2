namespace CoopSig.Models
{
    /// <summary>
    /// Entrada del catálogo de Cargos, unida con los valores ya en uso en
    /// Asociados (misma lógica que Servicio — Decisión de mapeo #3, plan.md).
    /// </summary>
    public class Cargo
    {
        public string Nombre { get; set; }

        public override string ToString()
        {
            return Nombre;
        }
    }
}
