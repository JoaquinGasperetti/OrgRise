using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance;

    public const string PrefsKeySFX = "OrgRise_SFX_Enabled";
    public const string PrefsKeyBGM = "OrgRise_BGM_Enabled";
    public const string PrefsKeyHaptics = "OrgRise_Haptics_Enabled";

    [Header("Audio")]
    public AudioClip snapSound;
    public AudioClip victorySound;
    public AudioClip backgroundMusicClip;
    private AudioClip errorClip;
    private AudioClip hintClip;

    private AudioSource audioSource;
    private AudioSource bgmAudioSource;

    private GameObject toastObject;
    private TextMeshProUGUI toastText;
    private Image toastBg;
    private Coroutine toastCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioSource = GetComponent<AudioSource>();
        InicializarClipsProceduralesYFaltantes();
        ConfigurarBGM();
    }

    private void InicializarClipsProceduralesYFaltantes()
    {
        if (backgroundMusicClip == null)
        {
            backgroundMusicClip = Resources.Load<AudioClip>("Audio/bgm_corporate");
            if (backgroundMusicClip == null)
                backgroundMusicClip = VectorUIKit.CreateProceduralCorporateBGM();
        }

        errorClip = Resources.Load<AudioClip>("Audio/sfx_error");
        if (errorClip == null)
            errorClip = VectorUIKit.CreateProceduralChime(new float[] { 190f, 145f }, 0.11f, 0.45f);

        hintClip = Resources.Load<AudioClip>("Audio/sfx_hint");
        if (hintClip == null)
            hintClip = VectorUIKit.CreateProceduralChime(new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.07f, 0.45f);

        if (snapSound == null)
            snapSound = VectorUIKit.CreateProceduralChime(new float[] { 587.33f, 880f }, 0.055f, 0.40f);

        if (victorySound == null)
            victorySound = VectorUIKit.CreateProceduralChime(new float[] { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f }, 0.085f, 0.50f);
    }

    private void ConfigurarBGM()
    {
        bgmAudioSource = gameObject.AddComponent<AudioSource>();
        bgmAudioSource.loop = true;
        bgmAudioSource.volume = 0.26f;
        bgmAudioSource.playOnAwake = false;

        if (backgroundMusicClip != null && IsBGMEnabled())
        {
            bgmAudioSource.clip = backgroundMusicClip;
            bgmAudioSource.Play();
        }
    }

    public bool IsSFXEnabled() => PlayerPrefs.GetInt(PrefsKeySFX, 1) == 1;
    public bool IsBGMEnabled() => PlayerPrefs.GetInt(PrefsKeyBGM, 1) == 1;
    public bool IsHapticsEnabled() => PlayerPrefs.GetInt(PrefsKeyHaptics, 1) == 1;

    public void SetSFXEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(PrefsKeySFX, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetBGMEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(PrefsKeyBGM, enabled ? 1 : 0);
        PlayerPrefs.Save();
        if (bgmAudioSource != null)
        {
            if (enabled && backgroundMusicClip != null)
            {
                if (!bgmAudioSource.isPlaying) bgmAudioSource.Play();
            }
            else
            {
                bgmAudioSource.Stop();
            }
        }
    }

    public void SetHapticsEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(PrefsKeyHaptics, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void TriggerHapticFeedback(bool error = false)
    {
        if (!IsHapticsEnabled()) return;

        #if UNITY_ANDROID || UNITY_IOS
        try
        {
            Handheld.Vibrate();
        }
        catch { }
        #endif
    }

    public void PlaySnapFeedback(Transform nodeTransform, string mensaje = "")
    {
        if (IsSFXEnabled() && snapSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.96f, 1.08f);
            audioSource.PlayOneShot(snapSound, 0.9f);
        }

        TriggerHapticFeedback(false);

        if (nodeTransform != null)
        {
            StartCoroutine(PopAnimation(nodeTransform));
            SpawnMiniBurst(nodeTransform.position, VectorUIKit.AccentEmerald, 8);
        }

        if (!string.IsNullOrEmpty(mensaje))
        {
            MostrarMensajeEstado(mensaje, new Color(0.06f, 0.44f, 0.31f, 0.96f), 2.1f);
        }
    }

    public void PlayHintFeedback(Transform nodeTransform, string mensaje = "")
    {
        if (IsSFXEnabled() && hintClip != null && audioSource != null)
        {
            audioSource.pitch = 1.0f;
            audioSource.PlayOneShot(hintClip, 0.95f);
        }

        TriggerHapticFeedback(false);

        if (nodeTransform != null)
        {
            StartCoroutine(PopAnimation(nodeTransform));
            SpawnMiniBurst(nodeTransform.position, VectorUIKit.AccentGold, 14);
        }

        if (!string.IsNullOrEmpty(mensaje))
        {
            MostrarMensajeEstado(mensaje, new Color(0.55f, 0.36f, 0.06f, 0.96f), 2.8f);
        }
    }

    public void PlayErrorFeedback(EmployeeNode targetNode, string motivo)
    {
        if (IsSFXEnabled() && audioSource != null)
        {
            AudioClip clipToPlay = errorClip != null ? errorClip : snapSound;
            if (clipToPlay != null)
            {
                audioSource.pitch = errorClip != null ? 1.0f : 0.65f;
                audioSource.PlayOneShot(clipToPlay, 0.8f);
            }
        }

        TriggerHapticFeedback(true);

        if (targetNode != null)
        {
            StartCoroutine(ShakeAndFlashRed(targetNode));
        }

        if (!string.IsNullOrEmpty(motivo))
        {
            MostrarMensajeEstado($"⚠️ {motivo}", new Color(0.68f, 0.14f, 0.16f, 0.97f), 3.4f);
        }
    }

    public void PlayVictoryFeedback(List<EmployeeNode> nodes)
    {
        if (IsSFXEnabled() && victorySound != null && audioSource != null)
        {
            audioSource.pitch = 1.0f;
            audioSource.PlayOneShot(victorySound, 1.0f);
        }

        TriggerHapticFeedback(false);
        StartCoroutine(AnimateNodesSequentially(nodes));
        SpawnVictoryConfetti();
    }

    public void MostrarMensajeEstado(string texto, Color colorFondo, float duracion = 3.0f)
    {
        AsegurarBannerUI();
        if (toastObject == null) return;

        if (toastCoroutine != null) StopCoroutine(toastCoroutine);
        toastCoroutine = StartCoroutine(MostrarToastRoutine(texto, colorFondo, duracion));
    }

    private void AsegurarBannerUI()
    {
        if (toastObject != null) return;

        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        toastObject = new GameObject("CommercialToastBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        toastObject.transform.SetParent(mainCanvas.transform, false);

        RectTransform rt = toastObject.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.anchorMin = new Vector2(0.04f, 0.074f);
        rt.anchorMax = new Vector2(0.96f, 0.132f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        toastBg = toastObject.GetComponent<Image>();
        VectorUIKit.ApplyRoundedStyle(toastBg, new Color(0.12f, 0.18f, 0.28f, 0.96f), pill: true, addShadow: true);
        VectorUIKit.ApplyOutline(toastBg, new Color(1f, 1f, 1f, 0.25f), 1.2f);
        toastBg.raycastTarget = false;

        GameObject txtObj = new GameObject("ToastText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(toastObject.transform, false);

        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.localScale = Vector3.one;
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(18f, 6f);
        txtRt.offsetMax = new Vector2(-18f, -6f);

        toastText = txtObj.GetComponent<TextMeshProUGUI>();
        toastText.alignment = TextAlignmentOptions.Center;
        toastText.color = Color.white;
        toastText.fontStyle = FontStyles.Bold;
        toastText.enableAutoSizing = true;
        toastText.fontSizeMin = 12f;
        toastText.fontSizeMax = 22f;
        toastText.raycastTarget = false;

        toastObject.SetActive(false);
    }

    private IEnumerator MostrarToastRoutine(string texto, Color colorFondo, float duracion)
    {
        toastObject.SetActive(true);
        toastObject.transform.SetAsLastSibling();
        toastBg.color = colorFondo;
        toastText.text = texto;
        yield return new WaitForSeconds(duracion);
        toastObject.SetActive(false);
    }

    // ==================== PARTÍCULAS VECTORIALES Y CONFETI ====================

    public void SpawnMiniBurst(Vector3 worldPos, Color baseColor, int count = 10)
    {
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        for (int i = 0; i < count; i++)
        {
            StartCoroutine(AnimateVectorParticle(mainCanvas.transform, worldPos, baseColor, false));
        }
    }

    public void SpawnVictoryConfetti()
    {
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        Color[] confettiColors = new Color[]
        {
            VectorUIKit.AccentGold,
            VectorUIKit.AccentEmerald,
            VectorUIKit.AccentCyan,
            VectorUIKit.AccentPurple,
            new Color(1f, 0.45f, 0.65f, 1f)
        };

        for (int i = 0; i < 32; i++)
        {
            Color col = confettiColors[i % confettiColors.Length];
            Vector3 startPos = mainCanvas.transform.position;
            StartCoroutine(AnimateVectorParticle(mainCanvas.transform, startPos, col, true));
        }
    }

    private IEnumerator AnimateVectorParticle(Transform parent, Vector3 startWorldPos, Color color, bool isConfetti)
    {
        GameObject pObj = new GameObject("VectorParticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pObj.transform.SetParent(parent, false);

        RectTransform rt = pObj.GetComponent<RectTransform>();
        if (isConfetti)
        {
            rt.anchorMin = new Vector2(Random.Range(0.1f, 0.9f), Random.Range(0.45f, 0.85f));
            rt.anchorMax = rt.anchorMin;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(Random.Range(14f, 24f), Random.Range(14f, 24f));
        }
        else
        {
            rt.position = startWorldPos;
            rt.sizeDelta = new Vector2(14f, 14f);
        }

        Image img = pObj.GetComponent<Image>();
        img.sprite = VectorUIKit.GetCircleSprite();
        img.color = color;
        img.raycastTarget = false;

        Vector2 velocity = isConfetti
            ? new Vector2(Random.Range(-220f, 220f), Random.Range(120f, 380f))
            : Random.insideUnitCircle.normalized * Random.Range(90f, 220f);

        float duration = isConfetti ? Random.Range(0.9f, 1.45f) : 0.42f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (rt == null) yield break;
            float dt = Time.deltaTime;
            if (isConfetti) velocity.y -= 680f * dt;
            rt.anchoredPosition += velocity * dt;
            float scale = Mathf.Lerp(1f, 0.1f, elapsed / duration);
            rt.localScale = Vector3.one * scale;
            elapsed += dt;
            yield return null;
        }

        if (pObj != null) Destroy(pObj);
    }

    private IEnumerator AnimateNodesSequentially(List<EmployeeNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node != null)
            {
                StartCoroutine(PopAnimation(node.transform));
                yield return new WaitForSeconds(0.06f);
            }
        }
    }

    private IEnumerator PopAnimation(Transform target)
    {
        if (target == null) yield break;

        Vector3 originalScale = Vector3.one;
        Vector3 punchScale = originalScale * 1.16f;
        float duration = 0.18f;
        float elapsed = 0f;

        while (elapsed < duration / 2f)
        {
            if (target == null) yield break;
            target.localScale = Vector3.Lerp(originalScale, punchScale, elapsed / (duration / 2f));
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < duration / 2f)
        {
            if (target == null) yield break;
            target.localScale = Vector3.Lerp(punchScale, originalScale, elapsed / (duration / 2f));
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target != null) target.localScale = originalScale;
    }

    private IEnumerator ShakeAndFlashRed(EmployeeNode node)
    {
        if (node == null) yield break;

        RectTransform rt = node.GetComponent<RectTransform>();
        Image img = node.GetComponent<Image>();
        if (rt == null) yield break;

        Vector2 originalPos = rt.anchoredPosition;
        Color originalColor = node.GetBaseColor();

        if (img != null) img.color = new Color(1f, 0.42f, 0.42f, 1f);

        float duration = 0.28f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (rt == null) yield break;
            float offsetX = Mathf.Sin(elapsed * 55f) * 10f;
            rt.anchoredPosition = originalPos + new Vector2(offsetX, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rt != null) rt.anchoredPosition = originalPos;
        if (img != null) img.color = originalColor;
    }
}