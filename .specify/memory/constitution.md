# Constitution — Sistema de Gestión de Asociados y Bonos

Principios no negociables. Toda especificación, plan e implementación debe respetarlos. Ante conflicto entre este documento y cualquier otro, prevalece este.

## I. La base de datos existente es intocable

El sistema opera sobre un archivo Microsoft Access `.mdb` en producción con más de 33.000 registros históricos. **No se modifica el esquema**: no se agregan, renombran ni eliminan tablas ni columnas, y no se convierte el archivo a otro formato.

Todo defecto detectado en los datos se corrige en la capa de aplicación, nunca alterando la estructura. Si una funcionalidad parece exigir un cambio de esquema, se rediseña la funcionalidad.

## II. Ningún dato se destruye

No existe operación de borrado físico en el sistema.

- Las bajas de asociados son lógicas: se completa `FechaBaja` y el registro permanece consultable.
- Las entradas obsoletas de catálogos se marcan como inactivas, no se eliminan.
- Los registros históricos deben seguir siendo consultables e imprimibles indefinidamente.

Este principio existe porque el sistema anterior borraba filas al dar de baja, lo que produjo 885 bonos huérfanos irrecuperables.

## III. Los comprobantes son inmutables

Un bono emitido representa un hecho ocurrido en una fecha. Todos los valores que determinan su importe se conservan en su propio registro y jamás se leen de tablas de referencia al reimprimir.

Reimprimir un bono de cualquier año debe producir exactamente el mismo documento que se emitió originalmente, sin importar cuántas veces hayan cambiado los valores de referencia desde entonces.

## IV. El entorno manda sobre las preferencias técnicas

El sistema debe funcionar en las máquinas que la oficina ya tiene: Windows 10, hardware de bajos recursos, Microsoft Office 2016 de 64 bits ya instalado.

Ninguna decisión técnica puede exigir actualizar el sistema operativo, el hardware o el Office. No se introducen dependencias que requieran instalación adicional en las máquinas de la oficina.

## V. Menos clics que el sistema anterior

Toda pantalla se evalúa contra el sistema que reemplaza. Si una operación cotidiana requiere más pasos que antes, la pantalla está mal diseñada.

- El foco inicia siempre en el campo de búsqueda.
- Enter avanza; Escape cancela.
- Toda operación frecuente debe ser completable sin usar el mouse.
- Las usuarias no tienen perfil técnico: los mensajes de error se escriben en lenguaje llano, sin códigos ni jerga.

## VI. Validar en la aplicación, no confiar en la base

La base no tiene integridad referencial declarada y no puede tenerla, porque los datos históricos inconsistentes harían fallar las relaciones. Por lo tanto:

- Toda regla de integridad se valida en código antes de escribir.
- Ningún bono ni anticipo se graba sin verificar que el asociado exista.
- Los datos preexistentes que violan reglas actuales se toleran en lectura y se rechazan en escritura.

## VII. Respaldo antes que nada

La aplicación respalda automáticamente el archivo de base de datos al iniciar, antes de cualquier operación. Los datos son locales a cada máquina y no hay servidor: el respaldo automático es la única red de seguridad existente.

## VIII. Cada instalación es independiente

Existen dos instalaciones con bases separadas y sin relación entre sí. Se distribuye un único ejecutable idéntico; lo único que difiere es la configuración externa.

No se compilan versiones distintas por instalación. No se asume acceso concurrente.

---

**Versión:** 1.0.0 | **Ratificada:** 2026-07-31
