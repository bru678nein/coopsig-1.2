# Sistema de Gestión de Asociados, Bonos y Anticipos

**Versión del documento:** 0.1 (borrador)
**Estado:** en definición — ver sección 11 "Blancos pendientes"

---

## 1. Objetivo

Reemplazar el programa actual de la oficina por un sistema de escritorio que gestione asociados (trabajadores), bonos y anticipos, manteniendo la base de datos Microsoft Access existente sin migración de datos.

### Problemas del sistema actual que hay que resolver

| Problema | Solución en el nuevo sistema |
|---|---|
| El buscador solo busca por nombre y apellido | Buscador único que detecta DNI vs. texto |
| Buscar "González" devuelve 40 resultados indistinguibles | Grilla con apellido, nombre, DNI y servicio en la misma fila |
| Demasiados clics para operaciones comunes | Foco automático en el buscador, doble clic abre edición, Enter avanza campos |

---

## 2. Restricciones

| Restricción | Detalle |
|---|---|
| Sistema operativo | Windows 10 (confirmar con `winver` en ambas máquinas) |
| Hardware | PCs viejas, pocos recursos — el programa debe arrancar rápido y pesar poco |
| Base de datos | Microsoft Access existente, **no se migra** |
| Puestos de trabajo | 2 PCs, ambas con Office/Access instalado, viendo los mismos registros |
| Usuarias | Personal no técnico — la interfaz tiene que ser simple y con controles grandes |

---

## 3. Stack técnico

| Componente | Elección | Motivo |
|---|---|---|
| Lenguaje / framework | C# + WinForms sobre .NET Framework 4.8 | Preinstalado en Windows 10: cero despliegue de runtime, arranque rápido |
| Acceso a datos | `System.Data.OleDb` con `Microsoft.ACE.OLEDB.12.0` | Driver ya presente al tener Access instalado |
| Impresión | `System.Drawing.Printing.PrintDocument` | Permite posicionar cada dato al milímetro sobre la plantilla |
| Compilación | **Plataforma x86** (no "Any CPU") | Si el Office instalado es de 32 bits, el driver ACE no carga desde un proceso de 64 bits |

### Alternativa descartada

**Nota:** este documento es un borrador previo. El stack vigente está en `specs/001-modulo-asociados/plan.md`.

---

## 4. Arquitectura y despliegue

```
PC 1 (principal)                    PC 2
├── Aplicación (.exe local)         ├── Aplicación (.exe local)
└── Carpeta compartida              │
    ├── base.accdb  ◄───────────────┘  (acceso por red cableada)
    └── /backups/
        └── base_AAAAMMDD_HHMMSS.accdb
```

### Reglas de despliegue

- El archivo de Access vive en **una sola** carpeta compartida; el ejecutable se instala local en cada PC.
- **Conexión por cable, no WiFi.** La corrupción de bases Access en red es casi siempre por caídas de conexión durante una escritura.
- **Backup automático al abrir la aplicación:** copiar el archivo a `/backups/` con timestamp antes de cualquier operación. Conservar los últimos N backups (ver blancos pendientes).
- Access soporta sin problemas la concurrencia de 2 usuarios: bloquea a nivel de registro, no de tabla.

### Capas del código

```
/UI          → Formularios WinForms
/Servicios   → Reglas de negocio y validaciones
/Datos       → Repositorios con OleDb (una clase por entidad)
/Modelos     → Clases planas (Asociado, Bono, Anticipo, Servicio)
/Impresion   → Plantillas y lógica de PrintDocument
```

La capa de Datos es la única que conoce OleDb. Esto deja la puerta abierta a migrar a SQLite o PostgreSQL más adelante cambiando solo esa carpeta.

---

## 5. Modelo de datos

> **Nota:** el esquema real de la base existente todavía no fue relevado. Este modelo es el objetivo; hay que mapearlo contra las tablas actuales antes de escribir código.

### Asociados

