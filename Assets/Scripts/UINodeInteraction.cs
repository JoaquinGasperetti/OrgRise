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
        if (myNode == null || myNode.manager == null) return;

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
        myNode.RemoveCurrentManager();

        if (currentManagerLine != null)
        {
            Destroy(currentManagerLine);
            currentManagerLine = null;
        }

        if (tieneSubordinados && comodinListo && WildcardManager.Instance != null)
        {
            WildcardManager.Instance.ConsumirComodin();
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
        // para que el jugador pueda arrastrar tanto de subordinado->jefe como de jefe->subordinado
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

        // Restricción de reestructuración: si el subordinado que cambia de jefe ya tiene gente a cargo y ya tenía un jefe asignado (o se mueve en bloque)
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

        if (subordinateNode.TryAssignManager(managerNode, out string motivoRechazo))
        {
            if (subordinateUI.currentManagerLine != null)
            {
                Destroy(subordinateUI.currentManagerLine);
            }

            Transform lineParent = transform.parent != null ? transform.parent : canvas.transform;
            GameObject permLine = Instantiate(linePrefab, lineParent);
            permLine.transform.SetAsFirstSibling();

            UILineConnection lineLogic = permLine.GetComponent<UILineConnection>();
            if (lineLogic == null) lineLogic = permLine.AddComponent<UILineConnection>();

            lineLogic.startNode = managerUI.myRect;
            lineLogic.endNode = subordinateUI.myRect;
            lineLogic.SetLineColor(new Color(0.18f, 0.32f, 0.48f, 0.95f));
            lineLogic.UpdateGeometry(managerUI.myRect.position, subordinateUI.myRect.position);

            subordinateUI.currentManagerLine = permLine;

            // Consumir comodín si se reestructuró una rama activa
            if (subordinateNode.directReports.Count > 0 && comodinActivo && WildcardManager.Instance != null)
            {
                WildcardManager.Instance.ConsumirComodin();
            }

            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.PlaySnapFeedback(
                    managerUI.transform,
                    $"✓ Cadena de mando: {subordinateNode.roleName} → {managerNode.roleName} (Tramo: {managerNode.directReports.Count}/{managerNode.maxDirectReports})"
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