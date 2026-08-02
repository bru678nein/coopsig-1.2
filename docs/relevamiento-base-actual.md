# Relevamiento de la base Access actual

**Versión del documento:** 1.0
**Última verificación contra la base:** 2026-08-02

---

## Cómo leer este documento

Este documento reemplaza por completo al borrador 0.1. Aquel describía un
esquema que **no existe** — hablaba de `IdAsociado`, `DNI` como texto,
`IdServicio` y una tabla `Bonos` con un campo único `Descuentos`. Ninguna de
esas columnas está en la base. El problema no fue que estuviera equivocado: fue
que no se notaba que lo estaba.

Para que eso no vuelva a pasar, **cada dato de acá lleva marca de origen**:

| Marca | Significado |
|---|---|
| ✅ **Verificado** | Visto en la vista Diseño o en datos reales de la base. Se puede escribir código contra esto. |
| 🔍 **Inferido** | Deducido de una convención observada, pero no visto. Usar con repliegue. |
| ❓ **Pendiente** | No se relevó. **No escribir código que dependa de esto.** |

Regla de oro: si algo está en ❓ y se escribe un `SELECT` contra eso, Access
responde *"No se han especificado valores para algunos de los parámetros
requeridos"* — un error que no dice qué columna falta y hace perder una hora.

---

## 1. Objetivo

Reemplazar el programa de la oficina por un sistema de escritorio que gestione
asociados, bonos y anticipos, **manteniendo la base Access existente sin
migración de datos**.

---

## 2. Restricciones

| Restricción | Detalle |
|---|---|
| Sistema operativo | Windows 10 / 11, se compila en Visual Studio 2022 |
| Base de datos | Microsoft Access `.mdb` existente, **no se migra** |
| Tamaño | ~50 MB |
| Ubicación | **Local en cada máquina, no en red** ✅ |
| Instalaciones | **Dos, independientes, con bases separadas** ✅ |
| Compilación | .NET Framework 4.8, WinForms, **x64** |
| Proveedor | `Microsoft.ACE.OLEDB.16.0`, con repliegue a `12.0` |
| Usuarias | Personal no técnico — interfaz simple, controles grandes |

**Nota sobre la arquitectura:** el borrador anterior indicaba x86 por suponer un
Office de 32 bits. La instalación real es de 64 bits y el proyecto compila x64.

---

## 3. Objetos reales de la base ✅

Relevado del panel de objetos de Access.

**Tablas:** `Anticipos`, `Asociados`, `Baja`, `Bono`, `Cargo`,
`Errores de pegado`, `Pasivos`, `Servicio`

**Informes:** `Activos`, `Activos1`, `Alta`, `Anticipo`, `Anticipos`,
`Asociados`, `Baja`, `Bono`, `Sueldo`

Dos aclaraciones que resuelven confusiones previas:

- **No existen dos tablas de anticipos.** La tabla es `Anticipos` (plural). El
  `Anticipo` en singular es un **informe**.
- **`Errores de pegado`** es la tabla que Access genera sola cuando falla un
  pegado masivo. Es basura, no un objeto de diseño. Ignorar.

---

## 4. Tabla `Asociados` ✅

Clave: `Documento`. Relevado en vista Diseño.

| Campo | Tipo Access | Notas |
|---|---|---|
| `Documento` 🔑 | Número | Clave de hecho del sistema |
| `Apellido`, `Nombre` | Texto corto | Obligatorios en la aplicación |
| `CUIL` | Número | Prefijo fiscal (20, 23, 27) |
| `Digito` | Número | Dígito verificador |
| `NumeroSocio` | Número | Número de socio de la cooperativa |
| `FechaNacimiento` | Fecha/Hora | |
| `Sexo` | Texto corto | Sin tabla de catálogo |
| `EstadoCivil` | Texto corto | Sin tabla de catálogo |
| `Direccion` | Texto corto | |
| `Telefono` | Texto corto | |
| `Servicio` | Texto corto | **Texto, no FK** |
| `Cargo` | Texto corto | **Texto, no FK** |
| `FechaIngreso` | Fecha/Hora | |
| `FechaBaja` | Fecha/Hora | Nulo = activo |
| `Libro`, `Folio`, `Acta` | Texto | Registro del libro de actas |
| `FechaActa` | Fecha/Hora | |
| `ActaBaja`, `FechaActaBaja` | Texto / Fecha | |
| `Impreso` | Sí/No | Uso desconocido ❓ |
| `Notas` | Texto largo | **Agregada para este sistema** |

**El estado activo/inactivo no es una columna.** Se deriva de `FechaBaja IS
NULL`. Dar de baja escribe la fecha; reactivar la limpia. Nunca se borra una
fila.

