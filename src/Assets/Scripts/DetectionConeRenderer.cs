using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Draws the detector's field of view as a translucent runtime mesh.
/// </summary>
[DisallowMultipleComponent]
public class DetectionConeRenderer : MonoBehaviour
{
    private const int Segments = 48;
    private const int EdgeSearchSteps = 6;
    private const int MaxMeshSegments = Segments * 3;
    private const float EdgeDistanceThreshold = 0.25f;

    public IConeDetector Detector { get; set; }

    private readonly Vector3[] vertices = new Vector3[MaxMeshSegments + 2];
    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private Material material;
    private Transform player;
    private Camera view;
    private readonly RaycastHit2D[] sightHits = new RaycastHit2D[32];

    private void Awake()
    {
        view=Camera.main;
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

        int[] triangles = new int[MaxMeshSegments * 3];
        Color32[] colors = new Color32[vertices.Length];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.white;

        // The top-down camera sees these triangles from the negative Z side.
        for (int i = 0; i < MaxMeshSegments; i++)
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
        if (meshRenderer == null || mesh == null || material == null) return;
        meshRenderer.enabled = Detector != null && Detector.ConeActive;
        if (!meshRenderer.enabled)
            return;
        if(view!=null)
        {
            Vector2 delta=Detector.ConeOrigin-(Vector2)view.transform.position;
            if(Mathf.Abs(delta.x)>view.orthographicSize*view.aspect+Detector.ConeRange ||
                Mathf.Abs(delta.y)>view.orthographicSize+Detector.ConeRange)
            {meshRenderer.enabled=false;return;}
        }

        material.color = Detector.ConeColor;
        RebuildMesh();
    }

    private void RebuildMesh()
    {
        float range = Detector.ConeRange;
        float fovAngle = Detector.ConeFovAngle;

        Vector2 origin = Detector.ConeOrigin;
        // Stay just behind coplanar wall sprites at the same sorting order.
        Vector3 worldOrigin = new Vector3(origin.x, origin.y, transform.position.z + 0.01f);
        Transform coneTransform = meshRenderer.transform;
        coneTransform.position = worldOrigin;

        Vector3 forward = Detector.ConeForward;
        forward = forward.sqrMagnitude < 0.0001f ? Vector3.right : forward.normalized;

        float startAngle = -fovAngle / 2f;
        float angleStep = fovAngle / Segments;

        int count = 1;
        SightSample previous = Cast(origin, worldOrigin, forward, startAngle, range);
        vertices[count] = coneTransform.InverseTransformPoint(previous.point);
        for (int i = 1; i <= Segments; i++)
        {
            SightSample current = Cast(origin, worldOrigin, forward, startAngle + angleStep * i, range);
            if (Mathf.Abs(previous.distance - current.distance) > EdgeDistanceThreshold)
            {
                // Locate the silhouette between rays instead of stretching one
                // large triangle across a wall corner as the detector turns.
                SightSample near = previous, far = current;
                for (int step = 0; step < EdgeSearchSteps; step++)
                {
                    SightSample middle = Cast(origin, worldOrigin, forward,
                        (near.angle + far.angle) * 0.5f, range);
                    if (Mathf.Abs(near.distance - middle.distance) > EdgeDistanceThreshold)
                        far = middle;
                    else near = middle;
                }
                vertices[++count] = coneTransform.InverseTransformPoint(near.point);
                vertices[++count] = coneTransform.InverseTransformPoint(far.point);
            }
            vertices[++count] = coneTransform.InverseTransformPoint(current.point);
            previous = current;
        }

        // Reuse the same mesh arrays. Unused fan triangles collapse to zero area.
        for (int i = count + 1; i < vertices.Length; i++) vertices[i] = vertices[count];
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }

    private struct SightSample
    {
        public float angle, distance;
        public Vector3 point;
    }

    private SightSample Cast(Vector2 origin, Vector3 worldOrigin, Vector3 forward, float angle, float range)
    {
        Vector3 direction = Quaternion.Euler(0f, 0f, angle) * forward;
        float distance = DetectionUtility.GetSightDistance(origin, direction, range, transform, sightHits, player);
        return new SightSample { angle = angle, distance = distance, point = worldOrigin + direction * distance };
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
