# Suelta: plan de 4 sprints

Criterio de priorización: prioridad de la HU (Crítica > Alta > Media > Baja), sus dependencias obligatorias y el orden de entrega del documento de HU/HT (hitos 0 a 4). Los puntos son la "estimación de esfuerzo" de cada HU.

Regla de trabajo: una rama por sprint (`SPRINT-1` ... `SPRINT-4`), que se sube a GitHub al terminar el sprint.

## Sprint 1: Recorrido mínimo (hablar, entender, guardar, ver)

Objetivo: abrir la app, dar permisos, hablar o escribir una frase, que se interprete y se vea en la lista.

| ID | Historia | Prioridad | Pts |
|----|----------|-----------|-----|
| HU-01 | Ver la bienvenida | Alta | 2 |
| HU-02 | Dar los permisos de micrófono y avisos | Alta | 3 |
| HU-04 | Hablar para crear un recordatorio | Crítica | 5 |
| HU-05 | Revisar y corregir la frase antes de guardar | Alta | 2 |
| HU-06 | Escribir en vez de hablar | Media | 2 |
| HU-07 | Guardar un cumpleaños | Alta | 3 |
| HU-11 | Usar fechas como las digo | Alta | 3 |
| HU-12 | Responder una pregunta cuando falta el día | Crítica | 5 |
| HU-15 | Ver la lista de próximos recordatorios | Crítica | 5 |

Historias técnicas: HT-01 (solución .NET 10), HT-03 (voz), HT-04 (parser en C# + pruebas), HT-05 (SQLite), HT-08 (estado de la lista, parte base), HT-14 (pruebas unitarias del parser).

Total: 30 pts de HU.

## Sprint 2: App completa sin avisos

Objetivo: terminar toda la experiencia de captura, organización y edición.

| ID | Historia | Prioridad | Pts |
|----|----------|-----------|-----|
| HU-03 | Ver una frase de ejemplo | Media | 1 |
| HU-08 | Separar una frase larga en recordatorios conectados | Media | 5 |
| HU-09 | Guardar recordatorios que se repiten | Media | 3 |
| HU-10 | Asignar la categoría automáticamente | Media | 3 |
| HU-13 | Elegir una opción cuando digo una sola palabra | Alta | 3 |
| HU-14 | Elegir la fecha en un calendario | Media | 2 |
| HU-16 | Ver el resumen de la carga | Baja | 1 |
| HU-17 | Ver los recordatorios por categoría | Alta | 5 |
| HU-18 | Ver lo que dije originalmente | Baja | 1 |
| HU-19 | Marcar un recordatorio como hecho | Alta | 3 |
| HU-24 | Deshacer justo después de guardar | Media | 2 |
| HU-25 | Editar o eliminar un recordatorio | Alta | 5 |

Historias técnicas: HT-02 (prototipo a Razor con estilos y fuente Inter), HT-07 (motor de repeticiones), HT-08 (completar: categorías, agrupación, deshacer).

Total: 34 pts de HU.

## Sprint 3: Avisos y APK

Objetivo: que la app cumpla la promesa "ella se acuerda" y se pueda instalar.

| ID | Historia | Prioridad | Pts |
|----|----------|-----------|-----|
| HU-20 | Recibir el aviso a la hora | Crítica | 5 |
| HU-21 | Resolver el aviso desde la notificación | Alta | 3 |
| HU-22 | Recibir un aviso anticipado de un cumpleaños | Media | 2 |
| HU-23 | Mantener los avisos después de reiniciar el teléfono | Alta | 3 |

Historias técnicas: HT-06 (avisos locales), HT-14 (pruebas de repeticiones y manuales en teléfonos), HT-15 (APK/AAB firmado), HT-16 (integración continua), HT-17 (registro de errores sin guardar frases).

Total: 13 pts de HU.

## Sprint 4: Respaldo

Objetivo: cambiar de teléfono sin perder nada.

Decisiones del equipo para este sprint:

- La base de datos del servidor es **SQLite** con EF Core, no PostgreSQL (HT-10 cambia de motor; la API sigue siendo ASP.NET Core).
- **HU-27 y HT-12 (interpretación avanzada con Claude) se aplazan**: el parser local resuelve las frases del documento y la clave de Claude solo puede vivir en un servidor.

| ID | Historia | Prioridad | Pts |
|----|----------|-----------|-----|
| HU-26 | Respaldar mis recordatorios y recuperarlos en otro teléfono | Media | 5 |

Historias técnicas: HT-09 (API ASP.NET Core con identidad anónima por código y límite de peticiones), HT-10 (esquema en SQLite con migraciones), HT-11 (sincronización offline primero, gana la última modificación), HT-13 (privacidad: pantalla de Ajustes, "Borrar mis datos" y política), HT-14 (pruebas de integración de la API contra SQLite real), HT-18 (Dockerfile, copia diaria de la base y guía de despliegue).

Total: 5 pts de HU.

Aplazado: HU-27 (5 pts) y HT-12.

## Cobertura

26 de 27 HU y 17 de 18 HT hechas. Aplazadas: HU-27 y HT-12.

## Puntos abiertos que afectan los sprints

- Hora por defecto de los avisos (9:00 am): por confirmar (Sprint 3).
- "Sin cuentas" frente al código de recuperación: por decidir (Sprint 4).
- Dónde se aloja el servidor: por decidir (Sprint 4).
- Voz sin conexión: validar en tres teléfonos reales (Sprint 1 y 3).
