using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Motor de Arte Vectorial Procedural y Síntesis de Audio en tiempo de ejecución para OrgRise.
/// Genera Sprites 9-slice con bordes redondeados y antialiasing SDF, círculos vectoriales,
/// fondos de grilla corporativa, sombras suaves y clips de audio sin depender de configuración manual.
/// </summary>
public static class VectorUIKit
{
    private static Sprite cachedRoundedCardSprite;
    private static Sprite cachedPillButtonSprite;
    private static Sprite cachedCircleSprite;
    private static Sprite cachedRingSprite;
    private static Sprite cachedGridSprite;

    // Paleta de Colores Comercial (Executive Dark & Vibrant Vector Accents)
    public static readonly Color BgDeepNavy = new Color(0.055f, 0.082f, 0.145f, 1f);       // #0E1525
    public static readonly Color BgPanelSlate = new Color(0.094f, 0.137f, 0.224f, 0.98f);  // #182339
    public static readonly Color BgCardSurface = new Color(0.125f, 0.176f, 0.278f, 1f);    // #202D47
    public static readonly Color AccentGold = new Color(0.984f, 0.749f, 0.141f, 1f);       // #FBBF24
    public static readonly Color AccentEmerald = new Color(0.063f, 0.725f, 0.506f, 1f);    // #10B981
    public static readonly Color AccentCyan = new Color(0.024f, 0.714f, 0.831f, 1f);       // #06B6D4
    public static readonly Color AccentIndigo = new Color(0.388f, 0.400f, 0.945f, 1f);     // #6366F1
    public static readonly Color AccentPurple = new Color(0.545f, 0.361f, 0.965f, 1f);     // #8B5CF6
    public static readonly Color AccentCoral = new Color(0.937f, 0.267f, 0.267f, 1f);      // #EF4444

    /// <summary>
    /// Devuelve un Sprite vectorial 9-slice con esquinas redondeadas y antialiasing por distancia (SDF).
    /// </summary>
    public static Sprite GetRoundedSprite(bool pill = false)
    {
        if (pill && cachedPillButtonSprite != null) return cachedPillButtonSprite;
        if (!pill && cachedRoundedCardSprite != null) return cachedRoundedCardSprite;

        int size = 128;
        float radius = pill ? 42f : 26f;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float half = size * 0.5f;
        float boxExtent = half - radius - 1.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = Mathf.Abs(x + 0.5f - half) - boxExtent;
                float py = Mathf.Abs(y + 0.5f - half) - boxExtent;
                float qx = Mathf.Max(px, 0f);
                float qy = Mathf.Max(py, 0f);
                float dist = Mathf.Sqrt(qx * qx + qy * qy) + Mathf.Min(Mathf.Max(px, py), 0f) - radius;

                float alpha = Mathf.Clamp01(0.5f - dist);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        float slice = radius + 4f;
        Vector4 border = new Vector4(slice, slice, slice, slice);
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);

        if (pill) cachedPillButtonSprite = sp;
        else cachedRoundedCardSprite = sp;