| Campo | Tipo Access | Notas |
|---|---|---|
| IdAsociado | Autonumérico | PK |
| Apellido | Texto (60) | Obligatorio |
| Nombre | Texto (60) | Obligatorio |
| DNI | Texto (15) | Obligatorio, único, indexado |
| CUIL | Texto (15) | *(pendiente de confirmar — ver blancos)* |
| FechaNacimiento | Fecha/Hora | |
| Domicilio | Texto (120) | |
| Teléfono | Texto (30) | |
| IdServicio | Numérico | FK → Servicios |
| FechaAlta | Fecha/Hora | Obligatorio |
| Activo | Sí/No | Por defecto: Sí |
| FechaBaja | Fecha/Hora | Nulo mientras esté activo |
| Observaciones | Memo | |

**DNI como texto, no numérico.** Evita perder ceros a la izquierda y permite documentos extranjeros.

### Servicios

| Campo | Tipo Access | Notas |
|---|---|---|
| IdServicio | Autonumérico | PK |
| Nombre | Texto (60) | Único |
| Activo | Sí/No | |

Tabla separada, no texto libre: si se escribe a mano, en un año conviven "Limpieza", "limpieza" y "Limpiez" como tres servicios distintos y no se puede filtrar ni totalizar nada.

### Bonos

| Campo | Tipo Access | Notas |
|---|---|---|
| IdBono | Autonumérico | PK |
| IdAsociado | Numérico | **FK obligatoria** → Asociados |
| Periodo | Texto (7) | Formato AAAA-MM |
| Horas | Numérico (Doble) | |
| ValorHora | Moneda | **Congelado al momento de grabar** |
| Descuentos | Moneda | *(estructura pendiente de definir)* |
| Total | Moneda | Calculado y persistido |
| FechaEmision | Fecha/Hora | |
| Impreso | Sí/No | |
| Anulado | Sí/No | |

### Anticipos

| Campo | Tipo Access | Notas |
|---|---|---|
| IdAnticipo | Autonumérico | PK |
| IdAsociado | Numérico | **FK obligatoria** → Asociados |
| Fecha | Fecha/Hora | |
| Monto | Moneda | |
| Observaciones | Texto (200) | |
| Descontado | Sí/No | Si ya se aplicó a un bono |
| IdBono | Numérico | FK opcional → Bonos *(pendiente de confirmar)* |

### Diagrama de relaciones

```
Servicios (1) ──< (N) Asociados (1) ──< (N) Bonos
                              │
                              └────< (N) Anticipos
```

---

## 6. Reglas de negocio

**R1 — Todo bono pertenece a un asociado.** Un asociado puede tener muchos bonos; un bono pertenece a exactamente uno. No puede existir un bono huérfano. Se garantiza con FK obligatoria más integridad referencial declarada en Access.

**R2 — Los anticipos siguen la misma regla.** Todo anticipo pertenece a exactamente un asociado.

**R3 — Baja lógica, nunca borrado físico.** Dar de baja a un asociado marca `Activo = No` y carga `FechaBaja`. Nunca se elimina la fila: los bonos históricos dejarían de tener a quién apuntar y se rompe todo el archivo anterior. El "eliminar asociado" del menú ejecuta una baja lógica.

**R4 — El bono congela sus propios valores.** `ValorHora`, `Horas`, `Descuentos` y `Total` se guardan en la fila del bono al momento de grabarlo. Si el valor hora cambia en junio, un bono de marzo reimpreso tiene que seguir mostrando el valor de marzo.

**R5 — El DNI es único entre asociados.** Al cargar un alta, validar que no exista otro asociado con ese DNI (activo o inactivo) y avisar antes de guardar.

**R6 — Un asociado inactivo no puede recibir bonos nuevos.** Aparece en consultas e históricos, pero no en el selector de carga de bonos.

**R7 — Los bonos no se borran, se anulan.** Marcar `Anulado = Sí` en lugar de eliminar la fila.

---

## 7. Pantallas

### Menú principal

Cuatro botones grandes:

