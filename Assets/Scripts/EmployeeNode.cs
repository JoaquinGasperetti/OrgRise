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
    public string displayTitle;
    public NivelJerarquico nivel;
    public Departamento departamento;
    public int maxDirectReports;
    public bool isRootNode;
    public bool isStaffNode = false;

    [Header("Conexiones")]
    public EmployeeNode manager;
    public EmployeeNode matrixProjectManager;
    public List<EmployeeNode> directReports = new List<EmployeeNode>();

    private TextMeshProUGUI cachedText;
    private Image cachedImage;
    private Image headerRibbonImage;
    private Image topPortImage;
    private Image bottomPortImage;
    private RectTransform topPortRect;
    private RectTransform bottomPortRect;
    private Image highlightRingImage;
    private Color baseColor = Color.white;

    void Awake()
    {
        cachedText = GetComponentInChildren<TextMeshProUGUI>();
        cachedImage = GetComponent<Image>();
        EnsureVectorCardHierarchy();
    }

    void Start()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.RegistrarNodo(this, isRootNode);

        RefreshNodeVisuals();
    }

    private void EnsureVectorCardHierarchy()
    {
        if (cachedImage == null) cachedImage = GetComponent<Image>();

        // 1. Estilo de tarjeta redondeada vectorial con sombra suave
        VectorUIKit.ApplyRoundedStyle(cachedImage, baseColor, pill: false, addShadow: true, shadowOffset: new Vector2(0f, -6f));
        VectorUIKit.ApplyOutline(cachedImage, new Color(1f, 1f, 1f, 0.35f), 1.5f);

        // 2. Franja superior vectorial de jerarquía (Header Ribbon)
        if (headerRibbonImage == null)
        {
            Transform existingRibbon = transform.Find("VectorHeaderRibbon");
            if (existingRibbon != null)
            {
                headerRibbonImage = existingRibbon.GetComponent<Image>();
            }
            else
            {
                GameObject ribObj = new GameObject("VectorHeaderRibbon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                ribObj.transform.SetParent(transform, false);
                ribObj.transform.SetAsFirstSibling();

                RectTransform ribRt = ribObj.GetComponent<RectTransform>();
                ribRt.localScale = Vector3.one;
                ribRt.anchorMin = new Vector2(0.03f, 0.78f);
                ribRt.anchorMax = new Vector2(0.97f, 0.96f);
                ribRt.offsetMin = Vector2.zero;
                ribRt.offsetMax = Vector2.zero;

                headerRibbonImage = ribObj.GetComponent<Image>();
                headerRibbonImage.sprite = VectorUIKit.GetRoundedSprite(pill: true);
                headerRibbonImage.type = Image.Type.Sliced;
                headerRibbonImage.raycastTarget = false;
            }
        }

        // 3. Pin conector superior (Entrada de reporte hacia jefe)
        if (topPortImage == null)
        {
            Transform existingTop = transform.Find("TopConnectorPort");
            if (existingTop != null)
            {
                topPortImage = existingTop.GetComponent<Image>();
                topPortRect = existingTop.GetComponent<RectTransform>();
            }
            else
            {
                GameObject topObj = new GameObject("TopConnectorPort", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                topObj.transform.SetParent(transform, false);
                topPortRect = topObj.GetComponent<RectTransform>();
                topPortRect.localScale = Vector3.one;
                topPortRect.anchorMin = new Vector2(0.5f, 1f);
                topPortRect.anchorMax = new Vector2(0.5f, 1f);
                topPortRect.pivot = new Vector2(0.5f, 0.5f);
                topPortRect.anchoredPosition = new Vector2(0f, 3f);
                topPortRect.sizeDelta = new Vector2(18f, 18f);

                topPortImage = topObj.GetComponent<Image>();
                topPortImage.sprite = VectorUIKit.GetCircleSprite();
                topPortImage.raycastTarget = false;
                VectorUIKit.ApplyOutline(topPortImage, new Color(0.08f, 0.12f, 0.20f, 0.9f), 1.5f);
            }
        }

        // 4. Pin conector inferior (Salida de supervisión hacia subordinados)
        if (bottomPortImage == null)
        {
            Transform existingBot = transform.Find("BottomConnectorPort");
            if (existingBot != null)
            {
                bottomPortImage = existingBot.GetComponent<Image>();
                bottomPortRect = existingBot.GetComponent<RectTransform>();
            }
            else
            {
                GameObject botObj = new GameObject("BottomConnectorPort", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                botObj.transform.SetParent(transform, false);
                bottomPortRect = botObj.GetComponent<RectTransform>();
                bottomPortRect.localScale = Vector3.one;
                bottomPortRect.anchorMin = new Vector2(0.5f, 0f);
                bottomPortRect.anchorMax = new Vector2(0.5f, 0f);
                bottomPortRect.pivot = new Vector2(0.5f, 0.5f);
                bottomPortRect.anchoredPosition = new Vector2(0f, -3f);
                bottomPortRect.sizeDelta = new Vector2(18f, 18f);

                bottomPortImage = botObj.GetComponent<Image>();
                bottomPortImage.sprite = VectorUIKit.GetCircleSprite();
                bottomPortImage.raycastTarget = false;
                VectorUIKit.ApplyOutline(bottomPortImage, new Color(0.08f, 0.12f, 0.20f, 0.9f), 1.5f);
            }
        }

        // 5. Aro de previsualización de arrastre (Highlight Ring)
        if (highlightRingImage == null)
        {
            Transform existingRing = transform.Find("PreviewHighlightBorder");
            if (existingRing != null)
            {
                highlightRingImage = existingRing.GetComponent<Image>();
            }
            else
            {
                GameObject ringObj = new GameObject("PreviewHighlightBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                ringObj.transform.SetParent(transform, false);
                ringObj.transform.SetAsFirstSibling();

                RectTransform ringRt = ringObj.GetComponent<RectTransform>();
                ringRt.localScale = Vector3.one;
                ringRt.anchorMin = Vector2.zero;
                ringRt.anchorMax = Vector2.one;
                ringRt.offsetMin = new Vector2(-5f, -5f);
                ringRt.offsetMax = new Vector2(5f, 5f);

                highlightRingImage = ringObj.GetComponent<Image>();
                highlightRingImage.sprite = VectorUIKit.GetRoundedSprite(pill: false);
                highlightRingImage.type = Image.Type.Sliced;
                highlightRingImage.raycastTarget = false;
                highlightRingImage.enabled = false;
            }
        }

        if (cachedText != null)
        {
            cachedText.transform.SetAsLastSibling();
            RectTransform txtRt = cachedText.GetComponent<RectTransform>();
            if (txtRt != null)
            {
                txtRt.anchorMin = new Vector2(0.05f, 0.06f);
                txtRt.anchorMax = new Vector2(0.95f, 0.82f);
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
            }
        }
    }

    public Vector3 GetTopPortWorldPosition()
    {
        EnsureVectorCardHierarchy();
        return topPortRect != null ? topPortRect.position : transform.position;
    }

    public Vector3 GetBottomPortWorldPosition()
    {
        EnsureVectorCardHierarchy();
        return bottomPortRect != null ? bottomPortRect.position : transform.position;
    }

    public void SetDragHighlight(bool active, bool validConnection, bool isMatrix = false)
    {
        EnsureVectorCardHierarchy();
        if (highlightRingImage == null) return;

        if (!active)
        {
            highlightRingImage.enabled = false;
            transform.localScale = Vector3.one;
            return;
        }

        highlightRingImage.enabled = true;
        if (validConnection)
        {
            highlightRingImage.color = isMatrix
                ? new Color(0.02f, 0.85f, 0.98f, 0.92f)
                : new Color(0.08f, 0.88f, 0.52f, 0.92f);
            transform.localScale = Vector3.one * 1.06f;
        }
        else
        {
            highlightRingImage.color = new Color(0.96f, 0.25f, 0.25f, 0.88f);
            transform.localScale = Vector3.one * 0.97f;
        }
    }

    public void ConfigureVisuals(string visualTitle, Color areaColor)
    {
        EnsureVectorCardHierarchy();
        displayTitle = visualTitle;
        baseColor = isStaffNode ? new Color(0.99f, 0.88f, 0.42f) : areaColor;

        if (cachedImage != null)
        {
            cachedImage.color = baseColor;
        }

        if (headerRibbonImage != null)
        {
            Color ribbonColor = Color.Lerp(baseColor, new Color(0.05f, 0.08f, 0.15f, 1f), isRootNode ? 0.65f : 0.42f);
            ribbonColor.a = 0.92f;
            headerRibbonImage.color = ribbonColor;
        }

        RefreshNodeVisuals();
    }

    public Color GetBaseColor()
    {
        return baseColor;
    }

    /// <summary>
    /// Evalúa sin modificar el estado si este nodo puede conectarse con otro (para la previsualización en vivo y el sistema de Pistas).
    /// </summary>
    public bool CanAssignManagerPreview(EmployeeNode newManager, out string motivoRechazo, out bool esConexionMatricial)
    {
        motivoRechazo = string.Empty;
        esConexionMatricial = false;

        if (newManager == null || newManager == this)
        {
            motivoRechazo = "No podés conectar un puesto consigo mismo.";
            return false;
        }

        if (this.isRootNode)
        {
            motivoRechazo = "El CEO es la cima del holding y no reporta a nadie.";
            return false;
        }

        if (this.manager == newManager || this.matrixProjectManager == newManager)
        {
            motivoRechazo = "Este puesto ya reporta a ese líder.";
            return false;
        }

        if (this.isStaffNode)
        {
            if (newManager.nivel != NivelJerarquico.CEO && newManager.nivel != NivelJerarquico.Director)
            {
                motivoRechazo = "Consultoría VIP / Staff: solo asesora a la Alta Dirección (CEO o Director).";
                return false;
            }
            if (OrgValidator.CheckForCycles(newManager, this))
            {
                motivoRechazo = "Conflicto jerárquico: generaría un ciclo cerrado.";
                return false;
            }
            return true;
        }

        if (newManager.nivel >= this.nivel)
        {
            motivoRechazo = $"Jerarquía inválida: {roleName} debe reportar a un cargo superior.";
            return false;
        }

        if (LevelManager.Instance != null && LevelManager.Instance.ExigirCadenaEstricta)
        {
            int diferenciaNivel = (int)this.nivel - (int)newManager.nivel;
            if (diferenciaNivel > 1 && LevelManager.Instance.ExisteNivelIntermedioParaArea(this.nivel, this.departamento))
            {
                motivoRechazo = $"Salto de mando: {roleName} debe reportar primero a su jefe directo de área.";
                return false;
            }
        }

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
                motivoRechazo = $"Capacidad llena: {newManager.roleName} ya supervisa {newManager.maxDirectReports}/{newManager.maxDirectReports} puestos.";
                return false;
            }
            if (OrgValidator.CheckForCycles(newManager, this))
            {
                motivoRechazo = "Conflicto: generaría un ciclo cerrado.";
                return false;
            }
            esConexionMatricial = true;
            return true;
        }

        if (newManager.nivel != NivelJerarquico.CEO && newManager.departamento != Departamento.General && newManager.departamento != this.departamento)
        {
            if (!(esMatricial && this.nivel == NivelJerarquico.EmpleadoBase && newManager.nivel == NivelJerarquico.Gerente))
            {
                string etiquetaPropia = LevelManager.Instance != null ? LevelManager.Instance.ObtenerNombreDepartamento(this.departamento) : this.departamento.ToString();
                string etiquetaJefe = LevelManager.Instance != null ? LevelManager.Instance.ObtenerNombreDepartamento(newManager.departamento) : newManager.departamento.ToString();
                motivoRechazo = $"División incorrecta: [{etiquetaPropia}] no puede reportar a [{etiquetaJefe}].";
                return false;
            }
        }

        if (newManager.directReports.Count >= newManager.maxDirectReports)
        {
            motivoRechazo = $"Capacidad llena: {newManager.roleName} alcanzó su límite de {newManager.maxDirectReports} reportes.";
            return false;
        }

        if (OrgValidator.CheckForCycles(newManager, this))
        {
            motivoRechazo = "Conflicto: esta conexión formaría un bucle cerrado.";
            return false;
        }

        return true;
    }

    public bool TryAssignManager(EmployeeNode newManager, out string motivoRechazo)
    {
        return TryAssignManager(newManager, out motivoRechazo, out _);
    }

    public bool TryAssignManager(EmployeeNode newManager, out string motivoRechazo, out bool esConexionMatricial)
    {
        if (!CanAssignManagerPreview(newManager, out motivoRechazo, out esConexionMatricial))
        {
            return false;
        }

        if (esConexionMatricial)
        {
            matrixProjectManager = newManager;
            newManager.directReports.Add(this);
            newManager.RefreshNodeVisuals();
            this.RefreshNodeVisuals();
            return true;
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
        EnsureVectorCardHierarchy();
        if (cachedText == null) cachedText = GetComponentInChildren<TextMeshProUGUI>();
        if (cachedText == null) return;

        string titulo = string.IsNullOrEmpty(displayTitle) ? roleName : displayTitle;
        string iconoRol = ObtenerIconoRol();

        // Actualizar pines vectoriales superior e inferior
        if (topPortImage != null)
        {
            topPortImage.gameObject.SetActive(!isRootNode);
            bool tieneJefe = (manager != null || matrixProjectManager != null);
            topPortImage.color = tieneJefe
                ? (isStaffNode ? VectorUIKit.AccentGold : VectorUIKit.AccentEmerald)
                : new Color(0.82f, 0.88f, 0.96f, 0.95f);
        }

        if (bottomPortImage != null)
        {
            bool puedeSupervisar = !isStaffNode && maxDirectReports > 0;
            bottomPortImage.gameObject.SetActive(puedeSupervisar);
            if (puedeSupervisar)
            {
                int actuales = directReports != null ? directReports.Count : 0;
                bottomPortImage.color = actuales >= maxDirectReports
                    ? VectorUIKit.AccentCoral
                    : (actuales > 0 ? VectorUIKit.AccentCyan : new Color(0.82f, 0.88f, 0.96f, 0.95f));
            }
        }

        string infoTramo = "";
        if (isStaffNode)
        {
            string estadoStaff = manager != null
                ? "<color=#065F46><b>● ASESORÍA ACTIVA</b></color>"
                : "<color=#78350F><b>○ ASIGNAR A CÚPULA</b></color>";
            infoTramo = $"\n<size=52%>{estadoStaff}</size>";
        }
        else if (maxDirectReports > 0)
        {
            int actuales = directReports != null ? directReports.Count : 0;
            string colorCupo = actuales >= maxDirectReports ? "#B91C1C" : "#065F46";
            string puntosCupo = ConstruirIndicadorPuntos(actuales, maxDirectReports);
            infoTramo = $"\n<size=56%><color={colorCupo}><b>Cupo: {actuales}/{maxDirectReports}</b> {puntosCupo}</color></size>";
        }
        else
        {
            bool esMatricial = LevelManager.Instance != null && LevelManager.Instance.criterioActual == CriterioDepartamentalizacion.Matricial;
            if (esMatricial)
            {
                int jefes = (manager != null ? 1 : 0) + (matrixProjectManager != null ? 1 : 0);
                string colMat = jefes >= 2 ? "#065F46" : (jefes == 1 ? "#0369A1" : "#475569");
                infoTramo = $"\n<size=52%><color={colMat}><b>Doble Mando: {jefes}/2 Jefes</b></color></size>";
            }
            else
            {
                bool conectado = (manager != null || isRootNode);
                string estadoConexion = conectado ? "<color=#065F46><b>● Conectado</b></color>" : "<color=#475569>○ Sin asignar</color>";
                infoTramo = $"\n<size=52%>{estadoConexion}</size>";
            }
        }

        cachedText.text = $"<color=#0F172A><b>{iconoRol} {titulo}</b></color>{infoTramo}";
    }

    private string ObtenerIconoRol()
    {
        if (isRootNode) return "👑";
        if (isStaffNode) return "⚖️";
        switch (nivel)
        {
            case NivelJerarquico.Director: return "💼";
            case NivelJerarquico.Gerente: return "📊";
            case NivelJerarquico.EmpleadoBase: return "👤";
            default: return "•";
        }
    }

    private string ConstruirIndicadorPuntos(int actuales, int max)
    {
        int mostrar = Mathf.Clamp(max, 1, 5);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < mostrar; i++)
        {
            sb.Append(i < actuales ? "●" : "○");
        }
        return sb.ToString();
    }

    public static string ObtenerEtiquetaNivel(NivelJerarquico n)
    {
        switch (n)
        {
            case NivelJerarquico.CEO: return "CEO / Cúpula";
            case NivelJerarquico.Director: return "Director VP";
            case NivelJerarquico.Gerente: return "Líder / Gerente";
            case NivelJerarquico.EmpleadoBase: return "Especialista";
            default: return n.ToString();
        }
    }
}