using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Deja que el proyecto de pruebas vea los miembros internal. Se usa esto en
// lugar de volverlos public porque son detalles de implementación:
// EsBusquedaNumerica y EsDigitoVerificadorValido merecen prueba propia, pero
// nadie fuera de la aplicación tiene por qué poder llamarlos.
[assembly: InternalsVisibleTo("CoopSig.Tests")]

[assembly: AssemblyTitle("CoopSig")]
[assembly: AssemblyDescription("Sistema de gestión de asociados")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("CoopSig")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
