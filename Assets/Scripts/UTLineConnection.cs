using UnityEngine;

public class UILineConnection : MonoBehaviour
{
    public RectTransform startNode;
    public RectTransform endNode;
    private RectTransform myRect;

    void Awake()
    {
        myRect = GetComponent<RectTransform>();
    }

    void Update()
    {
        // Si alguno de los nodos desaparece, la línea se autodestruye
        if (startNode == null || endNode == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 startPos = startNode.position;
        Vector2 endPos = endNode.position;
        Vector2 dir = endPos - startPos;

        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        myRect.position = startPos;
        myRect.rotation = Quaternion.Euler(0, 0, angle);
        myRect.sizeDelta = new Vector2(distance, 10f);
    }
}