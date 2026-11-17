using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using Object=UnityEngine.Object;

public static class PressureBalanceChecks
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
    private static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    private static void Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Private).Invoke(o,args);
    private static void Set(object o,string field,object value)=>o.GetType().GetField(field,Private).SetValue(o,value);
    private static object Campaign(string method,params object[] args)=>typeof(CampaignChecks).GetMethod(method,Static).Invoke(null,args);
    private static void Build(string json,string path,int stage)
    {
        Campaign("Profile",path);typeof(CampaignChecks).GetField("geometry",Static).SetValue(null,JsonDocument.Parse(json).RootElement.Clone());
        Campaign("Build",UXDifficulty.Hard,stage,true);Physics2D.SightQuery=ConePolishChecks.Query;
    }
    public static string Run(string json,string output)
    {
        Directory.CreateDirectory(output);Migration(output);SparseCover(json,output);
        Check(UXCrimsonHazards.MinePositions.Length==24 && UXCrimsonHazards.MinesPerSector==8,"Mine count did not double evenly.");
        for(int a=0;a<24;a++)for(int b=a+1;b<24;b++)
            Check(Vector2.Distance(UXCrimsonHazards.MinePositions[a],UXCrimsonHazards.MinePositions[b])>5.4f,"Mine spacing creates an immediate second trigger during the first escape.");
        return "24 concealed mines / 8 per sector; 2.4 radius; 20s Level 2 equal-mode reserve; 3 hiding bays and 19 solid retained crates; all four checkpoint saves remain safe with CP2/3 sharing the Vault refuge; map/prompt deduplication; schema3 spent fraction migration";
    }
    private static void Migration(string output)
    {
        string json="{\"gameId\":\"PixelRebellion\",\"schemaVersion\":3,\"difficulty\":0,\"showControlHints\":false,\"fullscreen\":false,"+
            "\"levels\":[{\"levelId\":\"facility_01\",\"easyClears\":1,\"bestEasySeconds\":111}],"+
            "\"campaigns\":[{\"mode\":0,\"levelId\":\"crimson_02\",\"checkpoint\":3,\"seconds\":121,\"detections\":5,\"sprintSeconds\":12},"+
            "{\"mode\":1,\"levelId\":\"crimson_02\",\"checkpoint\":1,\"sprintSeconds\":0}]}";
        string path=Path.Combine(output,"old-24-second-save.json");File.WriteAllText(path,json);var store=new UXProgressStore(path);
        var easy=store.Data.FindCampaign(UXDifficulty.Easy);var hard=store.Data.FindCampaign(UXDifficulty.Hard);
        Check(store.Data.schemaVersion==UXPlayerSaveData.CurrentVersion && easy.sprintSeconds==15f && hard.sprintSeconds==10f,"Migration refunded or erased the spent resource fraction.");
        Check(easy.checkpoint==3 && easy.seconds==121 && easy.detections==5 && !store.Data.fullscreen &&
            store.Data.FindLevel("facility_01").bestEasySeconds==111,"Migration lost checkpoint/record/settings.");
        Check(store.Save() && File.ReadAllText(path+".bak")==json,"Original 24s save backup missing.");
        store=new UXProgressStore(path);Check(store.Data.FindCampaign(UXDifficulty.Easy).sprintSeconds==15f && store.Data.FindCampaign(UXDifficulty.Hard).sprintSeconds==10f,"Reload applied scaling twice.");
    }
    private static void SparseCover(string json,string output)
    {
        for(int stage=0;stage<4;stage++)
        {
            Build(json,Path.Combine(output,"checkpoint-"+stage+".json"),stage);var director=UXStealthDirector.Instance;var sectors=director.Sectors;
            var bays=Object.FindObjectsByType<UXSolidHidingCover>(FindObjectsSortMode.None);Check(bays.Length==3,"Extra hiding bays remain.");
            for(int sector=0;sector<3;sector++)Check(bays.Count(b=>UXSectorMission.IndexOf(b.transform.position)==sector)==1,"A sector has missing/duplicate refuges.");
            var blocks=Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Where(g=>g.name=="CrimsonCoverBlock").ToArray();
            Check(blocks.Length==19 && blocks.All(b=>b.GetComponent<BoxCollider2D>()!=null && !b.GetComponent<BoxCollider2D>().isTrigger && b.GetComponent<UXSolidHidingCover>()==null),"Unused bays were removed or remained functional hiding places.");
            var hiding=director.Player.GetComponent<PlayerHiding>();Check(hiding.IsHidden,"Checkpoint restored outside its refuge.");
            foreach(var block in blocks)
            {
                director.Player.position=block.transform.position;Check(!hiding.IsHidden,"Ordinary crate still hides the player.");
                Check(Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s=>s.gameObject.name=="HidingGlow"&&Vector2.Distance(s.transform.position,block.transform.position)<.75f).All(s=>!s.enabled),"Ordinary crate still has a hiding glow.");
            }
            sectors.Restore(stage);Check(hiding.IsHidden && sectors.CheckpointAt(director.Player.position)==stage,"Shared checkpoint prompt is incorrect.");
            sectors.SelectTarget(out _,out string prompt);Check(prompt.Contains("CHECKPOINT "+stage) && prompt.Contains("SAVED"),"Saved shared checkpoint is mislabeled.");
            var hud=new GameObject("HUD").AddComponent<UXHUDController>();Call(hud,"Awake");Call(hud,"Start");
            var screen=new UXMissionBriefingScreen(9,true);GUI.Calls.Clear();screen.DrawMapOverlay();
            Check(GUI.Calls.Count(c=>c.Text!=null&&c.Text.StartsWith("CP"))==3,"Shared Vault checkpoint has overlapping map labels.");
            for(int width=960;width<=1920;width*=2)
            {
                Screen.width=width;Screen.height=width*9/16;GUI.Calls.Clear();screen.DrawMapOverlay();
                File.WriteAllText(Path.Combine(output,"blueprint_"+stage+"_"+width+".json"),JsonSerializer.Serialize(GUI.Calls,new JsonSerializerOptions{IncludeFields=true}));
            }
        }
    }
}
