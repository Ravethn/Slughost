using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using RWCustom;
using BepInEx;
using Debug = UnityEngine.Debug;
using System.Data.SqlClient;
using UnityEngine.Rendering;
using System.Runtime.Serialization;
using Newtonsoft.Json.Serialization;
using IL.Menu.Remix.MixedUI;
using Menu.Remix.MixedUI;
using On.Menu.Remix.MixedUI;
using System.Runtime.Remoting.Messaging;
using MonoMod.RuntimeDetour;
using System.Xml;
using MonoMod.Cil;
using System.Runtime.Remoting.Lifetime;
using Steamworks;
using MonoMod;
using Unity.Collections.LowLevel.Unsafe;
using IL.Stove.Sample.Session;
using System.Reflection.Emit;
using On.Stove.Sample.Session;
using MonoMod.Utils.Cil;
using Stove.Sample.Session;
using Microsoft.Win32.SafeHandles;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace SlughostMod;

[BepInPlugin("Ravethn.Slughost", "Slughosts", "1.0.4")]
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
            //Your hooks go here
            MyModdedEnums.CreatureTemplateType.RegisterValues();
            On.StaticWorld.InitCustomTemplates += StaticWorldOnInitCustomTemplates;
            On.StaticWorld.InitStaticWorld += StaticWorldOnInitStaticWorld;
            On.AbstractCreature.Realize += AbstractCreatureOnRealize;
            On.PlayerGraphics.InitiateSprites += PlayerGraphicsOnInitiateSprites;
            On.Player.Die += PlayerOnDie;
            On.Player.Grabability += PlayerOnGrabability;
            On.ShelterDoor.Close += ShelterDoorOnClose;
            On.RainWorldGame.ctor += RainWorldGameOnctor;
            On.Player.Destroy += PlayerOnDestroy;
            On.Player.Update += PlayerOnUpdate;
            On.Player.LungUpdate += PlayerOnLungUpdate;
            On.Weapon.HitThisObject += WeaponOnHitThisObject;
            On.PlayerGraphics.TailSpeckles.setSpearProgress += PlayerGraphicsOnSetSpearProgress;
            On.Player.PyroDeath += PlayerOnPyroDeath;
            On.BigEel.JawsSnap += BigEelOnJawsSnap;
            On.MirosBirdAI.DoIWantToBiteCreature += MirosBirdAIOnDoIWantToBiteCreature;
            On.MirosBird.JawSlamShut += MirosBirdOnJawSlamShut;
            On.Player.SlugOnBack.ChangeOverlap += SlugOnBackOnChangeOverlap;
            IL.GhostCreatureSedater.Update += GhostCreatureSedaterILUpdate;
            On.CreatureSymbol.ColorOfCreature += CreatureSymbolOnColorOfCreature;
            On.CreatureSymbol.SpriteNameOfCreature += CreatureSymbolOnSpriteNameOfCreature;
            IL.HUD.Map.Draw += MapILDraw;
            On.HUD.Map.ItemMarker.ItemMakerData.DataFromAbstractPhysical += ItemMakerDataOnDataFromAbstractPhysical;
            On.HUD.Map.ShelterMarker.ItemInShelterMarker.ItemInShelterData.DataFromAbstractPhysical += ItemInShelterDataOnDataFromAbstractPhysical;


            On.RainWorldGame.ShutDownProcess += RainWorldGameOnShutDownProcess;
            On.GameSession.ctor += GameSessionOnctor;
            
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
    }

    #endregion
}
