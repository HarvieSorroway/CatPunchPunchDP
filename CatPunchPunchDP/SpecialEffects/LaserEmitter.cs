using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static CatPunchPunchDP.Modules.PunchFunc;

public class LaserEmitter : CosmeticSprite, SharedPhysics.IProjectileTracer
{
    public readonly float maxBiasAngle = 90f;
    public readonly float laserWidth = 1f;
    public float maxLaserWidth;

    public float deltaDegDir = 0f;
    public float totalDamage;
    public int life;
    public int currentLife;
    public bool burnDamage;

    public Vector2 dir;
    public Color laserColor = Color.red;

    Player player;
    SlugcatHand hand;

    SharedPhysics.CollisionResult collisionResult;
    bool getCollisionResultThisFrame;

    LightSource light;
    LightSource light_E;

    List<Creature> damagedCreatures = new List<Creature>();

    ChunkSoundEmitter soundEmitter;
    ChunkSoundEmitter soundEmitter2;

    public LaserEmitter(Room room, Player player, Vector2 dir, SlugcatHand slugcatHand, float totalDamage, bool burnDamage = false)
    {
        this.room = room;
        this.dir = dir;
        this.hand = slugcatHand;
        life = (int)(Custom.LerpMap(totalDamage, 0.05f, 5f, 10f, 40f) * (burnDamage ? 1.5f : 1f));
        maxLaserWidth = Custom.LerpMap(totalDamage, 0.05f, 5f, 2f, 8f) * (burnDamage ? 0.8f : 1f);
        this.burnDamage = burnDamage;
        this.totalDamage = totalDamage;
        this.player = player;

        pos = hand.pos;
        lastPos = hand.lastPos;

        soundEmitter = room.PlaySound(SoundID.Flare_Bomb_Burn, player.mainBodyChunk, false, 1f, burnDamage ? 1.5f : 1f);
        soundEmitter2 = room.PlaySound(SoundID.Moon_Wake_Up_Green_Swarmer_Flash, player.mainBodyChunk, false, burnDamage ? 0.6f : 0.4f, 1f);
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[3];
        sLeaser.sprites[0] = new CustomFSprite("pixel") { shader = rCam.game.rainWorld.Shaders["HologramBehindTerrain"], isVisible = false };
        for (int i = 0; i < 4; i++)
        {
            (sLeaser.sprites[0] as CustomFSprite).verticeColors[i] = Color.red;
        }

        sLeaser.sprites[1] = new FSprite("Futile_White", true);
        sLeaser.sprites[1].shader = rCam.game.rainWorld.Shaders["FlatLight"];

        sLeaser.sprites[2] = new FSprite("Futile_White", true);
        sLeaser.sprites[2].shader = rCam.game.rainWorld.Shaders["FlatLight"];

        light = new LightSource(hand.pos, false, Color.red, this);
        player.room.AddObject(light);

        light_E = new LightSource(hand.pos, false, Color.red, this);
        player.room.AddObject(light_E);

        AddToContainer(sLeaser, rCam, null);
    }

    public override void Update(bool eu)
    {
        if (slatedForDeletetion)
            return;

        base.Update(eu);

        if (currentLife >= life)
        {
            Destroy();
            return;
        }
        currentLife++;
        pos = hand.pos;

        PunchPlugin.Log(currentLife);
        TryMakeDamage();

        soundEmitter.volume = Mathf.Pow((1f - Mathf.InverseLerp(0, life, currentLife)),0.5f);
        soundEmitter2.volume = Mathf.Pow((1f - Mathf.InverseLerp(0, life / 6f, currentLife)), 0.5f) * (burnDamage ? 0.8f : 0.6f);
    }

