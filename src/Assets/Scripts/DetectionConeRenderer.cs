using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Draws the detector's field of view as a translucent runtime mesh.
/// </summary>
[DisallowMultipleComponent]
public class DetectionConeRenderer : MonoBehaviour
{
    private const int Segments = 48;

    public IConeDetector Detector { get; set; }

    private readonly Vector3[] vertices = new Vector3[Segments + 2];
    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private Material material;
    private Transform player;
    private readonly RaycastHit2D[] sightHits = new RaycastHit2D[32];

    private void Awake()
    {
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
            player = playerController.transform;

        Shader shader = GraphicsSettings.currentRenderPipeline != null
            ? Shader.Find("Universal Render Pipeline/Unlit")
            : null;
        bool useUrpMaterial = shader != null && shader.isSupported;
        if (!useUrpMaterial)
            shader = Shader.Find("Sprites/Default");

        if (shader == null || !shader.isSupported)
        {
            Debug.LogWarning("Detection cone shader unavailable.", this);
            enabled = false;
            return;
        }

        // Keep the mesh separate from the detector's sprite renderer.
        GameObject cone = new GameObject("Detection Cone") { layer = gameObject.layer };
        cone.transform.SetParent(transform, false);
        MeshFilter meshFilter = cone.AddComponent<MeshFilter>();
        meshRenderer = cone.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.sortingOrder = 2;
        meshRenderer.enabled = false;

        mesh = new Mesh { name = "DetectionCone" };
        mesh.MarkDynamic();
        meshFilter.sharedMesh = mesh;

        int[] triangles = new int[Segments * 3];
        Color32[] colors = new Color32[vertices.Length];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.white;

        // The top-down camera sees these triangles from the negative Z side.
        for (int i = 0; i < Segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 2;
            triangles[i * 3 + 2] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.colors32 = colors;
        mesh.triangles = triangles;

        material = new Material(shader) { name = "DetectionConeMaterial" };
        material.mainTexture = Texture2D.whiteTexture;
        if (useUrpMaterial)
        {
            // URP's material inspector normally sets these blend states.
            material.SetFloat("_Surface", 1f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
            material.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        meshRenderer.sharedMaterial = material;
    }

    private void LateUpdate()
    {
        meshRenderer.enabled = Detector != null && Detector.ConeActive;
        if (!meshRenderer.enabled)
            return;

        material.color = Detector.ConeColor;
        RebuildMesh();
    }

    private void RebuildMesh()
    {
        float range = Detector.ConeRange;
        float fovAngle = Detector.ConeFovAngle;

        Vector2 origin = Detector.ConeOrigin;
        Vector3 worldOrigin = new Vector3(origin.x, origin.y, transform.position.z);
        Transform coneTransform = meshRenderer.transform;
        coneTransform.position = worldOrigin;

        Vector3 forward = Detector.ConeForward;
        forward = forward.sqrMagnitude < 0.0001f ? Vector3.right : forward.normalized;

        float startAngle = -fovAngle / 2f;
        float angleStep = fovAngle / Segments;

        for (int i = 0; i <= Segments; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * forward;
            float visibleRange = DetectionUtility.GetSightDistance(
                origin, direction, range, transform, sightHits, player);
            vertices[i + 1] = coneTransform.InverseTransformPoint(worldOrigin + direction * visibleRange);
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }

    private void OnDisable()
    {
        if (meshRenderer != null)
            meshRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (meshRenderer != null)
            Destroy(meshRenderer.gameObject);
        Destroy(mesh);
        Destroy(material);
    }
}
