using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace SlughostMod;

public partial class SlughostMod


{
    private static bool improvedInputEnabled = false;
    //Used to prevent ghosts from spawning and being saved in shelters
    private static bool forbidGhosts = false;

    //List of current scugghost AbstractCreatures
    //Used to teleport ghosts when a new region is loaded
    public static List<AbstractCreature> currentGhosts = new List<AbstractCreature>();

    public class CamCoordGetter
    {
        public WorldCoordinate coord;
        public World world;
        public CamCoordGetter(Player self)
        {
            //Outputs WorldCoordinate of player with camera,
            // or the closest exit that is in camera's room

            RainWorldGame rainWorldGame = self.abstractCreature.world.game;
            WorldCoordinate selfCoord = self.abstractCreature.pos;

            UnityEngine.Debug.Log("Current coord: " + selfCoord.ToString());
            float smallestDist = 0;
            int closestCamIndex = 0;
            WorldCoordinate? sendCoord = null;


            //Tests distance to all exits in rooms with cameras and gets smallest distance coord
            for (int i = 0; i < rainWorldGame.cameras.Length; i++)
            {
                if (rainWorldGame.cameras[i].followAbstractCreature == self.abstractCreature)
                {
                    continue;
                }
                UnityEngine.Debug.Log("At camera: " + i + ". Followed critter coord: " + (rainWorldGame.cameras[i].followAbstractCreature.pos).ToString());
                Room checkingCamRoom = rainWorldGame.cameras[i].room;
                for (int j = 0; j < checkingCamRoom.abstractRoom.connections.Length; j++)
                {
                    WorldCoordinate shortcutCoord = checkingCamRoom.LocalCoordinateOfNode(checkingCamRoom.abstractRoom.ExitIndex(checkingCamRoom.abstractRoom.connections[j]));
                    float distance = Custom.BetweenRoomsDistance(self.abstractCreature.world, selfCoord, shortcutCoord);
                    if (smallestDist == 0 || (distance <= smallestDist && distance > 0))
                    {
                        sendCoord = shortcutCoord;
                        closestCamIndex = i;
                        smallestDist = distance;
                    }
                }
            }

            //Send player to the closest cameras player
            AbstractCreature camCritter = rainWorldGame.cameras[closestCamIndex].followAbstractCreature;
            if (camCritter != null && camCritter != self.abstractCreature && camCritter.realizedCreature != null && !camCritter.realizedCreature.inShortcut && !camCritter.world.game.IsArenaSession)
            {
                coord = camCritter.pos;
                world = camCritter.world;
                UnityEngine.Debug.Log("Ghost spawn location at camera player: " + coord.ToString());
                UnityEngine.Debug.Log("At camera index: " + closestCamIndex.ToString());

            }
            //If camcritter dont exist sends to the closest pipe to ghost in camera room
            else if (sendCoord != null)
            {
                UnityEngine.Debug.Log("Smallest distance is: " + smallestDist.ToString() + " to pipe at: " + sendCoord.ToString());
                coord = (WorldCoordinate)sendCoord;
                world = rainWorldGame.cameras[closestCamIndex].room.world;
            }
            //Otherwise, choose random exit coord in camera room
            else
            {
                Room camRoom = self.abstractCreature.world.game.cameras[closestCamIndex].room;
                coord = camRoom.LocalCoordinateOfNode(camRoom.abstractRoom.ExitIndex(camRoom.abstractRoom.connections[UnityEngine.Random.Range(0, camRoom.abstractRoom.connections.Length)]));
                world = rainWorldGame.cameras[closestCamIndex].room.world;
            }
        }
    }

