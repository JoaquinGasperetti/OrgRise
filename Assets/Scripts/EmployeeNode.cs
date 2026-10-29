using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum NivelJerarquico { CEO = 0, Director = 1, Gerente = 2, EmpleadoBase = 3 }
public enum Departamento { General, Finanzas, Comercial, Operaciones, Tecnologia }
public enum CriterioDepartamentalizacion { Funcional, PorProducto, Geografica, Matricial }
public enum ModoAutoridad { Equilibrada, Centralizada, Descentralizada }

public class EmployeeNode : MonoBehaviour
{
    [Header("Atributos del Rol")]
    public string roleName;
    public string displayTitle; // Nombre con salto de línea para la UI
    public NivelJerarquico nivel;
    public Departamento departamento;
    public int maxDirectReports;
    public bool isRootNode;

    [Header("Conexiones")]
    public EmployeeNode manager;
    public List<EmployeeNode> directReports = new List<EmployeeNode>();

    private TextMeshProUGUI cachedText;
    private Image cachedImage;
    private Color baseColor = Color.white;

    void Awake()
    {
        cachedText = GetComponentInChildren<TextMeshProUGUI>();
        cachedImage = GetComponent<Image>();
    }

    void Start()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.RegistrarNodo(this, isRootNode);

        RefreshNodeVisuals();
    }

    public void ConfigureVisuals(string visualTitle, Color areaColor)
    {
        displayTitle = visualTitle;
        baseColor = areaColor;
        if (cachedImage == null) cachedImage = GetComponent<Image>();
        if (cachedImage != null) cachedImage.color = baseColor;
        RefreshNodeVisuals();
    }

    public Color GetBaseColor()
    {
        return baseColor;
    }

    /// <summary>
    /// Intenta asignar un jefe a este nodo validando todas las reglas de Gestión de la Organización.
    /// </summary>
    public bool TryAssignManager(EmployeeNode newManager, out string motivoRechazo)
    {
        motivoRechazo = string.Empty;

        if (newManager == null || newManager == this)
        {
            motivoRechazo = "No se puede conectar un rol consigo mismo.";
            return false;
        }

        if (this.isRootNode)
        {
            motivoRechazo = "Cadena de Mando: La Dirección General (CEO) es la cima estratégica y no reporta a otro nodo.";
            return false;
        }

        if (this.manager == newManager)
        {
            motivoRechazo = "Este rol ya reporta a ese superior.";
            return false;
        }

        // 1. Validar Cadena de Mando / Jerarquía
        if (newManager.nivel >= this.nivel)
        {
            motivoRechazo = $"Cadena de Mando inválida: {roleName} ({ObtenerEtiquetaNivel(this.nivel)}) debe reportar a un nivel jerárquico superior, no a {newManager.roleName} ({ObtenerEtiquetaNivel(newManager.nivel)}).";
            return false;
        }

        // Evitar saltos de línea jerárquica cuando existen mandos medios activos en el nivel
        if (LevelManager.Instance != null && LevelManager.Instance.ExigirCadenaEstricta)
        {
            int diferenciaNivel = (int)this.nivel - (int)newManager.nivel;
            if (diferenciaNivel > 1 && LevelManager.Instance.ExisteNivelIntermedioParaArea(this.nivel, this.departamento))
            {
                motivoRechazo = $"Respeto del Conducto Regular: {roleName} debe reportar a su mando inmediato superior antes de saltar directamente a {newManager.roleName}.";
                return false;
            }
        }

        // 2. Validar Departamentalización (El CEO o la estructura Matricial aceptan coordinación transversal)
        bool esMatricial = LevelManager.Instance != null && LevelManager.Instance.criterioActual == CriterioDepartamentalizacion.Matricial;
        if (newManager.nivel != NivelJerarquico.CEO && newManager.departamento != Departamento.General && newManager.departamento != this.departamento)
        {
            // En estructura matricial los Empleados Base pueden reportar a Gerentes de Proyecto de otra unidad si el nivel lo habilita
            if (!(esMatricial && this.nivel == NivelJerarquico.EmpleadoBase && newManager.nivel == NivelJerarquico.Gerente))
            {
                string etiquetaPropia = LevelManager.Instance != null ? LevelManager.Instance.ObtenerNombreDepartamento(this.departamento) : this.departamento.ToString();
                string etiquetaJefe = LevelManager.Instance != null ? LevelManager.Instance.ObtenerNombreDepartamento(newManager.departamento) : newManager.departamento.ToString();
                motivoRechazo = $"Departamentalización: {roleName} pertenece a [{etiquetaPropia}] y no puede reportar a [{etiquetaJefe}].";
                return false;
            }
        }

        // 3. Validar Tramo de Control (Span of Control)
        if (newManager.directReports.Count >= newManager.maxDirectReports)
        {
            motivoRechazo = $"Tramo de Control excedido: {newManager.roleName} ya alcanzó su límite máximo de {newManager.maxDirectReports} reportes directos (cuello de botella).";
            return false;
        }

        // 4. Validar Ciclos Cerrados
        if (OrgValidator.CheckForCycles(newManager, this))
        {
            motivoRechazo = "Conflicto de Autoridad: Esta conexión generaría un ciclo cerrado en la jerarquía.";
            return false;
        }

        EmployeeNode previousManager = manager;
        RemoveCurrentManager();

        manager = newManager;
        newManager.directReports.Add(this);

        if (previousManager != null) previousManager.RefreshNodeVisuals();
        newManager.RefreshNodeVisuals();
        this.RefreshNodeVisuals();

        return true;
    }

    /// <summary>
    /// Sobrecarga por compatibilidad con llamadas simples.
    /// </summary>
    public bool TryAssignManager(EmployeeNode newManager)
    {
        return TryAssignManager(newManager, out _);
    }

    public void RemoveCurrentManager()
    {
        if (manager != null)
        {
            EmployeeNode oldManager = manager;
            oldManager.directReports.Remove(this);
            manager = null;
            oldManager.RefreshNodeVisuals();
            this.RefreshNodeVisuals();
        }
    }

    public void RefreshNodeVisuals()
    {
        if (cachedText == null) cachedText = GetComponentInChildren<TextMeshProUGUI>();
        if (cachedText == null) return;

        string titulo = string.IsNullOrEmpty(displayTitle) ? roleName : displayTitle;

        string infoTramo = "";
        if (maxDirectReports > 0)
        {
            int actuales = directReports != null ? directReports.Count : 0;
            string colorCupo = actuales >= maxDirectReports ? "#B71C1C" : "#1B5E20";
            infoTramo = $"\n<size=58%><color={colorCupo}>Tramo: {actuales}/{maxDirectReports}</color></size>";
        }
        else
        {
            string estadoConexion = (manager != null || isRootNode) ? "<color=#1B5E20>●</color>" : "<color=#555555>○</color>";
            infoTramo = $"\n<size=52%>{estadoConexion} Operativo</size>";
        }

        cachedText.text = $"<color=#111111><b>{titulo}</b></color>{infoTramo}";
    }

    public static string ObtenerEtiquetaNivel(NivelJerarquico n)
    {
        switch (n)
        {
            case NivelJerarquico.CEO: return "Nivel Estratégico";
            case NivelJerarquico.Director: return "Nivel Ejecutivo";
            case NivelJerarquico.Gerente: return "Nivel Táctico";
            case NivelJerarquico.EmpleadoBase: return "Nivel Operativo";
            default: return n.ToString();
        }
    }
}