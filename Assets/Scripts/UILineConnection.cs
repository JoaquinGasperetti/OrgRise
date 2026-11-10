using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dibuja conectores vectoriales ortogonales con codos redondeados y pulso de datos animado
/// desde el pin inferior del jefe hasta el pin superior del subordinado.
/// </summary>
public class UILineConnection : MonoBehaviour
{
    public RectTransform startNode;
    public RectTransform endNode;

    [Header("Estilo de Conexión")]
    public bool useOrthogonal = true;
    public bool isStaffConnection = false;
    public bool isMatrixConnection = false;
    public float lineThickness = 7f;

    private RectTransform myRect;
    private Image lineImage;
    private EmployeeNode startEmp;
    private EmployeeNode endEmp;

    private RectTransform segVerticalStart;
    private RectTransform segHorizontal;
    private RectTransform segVerticalEnd;
    private RectTransform pulseDot;

    private Image imgVStart;
    private Image imgH;
    private Image imgVEnd;
    private Image imgPulse;

    private Color currentColor = new Color(0.22f, 0.74f, 0.97f, 0.95f); // #38BDF8 Sky Vector Blue
    private float pulsePhase = 0f;

    void Awake()
    {
        myRect = GetComponent<RectTransform>();
        lineImage = GetComponent<Image>();
        if (lineImage != null)
        {
            lineImage.raycastTarget = false;
            lineImage.sprite = VectorUIKit.GetRoundedSprite(pill: true);
            lineImage.type = Image.Type.Sliced;
        }

        pulsePhase = Random.Range(0f, 1f);
        AsegurarSegmentosOrtogonales();
    }

    private void AsegurarSegmentosOrtogonales()
    {
        if (segVerticalStart != null) return;

        segVerticalStart = CrearSegmento("Seg_VStart", false);
        segHorizontal = CrearSegmento("Seg_Horiz", false);
        segVerticalEnd = CrearSegmento("Seg_VEnd", false);
        pulseDot = CrearSegmento("Seg_PulseDot", true);
        pulseDot.sizeDelta = new Vector2(13f, 13f);

        imgVStart = segVerticalStart.GetComponent<Image>();
        imgH = segHorizontal.GetComponent<Image>();
        imgVEnd = segVerticalEnd.GetComponent<Image>();
        imgPulse = pulseDot.GetComponent<Image>();

        ActualizarColoresSegmentos();
    }

    private RectTransform CrearSegmento(string nombre, bool circular)
    {
        GameObject segObj = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        segObj.transform.SetParent(transform, false);
        RectTransform rt = segObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Image img = segObj.GetComponent<Image>();
        img.raycastTarget = false;
        img.sprite = circular ? VectorUIKit.GetCircleSprite() : VectorUIKit.GetRoundedSprite(pill: true);
        img.type = circular ? Image.Type.Simple : Image.Type.Sliced;
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
            SetLineColor(VectorUIKit.AccentGold);
        }
    }

    public void SetMatrixStyle(bool isMatrix)
    {
        isMatrixConnection = isMatrix;
        if (isMatrix)
        {
            SetLineColor(VectorUIKit.AccentPurple);
        }
    }

    private void ActualizarColoresSegmentos()
    {
        if (imgVStart != null) imgVStart.color = currentColor;
        if (imgH != null) imgH.color = currentColor;
        if (imgVEnd != null) imgVEnd.color = currentColor;
        if (imgPulse != null) imgPulse.color = Color.Lerp(currentColor, Color.white, 0.65f);
    }

    void LateUpdate()
    {
        if (startNode == null || endNode == null)
        {
            Destroy(gameObject);
            return;
        }

        if (startEmp == null) startEmp = startNode.GetComponent<EmployeeNode>();
        if (endEmp == null) endEmp = endNode.GetComponent<EmployeeNode>();

        Vector2 startPos = startEmp != null ? (Vector2)startEmp.GetBottomPortWorldPosition() : (Vector2)startNode.position;
        Vector2 endPos = endEmp != null ? (Vector2)endEmp.GetTopPortWorldPosition() : (Vector2)endNode.position;

        UpdateGeometry(startPos, endPos);
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

    public void UpdateDirectGeometry(Vector2 startWorldPos, Vector2 endWorldPos)
    {
        if (myRect == null) myRect = GetComponent<RectTransform>();

        if (segVerticalStart != null) segVerticalStart.gameObject.SetActive(false);
        if (segHorizontal != null) segHorizontal.gameObject.SetActive(false);
        if (segVerticalEnd != null) segVerticalEnd.gameObject.SetActive(false);
        if (pulseDot != null) pulseDot.gameObject.SetActive(false);

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
        pulseDot.gameObject.SetActive(true);

        float offsetMid = isMatrixConnection ? 0.62f : (isStaffConnection ? 0.38f : 0.50f);
        float yMid = Mathf.Lerp(localStart.y, localEnd.y, offsetMid);

        // Segmento 1: bajada desde el puerto inferior del superior
        float v1Length = Mathf.Abs(localStart.y - yMid);
        segVerticalStart.localPosition = new Vector2(localStart.x, (localStart.y + yMid) * 0.5f);
        segVerticalStart.sizeDelta = new Vector2(lineThickness, Mathf.Max(lineThickness, v1Length + lineThickness));

        // Segmento 2: tramo horizontal intermedio
        float hLength = Mathf.Abs(localEnd.x - localStart.x);
        segHorizontal.localPosition = new Vector2((localStart.x + localEnd.x) * 0.5f, yMid);
        segHorizontal.sizeDelta = new Vector2(Mathf.Max(lineThickness, hLength + lineThickness), lineThickness);

        // Segmento 3: llegada al puerto superior del subordinado
        float v2Length = Mathf.Abs(yMid - localEnd.y);
        segVerticalEnd.localPosition = new Vector2(localEnd.x, (yMid + localEnd.y) * 0.5f);
        segVerticalEnd.sizeDelta = new Vector2(lineThickness, Mathf.Max(lineThickness, v2Length + lineThickness));

        // Animación continua del pulso de flujo de datos a lo largo de los 3 tramos
        pulsePhase = (pulsePhase + Time.deltaTime * 0.65f) % 1f;
        Vector2 p0 = new Vector2(localEnd.x, localEnd.y);
        Vector2 p1 = new Vector2(localEnd.x, yMid);
        Vector2 p2 = new Vector2(localStart.x, yMid);
        Vector2 p3 = new Vector2(localStart.x, localStart.y);

        if (pulsePhase < 0.33f)
        {
            pulseDot.localPosition = Vector2.Lerp(p0, p1, pulsePhase / 0.33f);
        }
        else if (pulsePhase < 0.66f)
        {
            pulseDot.localPosition = Vector2.Lerp(p1, p2, (pulsePhase - 0.33f) / 0.33f);
        }
        else
        {
            pulseDot.localPosition = Vector2.Lerp(p2, p3, (pulsePhase - 0.66f) / 0.34f);
        }
    }
}