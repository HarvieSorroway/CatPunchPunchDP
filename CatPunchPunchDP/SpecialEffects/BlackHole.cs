
using MoreSlugcats;
using Noise;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class BlackHole : CosmeticSprite
{
    public Color effectColor = Color.blue;

    Vector2 startVel;

    int floatingLife;
    int life;

    float rad = 250f;

    private float size;
    private float sizeMul;
    private float intensity;
    public bool highLayer;
    public BlackHole(Room room,Vector2 startVel, Vector2 startPos, int floatingLife)
    {
        this.startVel = startVel;
        this.room = room;
        this.pos = startPos;
        this.floatingLife = floatingLife;
        intensity = 0.2f;
        size = 100f;
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[3];
        sLeaser.sprites[0] = new FSprite("Futile_White", true) { isVisible = true};
        sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders["ShockWave"];
        sLeaser.sprites[0].color = new Color(0f, 0.5f, 1f);
        sLeaser.sprites[1] = new FSprite("Futile_White", true)
        {
            shader = rCam.room.game.rainWorld.Shaders["FlatLight"]
        };
        sLeaser.sprites[2] = new FSprite("Circle20", true);
        AddToContainer(sLeaser, rCam, null);
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (slatedForDeletetion)
            return;
        if(life >= floatingLife)
        {
            Explode();
            Destroy();
            return;
        }
        pos += startVel / 40f * Mathf.InverseLerp(floatingLife, 0f, life) * 2f;

        sizeMul = life < 40 ? Mathf.InverseLerp(0,40,life) : Mathf.InverseLerp(floatingLife ,floatingLife - 40,life);

        for (int i = 0; i < 5; i++)
        {
            if(Random.value < 0.1f)
            {
                Vector2 dir = Custom.RNV();

                Vector2 corner = Custom.RectCollision(pos, pos + dir * 100000f, room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
                IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(room, pos, corner);
                if (intVector != null)
                {
                    corner = Custom.RectCollision(corner, pos, room.TileRect(intVector.Value)).GetCorner(FloatRect.CornerLabel.D);
                }
                corner = Vector2.ClampMagnitude(corner - pos, rad) + pos;

                var Lightning = new LightningBolt(pos, corner, 0, 0.4f, 0.35f, 0.64f, 0.64f, true);
                Lightning.intensity = 1f;
                Lightning.color = Color.blue;

                room.AddObject(Lightning);
                room.PlaySound(SoundID.SS_AI_Halo_Connection_Light_Up, pos, 2f, 1f);
            }
        }

        for (int n = 0; n < room.physicalObjects.Length; n++)
        {
            for (int num = 0; num < room.physicalObjects[n].Count; num++)
            {
                for (int num2 = 0; num2 < room.physicalObjects[n][num].bodyChunks.Length; num2++)
                {
                    BodyChunk bodyChunk2 = room.physicalObjects[n][num].bodyChunks[num2];
                    if (bodyChunk2.owner is Player)
                        continue;
                    if (Vector2.Distance(pos, bodyChunk2.pos) < 250f)
                    {
                        bodyChunk2.vel += (pos - bodyChunk2.pos) * bodyChunk2.mass * 0.01f;
                        if (bodyChunk2.vel.magnitude > 20f)
                        {
                            bodyChunk2.vel = bodyChunk2.vel.normalized * 20f;
                        }
                    }
                }
            }
        }


        life++;
    }

    public void Explode()
    {
        this.room.AddObject(new SingularityBomb.SparkFlash(pos, 300f, new Color(0f, 0f, 1f)));
        this.room.AddObject(new Explosion(this.room, null, pos, 7, rad, 10f, 20f, 280f, 0.25f, null, 0.3f, 160f, 1f));
        this.room.AddObject(new Explosion.ExplosionLight(pos, 280f, 1f, 7, this.effectColor));
        this.room.AddObject(new Explosion.ExplosionLight(pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
        this.room.AddObject(new Explosion.ExplosionLight(pos, 2000f, 2f, 60, this.effectColor));
        this.room.AddObject(new ShockWave(pos, 350f, 0.485f, 100, true));

        this.room.PlaySound(SoundID.Bomb_Explode, pos);
        this.room.InGameNoise(new InGameNoise(pos, 9000f, null, 1f));
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (slatedForDeletetion)
            return;

        Vector2 smoothPos = Vector2.Lerp(this.lastPos, this.pos, timeStacker) - camPos;
        sLeaser.sprites[0].SetPosition(smoothPos);
        sLeaser.sprites[1].SetPosition(smoothPos);
        sLeaser.sprites[2].SetPosition(smoothPos);

        float num = Mathf.Sin(Time.time * 2f) / 4f + 0.6f;
        sLeaser.sprites[0].color = new Color(Mathf.Pow(num, 0.1f), intensity, num);
        sLeaser.sprites[0].scale = sizeMul *  Mathf.Pow(num, 0.5f) * size / 8f;
        sLeaser.sprites[1].scale = sizeMul * Mathf.Pow(num, 0.5f) * size / 16f;
        sLeaser.sprites[2].scale = sizeMul;
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
    }

    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        sLeaser.sprites[1].color = effectColor;
        sLeaser.sprites[2].color = palette.blackColor;
    }

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[0]);
        rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[1]);
        rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[2]);
    }
}