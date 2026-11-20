using UnityEngine;

// Level-specific objectives only; movement, detection, damage, navigation and
// hold/cancel timing remain in the shared stealth director.
public sealed class UXSectorMission : MonoBehaviour
{
    [SerializeField] private EvidenceCollectible[] caches = new EvidenceCollectible[0];
    [SerializeField] private TerminalInteraction[] consoles = new TerminalInteraction[0];
    [SerializeField] private BoxCollider2D[] bulkheads = new BoxCollider2D[0];
    [SerializeField] private Vector2[] checkpointPositions = new Vector2[0];
    private readonly RaycastHit2D[] sightHits = new RaycastHit2D[16];
    private UXStealthDirector director;
    private int cacheMask;
    public int SecuredSectors { get; private set; }
    public UXAccessLock ExitDoor { get; private set; }
    public TerminalInteraction[] Consoles => consoles;
    public Vector2[] Checkpoints => checkpointPositions;
    public BoxCollider2D[] Bulkheads => bulkheads;
    public static readonly string[] SectorNames = { "DOCKS", "ARCHIVES", "REACTOR VAULT" };
    public string SectorName => SectorNames[Mathf.Min(2, SecuredSectors)];
    public string Objective => SecuredSectors >= 3 ?
        (ExitDoor.IsUnlocked ? "REACH THE EXTRACTION LIFT" : "OVERRIDE THE EXTRACTION DOOR - HOLD E") :
        (SectorData(SecuredSectors) < 3 ? SectorName + ": DOWNLOAD DATA " + SectorData(SecuredSectors) + " / 3" :
            "HACK " + SectorName + " SECURITY - HOLD E");

    public void Initialize(UXStealthDirector owner)
    {
        director = owner;
        // One physical refuge per sector. The final Vault save uses the same
        // refuge as its entry checkpoint, rather than adding a nearby fourth bay.
        if(checkpointPositions.Length>=4)checkpointPositions[3]=checkpointPositions[2];
        ExitDoor = UXAccessLock.Create(transform, "EXIT", new Vector2(24f,-11f),
            new Vector2(3.02f,.98f), new Vector2(24f,-10f));
    }

    public void Restore(int checkpoint)
    {
        SecuredSectors = Mathf.Min(3, Mathf.Max(0, checkpoint));
        cacheMask = (1 << (SecuredSectors * 3)) - 1;
        for (int i=0;i<caches.Length;i++) if ((cacheMask & (1<<i)) != 0 && caches[i] != null) caches[i].RestoreCollected();
        EvidenceManager.Instance?.RestoreEvidence(SecuredSectors * 3);
        for (int i=0;i<SecuredSectors;i++)
        {
            consoles[i].RestoreActivated();
            if (i<bulkheads.Length) OpenBulkhead(i);
        }
        if (SecuredSectors == 3) GameManager.Instance?.ActivateTerminal();
        if (checkpointPositions.Length > SecuredSectors && director.Player != null)
            director.Player.position = checkpointPositions[SecuredSectors];
        PlayerHiding hiding=director.Player!=null?director.Player.GetComponent<PlayerHiding>():null;
        if(hiding!=null)hiding.SynchronizeRestoredBay();
    }

    public bool IsHidingBay(Vector2 position)
    {
        for(int i=0;i<3 && i<checkpointPositions.Length;i++)
            if(Vector2.Distance(position,checkpointPositions[i])<.1f)return true;
        return false;
    }

    // Avoid duplicate labels/prompts where checkpoints 2 and 3 share a refuge.
    public int CheckpointAt(Vector2 position)
    {
        int saved=-1,future=-1;
        for(int i=0;i<checkpointPositions.Length;i++)
            if(Vector2.Distance(position,checkpointPositions[i])<.4f)
            {
                if(i<=SecuredSectors)saved=i;else if(future<0)future=i;
            }
        return saved>=0?saved:future;
    }

    public int SectorData(int sector)
    {
        int count=0;
        for(int i=sector*3;i<sector*3+3;i++)if((cacheMask&(1<<i))!=0)count++;
        return count;
    }

    public bool CacheAvailable(int index) => index >= 0 && index < caches.Length &&
        index/3 <= SecuredSectors && (cacheMask&(1<<index))==0 && caches[index]!=null && caches[index].gameObject.activeSelf;

    public EvidenceCollectible Cache(int index) => caches[index];

