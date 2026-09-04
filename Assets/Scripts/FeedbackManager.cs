using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance;

    [Header("Audio")]
    public AudioClip snapSound; // Sonido individual al conectar (opcional)
    public AudioClip victorySound; // Sonido final al completar
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioSource = GetComponent<AudioSource>();
    }

    public void PlayVictoryFeedback(List<EmployeeNode> nodes)
    {
        if (victorySound != null) audioSource.PlayOneShot(victorySound);
        StartCoroutine(AnimateNodesSequentially(nodes));
    }

    private IEnumerator AnimateNodesSequentially(List<EmployeeNode> nodes)
    {
        // Anima los nodos uno por uno con un pequeño retraso para un efecto "onda"
        foreach (var node in nodes)
        {
            StartCoroutine(PopAnimation(node.transform));
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator PopAnimation(Transform target)
    {
        Vector3 originalScale = target.localScale;
        Vector3 punchScale = originalScale * 1.2f; // Crece un 20%
        float duration = 0.2f;
        float elapsed = 0f;

        // Fase 1: Crecer
        while (elapsed < duration / 2f)
        {
            target.localScale = Vector3.Lerp(originalScale, punchScale, elapsed / (duration / 2f));
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;

        // Fase 2: Volver a la normalidad
        while (elapsed < duration / 2f)
        {
            target.localScale = Vector3.Lerp(punchScale, originalScale, elapsed / (duration / 2f));
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localScale = originalScale;
    }
}