using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static SporePlant;

namespace CatPunchPunchDP.Modules
{
    public static class BeeModuleManager
    {
        public static ConditionalWeakTable<SporePlant.Bee, BeeModule> beeModules = new ConditionalWeakTable<SporePlant.Bee, BeeModule>();
        public static ConditionalWeakTable<AttachedBee, BeeModule.AttachedBeeModule> attachedBeeModules = new ConditionalWeakTable<AttachedBee, BeeModule.AttachedBeeModule>();
        public static void HookOn()
        {
            On.SporePlant.Bee.Update += Bee_Update;
            On.SporePlant.Bee.LookForRandomCreatureToHunt += Bee_LookForRandomCreatureToHunt;
            On.SporePlant.Bee.Attach += Bee_Attach;
        }

        private static void Bee_Attach(On.SporePlant.Bee.orig_Attach orig, Bee self, BodyChunk chunk)
        {
            if (beeModules.TryGetValue(self, out var module))
            {
                module.Attach(self, chunk);
            }
            else
                orig.Invoke(self, chunk);
        }

        private static bool Bee_LookForRandomCreatureToHunt(On.SporePlant.Bee.orig_LookForRandomCreatureToHunt orig, Bee self)
        {
            if (beeModules.TryGetValue(self, out var module))
            {
                return module.LookForRandomCreatureToHunt(self);
            }
            else
                return orig.Invoke(self);
        }

        private static void Bee_Update(On.SporePlant.Bee.orig_Update orig, Bee self, bool eu)
        {
            if (beeModules.TryGetValue(self, out var module))
            {
                module.Update(self, eu);
            }
            else
                orig.Invoke(self, eu);
        }

        public static void AddToModule(Bee bee,EntityID playerID)
        {
            beeModules.Add(bee,new BeeModule(bee, playerID));
        }

        public static void AddToModule(AttachedBee attachedBee)
        {
            attachedBeeModules.Add(attachedBee, new BeeModule.AttachedBeeModule(attachedBee));
        }
    }
    public class BeeModule
    {
        public WeakReference<Bee> beeRef;
        public EntityID playerID;

        public BeeModule(Bee bee, EntityID playerID)
        {
            beeRef = new WeakReference<Bee>(bee);
            
            this.playerID = playerID;
        }