        return sp;
    }

    /// <summary>
    /// Devuelve un Sprite circular perfecto con antialiasing para pines de conexión, indicadores y partículas.
    /// </summary>
    public static Sprite GetCircleSprite()
    {
        if (cachedCircleSprite != null) return cachedCircleSprite;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float center = size * 0.5f;
        float radius = center - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) - center;
                float dy = (y + 0.5f) - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                float alpha = Mathf.Clamp01(0.5f - dist);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedCircleSprite;
    }

    /// <summary>
    /// Devuelve un Sprite de anillo circular (aro) para destacar puertos activos.
    /// </summary>
    public static Sprite GetRingSprite()
    {
        if (cachedRingSprite != null) return cachedRingSprite;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float center = size * 0.5f;
        float outerR = center - 2f;
        float innerR = center - 8f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) - center;
                float dy = (y + 0.5f) - center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float aOuter = Mathf.Clamp01(0.5f - (d - outerR));
                float aInner = Mathf.Clamp01(0.5f + (d - innerR));
                pixels[y * size + x] = new Color(1f, 1f, 1f, aOuter * aInner);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedRingSprite;
    }

    /// <summary>
    /// Devuelve un patrón de grilla táctica sutil para el fondo del tablero.
    /// </summary>
    public static Sprite GetBlueprintGridSprite()
    {
        if (cachedGridSprite != null) return cachedGridSprite;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isMajorLine = (x == 0 || y == 0);
                bool isCrossDot = ((x == 32 && y >= 30 && y <= 34) || (y == 32 && x >= 30 && x <= 34));
                float a = isMajorLine ? 0.055f : (isCrossDot ? 0.08f : 0f);
                pixels[y * size + x] = new Color(0.55f, 0.75f, 0.98f, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        cachedGridSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        return cachedGridSprite;
    }

    /// <summary>
    /// Estiliza cualquier Image con bordes redondeados vectoriales 9-slice y sombra suave.
    /// </summary>
    public static void ApplyRoundedStyle(Image img, Color color, bool pill = false, bool addShadow = true, Vector2? shadowOffset = null)
    {
        if (img == null) return;
        img.sprite = GetRoundedSprite(pill);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1.2f;
        img.color = color;

        if (addShadow)
        {
            Shadow sh = img.GetComponent<Shadow>();
            if (sh == null) sh = img.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.42f);
            sh.effectDistance = shadowOffset ?? new Vector2(0f, -6f);
        }
    }

    /// <summary>
    /// Agrega un contorno sutil (Outline) alrededor de un componente gráfico.
    /// </summary>
    public static void ApplyOutline(Graphic graphic, Color outlineColor, float width = 1.5f)
    {
        if (graphic == null) return;
        Outline ol = graphic.GetComponent<Outline>();
        if (ol == null) ol = graphic.gameObject.AddComponent<Outline>();
        ol.effectColor = outlineColor;
        ol.effectDistance = new Vector2(width, -width);
    }

    /// <summary>
    /// Sintetiza una pista musical ambiental corporativa (loop limpio de 12s) en memoria.
    /// </summary>
    public static AudioClip CreateProceduralCorporateBGM()
    {
        int sampleRate = 22050;
        float duration = 12f;
        int totalSamples = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[totalSamples];

        float[][] chords = new float[][]
        {
            new float[] { 261.63f, 329.63f, 392.00f, 493.88f }, // Cmaj7
            new float[] { 220.00f, 261.63f, 329.63f, 392.00f }, // Am7
            new float[] { 174.61f, 220.00f, 261.63f, 329.63f }, // Fmaj7
            new float[] { 196.00f, 246.94f, 293.66f, 329.63f }  // G6
        };

        float chordLen = duration / chords.Length;
        for (int i = 0; i < totalSamples; i++)
        {
            float t = i / (float)sampleRate;
            int cIdx = Mathf.FloorToInt(t / chordLen) % chords.Length;
            float localT = t % chordLen;
            float env = Mathf.Pow(Mathf.Sin(Mathf.PI * (localT / chordLen)), 0.35f);

            float val = 0f;
            float[] notes = chords[cIdx];
            for (int n = 0; n < notes.Length; n++)
            {
                float f = notes[n];
                val += 0.20f * Mathf.Sin(2f * Mathf.PI * f * t);
                val += 0.05f * Mathf.Sin(2f * Mathf.PI * (f * 2f) * t);
            }

            float lfo = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
            samples[i] = Mathf.Clamp(val * env * lfo * 0.35f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ProceduralCorporateBGM", totalSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Sintetiza un efecto de sonido melódico limpio (para conexiones, pistas, errores o victoria).
    /// </summary>
    public static AudioClip CreateProceduralChime(float[] freqs, float noteDuration = 0.08f, float volume = 0.45f)
    {
        int sampleRate = 22050;
        float totalDuration = freqs.Length * noteDuration + 0.12f;
        int totalSamples = Mathf.RoundToInt(sampleRate * totalDuration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = i / (float)sampleRate;
            int idx = Mathf.Min(freqs.Length - 1, Mathf.FloorToInt(t / noteDuration));
            float localT = t - idx * noteDuration;
            float f = freqs[idx];
            float env = Mathf.Exp(-localT * 15f);
            float val = Mathf.Sin(2f * Mathf.PI * f * t) + 0.28f * Mathf.Sin(2f * Mathf.PI * (f * 2f) * t);
            samples[i] = Mathf.Clamp(val * env * volume, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ProceduralSFX", totalSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
