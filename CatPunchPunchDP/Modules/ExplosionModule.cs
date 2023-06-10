using Noise;
using RWCustom;
using Smoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CatPunchPunchDP.Modules
{
    public static class ExplosionAndBombModuleManager
    {
        public static ConditionalWeakTable<Explosion, ExplosionModule> modules = new ConditionalWeakTable<Explosion, ExplosionModule>();
        public static ConditionalWeakTable<BombSmoke, BombSmokeModule> smokeModule = new ConditionalWeakTable<BombSmoke, BombSmokeModule>();
        public static void HookOn()
        {
            On.Explosion.Update += Explosion_Update;
            On.Smoke.BombSmoke.Update += BombSmoke_Update;
        }

        private static void BombSmoke_Update(On.Smoke.BombSmoke.orig_Update orig, BombSmoke self, bool eu)
        {
            orig.Invoke(self, eu);
            if(smokeModule.TryGetValue(self,out var module))
            {
                module.Update(self, eu);
            }
        }

        public static void AddToModule(Explosion explosion, Player player)
        {
            modules.Add(explosion, new ExplosionModule(explosion, player));
        }
        public static void AddToModule(BombSmoke smoke, SlugcatHand fireHand, int lifeTime)
        {
            smokeModule.Add(smoke, new BombSmokeModule(smoke, fireHand, lifeTime));
        }

        private static void Explosion_Update(On.Explosion.orig_Update orig, Explosion self, bool eu)
        {
            if (modules.TryGetValue(self, out var module))
            {
                module.Update(self, eu);
            }
            else
                orig.Invoke(self, eu);
        }
    }
    public class ExplosionModule
    {
        public WeakReference<Explosion> explosionRef;
        public WeakReference<Player> playerRef;

        public Player Player
        {
            get
            {
                if (playerRef.TryGetTarget(out var result))
                {
                    return result;
                }
                return null;
            }
        }
        public ExplosionModule(Explosion explosion, Player player)
        {
            explosionRef = new WeakReference<Explosion>(explosion);
            playerRef = new WeakReference<Player>(player);
        }

        public void Update(Explosion explosion, bool eu)
        {
            explosion.evenUpdate = eu;

            if (!explosion.explosionReactorsNotified)
            {
                explosion.explosionReactorsNotified = true;
                for (int i = 0; i < explosion.room.updateList.Count; i++)
                {
                    if (explosion.room.updateList[i] is Explosion.IReactToExplosions)
                    {
                        (explosion.room.updateList[i] as Explosion.IReactToExplosions).Explosion(explosion);
                    }
                }
                if (explosion.room.waterObject != null)
                {
                    explosion.room.waterObject.Explosion(explosion);
                }
                if (explosion.sourceObject != null)
                {
                    explosion.room.InGameNoise(new InGameNoise(explosion.pos, explosion.backgroundNoise * 2700f, explosion.sourceObject, explosion.backgroundNoise * 6f));
                }
            }
            explosion.room.MakeBackgroundNoise(explosion.backgroundNoise);
            float num = explosion.rad * (0.25f + 0.75f * Mathf.Sin(Mathf.InverseLerp(0f, (float)explosion.lifeTime, (float)explosion.frame) * 3.1415927f));
            for (int j = 0; j < explosion.room.physicalObjects.Length; j++)
            {
                for (int k = 0; k < explosion.room.physicalObjects[j].Count; k++)
                {
                    if (explosion.sourceObject != explosion.room.physicalObjects[j][k] && !explosion.room.physicalObjects[j][k].slatedForDeletetion && explosion.room.physicalObjects[j][k] != Player)
                    {
                        float num2 = 0f;
                        float num3 = float.MaxValue;
                        int num4 = -1;
                        for (int l = 0; l < explosion.room.physicalObjects[j][k].bodyChunks.Length; l++)
                        {
                            float num5 = Vector2.Distance(explosion.pos, explosion.room.physicalObjects[j][k].bodyChunks[l].pos);
                            num3 = Mathf.Min(num3, num5);
                            if (num5 < num)
                            {
                                float num6 = Mathf.InverseLerp(num, num * 0.25f, num5);
                                if (!explosion.room.VisualContact(explosion.pos, explosion.room.physicalObjects[j][k].bodyChunks[l].pos))
                                {
                                    num6 -= 0.5f;
                                }
                                if (num6 > 0f)
                                {
                                    explosion.room.physicalObjects[j][k].bodyChunks[l].vel += explosion.PushAngle(explosion.pos, explosion.room.physicalObjects[j][k].bodyChunks[l].pos) * (explosion.force / explosion.room.physicalObjects[j][k].bodyChunks[l].mass) * num6;
                                    explosion.room.physicalObjects[j][k].bodyChunks[l].pos += explosion.PushAngle(explosion.pos, explosion.room.physicalObjects[j][k].bodyChunks[l].pos) * (explosion.force / explosion.room.physicalObjects[j][k].bodyChunks[l].mass) * num6 * 0.1f;
                                    if (num6 > num2)
                                    {
                                        num2 = num6;
                                        num4 = l;
                                    }
                                }
                            }
                        }
                        if (explosion.room.physicalObjects[j][k] == explosion.killTagHolder)
                        {
                            num2 *= explosion.killTagHolderDmgFactor;
                        }
                        if (explosion.deafen > 0f && explosion.room.physicalObjects[j][k] is Creature && explosion.room.physicalObjects[j][k] != Player)
                        {
                            (explosion.room.physicalObjects[j][k] as Creature).Deafen((int)Custom.LerpMap(num3, num * 1.5f * explosion.deafen, num * Mathf.Lerp(1f, 4f, explosion.deafen), 650f * explosion.deafen, 0f));
                        }
                        if (num4 > -1)
                        {
                            if (explosion.room.physicalObjects[j][k] is Creature && explosion.room.physicalObjects[j][k] != Player)
                            {
                                int num7 = 0;
                                while ((float)num7 < Math.Min(Mathf.Round(num2 * explosion.damage * 2f), 8f))
                                {
                                    Vector2 p = explosion.room.physicalObjects[j][k].bodyChunks[num4].pos + Custom.RNV() * explosion.room.physicalObjects[j][k].bodyChunks[num4].rad * UnityEngine.Random.value;
                                    explosion.room.AddObject(new WaterDrip(p, Custom.DirVec(explosion.pos, p) * explosion.force * UnityEngine.Random.value * num2, false));
                                    num7++;
                                }
                                if (explosion.killTagHolder != null && explosion.room.physicalObjects[j][k] != explosion.killTagHolder)
                                {
                                    (explosion.room.physicalObjects[j][k] as Creature).SetKillTag(explosion.killTagHolder.abstractCreature);
                                }
                                (explosion.room.physicalObjects[j][k] as Creature).Violence(null, null, explosion.room.physicalObjects[j][k].bodyChunks[num4], null, Creature.DamageType.Explosion, num2 * explosion.damage / ((!((explosion.room.physicalObjects[j][k] as Creature).State is HealthState)) ? 1f : ((float)explosion.lifeTime)), num2 * explosion.stun);
                                if (explosion.minStun > 0f)
                                {
                                    (explosion.room.physicalObjects[j][k] as Creature).Stun((int)(explosion.minStun * Mathf.InverseLerp(0f, 0.5f, num2)));
                                }
                                if ((explosion.room.physicalObjects[j][k] as Creature).graphicsModule != null && (explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts != null)
                                {
                                    for (int m = 0; m < (explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts.Length; m++)
                                    {
                                        (explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m].pos += explosion.PushAngle(explosion.pos, (explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m].pos) * num2 * explosion.force * 5f;
                                        (explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m].vel += explosion.PushAngle(explosion.pos, (explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m].pos) * num2 * explosion.force * 5f;
                                        if ((explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m] is Limb)
                                        {
                                            ((explosion.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m] as Limb).mode = Limb.Mode.Dangle;
                                        }
                                    }
                                }
                            }
                            explosion.room.physicalObjects[j][k].HitByExplosion(num2, explosion, num4);
                        }
                    }
                }
            }
            explosion.frame++;
            if (explosion.frame > explosion.lifeTime)
            {
                explosion.Destroy();
            }
        }
    }

    public class BombSmokeModule
    {
        public int lifeTime;
        public SlugcatHand fireHand;
        public WeakReference<BombSmoke> bombSmoke;

        public BombSmokeModule(BombSmoke bombSmoke, SlugcatHand fireHand, int lifeTime)
        {
            this.bombSmoke = new WeakReference<BombSmoke>(bombSmoke);
            this.lifeTime = lifeTime;
            this.fireHand = fireHand;
        }

        public void Update(BombSmoke self, bool eu)
        {
            lifeTime--;

            if (fireHand != null)
            {
                self.pos = fireHand.pos;
            }

            if (lifeTime <= 0)
            {
                ExplosionAndBombModuleManager.smokeModule.Remove(self);
                self.Destroy();
            }
        }
    }
}