        public void Update(Bee bee, bool eu)
        {
            if (bee.slatedForDeletetion)
            {
                return;
            }
            bee.evenUpdate = eu;

            bee.inModeCounter++;
            bee.lastLastLastPos = bee.lastLastPos;
            bee.lastLastPos = bee.lastPos;
            bee.lastPos = bee.pos;
            bee.pos += bee.vel;
            bee.vel *= 0.9f;
            bee.flyDir.Normalize();
            bee.lastFlyDir = bee.flyDir;
            bee.vel += bee.flyDir * bee.flySpeed;
            bee.flyDir += Custom.RNV() * UnityEngine.Random.value * ((bee.mode != SporePlant.Bee.Mode.LostHive) ? 0.6f : 1.2f);
            bee.lastBlink = bee.blink;
            bee.blink += bee.blinkFreq;
            bee.lastBoostTrail = bee.boostTrail;
            bee.boostTrail = Mathf.Max(0f, bee.boostTrail - 0.3f);
            SharedPhysics.TerrainCollisionData terrainCollisionData = new SharedPhysics.TerrainCollisionData(bee.pos, bee.lastPos, bee.vel, 1f, new IntVector2(0, 0), true);
            SharedPhysics.VerticalCollision(bee.room, terrainCollisionData);
            SharedPhysics.HorizontalCollision(bee.room, terrainCollisionData);
            bee.pos = terrainCollisionData.pos;
            bee.vel = terrainCollisionData.vel;

            bee.life -= 1f / bee.lifeTime;

            if (bee.life < 0.2f * UnityEngine.Random.value)
            {
                bee.vel.y = bee.vel.y - Mathf.InverseLerp(0.2f, 0f, bee.life);
                if (bee.life <= 0f && (terrainCollisionData.contactPoint.y < 0 || bee.pos.y < -100f))
                {
                    bee.Destroy();
                }
                bee.flySpeed = Mathf.Min(bee.flySpeed, Mathf.Max(0f, bee.life) * 3f);
                if (bee.room.water && bee.pos.y < bee.room.FloatWaterLevel(bee.pos.x))
                {
                    bee.Destroy();
                }
                return;
            }
            if (bee.room.water && bee.pos.y < bee.room.FloatWaterLevel(bee.pos.x))
            {
                bee.pos.y = bee.room.FloatWaterLevel(bee.pos.x) + 1f;
                bee.vel.y = bee.vel.y + 1f;
                bee.flyDir.y = bee.flyDir.y + 1f;
            }
            if (terrainCollisionData.contactPoint.x != 0)
            {
                bee.flyDir.x = bee.flyDir.x - (float)terrainCollisionData.contactPoint.x;
            }
            if (terrainCollisionData.contactPoint.y != 0)
            {
                bee.flyDir.y = bee.flyDir.y - (float)terrainCollisionData.contactPoint.y;
            }

            if (bee.huntChunk != null && bee.mode != SporePlant.Bee.Mode.Hunt)
            {
                bee.ChangeMode(SporePlant.Bee.Mode.Hunt);
            }

            if (bee.huntChunk == null)
            {
                bee.blinkFreq = Custom.LerpAndTick(bee.blinkFreq, 0.033333335f, 0.05f, 0.033333335f);
                bee.flySpeed = Custom.LerpAndTick(bee.flySpeed, 0.9f, 0.08f, UnityEngine.Random.value / 30f);

                if (UnityEngine.Random.value < 0.0025f)
                {
                    bee.room.AddObject(new SporePlant.BeeSpark(bee.pos));
                }
                if (UnityEngine.Random.value < 0.016666668f)
                {
                    bee.room.PlaySound(SoundID.Spore_Bee_Angry_Buzz, bee.pos, Custom.LerpMap(bee.life, 0f, 0.25f, 0.1f, 0.5f) + UnityEngine.Random.value * 0.5f, Custom.LerpMap(bee.life, 0f, 0.5f, 0.8f, 0.9f, 0.4f));
                }

                var beeInRoom = (from update in bee.room.updateList
                                where update is SporePlant.Bee
                                select update as SporePlant.Bee).ToArray();
                var pickABee = beeInRoom[UnityEngine.Random.Range(0, beeInRoom.Length)];
                if(BeeModuleManager.beeModules.TryGetValue(pickABee,out var pickModule))
                {
                    if (pickABee != bee && pickABee.huntChunk != null && Custom.DistLess(pickABee.pos, bee.pos, pickABee.huntChunk != null ? 300f : 60f) && bee.room.VisualContact(pickABee.pos, bee.pos))
                    {
                        if (bee.huntChunk != null && bee.huntChunk.owner.TotalMass > 0.3f && UnityEngine.Random.value < bee.CareAboutChunk(bee.huntChunk))
                        {
                            if (bee.HuntChunkIfPossible(pickABee.huntChunk))
                            {
                                return;
                            }
                            if (Vector2.Distance(pickABee.pos, pickABee.huntChunk.pos) < Vector2.Distance(bee.hoverPos, pickABee.huntChunk.pos))
                            {
                                bee.vel += Vector2.ClampMagnitude(pickABee.pos - bee.pos, 60f) / 20f * 3f;
                            }
                        }
                    }
                }
                
            }
            else
            {
                bee.blinkFreq = Custom.LerpAndTick(bee.blinkFreq, 0.33333334f, 0.05f, 0.033333335f);
                float num3 = Mathf.InverseLerp(-1f, 1f, Vector2.Dot(bee.flyDir.normalized, Custom.DirVec(bee.pos, bee.huntChunk.pos)));
                bee.flySpeed = Custom.LerpAndTick(bee.flySpeed, Mathf.Clamp(Mathf.InverseLerp(bee.huntChunk.rad, bee.huntChunk.rad + 110f, Vector2.Distance(bee.pos, bee.huntChunk.pos)) * 2f + num3, 0.4f, 2.2f), 0.08f, UnityEngine.Random.value / 30f);
                bee.flySpeed = Custom.LerpAndTick(bee.flySpeed, Custom.LerpMap(Vector2.Dot(bee.flyDir.normalized, Custom.DirVec(bee.pos, bee.huntChunk.pos)), -1f, 1f, 0.4f, 1.8f), 0.08f, UnityEngine.Random.value / 30f);
                bee.vel *= 0.9f;
                bee.flyDir = Vector2.Lerp(bee.flyDir, Custom.DirVec(bee.pos, bee.huntChunk.pos), UnityEngine.Random.value * 0.4f);
                if (UnityEngine.Random.value < 0.033333335f)
                {
                    bee.room.PlaySound(SoundID.Spore_Bee_Angry_Buzz, bee.pos, Custom.LerpMap(bee.life, 0f, 0.25f, 0.1f, 1f), Custom.LerpMap(bee.life, 0f, 0.5f, 0.8f, 1.2f, 0.25f));
                }
                if (UnityEngine.Random.value < 0.1f && bee.lastBoostTrail <= 0f && num3 > 0.7f && Custom.DistLess(bee.pos, bee.huntChunk.pos, bee.huntChunk.rad + 150f) && !Custom.DistLess(bee.pos, bee.huntChunk.pos, bee.huntChunk.rad + 50f) && bee.room.VisualContact(bee.pos, bee.huntChunk.pos))
                {
                    Vector2 a = Vector3.Slerp(Custom.DirVec(bee.pos, bee.huntChunk.pos), bee.flyDir.normalized, 0.5f);
                    float num4 = Vector2.Distance(bee.pos, bee.huntChunk.pos) - bee.huntChunk.rad;
                    Vector2 b = bee.pos + a * num4;
                    if (num4 > 30f && !bee.room.GetTile(b).Solid && !bee.room.PointSubmerged(b) && bee.room.VisualContact(bee.pos, b))
                    {
                        bee.boostTrail = 1f;
                        bee.pos = b;
                        bee.vel = a * 10f;
                        bee.flyDir = a;
                        bee.room.AddObject(new SporePlant.BeeSpark(bee.lastPos));
                        bee.room.PlaySound(SoundID.Spore_Bee_Dash, bee.lastPos);
                        bee.room.PlaySound(SoundID.Spore_Bee_Spark, bee.pos, 0.2f, 1.5f);
                    }
                }
                for (int j = 0; j < bee.huntChunk.owner.bodyChunks.Length; j++)
                {
                    if (Custom.DistLess(bee.pos, bee.huntChunk.owner.bodyChunks[j].pos, bee.huntChunk.owner.bodyChunks[j].rad))
                    {
                        bee.Attach(bee.huntChunk.owner.bodyChunks[j]);
                        return;
                    }
                }
                if (!Custom.DistLess(bee.pos, bee.huntChunk.pos, bee.huntChunk.rad + 400f) || (UnityEngine.Random.value < 0.1f && bee.huntChunk.submersion > 0.8f) || bee.ObjectAlreadyStuck(bee.huntChunk.owner) || !bee.room.VisualContact(bee.pos, bee.huntChunk.pos))
                {
                    bee.huntChunk = null;
                    return;
                }
            }

            if (bee.huntChunk == null)
            {
                bee.LookForRandomCreatureToHunt();
            }
        }

