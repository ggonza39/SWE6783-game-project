using UnityEngine;

public enum UXAudioCue
{
    UI, Deploy, Data, Keycard, Door, Security, Checkpoint, Shot, Hit,
    MineArmed, MineBlast, LaserCharge, LaserOn, LaserBurn, GuardDeath,
    MineDeath, Failed, LevelClear, Victory, Step, SprintStep, Hide, Reveal,
    SprintEmpty, Suspicion, Alert
}

// Scene-local, bounded audio presentation. It never changes detection or damage.
// Imported clips also work with WebGL's basic AudioSource implementation.
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class UXGameAudio : MonoBehaviour
{
    private const int VoiceLimit = 10;
    private const float EffectsCeiling = .62f, MusicCeiling = .30f;
    public static UXGameAudio Instance { get; private set; }
    private static readonly string[] ClipNames = {
        "ui", "deploy", "data", "keycard", "door", "security", "checkpoint", "shot", "hit",
        "mine_armed", "mine_blast", "laser_charge", "laser_on", "laser_burn", "guard_death",
        "mine_death", "failed", "level_clear", "victory", "step", "sprint_step", "hide", "reveal",
        "sprint_empty", "suspicion", "alert" };
    private sealed class Voice
    {
        public AudioSource source;
        public UXAudioCue cue;
        public Vector2 position;
        public float gain, until;
        public int priority;
        public bool world;
    }
    private readonly Voice[] voices = new Voice[VoiceLimit];
    private readonly AudioClip[] clips = new AudioClip[ClipNames.Length];
    private readonly float[] lastCue = new float[ClipNames.Length];
    private AudioClip alternateShot, alternateStep;
    private AudioSource music, interaction, alarm;
    private AudioListener fallbackListener;
    private UXStealthDirector director;
    private PlayerController controller;
    private Vector2 previousPosition;
    private UXAlertPhase previousPhase;
    private float nextStep, listenerCheck, duckUntil, lastSprint;
    private bool ready, initialized, wasPaused, wasHidden, mapHeld;
    private int shotNumber, stepNumber;
    public int MissingClipCount { get; private set; }
    public int ActiveVoiceCount
    {
        get { int n=0; for(int i=0;i<voices.Length;i++)if(voices[i]!=null && voices[i].until>Time.unscaledTime)n++; return n; }
    }

    public static void Attach(UXStealthDirector owner)
    {
        if(owner==null || !Application.isPlaying || !UXCampaign.IsCampaignScene)return;
        UXGameAudio audio=owner.GetComponent<UXGameAudio>();
        if(audio==null)audio=owner.gameObject.AddComponent<UXGameAudio>();
        audio.Initialize(owner);
    }

    private void Initialize(UXStealthDirector owner)
    {
        if(initialized)return;
        initialized=true; Instance=this; director=owner;
        controller=owner.Player!=null?owner.Player.GetComponent<PlayerController>():null;
        for(int i=0;i<clips.Length;i++)
        {
            clips[i]=Load(ClipNames[i]); lastCue[i]=-100f;
        }
        alternateShot=Load("shot_alt");alternateStep=Load("step_alt");
        for(int i=0;i<voices.Length;i++)voices[i]=new Voice { source=Source("Effect_"+i, false), until=-1f };
        music=Source("Music",true);
        music.clip=Load(owner.IsCrimson?"music_crimson":"music_facility");
        interaction=Source("Interaction",true);interaction.clip=Load("interaction_loop");
        alarm=Source("Security_Alarm",true);alarm.clip=Load("alarm_loop");
        previousPosition=owner.Player!=null?(Vector2)owner.Player.position:Vector2.zero;
        lastSprint=UXCampaign.CapacityForLevel(owner.LevelId);
        EnsureListener();
#if UNITY_WEBGL && !UNITY_EDITOR
        ready=false; // Browser audio starts only after the first player gesture.
#else
        ready=true;
#endif
        if(ready && music.clip!=null){music.volume=0f;music.Play();}
        ApplyMix();
        if(MissingClipCount>0)Debug.LogWarning("Pixel Rebellion audio: "+MissingClipCount+" missing clips. Reapply the complete audio package.");
    }

    private AudioClip Load(string name)
    {
        AudioClip clip=Resources.Load<AudioClip>("UXAudio/"+name);
        if(clip==null)MissingClipCount++;
        return clip;
    }
    private AudioSource Source(string label,bool loop)
    {
        var child=new GameObject("Audio_"+label);child.transform.SetParent(transform,false);
        var source=child.AddComponent<AudioSource>();
        source.playOnAwake=false;source.loop=loop;source.spatialBlend=0f;
        source.volume=0f;source.dopplerLevel=0f;source.priority=loop?200:100;
        return source;
    }
    private void EnsureListener()
    {
        bool other=false;
        foreach(AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            if(listener!=fallbackListener && listener.isActiveAndEnabled){other=true;break;}
        if(!other && fallbackListener==null)
        {
            var child=new GameObject("Audio_Listener");child.transform.SetParent(transform,false);
            fallbackListener=child.AddComponent<AudioListener>();
        }
        if(fallbackListener!=null)fallbackListener.enabled=!other;
        listenerCheck=Time.unscaledTime+1f;
    }
    public static void Unlock()
    {
        var audio=Instance;
        if(audio==null || audio.ready)return;
        audio.ready=true;
        if(audio.music.clip!=null)audio.music.Play();
        audio.ApplyMix();
    }
    public static void UI() { Unlock(); Play(UXAudioCue.UI); }
    public static void Play(UXAudioCue cue)
    {
        if(Instance!=null)Instance.Emit(cue,Vector2.zero,false);
    }
    public static void At(UXAudioCue cue,Vector2 position)
    {
        if(Instance!=null)Instance.Emit(cue,position,true);
    }
    public static void MineExploded(Vector2 position)
    {
        if(Instance==null)return;
        foreach(Voice voice in Instance.voices)
            if(voice.cue==UXAudioCue.MineArmed && Vector2.Distance(voice.position,position)<.1f)Instance.Stop(voice);
        At(UXAudioCue.MineBlast,position);
    }
    public static void StopMineWarnings()
    {
        if(Instance==null)return;
        foreach(Voice voice in Instance.voices)if(voice.cue==UXAudioCue.MineArmed)Instance.Stop(voice);
    }
    public static void Outcome(bool won)
    {
        var audio=Instance;if(audio==null)return;
        audio.interaction.Stop();audio.alarm.Stop();StopMineWarnings();
        if(won)Play(audio.director.IsCrimson?UXAudioCue.Victory:UXAudioCue.LevelClear);
        else
        {
            string cause=audio.director.LastDamageCause;
            if(cause=="GUARD FIRE")Play(UXAudioCue.GuardDeath);
            else if(cause=="PROXIMITY MINE")Play(UXAudioCue.MineDeath);
            // Laser contact already plays the electrical burn, once.
            Play(UXAudioCue.Failed);
        }
    }
    private static int Priority(UXAudioCue cue)
    {
        if(cue==UXAudioCue.MineBlast || cue==UXAudioCue.LaserBurn || cue==UXAudioCue.GuardDeath ||
            cue==UXAudioCue.MineDeath || cue==UXAudioCue.Failed || cue==UXAudioCue.Victory || cue==UXAudioCue.LevelClear)return 4;
        if(cue==UXAudioCue.MineArmed || cue==UXAudioCue.Hit || cue==UXAudioCue.Checkpoint || cue==UXAudioCue.Security)return 3;
        if(cue==UXAudioCue.Step || cue==UXAudioCue.SprintStep)return 0;
        return 2;
    }
    private bool Emit(UXAudioCue cue,Vector2 position,bool world)
    {
        int index=(int)cue;
        if(!initialized || !ready || clips[index]==null || UXPlayerProgress.Data.audioMuted)return false;
        bool ended=GameManager.Instance!=null && GameManager.Instance.GameEnded;
        if(world && (Time.timeScale<=0f || ended && Priority(cue)<4))return false;
        float now=Time.unscaledTime;
        float interval=cue==UXAudioCue.Shot?.065f:cue==UXAudioCue.UI?.07f:
            cue==UXAudioCue.Step || cue==UXAudioCue.SprintStep?.10f:
            cue==UXAudioCue.Suspicion || cue==UXAudioCue.Alert?1.5f:.025f;
        if(now-lastCue[index]<interval)return false;
        float distance=world && director.Player!=null?Vector2.Distance(position,director.Player.position):0f;
        float range=cue==UXAudioCue.MineBlast?18f:cue==UXAudioCue.MineArmed?4.5f:
            cue==UXAudioCue.LaserCharge || cue==UXAudioCue.LaserOn?6f:12f;
        if(distance>=range)return false;
        float gain=cue==UXAudioCue.Step?.15f:cue==UXAudioCue.SprintStep?.23f:
            cue==UXAudioCue.UI?.28f:cue==UXAudioCue.Shot?.78f:
            cue==UXAudioCue.MineBlast || cue==UXAudioCue.LaserBurn?1f:.65f;
        gain*=Mathf.Lerp(1f,.04f,Mathf.Clamp01(distance/range));
        int priority=Priority(cue);Voice chosen=null;
        foreach(Voice voice in voices)if(voice.until<=now){chosen=voice;break;}
        if(chosen==null)
        {
            foreach(Voice voice in voices)
                if(voice.priority<priority && (chosen==null || voice.priority<chosen.priority ||
                    voice.priority==chosen.priority && voice.until<chosen.until))chosen=voice;
            if(chosen==null)return false;
        }
        Stop(chosen);
        AudioClip clip=clips[index];
        if(cue==UXAudioCue.Shot && ++shotNumber%2==0 && alternateShot!=null)clip=alternateShot;
        if(cue==UXAudioCue.Step && ++stepNumber%2==0 && alternateStep!=null)clip=alternateStep;
        chosen.cue=cue;chosen.position=position;chosen.gain=gain;chosen.priority=priority;chosen.world=world;
        chosen.source.clip=clip;chosen.source.pitch=1f;
        chosen.source.panStereo=world && director.Player!=null?
            Mathf.Clamp((position.x-director.Player.position.x)/14f,-.55f,.55f):0f;
        chosen.until=now+clip.length;lastCue[index]=now;
        if(priority>=3)duckUntil=Mathf.Max(duckUntil,now+Mathf.Min(clip.length,2f));
        ApplyMix();chosen.source.Play();return true;
    }
    private void Stop(Voice voice){voice.source.Stop();voice.until=-1f;voice.gain=0f;}

    private void Update()
    {
        if(!initialized)return;
        if(Input.anyKeyDown || Input.GetMouseButtonDown(0))Unlock();
        if(Time.unscaledTime>=listenerCheck)EnsureListener();
        bool ended=GameManager.Instance!=null && GameManager.Instance.GameEnded;
        bool playing=director.MissionStarted && !ended;
        bool paused=playing && Time.timeScale<=0f;
        if(paused && !wasPaused)
            foreach(Voice voice in voices)if(voice.world)Stop(voice);
        wasPaused=paused;
        if(playing && !paused && director.Player!=null)
        {
            bool held=Input.GetKey(KeyCode.Tab);
            if(held!=mapHeld)UI();mapHeld=held;
            bool hidden=director.PlayerHidden;
            if(hidden!=wasHidden)Play(hidden?UXAudioCue.Hide:UXAudioCue.Reveal);
            wasHidden=hidden;
            if(director.State.Phase!=previousPhase)
            {
                if(director.State.Phase==UXAlertPhase.Engaged)Play(UXAudioCue.Alert);
                else if(director.State.Phase==UXAlertPhase.Suspicious)Play(UXAudioCue.Suspicion);
                previousPhase=director.State.Phase;
            }
            Vector2 position=director.Player.position;
            float moved=Vector2.Distance(position,previousPosition);
            if(moved>.004f && moved<1f && !hidden && Time.unscaledTime>=nextStep)
            {
                bool sprint=controller!=null && controller.IsSprinting;
                At(sprint?UXAudioCue.SprintStep:UXAudioCue.Step,position);
                nextStep=Time.unscaledTime+(sprint?.24f:.48f);
            }
            previousPosition=position;
            float reserve=UXCampaign.SprintRemaining;
            if(lastSprint>0f && reserve<=0f)Play(UXAudioCue.SprintEmpty);
            lastSprint=reserve;
        }
        bool holding=playing && !paused && director.InteractionAvailable && director.InteractionProgress>0f && Input.GetKey(KeyCode.E);
        Loop(interaction,holding);
        bool alert=playing && !paused && (director.State.Phase==UXAlertPhase.Engaged || director.State.Phase==UXAlertPhase.Searching);
        Loop(alarm,alert);
        if(UXPlayerProgress.Data.audioMuted)
        {
            foreach(Voice voice in voices)if(voice.until>Time.unscaledTime)Stop(voice);
        }
        ApplyMix();
    }
    private void Loop(AudioSource source,bool active)
    {
        active=active && ready && !UXPlayerProgress.Data.audioMuted;
        if(active && !source.isPlaying && source.clip!=null)source.Play();
        else if(!active && source.isPlaying)source.Stop();
    }
    private void ApplyMix()
    {
        if(!initialized)return;
        UXPlayerSaveData settings=UXPlayerProgress.Data;
        float sfx=settings.audioMuted?0f:settings.effectsVolume;
        // Sum of all source gains has a hard ceiling, even during a crowded firefight.
        float alarmGain=alarm.isPlaying?.10f:0f;
        float interactionGain=interaction.isPlaying?.14f:0f;
        float sum=alarmGain+interactionGain;
        foreach(Voice voice in voices)if(voice.until>Time.unscaledTime)sum+=voice.gain;
        float unit=EffectsCeiling*sfx/Mathf.Max(1f,sum);
        foreach(Voice voice in voices)voice.source.volume=voice.until>Time.unscaledTime?voice.gain*unit:0f;
        alarm.volume=alarmGain*unit;interaction.volume=interactionGain*unit;
        bool ended=GameManager.Instance!=null && GameManager.Instance.GameEnded;
        float ambience=ended?.35f:Time.timeScale<=0f?.55f:1f;
        if(Time.unscaledTime<duckUntil)ambience*=.5f;
        music.volume=settings.audioMuted || !ready?0f:MusicCeiling*settings.musicVolume*ambience;
    }
    private void OnDisable()
    {
        if(!initialized)return;
        music.Stop();interaction.Stop();alarm.Stop();
        foreach(Voice voice in voices)Stop(voice);
    }
    private void OnDestroy(){if(Instance==this)Instance=null;}
}
