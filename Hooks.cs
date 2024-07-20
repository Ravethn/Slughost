using BepInEx;
using DevInterface;
using HarmonyLib;
using IL.JollyCoop;
using IL.MoreSlugcats;
using JetBrains.Annotations;
using RWCustom;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace SlughostMod;

public partial class SlughostMod
{
    private bool improvedInputEnabled = false;
    //Used to prevent ghosts from spawning and being saved in shelters
    private bool forbidGhosts = false;

    private static WorldCoordinate ToPipeOrCam(Player self)
    {
        //Sets ghost spawn point to the player with the camera
        if (self.room.game.RealizedPlayerFollowedByCamera != self && !self.room.game.RealizedPlayerFollowedByCamera.inShortcut && self.room.game.RealizedPlayerFollowedByCamera != null)
        {
            return self.room.game.RealizedPlayerFollowedByCamera.coord;
        }
        else
        {
            //Spawn ghost at random room entrance if there are no alive players or camera is on self
            return self.room.LocalCoordinateOfNode(self.room.abstractRoom.ExitIndex(self.room.abstractRoom.connections[UnityEngine.Random.Range(0, self.room.abstractRoom.connections.Length)]));
        }
    }

    private static void CreateGhost(Player self, WorldCoordinate coord)
    {
        AbstractCreature newGhost = new AbstractCreature(self.room.world, StaticWorld.GetCreatureTemplate(MyModdedEnums.CreatureTemplateType.SlugcatGhost), null, coord, self.room.game.GetNewID());
        newGhost.state = new PlayerGhostState(newGhost, self.playerState.playerNumber, self.playerState.slugcatCharacter, false);
        if (ModManager.CoopAvailable)
        {
            (newGhost.state as PlayerGhostState).isPup = self.playerState.isPup;
        }
        self.room.abstractRoom.AddEntity(newGhost);
        newGhost.RealizeInRoom();
    }

    #region StaticWorld Init

