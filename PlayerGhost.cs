using System.Collections.Generic;
using static SlughostMod.SlughostMod;
namespace SlughostMod;

//Maybe stop inheriting player to avoid incompatabilities??
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
            currentGhosts.Remove(this.abstractCreature);
        }
        CamCoordGetter to = new CamCoordGetter(this);
        CreateGhost(this, to.coord, to.world);
        //UnityEngine.Debug.Log("Scughost: My room unloaded!");
    }

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
    }
}
