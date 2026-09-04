using UnityEngine;
using System;

public class DailyStreakManager : MonoBehaviour
{
    public static DailyStreakManager Instance;

    private string lastPlayDateKey = "LastPlayDate";
    private string currentStreakKey = "CurrentStreak";

    [HideInInspector] public int currentStreak;
    [HideInInspector] public bool alreadyPlayedToday;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadStreakData();
    }

    private void LoadStreakData()
    {
        currentStreak = PlayerPrefs.GetInt(currentStreakKey, 0);
        string lastDateStr = PlayerPrefs.GetString(lastPlayDateKey, "");

        if (!string.IsNullOrEmpty(lastDateStr))
        {
            DateTime lastDate = DateTime.Parse(lastDateStr);
            DateTime today = DateTime.Today;

            TimeSpan difference = today - lastDate.Date;

            if (difference.Days == 0)
            {
                alreadyPlayedToday = true;
            }
            else if (difference.Days == 1)
            {
                alreadyPlayedToday = false; // Está en racha, pero aún no jugó hoy
            }
            else if (difference.Days > 1)
            {
                currentStreak = 0; // Perdió la racha
                alreadyPlayedToday = false;
                PlayerPrefs.SetInt(currentStreakKey, currentStreak);
            }
        }
        else
        {
            alreadyPlayedToday = false; // Primer inicio del juego
        }
    }

    /// <summary>
    /// Se llama cuando el jugador resuelve el puzzle diario correctamente.
    /// </summary>
    public void RegisterWin()
    {
        if (!alreadyPlayedToday)
        {
            currentStreak++;
            PlayerPrefs.SetInt(currentStreakKey, currentStreak);
            PlayerPrefs.SetString(lastPlayDateKey, DateTime.Today.ToString());
            PlayerPrefs.Save();

            alreadyPlayedToday = true;
            Debug.Log($"¡Victoria! Racha actual: {currentStreak} días.");
        }
        else
        {
            Debug.Log("El puzzle de hoy ya estaba resuelto. Racha mantenida.");
        }
    }
}