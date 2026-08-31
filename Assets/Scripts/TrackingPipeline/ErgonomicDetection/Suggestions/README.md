# Sugerencias híbridas: primera iteración

El sistema combina evidencia de participación motora con exposición postural. Sus reglas y límites son propios del prototipo; no producen un score RULA ni una estimación clínica de riesgo, fuerza, fatiga o compensación anatómica confirmada.

## Flujo y responsabilidades

`MotionEventBus` → `UsageInterpreter` → `FrameUsageData`.

`ErgonomicEventBus` → intérprete de exposición existente → `ErgonomicExposureEventBus`.

`HybridSuggestionTrackingSystem` empareja estas salidas por mano, frameId y timestamp mediante `HybridFramePairer`. `SuggestionDecisionOrchestrator` combina ambas evidencias; `HybridSuggestionGate` limita emisiones. `HybridSuggestionEventBus.OnSuggestion` publica la decisión con las dos evidencias originales, además del log. El snackbar es opcional.

No se cambian los agregadores, detectores, métricas, reglas ni motores del sistema anterior. Consumir `FrameMotionData` sí implica depender de las señales producidas por sus detectores; la independencia es de lógica y estado, no de procedencia de los datos.

## Qué significa participación

Durante los últimos 5 segundos válidos se integra `MotionData.value × dt`, por separado para muñeca y antebrazo. La participación de muñeca es su integral dividida por la suma de ambas integrales. Las dos participaciones suman uno cuando hay señal suficiente. La mano no forma parte de ese denominador; se conservan además tres ratios de tiempo activo por zona usando `isActive`.

Son proporciones de señales normalizadas del detector, no porcentajes del esfuerzo ni grados de libertad anatómicos. Dependen de la normalización, filtros y frecuencia de captura de los detectores actuales. Un objetivo de 35/65 no es un valor RULA. La baja actividad no autoriza recomendar más movimiento de una articulación.

Se exige una ventana observada mínima de 2 s y una señal rotacional media mínima de 0.01 para reglas de coordinación. Los intervalos se ponderan por tiempo; el primer frame, los saltos >0.25 s, los valores no finitos, las zonas ausentes/duplicadas y los frames no crecientes no añaden tiempo. Tras una interrupción se exige continuidad nueva. El buffer tiene 2048 intervalos por mano; si se llena, se descarta el más antiguo y se informa la cobertura temporal efectiva en `observedSeconds`.

## Reglas y objetivos

La calibración angular y los umbrales de exposición se leen del mismo asset que usa `ErgonomicExposureTrackingSystem` (por defecto 60 s acumulados o continuos). No se duplican estos umbrales en el perfil de sugerencias.

| Condición | Salida |
| --- | --- |
| Dimensión conocida, actualmente fuera de rango y exposición elevada | Volver a una posición más neutra; para giro, reducir el giro sin forzar. |
| Condición anterior + muñeca >=65%, antebrazo <=35%, giro neutral y objetivo `IncludeForearm` | Reducir participación de muñeca e involucrar antebrazo. |
| Muñeca <=35%, antebrazo >=65%, las tres dimensiones válidas/neutrales, sin alertas históricas y objetivo `IncludeWrist` | Incluir muñeca manteniendo una postura neutra. |

La coordinación debe mantenerse 2 s. Se requieren 2 s de calentamiento válido por mano. La protección postural no requiere actividad motora: una postura estática también puede generar exposición. Una alerta acumulada histórica no genera un aviso correctivo cuando la mano ya está neutral. Una dimensión inválida o deshabilitada impide recomendar aumentar articulaciones; no se interpreta como postura segura. El aumento de muñeca queda bloqueado después de una alerta de exposición durante el ejercicio, aun al volver a neutral.

La prioridad es ordinal: sostenida (2), acumulada (1), objetivo motor (0). Nunca se suman ángulos, porcentajes y segundos en una puntuación. La regla de redistribución conserva la prioridad de su causa postural. Entre manos gana la prioridad mayor; si empatan, la duración mayor de la condición y después la izquierda. Una recomendación postural actual bloquea las instrucciones de aumentar movimiento de la otra mano, aunque aquella ya se haya emitido. Se comparan candidatos frescos en `LateUpdate`, sin encolar mensajes antiguos. Máximo 3 por ejercicio, 8 s de cooldown global y deduplicación por mano/tipo.

## Configuración en Unity

Las escenas Insert, OSU y DuckHunter tienen `HybridSuggestionTrackingSystem` en GameManager, junto al tracker de exposición. Comparten `DefaultHybridSuggestionProfile` y la calibración angular existente. Los parámetros se toman al comenzar cada ejercicio; cambiar configuración entre ejercicios, no durante una captura.

- `leftGoal` / `rightGoal`: por defecto `ObserveOnly`. Solo permite protección postural. Seleccionar `IncludeWrist` o `IncludeForearm` cuando el objetivo motor de ese ejercicio lo justifique. No se inventan objetivos terapéuticos para las escenas existentes.
- `output`: por defecto `LogOnly`. El híbrido procesa y publica eventos, pero el baseline conserva la pantalla. `LogAndSnackbar` muestra también el híbrido usando el adaptador existente; **no desactiva el baseline**. Evitar activar las dos salidas visuales para una evaluación comparativa.
- `suggestionProfile`: ventana, mínimos de señal, bandas de participación y control de notificaciones propios del prototipo.
- `calibrationProfile`: debe ser el mismo asset asignado al tracker de exposición de esa escena.

El inicio (`OnExcerciseStart`) reinicia ambos intérpretes de uso, emparejamiento, reglas y anti-spam. El final (`OnExerciseEnd`) deja de procesar, elimina pendientes y registra cantidad de emisiones/descartes. Deshabilitar el componente cancela las suscripciones y no reanuda a mitad de ejercicio sin un nuevo inicio.

## Comparación y límites

La fase actual mantiene el baseline sin cambios y ejecuta el híbrido en observación. Los logs incluyen mano, identidad, regla, causa, duración y participación; el evento lleva las evidencias completas para un futuro consumidor. No incluye persistencia, resultados de usuarios ni conmutación automática al híbrido como sistema principal.

Comparar comprensión de mensajes, pertinencia percibida, interrupciones, errores de interpretación y usabilidad. Una mejora de usabilidad no demuestra prevención de lesiones, eficacia terapéutica, precisión clínica del seguimiento ni adherencia longitudinal. Antes de retirar el baseline hacen falta esa comparación y una revisión de los objetivos de cada ejercicio. El giro palma–antebrazo del SDK debe validarse antes de interpretarlo como pronación/supinación anatómica.
