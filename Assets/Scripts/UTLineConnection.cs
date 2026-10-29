using UnityEngine;
using UnityEngine.UI;

public class UILineConnection : MonoBehaviour
{
    public RectTransform startNode;
    public RectTransform endNode;
    private RectTransform myRect;
    private Image lineImage;

    void Awake()
    {
        myRect = GetComponent<RectTransform>();
        lineImage = GetComponent<Image>();
        if (lineImage != null)
        {
            lineImage.raycastTarget = false;
        }
    }

    public void SetLineColor(Color c)
    {
        if (lineImage == null) lineImage = GetComponent<Image>();
        if (lineImage != null) lineImage.color = c;
    }

    void LateUpdate()
    {
        // Si alguno de los nodos desaparece, la línea se autodestruye
        if (startNode == null || endNode == null)
        {
            Destroy(gameObject);
            return;
        }

        UpdateGeometry(startNode.position, endNode.position);
    }

    public void UpdateGeometry(Vector2 startWorldPos, Vector2 endWorldPos)
    {
        if (myRect == null) myRect = GetComponent<RectTransform>();

        myRect.pivot = new Vector2(0f, 0.5f);
        myRect.position = startWorldPos;

        Vector3 localEnd = myRect.parent != null
            ? myRect.parent.InverseTransformPoint(endWorldPos)
            : (Vector3)endWorldPos;
        Vector3 localStart = myRect.parent != null
            ? myRect.parent.InverseTransformPoint(startWorldPos)
            : (Vector3)startWorldPos;

        Vector2 dir = (Vector2)(localEnd - localStart);
        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        myRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        myRect.sizeDelta = new Vector2(distance, 8f);
    }
}