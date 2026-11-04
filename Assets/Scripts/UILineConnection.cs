using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dibuja y actualiza la conexión visual entre dos roles del organigrama.
/// Soporta trazado ortogonal jerárquico (en codo), conexiones lineales directas,
/// y estilos diferenciados para Órganos de Staff (asesoría) y Estructuras Matriciales (doble reporte).
/// </summary>
public class UILineConnection : MonoBehaviour
{
    public RectTransform startNode;
    public RectTransform endNode;

    [Header("Estilo de Conexión")]
    public bool useOrthogonal = true;
    public bool isStaffConnection = false;
    public bool isMatrixConnection = false;
    public float lineThickness = 6f;

    private RectTransform myRect;
    private Image lineImage;

    // Segmentos para trazado ortogonal escalonado (en codo)
    private RectTransform segVerticalStart;
    private RectTransform segHorizontal;
    private RectTransform segVerticalEnd;
    private Image imgVStart;
    private Image imgH;
    private Image imgVEnd;

    private Color currentColor = new Color(0.18f, 0.32f, 0.48f, 0.95f);

    void Awake()
    {
        myRect = GetComponent<RectTransform>();
        lineImage = GetComponent<Image>();
        if (lineImage != null)
        {
            lineImage.raycastTarget = false;
        }

        AsegurarSegmentosOrtogonales();
    }

    private void AsegurarSegmentosOrtogonales()
    {
        if (segVerticalStart != null) return;

        segVerticalStart = CrearSegmento("Seg_VStart");
        segHorizontal = CrearSegmento("Seg_Horiz");
        segVerticalEnd = CrearSegmento("Seg_VEnd");

        imgVStart = segVerticalStart.GetComponent<Image>();
        imgH = segHorizontal.GetComponent<Image>();
        imgVEnd = segVerticalEnd.GetComponent<Image>();

        ActualizarColoresSegmentos();
    }

    private RectTransform CrearSegmento(string nombre)
    {
        GameObject segObj = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        segObj.transform.SetParent(transform, false);
        RectTransform rt = segObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Image img = segObj.GetComponent<Image>();
        img.raycastTarget = false;
        return rt;
    }

    public void SetLineColor(Color c)
    {
        currentColor = c;
        if (lineImage == null) lineImage = GetComponent<Image>();
        if (lineImage != null) lineImage.color = c;
        ActualizarColoresSegmentos();
    }

    public void SetStaffStyle(bool isStaff)
    {
        isStaffConnection = isStaff;
        if (isStaff)
        {
            SetLineColor(new Color(0.92f, 0.72f, 0.18f, 0.95f)); // Dorado/Ámbar representativo de Staff de Apoyo
        }
    }

    public void SetMatrixStyle(bool isMatrix)
    {
        isMatrixConnection = isMatrix;
        if (isMatrix)
        {
            SetLineColor(new Color(0.22f, 0.75f, 0.85f, 0.95f)); // Cian/Celeste para línea transversal de proyecto
        }
    }

    private void ActualizarColoresSegmentos()
    {
        if (imgVStart != null) imgVStart.color = currentColor;
        if (imgH != null) imgH.color = currentColor;
        if (imgVEnd != null) imgVEnd.color = currentColor;
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

        if (useOrthogonal)
        {
            UpdateOrthogonalGeometry(startWorldPos, endWorldPos);
        }
        else
        {
            UpdateDirectGeometry(startWorldPos, endWorldPos);
        }
    }

    /// <summary>
    /// Trazado directo en diagonal (utilizado durante el arrastre preliminar).
    /// </summary>
    public void UpdateDirectGeometry(Vector2 startWorldPos, Vector2 endWorldPos)
    {
        if (myRect == null) myRect = GetComponent<RectTransform>();

        // Desactivamos segmentos ortogonales si existieran
        if (segVerticalStart != null) segVerticalStart.gameObject.SetActive(false);
        if (segHorizontal != null) segHorizontal.gameObject.SetActive(false);
        if (segVerticalEnd != null) segVerticalEnd.gameObject.SetActive(false);

        if (lineImage != null) lineImage.enabled = true;

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
        myRect.sizeDelta = new Vector2(distance, lineThickness);
    }

    /// <summary>
    /// Trazado ortogonal escalonado (en codo):
    /// Línea vertical que desciende del superior, bus horizontal intermedio,
    /// y bajada vertical hacia el subordinado.
    /// Evita atravesar tarjetas en diagonal y otorga la estética profesional de organigrama.
    /// </summary>
    public void UpdateOrthogonalGeometry(Vector2 startWorldPos, Vector2 endWorldPos)
    {
        AsegurarSegmentosOrtogonales();

        if (lineImage != null) lineImage.enabled = false;

        Transform parentTransform = myRect.parent != null ? myRect.parent : transform;
        Vector3 localStart = parentTransform.InverseTransformPoint(startWorldPos);
        Vector3 localEnd = parentTransform.InverseTransformPoint(endWorldPos);

        myRect.localPosition = Vector3.zero;
        myRect.localRotation = Quaternion.identity;
        myRect.sizeDelta = Vector2.zero;

        segVerticalStart.gameObject.SetActive(true);
        segHorizontal.gameObject.SetActive(true);
        segVerticalEnd.gameObject.SetActive(true);

        float yMid = Mathf.Lerp(localStart.y, localEnd.y, 0.5f);

        // Segmento 1: vertical desde el superior hasta el codo medio
        float v1Length = Mathf.Abs(localStart.y - yMid);
        segVerticalStart.localPosition = new Vector2(localStart.x, (localStart.y + yMid) * 0.5f);
        segVerticalStart.sizeDelta = new Vector2(lineThickness, Mathf.Max(2f, v1Length));

        // Segmento 2: horizontal entre las columnas de ambos nodos
        float hLength = Mathf.Abs(localEnd.x - localStart.x);
        segHorizontal.localPosition = new Vector2((localStart.x + localEnd.x) * 0.5f, yMid);
        segHorizontal.sizeDelta = new Vector2(Mathf.Max(2f, hLength + lineThickness), lineThickness);

        // Segmento 3: vertical desde el codo medio hasta el subordinado
        float v2Length = Mathf.Abs(yMid - localEnd.y);
        segVerticalEnd.localPosition = new Vector2(localEnd.x, (yMid + localEnd.y) * 0.5f);
        segVerticalEnd.sizeDelta = new Vector2(lineThickness, Mathf.Max(2f, v2Length));
    }
}