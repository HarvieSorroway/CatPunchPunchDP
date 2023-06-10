using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class FriendlyPunchEffect : CosmeticSprite
{
    public FriendlyPunchEffect(Creature creature, Color color, float cof, Vector2? bias = null)
    {
        current_Pos = creature.mainBodyChunk.pos + (bias.HasValue ? bias.Value : Vector2.zero);
        last_Pos = creature.mainBodyChunk.pos + (bias.HasValue ? bias.Value : Vector2.zero);
        aim_Pos = creature.mainBodyChunk.pos + 60f * Vector2.up + (bias.HasValue ? bias.Value : Vector2.zero);

        sColor = color;
        sCof = cof;
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[2];

        plus_x = new FSprite("pixel", true) { scaleX = 30 * sCof, scaleY = 5 * sCof, color = sColor };
        plus_y = new FSprite("pixel", true) { scaleX = 5 * sCof, scaleY = 30 * sCof, color = sColor };

        sLeaser.sprites[0] = plus_x;
        sLeaser.sprites[1] = plus_y;

        AddToContainer(sLeaser, rCam, null);
        base.InitiateSprites(sLeaser, rCam);
    }

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        sLeaser.RemoveAllSpritesFromContainer();
        rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[0]);
        rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[1]);
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        plus_x.SetPosition(current_Pos - camPos);
        plus_y.SetPosition(current_Pos - camPos);

        plus_x.alpha = current_A;
        plus_y.alpha = current_A;

        current_Pos = Vector2.Lerp(last_Pos, aim_Pos, 0.05f);
        last_Pos = current_Pos;

        current_A = Mathf.Lerp(last_A, 0, 0.05f);
        last_A = current_A;

        if (current_A < 0.05f)
        {
            Destroy();
        }

        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
    }

    public override void Destroy()
    {
        plus_x.isVisible = false;
        plus_x.RemoveFromContainer();
        plus_y.isVisible = false;
        plus_y.RemoveFromContainer();
    }
    public Vector2 current_Pos;
    public Vector2 last_Pos;
    public Vector2 aim_Pos;

    public float current_A = 1f;
    public float last_A = 1f;

    public FSprite plus_x;
    public FSprite plus_y;

    Color sColor;
    float sCof;
}
