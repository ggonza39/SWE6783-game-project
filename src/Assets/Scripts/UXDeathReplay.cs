using System;
using System.Collections;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine.Rendering;

#endif

// A bounded GPU-only buffer of the rendered Game view, including the real HUD,
// guards, projectiles and hazard warning. Never rewinds/resimulates game objects,
// reads pixels to the CPU, writes recordings to disk, or changes saved progress.
public sealed class UXDeathReplay : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    // Keep four seconds of browser replay at lower GPU cost than desktop.
    public const int Capacity = 40;
    public const float SampleInterval = 1f / 10f;
    public const int FrameWidth = 640, FrameHeight = 360;
#else
    public const int Capacity = 80;
    public const float SampleInterval = 1f / 20f;
    public const int FrameWidth = 1280, FrameHeight = 720;
#endif
    private const float PlaybackSpeed = 1f;
    private readonly RenderTexture[] frames = new RenderTexture[Capacity];
    private readonly DateTime[] frameTimes = new DateTime[Capacity];
    private readonly WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
    private UXStealthDirector director;
    private RenderTexture capture;
    private Camera sourceView;
    private Rect capturedRegion;
    private int write, count;
    private float nextSample, freezeAt = -1f, frozenAt = -1f, loopAt;
    private bool autoSkipped, unavailable;
    public DateTime DeathLocalTime { get; private set; }
    public float DeathMissionSeconds { get; private set; }
    public string CameraLabel => director!=null && director.IsCrimson ? "CAM 02 / INCIDENT ARCHIVE" : "CAM 01 / INCIDENT ARCHIVE";
    public bool HasFrames => frozenAt >= 0f && count >= 2 && !unavailable;
    public bool WaitingForFreeze => freezeAt >= 0f && frozenAt < 0f && count >= 2 && !unavailable;
    public float ClipSeconds => Mathf.Max(SampleInterval, (count - 1) * SampleInterval);
    public bool AutoPlaying => HasFrames && !autoSkipped && Time.unscaledTime - frozenAt < ClipSeconds / PlaybackSpeed + .25f;
    public float Progress => count < 2 ? 0f : CurrentIndex / (float)(count - 1);
    public float CursorSeconds => HasFrames ? CurrentIndex*SampleInterval : 0f;
    public DateTime CurrentFrameTime => HasFrames ? frameTimes[(write-count+CurrentIndex+Capacity)%Capacity] : DeathLocalTime;
    private int CurrentIndex
    {
        get
        {
            float elapsed = Mathf.Max(0f, Time.unscaledTime - (AutoPlaying ? frozenAt : loopAt));
            float position = AutoPlaying ? Mathf.Min(ClipSeconds, elapsed * PlaybackSpeed) : Mathf.Repeat(elapsed, ClipSeconds + .35f);
            return Mathf.Min(count - 1, Mathf.FloorToInt(position / SampleInterval));
        }
    }
    public RenderTexture CurrentFrame => HasFrames ? frames[(write - count + CurrentIndex + Capacity) % Capacity] : null;
    // Captured Game-view RTs use the graphics backend's vertical convention.
    // Explicit UVs correct top-origin backends (including the observed D3D
    // capture) while leaving bottom-origin backends unchanged.
    public Rect CapturedViewport => capturedRegion.width>0f ? capturedRegion : new Rect(0f,0f,1f,1f);
    public Rect PlaybackUV
    {
        get
        {
            Rect r=CapturedViewport;
            return SystemInfo.graphicsUVStartsAtTop ? new Rect(r.x,1f-r.y,r.width,-r.height) : r;
        }
    }
    public float PlaybackAspect => CurrentFrame!=null ? CurrentFrame.width*CapturedViewport.width/(CurrentFrame.height*CapturedViewport.height) : 16f/9f;

    public static UXDeathReplay Create(UXStealthDirector owner, Transform parent)
    {
        var replay = parent.gameObject.AddComponent<UXDeathReplay>(); replay.director = owner;
        return replay;
    }
    private void Start() { StartCoroutine(CaptureLoop()); }
    public void FreezeAfter(float seconds)
    {
        if(freezeAt>=0f)return;
        DeathLocalTime=DateTime.Now;DeathMissionSeconds=UXCampaign.RunSeconds;
        freezeAt=Time.unscaledTime+Mathf.Max(0f,seconds);
    }
    public void SkipAuto() { autoSkipped = true; loopAt = Time.unscaledTime; }
    public void WatchAgain() { SkipAuto(); }

    private IEnumerator CaptureLoop()
    {
        while (enabled)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Experimental: issue the offscreen replay render BEFORE Unity's
            // normal camera render, allowing it to restore the screen viewport.
            yield return null;
#else
            yield return endOfFrame;
#endif
            CaptureAtEndOfFrame();
            if (frozenAt >= 0f || unavailable) yield break;
        }
    }

    private void CaptureAtEndOfFrame()
    {
        if (unavailable || frozenAt >= 0f || director == null || !director.MissionStarted || GameManager.Instance == null) return;
        if(GameManager.Instance.GameEnded && GameManager.Instance.PlayerWon) { unavailable=true;ReleaseAll();count=0;return; }
        if (freezeAt >= 0f && Time.unscaledTime >= freezeAt)
        {
            frozenAt = Time.unscaledTime; loopAt = frozenAt;
            // The full-size staging texture is not needed while viewing a clip.
            Release(ref capture);
            return;
        }
        bool fatalReveal = freezeAt >= 0f && GameManager.Instance.GameEnded && !GameManager.Instance.PlayerWon;
        if ((!fatalReveal && (Time.timeScale <= 0f || GameManager.Instance.GameEnded)) || Time.unscaledTime < nextSample) return;
        nextSample+=SampleInterval;
        if(nextSample<=Time.unscaledTime)nextSample=Time.unscaledTime+SampleInterval;
        RenderTexture previous = RenderTexture.active;
        try
        {
            if (Screen.width < 1 || Screen.height < 1) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL capture path: render the live world camera off-screen.
            // Do not call ScreenCapture.CaptureScreenshotIntoRenderTexture,
            // which caused repeated full-canvas pulsing in the existing Web build.
            if (sourceView == null) sourceView = Camera.main;
            if (sourceView == null) return;
            Rect region = sourceView.rect;
            float left = Mathf.Clamp01(region.x), bottom = Mathf.Clamp01(region.y);
            region = new Rect(left, bottom,
                Mathf.Max(.01f, Mathf.Min(region.width, 1f - left)),
                Mathf.Max(.01f, Mathf.Min(region.height, 1f - bottom)));
            bool regionChanged = Mathf.Abs(region.x - capturedRegion.x) > .0001f ||
                Mathf.Abs(region.y - capturedRegion.y) > .0001f ||
                Mathf.Abs(region.width - capturedRegion.width) > .0001f ||
                Mathf.Abs(region.height - capturedRegion.height) > .0001f;
            if (regionChanged)
            {
                ReleaseAll(); count = 0; write = 0;
                capturedRegion = region;
            }
            if (frames[write] == null)
            {
                float nativeWidth = Screen.width, nativeHeight = Screen.height;
                float scale = Mathf.Min(1f,
                    Mathf.Min(FrameWidth / nativeWidth, FrameHeight / nativeHeight));
                int width = Mathf.Max(1, Mathf.RoundToInt(nativeWidth * scale));
                int height = Mathf.Max(1, Mathf.RoundToInt(nativeHeight * scale));
                frames[write] = MakeTexture(width, height, "WebGL_URP_Replay_Frame");
            }
            var request = new RenderPipeline.StandardRequest();
            if (!RenderPipeline.SupportsRenderRequest(sourceView, request))
                throw new InvalidOperationException("URP camera render requests are unsupported.");
            request.destination = frames[write];
            RenderPipeline.SubmitRenderRequest(sourceView, request);
#else
            if(sourceView==null)sourceView=Camera.main;

            Rect region=sourceView!=null?sourceView.rect:new Rect(0f,0f,1f,1f);
            float left=Mathf.Clamp01(region.x),bottom=Mathf.Clamp01(region.y);
            region=new Rect(left,bottom,Mathf.Max(.01f,Mathf.Min(region.width,1f-left)),Mathf.Max(.01f,Mathf.Min(region.height,1f-bottom)));
            bool regionChanged=Mathf.Abs(region.x-capturedRegion.x)>.0001f || Mathf.Abs(region.y-capturedRegion.y)>.0001f ||
                Mathf.Abs(region.width-capturedRegion.width)>.0001f || Mathf.Abs(region.height-capturedRegion.height)>.0001f;
            if (capture == null || capture.width != Screen.width || capture.height != Screen.height || !capture.IsCreated() || regionChanged)
            {
                // A live resize invalidates old aspect ratios; never mix them.
                ReleaseAll(); count = 0; write = 0;
                capture = MakeTexture(Screen.width, Screen.height, "Replay_Staging");
                capturedRegion=region;
            }
            if (frames[write] == null)
            {
                float nativeWidth=Screen.width,nativeHeight=Screen.height;
                float scale=Mathf.Min(1f,Mathf.Min(FrameWidth/nativeWidth,FrameHeight/nativeHeight));
                int width=Mathf.Max(1,Mathf.RoundToInt(nativeWidth*scale));
                int height=Mathf.Max(1,Mathf.RoundToInt(nativeHeight*scale));
                frames[write] = MakeTexture(width, height, "Death_Replay_Frame");
            }
            ScreenCapture.CaptureScreenshotIntoRenderTexture(capture);
            // Keep the tested capture path. Playback composes the camera crop
            // with its existing backend orientation correction in a single UV.
            Graphics.Blit(capture, frames[write]);
#endif
            frameTimes[write]=DateTime.Now;
            write = (write + 1) % Capacity; count = Mathf.Min(Capacity, count + 1);
        }
        catch (System.Exception error)
        {
            // Capture failure must never block death/Replay or keep allocating.
            unavailable = true; ReleaseAll(); count = 0;
            Debug.LogWarning("Death replay unavailable on this graphics device: " + error.Message);
        }
        finally { RenderTexture.active = previous; }
    }

    private static RenderTexture MakeTexture(int width, int height, string name)
    {
        var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        { name = name, antiAliasing = 1, useMipMap = false, autoGenerateMips = false,
            filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        if (!rt.Create()) { Destroy(rt); throw new System.InvalidOperationException("Replay texture allocation failed."); }
        return rt;
    }
    private static void Release(ref RenderTexture rt)
    {
        if (rt == null) return;
        rt.Release();
        if (Application.isPlaying) Destroy(rt); else DestroyImmediate(rt);
        rt = null;
    }
    private void ReleaseAll()
    {
        Release(ref capture);
        for (int i = 0; i < frames.Length; i++) Release(ref frames[i]);
    }
    private void OnDestroy() { ReleaseAll(); }
}
