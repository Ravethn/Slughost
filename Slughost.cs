using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace SlughostMod;

[BepInPlugin("Ravethn.Slughost", "Slughosts", "1.0.6")]
public partial class SlughostMod : BaseUnityPlugin
{
    private SlughostModOptions Options;

    public SlughostMod()
    {
        try
        {
            Options = new SlughostModOptions(this, Logger);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }
    private void OnEnable()
    {
        MyModdedEnums.CreatureTemplateType.RegisterValues();
        On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
    }

    private void OnDisable()
    {
        MyModdedEnums.CreatureTemplateType.UnregisterValues();
    }

    private bool IsInit;
    private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        try
        {
            if (IsInit) return;

            //Hooks

            //Implement scug ghost creature template
            On.StaticWorld.InitCustomTemplates += StaticWorldOnInitCustomTemplates;
            On.StaticWorld.InitStaticWorld += StaticWorldOnInitStaticWorld;
            On.AbstractCreature.Realize += AbstractCreatureOnRealize;

            //Game over prompt
            On.HUD.TextPrompt.ctor += HUDTextPromptOnCtor;

            //Creation and deletion
            On.Player.InitiateGraphicsModule += PlayerOnInitiateGraphicsModule;
            //On.PlayerGraphics.InitiateSprites += PlayerGraphicsOnInitiateSprites;
            On.Player.Die += PlayerOnDie;
            On.ShelterDoor.Close += ShelterDoorOnClose;
            On.RainWorldGame.ctor += RainWorldGameOnctor;
            On.Player.Destroy += PlayerOnDestroy;
            On.Player.Update += PlayerOnUpdate;

            //Camera adjustments
            On.Player.TriggerCameraSwitch += PlayerOnTriggerCameraSwitch;
            On.RoomCamera.ChangeCameraToPlayer += RoomCameraOnChangeCameraToPlayer;

            //Interactions
            On.Player.Grabability += PlayerOnGrabability;
            On.Player.SlugOnBack.ChangeOverlap += SlugOnBackOnChangeOverlap;
            On.Weapon.HitThisObject += WeaponOnHitThisObject;
            On.Player.LungUpdate += PlayerOnLungUpdate;
            On.PlayerGraphics.TailSpeckles.setSpearProgress += PlayerGraphicsOnSetSpearProgress;
            On.Player.PyroDeath += PlayerOnPyroDeath;

            //Creature reactions
            On.BigEel.JawsSnap += BigEelOnJawsSnap;
            On.MirosBirdAI.DoIWantToBiteCreature += MirosBirdAIOnDoIWantToBiteCreature;
            On.MirosBird.JawSlamShut += MirosBirdOnJawSlamShut;
            IL.MoreSlugcats.StowawayBug.Update += StowawayBugILUpdate;
            On.NoiseTracker.HeardNoise += NoiseTrackerOnHeardNoise;
            IL.JellyFish.Update += JellyFishILUpdate;
            On.MoreSlugcats.BigJellyFish.ValidGrabCreature += BigJellyFishOnValidGrabCreature;
            On.MoreSlugcats.BigJellyFish.HeardNoise += BigJellyFishOnHeardNoise;

            //Map for ghost
            IL.GhostCreatureSedater.Update += GhostCreatureSedaterILUpdate;
            On.CreatureSymbol.ColorOfCreature += CreatureSymbolOnColorOfCreature;
            On.CreatureSymbol.SpriteNameOfCreature += CreatureSymbolOnSpriteNameOfCreature;
            On.CreatureSymbol.SymbolDataFromCreature += CreatureSymbolOnSymbolDataFromCreature;
            IL.HUD.Map.Draw += MapILDraw;
            On.HUD.Map.ItemMarker.ItemMakerData.DataFromAbstractPhysical += ItemMakerDataOnDataFromAbstractPhysical;
            On.HUD.Map.ShelterMarker.ItemInShelterMarker.ItemInShelterData.DataFromAbstractPhysical += ItemInShelterDataOnDataFromAbstractPhysical;


            //Remove ghosts at shutdown or cycle end
            On.GameSession.ctor += GameSessionOnctor;
            On.RainWorldGame.ShutDownProcess += RainWorldGameOnShutDownProcess;

            MachineConnector.SetRegisteredOI("Ravethn.Slughost", Options);
            IsInit = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }
    
    private void RainWorldGameOnShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
    {
        forbidGhosts = true;
        orig(self);
        ClearMemory();
    }
    private void GameSessionOnctor(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig(self, game);
        ClearMemory();
    }

    #region Helper Methods

    private void ClearMemory()
    {
        //If you have any collections (lists, dictionaries, etc.)
        //Clear them here to prevent a memory leak
        //YourList.Clear();
        currentGhosts.Clear();
    }

    #endregion
}
