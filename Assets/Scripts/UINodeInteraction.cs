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
        // Instanciar la línea temporal al empezar a arrastrar
        GameObject lineObj = Instantiate(linePrefab, canvas.transform);
        tempLine = lineObj.GetComponent<RectTransform>();
        tempLine.SetAsFirstSibling(); // Mandar al fondo para que quede detrás de los nodos

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
        UINodeInteraction droppedNodeUI = eventData.pointerDrag.GetComponent<UINodeInteraction>();

        if (droppedNodeUI != null && droppedNodeUI != this)
        {
            EmployeeNode subordinateNode = droppedNodeUI.GetComponent<EmployeeNode>();

            if (subordinateNode.TryAssignManager(myNode))
            {
                // 1. Destruir la línea anterior si el empleado ya tenía otro jefe
                if (droppedNodeUI.currentManagerLine != null)
                {
                    Destroy(droppedNodeUI.currentManagerLine);
                }

                // 2. Instanciar la línea permanente
                GameObject permLine = Instantiate(linePrefab, canvas.transform);
                permLine.transform.SetAsFirstSibling(); // Mandar al fondo

                // 3. Asignar los puntos de inicio y fin
                UILineConnection lineLogic = permLine.AddComponent<UILineConnection>();
                lineLogic.startNode = myRect; // Jefe
                lineLogic.endNode = droppedNodeUI.GetComponent<RectTransform>(); // Subordinado

                // 4. Guardar la referencia en el nodo subordinado
                droppedNodeUI.currentManagerLine = permLine;

                Debug.Log("Línea permanente creada con éxito.");

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