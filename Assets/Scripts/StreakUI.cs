using UnityEngine;
using TMPro;

public class StreakUI : MonoBehaviour
{
    public static StreakUI Instance;
    public TextMeshProUGUI textoRacha;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ActualizarPantalla();
    }

    public void ActualizarPantalla()
    {
        if (DailyStreakManager.Instance != null)
        {
            int racha = DailyStreakManager.Instance.currentStreak;
            textoRacha.text = $"🔥 {racha}";
        }
    }
}