**`Documento` es Número, no texto.** El borrador decía lo contrario.
`Convert.ToInt64` es correcto.

---

## 5. Tabla `Bono` ✅

Nombre en **singular**. ~31.007 registros.

| Campo | Tipo Access | Notas |
|---|---|---|
| `Id` 🔑 | Autonumeración | ✅ Verificado. Access lo muestra como "Id de pago" |
| `Fecha` | Fecha/Hora | ⚠️ **No confiable** — ver abajo |
| `PeriodoMes` | Texto corto | Nombre del mes: `"ENERO"`, `"DICIEMBRE"` |
| `PeriodoAño` | Texto corto | Año de 4 dígitos: `"2018"` |
| `Documento` | Número | Copia del asociado, **no es FK** |
| `Nombre`, `Apellido`, `Servicio` | Texto corto | Copias congeladas del asociado |
| `CUIL`, `Digito` | **Texto corto** | ⚠️ Son Número en `Asociados` |
| `Horas`, `ValorHora` | Número | |
| `Basico`, `Mutual`, `Anticipo`, `Otros` | Número | Conceptos **separados** |
| `Comentario`, `OtrosComentario` | Texto corto | Texto libre |

### El bono no usa clave foránea, y está bien

El bono se copia adentro `Documento`, `Nombre`, `Apellido`, `Servicio`, `CUIL` y
`Digito` de la persona. Parece redundancia; no lo es. Si el asociado cambia de
servicio en 2027, el bono de 2019 sigue diciendo el servicio que tenía en 2019.
**El bono congela quién era la persona**, no solo cuánto cobró, y por eso es
autosuficiente para reimprimir.

### `Fecha` no sirve para razonar sobre el período ⚠️

Contradice al período en registros reales:

