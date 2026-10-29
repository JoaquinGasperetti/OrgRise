using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Nivel_01", menuName = "OrgRise/Nivel")]
public class LevelData : ScriptableObject
{
    [Header("Configuración del Nivel")]
    public string idNivel;
    public string tituloAcademico;
    [TextArea(2, 4)]
    public string descripcionTeorica;
    public CriterioDepartamentalizacion criterioDepartamentalizacion = CriterioDepartamentalizacion.Funcional;
    public ModoAutoridad modoAutoridad = ModoAutoridad.Equilibrada;

    [Header("Nodos del Organigrama")]
    public List<NodeSetup> empleados = new List<NodeSetup>();
}

[System.Serializable]
public class NodeSetup
{
    public string nombreRol;
    public NivelJerarquico nivel = NivelJerarquico.EmpleadoBase;
    public Departamento departamento = Departamento.Finanzas;
    public int maxReportesDirectos = 2;
    public bool esDirectorGeneral; // Sirve para identificar la raíz del árbol
}