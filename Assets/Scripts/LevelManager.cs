using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public LevelData nivelActual;
    public List<EmployeeNode> nodosActivos = new List<EmployeeNode>();
    private EmployeeNode nodoRaiz;

    void Awake()
    {
        // Singleton básico para acceso rápido
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegistrarNodo(EmployeeNode nodo, bool esRaiz)
    {
        if (!nodosActivos.Contains(nodo))
        {
            nodosActivos.Add(nodo);
        }
        if (esRaiz) nodoRaiz = nodo;
    }

    /// <summary>
    /// Se llama cada vez que el jugador hace una conexión exitosa.
    /// </summary>
    public void EvaluarVictoria()
    {
        // Usamos el validador estático que creamos al principio
        if (OrgValidator.ValidateWinCondition(nodosActivos, nodoRaiz))
        {
            Debug.Log("¡Puzzle Resuelto! Organigrama perfecto.");
            EjecutarFeedbackVictoria();
        }
    }

    private void EjecutarFeedbackVictoria()
    {
        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.PlayVictoryFeedback(nodosActivos);
        }

        // Registramos la victoria para sumar a la racha diaria
        if (DailyStreakManager.Instance != null)
        {
            DailyStreakManager.Instance.RegisterWin();
        }
    }
}