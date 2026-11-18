using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CampaignMenuStaminaChecks
{
    private static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    private static void Profile(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        Application.persistentDataPath=Path.GetDirectoryName(path);
        typeof(UXPlayerProgress).GetField("store",BindingFlags.Static|BindingFlags.NonPublic)
            .SetValue(null,new UXProgressStore(path));
        typeof(UXCampaign).GetMethod("ResetRuntime",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    }
    public static string Run(string output)
    {
        Directory.CreateDirectory(output);
        foreach(var mode in new[]{UXDifficulty.Easy,UXDifficulty.Hard})
        foreach(float bank in new[]{0f,4f,10f})
        {
            string path=Path.Combine(output,"upgrade-"+mode+"-"+bank+".json");
            var old=new UXPlayerSaveData{schemaVersion=4,difficulty=mode,fullscreen=false,showControlHints=false};
            old.RecordCompletion(UXPlayerProgress.FacilityLevelId,mode,81f);
            old.campaigns.Add(new UXCampaignSlot{mode=mode,levelId=UXCampaign.CrimsonLevelId,checkpoint=2,
                seconds=123f,detections=7,sprintSeconds=bank,mineSeed=1234});
            string original=JsonUtility.ToJson(old);
            File.WriteAllText(path,original);
            var store=new UXProgressStore(path);var slot=store.Data.FindCampaign(mode);
            Check(slot!=null && slot.sprintSeconds==bank+10f && slot.checkpoint==2 && slot.seconds==123f &&
                slot.detections==7 && slot.mineSeed==1234,"Level 2 upgrade lost progress or charged spent stamina twice.");
            Check(!store.Data.fullscreen && !store.Data.showControlHints &&
                store.Data.FindLevel(UXPlayerProgress.FacilityLevelId).BestTime(mode)==81f,"Upgrade lost settings/records.");
            Check(store.Save() && File.ReadAllText(path+".bak")==original,"Upgrade did not retain its original backup.");
            store=new UXProgressStore(path);
            Check(store.Data.FindCampaign(mode).sprintSeconds==bank+10f,"Reload granted extra stamina a second time.");
        }
        foreach(var mode in new[]{UXDifficulty.Easy,UXDifficulty.Hard})
        foreach(var level in new[]{UXPlayerProgress.FacilityLevelId,UXCampaign.CrimsonLevelId})
        {
            Profile(Path.Combine(output,"reserve-"+mode+"-"+level+".json"));
            SceneManager.Active=new Scene{name=level==UXCampaign.CrimsonLevelId?"UXCrimsonDepths":"UXFacilityExpanded"};
            float expected=level==UXCampaign.CrimsonLevelId?20f:10f;
            UXCampaign.RestartLevel(level,mode);UXCampaign.Begin(level,mode);
            Check(UXCampaign.SprintCapacity==expected && UXCampaign.SprintRemaining==expected,"Fresh level has the wrong capacity.");
            UXCampaign.SpendSprint(expected+5f);UXCampaign.RecordDeath();UXCampaign.PrepareReplay();UXCampaign.Begin(level,mode);
            Check(UXCampaign.SprintRemaining==expected,"Replay did not restore its level's capacity.");
        }
        Profile(Path.Combine(output,"menu.json"));
        SceneManager.Active=new Scene{name="UXFacilityExpanded"};
        Screen.width=1280;Screen.height=800;Input.Keys.Clear();Input.DownKeys.Clear();
        var menu=new UXMissionMenuScreen();bool started=false;string continued=null;
        menu.RestartConfirmed=()=>started=true;
        menu.ContinueRequested=()=>continued=UXCampaign.PrepareContinue();
        Capture(menu,output,"fresh");
        var button=GUI.Calls.Last(c=>c.Text=="NEW GAME  /  LEVEL 1  >");
        var normal=GUI.Calls.First(c=>c.Kind=="rect" && c.X==button.X && c.Y==button.Y && c.W==button.W && c.H==button.H);
        Event.current.mousePosition=new Vector2(button.X+button.W/2,button.Y+button.H/2);
        Capture(menu,output,"hover");
        var hover=GUI.Calls.First(c=>c.Kind=="rect" && c.X==button.X && c.Y==button.Y && c.W==button.W && c.H==button.H);
        Input.MouseButtons.Add(0);Capture(menu,output,"pressed");
        var pressed=GUI.Calls.First(c=>c.Kind=="rect" && c.X==button.X && c.Y==button.Y && c.W==button.W && c.H==button.H);
        Input.MouseButtons.Clear();Event.current.mousePosition=Vector2.zero;
        Check(normal.R!=hover.R && hover.R!=pressed.R,"Normal, hover and pressed states are visually identical.");
        Capture(menu,output,"fresh");
        Check(GUI.Calls.Any(c=>c.Text=="LEVEL 1") && GUI.Calls.Any(c=>c.Text=="LEVEL 2") &&
            GUI.Calls.Any(c=>c.Text=="NEW GAME STARTS HERE") && GUI.Calls.Any(c=>c.Text=="LOCKED  /  CLEAR LEVEL 1"),
            "Fresh player cannot see both levels and Level 1 start.");
        GUI.ButtonClick=t=>t=="NEW GAME  /  LEVEL 1  >";menu.Draw();GUI.ButtonClick=null;
        Check(started && UXCampaign.SelectedSlot==null && continued==null,"Fresh New Game inherited a saved level.");
        started=false;UXCampaign.Begin(UXPlayerProgress.FacilityLevelId,UXDifficulty.Easy);
        Capture(menu,output,"continue-level1");
        Check(GUI.Calls.Any(c=>c.Text=="CONTINUE HERE" && c.X<Screen.width/2),"Level 1 resume highlight is on the wrong card.");
        UXCampaign.Complete(UXPlayerProgress.FacilityLevelId,UXDifficulty.Easy,50f);
        UXCampaign.Begin(UXCampaign.CrimsonLevelId,UXDifficulty.Easy);UXCampaign.SecureSector(1);
        UXCampaign.Begin(UXPlayerProgress.FacilityLevelId,UXDifficulty.Hard);
        Capture(menu,output,"continue-level2");
        Check(continued==null && GUI.Calls.Any(c=>c.Text=="CONTINUE HERE" && c.X>Screen.width/2) &&
            GUI.Calls.Any(c=>c.Text=="SAVED CHECKPOINT 1 / 3"),"Saved Level 2 is not explicit or auto-loaded.");
        GUI.ButtonClick=t=>t=="CONTINUE LEVEL 2  >";menu.Draw();GUI.ButtonClick=null;
        Check(continued==UXCampaign.CrimsonScene,"Continue loaded the wrong scene.");
        UXPlayerProgress.SelectDifficulty(UXDifficulty.Hard);Capture(menu,output,"hard-level1");
        Check(GUI.Calls.Any(c=>c.Text=="CONTINUE LEVEL 1  >"),"Difficulty choice used another mode's checkpoint.");
        UXPlayerProgress.SelectDifficulty(UXDifficulty.Easy);
        GUI.ButtonClick=t=>t=="NEW GAME  /  START AT LEVEL 1";menu.Draw();GUI.ButtonClick=null;
        Capture(menu,output,"new-game-confirm");
        Check(!started && GUI.Calls.Any(c=>c.Text=="START A NEW GAME?"),"New Game erased progress without confirmation.");
        GUI.ButtonClick=t=>t=="CANCEL";menu.Draw();GUI.ButtonClick=null;
        Check(UXCampaign.SelectedSlot.levelId==UXCampaign.CrimsonLevelId && UXCampaign.SelectedSlot.checkpoint==1,
            "Cancel erased the saved checkpoint.");
        UXCampaign.Complete(UXCampaign.CrimsonLevelId,UXDifficulty.Easy,100f);
        Capture(menu,output,"complete");
        Check(GUI.Calls.Any(c=>c.Text=="REPLAY LEVEL 2") && !GUI.Calls.Any(c=>c.Text=="CONTINUE HERE"),
            "Completed campaign is mislabeled as an unfinished Continue.");
        GUI.ButtonClick=t=>t=="NEW GAME  /  LEVEL 1  >";menu.Draw();GUI.ButtonClick=null;
        GUI.ButtonClick=t=>t=="START NEW GAME";menu.Draw();GUI.ButtonClick=null;
        Check(started && UXPlayerProgress.Data.campaigns.Count==0 && UXPlayerProgress.Data.levels.Count==0 &&
            UXCampaign.SelectedSlot==null,"Confirmed New Game did not return to a fresh Level 1 campaign.");
        return "fresh Level 1 start; separate Continue; both cards and correct checkpoint highlight; mode isolation; reset Cancel/confirm; completed campaign; 10s/20s budgets; six one-time save upgrades; Replay capacity";
    }
    private static void Capture(UXMissionMenuScreen menu,string output,string name)
    {
        GUI.ButtonClick=null;GUI.Calls.Clear();menu.Draw();
        foreach(var c in GUI.Calls)Check(c.X>=-1&&c.Y>=-1&&c.X+c.W<=Screen.width+1&&c.Y+c.H<=Screen.height+1,
            "Menu bounds overflow: "+c.Text);
        File.WriteAllText(Path.Combine(output,name+".json"),
            JsonSerializer.Serialize(GUI.Calls,new JsonSerializerOptions{IncludeFields=true}));
    }
}

