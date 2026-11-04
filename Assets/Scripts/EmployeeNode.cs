using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum NivelJerarquico { CEO = 0, Director = 1, Gerente = 2, EmpleadoBase = 3 }
public enum Departamento { General, Finanzas, Comercial, Operaciones, Tecnologia, StaffAsesoria }
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
    public bool isStaffNode = false; // Órgano de Asesoría / Staff de Apoyo (Mintzberg)

    [Header("Conexiones")]
    public EmployeeNode manager; // Superior jerárquico principal (Línea)
    public EmployeeNode matrixProjectManager; // Superior secundario en Estructura Matricial (Doble Mando)
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
        baseColor = isStaffNode ? new Color(0.95f, 0.82f, 0.28f) : areaColor;
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
    /// Soporta validación de Cadena de Mando, Tramo de Control, Departamentalización,
    /// Órganos de Staff (Mintzberg) y Estructura Matricial con Doble Mando.
    /// </summary>
    public bool TryAssignManager(EmployeeNode newManager, out string motivoRechazo)
    {
        return TryAssignManager(newManager, out motivoRechazo, out _);
    }

    public bool TryAssignManager(EmployeeNode newManager, out string motivoRechazo, out bool esConexionMatricial)
    {
        motivoRechazo = string.Empty;
        esConexionMatricial = false;

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

        if (this.manager == newManager || this.matrixProjectManager == newManager)
        {
            motivoRechazo = "Este rol ya reporta a ese superior.";
            return false;
        }

        // 1. Validación especial para Órganos de Staff (Asesoría / Consultoría)
        if (this.isStaffNode)
        {
            if (newManager.nivel != NivelJerarquico.CEO && newManager.nivel != NivelJerarquico.Director)
            {
                motivoRechazo = "Órgano de Staff: Los puestos de asesoría y tecnoestructura asesoran a la Cúpula Estratégica (CEO o Director) y no reportan a mandos tácticos u operativos.";
                return false;
            }

            if (OrgValidator.CheckForCycles(newManager, this))
            {
                motivoRechazo = "Conflicto de Autoridad: Esta conexión generaría un ciclo cerrado.";
                return false;
            }

            EmployeeNode prevStaffMgr = manager;
            RemoveCurrentManager();
            manager = newManager;
            newManager.directReports.Add(this);

            if (prevStaffMgr != null) prevStaffMgr.RefreshNodeVisuals();
            newManager.RefreshNodeVisuals();
            this.RefreshNodeVisuals();
            return true;
        }

        // 2. Validar Cadena de Mando / Jerarquía
        if (newManager.nivel >= this.nivel)
        {
            motivoRechazo = $"Cadena de Mando inválida: {roleName} ({ObtenerEtiquetaNivel(this.nivel)}) debe reportar a un nivel superior, no a {newManager.roleName} ({ObtenerEtiquetaNivel(newManager.nivel)}).";
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

        // 3. Estructura Matricial (Doble Mando): un Empleado Base puede tener un segundo jefe de proyecto
        bool esMatricial = LevelManager.Instance != null && LevelManager.Instance.criterioActual == CriterioDepartamentalizacion.Matricial;
        bool asignarComoMatricial = esMatricial &&
                                    this.nivel == NivelJerarquico.EmpleadoBase &&
                                    this.manager != null &&
                                    this.matrixProjectManager == null &&
                                    newManager.nivel == NivelJerarquico.Gerente;

        if (asignarComoMatricial)
        {
            if (newManager.directReports.Count >= newManager.maxDirectReports)
            {
                motivoRechazo = $"Tramo de Control excedido: {newManager.roleName} ya alcanzó su límite de {newManager.maxDirectReports} reportes directos.";
                return false;
            }

            if (OrgValidator.CheckForCycles(newManager, this))
            {
                motivoRechazo = "Conflicto de Autoridad: Generaría un ciclo cerrado en la estructura matricial.";
                return false;
            }

            matrixProjectManager = newManager;
            newManager.directReports.Add(this);
            newManager.RefreshNodeVisuals();
            this.RefreshNodeVisuals();
            esConexionMatricial = true;
            return true;
        }

        // 4. Validar Departamentalización estándar
        if (newManager.nivel != NivelJerarquico.CEO && newManager.departamento != Departamento.General && newManager.departamento != this.departamento)
        {
            if (!(esMatricial && this.nivel == NivelJerarquico.EmpleadoBase && newManager.nivel == NivelJerarquico.Gerente))
            {
                string etiquetaPropia = LevelManager.Instance != null ? LevelManager.Instance.ObtenerNombreDepartamento(this.departamento) : this.departamento.ToString();
                string etiquetaJefe = LevelManager.Instance != null ? LevelManager.Instance.ObtenerNombreDepartamento(newManager.departamento) : newManager.departamento.ToString();
                motivoRechazo = $"Departamentalización: {roleName} pertenece a [{etiquetaPropia}] y no puede reportar a [{etiquetaJefe}].";
                return false;
            }
        }

        // 5. Validar Tramo de Control (Span of Control)
        if (newManager.directReports.Count >= newManager.maxDirectReports)
        {
            motivoRechazo = $"Tramo de Control excedido: {newManager.roleName} ya alcanzó su límite máximo de {newManager.maxDirectReports} reportes directos (cuello de botella).";
            return false;
        }

        // 6. Validar Ciclos Cerrados
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

    public bool TryAssignManager(EmployeeNode newManager)
    {
        return TryAssignManager(newManager, out _, out _);
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

    public void RemoveMatrixProjectManager()
    {
        if (matrixProjectManager != null)
        {
            EmployeeNode oldMatrix = matrixProjectManager;
            oldMatrix.directReports.Remove(this);
            matrixProjectManager = null;
            oldMatrix.RefreshNodeVisuals();
            this.RefreshNodeVisuals();
        }
    }

    public void RefreshNodeVisuals()
    {
        if (cachedText == null) cachedText = GetComponentInChildren<TextMeshProUGUI>();
        if (cachedText == null) return;

        string titulo = string.IsNullOrEmpty(displayTitle) ? roleName : displayTitle;

        string infoTramo = "";
        if (isStaffNode)
        {
            string estadoStaff = manager != null ? "<color=#B78103>● Asesoría Activa</color>" : "<color=#666666>○ Staff Vacante</color>";
            infoTramo = $"\n<size=52%>{estadoStaff}</size>";
        }
        else if (maxDirectReports > 0)
        {
            int actuales = directReports != null ? directReports.Count : 0;
            string colorCupo = actuales >= maxDirectReports ? "#B71C1C" : "#1B5E20";
            infoTramo = $"\n<size=58%><color={colorCupo}>Tramo: {actuales}/{maxDirectReports}</color></size>";
        }
        else
        {
            bool conectado = (manager != null || matrixProjectManager != null || isRootNode);
            string estadoConexion = conectado ? "<color=#1B5E20>●</color>" : "<color=#555555>○</color>";
            string extraMatricial = matrixProjectManager != null ? " <color=#0277BD>[2 Jefes]</color>" : "";
            infoTramo = $"\n<size=52%>{estadoConexion} Operativo{extraMatricial}</size>";
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