        public bool LookForRandomCreatureToHunt(SporePlant.Bee bee)
        {
            if (bee.huntChunk != null)
            {
                return false;
            }

            var creatureInRoom = (from update in bee.room.updateList
                                     where update is Creature && !(update as Creature).dead && (update as Creature).abstractCreature.ID.number != playerID.number && (PunchConfig.IgnoreSlugs() ? !(update is Player) : true)
                                  select update as Creature).ToArray();

            if (creatureInRoom.Length > 0)
            {
                Creature creature = creatureInRoom[UnityEngine.Random.Range(0, creatureInRoom.Length)];
                if (creature.room == bee.room && SporePlant.SporePlantInterested(creature.Template.type))
                {
                    for (int i = 0; i < creature.bodyChunks.Length; i++)
                    {
                        if (Custom.DistLess(bee.pos, creature.bodyChunks[i].pos, creature.bodyChunks[i].rad))
                        {
                            bee.Attach(creature.bodyChunks[i]);
                            return true;
                        }
                    }
                    return bee.HuntChunkIfPossible(creature.bodyChunks[UnityEngine.Random.Range(0, creature.bodyChunks.Length)]);
                }
            }
            if (UnityEngine.Random.value < 0.1f)
            {
                var attackedBeeInRoom = (from update in bee.room.updateList
                                 where update is AttachedBee
                                 select update as AttachedBee).ToArray();
                if(attackedBeeInRoom.Length > 0)
                {
                    var pickAnAttackedBee = attackedBeeInRoom[UnityEngine.Random.Range(0, attackedBeeInRoom.Length)];

                    if (BeeModuleManager.attachedBeeModules.TryGetValue(pickAnAttackedBee, out var module))
                    {
                        if (pickAnAttackedBee.attachedChunk != null)
                        {
                            return bee.HuntChunkIfPossible(pickAnAttackedBee.attachedChunk.owner.bodyChunks[UnityEngine.Random.Range(0, pickAnAttackedBee.attachedChunk.owner.bodyChunks.Length)]);
                        }
                    }
                }
            }
            return false;
        }

        public void Attach(Bee bee,BodyChunk chunk)
        {
            SporePlant.AttachedBee attachedBee = new SporePlant.AttachedBee(bee.room, new AbstractPhysicalObject(bee.room.world, AbstractPhysicalObject.AbstractObjectType.AttachedBee, null, bee.room.GetWorldCoordinate(bee.pos), bee.room.game.GetNewID()), chunk, bee.pos, Custom.DirVec(bee.lastLastPos, bee.pos), bee.life, bee.lifeTime, bee.boostTrail > 0f);
            BeeModuleManager.AddToModule(attachedBee);

            (chunk.owner as Creature).Violence(null, null, chunk, null, Creature.DamageType.Bite, 0.2f, 1f);
            bee.room.AddObject(attachedBee);
            bee.room.PlaySound(SoundID.Spore_Bee_Attach_Creature, chunk);
            bee.Destroy();
        }


        public class AttachedBeeModule
        {
            public WeakReference<SporePlant.AttachedBee> attachedBeeRef;
            public AttachedBeeModule(SporePlant.AttachedBee attachedBee)
            {
                attachedBeeRef = new WeakReference<SporePlant.AttachedBee>(attachedBee);
            }
        }
    }
}