    public static void CreateGhost(Player self, WorldCoordinate coord, World world)
    {
        if (forbidGhosts)
        {
            return;
        }
        AbstractCreature newGhost = new AbstractCreature(world, StaticWorld.GetCreatureTemplate(MyModdedEnums.CreatureTemplateType.SlugcatGhost), null, coord, world.game.GetNewID());
        newGhost.state = new PlayerGhostState(newGhost, self.playerState.playerNumber, self.playerState.slugcatCharacter, false);
        if (ModManager.CoopAvailable)
        {
            (newGhost.state as PlayerGhostState).isPup = self.playerState.isPup;
        }
        currentGhosts.Add(newGhost);
        UnityEngine.Debug.Log("Added ghost to currentGhosts!");
        AbstractRoom targetRoom = world.GetAbstractRoom(coord);
        if(targetRoom == null)
        {
            UnityEngine.Debug.Log("Tried to spawn ghost in null room!");
            return;
        }
        if (targetRoom != null)
        {
            targetRoom.AddEntity(newGhost);
            if (targetRoom.realizedRoom != null)
            {
                newGhost.RealizeInRoom();
            }
        }
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
        StaticWorld.EstablishRelationship(CreatureTemplate.Type.TentaclePlant, MyModdedEnums.CreatureTemplateType.SlugcatGhost, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.DoesntTrack, 1f));
        StaticWorld.EstablishRelationship(CreatureTemplate.Type.MirosBird, MyModdedEnums.CreatureTemplateType.SlugcatGhost, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.DoesntTrack, 1f));
        StaticWorld.EstablishRelationship(CreatureTemplate.Type.Leech, MyModdedEnums.CreatureTemplateType.SlugcatGhost, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.DoesntTrack, 1f));
        StaticWorld.EstablishRelationship(CreatureTemplate.Type.SeaLeech, MyModdedEnums.CreatureTemplateType.SlugcatGhost, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.DoesntTrack, 1f));
        StaticWorld.EstablishRelationship(CreatureTemplate.Type.DropBug, MyModdedEnums.CreatureTemplateType.SlugcatGhost, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.DoesntTrack, 1f));
        StaticWorld.EstablishRelationship(CreatureTemplate.Type.Centipede, MyModdedEnums.CreatureTemplateType.SlugcatGhost, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.DoesntTrack, 1f));
        if (ModManager.MSC)
        {
            StaticWorld.EstablishRelationship(DLCSharedEnums.CreatureTemplateType.JungleLeech, MyModdedEnums.CreatureTemplateType.SlugcatGhost, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.DoesntTrack, 1f));
            StaticWorld.EstablishRelationship(DLCSharedEnums.CreatureTemplateType.StowawayBug, MyModdedEnums.CreatureTemplateType.SlugcatGhost, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.DoesntTrack, 1f));
            StaticWorld.EstablishRelationship(DLCSharedEnums.CreatureTemplateType.MirosVulture, MyModdedEnums.CreatureTemplateType.SlugcatGhost, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.DoesntTrack, 1f));
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


    #region MapIcon Code
    private static Color CreatureSymbolOnColorOfCreature(On.CreatureSymbol.orig_ColorOfCreature orig, IconSymbol.IconSymbolData iconData)
    {
        if (iconData.critType == MyModdedEnums.CreatureTemplateType.SlugcatGhost && iconData.critType.index != -1)
        {
            Color playerColor = PlayerGraphics.DefaultSlugcatColor(SlugcatStats.Name.ArenaColor(iconData.intData));
            return playerColor.CloneWithNewAlpha(0.5f);
        }
        return orig(iconData);
    }

    private static string CreatureSymbolOnSpriteNameOfCreature(On.CreatureSymbol.orig_SpriteNameOfCreature orig, IconSymbol.IconSymbolData iconData)
    {
        if (iconData.critType == MyModdedEnums.CreatureTemplateType.SlugcatGhost && iconData.critType.index != -1)
        {
            return "Kill_Slugcat";
        }
        return orig(iconData);
    }

    private static IconSymbol.IconSymbolData CreatureSymbolOnSymbolDataFromCreature(On.CreatureSymbol.orig_SymbolDataFromCreature orig, AbstractCreature creature)
    {
        if (creature.creatureTemplate.type == MyModdedEnums.CreatureTemplateType.SlugcatGhost)
        {
            return new IconSymbol.IconSymbolData(creature.creatureTemplate.type, AbstractPhysicalObject.AbstractObjectType.Creature, (creature.state as PlayerGhostState).playerNumber);
        }
        return orig(creature);
    }

    private void MapILDraw(ILContext il)
    {
        try
        {

            //Adds slugcat ghost check to method to not skip coloring ghost icon
            var c = new ILCursor(il);
            var d = new ILCursor(il);
            c.GotoNext(
                i => i.MatchLdloc(11),
                i => i.MatchLdfld<AbstractCreature>(nameof(AbstractCreature.creatureTemplate)),
                i => i.MatchLdfld<CreatureTemplate>(nameof(CreatureTemplate.type)),
                i => i.MatchLdsfld<CreatureTemplate.Type>(nameof(CreatureTemplate.Type.Slugcat)),
                i => i.MatchCall(typeof(ExtEnum<CreatureTemplate.Type>).GetMethod("op_Equality")),
                i => i.MatchBrfalse(out _)
                );

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 11);
            c.EmitDelegate((HUD.Map self, AbstractCreature abstractCreature) =>
            {
                if (abstractCreature.creatureTemplate.type == MyModdedEnums.CreatureTemplateType.SlugcatGhost)
                {
                    self.creatureSymbols[self.creatureSymbols.Count - 1].myColor = RainWorld.PlayerObjectBodyColors[(abstractCreature.realizedCreature as PlayerGhost).playerState.playerNumber];
                }
            });

        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    private static HUD.Map.ItemMarker.ItemMakerData? ItemMakerDataOnDataFromAbstractPhysical(On.HUD.Map.ItemMarker.ItemMakerData.orig_DataFromAbstractPhysical orig, AbstractPhysicalObject obj)
    {
        if (obj is AbstractCreature && (obj as AbstractCreature).creatureTemplate.type == MyModdedEnums.CreatureTemplateType.SlugcatGhost)
        {
            return null;
        }
        return orig(obj);
    }

    private static HUD.Map.ShelterMarker.ItemInShelterMarker.ItemInShelterData? ItemInShelterDataOnDataFromAbstractPhysical(On.HUD.Map.ShelterMarker.ItemInShelterMarker.ItemInShelterData.orig_DataFromAbstractPhysical orig, AbstractPhysicalObject obj)
    {
        if (obj is AbstractCreature && (obj as AbstractCreature).creatureTemplate.type == MyModdedEnums.CreatureTemplateType.SlugcatGhost)
        {
            return null;
        }
        return orig(obj);
    }
    #endregion


    private void PlayerOnInitiateGraphicsModule(On.Player.orig_InitiateGraphicsModule orig, Player self)
    {
        if(self is PlayerGhost)
        {
            self.graphicsModule = new PlayerGhostGraphics(self);
        }
        else
        {
            orig(self);
        }
    }

    //Updates HUD for Game Over text so that keyboard does use SPACEBAR for restart so keyboard ghost can use map in gameover mode
    private void HUDTextPromptOnCtor(On.HUD.TextPrompt.orig_ctor orig, HUD.TextPrompt self, HUD.HUD hud)
    {
        orig(self, hud);
        for(int i = 0; i < self.defaultPauseControls.Length; i++)
        {
            self.defaultMapControls[i] = false;
        }
        self.UpdateGameOverString(self.lastControllerType);
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
        //Avoid calling self.room here unless checking for null because it is null whenever player is dragged into den


        if (!self.isNPC && !self.dead && self.abstractCreature != null && !self.abstractCreature.world.game.setupValues.invincibility && !forbidGhosts && !self.playerState.isGhost && !(self is PlayerGhost))
        {
            ////Creates ghost slugcat
            CreateGhost(self, self.abstractCreature.pos, self.abstractCreature.world);
        }
        orig(self);
    }

    private void PlayerOnDestroy(On.Player.orig_Destroy orig, Player self)
    {
        UnityEngine.Debug.Log("DESTROY RUN");
        if (self is PlayerGhost)
        {
            //FOR GHOST DEATH
            //Ghost deleted spawns new ghost (separate to make sure ghosts that somehow died are respawned when destroyed)
            //New ghost doesn't need to be created when a ghost permadies since it does not delete itself
            if (!forbidGhosts)
            {
                for(int i = 0; i < currentGhosts.Count; i++)
                {
                    if(self.playerState.playerNumber == (currentGhosts[i].state as PlayerGhostState).playerNumber)
                    {
                        if (currentGhosts.Remove(self.abstractCreature))
                        {
                            UnityEngine.Debug.Log("Removed a scughost from currentGhosts!");
                        }
                    }
                }

                CamCoordGetter to = new CamCoordGetter(self);
                if((self as PlayerGhost).destroyRespawn)
                {
                    CreateGhost(self, to.coord, to.world);
                }
            }
        }
        else if (!ModManager.CoopAvailable && !forbidGhosts && !self.isNPC && !self.playerState.isGhost && !self.playerState.dead)
        {
            //FOR PLAYER DEATH
            //Back up case for when not coopavailable since with without it, does not permadie player and just deletes them instantly
            //If player deleted spawns new ghost
            CamCoordGetter to = new CamCoordGetter(self);
            CreateGhost(self, to.coord, to.world);
        }
        orig(self);
    }

    private void PlayerOnUpdate(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        if(self is PlayerGhost)
        {
            PlayerGhost ghost = self as PlayerGhost;
            if (forbidGhosts)
            {
                //Might be able to get rid of this variable and replace it with hook into ShelterDoor.DoorClosed to delete there
                ghost.destroyRespawn = false;
                ghost.Destroy();
            }
            //Checks if player has been revived, or is alive again (i'm not sure why check for inShortcut??)
            if (!self.inShortcut)
            {
                foreach (AbstractCreature absPlayer in self.abstractCreature.world.game.AlivePlayers)
                {
                    if (absPlayer.realizedCreature != null && (absPlayer.realizedCreature as Player).playerState.playerNumber == self.playerState.playerNumber)
                    {
                        //Deletes ghost if the player is revived
                        //Need to make a cool deletion effect for this
                        ghost.destroyRespawn = false;
                        ghost.Destroy();
                    }
                }


                //Stop ghost from being able to eat
                //Always checked since eating logic continues even while button is not pressed
                if (self.eatCounter < 15)
                {
                    self.eatCounter = 15;
                }
            }

            //Warping ghost to player with camera
            if (IsGhostTeleportPressed(ghost))
            {
                if(ghost.ghostTeleTimer <= 45)
                {
                    ghost.ghostTeleTimer++;
                    if(ghost.ghostTeleTimer > 15)
                    {
                        self.Blink(5);
                    }
                }
                //Stop ghost with grabbing ability from swallowing items and accidentally deleting them
                self.swallowAndRegurgitateCounter = 0;
                
                

                //Debug.Log("GhostTeleTimer: " + (self as PlayerGhost).ghostTeleTimer.ToString());
                if (ghost.ghostTeleTimer == 45)
                {
                    CamCoordGetter to = new CamCoordGetter(self);
                    ghost.WarpAndRevive(to.coord, to.world);
                    Color playerColor = RainWorld.PlayerObjectBodyColors[self.playerState.playerNumber];
                    Vector2 playerPos = self.mainBodyChunk.pos;
                    //Teleporting effects
                    if (self.room != null)
                    {
                        self.room.PlaySound(SoundID.Overseer_Image_Big_Flicker, playerPos, UnityEngine.Random.Range(1.2f, 1.6f), 1f, self.abstractCreature);
                        self.room.PlaySound(SoundID.Player_Tick_Along_In_Shortcut, playerPos, 2f, 1f, self.abstractCreature);
                        self.room.AddObject(new ShockWave(self.bodyChunks[1].pos, 80f, UnityEngine.Random.Range(0.01f, 0.05f), 7, false));
                        for (int i = 0; i < 10; i++)
                        {
                            //Arti-sparks
                            Vector2 angle = Custom.RNV();
                            self.room.AddObject(new Spark(playerPos + angle * (UnityEngine.Random.value * 40f), angle * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), playerColor, null, 4, 18));
                        }
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

            //Makes it so ghosts are not ever considered winning
            self.readyForWin = false;
        }
    }

    private void PlayerOnTriggerCameraSwitch(On.Player.orig_TriggerCameraSwitch orig, Player self)
    {
        //If there are alive players, ghost cannot use camera
        if (self is PlayerGhost && self.abstractCreature.world.game.AlivePlayers.Count >= self.abstractCreature.world.game.cameras.Length)
        {
            return;
        }
        //While all players dead, dead players cannot call the camera only ghosts
        else if (self.dead && self.abstractCreature.world.game.AlivePlayers.Count == 0)
        {
            return;
        }
        //If cycling is on and camera is on self, then orig will return early and point to toward player with next player number
        //Using RoomCameraOnChangeCameraToPlayer hook to catch it
        orig(self);
    }

    private void RoomCameraOnChangeCameraToPlayer(On.RoomCamera.orig_ChangeCameraToPlayer orig, RoomCamera self, AbstractCreature cameraTarget)
    {
        if (cameraTarget != null)
        {
            //Ghost cycling
            //Player.TriggerCameraSwitch sends actual player with next playernumber, tries to convert that to its corresponding ghost
            if (self.game.AlivePlayers.Count == 0 && cameraTarget.state.dead && Custom.rainWorld.options.cameraCycling)
            {
                UnityEngine.Debug.Log("We have a total of: " + currentGhosts.Count.ToString() + " scug ghosts!");
                if (currentGhosts.Count > 0)
                {
                    //Find related slughost to target instead
                    for (int i = 0; i < currentGhosts.Count; i++)
                    {
                        if ((currentGhosts[i].state as PlayerState).playerNumber == (cameraTarget.state as PlayerState).playerNumber)
                        {
                            cameraTarget = currentGhosts[i];
                            break;
                        }
                    }
                    //int index = (ghostTargetIndex + 1) % currentGhosts.Count;
                    //cameraTarget = currentGhosts[index];
                    

                }
            }
        }
        orig(self, cameraTarget);
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

    private bool MirosBirdAIOnDoIWantToBiteCreature(On.MirosBirdAI.orig_DoIWantToBiteCreature orig, MirosBirdAI self, AbstractCreature creature)
    {
        if (creature.creatureTemplate.type == MyModdedEnums.CreatureTemplateType.SlugcatGhost)
        {
            return false;
        }
        return orig(self, creature);
    }

    private void MirosBirdOnJawSlamShut(On.MirosBird.orig_JawSlamShut orig, MirosBird self)
    {
        orig(self);
    }

    private void SlugOnBackOnChangeOverlap(On.Player.SlugOnBack.orig_ChangeOverlap orig, Player.SlugOnBack self, bool newOverlap)
    {
        orig(self, newOverlap);
        if(self.slugcat is PlayerGhost)
        {
            //Makes sure that when Slugcat Ghost stays not colliding with objects when leaving a slugback
            self.slugcat.CollideWithObjects = false;
            self.slugcat.canBeHitByWeapons = false;
        }
        
    }

    private void GhostCreatureSedaterILUpdate(ILContext il)
    {
        try
        {
            
            //Checks if creature is Slugcat Ghost to skip ghost dream cycle stun 
            var c = new ILCursor(il);
            var d = new ILCursor(il);
            ILLabel skipLabel = d.DefineLabel();
            c.GotoNext(
                i => i.MatchLdloc(0),
                i => i.MatchCallvirt(typeof(List<AbstractCreature>).GetProperty("Item").GetGetMethod()),
                i => i.MatchLdfld<AbstractCreature>(nameof(AbstractCreature.creatureTemplate)),
                i => i.MatchLdfld<CreatureTemplate>(nameof(CreatureTemplate.type)),
                i => i.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType>(nameof(MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC)),
                i => i.MatchCall(typeof(ExtEnum<CreatureTemplate.Type>).GetMethod("op_Inequality")),
                i => i.MatchBrfalse(out _)
                );
            d.GotoNext(
                i => i.MatchLdloc(0),
                i => i.MatchLdcI4(1),
                i => i.MatchAdd(),
                i => i.MatchStloc(0)
                );
            d.MarkLabel(skipLabel);
            c.Index += 7;

            c.Emit(OpCodes.Ldarg_0); //calling self (GhostCreatureSedater)
            c.Emit(OpCodes.Ldloc_0); //local int32 for loop index
            c.EmitDelegate((GhostCreatureSedater ghostCreatureSedater, Int32 index) =>
            {
                return ghostCreatureSedater.room.abstractRoom.creatures[index].creatureTemplate.type != MyModdedEnums.CreatureTemplateType.SlugcatGhost;
            });
            c.Emit(OpCodes.Brfalse, skipLabel);

        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    private void StowawayBugILUpdate(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var d = new ILCursor(il);
            ILLabel skipLabel = d.DefineLabel();
            Type[] matchTypes = new Type[] {typeof(Vector2), typeof(Vector2), typeof(float)};
            d.GotoNext(
                i => i.MatchLdloc(10),
                i => i.MatchLdcI4(1),
                i => i.MatchAdd(),
                i => i.MatchStloc(10),
                i => i.MatchLdarg(0)
                );
            d.MarkLabel(skipLabel);
            c.GotoNext(
                i => i.MatchLdloc(10),
                i => i.MatchLdelemRef(),
                i => i.MatchLdfld<BodyChunk>(nameof(BodyChunk.rad)),
                i => i.MatchLdcR4(16),
                i => i.MatchAdd(),
                i => i.MatchCall(typeof(Custom).GetMethod("DistLess", matchTypes)),
                i => i.MatchBrfalse(out _)
                );

            c.Index += 7;
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 9);
            c.EmitDelegate((MoreSlugcats.StowawayBug self, Int32 num2) =>
            {
                return self.room.abstractRoom.creatures[num2].creatureTemplate.type == MyModdedEnums.CreatureTemplateType.SlugcatGhost;
            });
            c.Emit(OpCodes.Brtrue, skipLabel);

        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    private void NoiseTrackerOnHeardNoise(On.NoiseTracker.orig_HeardNoise orig, NoiseTracker self, Noise.InGameNoise noise)
    {
        if (noise.sourceObject is Creature && (noise.sourceObject as Creature).Template.type == MyModdedEnums.CreatureTemplateType.SlugcatGhost)
        {
            return;
        }
        orig(self, noise);
    }

    private void JellyFishILUpdate(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var d = new ILCursor(il);
            ILLabel skipLabel = d.DefineLabel();

            d.GotoNext(
                i => i.MatchLdloc(14),
                i => i.MatchLdcI4(1),
                i => i.MatchAdd(),
                i => i.MatchStloc(14)
                );

            d.MarkLabel(skipLabel);

            c.GotoNext(
                i => i.MatchCallvirt(typeof(AbstractCreature).GetProperty("realizedCreature").GetGetMethod()),
                i => i.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room)),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room)),
                i => i.MatchBneUn(out _)
                );
            c.Index += 5;

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 14);
            c.EmitDelegate((JellyFish self, int num4) =>
            {
                return self.room.abstractRoom.creatures[num4].realizedCreature.Template.type == MyModdedEnums.CreatureTemplateType.SlugcatGhost;
            });
            c.Emit(OpCodes.Brtrue, skipLabel);

        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    private bool BigJellyFishOnValidGrabCreature(On.MoreSlugcats.BigJellyFish.orig_ValidGrabCreature orig, MoreSlugcats.BigJellyFish self, AbstractCreature abs)
    {
        if(abs.creatureTemplate.type == MyModdedEnums.CreatureTemplateType.SlugcatGhost)
        {
            return false;
        }
        return orig(self, abs);
    }

    private void BigJellyFishOnHeardNoise(On.MoreSlugcats.BigJellyFish.orig_HeardNoise orig, MoreSlugcats.BigJellyFish self, Noise.InGameNoise noise)
    {
        if(noise.sourceObject is Creature && (noise.sourceObject as Creature).Template.type == MyModdedEnums.CreatureTemplateType.SlugcatGhost)
        {
            return;
        }
        orig(self, noise);
    }

}
