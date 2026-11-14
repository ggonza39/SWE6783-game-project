// Rendering API stand-ins for type checks and mesh-math checks, not a GPU renderer.
namespace UnityEngine
{
    public sealed class Shader : Object
    {
        public bool isSupported = true;
        public static Shader Find(string name) => new Shader { name = name };
    }
    public sealed class Material : Object
    {
        public Texture2D mainTexture;
        public Color color;
        public int renderQueue;
        public Material(Shader shader) { }
        public void SetFloat(string key, float value) { }
        public void SetInt(string key, int value) { }
        public void SetOverrideTag(string key, string value) { }
        public void EnableKeyword(string key) { }
    }
    public sealed class Mesh : Object
    {
        public Vector3[] vertices;
        public Color32[] colors32;
        public int[] triangles;
        public void MarkDynamic() { }
        public void RecalculateBounds() { }
    }
    public sealed class MeshFilter : Component { public Mesh sharedMesh; }
    public sealed class MeshRenderer : Component
    {
        public bool enabled, receiveShadows;
        public int sortingOrder;
        public Material sharedMaterial;
        public Rendering.ShadowCastingMode shadowCastingMode;
    }
}
namespace UnityEngine.Rendering
{
    public static class GraphicsSettings { public static object currentRenderPipeline; }
    public enum ShadowCastingMode { Off }
    public enum BlendMode { One, SrcAlpha, OneMinusSrcAlpha }
    public enum RenderQueue { Transparent = 3000 }
}
