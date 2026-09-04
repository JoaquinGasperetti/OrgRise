using System.Collections.Generic;
using UnityEngine;

// Definimos los niveles posibles. Cuanto menor el número, más alto el cargo.
public enum NivelJerarquico
{
    CEO = 0,
    Director = 1,
    Gerente = 2,
    EmpleadoBase = 3
}

public class EmployeeNode : MonoBehaviour
{
    [Header("Atributos del Rol")]
    public string roleName;
    public NivelJerarquico nivel; // ¡Nueva variable!
    public int maxDirectReports;
    public bool isRootNode;

    [Header("Conexiones")]
    public EmployeeNode manager;
    public List<EmployeeNode> directReports = new List<EmployeeNode>();

    void Start()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RegistrarNodo(this, isRootNode);
        }
    }

    public bool TryAssignManager(EmployeeNode newManager)
    {
        if (newManager == null || newManager == this) return false;

        // 1. Validar la Cadena de Mando (Jerarquía)
        // Un nodo solo puede reportar a alguien con un cargo superior (número menor)
        if (newManager.nivel >= this.nivel)
        {
            Debug.LogWarning($"Conexión rechazada: {roleName} ({this.nivel}) no puede reportar a {newManager.roleName} ({newManager.nivel}).");
            return false;
        }

        // OPCIONAL: Si querés ser súper estricto y que no se puedan saltear pasos
        // (ej. un EmpleadoBase no puede ir al CEO, tiene que pasar por Gerente sí o sí):
        /*
        if (this.nivel - newManager.nivel > 1)
        {
            Debug.LogWarning("Conexión rechazada: Se está saltando un eslabón de la cadena de mando.");
            return false;
        }
        */

        // 2. Validar el tramo de control del nuevo jefe
        if (newManager.directReports.Count >= newManager.maxDirectReports)
        {
            Debug.LogWarning($"El nodo '{newManager.roleName}' excedió su límite de reportes.");
            return false;
        }

        // 3. Validar ciclos cerrados
        if (OrgValidator.CheckForCycles(newManager, this))
        {
            Debug.LogWarning("Conexión inválida: genera un ciclo jerárquico.");
            return false;
        }

        // 4. Desvincular del manager anterior
        RemoveCurrentManager();

        // 5. Establecer nueva conexión
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