using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EmployeeNode))]
public class UINodeInteraction : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    private EmployeeNode myNode;
    private RectTransform myRect;
    private Canvas canvas;

    [Header("Referencias UI")]
    public GameObject linePrefab;
    private RectTransform tempLine;
    private Image tempLineImage;
    private EmployeeNode lastHoveredNode;

    [HideInInspector] public GameObject currentManagerLine;
    [HideInInspector] public GameObject currentMatrixLine;

    void Awake()
    {
        myNode = GetComponent<EmployeeNode>();
        myRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right || eventData.clickCount >= 2)
        {
            TryDisconnectFromManager();
        }
    }

    public void TryDisconnectFromManager()
    {
        if (myNode == null) return;

        if (myNode.matrixProjectManager != null)
        {
            string exMatrixJefe = myNode.matrixProjectManager.roleName;
            myNode.RemoveMatrixProjectManager();
            if (currentMatrixLine != null)
            {
                Destroy(currentMatrixLine);
                currentMatrixLine = null;
            }

            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.PlaySnapFeedback(transform, $"Desconectado: {myNode.roleName} liberó su reporte matricial con {exMatrixJefe}.");
            }
            return;
        }

        if (myNode.manager == null) return;

        bool tieneSubordinados = myNode.directReports.Count > 0;
        bool comodinListo = WildcardManager.Instance != null && WildcardManager.Instance.comodinActivo;

        if (tieneSubordinados && !comodinListo)
        {
            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.PlayErrorFeedback(myNode, "Reestructuración bloqueada: este líder ya tiene equipo a cargo. Activá un ⚡ Comodín para mover toda la rama.");
            }
            return;
        }

        string exJefe = myNode.manager.roleName;
        EmployeeNode prevMgr = myNode.manager;
        myNode.RemoveCurrentManager();

        if (currentManagerLine != null)
        {
            Destroy(currentManagerLine);
            currentManagerLine = null;
        }

        bool comodinConsumido = false;
        if (tieneSubordinados && comodinListo && WildcardManager.Instance != null)
        {
            WildcardManager.Instance.ConsumirComodin();
            comodinConsumido = true;
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RegistrarAccionDesconexion(myNode, prevMgr, comodinConsumido);
        }

        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.PlaySnapFeedback(transform, $"Desconectado: {myNode.roleName} dejó de reportar a {exJefe}.");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (linePrefab == null || canvas == null) return;

        Transform lineParent = transform.parent != null ? transform.parent : canvas.transform;
        GameObject lineObj = Instantiate(linePrefab, lineParent);
        tempLineImage = lineObj.GetComponent<Image>();
        if (tempLineImage != null)
        {
            tempLineImage.raycastTarget = false;
            tempLineImage.sprite = VectorUIKit.GetRoundedSprite(pill: true);
            tempLineImage.type = Image.Type.Sliced;
            tempLineImage.color = VectorUIKit.AccentCyan;
        }

        tempLine = lineObj.GetComponent<RectTransform>();
        tempLine.SetAsFirstSibling();

        UpdateLineVisuals(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateLineVisuals(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        LimpiarHoverTemporal();
        if (tempLine != null)
        {
            Destroy(tempLine.gameObject);
            tempLine = null;
            tempLineImage = null;
        }
    }

    private void LimpiarHoverTemporal()
    {
        if (lastHoveredNode != null)
        {
            lastHoveredNode.SetDragHighlight(false, false);
            lastHoveredNode = null;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        LimpiarHoverTemporal();
        if (eventData.pointerDrag == null) return;

        UINodeInteraction draggedNodeUI = eventData.pointerDrag.GetComponent<UINodeInteraction>();
        if (draggedNodeUI == null || draggedNodeUI == this) return;

        ConectarDosNodos(draggedNodeUI.myNode, this.myNode);
    }

    /// <summary>
    /// Ejecuta la conexión entre dos nodos (usado tanto por Drag & Drop como por el sistema de Pista Inteligente / Headhunter).
    /// </summary>
    public static bool ConectarDosNodos(EmployeeNode nodeA, EmployeeNode nodeB, bool silencioso = false)
    {
        if (nodeA == null || nodeB == null || nodeA == nodeB) return false;

        EmployeeNode subordinateNode = nodeA;
        EmployeeNode managerNode = nodeB;

        if (nodeA.nivel < nodeB.nivel || (nodeA.isRootNode && !nodeB.isRootNode))
        {
            subordinateNode = nodeB;
            managerNode = nodeA;
        }

        UINodeInteraction subordinateUI = subordinateNode.GetComponent<UINodeInteraction>();
        UINodeInteraction managerUI = managerNode.GetComponent<UINodeInteraction>();
        if (subordinateUI == null || managerUI == null) return false;

        bool requiereComodin = !silencioso && subordinateNode.directReports.Count > 0 && subordinateNode.manager != null;
        bool comodinActivo = WildcardManager.Instance != null && WildcardManager.Instance.comodinActivo;

        if (requiereComodin && !comodinActivo)
        {
            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.PlayErrorFeedback(subordinateNode, "Para reestructurar un líder con equipo a cargo necesitás activar un ⚡ Comodín.");
            }
            if (LevelManager.Instance != null) LevelManager.Instance.RegistrarErrorMovimiento();
            return false;
        }

        EmployeeNode prevManager = subordinateNode.manager;

        if (subordinateNode.TryAssignManager(managerNode, out string motivoRechazo, out bool esConexionMatricial))
        {
            subordinateUI.RecrearLinea(managerNode, esConexionMatricial);

            bool comodinConsumido = false;
            if (!silencioso && subordinateNode.directReports.Count > 0 && comodinActivo && WildcardManager.Instance != null)
            {
                WildcardManager.Instance.ConsumirComodin();
                comodinConsumido = true;
            }

            if (!silencioso && LevelManager.Instance != null)
            {
                LevelManager.Instance.RegistrarAccionConexion(subordinateNode, prevManager, managerNode, esConexionMatricial, comodinConsumido);
            }

            if (!silencioso && FeedbackManager.Instance != null)
            {
                string tipoReporte = esConexionMatricial ? "Doble Mando Matricial" : (subordinateNode.isStaffNode ? "Consultoría VIP" : "Línea Ejecutiva");
                FeedbackManager.Instance.PlaySnapFeedback(
                    managerUI.transform,
                    $"✓ {tipoReporte}: {subordinateNode.roleName} → {managerNode.roleName}"
                );
            }

            if (!silencioso && TutorialManager.Instance != null)
            {
                TutorialManager.Instance.NotificarConexionExitosa();
            }

            if (!silencioso && LevelManager.Instance != null)
            {
                LevelManager.Instance.EvaluarVictoria();
            }
            return true;
        }
        else
        {
            if (!silencioso)
            {
                if (FeedbackManager.Instance != null)
                {
                    FeedbackManager.Instance.PlayErrorFeedback(subordinateNode, motivoRechazo);
                }
                if (LevelManager.Instance != null) LevelManager.Instance.RegistrarErrorMovimiento();
            }
            return false;
        }
    }

    public void DestruirLineas()
    {
        if (currentManagerLine != null)
        {
            Destroy(currentManagerLine);
            currentManagerLine = null;
        }
        if (currentMatrixLine != null)
        {
            Destroy(currentMatrixLine);
            currentMatrixLine = null;
        }
    }

    public void RecrearLinea(EmployeeNode targetManager, bool esMatricial)
    {
        if (targetManager == null || linePrefab == null) return;

        UINodeInteraction mgrUI = targetManager.GetComponent<UINodeInteraction>();
        if (mgrUI == null) return;

        Transform lineParent = transform.parent != null ? transform.parent : (canvas != null ? canvas.transform : transform);
        GameObject permLine = Instantiate(linePrefab, lineParent);
        permLine.transform.SetAsFirstSibling();

        UILineConnection lineLogic = permLine.GetComponent<UILineConnection>();
        if (lineLogic == null) lineLogic = permLine.AddComponent<UILineConnection>();

        lineLogic.startNode = mgrUI.myRect;
        lineLogic.endNode = this.myRect;

        if (esMatricial)
        {
            lineLogic.SetMatrixStyle(true);
            if (currentMatrixLine != null) Destroy(currentMatrixLine);
            currentMatrixLine = permLine;
        }
        else
        {
            if (myNode != null && myNode.isStaffNode)
            {
                lineLogic.SetStaffStyle(true);
            }
            else
            {
                lineLogic.SetLineColor(new Color(0.22f, 0.74f, 0.97f, 0.95f));
            }
            if (currentManagerLine != null) Destroy(currentManagerLine);
            currentManagerLine = permLine;
        }

        lineLogic.UpdateGeometry(targetManager.GetBottomPortWorldPosition(), myNode.GetTopPortWorldPosition());
    }

    private void UpdateLineVisuals(PointerEventData eventData)
    {
        if (tempLine == null || myRect == null) return;

        Vector2 screenPointerPos = eventData.position;
        tempLine.pivot = new Vector2(0f, 0.5f);
        tempLine.position = myRect.position;

        // Detectar si el puntero está sobre otra tarjeta para mostrar previsualización en vivo (Verde / Rojo)
        EmployeeNode hoveredNode = DetectarNodoBajoPuntero(eventData);
        if (hoveredNode != lastHoveredNode)
        {
            LimpiarHoverTemporal();
            lastHoveredNode = hoveredNode;
        }

        if (lastHoveredNode != null && lastHoveredNode != myNode)
        {
            EmployeeNode sub = myNode.nivel >= lastHoveredNode.nivel ? myNode : lastHoveredNode;
            EmployeeNode mgr = myNode.nivel >= lastHoveredNode.nivel ? lastHoveredNode : myNode;
            bool valido = sub.CanAssignManagerPreview(mgr, out _, out bool esMat);

            lastHoveredNode.SetDragHighlight(true, valido, esMat);
            if (tempLineImage != null)
            {
                tempLineImage.color = valido
                    ? (esMat ? VectorUIKit.AccentPurple : VectorUIKit.AccentEmerald)
                    : VectorUIKit.AccentCoral;
            }
        }
        else if (tempLineImage != null)
        {
            tempLineImage.color = VectorUIKit.AccentCyan;
        }

        RectTransform parentRect = tempLine.parent as RectTransform;
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPointerPos, cam, out Vector2 localPointerPos))
        {
            Vector2 localStart = parentRect.InverseTransformPoint(myRect.position);
            Vector2 dir = localPointerPos - localStart;
            float distance = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            tempLine.localRotation = Quaternion.Euler(0f, 0f, angle);
            tempLine.sizeDelta = new Vector2(distance, 9f);
        }
    }

    private EmployeeNode DetectarNodoBajoPuntero(PointerEventData eventData)
    {
        if (EventSystem.current == null) return null;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var res in results)
        {
            if (res.gameObject == null) continue;
            EmployeeNode node = res.gameObject.GetComponentInParent<EmployeeNode>();
            if (node != null && node != myNode)
            {
                return node;
            }
        }
        return null;
    }
}