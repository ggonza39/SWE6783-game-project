using UnityEngine;

// Authored security hardware for Crimson only. No solid colliders, scene rewrites,
// per-frame object searches or real-time clocks. Guards do not trigger their own traps.
[DefaultExecutionOrder(110)]
public sealed class UXCrimsonHazards : MonoBehaviour
{
    public const float TriggerRadius = .72f;
    public const float BlastRadius = 2.40f; // Includes body clearance; 25% smaller in both modes.
    public const int MinesPerSector = 8;
    public static readonly Vector2[] MinePositions = {
        new Vector2(-18,12), new Vector2(-25,12), new Vector2(-11,10), new Vector2(-23,-1),
        new Vector2(-20,4), new Vector2(-10.75f,-2.2f), new Vector2(-17.5f,-12.7f), new Vector2(-17,-7),
        new Vector2(-6,13), new Vector2(0,13), new Vector2(4,8.5f), new Vector2(-5.5f,6),
        new Vector2(2.5f,2.4f), new Vector2(-5,-5.5f), new Vector2(-2,-12), new Vector2(5,-11),
        new Vector2(18,13.2f), new Vector2(25,12), new Vector2(18,7.6f), new Vector2(12,4),
        new Vector2(24,3.5f), new Vector2(17.5f,-3.8f), new Vector2(12,-12.5f), new Vector2(24.5f,-7) };
    public static readonly Vector2[] LaserPositions = {
        new Vector2(-19,6), new Vector2(-15,-4), new Vector2(-4,-8), new Vector2(-4,4),
        new Vector2(18,6), new Vector2(24,-1.5f) };
    private static readonly Color Amber = new Color(1f,.72f,.12f);
    private static readonly Color Pink = new Color(1f,.13f,.38f);
    private static readonly Color Green = new Color(.25f,1f,.62f);
    private sealed class Mine
    {
        public UXMineState state;
        public SpriteRenderer lamp;
        public SpriteRenderer[] hardware, ring, sparks;
        public bool revealed, visible;
        public float burst;
    }
    private sealed class Laser
    {
        public UXLaserState state;
        public SpriteRenderer beam, core;
        public SpriteRenderer[] lamps;
    }
    private readonly Vector2[] minePositions=(Vector2[])MinePositions.Clone();
    private PlayerController playerController;
    public Vector2 MinePosition(int index)=>minePositions[index];
    public Vector2 LastExplosionAt { get; private set; }
    private readonly Mine[] mines = new Mine[MinePositions.Length];
    private readonly Laser[] lasers = new Laser[6];
    private readonly RaycastHit2D[] sight = new RaycastHit2D[16];
    private UXStealthDirector director;
    private Sprite pixel;
    private Vector2 previousPlayer;
    private bool started;
    private SpriteRenderer[] emp;
    private Vector2 empAt;
    private float empRemaining;
    public string Warning { get; private set; }
    public float ImpactFlash { get; private set; }
    public UXMineState MineState(int index) => mines[index].state;
    public UXLaserState LaserState(int index) => lasers[index].state;

