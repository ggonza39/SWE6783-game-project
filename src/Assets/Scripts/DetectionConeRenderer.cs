using UnityEngine;

/// <summary>
/// Renders a guard/camera's field-of-view cone as a semi-transparent mesh
/// that is actually visible in the Game view (and in builds) at runtime -
/// Gizmos (used elsewhere for debugging) only ever draw in the Scene view
/// and are compiled out of player builds entirely, so they can't serve as
/// the player-facing detection cue the backlog calls for.
///
/// This is a minimal, functional placeholder driven by the same numbers the
/// detection logic uses (via <see cref="IConeDetector"/>), so the cone shown
/// to the player can never be inconsistent with actual detection. It is a
/// separate, self-contained component so it can be freely restyled or
/// replaced by more polished UX/art-driven visuals later without touching
/// any detection logic.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DetectionConeRenderer : MonoBehaviour
{
    private const int Segments = 16;

    public IConeDetector Detector { get; set; }

    private Mesh mesh;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        mesh = new Mesh { name = "DetectionCone" };
        GetComponent<MeshFilter>().mesh = mesh;

        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.sortingOrder = 2;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Sprites/Default");

        Material material = new Material(shader) { name = "DetectionConeMaterial" };

        // Transparent surface so the cone overlays the floor without hiding it.
        if (shader.name.Contains("Universal Render Pipeline"))
        {
            material.SetFloat("_Surface", 1f); // Transparent
            material.SetFloat("_Blend", 0f); // Alpha blend
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        meshRenderer.material = material;
    }

    private void LateUpdate()
    {
        if (Detector == null)
        {
            meshRenderer.enabled = false;
            return;
        }

        meshRenderer.enabled = Detector.ConeActive;

        if (!Detector.ConeActive)
            return;

        meshRenderer.material.color = Detector.ConeColor;
        RebuildMesh();
    }

    private void RebuildMesh()
    {
        float range = Detector.ConeRange;
        float fovAngle = Detector.ConeFovAngle;

        // Build in local space using the detector's world-space forward
        // converted to local space, so the cone is correct regardless of
        // whether this object's own transform rotation tracks facing
        // (SecurityCamera rotates its transform; GuardController does not).
        Vector3 localForward = transform.InverseTransformDirection(
            new Vector3(Detector.ConeForward.x, Detector.ConeForward.y, 0f));

        if (localForward.sqrMagnitude < 0.0001f)
            localForward = Vector3.right;

        Vector3[] vertices = new Vector3[Segments + 2];
        int[] triangles = new int[Segments * 3];

        vertices[0] = Vector3.zero;

        float startAngle = -fovAngle / 2f;
        float angleStep = fovAngle / Segments;

        for (int i = 0; i <= Segments; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * localForward;
            vertices[i + 1] = direction.normalized * range;
        }

        for (int i = 0; i < Segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }
}
