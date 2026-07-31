# Plan técnico — Feature 001

## Stack

| Componente | Elección | Motivo |
|---|---|---|
| Lenguaje | C# | |
| Framework | .NET Framework 4.8 | Viene preinstalado en Windows 10: cero despliegue de runtime. Ver alternativa evaluada abajo. |
| UI | WinForms | Arranque instantáneo en hardware viejo. |
| Datos | `System.Data.OleDb` | Acceso nativo a Access sin dependencias externas. |
| Proveedor | `Microsoft.ACE.OLEDB.16.0` | Es el que registra Office 2016. Fallback a `12.0`. |
| Plataforma de compilación | **x64 fija** | Debe coincidir con Office 2016 de 64 bits. Nunca "Any CPU". |

### Alternativa evaluada: .NET 8 con WinForms

Al confirmarse que las máquinas destino son Windows 10 y no Windows 7, .NET 8 pasa a ser técnicamente viable. Se descarta igual:

- .NET Framework 4.8 **ya está instalado** en Windows 10 como componente del sistema. El despliegue es copiar un ejecutable de pocos MB.
- .NET 8 exige instalar el runtime en cada máquina o publicar self-contained, lo que lleva el paquete a unas decenas de MB y agrega un paso de instalación en máquinas usadas por personal no técnico.
- El arranque de .NET Framework es más rápido en hardware modesto.
- La aplicación es CRUD sobre Access más impresión: no usa nada que .NET 8 aporte.
- `System.Data.OleDb` en .NET 8 requiere paquete NuGet aparte y sigue siendo Windows-only. No hay ganancia de portabilidad.

.NET Framework 4.8 está en mantenimiento —no recibe funcionalidad nueva— pero tiene soporte mientras lo tenga el sistema operativo. Para el ciclo de vida de este sistema, es suficiente.

### Entorno de desarrollo

- **Desarrollo:** Mac con Apple Silicon. Solo edición de código y specs; no compila ni ejecuta.
- **Compilación y prueba:** PC con Windows 10 y Visual Studio 2022 Community. Se requiere el targeting pack de .NET Framework 4.8.
- **Prueba de aceptación:** PC de la oficina, único entorno con Office 2016 x64 real.
- **Puente:** repositorio Git privado.
- No se usa máquina virtual: en Apple Silicon sería Windows ARM, que no representa el entorno destino y no valida el comportamiento del driver ACE.

### Restricciones derivadas

- **`Microsoft.Jet.OLEDB.4.0` no es utilizable**: existe solo en 32 bits y no carga en un proceso x64, aunque el archivo sea `.mdb`.
- **No usar asistentes de origen de datos de Visual Studio ni DataSets tipados**: el diseñador corre en 32 bits y falla contra el driver de 64. La capa de datos se escribe a mano.
- Si aparece *"El proveedor no está registrado en el equipo local"*, es desajuste de arquitectura, no driver ausente.
- Sin paquetes NuGet externos, sin ORM.

## Cálculo del bono (confirmado)

```
Total = (Horas × ValorHora) + Basico − Mutual − Anticipo − Otros
```

Verificado contra los 33.578 registros históricos:

- **Todos los componentes se almacenan como valores positivos.** Los signos los aplica la fórmula, no los datos. Ningún campo tiene valores negativos.
- **`Basico` es aditivo, no alternativo.** En el 53% de los bonos coexiste con `Horas × ValorHora`; en el 43% solo hay horas y en el 2% solo básico. Se suman.
- **`Mutual` no es un valor fijo.** Fue 120 entre 2016 y 2021, y pasó a 250 desde 2022. También aparecen 0, 500, 700 y 3000 según el caso.

Ese último punto es la justificación empírica del principio III de la constitución: un bono de 2019 reimpreso hoy debe descontar 120, no 250. Por eso el valor se lee siempre del registro del bono y nunca de una tabla de referencia.

## Mapeo al esquema existente

> El esquema **no se modifica**. La tabla `Asociados` ya contiene todos los campos necesarios, incluido `FechaBaja`, que el sistema anterior nunca utilizó.

