using UnityEngine;

namespace SlughostMod
{
    internal class PlayerGhostGraphics : PlayerGraphics
    {
        LightSource ghostLightSource;

        public PlayerGhostGraphics(PhysicalObject ow) : base(ow)
        {

        }

        public override void Update()
        {
            if(this.ghostLightSource != null)
            {
                this.ghostLightSource.stayAlive = true;
                this.ghostLightSource.setPos = new Vector2?(this.player.mainBodyChunk.pos);
                if(this.ghostLightSource.slatedForDeletetion || this.player.room.Darkness(this.player.mainBodyChunk.pos) == 0f || this.ghostLightSource.room != this.player.room)
                {
                    this.ghostLightSource = null;
                }
            }
            else if(this.player.room.Darkness(this.player.mainBodyChunk.pos) > 0f && !this.player.DreamState)
            {
                this.ghostLightSource = new LightSource(this.player.mainBodyChunk.pos, false, Color.Lerp(new Color(1f, 1f, 1f), PlayerGraphics.SlugcatColor(this.CharacterForColor), 0.5f), this.player);
                this.ghostLightSource.requireUpKeep = true;
                this.ghostLightSource.setRad = new float?(100f);
                this.ghostLightSource.setAlpha = new float?(1f);
                this.ghostLightSource.shaderDirty = true;
                this.player.room.AddObject(this.ghostLightSource);
            }
            base.Update();
        }

        //Ghost transparency
        //Want to rework to be a semi-transparent scug instead of flickering Hologram
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            for (int ghostSprite = 0; ghostSprite < 9; ghostSprite++)
            {
                sLeaser.sprites[ghostSprite].shader = rCam.game.rainWorld.Shaders["Hologram"];
                sLeaser.sprites[ghostSprite].alpha = 0.93f;
            }
        }
    }
}
