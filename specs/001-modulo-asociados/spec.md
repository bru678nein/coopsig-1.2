# Feature 001 — Gestión de Asociados

**Estado:** borrador
**Alcance:** primera entrega funcional. Bonos, anticipos e impresión quedan fuera.

## Contexto

Una oficina administrativa gestiona el padrón de sus asociados (trabajadores) usando un programa de escritorio antiguo con problemas de usabilidad y de integridad de datos. Se reemplaza ese programa manteniendo la base de datos existente.

El padrón actual tiene 4.202 asociados. La operación diaria consiste en buscar una persona, consultar o corregir sus datos, dar de alta a quien ingresa y dar de baja a quien deja de trabajar.

## Problema

El sistema actual falla en tres puntos concretos:

1. **La búsqueda no discrimina.** Buscar por apellido devuelve decenas de resultados sin forma de distinguirlos, cuando el dato que la oficina tiene a mano es el documento.
2. **Cada operación cuesta demasiados pasos.** Consultar, editar y agregar están en pantallas separadas, obligando a navegar entre menús para tareas cotidianas.
3. **Las bajas destruyen información.** Dar de baja elimina el registro, dejando sin referencia a los comprobantes históricos de esa persona.

## Historias de usuario

### HU-1 — Encontrar un asociado (Prioridad: P1)

Como administrativa, quiero encontrar a una persona escribiendo lo que tenga a mano —su documento o su apellido— para acceder a sus datos sin navegar menús.

**Por qué es P1:** es la operación más frecuente del sistema y la que más tiempo desperdicia hoy. Entregada sola, ya representa una mejora tangible.

**Criterios de aceptación:**

- Existe un único campo de búsqueda que recibe el foco automáticamente al abrir la pantalla.
- Si el texto ingresado es enteramente numérico, la búsqueda se resuelve por documento.
- Si el texto contiene letras, la búsqueda se resuelve por apellido y nombre.
- Los resultados se actualizan a medida que se escribe, sin necesidad de confirmar.
- Cada resultado muestra apellido, nombre, documento, servicio y estado, de modo que dos personas del mismo apellido sean distinguibles sin abrir sus fichas.
- Buscando el documento completo de un asociado existente, el resultado correcto aparece en menos de un segundo.

---

### HU-2 — Consultar y corregir datos (Prioridad: P1)

Como administrativa, quiero abrir la ficha de una persona y corregir sus datos, para mantener el padrón actualizado.

**Criterios de aceptación:**

- La ficha se abre desde el resultado de búsqueda con una sola acción.
- Se muestran todos los datos registrados de la persona.
- Al confirmar, los cambios quedan guardados y visibles en el listado.
- Al cancelar, ningún cambio se conserva.
- Los datos que el sistema no puede validar por estar incompletos en el padrón histórico se muestran tal como están y pueden guardarse sin corregirse.

---

### HU-3 — Dar de alta un asociado (Prioridad: P1)

Como administrativa, quiero registrar a una persona que ingresa, para poder emitirle comprobantes.

**Criterios de aceptación:**

- Se exige apellido, nombre, documento y servicio. El resto es opcional.
- El sistema impide registrar un documento que ya existe en el padrón, indicando de quién es.
- El identificador fiscal es opcional: hay casos en que solo se cuenta con el documento, y el registro debe poder guardarse igual.
- Si el identificador fiscal se completa parcial o totalmente, se valida su coherencia; si se deja vacío, no se reclama.
- El servicio se elige de una lista existente, no se escribe libremente.
- La fecha de ingreso se propone con la fecha del día y puede corregirse.

---

### HU-4 — Dar de baja y reactivar (Prioridad: P2)

Como administrativa, quiero registrar que una persona dejó de trabajar sin perder su historial, para poder consultar sus comprobantes anteriores.

**Criterios de aceptación:**

- Dar de baja registra la fecha de baja y marca a la persona como inactiva.
- El registro sigue existiendo y sigue siendo consultable.
- Ningún camino de la interfaz permite eliminar definitivamente a un asociado.
- Por defecto el listado muestra solo activos; una opción visible permite incluir a los dados de baja.
- Una persona dada de baja puede reactivarse, lo que limpia su fecha de baja.
- El estado de cada persona es visible en el listado sin abrir su ficha.

---

### HU-5 — Trabajar con teclado (Prioridad: P2)

Como administrativa que carga datos todo el día, quiero completar las operaciones frecuentes sin soltar el teclado.

**Criterios de aceptación:**

- Enter avanza al campo siguiente dentro de una ficha.
- Escape cierra la ficha sin guardar.
- El recorrido por teclado sigue el orden visual de los campos.
- Buscar una persona y abrir su ficha es posible sin usar el mouse.

---

### HU-6 — Confiar en que los datos están a salvo (Prioridad: P2)

Como responsable de la oficina, quiero que exista una copia reciente de la información, para poder recuperarla ante una falla.

**Criterios de aceptación:**

- Al iniciar la aplicación se genera automáticamente una copia de respaldo de la base.
- Cada copia es identificable por fecha y hora.
- Se conservan las últimas 30 copias; las más antiguas se descartan.
- Si el respaldo falla, se informa con claridad y el sistema permite continuar trabajando.
- La aplicación indica en todo momento sobre qué base se está trabajando.

## Fuera de alcance

- Bonos, anticipos e impresión de comprobantes.
- Usuarios, contraseñas y permisos.
- Corrección de los 885 comprobantes históricos huérfanos: se toleran y no se generan nuevos.
- Depuración de las entradas obsoletas de catálogos.
- Migración de datos o cambios en la estructura de la base.
- Acceso remoto, web o multiusuario.

## Éxito medible

- Registrar un alta completa toma menos de 90 segundos.
- Localizar a una persona por documento toma menos de 5 segundos desde que se abre la pantalla.
- Transcurrido un mes de uso, la cantidad de comprobantes huérfanos no aumentó.
- La operación cotidiana no requiere consultar un manual.

## Pendientes de definición

- `[NEEDS CLARIFICATION]` Qué campos del padrón deben ser editables y cuáles solo consultables.
- `[NEEDS CLARIFICATION]` Si al dar de baja se requiere registrar un motivo.
