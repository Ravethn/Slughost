using System.Collections.Generic;
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
        this.canBeHitByWeapons = false;
        ghostTeleTimer = 0;
    }

    //public override void Abstractize()
    //{
    //    UnityEngine.Debug.Log("Abstracting ghost!");
    //    this.Destroy();
    //    base.Abstractize();
    //}

    public void WarpAndRevive(WorldCoordinate tCoord)
    {
        AbstractCreature absGhost = this.abstractCreature;
        AbstractRoom room = absGhost.Room;
        AbstractRoom newRoom = absGhost.world.GetAbstractRoom(tCoord.room);
        if (newRoom == null)
        {
            UnityEngine.Debug.Log("Tried to send ghost to null room!");
            return;
        }
        if (this.room != null && this.playerState.permaDead) //Drop items before
        {
            this.LoseAllGrasps();
            List<AbstractPhysicalObject> allConnectedObjects = absGhost.GetAllConnectedObjects();
            for (int i = 0; i < allConnectedObjects.Count; i++)
            {
                if (allConnectedObjects[i].realizedObject != null)
                {
                    this.room.RemoveObject(allConnectedObjects[i].realizedObject);
                }
            }
            if(this.room != null) //have to recheck null room in case it was connected to absGhost
            {
                this.room.RemoveObject(this);
            }
        }
        if(this.room == null || room == null || this.playerState.permaDead || absGhost.world != newRoom.world || room.name != newRoom.name) //Different room or null
        {
            UnityEngine.Debug.Log("Reviving null / other room ghost to " + newRoom.name);
            if(absGhost.world != newRoom.world)
            {
                absGhost.world = newRoom.world;
                absGhost.pos = tCoord;
            }
            if (room != null)
            {
                room.RemoveEntity(absGhost);
            }
            newRoom.AddEntity(absGhost);
            absGhost.Move(tCoord);
            if (newRoom.realizedRoom == null)
            {
                newRoom.world.ActivateRoom(newRoom);
            }
            this.PlaceInRoom(newRoom.realizedRoom);
        }
        else //Same room
        {
            if (newRoom.realizedRoom == null)
            {
                newRoom.world.ActivateRoom(newRoom);
            }
            //Spits player out at position chosen coord (room exit or camera followed player)
            UnityEngine.Debug.Log("Ghost tp to same room");
            this.SpitOutOfShortCut(tCoord.Tile, newRoom.realizedRoom, false);
        }
        this.playerState.permaDead = false;
        this.dead = false;

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
