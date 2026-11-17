using UnityEngine;

// Presentation only. No colliders, damage, new objectives, random failure rolls,
// per-frame scene searches or physics changes. Death feedback uses unscaled time
// after gameplay has ended; environmental motion follows the paused game clock.
[DefaultExecutionOrder(350)]
public sealed class UXMissionEffects : MonoBehaviour
{
    public const float FatalRevealSeconds = 1.05f;
    private UXStealthDirector director;
    private Sprite pixel;
    private SpriteRenderer[] lamps, wisps, lift, chevrons, arcs, sparks, smoke, body, bloodPool, bloodDrops, scorch, embers;
    private SpriteRenderer[] fireball,fragments,blastStains;
    private Vector2 blastDirection;
    private readonly float[] fireWidths = { .50f,1f,1.50f,1.75f,1.85f,1.75f,1.50f,1f,.50f };
    private readonly float[] bloodWidths = { .55f,1.05f,1.35f,1.55f,1.40f,1.05f,.60f };
    private readonly float[] bloodOffsets = { -.12f,.04f,-.06f,.02f,-.04f,.08f,-.02f };
    private Color[] bodyColors;
    private Vector3[] wispOrigins;
    private Vector2 deathAt;
    private float fatalAt = -1f;
    private string cause;
    private bool builtHard;
    private bool fatalSettled;
    public UXDeathReplay Replay { get; private set; }
    private Camera impactCamera;
    private Vector3 cameraAt;
    public float ImpactPulse => fatalAt < 0f ? 0f : Mathf.Clamp01(1f - (Time.unscaledTime - fatalAt) /
        (cause=="GUARD FIRE"?.08f:cause=="LASER BARRIER"?.14f:.24f))*(cause=="GUARD FIRE"?.25f:1f);
    public bool DeathRevealing => fatalAt >= 0f && Time.unscaledTime - fatalAt < FatalRevealSeconds;
    public bool HardAtmosphere => builtHard;

    public static UXMissionEffects Create(UXStealthDirector owner)
    {
        var root = new GameObject("Mission_Presentation");
        root.transform.SetParent(owner.transform, false);
        var effects = root.AddComponent<UXMissionEffects>();
        effects.director = owner;
        effects.Replay = UXDeathReplay.Create(owner, root.transform);
        effects.pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
        return effects;
    }

    public void Begin()
    {
        if (!director.IsCrimson || director.Difficulty.Mode != UXDifficulty.Hard || builtHard) return;
        builtHard = true;
        BuildIndustrialAtmosphere();
        AnimateAtmosphere();
    }

    public void ShowFatalHit(string source)
    {
        if (fatalAt >= 0f || director.Player == null) return;
        fatalAt = Time.unscaledTime;
        Replay.FreezeAfter(FatalRevealSeconds);
        impactCamera=Camera.main;
        if(impactCamera!=null)cameraAt=impactCamera.transform.position;
        cause = source;
        deathAt = director.Player.position;
        body = director.Player.GetComponentsInChildren<SpriteRenderer>();
        bodyColors = new Color[body.Length];
        for (int i = 0; i < body.Length; i++) bodyColors[i] = body[i].color;
        bool gun=source=="GUARD FIRE",mine=source=="PROXIMITY MINE",electric=source=="LASER BARRIER";
        arcs = new SpriteRenderer[electric?10:0];
        sparks = new SpriteRenderer[mine?20:electric?12:0];
        smoke = new SpriteRenderer[mine?8:electric?5:0];
        bloodPool = new SpriteRenderer[gun?7:0];bloodDrops = new SpriteRenderer[gun?6:0];
        scorch = new SpriteRenderer[electric?3:0];embers = new SpriteRenderer[electric?4:0];
        for (int i = 0; i < arcs.Length; i++)
            arcs[i] = Part("Fatal_Electric_Arc", deathAt, new Vector2(.20f, .045f), Color.white, 33);
        for (int i = 0; i < sparks.Length; i++)
            sparks[i] = Part(mine?"Fatal_Mine_Shrapnel":"Fatal_Laser_Spark", deathAt, new Vector2(.10f, .035f), Color.white, 34);
        for (int i = 0; i < smoke.Length; i++)
            smoke[i] = Part(mine?"Fatal_Mine_Smoke":"Fatal_Laser_Smoke", deathAt, new Vector2(.15f, .15f), Color.white, 32);
        fireball=new SpriteRenderer[mine?9:0];
        for(int i=0;i<fireball.Length;i++)
            fireball[i]=Part("Fatal_Mine_Fireball",deathAt,new Vector2(fireWidths[i],.20f),Color.white,32);
        // Blood lies on the floor beneath the fallen character, walls and crates.
        // Irregular pixel rows make a pooled stain rather than an explosive burst.
        for(int i=0;i<bloodPool.Length;i++)
            bloodPool[i]=Part("Fatal_Gun_Blood_Pool",deathAt,new Vector2(bloodWidths[i],.11f),new Color(.45f,.015f,.035f),4);
        for(int i=0;i<bloodDrops.Length;i++)
            bloodDrops[i]=Part("Fatal_Gun_Blood_Drop",deathAt,new Vector2(.045f,.055f),new Color(.68f,.025f,.04f),25);
        for(int i=0;i<scorch.Length;i++)
            scorch[i]=Part("Fatal_Laser_Scorch",deathAt+new Vector2((i-1)*.25f,-.18f),new Vector2(.62f,.30f),new Color(.025f,.025f,.03f,.8f),4);
        for(int i=0;i<embers.Length;i++)
            embers[i]=Part("Fatal_Laser_Ember",deathAt+new Vector2((i-1.5f)*.12f,-.08f),new Vector2(.035f,.035f),Color.white,34);
        fragments=new SpriteRenderer[mine?12:0];blastStains=new SpriteRenderer[mine?12:0];
        Vector2 away=mine && director.Hazards!=null?deathAt-director.Hazards.LastExplosionAt:new Vector2(0,-1);
        blastDirection=away.sqrMagnitude>.0001f?away.normalized:new Vector2(0,-1);
        for(int i=0;i<fragments.Length;i++)
        {
            bool armor=i%3!=0;
            fragments[i]=Part("Fatal_Mine_Body_Fragment",deathAt,new Vector2(armor?.15f:.10f,armor?.10f:.12f),
                armor?new Color(.10f,.30f,.38f):new Color(.58f,.015f,.035f),27);
            blastStains[i]=Part("Fatal_Mine_Splatter",deathAt,new Vector2(.16f,.10f),new Color(.36f,.008f,.025f,0f),4);
        }
        if(mine)foreach(var part in body)if(part!=null)part.enabled=false;
        AnimateFatal();
    }

