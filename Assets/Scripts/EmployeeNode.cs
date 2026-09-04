using System.Collections.Generic;
using UnityEngine;

public class EmployeeNode : MonoBehaviour
{
    [Header("Atributos del Rol")]
    public string roleName;
    public int maxDirectReports; // Límite del tramo de control

    [Header("Conexiones")]
    public EmployeeNode manager;
    public List<EmployeeNode> directReports = new List<EmployeeNode>();

    public bool isRootNode;

    void Start()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RegistrarNodo(this, isRootNode);
        }
    }

    /// <summary>
    /// Intenta establecer una conexión de subordinado a jefe.
    /// </summary>
    public bool TryAssignManager(EmployeeNode newManager)
    {
        if (newManager == null || newManager == this) return false;

        // 1. Validar el tramo de control del nuevo jefe
        if (newManager.directReports.Count >= newManager.maxDirectReports)
        {
            Debug.LogWarning($"El nodo '{newManager.roleName}' excedió su límite de reportes.");
            return false;
        }

        // 2. Validar que no se generen ciclos cerrados
        if (OrgValidator.CheckForCycles(newManager, this))
        {
            Debug.LogWarning("Conexión inválida: genera un ciclo jerárquico.");
            return false;
        }

        // 3. Desvincular del manager anterior si ya tenía uno
        RemoveCurrentManager();

        // 4. Establecer la nueva conexión
        manager = newManager;
        newManager.directReports.Add(this);

        return true;
    }

    public void RemoveCurrentManager()
    {
        if (manager != null)
        {
            manager.directReports.Remove(this);
            manager = null;
        }
    }
}