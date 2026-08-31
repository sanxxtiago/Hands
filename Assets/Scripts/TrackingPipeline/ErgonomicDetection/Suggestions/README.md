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

Las escenas Insert, OSU y DuckHunter tienen `HybridSuggestionTrackingSystem` en GameManager, junto al tracker de exposición. Cada escena usa su propio `HybridExerciseProfile`, su `HybridRuntimeProfile` y su `ErgonomicCalibrationProfile` en `Assets/Resources/ErgonomicProfiles/`. Los assets Default permanecen como referencia. Cambiar configuración entre ejercicios, no durante una captura.

- `exerciseProfile`: configura desempeño, objetivos finales, calibración y runtime por ejercicio. Sus objetivos runtime por mano están inicialmente en `ObserveOnly`; seleccionar `IncludeWrist` o `IncludeForearm` solo cuando el objetivo motor del ejercicio lo justifique. `coordinationEnabled = false` fuerza observación en vivo y desactiva coordinación final.
- `output`: por defecto `LogOnly`. El híbrido procesa y publica eventos, pero el baseline conserva la pantalla. `LogAndSnackbar` muestra también el híbrido usando el adaptador existente; **no desactiva el baseline**. Evitar activar las dos salidas visuales para una evaluación comparativa.
- `suggestionProfile` y `leftGoal` / `rightGoal` del componente se mantienen por compatibilidad con instancias antiguas. Con `exerciseProfile` asignado se toman los valores del perfil al iniciar.
- `calibrationProfile`: el asset referenciado por el perfil del ejercicio debe ser el mismo asignado al tracker de exposición de esa escena. El cierre verifica esa identidad y usa desempeño como respaldo si no coincide.

El inicio (`OnExcerciseStart`) reinicia ambos intérpretes de uso, emparejamiento, reglas y anti-spam. El final (`OnExerciseEnd`) deja de procesar, elimina pendientes y registra cantidad de emisiones/descartes. Deshabilitar el componente cancela las suscripciones y no reanuda a mitad de ejercicio sin un nuevo inicio.

## Comparación y límites

La fase actual mantiene el baseline sin cambios y ejecuta el híbrido runtime en observación. Los logs incluyen mano, identidad, regla, causa, duración y participación; el evento lleva las evidencias completas para un futuro consumidor. La sugerencia final híbrida sí alimenta resultados y persistencia. No incluye resultados de usuarios ni conmutación automática del feedback runtime.

## Sugerencia final por ejercicio

`OnExerciseEnd` detiene los trackers. `SessionRecorder` captura ambos resúmenes en cualquier orden. Al terminar todos los handlers, `GameManager.OnExerciseFinalizing` construye y confirma el resultado una sola vez; después `OnShowResults` permite a `ResultsManager` poblar la UI. El texto queda en `LastGeneralSuggestion` y en el campo existente `ExerciseSummary.generalSuggestion`. No se amplía el modelo persistido.

`HybridFinalSuggestionBuilder` es independiente del baseline `GeneralSuggestionBuilder`. Prioriza exposición sostenida, luego acumulada, luego objetivos finales de uso; añade una segunda línea lógica de desempeño. Las líneas pueden ocupar varios renglones por ajuste de texto. El panel de resultados conserva su tamaño de fuente y amplía el espacio disponible.

Para el resultado final se conserva `maximumSustainedExposureSeconds`: el episodio continuo más largo de todo el ejercicio. `sustainedExposureSeconds` sigue siendo el episodio actual al detenerse. La alerta sostenida del resumen usa el máximo, aunque el usuario haya corregido la postura. `validObservationSeconds` cuenta intervalos válidos por dimensión y permite distinguir ausencia de exposición de ausencia de datos. Estos campos son transitorios, no se guardan en `ExerciseSummary`.

Los umbrales se leen únicamente de la calibración asociada: actualmente 60 s acumulados / 60 s sostenidos por ejercicio. No se escalan automáticamente por la duración esperada (90 s iniciales). Cambiar cada asset de calibración permite estudiar otros tiempos del prototipo sin atribuirlos a RULA.

Los objetivos finales usan `HandUsageSummary.relativeUsage`, cuyo denominador incluye actividad de **mano + muñeca + antebrazo**. Insert inicia con muñeca 0.40, antebrazo 0.20 y tolerancia 0.05 para ambas manos. OSU y DuckHunter contienen valores editables de referencia, con coordinación desactivada hasta definir sus objetivos. Estos valores no se trasladan a las bandas 0.35/0.65 de señal rotacional en vivo: son magnitudes distintas.

La coordinación requiere arrays de uso completos, únicos y finitos, suma relativa unitaria, actividad suficiente y al menos 2 s observados por dimensión. Cero exposición no genera elogios posturales. Si existe exposición de giro en esa mano, no se recomienda redistribuir hacia el antebrazo. Una exposición registrada, aun inferior al umbral, bloquea la conclusión genérica de coordinación sin exposición. Ante exposición elevada, las recomendaciones de desempeño no presionan para ganar velocidad ni prolongar posturas incómodas.

La sincronización delimita cada ejercicio con inicio/cierre y duración común de los buses síncronos; no es un protocolo para resultados remotos o asíncronos tardíos. Si falta exposición, hay desacuerdo de duración/calibración o el perfil es inválido, se registra un diagnóstico y se conserva el desempeño baseline. Sin resumen de uso no se confirma un resultado incompleto. Sin datos de desempeño válidos ni otra conclusión, se informa que los datos son insuficientes.

Comparar comprensión de mensajes, pertinencia percibida, interrupciones, errores de interpretación y usabilidad. Una mejora de usabilidad no demuestra prevención de lesiones, eficacia terapéutica, precisión clínica del seguimiento ni adherencia longitudinal. Antes de retirar el baseline hacen falta esa comparación y una revisión de los objetivos de cada ejercicio. El giro palma–antebrazo del SDK debe validarse antes de interpretarlo como pronación/supinación anatómica.
