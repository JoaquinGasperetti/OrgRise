using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance;

    [Header("Audio")]
    public AudioClip snapSound;    // Sonido individual al conectar
    public AudioClip victorySound; // Sonido final al completar
    private AudioSource audioSource;

    // Banner dinámico de estado académico
    private GameObject toastObject;
    private TextMeshProUGUI toastText;
    private Image toastBg;
    private Coroutine toastCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySnapFeedback(Transform nodeTransform, string mensaje = "")
    {
        if (snapSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.08f);
            audioSource.PlayOneShot(snapSound);
        }

        if (nodeTransform != null)
        {
            StartCoroutine(PopAnimation(nodeTransform));
        }

        if (!string.IsNullOrEmpty(mensaje))
        {
            MostrarMensajeEstado(mensaje, new Color(0.12f, 0.45f, 0.22f, 0.92f), 2.2f);
        }
    }

    public void PlayErrorFeedback(EmployeeNode targetNode, string motivo)
    {
        if (snapSound != null && audioSource != null)
        {
            audioSource.pitch = 0.65f;
            audioSource.PlayOneShot(snapSound, 0.7f);
        }

        if (targetNode != null)
        {
            StartCoroutine(ShakeAndFlashRed(targetNode));
        }

        if (!string.IsNullOrEmpty(motivo))
        {
            MostrarMensajeEstado($"⚠️ {motivo}", new Color(0.68f, 0.14f, 0.14f, 0.95f), 3.8f);
        }
    }

    public void PlayVictoryFeedback(List<EmployeeNode> nodes)
    {
        if (victorySound != null && audioSource != null)
        {
            audioSource.pitch = 1.0f;
            audioSource.PlayOneShot(victorySound);
        }
        StartCoroutine(AnimateNodesSequentially(nodes));
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

        toastObject = new GameObject("AcademicToastBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        toastObject.transform.SetParent(mainCanvas.transform, false);

        RectTransform rt = toastObject.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0f);
        rt.anchorMax = new Vector2(0.95f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 35f);
        rt.sizeDelta = new Vector2(0f, 115f);

        toastBg = toastObject.GetComponent<Image>();
        toastBg.color = new Color(0.15f, 0.2f, 0.28f, 0.92f);
        toastBg.raycastTarget = false;

        GameObject txtObj = new GameObject("ToastText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(toastObject.transform, false);

        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(24f, 10f);
        txtRt.offsetMax = new Vector2(-24f, -10f);

        toastText = txtObj.GetComponent<TextMeshProUGUI>();
        toastText.fontSize = 26f;
        toastText.alignment = TextAlignmentOptions.Center;
        toastText.color = Color.white;
        toastText.raycastTarget = false;

        toastObject.SetActive(false);
    }

    private IEnumerator MostrarToastRoutine(string texto, Color colorFondo, float duracion)
    {
        toastObject.SetActive(true);
        toastBg.color = colorFondo;
        toastText.text = texto;
        yield return new WaitForSeconds(duracion);
        toastObject.SetActive(false);
    }

    private IEnumerator AnimateNodesSequentially(List<EmployeeNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node != null)
            {
                StartCoroutine(PopAnimation(node.transform));
                yield return new WaitForSeconds(0.08f);
            }
        }
    }

    private IEnumerator PopAnimation(Transform target)
    {
        if (target == null) yield break;

        Vector3 originalScale = Vector3.one;
        Vector3 punchScale = originalScale * 1.18f;
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

        if (img != null) img.color = new Color(1f, 0.45f, 0.45f, 1f);

        float duration = 0.28f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (rt == null) yield break;
            float offsetX = Mathf.Sin(elapsed * 55f) * 12f;
            rt.anchoredPosition = originalPos + new Vector2(offsetX, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rt != null) rt.anchoredPosition = originalPos;
        if (img != null) img.color = originalColor;
    }
}