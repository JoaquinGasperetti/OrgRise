using UnityEngine;
using System;
using System.Globalization;

/// <summary>
/// Gestiona la racha diaria de retención, premios automáticos por hitos (días 3, 7, 14, 30)
/// y recuperación de racha perdida mediante anuncio bonificado ("Salva-Racha").
/// </summary>
public class DailyStreakManager : MonoBehaviour
{
    public static DailyStreakManager Instance;

    private string lastPlayDateKey = "LastPlayDate";
    private string currentStreakKey = "CurrentStreak";
    private string bestStreakKey = "OrgRise_BestStreak";
    private const string DateFormat = "yyyy-MM-dd";

    [HideInInspector] public int currentStreak;
    [HideInInspector] public int bestStreak;
    [HideInInspector] public int lostStreakToRecover = 0;
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
        bestStreak = PlayerPrefs.GetInt(bestStreakKey, 0);
        string lastDateStr = PlayerPrefs.GetString(lastPlayDateKey, "");

        if (!string.IsNullOrEmpty(lastDateStr))
        {
            if (!DateTime.TryParseExact(lastDateStr, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime lastDate))
            {
                DateTime.TryParse(lastDateStr, out lastDate);
            }

            if (lastDate != default)
            {
                DateTime today = DateTime.Today;
                TimeSpan difference = today - lastDate.Date;

                if (difference.Days == 0)
                {
                    alreadyPlayedToday = true;
                    lostStreakToRecover = 0;
                }
                else if (difference.Days == 1)
                {
                    alreadyPlayedToday = false;
                    lostStreakToRecover = 0;
                }
                else if (difference.Days > 1)
                {
                    // Guardamos la racha perdida para ofrecerle al jugador rescatarla viendo un video bonificado
                    if (currentStreak >= 2)
                    {
                        lostStreakToRecover = currentStreak;
                    }
                    currentStreak = 0;
                    alreadyPlayedToday = false;
                    PlayerPrefs.SetInt(currentStreakKey, currentStreak);
                    PlayerPrefs.Save();
                }
            }
            else
            {
                alreadyPlayedToday = false;
            }
        }
        else
        {
            alreadyPlayedToday = false;
        }
    }

    /// <summary>
    /// Permite recuperar la racha perdida tras ver un anuncio bonificado.
    /// </summary>
    public void RecuperarRachaConVideo()
    {
        if (lostStreakToRecover <= 0) return;

        currentStreak = lostStreakToRecover;
        lostStreakToRecover = 0;
        PlayerPrefs.SetInt(currentStreakKey, currentStreak);
        PlayerPrefs.SetString(lastPlayDateKey, DateTime.Today.AddDays(-1).ToString(DateFormat, CultureInfo.InvariantCulture));
        PlayerPrefs.Save();

        if (StreakUI.Instance != null) StreakUI.Instance.ActualizarPantalla();
        if (LevelManager.Instance != null) LevelManager.Instance.ActualizarBadgeMenu();
    }

    public void RegisterWin()
    {
        if (!alreadyPlayedToday)
        {
            currentStreak++;
            lostStreakToRecover = 0;
            if (currentStreak > bestStreak)
            {
                bestStreak = currentStreak;
                PlayerPrefs.SetInt(bestStreakKey, bestStreak);
            }

            PlayerPrefs.SetInt(currentStreakKey, currentStreak);
            PlayerPrefs.SetString(lastPlayDateKey, DateTime.Today.ToString(DateFormat, CultureInfo.InvariantCulture));
            PlayerPrefs.Save();

            alreadyPlayedToday = true;

            // Premios automáticos de retención en hitos de racha (Días 3, 7, 14, 30)
            if (currentStreak == 3 || currentStreak == 7 || currentStreak == 14 || currentStreak % 15 == 0)
            {
                int premio = currentStreak >= 7 ? 3 : 2;
                if (WildcardManager.Instance != null)
                {
                    WildcardManager.Instance.RecompensarComodin(premio);
                }
                if (FeedbackManager.Instance != null)
                {
                    FeedbackManager.Instance.MostrarMensajeEstado(
                        $"🔥 ¡Hito de Racha ({currentStreak} días)! Ganaste +{premio} ⚡ Comodines de regalo.",
                        new Color(0.85f, 0.42f, 0.08f, 0.96f),
                        3.5f
                    );
                }
            }
        }
    }
}