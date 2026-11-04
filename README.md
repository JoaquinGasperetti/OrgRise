# 🏢 OrgRise - Puzzle de Diseño Organizacional

## 📌 Resumen del Proyecto
**OrgRise** es un juego de puzzles en 2D para dispositivos móviles desarrollado en **Unity 6 (`6000.3.10f1`)** donde el jugador diseña organigramas funcionales conectando distintos roles empresariales bajo restricciones reales de gestión.

Este proyecto funciona como **Trabajo Final Integrador para la materia Gestión de la Organización**, traduciendo conceptos clásicos de diseño estructural (Mintzberg, Fayol, Robbins) en mecánicas interactivas con validación en tiempo real y evaluación de eficiencia estructural.

---

## 📚 Articulación Teórico-Práctica (Gestión de la Organización)

Cada regla de validación del motor de juego responde directamente a un principio de la asignatura:

| Concepto Académico | Implementación en el Juego (Mecánica) | Script Responsable |
| :--- | :--- | :--- |
| **Cadena de Mando y Jerarquía** | Los reportes solo pueden fluir desde el Nivel Operativo (`EmpleadoBase`) hacia el Nivel Táctico (`Gerente`), Nivel Ejecutivo (`Director`) y Nivel Estratégico (`CEO`), respetando el conducto regular. | `EmployeeNode.cs`, `OrgValidator.cs` |
| **Unidad de Mando vs. Estructura Matricial** | En estructuras clásicas, cada subordinado posee un único superior directo (`manager`). En el modo **Matricial**, se habilita el **Doble Mando real**, permitiendo reportar simultáneamente a un Gerente Funcional y a un Gerente de Proyecto transversal (`matrixProjectManager`). | `EmployeeNode.cs`, `OrgValidator.cs` |
| **Órganos de Staff y Asesoría** | Introducción de puestos de asesoría directa a la Alta Dirección (CEO o Directores) según la tipología de Mintzberg. Se representan con conexiones doradas diferenciadas y no consumen tramo operativo. | `EmployeeNode.cs`, `UILineConnection.cs` |
| **Tramo de Control (*Span of Control*)** | Cada mando posee un límite estricto de reportes directos (`Tramo: X/Max`). Al completarse el cupo, el indicador cambia a rojo para evitar cuellos de botella y sobrecarga de supervisión. | `EmployeeNode.cs`, `ProceduralLevelGenerator.cs` |
| **Departamentalización** | Agrupamiento por criterios: **Funcional** (Finanzas, Comercial, Operaciones, Tecnología), **Por Producto** (PyMEs, Consumo, Industria, Cloud), **Geográfica** (Regiones Norte, Centro, Sur, Exterior) y **Matricial** (Proyectos Alfa, Beta, Gamma). | `LevelManager.cs`, `ProceduralLevelGenerator.cs` |
| **Centralización vs. Descentralización** | **Estructura Centralizada (Chata):** Tramo de control amplio en la cúpula y menos niveles intermedios.<br>**Estructura Descentralizada (Vertical):** Tramo de control estrecho que obliga al CEO a delegar autoridad táctica en mandos medios. | `LevelManager.cs`, `ProceduralLevelGenerator.cs` |
| **Evaluación Cuantitativa y KPIs** | Al validar la estructura, se computa un **Scorecard Académico** con costo de masa salarial / overhead, distancia jerárquica media y calificación de **1 a 3 Estrellas** según el equilibrio de la estructura. | `OrgValidator.cs`, `LevelManager.cs` |
| **Costo de Reestructuración** | Mover o desconectar un mando medio con subordinados a cargo requiere activar un **Comodín de Reestructuración** (Consultoría Organizacional). | `WildcardManager.cs`, `UINodeInteraction.cs` |

---

## 🎮 Flujo de Niveles de la Campaña y Desafío Diario

El juego incluye una campaña progresiva de **5 niveles académicos** respaldados por ScriptableObjects (`Assets/Levels/`) y un **Modo Desafío Diario Procedural**:

1. **Nivel 1 — Cadena de Mando y Tramo de Control:** Introducción a la estructura lineal-funcional básica.
2. **Nivel 2 — Departamentalización por Producto:** Agrupamiento divisional por líneas de negocio (PyMEs, Consumo).
3. **Nivel 3 — Departamentalización Geográfica:** Organización territorial por zonas operativas (Norte, Centro, Sur).
4. **Nivel 4 — Estructura Centralizada (Horizontal / Chata):** Tramo de control amplio y supervisión directa desde la cúpula.
5. **Nivel 5 — Descentralizada con Órgano de Staff:** Tramo estrecho con delegación obligatoria y Asesor Legal Staff asistiendo a Presidencia (Mintzberg).
6. **Modo Desafío Diario Infinito:** Generación procedural con semilla determinista diaria (`yyyyMMdd`), racha acumulable y solubilidad matemática garantizada.

---

## 🕹️ Controles e Interacción

* **Conectar roles:** Arrastrar una línea desde un subordinado hacia su superior (o viceversa). El motor traza **conectores ortogonales escalonados en codo**, evitando el cruce diagonal sobre otras tarjetas.
* **Deshacer conexión (Undo):** Presionar el botón **↶ Deshacer** en la barra superior para revertir el último movimiento sin penalización de comodín.
* **Desconectar un rol:** Doble toque / doble clic sobre el nodo subordinado (si tiene subordinados a cargo, requiere comodín).
* **Comodín de Reestructuración:** Presionar **Comodín** para trasladar o desconectar una rama activa. Se adquieren completando niveles o mirando videos bonificados (Opt-In).
* **Menú de Ajustes:** Control de audio para efectos (SFX), música ambiental (BGM), respuesta háptica móvil (vibración) y reinicio de progreso.

---

## ⚙️ Especificaciones Técnicas y Cumplimiento

* **Motor:** Unity `6000.3.10f1` (Universal Render Pipeline 2D).
* **Interfaz:** Unity uGUI + TextMeshPro con escalado responsivo portrait (`1080x1920`) y áreas seguras para anuncios.
* **Persistencia:** Guardado local con `PlayerPrefs` para:
  - Racha diaria (`LastPlayDate`, `CurrentStreak`).
  - Progreso de campaña desbloqueada (`OrgRise_NivelMaxDesbloqueado`, `OrgRise_IndiceNivelCampana`).
  - Estrellas obtenidas por nivel (`OrgRise_Nivel_X_Estrellas`).
  - Balance de comodines (`OrgRise_WildcardsDisponibles`).
  - Preferencias de audio y háptica.
* **Monetización y Privacidad (Google AdMob v11.6.0):**
  - Soporte de **Modo Seguro de Pruebas** (`modoPruebas = true` / `#if DEVELOPMENT_BUILD`) con IDs de prueba oficiales de Google para evitar suspensiones por tráfico inválido.
  - Flujo de **Consentimiento UMP (GDPR / Privacidad)** integrado antes de la carga de anuncios.
