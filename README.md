# 🏢 OrgRise - Puzzle de Organigramas

## 📌 Resumen del Proyecto
OrgRise es un juego de puzzles para móviles donde el jugador debe armar organigramas funcionales conectando diferentes roles bajo restricciones específicas de gestión. 

Este proyecto tiene un doble propósito: funciona como el trabajo final integrador para la materia **Gestión de la Organización** y, al mismo tiempo, está diseñado como un producto viable para la Play Store con un fuerte enfoque en el contenido visual "satisfying" para redes sociales.

## 🎮 Mecánicas Principales y Loop de Juego
El jugador recibe un conjunto de "empleados" (nodos) y un objetivo estructural para armar la jerarquía.
* **Conexión de nodos:** Se trazan líneas de reporte directo entre un superior y sus subordinados.
* **Validación en tiempo real:** El sistema resalta errores como cuellos de botella, ciclos cerrados o excesos en el tramo de control.
* **Feedback "Satisfying":** Al resolver el organigrama correctamente, se activa una animación de encastre con un sonido satisfactorio, haciendo que la estructura cobre vida.
* **Departamentalización:** Cada "mundo" o paquete de niveles cambia las reglas de agrupamiento (funcional, por producto, geográfica) a modo de skins mecánicos.

## ⚙️ Especificaciones Técnicas
* **Motor:** Unity 6000.3.10f1.
* **Entorno:** 2D, utilizando uGUI para la interfaz.
* **Plataforma Objetivo:** Android (Google Play) en orientación vertical.
* **Persistencia de Datos:** Guardado local de progreso del jugador y rachas de conexión.
* **Estructura de Niveles:** Definidos mediante ScriptableObjects y JSON para permitir la generación de puzzles diarios de forma procedimental.

## 📚 Justificación Académica (Gestión de la Organización)
El diseño del juego traduce directamente la teoría a mecánicas jugables:
* **Cadena de mando** = Mecánica base de conexión jerárquica padre-hijo.
* **Tramo de control** = Restricción numérica de reportes directos permitidos por nodo.
* **Departamentalización** = "Skins" de reglas distintas según el mundo o nivel.
* **Centralización vs. Descentralización** = Puzzles que obligan a distribuir la autoridad en lugar de concentrarla en un solo nodo.

## 🚀 Roadmap / Próximos Pasos (To-Do)
- [ ] Desarrollar el prototipo jugable de las mecánicas base (conexión, tramo de control y validación.
- [ ] Realizar playtesting interno y ajustar la curva de dificultad.
- [ ] Implementar el sistema de racha y el generador de puzzle diario.
- [ ] Integrar monetización: AdMob (video recompensado) y compras in-app (IAP) cosméticas.
- [ ] Pulir el arte final de los 4 skins temáticos (Startup, Corporación, Gobierno, ONG).
- [ ] Grabar clips de 15-20 segundos del gameplay final para la campaña en redes.