    private SpriteRenderer Part(string name, Vector2 at, Vector2 size, Color color, int order, float angle = 0f)
    {
        var part = new GameObject(name);
        part.transform.SetParent(transform, false);
        part.transform.position = at;
        part.transform.localScale = new Vector3(size.x, size.y, 1f);
        part.transform.rotation = Quaternion.Euler(0, 0, angle);
        var art = part.AddComponent<SpriteRenderer>();
        art.sprite = pixel; art.color = color; art.sortingOrder = order;
        return art;
    }

    private void BuildIndustrialAtmosphere()
    {
        // Existing wall-mounted machinery vents: vapor is faint and drawn below
        // actors/cones, so it never masquerades as a hazard or hides a guard.
        Vector2[] vents = { new Vector2(-26f, 5.5f), new Vector2(-8f, -5f), new Vector2(26f, 8f) };
        lamps = new SpriteRenderer[6]; wisps = new SpriteRenderer[15]; wispOrigins = new Vector3[15];
        for (int v = 0; v < vents.Length; v++)
        {
            Vector2 at = vents[v];
            Part("Wall_Vent_Housing", at, new Vector2(.42f, .72f), new Color(.10f, .14f, .18f), 6);
            for (int g = 0; g < 4; g++)
                Part("Wall_Vent_Grille", at + new Vector2(0, -.22f + g * .14f),
                    new Vector2(.34f, .04f), new Color(.40f, .44f, .48f), 7);
            for (int side = 0; side < 2; side++)
                lamps[v * 2 + side] = Part("Industrial_Status_Lamp", at + new Vector2(0, side == 0 ? -.47f : .47f),
                    new Vector2(.25f, .07f), Color.white, 9);
            for (int w = 0; w < 5; w++)
            {
                int i = v * 5 + w;
                wispOrigins[i] = at + new Vector2(v == 2 ? -.20f : .20f, -.2f);
                wisps[i] = Part("Vent_Vapor", wispOrigins[i], new Vector2(.24f, .1f), Color.white, 3);
            }
        }

        // Extraction lift: metal rails, caution paint, animated perimeter lamps
        // and inward floor guides. All stay inside the existing lift footprint.
        Vector2 liftAt = new Vector2(24f, -13.5f);
        lift = new SpriteRenderer[12]; chevrons = new SpriteRenderer[10];
        for (int side = 0; side < 2; side++)
        {
            float x = side == 0 ? 22.7f : 25.3f;
            Part("Lift_Rail", new Vector2(x, -13.5f), new Vector2(.13f, 2.6f), new Color(.40f, .46f, .52f), 6);
            for (int l = 0; l < 6; l++)
                lift[side * 6 + l] = Part("Lift_Perimeter_Lamp", new Vector2(x, -12.35f - l * .44f),
                    new Vector2(.22f, .20f), Color.white, 10);
        }
        for (int c = 0; c < 5; c++)
        {
            Vector2 at = liftAt + new Vector2(0, .85f - c * .42f);
            chevrons[c * 2] = Part("Lift_Inward_Guide", at + new Vector2(-.13f, 0), new Vector2(.34f, .055f), Color.white, 5, -35f);
            chevrons[c * 2 + 1] = Part("Lift_Inward_Guide", at + new Vector2(.13f, 0), new Vector2(.34f, .055f), Color.white, 5, 35f);
        }
        for (int i = 0; i < 9; i++)
            Part("Lift_Caution_Paint", new Vector2(22.85f + i * .28f, -12f), new Vector2(.14f, .16f),
                new Color(.72f, .45f, .10f, .8f), 5, 25f);
    }

