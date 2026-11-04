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
    public GameObject linePrefab; // Prefab de la línea (Image)
    private RectTransform tempLine; // Línea que se dibuja al arrastrar

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
        // Doble clic / doble toque o clic derecho para desconectar al subordinado de su jefe actual
        if (eventData.button == PointerEventData.InputButton.Right || eventData.clickCount >= 2)
        {
            TryDisconnectFromManager();
        }
    }

    public void TryDisconnectFromManager()
    {
        if (myNode == null) return;

        // Si tiene conexión matricial activa y se solicita desconexión, removemos la matricial primero
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
                FeedbackManager.Instance.PlayErrorFeedback(myNode, "Reestructuración bloqueada: este mando ya tiene subordinados a cargo. Activá el Comodín para mover o desconectar toda la rama.");
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
        Image lineImg = lineObj.GetComponent<Image>();
        if (lineImg != null)
        {
            lineImg.raycastTarget = false;
            lineImg.color = new Color(0.2f, 0.25f, 0.35f, 0.85f);
        }

        tempLine = lineObj.GetComponent<RectTransform>();
        tempLine.SetAsFirstSibling();

        UpdateLineVisuals(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateLineVisuals(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (tempLine != null)
        {
            Destroy(tempLine.gameObject);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        UINodeInteraction draggedNodeUI = eventData.pointerDrag.GetComponent<UINodeInteraction>();
        if (draggedNodeUI == null || draggedNodeUI == this) return;

        EmployeeNode nodeA = draggedNodeUI.myNode;
        EmployeeNode nodeB = this.myNode;

        if (nodeA == null || nodeB == null) return;

        // Determinamos automáticamente quién es el subordinado y quién es el superior según su nivel jerárquico
        EmployeeNode subordinateNode = nodeA;
        EmployeeNode managerNode = nodeB;
        UINodeInteraction subordinateUI = draggedNodeUI;
        UINodeInteraction managerUI = this;

        if (nodeA.nivel < nodeB.nivel)
        {
            subordinateNode = nodeB;
            managerNode = nodeA;
            subordinateUI = this;
            managerUI = draggedNodeUI;
        }

        // Restricción de reestructuración: si el subordinado que cambia de jefe ya tiene gente a cargo y ya tenía un jefe asignado
        bool requiereComodin = subordinateNode.directReports.Count > 0 && subordinateNode.manager != null;
        bool comodinActivo = WildcardManager.Instance != null && WildcardManager.Instance.comodinActivo;

        if (requiereComodin && !comodinActivo)
        {
            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.PlayErrorFeedback(subordinateNode, "No podés reasignar una rama con subordinados sin activar el Comodín de Reestructuración.");
            }
            return;
        }

        EmployeeNode prevManager = subordinateNode.manager;

        if (subordinateNode.TryAssignManager(managerNode, out string motivoRechazo, out bool esConexionMatricial))
        {
            Transform lineParent = transform.parent != null ? transform.parent : canvas.transform;
            GameObject permLine = Instantiate(linePrefab, lineParent);
            permLine.transform.SetAsFirstSibling();

            UILineConnection lineLogic = permLine.GetComponent<UILineConnection>();
            if (lineLogic == null) lineLogic = permLine.AddComponent<UILineConnection>();

            lineLogic.startNode = managerUI.myRect;
            lineLogic.endNode = subordinateUI.myRect;

            if (esConexionMatricial)
            {
                lineLogic.SetMatrixStyle(true);
                if (subordinateUI.currentMatrixLine != null) Destroy(subordinateUI.currentMatrixLine);
                subordinateUI.currentMatrixLine = permLine;
            }
            else
            {
                if (subordinateNode.isStaffNode)
                {
                    lineLogic.SetStaffStyle(true);
                }
                else
                {
                    lineLogic.SetLineColor(new Color(0.18f, 0.32f, 0.48f, 0.95f));
                }

                if (subordinateUI.currentManagerLine != null) Destroy(subordinateUI.currentManagerLine);
                subordinateUI.currentManagerLine = permLine;
            }

            lineLogic.UpdateGeometry(managerUI.myRect.position, subordinateUI.myRect.position);

            // Consumir comodín si se reestructuró una rama activa
            bool comodinConsumido = false;
            if (subordinateNode.directReports.Count > 0 && comodinActivo && WildcardManager.Instance != null)
            {
                WildcardManager.Instance.ConsumirComodin();
                comodinConsumido = true;
            }

            // Registrar acción para Deshacer (Undo)
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RegistrarAccionConexion(subordinateNode, prevManager, managerNode, esConexionMatricial, comodinConsumido);
            }

            if (FeedbackManager.Instance != null)
            {
                string tipoReporte = esConexionMatricial ? "Reporte Matricial (Transversal)" : (subordinateNode.isStaffNode ? "Asesoría Staff" : "Cadena de Mando");
                FeedbackManager.Instance.PlaySnapFeedback(
                    managerUI.transform,
                    $"✓ {tipoReporte}: {subordinateNode.roleName} → {managerNode.roleName} (Tramo: {managerNode.directReports.Count}/{managerNode.maxDirectReports})"
                );
            }

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.NotificarConexionExitosa();
            }

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.EvaluarVictoria();
            }
        }
        else
        {
            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.PlayErrorFeedback(subordinateNode, motivoRechazo);
            }
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
                lineLogic.SetLineColor(new Color(0.18f, 0.32f, 0.48f, 0.95f));
            }
            if (currentManagerLine != null) Destroy(currentManagerLine);
            currentManagerLine = permLine;
        }

        lineLogic.UpdateGeometry(mgrUI.myRect.position, this.myRect.position);
    }

    private void UpdateLineVisuals(Vector2 screenPointerPos)
    {
        if (tempLine == null || myRect == null) return;

        tempLine.pivot = new Vector2(0f, 0.5f);
        tempLine.position = myRect.position;

        RectTransform parentRect = tempLine.parent as RectTransform;
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPointerPos, cam, out Vector2 localPointerPos))
        {
            Vector2 localStart = parentRect.InverseTransformPoint(myRect.position);
            Vector2 dir = localPointerPos - localStart;
            float distance = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            tempLine.localRotation = Quaternion.Euler(0f, 0f, angle);
            tempLine.sizeDelta = new Vector2(distance, 8f);
        }
    }
}