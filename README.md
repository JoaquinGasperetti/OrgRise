# 🏢 OrgRise — Corporate Strategy & Puzzle Tycoon (Play Store Edition)

## 📌 Resumen Comercial
**OrgRise** es un juego comercial de estrategia y puzzles para dispositivos móviles desarrollado en **Unity 6 (`6000.3.10f1`)**, diseñado para maximizar la retención de audiencia, la rejugabilidad y la rentabilidad mediante **Google AdMob (v11.6.0)**.

El jugador asume el rol de un **Arquitecto Corporativo** que escala compañías desde una *Startup Fintech* hasta una *Cúpula Fortune 500*, conectando líderes y especialistas bajo desafíos de capacidad, consultoría VIP (*Staff*), estructuras de *Doble Mando Matricial* y rescates empresariales en *Modo Crisis*.

---

## ✨ Novedades de la Edición Comercial 100%

### 1. Arte Vectorial Procedural y Efectos Visuales (`VectorUIKit.cs`)
* **Tarjetas Vectoriales Redondeadas (SDF 9-Slice):** Generación automática de bordes redondeados con antialiasing, cinta superior de jerarquía, sombras suaves (`Shadow`) y contornos (`Outline`).
* **Pines de Conexión y Pulso de Datos en Vivo:** Cada tarjeta cuenta con puerto superior de entrada y puerto inferior de supervisión, conectados por líneas ortogonales con pulso animado continuo.
* **Previsualización Inteligente al Arrastrar:** Al trazar una línea sobre otro nodo, el conector y el borde del objetivo se iluminan en **Verde Esmeralda** (conexión válida), **Púrpura** (Doble Mando Matricial) o **Rojo Coral** (conexión inválida).
* **Partículas Vectoriales, Confeti y Audio Integrado:** Explosiones de partículas al conectar, lluvia de confeti en cada victoria y síntesis automática de música ambiental corporativa (`bgm_corporate.wav`) y efectos de sonido (`sfx_error.wav`, `sfx_hint.wav`).

### 2. Retención y Rejugabilidad (`LevelManager.cs`, `DailyStreakManager.cs`, `OrgValidator.cs`)
* **Campaña de 15 Niveles Comerciales en 3 Etapas:**
  - **Etapa 1 — Startups (Niveles 1–5):** *Garage Fintech*, *Lanzamiento SaaS*, *Escala E-Commerce*, *Despliegue Regional* y *Auditoría & Staff VIP*.
  - **Etapa 2 — Scaleups & Squads (Niveles 6–10):** *Squads Matriciales (Doble Mando)*, *Crisis en Logística*, *Holding Multirregional*, *Fusión Corporativa M&A* y *Unicornio Tech ($1B)*.
  - **Etapa 3 — Imperios Globales (Niveles 11–15):** *Red Global 4 Regiones*, *Rescate Bancario*, *Consorcio Aeroespacial*, *Toma de Control Hostil* y *Cúpula Fortune 500*.
* **Sistema de Carrera Corporativa y Trofeos:** Ascenso por 6 rangos ejecutivos (*🌱 Fundador Startup* → *🌍 Leyenda de Wall Street*) según las estrellas (`45 ⭐` posibles) y puntos de reputación (`XP`).
* **Hitos de Racha Diaria y "Salva-Racha":** Premios automáticos de Comodines al cumplir 3, 7, 14 y 30 días de racha, más opción de recuperar una racha perdida mirando un video bonificado.

### 3. Monetización Rentable con Anuncios (`AdMobManager.cs`, `WildcardManager.cs`, `TutorialManager.cs`)
* **5 Formatos de AdMob Integrados + Consentimiento UMP (GDPR):**
  1. **Banner Adaptativo (Anchored Adaptive Banner):** Fijo en la franja inferior segura.
  2. **App Open Ad:** Monetiza cada apertura de la app y el regreso desde segundo plano.
  3. **Intersticial Estándar:** En transiciones naturales con cooldown optimizado de 35s.
  4. **Intersticial Bonificado:** En fin de nivel con modal opt-out reglamentario.
  5. **4 Ganchos de Video Bonificado (Rewarded Ads) de Alta Conversión:**
     - **🎯 Auto-Pista / Headhunter (en partida):** Conecta automáticamente un puesto difícil en su lugar óptimo.
     - **⭐ Subir a 3 Estrellas + Bono (en pantalla de victoria):** Permite mejorar una calificación de 1 o 2 estrellas a **3⭐** y sumar comodines extra.
     - **🔥 Salva-Racha (en menú principal):** Recupera la racha diaria si el jugador olvidó conectarse el día anterior.
     - **⚡ Inyección de Capital:** Recarga directa de +2 Comodines desde el tablero o el menú.

---

## 🚀 Paso Final antes de Publicar en Google Play Store
En `Assets/Scripts/AdMobManager.cs` (o en el Inspector del objeto `AdMobManager`), cambiá `modoPruebas = false` cuando generes el `.aab` (Android App Bundle) de producción para activar tus bloques de anuncios reales.