    public Object SelectTarget(out float duration, out string prompt)
    {
        duration=2.5f*director.Difficulty.InteractionDuration;
        prompt=director.PlayerHidden ? "HIDDEN - WATCH THE PATROLS BEFORE MOVING" :
            "FOLLOW THE SECTOR OBJECTIVES   |   TAB: FACILITY MAP";
        Object target=null;float closest=1.15f;
        if(director.PlayerHidden)
        {
            int checkpoint=CheckpointAt(director.Player.position);
            if(checkpoint>=0)prompt=checkpoint<=SecuredSectors?"CHECKPOINT "+checkpoint+
                (UXPlayerProgress.Store.HasSaveError?" / SESSION ONLY - SAVE UNAVAILABLE":" / SAVED - HIDDEN"):
                "MAINTENANCE BAY / HIDDEN - SECURE THE SECTOR TO SAVE";
        }
        for(int i=0;i<caches.Length;i++)
        {
            if(!CacheAvailable(i))continue;
            float distance=Vector2.Distance(director.Player.position,caches[i].transform.position);
            if(distance>=closest || !Visible(caches[i].transform))continue;
            target=caches[i];closest=distance;prompt="HOLD E - DOWNLOAD EVIDENCE";
        }
        if(SecuredSectors<3 && consoles.Length>SecuredSectors)
        {
            TerminalInteraction console=consoles[SecuredSectors];
            if(Vector2.Distance(director.Player.position,console.transform.position)<1.15f && Visible(console.transform))
            {
                if(SectorData(SecuredSectors)<3) prompt="SECURITY LOCKED - DOWNLOAD THIS SECTOR'S THREE CACHES";
                else {target=console;duration=4f*director.Difficulty.InteractionDuration;prompt="HOLD E - HACK SECTOR SECURITY";}
            }
        }
        if(!ExitDoor.IsUnlocked && Vector2.Distance(director.Player.position,ExitDoor.transform.position)<1.15f && Visible(ExitDoor.transform))
        {
            if(SecuredSectors<3)prompt="EXTRACTION LOCKED - SECURE ALL THREE SECTORS";
            else {target=ExitDoor;duration=director.ExitDoorSeconds;prompt="HOLD E - OVERRIDE EXTRACTION DOOR";}
        }
        return target;
    }

    private bool Visible(Transform target) => DetectionUtility.HasLineOfSight(director.Player.position,
        target.position,director.Player,sightHits,target);

    public void Complete(Object target)
    {
        if(target is EvidenceCollectible cache)
        {
            for(int i=0;i<caches.Length;i++)if(caches[i]==cache && CacheAvailable(i))
            {
                int before=EvidenceManager.Instance.EvidenceCollected;cache.Collect();
                if(EvidenceManager.Instance.EvidenceCollected>before)cacheMask|=1<<i;
                return;
            }
        }
        else if(target is TerminalInteraction console && SecuredSectors<3 &&
            console==consoles[SecuredSectors] && SectorData(SecuredSectors)==3)
        {
            console.ActivateFromDownload();
            if(!console.IsActivated)return;
            if(SecuredSectors<bulkheads.Length)OpenBulkhead(SecuredSectors);
            SecuredSectors++;
            director.Hazards?.SecureSector(SecuredSectors-1,console.transform.position);
            if(SecuredSectors==3)GameManager.Instance.ActivateTerminal();
            director.RefreshNavigation();
            bool saved=UXCampaign.SecureSector(SecuredSectors);
            UXGameAudio.StopMineWarnings();
            UXGameAudio.Play(UXAudioCue.Checkpoint);
            director.Notice(saved ? "SECTOR SECURED - DEFENSES OFF - CHECKPOINT SAVED" : "SECTOR SECURED - SAVE UNAVAILABLE",4f);
        }
        else if(target==ExitDoor && SecuredSectors==3)
        {
            ExitDoor.Unlock();director.RefreshNavigation();
            director.Notice("EXTRACTION DOOR OPEN - REACH THE LIFT",4f);
        }
    }

    private void OpenBulkhead(int index)
    {
        bulkheads[index].enabled=false;
        SpriteRenderer art=bulkheads[index].GetComponent<SpriteRenderer>();
        if(art!=null)art.enabled=false;
    }

    public static int IndexOf(Vector2 at) => at.x < -9f ? 0 : (at.x < 9f ? 1 : 2);
    public static int Membership(Transform actor)
    {
        UXSecuritySector member=actor.GetComponent<UXSecuritySector>();
        return member!=null?Mathf.Min(2,Mathf.Max(0,member.sector)):IndexOf(actor.position);
    }
    public static Vector2 ClampTarget(int sector,Vector2 at) => new Vector2(
        Mathf.Clamp(at.x,sector==0?-26.5f:(sector==1?-8.45f:9.55f),sector==0?-9.55f:(sector==1?8.45f:26.5f)),
        Mathf.Clamp(at.y,-15.5f,15.5f));
    public static bool InSector(int sector,Vector2 at) => at.x >= (sector==0?-26.5f:(sector==1?-8.45f:9.55f)) &&
        at.x <= (sector==0?-9.55f:(sector==1?8.45f:26.5f));
}
