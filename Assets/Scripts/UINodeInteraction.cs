using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EmployeeNode))]
public class UINodeInteraction : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
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

    public void OnBeginDrag(PointerEventData eventData)
    {
        // RESTRICCIÓN: Si tiene subordinados y no está usando el comodín, bloqueamos el arrastre.
        if (myNode.directReports.Count > 0 && !WildcardManager.Instance.comodinActivo)
        {
            Debug.LogWarning("No puedes mover una rama entera sin un Comodín de Reestructuración.");
            eventData.pointerDrag = null; // Cancela el evento de arrastre de uGUI
            return;
        }

        GameObject lineObj = Instantiate(linePrefab, canvas.transform);
        tempLine = lineObj.GetComponent<RectTransform>();
        tempLine.SetAsFirstSibling();

        UpdateLineVisuals(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Actualizar la línea mientras movemos el cursor
        UpdateLineVisuals(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Destruir la línea temporal al soltar
        if (tempLine != null)
        {
            Destroy(tempLine.gameObject);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        UINodeInteraction droppedNodeUI = eventData.pointerDrag?.GetComponent<UINodeInteraction>();

        if (droppedNodeUI != null && droppedNodeUI != this)
        {
            EmployeeNode subordinateNode = droppedNodeUI.GetComponent<EmployeeNode>();

            if (subordinateNode.TryAssignManager(myNode))
            {
                if (droppedNodeUI.currentManagerLine != null) Destroy(droppedNodeUI.currentManagerLine);

                GameObject permLine = Instantiate(linePrefab, canvas.transform);
                permLine.transform.SetAsFirstSibling();

                UILineConnection lineLogic = permLine.AddComponent<UILineConnection>();
                lineLogic.startNode = myRect;
                lineLogic.endNode = droppedNodeUI.GetComponent<RectTransform>();

                droppedNodeUI.currentManagerLine = permLine;

                // CONSUMIR EL COMODÍN si el nodo movido tenía una rama colgada
                if (subordinateNode.directReports.Count > 0 && WildcardManager.Instance.comodinActivo)
                {
                    WildcardManager.Instance.ConsumirComodin();
                }

                LevelManager.Instance.EvaluarVictoria();
            }
        }
    }

    private void UpdateLineVisuals(Vector2 targetPosition)
    {
        if (tempLine == null) return;

        // Calcular distancia y ángulo desde este nodo hasta el mouse
        Vector2 startPos = myRect.position;
        Vector2 dir = targetPosition - startPos;
        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Posicionar, rotar y estirar la línea
        tempLine.position = startPos;
        tempLine.rotation = Quaternion.Euler(0, 0, angle);
        tempLine.sizeDelta = new Vector2(distance, 10f); // 10f es el grosor de la línea
    }
}