    public void TryMakeDamage()
    {
        if (burnDamage)
        {
            if (getCollisionResultThisFrame)
            {
                if (collisionResult.chunk != null || collisionResult.onAppendagePos != null)
                {
                    Creature owner = collisionResult.chunk != null ? (collisionResult.chunk.owner as Creature) : (collisionResult.onAppendagePos.appendage.owner as Creature);
                    owner.Violence(player.mainBodyChunk, null, collisionResult.chunk, collisionResult.onAppendagePos, Creature.DamageType.Explosion, totalDamage / life, 1f);
                }
            }
        }
        else
        {
            List<Creature> tempList = new List<Creature>();
            foreach (var creature in (from physicalObject in player.room.physicalObjects[player.collisionLayer]
                                      where physicalObject is Creature && physicalObject != player && !(damagedCreatures.Contains(physicalObject as Creature)) && (PunchConfig.IgnoreSlugs() ? !(physicalObject is Player) : true)
                                      select physicalObject as Creature))
            {
                float maxAppendageDist = float.MaxValue;
                float maxChunkDist = float.MaxValue;

                PhysicalObject.Appendage targetAppendage = null;
                BodyChunk targetChunk = null;

                if (creature.appendages != null)
                {
                    foreach (var appendage in creature.appendages)
                    {
                        if (!appendage.canBeHit)
                            continue;

                        foreach (var pos in appendage.segments)
                        {
                            float lineDist = Mathf.Abs(Custom.DistanceToLine(pos, this.pos, this.pos + dir));
                            float dist = Custom.Dist(pos, this.pos);
                            if (lineDist < 10f && dist < maxAppendageDist)
                            {
                                maxAppendageDist = dist;
                                targetAppendage = appendage;
                            }
                        }
                    }
                }

                foreach (var chunk in creature.bodyChunks)
                {
                    float lineDist = Mathf.Abs(Custom.DistanceToLine(chunk.pos, this.pos, this.pos + dir));
                    float dist = (chunk.pos - player.mainBodyChunk.pos).magnitude;
                    if (lineDist < chunk.rad && dist < maxAppendageDist)
                    {
                        maxChunkDist = dist;
                        targetChunk = chunk;
                        targetAppendage = null;
                    }
                }

                if (targetChunk != null || targetAppendage != null)
                {
                    Creature owner = (Creature)(targetChunk != null ? targetChunk.owner : targetAppendage.owner);
                    owner.Violence(player.mainBodyChunk, null, targetChunk, null, Creature.DamageType.Explosion, totalDamage * (targetAppendage == null ? 1f : 0.5f), 1.5f);
                    tempList.Add(owner);
                }
            }

            if (tempList.Count > 0)
            {
                damagedCreatures.AddRange(tempList);
            }
        }
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

        if (slatedForDeletetion)
            return;

        float t = (1f - Mathf.InverseLerp(0, life, currentLife));
        float dynamicLaserWidth = t * maxLaserWidth;

        Vector2 pos = Vector2.Lerp(this.lastPos, this.pos, timeStacker);
        Vector2 perpDir = Custom.PerpendicularVector(dir);

        Vector2 output = pos + dir * 10000f;
        Vector2 corner = Vector2.zero;

        getCollisionResultThisFrame = true;
        collisionResult = new SharedPhysics.CollisionResult();

        try { collisionResult = SharedPhysics.TraceProjectileAgainstBodyChunks(this, room, pos, ref output, 5f, 1, null, true); }
        catch { getCollisionResultThisFrame = false; }
        finally { getCollisionResultThisFrame = collisionResult.hitSomething; }

        if (getCollisionResultThisFrame)
        {
            corner = collisionResult.collisionPoint;
        }

        var corner2 = Custom.RectCollision(pos, pos + dir * 100000f, rCam.room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
        IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(rCam.room, pos, corner2);
        if (intVector != null)
        {
            corner2 = Custom.RectCollision(corner2, pos, rCam.room.TileRect(intVector.Value)).GetCorner(FloatRect.CornerLabel.D);
        }

        if(Custom.Dist(pos,corner) > Custom.Dist(pos,corner2) || !collisionResult.hitSomething)
        {
            corner = corner2;
            getCollisionResultThisFrame = false;
        }

        (sLeaser.sprites[0] as CustomFSprite).MoveVertice(0, pos + perpDir * dynamicLaserWidth - camPos);
        (sLeaser.sprites[0] as CustomFSprite).MoveVertice(1, pos - perpDir * dynamicLaserWidth - camPos);
        (sLeaser.sprites[0] as CustomFSprite).MoveVertice(2, corner - perpDir * dynamicLaserWidth - camPos);
        (sLeaser.sprites[0] as CustomFSprite).MoveVertice(3, corner + perpDir * dynamicLaserWidth - camPos);

        sLeaser.sprites[1].SetPosition(pos - camPos);
        sLeaser.sprites[2].SetPosition(corner - camPos);

        Color color = Color.Lerp(Color.white, laserColor, Mathf.Pow(Mathf.InverseLerp(0, life, currentLife), 0.2f));
        for (int i = 0; i < 4; i++)
        {
            (sLeaser.sprites[0] as CustomFSprite).verticeColors[i] = color;
            (sLeaser.sprites[0] as CustomFSprite).verticeColors[i].a = Mathf.Pow(Mathf.InverseLerp(0, life, currentLife),0.2f);
        }

        for(int i = 1;i < 3; i++)
        {
            sLeaser.sprites[i].color = color;
            sLeaser.sprites[i].alpha = Mathf.Pow(t, 0.5f);
            sLeaser.sprites[i].scale = Mathf.Pow(t, 1.2f) * 1.5f * totalDamage;
        }

        light.color = color;
        light.pos = pos;
        light.alpha = t;
        light.rad = Mathf.Lerp(50f, 120f, Mathf.Pow(t, 1.2f)) * totalDamage;

        light_E.color = color;
        light_E.pos = corner;
        light_E.alpha = t;
        light_E.rad = Mathf.Lerp(50f, 120f, Mathf.Pow(t, 1.2f)) * totalDamage;

        foreach (var sprite in sLeaser.sprites)
        {
            sprite.isVisible = room == rCam.room;
        }
    }

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        if (newContatiner == null)
            newContatiner = rCam.ReturnFContainer("Foreground");
        foreach (var sprite in sLeaser.sprites)
        {
            sprite.RemoveFromContainer();
            newContatiner.AddChild(sprite);
        }
    }

    public override void Destroy()
    {
        light.Destroy();
        light_E.Destroy();
        soundEmitter.Destroy();
        soundEmitter2.Destroy();
        base.Destroy();
    }

    public bool HitThisObject(PhysicalObject obj)
    {
        return (obj is Creature) && burnDamage && obj != player && (PunchConfig.IgnoreSlugs() ? !(obj is Player) : true);
    }

    public bool HitThisChunk(BodyChunk chunk)
    {
        return burnDamage;
    }
}
