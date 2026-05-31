using System.Collections.Generic;
using static SlughostMod.SlughostMod;
namespace SlughostMod;

public class PlayerGhost : Player, INotifyWhenRoomUnloaded
{
    public int ghostTeleTimer;
    public float flickerFactor;
    public bool destroyRespawn;
    
    public PlayerGhost(AbstractCreature abstractCreature, World world) : base(abstractCreature, world)
    {

        this.destroyRespawn = true;
        foreach (var chunk in bodyChunks)
        {
            chunk.collideWithObjects = false;
        }
        this.abstractCreature.tentacleImmune = true;
        this.objectInStomach = null;
        this.canBeHitByWeapons = false;
        ghostTeleTimer = 0;
        this.glowing = false;
    }

    ~PlayerGhost()
    {
        if(this.abstractCreature != null)
        {
            currentGhosts.Remove(this.abstractCreature);
        }
    }

    public void RoomUnloaded()
    {
        if(this.room != null)
        {
            this.room.RemoveObject(this);
            this.abstractCreature.Room.RemoveEntity(this.abstractCreature);
        }
        CamCoordGetter to = new CamCoordGetter(this);
        CreateGhost(this, to.coord, to.world);
        //UnityEngine.Debug.Log("Scughost: My room unloaded!");
    }

    //public override void Abstractize()
    //{
    //    UnityEngine.Debug.Log("Abstracting ghost!");
    //    this.Destroy();
    //    base.Abstractize();
    //}

    public void WarpAndRevive(WorldCoordinate toCoord, World toWorld)
    {
        AbstractCreature absGhost = this.abstractCreature;
        AbstractRoom absRoom = absGhost.Room;
        AbstractRoom newRoom = toWorld.GetAbstractRoom(toCoord.room);
        if (newRoom == null)
        {
            UnityEngine.Debug.Log("Tried to send ghost to null room!");
            return;
        }
        if (this.room != null && absRoom != newRoom) 
        {
            this.LoseAllGrasps(); //Drop items before
            List<AbstractPhysicalObject> allConnectedObjects = absGhost.GetAllConnectedObjects(); //Removing realizedCreature from room
            for (int i = 0; i < allConnectedObjects.Count; i++)
            {
                if (allConnectedObjects[i].realizedObject != null)
                {
                    this.room.RemoveObject(allConnectedObjects[i].realizedObject);
                }
            }
            if(this.room != null)
            {
                this.room.RemoveObject(this);
            }
            UnityEngine.Debug.Log("Warping ghost ghost to " + newRoom.name);
            if (absGhost.world != newRoom.world)
            {
                absGhost.world = newRoom.world;
                absGhost.pos = toCoord;
            }
            if (absRoom != null)
            {
                absRoom.RemoveEntity(absGhost);
            }
            newRoom.AddEntity(absGhost);
            absGhost.Move(toCoord);
            if (newRoom.realizedRoom == null)
            {
                newRoom.world.ActivateRoom(newRoom);
            }
            this.PlaceInRoom(newRoom.realizedRoom);
        }
        else
        {
            if(newRoom.realizedRoom == null)
            {
                newRoom.world.ActivateRoom(newRoom);
            }
            this.SpitOutOfShortCut(toCoord.Tile, newRoom.realizedRoom, false);
        }

        //if(this.room == null || room == null || this.playerState.permaDead || absGhost.world != newRoom.world) //Different room or null
        //{
        //    UnityEngine.Debug.Log("Reviving null / other room ghost to " + newRoom.name);
        //    if(absGhost.world != newRoom.world)
        //    {
        //        absGhost.world = newRoom.world;
        //        absGhost.pos = toCoord;
        //    }
        //    if (room != null)
        //    {
        //        room.RemoveEntity(absGhost);
        //    }
        //    newRoom.AddEntity(absGhost);
        //    absGhost.Move(toCoord);
        //    if (newRoom.realizedRoom == null)
        //    {
        //        newRoom.world.ActivateRoom(newRoom);
        //    }
        //    this.PlaceInRoom(newRoom.realizedRoom);
        //}
        //else //Same room
        //{
        //    if (newRoom.realizedRoom == null)
        //    {
        //        newRoom.world.ActivateRoom(newRoom);
        //    }
        //    //Spits player out at position chosen coord (room exit or camera followed player)
        //    UnityEngine.Debug.Log("Ghost tp to same room");
        //    this.SpitOutOfShortCut(toCoord.Tile, newRoom.realizedRoom, false);
        //}


        //if (this.playerState.permaDead || this.abstractCreature.world != newRoom.world)
        //{
        //    //Debug.Log("2");

        //    if (this.abstractCreature.world != newRoom.world)
        //    {
        //        this.abstractCreature.world = newRoom.world;
        //        this.abstractCreature.pos = tCoord;
        //        this.abstractCreature.Room.RemoveEntity(this.abstractCreature);
        //    }
        //    newRoom.AddEntity(this.abstractCreature);
        //    this.abstractCreature.Move(tCoord);
        //    this.PlaceInRoom(newRoom.realizedRoom);
        //}
        ////If ghost is in another room
        //else if (this.abstractCreature.Room.name != newRoom.name)
        //{
        //    //Debug.Log("3");

        //    if (newRoom == null)
        //    {
        //        this.abstractCreature.world.GetAbstractRoom(newRoom.name);
        //    }
        //    if (newRoom.realizedRoom == null)
        //    {
        //        newRoom.realizedRoom.game.world.ActivateRoom(newRoom);
        //    }
        //    this.room.RemoveObject(this);
        //    this.abstractCreature.Move(tCoord);
        //    this.PlaceInRoom(newRoom.realizedRoom);
        //    UnityEngine.Debug.Log("Ghost teleported from another room!");

        //}
    }
}
