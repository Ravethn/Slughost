using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SlughostMod;

public class PlayerGhost : Player
{
    public int ghostTeleTimer;
    
    public PlayerGhost(AbstractCreature abstractCreature, World world) : base(abstractCreature, world)
    {
        
        foreach (var chunk in bodyChunks)
        {
            chunk.collideWithObjects = false;
        }
        this.abstractCreature.tentacleImmune = true;
        this.objectInStomach = null;
        ghostTeleTimer = 0;
    }


    public static void WarpAndReviveGhost(PlayerGhost self, AbstractRoom newRoom, WorldCoordinate position)
    {
        //JollyCustom.WarpAndRevivePlayer()
        //
        if (self.playerState.permaDead && self != null && self.room != null)
        {
            //Debug.Log("1");
            
            self.room.RemoveObject(self.abstractCreature.realizedCreature);
            if (self.grasps[0] != null)
            {
                self.ReleaseGrasp(0);
            }
            if (self.grasps[1] != null)
            {
                self.ReleaseGrasp(1);
            }
            List<AbstractPhysicalObject> allConnectedObjects = self.abstractCreature.GetAllConnectedObjects();
            if(self.room != null)
            {
                for (int i = 0; i < allConnectedObjects.Count; i++)
                {
                    if (allConnectedObjects[i].realizedObject != null)
                    {
                        self.room.RemoveObject(allConnectedObjects[i].realizedObject);
                    }
                }
            }
            

        }
        if(self == null || self.room == null || self.playerState.permaDead || self.abstractCreature.world != newRoom.world)
        {
            //Debug.Log("2");
            
            if(self.abstractCreature.world != newRoom.world)
            {
                self.abstractCreature.world = newRoom.world;
                self.abstractCreature.pos = position;
                self.abstractCreature.Room.RemoveEntity(self.abstractCreature);
            }
            newRoom.AddEntity(self.abstractCreature);
            self.abstractCreature.Move(position);
            self.abstractCreature.realizedCreature.PlaceInRoom(newRoom.realizedRoom);
        }
        //If ghost is in another room
        else if (self.abstractCreature.Room.name != newRoom.name)
        {
            //Debug.Log("3");
            
            if(newRoom == null)
            {
                self.abstractCreature.world.GetAbstractRoom(newRoom.name);
            }
            if(newRoom.realizedRoom == null)
            {
                newRoom.realizedRoom.game.world.ActivateRoom(newRoom);
            }
            self.room.RemoveObject(self);
            self.abstractCreature.Move(position);
            self.PlaceInRoom(newRoom.realizedRoom);
            Debug.Log("Ghost teleported from another room!");
            
        }
        //If ghost is in the same room
        else if (self.abstractCreature.Room.name == newRoom.name && newRoom.realizedRoom != null)
        {
            if (self.firstChunk.pos != self.room.game.RealizedPlayerFollowedByCamera.firstChunk.pos)
            {
                Debug.Log("Ghost tp to camera in same room");
                for (int i = 0; i < self.bodyChunks.Length; i++)
                {
                    self.bodyChunks[i].pos = self.room.game.RealizedPlayerFollowedByCamera.firstChunk.pos;
                }
            }
            else if (newRoom.realizedRoom != null)
            {
                Debug.Log("Ghost tp to shortcut spot");
                self.SpitOutOfShortCut(new RWCustom.IntVector2(position.x, position.y), newRoom.realizedRoom, false);
            }


        }
        self.playerState.permaDead = false;
        self.dead = false;
        self.exhausted = false;
    }
}