1. **Bonos**
2. **Anticipos**
3. **Asociados**
4. *(cuarta opción pendiente de definir — probablemente reportes/impresión)*

### Patrón común de pantalla de entidad

Una sola pantalla por entidad en lugar de opciones separadas para consultar / editar / agregar:

```
┌──────────────────────────────────────────────┐
│  [ buscador ..................... ]          │
│  ( ) Solo activos   ( ) Todos                │
├──────────────────────────────────────────────┤
│  Apellido │ Nombre │ DNI │ Servicio │ Estado │
│  ...........................................  │
│  ...........................................  │
├──────────────────────────────────────────────┤
│  [ Nuevo ]  [ Editar ]  [ Ver ]  [ Baja ]    │
└──────────────────────────────────────────────┘
```

### Buscador inteligente (crítico)

Un único campo de texto:

- Si el contenido es **todo numérico** → busca por DNI (coincidencia por prefijo).
- Si contiene **letras** → busca por apellido y nombre.
- Filtra **mientras se tipea**, sin botón "Buscar".
- El foco arranca siempre acá al abrir la pantalla.

### Carga de bono — flujo objetivo

1. Tipear DNI en el buscador → Enter (el asociado queda seleccionado y sus datos se muestran arriba).
2. Cargar horas, valor hora y descuentos, avanzando con Enter.
3. Grabar e imprimir.

Meta: cargar un bono sin tocar el mouse.

---

## 8. Impresión de bonos

**Sección pendiente de relevamiento.**

Requisito conocido: los datos del bono se sobreimprimen sobre una plantilla preexistente y sale el bono completo.

A definir tras revisar el programa actual:

- ¿La plantilla es papel preimpreso o una imagen/PDF que se imprime junto con los datos?
- Medidas exactas del formulario y coordenadas de cada campo.
- Tamaño de papel y orientación.
- ¿Se imprime de a uno o por lote (todos los bonos del período)?
- ¿Hay copias (original/duplicado)?

---

## 9. Criterios de usabilidad

- Botones y tipografía grandes; nada de iconos sin texto.
- **Enter avanza al campo siguiente** en todos los formularios de carga.
- Doble clic en una fila de grilla abre la edición directamente.
- Confirmación explícita solo en acciones destructivas (baja, anulación).
- Mensajes de error en castellano llano, sin códigos técnicos.
- La aplicación abre directamente en el menú principal, sin login *(a confirmar — ver blancos)*.

---

## 10. Fuera de alcance (versión 1)

- Usuarios y permisos.
- Reportes estadísticos o exportación a Excel.
- Cálculo automático de aportes o cargas sociales.
- Migración de la base a otro motor.

---

## 11. Blancos pendientes

| # | Pendiente | Cómo se resuelve |
|---|---|---|
| 1 | Esquema real de la base Access actual (nombres de tablas y campos) | Abrir la base y documentar tablas, campos y tipos |
| 2 | Formato del archivo: `.mdb` o `.accdb` | Mirar la extensión del archivo |
| 3 | ¿La base tiene macros o módulos VBA con lógica? | Revisar en Access; si los hay, esa lógica hay que replicarla |
| 4 | Cuarta opción del menú principal | Revisar el programa actual |
| 5 | ¿Se guarda CUIL, CUIT o ambos? | Cambia la validación del dígito verificador |
| 6 | Estructura de los descuentos del bono: ¿monto único o conceptos separados? | Revisar un bono real |
| 7 | Detalle completo de campos del bono | Revisar la pantalla de carga actual |
| 8 | Todo lo de la sección 8 (impresión) | Revisar el programa actual e imprimir un bono de muestra |
| 9 | ¿Los anticipos se descuentan automáticamente del bono del período? | Preguntar cómo se hace hoy |
| 10 | ¿Hace falta login por usuario? | Preguntar |
| 11 | Cantidad de backups a conservar | Definir (sugerencia: últimos 30) |
| 12 | Arquitectura del Office instalado (32 o 64 bits) | En Access: Archivo → Cuenta → Acerca de |