    public static UXCrimsonHazards Create(UXStealthDirector owner)
    {
        var root = new GameObject("Crimson_Security_Hazards"); root.transform.SetParent(owner.transform, false);
        var system = root.AddComponent<UXCrimsonHazards>(); system.director = owner;
        system.playerController=owner.Player!=null?owner.Player.GetComponent<PlayerController>():null;
        system.pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0,0,1,1), new Vector2(.5f,.5f), 1f);
        system.BuildArt(); return system;
    }

    public void Begin(UXDifficulty mode, int checkpoint)
    {
        bool hard = mode == UXDifficulty.Hard;
        ShuffleMines(UXCampaign.MineSeed);
        Warning=null;ImpactFlash=0f;empRemaining=0f;
        for (int i=0;i<mines.Length;i++) { mines[i].burst=0f;mines[i].revealed=false;mines[i].state = new UXMineState(hard); if(i/MinesPerSector<checkpoint)mines[i].state.Neutralize(); }
        for (int i=0;i<lasers.Length;i++) { lasers[i].state = new UXLaserState(hard,(i%2)*.65f); if(i/2<checkpoint)lasers[i].state.Neutralize(); }
        previousPlayer = director.Player.position; started = true; RefreshArt(0f);
    }

    public void SecureSector(int sector, Vector2 console)
    {
        for(int i=sector*MinesPerSector;i<(sector+1)*MinesPerSector;i++)mines[i].state.Neutralize();
        for(int i=sector*2;i<sector*2+2;i++)lasers[i].state.Neutralize();
        empAt=console;empRemaining=.80f;RefreshArt(0f);
    }

    private void Update()
    {
        if(!started || !director.MissionStarted || Time.timeScale<=0f || director.Player==null ||
            GameManager.Instance==null || GameManager.Instance.GameEnded)return;
        Step(Time.deltaTime);
    }

    private void Step(float dt)
    {
        if(dt<=0f)return;
        Vector2 player = director.Player.position; bool hidden = director.PlayerHidden;
        Warning = null; ImpactFlash = Mathf.Max(0f,ImpactFlash-dt*2f);
        empRemaining=Mathf.Max(0f,empRemaining-dt);
        for(int i=0;i<mines.Length;i++)
        {
            Mine mine=mines[i];Vector2 at=minePositions[i];bool wasArmed=mine.state.Armed;
            bool trigger=!mine.state.Spent && !mine.state.Neutralized && !mine.state.Armed && !hidden && SegmentDistance(previousPlayer,player,at)<=TriggerRadius && Visible(at,player);
            bool explode=mine.state.Tick(dt,trigger);
            if(!wasArmed && mine.state.Armed){mine.revealed=true;UXGameAudio.At(UXAudioCue.MineArmed,at);UXCampaign.RecordHazard("MineArmed_"+i);}
            if(explode)
            {
                UXGameAudio.MineExploded(at);
                mine.burst=.55f;director.InvestigateNoise(at);
                bool exposed=!hidden && Vector2.Distance(player,at)<=BlastRadius && Visible(at,player);
                LastExplosionAt=at;
                bool hit=exposed && director.HazardHit(director.Difficulty.Mode==UXDifficulty.Hard?85:60,"PROXIMITY MINE");
                if(hit && director.State.Alive && playerController!=null)playerController.ApplyMineKnockback(at);
                UXCampaign.RecordHazard(hit?"MineHit_"+i:exposed?"MineProtected_"+i:"MineEscaped_"+i);
                if(Vector2.Distance(player,at)<4f)ImpactFlash=1f;
            }
            if(mine.state.Armed && Vector2.Distance(player,at)<4f)
                Warning="MINE ARMED  /  "+(UXCampaign.SprintRemaining>0f?"SPRINT CLEAR!  ":"BACK AWAY / FIND COVER!  ")+mine.state.Remaining.ToString("0.0")+"s";
            mine.burst=Mathf.Max(0f,mine.burst-dt);
        }
        for(int i=0;i<lasers.Length;i++)
        {
            Laser laser=lasers[i];UXLaserPhase before=laser.state.Phase;laser.state.Tick(dt);Vector2 at=LaserPositions[i];
            if(laser.state.Phase!=before)
            {
                if(laser.state.Phase==UXLaserPhase.Warning)UXGameAudio.At(UXAudioCue.LaserCharge,at);
                else if(laser.state.Phase==UXLaserPhase.Active)UXGameAudio.At(UXAudioCue.LaserOn,at);
            }
            bool crosses=UXLaserState.Crosses(previousPlayer.x,previousPlayer.y,player.x,player.y,at.x,at.y,3.5f,.90f);
            if(laser.state.Phase==UXLaserPhase.Active && crosses && !hidden && Visible(at,player) &&
                director.HazardHit(100,"LASER BARRIER",true))
            {ImpactFlash=.6f;UXCampaign.RecordHazard("LaserHit_"+i);}
            if(Warning==null && !laser.state.Neutralized && Mathf.Abs(player.x-at.x)<2.4f && Mathf.Abs(player.y-at.y)<1.3f)
                Warning=laser.state.Phase==UXLaserPhase.Active?"LASER ACTIVE  /  LETHAL CONTACT":
                    laser.state.Phase==UXLaserPhase.Warning?"LASER CHARGING  /  STAND BACK":"LASER GAP  /  MOVE THROUGH";
        }
        previousPlayer=player;RefreshArt(dt);
    }

    private static float Next(ref uint state)
    {
        state^=state<<13;state^=state>>17;state^=state<<5;
        return (state&0x00ffffffu)/16777216f;
    }
    private bool SafeMine(Vector2 at,int index,Vector2[] chosen)
    {
        int sector=index/MinesPerSector;
        float left=sector==0?-25.8f:sector==1?-7.8f:10.2f;
        float right=sector==0?-10.2f:sector==1?7.8f:25.8f;
        if(at.x<left || at.x>right || Mathf.Abs(at.y)>13.8f)return false;
        for(int i=0;i<9;i++)
        {
            var cache=director.Sectors.Cache(i);
            if(cache!=null && Vector2.Distance(at,cache.transform.position)<2.75f)return false;
        }
        foreach(var console in director.Sectors.Consoles)
            if(console!=null && Vector2.Distance(at,console.transform.position)<2.75f)return false;
        foreach(var refuge in director.Sectors.Checkpoints)
            if(Vector2.Distance(at,refuge)<4.3f)return false;
        if(Vector2.Distance(at,new Vector2(24,-10))<2.75f ||
            Vector2.Distance(at,new Vector2(24,-14))<2.75f)return false;
        foreach(var laser in LaserPositions)
            if(Mathf.Abs(at.x-laser.x)<2.4f && Mathf.Abs(at.y-laser.y)<2.2f)return false;
        for(int i=0;i<index;i++)if(Vector2.Distance(at,chosen[i])<2.85f)return false;
        int escapes=0;
        for(int i=0;i<8;i++)
        {
            float angle=i*Mathf.PI*.25f;
            Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            if(!director.ClearNavigationSegment(at,at+direction*.85f))return false;
            if(director.ClearNavigationSegment(at,at+direction*3.25f))escapes++;
        }
        return escapes>=3;
    }
    private void ShuffleMines(int seed)
    {
        uint random=(uint)Mathf.Max(1,seed);
        var chosen=new Vector2[mines.Length];bool complete=false;
        for(int layout=0;layout<8 && !complete;layout++)
        {
            complete=true;
            for(int i=0;i<chosen.Length;i++)
            {
                int sector=i/MinesPerSector;
                float left=sector==0?-25.8f:sector==1?-7.8f:10.2f;
                float right=sector==0?-10.2f:sector==1?7.8f:25.8f;
                bool found=false;
                for(int attempt=0;attempt<2048;attempt++)
                {
                    Vector2 candidate=attempt<128 ? MinePositions[i]+new Vector2(
                        (Next(ref random)*2f-1f)*3f,(Next(ref random)*2f-1f)*3f) :
                        new Vector2(left+Next(ref random)*(right-left),-13.8f+Next(ref random)*27.6f);
                    if(Vector2.Distance(candidate,MinePositions[i])<.9f || !SafeMine(candidate,i,chosen))continue;
                    chosen[i]=candidate;found=true;break;
                }
                if(!found){complete=false;break;}
            }
        }
        for(int i=0;i<chosen.Length;i++)MoveMine(i,complete?chosen[i]:MinePositions[i]);
    }
    private void MoveMine(int index,Vector2 at)
    {
        Vector3 delta=(Vector3)(at-minePositions[index]);Mine mine=mines[index];
        foreach(var part in mine.hardware)part.transform.position+=delta;
        foreach(var part in mine.ring)part.transform.position+=delta;
        foreach(var part in mine.sparks)part.transform.position+=delta;
        minePositions[index]=at;
    }

    private bool Visible(Vector2 from, Vector2 to) => DetectionUtility.HasLineOfSight(from,to,transform,sight,director.Player);
    private static float SegmentDistance(Vector2 a,Vector2 b,Vector2 p)
    {
        Vector2 d=b-a;float t=d.sqrMagnitude<.00001f?0f:Mathf.Clamp01(((p.x-a.x)*d.x+(p.y-a.y)*d.y)/d.sqrMagnitude);
        return Vector2.Distance(p,a+d*t);
    }

    private SpriteRenderer Part(string name,Vector2 at,Vector2 size,Color color,int order,float angle=0f)
    {
        var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.position=at;
        go.transform.localScale=new Vector3(size.x,size.y,1);go.transform.rotation=Quaternion.Euler(0,0,angle);
        var art=go.AddComponent<SpriteRenderer>();art.sprite=pixel;art.color=color;art.sortingOrder=order;return art;
    }
    private SpriteRenderer[] Ring(string name,Vector2 at,float radius,int count,Color color,int order)
    {
        var ring=new SpriteRenderer[count];
        for(int i=0;i<count;i++)
        {
            float angle=i*360f/count,a=angle*Mathf.Deg2Rad;
            ring[i]=Part(name,at+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,
                new Vector2(radius*5.1f/count,.045f),color,order,angle+90f);ring[i].enabled=false;
        }
        return ring;
    }
    private void BuildArt()
    {
        for(int i=0;i<mines.Length;i++)
        {
            Vector2 at=minePositions[i];var mine=new Mine();mines[i]=mine;
            mine.hardware=new SpriteRenderer[] {
                Part("Mine_Casing",at,new Vector2(.58f,.58f),new Color(.07f,.10f,.14f),4,45),
                Part("Mine_Caution",at,new Vector2(.45f,.45f),Amber,5,45),
                Part("Mine_Inner",at,new Vector2(.36f,.36f),new Color(.12f,.15f,.18f),6,45),
                Part("Mine_X",at,new Vector2(.35f,.06f),new Color(.65f,.78f,.85f),7,45),
                Part("Mine_X",at,new Vector2(.35f,.06f),new Color(.65f,.78f,.85f),7,-45),
                Part("Mine_Core",at,new Vector2(.12f,.12f),Amber,8) };
            mine.lamp=mine.hardware[5];
            foreach(SpriteRenderer part in mine.hardware)part.enabled=false;
            mine.ring=Ring("Mine_Blast_Warning",at,BlastRadius,20,Amber,21);
            mine.sparks=Ring("Mine_Burst",at,.4f,8,new Color(1f,.85f,.3f),24);
        }
        for(int i=0;i<lasers.Length;i++)
        {
            Vector2 at=LaserPositions[i];var laser=new Laser();lasers[i]=laser;laser.lamps=new SpriteRenderer[2];
            Part("Laser_Floor_Track",at,new Vector2(3f,.24f),new Color(.06f,.12f,.17f),2);
            for(int j=0;j<8;j++)Part("Laser_Caution_Stripe",at+new Vector2(-1.3f+j*.37f,0),new Vector2(.12f,.24f),new Color(.45f,.29f,.12f),3,25);
            for(int side=0;side<2;side++)
            {
                Vector2 end=at+new Vector2(side==0?-1.52f:1.52f,0);
                Part("Laser_Emitter",end,new Vector2(.21f,.50f),new Color(.10f,.18f,.25f),8);
                laser.lamps[side]=Part("Laser_Emitter_Lamp",end,new Vector2(.09f,.30f),Amber,9);
            }
            laser.beam=Part("Laser_Beam_Glow",at,new Vector2(3f,.17f),Pink,22);
            laser.core=Part("Laser_Beam_Core",at,new Vector2(3f,.045f),new Color(1f,.9f,.95f),23);
        }
        emp=Ring("Security_EMP_Pulse",Vector2.zero,1f,24,new Color(.2f,1f,.8f),25);
        // Safe dormant presentation before the player deploys.
        foreach(Laser laser in lasers){laser.beam.enabled=false;laser.core.enabled=false;}
    }

    private void RefreshArt(float dt)
    {
        for(int i=0;i<mines.Length;i++)
        {
            Mine mine=mines[i];if(mine.state==null)continue;
            if(mine.visible!=mine.revealed)
            {
                foreach(SpriteRenderer part in mine.hardware)part.enabled=mine.revealed;
                if(!mine.revealed)
                {
                    foreach(SpriteRenderer part in mine.ring)part.enabled=false;
                    foreach(SpriteRenderer part in mine.sparks)part.enabled=false;
                }
                mine.visible=mine.revealed;
            }
            if(!mine.revealed)continue;
            bool armed=mine.state.Armed;float pulse=.6f+.4f*Mathf.Sin(Time.time*(armed?26f:3f));
            mine.lamp.color=mine.state.Neutralized?Green:mine.state.Spent?new Color(.12f,.15f,.17f):
                armed?new Color(1f,.12f+pulse*.28f,.12f):new Color(1f,.5f+pulse*.25f,.08f);
            for(int j=0;j<mine.ring.Length;j++)
            {
                mine.ring[j].enabled=armed;
                mine.ring[j].color=j/(float)mine.ring.Length<=mine.state.Fraction?Pink:Amber;
            }
            for(int j=0;j<mine.sparks.Length;j++)
            {
                var spark=mine.sparks[j];spark.enabled=mine.burst>0f;
                if(!spark.enabled)continue;
                float angle=j*360f/mine.sparks.Length,a=angle*Mathf.Deg2Rad,r=(1f-mine.burst/.55f)*BlastRadius;
                spark.transform.position=minePositions[i]+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
                spark.transform.localScale=new Vector3(.16f+mine.burst*.5f,.055f,1);
                spark.color=new Color(1f,.35f+mine.burst,.12f,mine.burst/.55f);
            }
        }
        foreach(Laser laser in lasers)
        {
            if(laser.state==null)continue;bool active=laser.state.Phase==UXLaserPhase.Active;
            bool warning=laser.state.Phase==UXLaserPhase.Warning;
            laser.beam.enabled=active||warning;laser.core.enabled=active;
            laser.beam.color=active?Pink:new Color(1f,.75f,.1f,.20f+.35f*(.5f+.5f*Mathf.Sin(Time.time*18f)));
            foreach(SpriteRenderer lamp in laser.lamps)lamp.color=laser.state.Neutralized?Green:active?Pink:warning?Amber:new Color(.15f,.7f,.8f);
        }
        for(int i=0;i<emp.Length;i++)
        {
            emp[i].enabled=empRemaining>0f;if(!emp[i].enabled)continue;
            float a=i*360f/emp.Length*Mathf.Deg2Rad,r=(1f-empRemaining/.8f)*4f;
            emp[i].transform.position=empAt+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
            emp[i].transform.localScale=new Vector3(.12f+r*.20f,.045f,1);
            emp[i].color=new Color(.2f,1f,.8f,empRemaining/.8f);
        }
    }
    private void OnDestroy(){if(pixel!=null)Destroy(pixel);}
}