    private void LateUpdate()
    {
        if (fatalAt >= 0f)
        {
            if (!fatalSettled) { AnimateFatal(); fatalSettled = Time.unscaledTime - fatalAt >= 1.5f; }
            return;
        }
        if (builtHard && director.MissionStarted && Time.timeScale > 0f &&
            GameManager.Instance != null && !GameManager.Instance.GameEnded) AnimateAtmosphere();
    }

    private void AnimateAtmosphere()
    {
        float t = Time.time;
        for (int i = 0; i < lamps.Length; i++)
        {
            float pulse = .55f + .20f * Mathf.Sin(t * 2f + i * .8f);
            lamps[i].color = new Color(1f, .32f + pulse * .35f, .12f, pulse);
        }
        for (int i = 0; i < wisps.Length; i++)
        {
            float phase = Mathf.Repeat(t * .3f + i * .21f, 1f);
            float outward = i / 5 == 2 ? -1f : 1f;
            wisps[i].transform.position = wispOrigins[i] + new Vector3(outward * phase * .65f, phase * .8f, 0f);
            wisps[i].transform.localScale = new Vector3(.20f + phase * .38f, .08f + phase * .12f, 1f);
            wisps[i].color = new Color(.62f, .72f, .77f, Mathf.Sin(phase * Mathf.PI) * .13f);
        }
        bool secured = director.Sectors.SecuredSectors >= 3;
        bool open = secured && director.ExtractionLocksReleased;
        for (int i = 0; i < lift.Length; i++)
        {
            float light = .45f + .5f * Mathf.Max(0f, Mathf.Sin(t * 3f - (i % 6) * .7f));
            lift[i].color = open ? new Color(.24f, 1f, .60f, light) :
                secured ? new Color(1f, .70f, .16f, light) : new Color(.45f, .18f, .15f, .6f);
        }
        for (int i = 0; i < chevrons.Length; i++)
        {
            float a = open ? .28f + .45f * Mathf.Max(0f, Mathf.Sin(t * 3f + (i / 2) * .8f)) : .12f;
            chevrons[i].color = new Color(.30f, 1f, .68f, a);
        }
    }

