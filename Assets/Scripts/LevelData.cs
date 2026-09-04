using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Nivel_01", menuName = "OrgRise/Nivel")]
public class LevelData : ScriptableObject
{
    [Header("Configuración del Nivel")]
    public string idNivel;

    [Header("Nodos del Organigrama")]
    public List<NodeSetup> empleados;
}

[System.Serializable]
public class NodeSetup
{
    public string nombreRol;
    public int maxReportesDirectos;
    public bool esDirectorGeneral; // Sirve para identificar la raíz del árbol
}