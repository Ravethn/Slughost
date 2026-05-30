using UnityEngine;

namespace Slughost
{
    internal class PlayerGhostGraphics : PlayerGraphics
    {
        public PlayerGhostGraphics(PhysicalObject ow) : base(ow)
        {

        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            for (int i = 0; i < 9; i++)
            {
                sLeaser.sprites[i].alpha = 0.5f;
                sLeaser.sprites[i].shader = rCam.game.rainWorld.Shaders["Hologram"];
            }
        }
    }
}