    //Initializing SlugcatGhost enum (copies Slugcat's stats)
    private static void StaticWorldOnInitCustomTemplates(On.StaticWorld.orig_InitCustomTemplates orig)
    {
        orig();
        List<TileTypeResistance> ghostlist2 = new List<TileTypeResistance>();
        List<TileConnectionResistance> ghostlist3 = new List<TileConnectionResistance>();
        ghostlist2.Add(new TileTypeResistance(AItile.Accessibility.Floor, 1f, PathCost.Legality.Allowed));
        ghostlist2.Add(new TileTypeResistance(AItile.Accessibility.Corridor, 1.2f, PathCost.Legality.Allowed));
        ghostlist2.Add(new TileTypeResistance(AItile.Accessibility.Climb, 1.5f, PathCost.Legality.Allowed));
        ghostlist3.Add(new TileConnectionResistance(MovementConnection.MovementType.Standard, 1f, PathCost.Legality.Allowed));
        ghostlist3.Add(new TileConnectionResistance(MovementConnection.MovementType.OpenDiagonal, 2f, PathCost.Legality.Allowed));
        ghostlist3.Add(new TileConnectionResistance(MovementConnection.MovementType.ReachOverGap, 3f, PathCost.Legality.Allowed));
        ghostlist3.Add(new TileConnectionResistance(MovementConnection.MovementType.ReachUp, 2f, PathCost.Legality.Allowed));
        ghostlist3.Add(new TileConnectionResistance(MovementConnection.MovementType.ReachDown, 2f, PathCost.Legality.Allowed));
        ghostlist3.Add(new TileConnectionResistance(MovementConnection.MovementType.DropToFloor, 3000f, PathCost.Legality.Allowed));
        ghostlist3.Add(new TileConnectionResistance(MovementConnection.MovementType.DropToClimb, 6000f, PathCost.Legality.Allowed));
        ghostlist3.Add(new TileConnectionResistance(MovementConnection.MovementType.DropToWater, 4000f, PathCost.Legality.Allowed));
        ghostlist3.Add(new TileConnectionResistance(MovementConnection.MovementType.ShortCut, 0.9f, PathCost.Legality.Allowed));
        //Slugcat Ghost
        CreatureTemplate slugcatghostre = new CreatureTemplate(MyModdedEnums.CreatureTemplateType.SlugcatGhost, null, ghostlist2, ghostlist3, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.DoesntTrack, 1f));
        slugcatghostre.baseDamageResistance = 1f;
        slugcatghostre.baseStunResistance = 1f;
        slugcatghostre.instantDeathDamageLimit = 1f;
        slugcatghostre.doPreBakedPathing = false;
        slugcatghostre.grasps = 2;
        slugcatghostre.offScreenSpeed = 0.1f;
        slugcatghostre.bodySize = 1f;
        slugcatghostre.shortcutSegments = 2;
        slugcatghostre.preBakedPathingAncestor = StaticWorld.creatureTemplates[CreatureTemplate.Type.StandardGroundCreature.Index];
        slugcatghostre.meatPoints = 3;
        slugcatghostre.waterRelationship = CreatureTemplate.WaterRelationship.Amphibious;
        slugcatghostre.waterPathingResistance = 2f;
        slugcatghostre.canSwim = true;
        slugcatghostre.wormGrassImmune = true;
        slugcatghostre.name = "Slugcat Ghost";
        ghostlist2.Clear();
        ghostlist3.Clear();
        StaticWorld.creatureTemplates[slugcatghostre.type.Index] = slugcatghostre;
    }

    //StaticWorld Creature Relationships
    private static void StaticWorldOnInitStaticWorld(On.StaticWorld.orig_InitStaticWorld orig)
    {
        orig();
        StaticWorld.EstablishRelationship(MyModdedEnums.CreatureTemplateType.SlugcatGhost, CreatureTemplate.Type.LizardTemplate, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 1f));
        StaticWorld.EstablishRelationship(MyModdedEnums.CreatureTemplateType.SlugcatGhost, CreatureTemplate.Type.Fly, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.5f));
        StaticWorld.EstablishRelationship(MyModdedEnums.CreatureTemplateType.SlugcatGhost, CreatureTemplate.Type.Vulture, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 1f));
        StaticWorld.EstablishRelationship(MyModdedEnums.CreatureTemplateType.SlugcatGhost, CreatureTemplate.Type.BigEel, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 1f));
        StaticWorld.EstablishRelationship(MyModdedEnums.CreatureTemplateType.SlugcatGhost, CreatureTemplate.Type.DaddyLongLegs, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 1f));
        StaticWorld.EstablishRelationship(MyModdedEnums.CreatureTemplateType.SlugcatGhost, CreatureTemplate.Type.TentaclePlant, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 1f));
        StaticWorld.EstablishRelationship(MyModdedEnums.CreatureTemplateType.SlugcatGhost, CreatureTemplate.Type.MirosBird, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.8f));
        StaticWorld.EstablishRelationship(MyModdedEnums.CreatureTemplateType.SlugcatGhost, CreatureTemplate.Type.Centipede, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.3f));
        StaticWorld.EstablishRelationship(MyModdedEnums.CreatureTemplateType.SlugcatGhost, CreatureTemplate.Type.Centiwing, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.3f));
        StaticWorld.EstablishRelationship(MyModdedEnums.CreatureTemplateType.SlugcatGhost, CreatureTemplate.Type.SmallCentipede, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0.3f));
        StaticWorld.EstablishRelationship(MyModdedEnums.CreatureTemplateType.SlugcatGhost, CreatureTemplate.Type.BigSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.5f));
        StaticWorld.EstablishRelationship(MyModdedEnums.CreatureTemplateType.SlugcatGhost, CreatureTemplate.Type.SpitterSpider, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.65f));
        StaticWorld.EstablishRelationship(MyModdedEnums.CreatureTemplateType.SlugcatGhost, CreatureTemplate.Type.DropBug, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 0.5f));
        StaticWorld.EstablishRelationship(MyModdedEnums.CreatureTemplateType.SlugcatGhost, CreatureTemplate.Type.RedCentipede, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 1f));
        StaticWorld.EstablishRelationship(CreatureTemplate.Type.PoleMimic, MyModdedEnums.CreatureTemplateType.SlugcatGhost, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.DoesntTrack, 1f));
        StaticWorld.EstablishRelationship(CreatureTemplate.Type.Spider, MyModdedEnums.CreatureTemplateType.SlugcatGhost, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.DoesntTrack, 1f));
        StaticWorld.EstablishRelationship(CreatureTemplate.Type.BigEel, MyModdedEnums.CreatureTemplateType.SlugcatGhost, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.DoesntTrack, 1f));
        if (ModManager.MSC)
        {
            StaticWorld.EstablishRelationship(MyModdedEnums.CreatureTemplateType.SlugcatGhost, MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.MirosVulture, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, 1f));
        }
    }

    //Associating SlugcatGhost Enum with PlayerGhost class
    private void AbstractCreatureOnRealize(On.AbstractCreature.orig_Realize orig, AbstractCreature self)
    {
        if (self.Room != null && self.realizedCreature == null && self.creatureTemplate.TopAncestor().type == MyModdedEnums.CreatureTemplateType.SlugcatGhost)
        {
            self.realizedCreature = new PlayerGhost(self, self.world);
        }
        orig(self);
    }
    #endregion

    //Shader for ghosts
    /*
    private void RainWorldOnProcessManagerInitializsation(On.RainWorld.orig_ProcessManagerInitializsation orig, RainWorld self)
    {
        orig(self);
    }
    */
    
    //Ghost transparency
    private void PlayerGraphicsOnInitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);
        if (self.player is PlayerGhost && !rCam.room.game.DEBUGMODE)
        {
            for (int ghostSprite = 0; ghostSprite < 9; ghostSprite++)
            {
                sLeaser.sprites[ghostSprite].shader = rCam.game.rainWorld.Shaders["Hologram"];
                sLeaser.sprites[ghostSprite].alpha = 0.95f;
            }

        }
    }

    //Alters ghost players to be unable to grab or be grabbed
    private Player.ObjectGrabability PlayerOnGrabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        if (self is PlayerGhost)
        {
            if (ModManager.MSC)
            {
                if (self.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint)
                {
                    //Saint power to pickup things while a ghost
                    return orig(self, obj);
                }
            }
            //Makes ghosts unable to pick up items
            return Player.ObjectGrabability.CantGrab;
        }
        if (obj is PlayerGhost)
        {
            //Makes slughosts unable to be grabbed
            return Player.ObjectGrabability.CantGrab;
        }


        return orig(self, obj);
    }

    //Stops ghost from drowning
    private void PlayerOnLungUpdate(On.Player.orig_LungUpdate orig, Player self)
    {
        if (self is PlayerGhost)
        {
            //arti explodes if it goes under 0.65
            if (self.airInLungs < 0.66f)
            {
                self.airInLungs = 1f;
            }
        }
        orig(self);
    }

    //Stops ghosts from being hit by weapons
    private bool WeaponOnHitThisObject(On.Weapon.orig_HitThisObject orig, Weapon self, PhysicalObject obj)
    {
        if (obj is PlayerGhost)
        {
            return false;
        }
        return orig(self, obj);
    }


    //Checks in Player.Update to do teleporting ability
    //Need to set up a improved input mod compatability thing
    private bool IsGhostTeleportPressed(PlayerGhost ghost)
    {
        if (!improvedInputEnabled) //improvedInputEnabled Temporarily set always false
        {
            return RWInput.CheckSpecificButton((ghost.State as PlayerState).playerNumber, 3);
        }
        return false;
    }



    //Removes Ghosts when sheltering, and prevents them from spawning so ghosts can't be saved in shelter
    private void ShelterDoorOnClose(On.ShelterDoor.orig_Close orig, ShelterDoor self)
    {
        if (!self.Broken)
        {
            forbidGhosts = true;
        }
        orig(self);

    }

    //Checks for when game starts again to reallow ghosts to be spawned
    private void RainWorldGameOnctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        orig(self, manager);
        forbidGhosts = false;
    }


    //Creates ghost when a player dies
    private void PlayerOnDie(On.Player.orig_Die orig, Player self)
    {
        if ((self.room != null) && self.room.game != null && self.playerState != null && !self.isNPC && !self.dead && !self.room.game.setupValues.invincibility && !forbidGhosts && !self.playerState.isGhost && !(self is PlayerGhost))
        {
            WorldCoordinate spawnCoord = self.room.GetWorldCoordinate(self.firstChunk.pos);
            if (self.playerState.permaDead)
            {
                //To prevent slughost spawning in lizard den which traumatizes the lizard into mental break
                spawnCoord = ToPipeOrCam(self);
            }
            //Creates ghost slugcat
            CreateGhost(self, spawnCoord);
        }
        orig(self);
    }

    //Create ghost after destorying a ghost since ghost is about to be deleted from the game
    //New ghost doesn't need to be created when a ghost permadies since it does not delete itself
    //Destroying happens when you fall off of death cliffs
    private void PlayerOnDestroy(On.Player.orig_Destroy orig, Player self)
    {

        if (self is PlayerGhost && !forbidGhosts)
        {
            //Creates new ghost if ghost falls off of map into deathpit since it is about to be deleted
            WorldCoordinate spawnCoord = ToPipeOrCam(self);
            CreateGhost(self, spawnCoord);
            Debug.Log("Ghost destroyed! Creating new ghost!");
        }
        else if (!ModManager.CoopAvailable && !forbidGhosts && !self.isNPC && !self.playerState.isGhost)
        {
            //If Jolly coop is off, creates a ghost when a player falls since Player.Destroy without Jolly does not run PermaDie method
            WorldCoordinate spawnCoord2 = ToPipeOrCam(self);
            CreateGhost(self, spawnCoord2);
        }
        orig(self);
    }

    private void PlayerOnUpdate(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        if(self is PlayerGhost)
        {
            //Checks if player has been revived, or is alive again
            if (!self.inShortcut)
            {
                foreach (AbstractCreature absPlayer in self.room.game.AlivePlayers)
                {
                    if (absPlayer.realizedCreature != null && (absPlayer.realizedCreature as Player).playerState.playerNumber == self.playerState.playerNumber)
                    {
                        //Deletes ghost if the player is revived
                        //Need to make a cool deletion effect for this
                        self.slatedForDeletetion = true;
                    }
                }
                if (forbidGhosts)
                {
                    self.slatedForDeletetion = true;
                }
            }

            //Warping ghost to player with camera
            if (IsGhostTeleportPressed(self as PlayerGhost))
            {
                if((self as PlayerGhost).ghostTeleTimer <= 60)
                {
                    (self as PlayerGhost).ghostTeleTimer++;
                }
                //Stop ghost with grabbing ability from swallowing items and accidentally deleting them
                self.swallowAndRegurgitateCounter = 0;
                //Stop ghost from being able to eat
                self.eatCounter = 15;

                //Debug.Log("GhostTeleTimer: " + (self as PlayerGhost).ghostTeleTimer.ToString());
                if ((self as PlayerGhost).ghostTeleTimer == 60)
                {
                    if(self.room.game.RealizedPlayerFollowedByCamera != self && !self.room.game.RealizedPlayerFollowedByCamera.inShortcut && self.room.game.RealizedPlayerFollowedByCamera != null)
                    {
                        PlayerGhost.WarpAndReviveGhost(self as PlayerGhost, self.room.game.RealizedPlayerFollowedByCamera.room.abstractRoom, ToPipeOrCam(self));
                    }
                    else
                    {
                        PlayerGhost.WarpAndReviveGhost(self as PlayerGhost, self.room.abstractRoom, ToPipeOrCam(self));
                    }
                    
                }

            }
            else if ((self as PlayerGhost).ghostTeleTimer > 0)
            {
                (self as PlayerGhost).ghostTeleTimer = 0;
            }

            if (self.room.game.paused)
            {
                //Ignore ghost input while in pause menu
                self.input[0].x = 0;
                self.input[0].y = 0;
                self.input[0].jmp = false;
            }

        }
    }

    
    private void PlayerGraphicsOnSetSpearProgress(On.PlayerGraphics.TailSpeckles.orig_setSpearProgress orig, PlayerGraphics.TailSpeckles self, float p)
    {
        orig(self, p);
        //Stop spearmaster from making spears for now so they can't get food
        if (self.spearProg > 0 && self.pGraphics.player is PlayerGhost)
        {
            self.spearProg = 0;
        }
    }

    
    private void PlayerOnPyroDeath(On.Player.orig_PyroDeath orig, Player self)
    {
        //Stop ghost arti from exploding when dying from pyrodeath
        if (self is PlayerGhost)
        {
            self.Die();
            return;
        }
        orig(self);
    }

    private void BigEelOnJawsSnap(On.BigEel.orig_JawsSnap orig, BigEel self)
    {
        orig(self);
        for (int i = 0; i < self.clampedObjects.Count; i++)
        {
            if (self.clampedObjects[i].chunk.owner is PlayerGhost)
            {
                (self.clampedObjects[i].chunk.owner as PlayerGhost).dead = false;
                self.clampedObjects.RemoveAt(i);
                self.clampedObjects.RemoveAt(i);
            }
        }
    }


}
