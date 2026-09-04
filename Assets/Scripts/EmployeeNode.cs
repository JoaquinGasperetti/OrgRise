using System.Collections.Generic;
using UnityEngine;

public enum NivelJerarquico { CEO = 0, Director = 1, Gerente = 2, EmpleadoBase = 3 }
public enum Departamento { General, Finanzas, Comercial, Operaciones, Tecnologia }

public class EmployeeNode : MonoBehaviour
{
    [Header("Atributos del Rol")]
    public string roleName;
    public NivelJerarquico nivel;
    public Departamento departamento; // Nueva variable de área
    public int maxDirectReports;
    public bool isRootNode;

    [Header("Conexiones")]
    public EmployeeNode manager;
    public List<EmployeeNode> directReports = new List<EmployeeNode>();

    void Start()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.RegistrarNodo(this, isRootNode);
    }

    public bool TryAssignManager(EmployeeNode newManager)
    {
        if (newManager == null || newManager == this) return false;

        // 1. Validar Jerarquía
        if (newManager.nivel >= this.nivel) return false;

        // 2. Validar Departamentalización (El CEO acepta de cualquier área)
        if (newManager.nivel != NivelJerarquico.CEO && newManager.departamento != this.departamento)
        {
            Debug.LogWarning($"Rechazado: {roleName} es de {this.departamento}, no puede ir a {newManager.departamento}.");
            return false;
        }

        // 3. Validar Tramo de control y Ciclos
        if (newManager.directReports.Count >= newManager.maxDirectReports) return false;
        if (OrgValidator.CheckForCycles(newManager, this)) return false;

        RemoveCurrentManager();
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