| Concepto del spec | Campo real | Nota |
|---|---|---|
| Documento | `Documento` (Long Integer) | Clave de hecho. 4.202 valores, todos únicos. Sin PK declarada. |
| Identificador fiscal | `CUIL` + `Digito` | Dos campos numéricos separados. Se reconstruye como `CUIL + Documento + Digito`. |
| Estado activo | `FechaBaja` | Vacío = activo. No existe campo booleano y no se agrega. |
| Servicio | `Servicio` (texto) | Copiado como texto desde el catálogo `Servicio`, que no tiene ID. |
| Cargo | `Cargo` (texto) | Ídem, catálogo `Cargo`. |

### Decisiones de mapeo

1. **El estado se deriva de `FechaBaja`**, no de un booleano. `Activo` es una propiedad calculada en el modelo, no una columna.
2. **El identificador fiscal permanece partido.** 458 registros no tienen prefijo y es un estado válido: se contempla el caso en que solo se dispone del documento. La validación es opcional por diseño.
3. **Los catálogos se leen como texto distinto** de la tabla correspondiente, unidos a los valores ya presentes en `Asociados`. Hay 2 servicios en uso que no figuran en el catálogo; deben seguir apareciendo.
4. **No se declaran claves foráneas.** Los 885 comprobantes huérfanos harían fallar la relación. La integridad se valida en código.

## Arquitectura

```
/Config    ConexionManager, AppSettings
/Models    Asociado, Servicio, Cargo
/Data      AsociadoRepository, CatalogoRepository
/Forms     FrmPrincipal, FrmAsociados, FrmAsociadoDetalle
/Utils     BackupService, Validaciones
```

### `ConexionManager`

Resuelve el proveedor una única vez al arrancar probando `ACE.OLEDB.16.0` y luego `12.0`; cachea el resultado. Expone `CrearConexion()`. La ruta del `.mdb` sale de `App.config`.

### `AsociadoRepository`

- `Buscar(string texto, bool incluirBajas)` — si el texto es enteramente numérico, filtra por `Documento` con coincidencia por prefijo; si contiene letras, por `Apellido` y `Nombre`. Siempre con parámetros OleDb.
- `ObtenerPorDocumento(long doc)`
- `Insertar` / `Actualizar`
- `DarDeBaja(long doc)` — escribe `FechaBaja`. **Nunca `DELETE`.**
- `Reactivar(long doc)` — limpia `FechaBaja`.
- `ExisteDocumento(long doc)`

### Rendimiento

La búsqueda con filtrado en vivo se aplica sobre 4.202 registros. Se carga el padrón en memoria al abrir la pantalla y se filtra en cliente; es más rápido y simple que consultar por pulsación. Debounce de 300 ms sobre el campo de búsqueda.

Para la futura pantalla de bonos (33.578 filas) esta estrategia **no** aplica: ahí se consulta filtrado por documento.

### `BackupService`

Corre antes de abrir el formulario principal. Copia el `.mdb` a `Backups/` con nombre `base_AAAAMMDD_HHmmss.mdb`, conserva los últimos 30. Si falla, avisa y permite continuar.

## Verificación previa a implementar

1. Confirmar el proveedor disponible en la PC destino.
2. Compilar en x64 y verificar conexión contra una copia del `.mdb`, nunca contra el original.
3. Confirmar que `FechaBaja` acepta nulos y escritura.

## Cumplimiento de la constitución

| Principio | Cómo se cumple |
|---|---|
| I — Base intocable | Solo `SELECT`, `INSERT`, `UPDATE`. Ningún DDL. |
| II — Nada se destruye | No existe `DELETE` en el repositorio. |
| III — Comprobantes inmutables | No aplica a esta feature. |
| IV — Entorno manda | .NET 4.8 + x64 + ACE ya presente. Sin instalaciones extra. |
| V — Menos clics | Pantalla única con foco en búsqueda; alta en menos de 90 s. |
| VI — Validar en la app | Documento único y existencia verificados en código. |
| VII — Respaldo | `BackupService` al iniciar. |
| VIII — Instalación independiente | Ruta en `App.config`; ejecutable único. |