    private void AnimateFatal()
    {
        float t = Mathf.Max(0f, Time.unscaledTime - fatalAt);
        bool electric = cause == "LASER BARRIER", gun = cause == "GUARD FIRE", mine = cause == "PROXIMITY MINE";
        float hot = Mathf.Clamp01(1f - t / .65f);
        if(impactCamera!=null && GameManager.Instance!=null && GameManager.Instance.GameEnded)
        {
            float duration=mine?.35f:electric?.22f:.10f;
            float shake=Mathf.Clamp01(1f-t/duration)*(mine?.18f:electric?.055f:.025f);
            impactCamera.transform.position=cameraAt+new Vector3(Mathf.Sin(t*53f)*shake,Mathf.Sin(t*41f)*shake,0f);
        }
        for (int i = 0; i < body.Length; i++)
        {
            if(body[i]!=null && mine){body[i].enabled=false;continue;}
            if (body[i] == null || !body[i].enabled) continue;
            if (electric)
            {
                float white = hot * (.65f + .35f * Mathf.Sin(t * 28f));
                body[i].color = new Color(.035f + white*.965f,.04f + white*.96f,.045f + white*.955f,bodyColors[i].a);
            }
            else if(gun)
                body[i].color = new Color(bodyColors[i].r*.72f,bodyColors[i].g*.72f,bodyColors[i].b*.72f,bodyColors[i].a);
            else if(mine)
                body[i].color = new Color(.19f+.65f*hot,.12f+.30f*hot,.08f+.10f*hot,bodyColors[i].a);
            if (body[i].transform != director.Player)
                body[i].transform.rotation = Quaternion.Euler(0,0,-(electric?65f:80f)*Mathf.Clamp01(t/(gun?.35f:.65f)));
        }
        for (int i = 0; i < arcs.Length; i++)
        {
            arcs[i].enabled = t < .72f;
            float a = i * .63f + Mathf.FloorToInt(t * 18f) * .8f;
            arcs[i].transform.position = deathAt + new Vector2(Mathf.Cos(a) * .42f, Mathf.Sin(a) * .57f);
            arcs[i].transform.rotation = Quaternion.Euler(0,0,a*Mathf.Rad2Deg+40f);
            arcs[i].color = new Color(.65f,.95f,1f,hot);
        }
        for(int i=0;i<fireball.Length;i++)
        {
            float growth=.25f+t*3.8f,core=1f-Mathf.Abs(i-4f)/4f;
            fireball[i].enabled=t<.50f;
            fireball[i].transform.position=deathAt+new Vector2(0f,(i-4)*.18f*growth);
            fireball[i].transform.localScale=new Vector3(fireWidths[i]*growth,.20f*growth,1f);
            fireball[i].color=new Color(1f,Mathf.Max(.20f,.55f+core*.45f-t*1.2f),.06f+core*.28f,Mathf.Clamp01(1f-t/.50f));
        }
        for (int i = 0; i < sparks.Length; i++)
        {
            float a=i*2.39996f,speed=mine?3.7f:1.2f;
            sparks[i].enabled=t<(mine?.9f:.7f);
            sparks[i].transform.position=deathAt+new Vector2(Mathf.Cos(a)*t*speed,Mathf.Sin(a)*t*speed+t*.25f);
            sparks[i].transform.rotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);
            sparks[i].color=electric?new Color(.55f,.95f,1f,hot):new Color(.55f+.45f*hot,.27f+.25f*hot,.10f,hot);
        }
        for (int i = 0; i < smoke.Length; i++)
        {
            smoke[i].enabled=t<1.5f;
            smoke[i].transform.position=deathAt+new Vector2((i-(smoke.Length-1)*.5f)*.10f,t*.8f+i*.06f);
            smoke[i].transform.localScale=new Vector3((electric?.07f:.15f)+t*.18f,.12f+t*.18f,1f);
            smoke[i].color=new Color(.28f,.30f,.32f,Mathf.Clamp01(t*3f)*Mathf.Clamp01(1.5f-t)*.6f);
        }
        for(int i=0;i<fragments.Length;i++)
        {
            float angle=i*2.39996f,travel=Mathf.Clamp01(t/.85f);
            travel=1f-(1f-travel)*(1f-travel);
            Vector2 offset=(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(.45f+(i%4)*.25f)+blastDirection*.65f)*travel;
            Vector2 at=director.ClampBlastTravel(deathAt,offset);
            fragments[i].transform.position=at;
            fragments[i].transform.rotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg+Mathf.Min(t,.9f)*(i%2==0?310f:-280f));
            blastStains[i].transform.position=at;
            blastStains[i].color=new Color(.36f,.008f,.025f,Mathf.Clamp01((t-.12f)*3f)*.85f);
        }
        float poolGrowth=.16f+.84f*Mathf.Clamp01(t/.85f);
        for(int i=0;i<bloodPool.Length;i++)
        {
            bloodPool[i].transform.position=deathAt+new Vector2(.10f+bloodOffsets[i]*poolGrowth,-.18f+(i-3)*.105f*poolGrowth);
            bloodPool[i].transform.localScale=new Vector3(bloodWidths[i]*poolGrowth,.11f*poolGrowth,1f);
            bloodPool[i].color=new Color(i==2||i==3?.48f:.34f,.012f,.028f,.92f);
        }
        for(int i=0;i<bloodDrops.Length;i++)
        {
            float a=i*2.39996f;
            bloodDrops[i].enabled=t<.4f;
            bloodDrops[i].transform.position=deathAt+new Vector2(Mathf.Cos(a)*t*.7f,.18f+Mathf.Sin(a)*t*.35f-t*.8f);
            bloodDrops[i].color=new Color(.65f,.015f,.035f,Mathf.Clamp01(1f-t/.4f));
        }
        for(int i=0;i<scorch.Length;i++)
            scorch[i].color=new Color(.025f,.025f,.03f,Mathf.Clamp01(t*3f)*.85f);
        for(int i=0;i<embers.Length;i++)
        {
            embers[i].enabled=t<1.35f;
            embers[i].color=new Color(.85f,.24f,.045f,Mathf.Clamp01(1.35f-t)*(.55f+.2f*Mathf.Sin(t*7f+i)));
        }
    }

    private void OnDestroy()
    {
        if (pixel == null) return;
        if (Application.isPlaying) Destroy(pixel); else DestroyImmediate(pixel);
    }
}