| Id de pago | `Fecha` | Período |
|---|---|---|
| 31144 | 1/1/**2000** | ENERO **2020** |
| 36361 | 10/1/**2018** | **DICIEMBRE** 2018 |
| 36606 | 2/2/**2016** | ENERO **2018** |

**El período es `PeriodoMes` + `PeriodoAño`, punto.** Filtrar o agrupar por
`Fecha` produce números incorrectos que nadie detecta.

### El mes es texto y no ordena ⚠️

Ordenar `PeriodoMes` alfabéticamente da ABRIL, AGOSTO, DICIEMBRE, ENERO,
FEBRERO… Hace falta traducir nombre de mes a número antes de ordenar o comparar.
Esa función se escribe una sola vez y la usan bonos y anticipos.

### Columnas que el borrador inventaba y no existen

- **`Total`** — se calcula, no se guarda. Correcto: como todos los componentes
  están congelados, el total se reconstruye siempre igual.
- **`Anulado`** — no existe. La anulación se hace escribiendo texto libre en
  `Comentario` (se observó `"BONO ERRONE…"`). **No es consultable de forma
  confiable**: alguien puede escribir "ERRONEO", "ERROR", "ANULADO" o nada.
- **`Impreso`** — no existe en `Bono`, aunque sí en `Asociados`.
- **`Descuentos`** como campo único — son cuatro conceptos separados.

---

## 6. Tabla `Anticipos` ✅

| Campo | Tipo Access | Notas |
|---|---|---|
| `Documento` 🔑 | Número (entero largo) | PK, requerido, **indexado sin duplicados** |
| `Anticipo` | Número | Monto pendiente |
| `PeriodoMes` | Texto corto | ⏳ **A agregar** — misma convención que `Bono` |
| `PeriodoAño` | Texto corto | ⏳ **A agregar** |

**Es un saldo, no un historial.** Con `Documento` como clave sin duplicados,
cada persona tiene **una sola fila**. No puede haber un anticipo de marzo y otro
de abril conviviendo: el segundo pisa al primero.

Eso modela correctamente el proceso real de la oficina: se graba el anticipo
cuando el empleado lo cobra, y no se toca hasta que se emite el bono y se le
paga.

Todo lo que el borrador describía para esta tabla — `IdAnticipo`, `Fecha`,
`Monto`, `Observaciones`, `Descontado`, `IdBono` — **no existe**.

---

## 7. Tabla `Servicio` ✅

| Campo | Tipo Access | Notas |
|---|---|---|
| `Servicio` 🔑 | Texto corto (50) | Única columna. PK, requerido, sin duplicados |

**La columna se llama igual que la tabla.** No existe ninguna columna `Nombre`.

Esta fue la causa del error *"No se han especificado valores para algunos de los
parámetros requeridos"* que impedía editar asociados: la consulta pedía
`SELECT Nombre FROM Servicio`, Access no reconocía el identificador y lo tomaba
por un parámetro sin valor.

## 8. Tabla `Cargo` 🔍

**No relevada.** Se asume la misma convención que `Servicio` — una única columna
llamada `Cargo`. Por eso el código conserva un repliegue: si la consulta falla,
los valores salen de los que ya están en uso en `Asociados`.

## 9. Tablas `Baja` y `Pasivos` ✅ — no se usan

Ambas tienen **un solo registro** y están fuera de uso. No se relevó su
estructura porque no hace falta: ningún módulo las va a tocar. No borrarlas
igual — no cuestan nada y borrar en esta base es lo que dejó 885 bonos
huérfanos la vez anterior.

---

## 10. Impresión del bono — informe de Access ✅

El recibo se imprime desde un informe de Access. **Existen dos informes con el
mismo diseño**: uno se abre con el título `Sueldo` y otro con el título `Bono`
✅ (relevados ambos). Falta confirmar cuál usa la oficina en la práctica ❓ —
para el port da igual, el diseño es el mismo.

### Encabezado fijo del recibo

```
              Cooperativa de Trabajo
    "Sistema de Informaciones Generales" Ltda.
         Rioja 443, Ciudad - Mendoza
           C.U.I.T. 30-62630506-3
```

### Campos y fórmulas leídos en vista Diseño

| Etiqueta en el recibo | Origen |
|---|---|
| Apellido / Nombre | `[Apellido]`, `[Nombre]` |
| CUIL | `[CUIL] - [Documento] - [Digito]` |
| Servicio | `[Servicio]` |
| Período | `[PeriodoMes]` `[PeriodoAño]` |
| Horas × ValorHora | `=[ValorHora]*[Hor…]` |
| **Ley 20337 (2%)** | **`=[Haberes]*0,02`** |
| Seguro | `[Mutual]` |
| Anticipo | `[Anticipo]` |
| Otros / OtrosComentario | `[Otros]`, `[OtrosComentario]` |
| Total Excedentes Repartibles | `=[Basico]+[Total H…]` |
| Total Descuentos | `=[Anticipo]+[Mutu…]` |
| Neto a Cobrar | `=[Haberes]-[Desc…]` |
| Recibí la cantidad de Pesos | `=Enletras([Neto])` |

El pie tiene **líneas de firma** con la leyenda `Asociado:`. No es un listado:
es el recibo que la persona firma.

### Dos hallazgos que cambian el diseño

**1. Existe un descuento del 2% (Ley 20337) que no está en la tabla.** Se
calcula al imprimir sobre `[Haberes]` y no hay ninguna columna en `Bono` que lo
guarde. **Se resta** ✅. La fórmula de `plan.md` no lo contempla. ⚠️ Ver R1.

**2. `Enletras()` es una función VBA que vive dentro de la base, y hoy falla.**
Convierte el importe a letras para el recibo. En la Vista Preliminar relevada, el
renglón "Recibí la cantidad de Pesos" imprime **`#Error`** ⚠️ — es decir, los
recibos que salen hoy no muestran el importe en letras.

Causa más probable: el archivo no está en una ubicación de confianza de Access y
el VBA queda bloqueado, que es el motivo clásico de `#Error` en una función
propia. No bloquea este proyecto —el importe en letras se escribe en C#— pero es
un defecto activo del sistema actual y conviene avisarlo en la oficina.

### Nota sobre el vocabulario

En una cooperativa no se paga "sueldo": se reparten **excedentes repartibles**.
El recibo usa esa terminología y la interfaz debería respetarla.

---

## 11. Pantalla de carga del sistema actual ✅

Relevada del programa en uso (`Cooperativa de Trabajo SIG - [Form1]`).

### Menú del sistema viejo

```
Asociados   Pagos   Altas   Imprimir   Salir
```

`Pagos` es el módulo de bonos — coincide con que la clave de `Bono` se muestre
como "Id de pago": internamente al bono le dicen *pago*. `Imprimir` responde la
pregunta sobre la cuarta opción del menú que quedó abierta en el borrador 0.1.

### Disposición de la pantalla de bono

**Se carga de a un bono por vez**, no en tanda. Encabezado con la persona y el
período, cuerpo con importes, y botones Cerrar / Cancelar / Guardar.

| Zona | Campos |
|---|---|
| Identificación | `Apellido` (se tipea), `Nombre` y `N° CUIL` (se completan solos) |
| Cabecera derecha | `Fecha` (precargada con hoy), `Periodo` = mes desplegable + año |
| Servicio | Desplegable, **se elige en el bono**, no se hereda del asociado |
| Haberes | `Horas` × `ValorHora` = importe; debajo `Comentario` + `Basico` |
| Descuentos | `Ley 20337 (2%)` calculada, `Mutual`, `Anticipo`, `OtrosComentario` + `Otros` |
| Totales | Total de haberes, Total Descuentos, Neto a Cobrar |

### Lo que esta pantalla confirma

- **`Ley 20337` no tiene casilla de carga**: es calculada y mostrada, nunca
  escrita ni guardada ✅.
- **`Otros` está en la columna de descuentos**, junto a Mutual y Anticipo ✅.
- **`Anticipo` se escribe a mano hoy.** La operadora tiene que acordarse de
  consultar el pendiente. Autocompletarlo desde `Anticipos` (ver R5) es una
  mejora sobre el sistema actual, no una réplica.
- **`Fecha` es un campo aparte precargado con la fecha del día**, independiente
  del período. Explica por qué contradice al período en los datos históricos y
  confirma que no sirve para filtrar.
- **El flujo es de a uno.** La pantalla nueva debe ser buscador + ficha con
  avance por Enter, igual que Asociados — no una grilla editable en tanda.

---

## 12. Reglas de negocio

### R1 — Fórmula del bono ✅ completa y verificada

```
Total Horas      = Horas × ValorHora
Haberes          = Basico + Total Horas          ← también rotulado
                                                   "Total Excedentes Repartibles"
Ley 20337        = Haberes × 0,02
Total Descuentos = Mutual + Anticipo + Otros + Ley 20337
Neto a Cobrar    = Haberes − Total Descuentos
```

Equivalente compacto:

```
Neto = (Basico + Horas × ValorHora) × 0,98 − Mutual − Anticipo − Otros
```

**`Haberes` incluye el básico** ✅. Confirmado leyendo la propiedad *Origen del
control* del cuadro `Haberes` en el informe: `=[Basico]+[Total Horas]`.
"Haberes" y "Total Excedentes Repartibles" son el mismo valor con dos etiquetas
distintas en el recibo.

La fórmula que figura en `specs/001-modulo-asociados/plan.md` —
`(Horas × ValorHora) + Basico − Mutual − Anticipo − Otros`— es correcta salvo
que **omite el descuento del 2%**. Ese 2% no está guardado en ninguna columna de
`Bono`: se calcula al imprimir, y hay que recalcularlo igual en el port.

**El 2% se resta y entra en "Total Descuentos"** ✅, junto con seguro, anticipo
y otros.

### Verificación contra recibo impreso ✅

VILLEGAS, CAROLINA ELIZABETH — OSEP VIGILANCIA — ENERO 2020:

| Concepto | Valor | Comprobación |
|---|---|---|
| 228 horas × $40,00 | $9.120,00 | ✅ |
| Ley 20337 (2%) | $182,40 | `9.120 × 0,02` ✅ |
| Seguro / Anticipo / Otros | $0,00 | |
| Total Excedentes Repartibles | $9.120,00 | |
| Total Descuentos | $182,40 | incluye la Ley 20337 ✅ |
| **Neto a Cobrar** | **$8.937,60** | `9.120 − 182,40` ✅ |

Cierra al centavo. La etiqueta **"Seguro" del recibo corresponde a la columna
`Mutual`** de la tabla ✅.

### Confirmación cruzada del 2% sobre el básico ✅

Además del *Origen del control*, los propios datos lo confirman. Bonos de
`COOPERATIVA SIG` sin horas, con solo básico:

| `Basico` | menos 2% | Resultado |
|---:|---:|---|
| $612.244,90 | $12.244,90 | **$600.000,00** exacto |
| $586.734,70 | $11.734,69 | **$575.000,00** exacto |

Esos básicos se fijaron trabajando hacia atrás desde un neto redondo
(`600.000 ÷ 0,98 = 612.244,90`). Si el 2% no se aplicara al básico, no habría
motivo para elegir un número así en lugar de $600.000 directo.

La fila con básico $600.000,00 exacto corresponde a un `PREMIO`: ese se fijó en
bruto, no en neto. Lógica distinta y consistente.

Tres hallazgos que salieron de mirar los datos, no de suponer:

- **Todo se guarda en positivo.** Los signos los pone la fórmula, no los datos.
  No hay un solo valor negativo en la base.
- **`Basico` se suma, no reemplaza.** En el 53% de los bonos convive con
  horas × valor hora; en el 43% solo hay horas; en el 2% solo básico.
- **`Mutual` no es fijo.** Fue 120 entre 2016 y 2021, pasó a 250 en 2022, y
  también aparecen 0, 500, 700 y 3000.

Ese último punto manda sobre todo el diseño: **un bono de 2019 reimpreso hoy
tiene que descontar 120, no 250.**

### R2 — Valores congelados ✅

Cada bono guarda sus propios importes y sus propias copias de los datos del
asociado. **Jamás se leen de una tabla de referencia al reimprimir.**

### R3 — Baja lógica, nunca borrado

Dar de baja escribe `FechaBaja`. Nunca se elimina una fila. El sistema viejo
borraba, y dejó 885 bonos huérfanos.

### R4 — Anulación de bonos ⚠️ no implementable hoy

La regla "los bonos no se borran, se anulan" **no se puede cumplir de forma
consultable**: no existe columna `Anulado` y hoy se hace por texto libre en
`Comentario`. Pendiente de decisión.

### R5 — Descuento del anticipo en el bono ✅ decidido

Flujo acordado:

1. Se graba el anticipo cuando el empleado lo cobra, con su mes y año.
2. Al cargar un bono, se busca en `Anticipos` por `Documento` + `PeriodoMes` +
   `PeriodoAño`. Si hay coincidencia, el monto se **copia** al campo `Anticipo`
   del bono y el total lo resta.
3. **Al grabar el bono, la fila de `Anticipos` se pone en cero.** El anticipo
   dejó de estar pendiente: ya vive congelado dentro del bono.

Poner la fila en cero resuelve tres cosas: un segundo bono del mismo período no
encuentra nada que descontar, la pregunta "¿ya se lo descontamos?" se responde
mirando la tabla, y la fila queda libre para el próximo anticipo.

**Si se intenta cargar un anticipo a alguien que ya tiene uno pendiente**, el
sistema avisa e invita a modificar el existente. No se ofrece un botón de
"reemplazar": si alguien pide más plata en el mismo mes, lo correcto es que el
pendiente suba, y un reemplazo de un clic convierte un error en plata regalada.

### R6 — El cruce nunca va por `Fecha`

Ni para anticipos ni para bonos. Solo `PeriodoMes` + `PeriodoAño`.

---

## 13. Estrategia de datos

| Entidad | Estrategia | Motivo |
|---|---|---|
| `Asociados` | Padrón completo en memoria, filtrado en cliente | ~4.202 filas. Más rápido y simple que consultar por tecla. |
| `Bono` | **Consulta filtrada por documento** | ~31.007 filas. **No traer el histórico entero.** |
| `Anticipos` | Consulta puntual por documento | Una fila por persona. |

---

## 14. Pendientes

| # | Pendiente | Cómo se resuelve | Bloquea |
|---|---|---|---|
| 1 | Agregar `PeriodoMes` y `PeriodoAño` a `Anticipos` | Vista Diseño → dos filas nuevas | El módulo de anticipos |
| 2 | Medidas del papel y si es preimpreso | Configurar página del informe | La impresión |
| 3 | Fórmulas del informe cortadas a la derecha | Foto de la vista Diseño con la columna de cálculo completa | La impresión |
| 4 | Estructura de `Cargo` | Vista Diseño | Nada (hay repliegue) |
| 5 | Por qué `Enletras()` devuelve `#Error` hoy | Probable VBA bloqueado por ubicación no confiable. Avisar en la oficina | Nada (se reescribe en C#) |
| 6 | Cuál de los dos informes usa la oficina | Preguntar. Para el port da igual: mismo diseño | Nada |
| 7 | Qué hacer si el anticipo supera al bono | Decisión de negocio | Nada por ahora |
| 8 | Cómo anular bonos de forma consultable | Decisión de negocio | R4 |
| 9 | Discrepancia de conteo: 31.007 vs 33.578 en `plan.md` | Puede ser la base de la otra oficina | Nada |

**Resuelto:** la base de cálculo del 2% de Ley 20337 — ver R1. Era el pendiente
que bloqueaba todo el módulo de bonos.

---

## 15. Estado del sistema

**Módulo de Asociados: terminado y en uso.** Búsqueda por documento o apellido
con filtrado mientras se tipea, filtro por servicio, alta y edición con los
campos completos, campo único que acepta CUIT o DNI, baja lógica y
reactivación, y respaldo automático al arrancar.

**Módulo de Bonos: no iniciado, pero ya no bloqueado.** El esquema de `Bono` y
la fórmula completa están verificados. La impresión sigue necesitando las
medidas del papel (pendiente #2).

**Módulo de Anticipos: no iniciado.** Bloqueado por el pendiente #1 — faltan las
dos columnas de período en la tabla.
