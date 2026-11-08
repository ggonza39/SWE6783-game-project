using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Visual feedback ONLY. Does not change detection, damage, movement,
// scene colliders, or gameplay rules.
public sealed class UXVisionConeOverlay : MonoBehaviour
{
    private const int Segments = 30;
    private Component detector;
    private FieldInfo rangeField;
    private FieldInfo angleField;
    private Transform visual;
    private Mesh mesh;
    private Material material;
    private LineRenderer outline;
    private float previousRange = -1f;
    private float previousAngle = -1f;
    private bool isCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Attach();
    }

    private static void Attach()
    {
        foreach (GuardDetection guard in
            UnityEngine.Object.FindObjectsByType<GuardDetection>(
                FindObjectsSortMode.None))
        {
            if (guard.GetComponent<UXVisionConeOverlay>() == null)
                guard.gameObject.AddComponent<UXVisionConeOverlay>();
        }

        foreach (SecurityCamera camera in
            UnityEngine.Object.FindObjectsByType<SecurityCamera>(
                FindObjectsSortMode.None))
        {
            if (camera.GetComponent<UXVisionConeOverlay>() == null)
                camera.gameObject.AddComponent<UXVisionConeOverlay>();
        }
    }

    private void Awake()
    {
        detector = GetComponent<SecurityCamera>();
        isCamera = detector != null;
        if (!isCamera)
            detector = GetComponent<GuardDetection>();

        if (detector == null)
        {
            enabled = false;
            return;
        }

        Type type = detector.GetType();
        BindingFlags flags = BindingFlags.Instance |
                             BindingFlags.NonPublic;

        rangeField = type.GetField("detectionRange", flags);
        angleField = type.GetField("detectionAngle", flags);

        if (rangeField == null || angleField == null)
        {
            Debug.LogWarning("UX vision overlay: detection fields unavailable.");
            enabled = false;
            return;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("UX vision overlay: transparent sprite shader unavailable.");
            enabled = false;
            return;
        }

        GameObject child = new GameObject("UX_VisualOnly_VisionCone");
        visual = child.transform;
        visual.SetParent(transform, false);
        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;

        mesh = new Mesh();
        mesh.name = "UX Detection Cone";

        MeshFilter filter = child.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        material = new Material(shader);
        material.name = "UX Vision Cone (Runtime)";
        material.mainTexture = Texture2D.whiteTexture;

        MeshRenderer renderer = child.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = 2;

        outline = child.AddComponent<LineRenderer>();
        outline.sharedMaterial = material;
        outline.useWorldSpace = false;
        outline.positionCount = Segments + 3;
        outline.startWidth = 0.025f;
        outline.endWidth = 0.025f;
        outline.sortingOrder = 2;

        Color edge = isCamera
            ? new Color(1f, 0.89f, 0.35f, 0.35f)
            : new Color(1f, 0.32f, 0.29f, 0.35f);

        outline.startColor = edge;
        outline.endColor = edge;
    }

    private void LateUpdate()
    {
        if (visual == null || detector == null) return;

        Vector3 scale = transform.lossyScale;
        visual.localScale = new Vector3(
            1f / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            1f / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
            1f
        );

        float range = (float)rangeField.GetValue(detector);
        float angle = (float)angleField.GetValue(detector);

        if (!Mathf.Approximately(range, previousRange) ||
            !Mathf.Approximately(angle, previousAngle))
        {
            Rebuild(range, angle);
            previousRange = range;
            previousAngle = angle;
        }
    }

    private void Rebuild(float range, float angle)
    {
        range = Mathf.Max(0.1f, range);
        angle = Mathf.Clamp(angle, 1f, 179f);

        Vector3[] vertices = new Vector3[Segments + 2];
        Color[] colors = new Color[Segments + 2];
        int[] triangles = new int[Segments * 3];

        Color tint = isCamera
            ? new Color(1f, 0.88f, 0.32f, 0.17f)
            : new Color(1f, 0.30f, 0.27f, 0.15f);

        vertices[0] = Vector3.zero;
        colors[0] = tint;
        outline.SetPosition(0, Vector3.zero);

        for (int i = 0; i <= Segments; i++)
        {
            float radians = (-angle * 0.5f +
                angle * i / Segments) * Mathf.Deg2Rad;

            Vector3 point = new Vector3(
                Mathf.Cos(radians) * range,
                Mathf.Sin(radians) * range, 0f);

            vertices[i + 1] = point;
            colors[i + 1] = new Color(
                tint.r, tint.g, tint.b, 0.055f);
            outline.SetPosition(i + 1, point);

            if (i < Segments)
            {
                int j = i * 3;
                triangles[j] = 0;
                triangles[j + 1] = i + 1;
                triangles[j + 2] = i + 2;
            }
        }

        outline.SetPosition(Segments + 2, Vector3.zero);
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }
}