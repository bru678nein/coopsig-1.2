# Prompt para construir la versión 1

Copiar y pegar el bloque de abajo. Antes de usarlo, reemplazar los `<<< >>>` con los datos reales. Va acompañado del archivo `ESPECIFICACION.md`.

---

```
Necesito que construyas la versión 1 de una aplicación de escritorio en C# con
WinForms sobre .NET Framework 4.8. Te adjunto la especificación completa en
ESPECIFICACION.md; leela entera antes de escribir código.

## Contexto no negociable

- Tiene que correr en Windows 10 con hardware viejo y poca memoria.
- La base de datos es un archivo Microsoft Access YA EXISTENTE con muchos
  registros cargados. NO se migra, NO se recrea, NO se modifica su estructura
  salvo que yo lo pida explícitamente.
- Acceso a datos con System.Data.OleDb usando Microsoft.ACE.OLEDB.12.0.
- El proyecto se compila con plataforma de destino x86, no "Any CPU".
- Todo el texto de la interfaz en castellano rioplatense.

## Esquema real de la base

<<< PEGAR ACÁ LOS NOMBRES REALES DE TABLAS Y CAMPOS DE LA BASE ACTUAL.
    Si todavía no los tenés, indicalo y usá el modelo de la sección 5 de
    ESPECIFICACION.md, dejando la capa de datos con los nombres de tabla y
    campo centralizados en constantes para poder cambiarlos en un solo lugar. >>>

## Alcance de esta versión 1

Solo el módulo de ASOCIADOS, completo y funcionando:

1. Ventana de menú principal con cuatro botones (Bonos, Anticipos, Asociados y
   el cuarto deshabilitado por ahora). Solo Asociados abre pantalla.
2. Pantalla de Asociados con:
   - Buscador único que detecta si lo tipeado es numérico (busca por DNI, por
     prefijo) o alfabético (busca por apellido y nombre). Filtra mientras se
     tipea, sin botón Buscar. El foco arranca ahí al abrir la pantalla.
   - Grilla mostrando apellido, nombre, DNI, servicio y estado.
   - Filtro "Solo activos / Todos".
   - Botones Nuevo, Editar, Ver y Baja. Doble clic en una fila abre Editar.
3. Formulario de alta y edición de asociado, con Enter avanzando al campo
   siguiente y validación de DNI único.
4. Baja lógica: marca Activo = No y carga FechaBaja. NUNCA borra la fila.
5. Backup automático del archivo Access a una subcarpeta /backups con timestamp
   cada vez que arranca la aplicación.

No implementes todavía Bonos, Anticipos ni impresión.

## Estructura del código

Separá en carpetas: /UI, /Servicios, /Datos, /Modelos. La capa /Datos es la
única que puede conocer OleDb — el resto trabaja contra interfaces de
repositorio. Esto tiene que permitir cambiar de motor más adelante tocando solo
esa carpeta.

## Cómo quiero que trabajes

- Usá parámetros en todas las consultas, nunca concatenación de strings.
- Manejá y mostrá los errores de conexión en castellano llano, sin códigos
  técnicos ni stack traces en pantalla.
- Cerrá siempre las conexiones con using.
- Comentá en castellano solo lo que no sea evidente.
- Al terminar, dame las instrucciones exactas para abrir y compilar el proyecto,
  y para configurar la ruta del archivo Access.

Empezá mostrándome la estructura de archivos que vas a crear y la cadena de
conexión que vas a usar. Si algo de la especificación te resulta ambiguo,
preguntámelo antes de escribir código en lugar de asumir.
```

---

## Por qué la v1 es solo Asociados

Bonos y Anticipos dependen de Asociados por clave foránea, así que no se pueden
construir antes. Además, el módulo de Asociados ejercita todo el andamiaje —
conexión a Access, repositorios, grillas, buscador, validaciones — sobre la
parte menos riesgosa del sistema. Si la conexión a Access da problemas de driver
o de arquitectura 32/64 bits, aparecen acá y no a mitad de la lógica de bonos.

## Orden sugerido de las siguientes versiones

| Versión | Alcance |
|---|---|
| v1 | Asociados + servicios + backup automático |
| v2 | Anticipos (entidad simple, valida el patrón de FK) |
| v3 | Bonos sin impresión |
| v4 | Impresión sobre plantilla |
| v5 | Cuarta opción del menú y ajustes de uso real |
