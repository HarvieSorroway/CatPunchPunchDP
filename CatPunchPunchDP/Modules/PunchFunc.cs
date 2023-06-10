using MoreSlugcats;
using RWCustom;
using Smoke;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static CatPunchPunchDP.Modules.PunchFunc;
using Random = UnityEngine.Random;

namespace CatPunchPunchDP.Modules
{
    public class PunchFunc
    {
        public readonly PunchType punchType;
        public virtual float VelMulti => 1f;
        public virtual int coolDown => PunchConfig.GetCoolDownConfig(punchType).Value;
        public PunchFunc(PunchType punchType)
        {
            this.punchType = punchType;
        }

        public virtual void PunchAnimation(Player player,PlayerGraphics playerGraphics, int attackHand,Vector2 PunchVec)
        {
            SlugcatHand slugcatHand1 = playerGraphics.hands[attackHand];

            slugcatHand1.mode = Limb.Mode.Dangle;


            slugcatHand1.pos += PunchVec * 1.65f * VelMulti;
            slugcatHand1.vel += PunchVec * 10f * VelMulti;

            playerGraphics.LookAtPoint(player.mainBodyChunk.pos + PunchVec, 99999f);

            if (player.room.gravity != 0)
            {
                player.mainBodyChunk.vel += PunchVec * 0.15f * VelMulti;
                player.bodyChunks[1].vel -= PunchVec * 0.15f * VelMulti;
            }
            playerGraphics.blink = 0;
        }

        public virtual TargetPackage SearchTarget(Player player,float maxColliderDistanceSq,float actualPunchDistanceSq, bool ignoreDead = false, bool ignoreAppendage = false, IEnumerable<Creature> creaturePackage = null)
        {
            TargetPackage targetPackage = new TargetPackage();

            if (player.room.gravity == 0)
                targetPackage.punchVec = (player.bodyChunks[0].pos - player.bodyChunks[1].pos).normalized * 20f;
            else
                targetPackage.punchVec = (new Vector2(player.flipDirection, 0)).normalized * 20f;


            float maxAppendageDist = float.MaxValue;
            float maxChunkDist = float.MaxValue;
            Vector2 segment = player.DangerPos;

            if (creaturePackage == null)
            {
                creaturePackage = (from physicalObject in player.room.physicalObjects[player.collisionLayer]
                         where physicalObject is Creature && physicalObject != player && (ignoreDead ? !(physicalObject as Creature).dead : true)
                         select physicalObject as Creature);
            }

            foreach (var creature in creaturePackage)
            {
                if (creature.appendages != null && !ignoreAppendage)
                {
                    foreach (var appendage in creature.appendages)
                    {
                        if (!appendage.canBeHit)
                            continue;

                        foreach (var pos in appendage.segments)
                        {
                            float dist = (pos - player.mainBodyChunk.pos).magnitude;
                            if (dist < maxAppendageDist && dist < maxColliderDistanceSq)
                            {
                                maxAppendageDist = dist;
                                targetPackage.targetAppendage = appendage;
                                segment = pos;
                                targetPackage.segment = segment;
                            }
                        }
                    }
                }

                foreach (var chunk in creature.bodyChunks)
                {
                    float dist = (chunk.pos - player.mainBodyChunk.pos).magnitude;
                    if (dist < maxChunkDist && dist < maxColliderDistanceSq && dist < maxAppendageDist)
                    {
                        maxChunkDist = dist;
                        targetPackage.targetChunck = chunk;
                        targetPackage.targetAppendage = null;
                    }
                }
            }
            targetPackage.warningPunch = !(maxAppendageDist < float.MaxValue || maxChunkDist < float.MaxValue);

            if (targetPackage.targetAppendage != null)
            {
                targetPackage.punchVec = (segment - player.DangerPos);
            }
            else if(targetPackage.targetChunck != null)
            {
                targetPackage.punchVec = (targetPackage.targetChunck.pos - player.DangerPos);
            }
    

            return targetPackage;
        }

        public virtual void Punch(Player player,TargetPackage targetPackage)
        {
            float damage = PunchConfig.GetFloatValConfig(punchType).Value;
            float damageMulti = 1f;

            Creature target = null;
            BodyChunk bodyChunk = null;
            PhysicalObject.Appendage.Pos pos = null;
            if (targetPackage.targetChunck != null)
            {
                target = targetPackage.targetChunck.owner as Creature;
                bodyChunk = targetPackage.targetChunck;
            }
            else if(targetPackage.targetAppendage != null)
            {
                target = targetPackage.targetAppendage.owner as Creature;
                pos = new PhysicalObject.Appendage.Pos(targetPackage.targetAppendage, 0, 0.5f);
                damageMulti = 0.5f;
            }

            if (target == null || targetPackage.warningPunch)
                return;
            target.Violence(player.mainBodyChunk, new Vector2?(targetPackage.punchVec * 0.15f), bodyChunk, pos, Creature.DamageType.Blunt, damage * damageMulti, 19f * damageMulti);

            Vector3 hitPos = bodyChunk != null ? bodyChunk.pos : targetPackage.segment;
            player.room.PlaySound(SoundID.Rock_Hit_Creature, hitPos, 0.5f, 1f);
            player.room.AddObject(new ExplosionSpikes(player.room, hitPos, 5, 2f, 4f, 4.5f, 30f, new Color(1f, 1f, 1f, 0.5f)));
        }

        public static PunchFunc CreatePunchFunc(PunchType punchType)
        {
            if (punchType == PunchType.NormalPunch)
                return new NormalPunch();
            else if (punchType == PunchType.FastPunch)
                return new FastPunch();
            else if (punchType == PunchType.BombPunch)
                return new BombPunch();
            else if (punchType == PunchType.FriendlyPunch)
                return new FriendlyPunch(PunchType.FriendlyPunch);
            else if (punchType == PunchType.FriendlyPunch_HighLevel)
                return new FriendlyPunch_HighLevel();
            else if (punchType == PunchType.NeedlePunch)
                return new NeedlePunch();
            else if (punchType == PunchType.LaserPunch)
                return new LaserPunch();
            else if (punchType == PunchType.BeePunch)
                return new BeePunch();
            else if (punchType == PunchType.BlackHolePunch)
                return new BlackHolePunch();
            else if (punchType == PunchType.PuffPunch)
                return new PuffPunch();
            return new NormalPunch();
        }

        public class TargetPackage
        {
            public Vector2 punchVec;
            public Vector2 segment;
            public bool warningPunch;
            public BodyChunk targetChunck;
            public Creature.Appendage targetAppendage;
        }
    }

    public class PunchType : ExtEnum<PunchType>
    {
        public PunchType(string value, bool register = false) : base(value, register)
        {
        }

        public static void Init()
        {
            NormalPunch = new PunchType("NormalPunch", true);
            FastPunch = new PunchType("FastPunch", true);
            BombPunch = new PunchType("BombPunch", true);
            LaserPunch = new PunchType("LaserPunch", true);
            //SmokePunch = new PunchType("SmokePunch", true);
            FriendlyPunch = new PunchType("FriendlyPunch", true);
            FriendlyPunch_HighLevel = new PunchType("FriendlyPunch_HighLevel", true);
            BeePunch = new PunchType("BeePunch", true);
            PuffPunch = new PunchType("PuffPunch", true);
            NeedlePunch = new PunchType("NeedlePunch", true);
            BlackHolePunch = new PunchType("BlackHolePunch", true);
        }

        public static PunchType NormalPunch;
        public static PunchType FastPunch;
        public static PunchType BombPunch;
        public static PunchType LaserPunch;
        public static PunchType SmokePunch;
        public static PunchType FriendlyPunch;
        public static PunchType FriendlyPunch_HighLevel;
        public static PunchType BeePunch;
        public static PunchType PuffPunch;
        public static PunchType NeedlePunch;
        public static PunchType BlackHolePunch;

        public static PunchType ParseObjectType(AbstractPhysicalObject obj)
        {
            if (obj == null)
                return NormalPunch;
            var type = obj.type;

            if (type == AbstractPhysicalObject.AbstractObjectType.Rock)
                return FastPunch;
            else if (type == AbstractPhysicalObject.AbstractObjectType.ScavengerBomb)
                return BombPunch;
            else if (type == AbstractPhysicalObject.AbstractObjectType.NeedleEgg)
                return NeedlePunch;
            else if (type == AbstractPhysicalObject.AbstractObjectType.SporePlant)
                return BeePunch;
            else if(type == AbstractPhysicalObject.AbstractObjectType.PuffBall)
                return PuffPunch;
            else if(type == MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType.SingularityBomb)
                return BlackHolePunch;
            else if (obj is DataPearl.AbstractDataPearl)
            {
                DataPearl.AbstractDataPearl pearl = obj as DataPearl.AbstractDataPearl;
                if (pearl.dataPearlType == DataPearl.AbstractDataPearl.DataPearlType.Misc || pearl.dataPearlType == DataPearl.AbstractDataPearl.DataPearlType.Misc2 || pearl.type == AbstractPhysicalObject.AbstractObjectType.PebblesPearl)
                    return FriendlyPunch;
                else
                    return FriendlyPunch_HighLevel;
            }
            else if (obj is AbstractCreature)
            {
                AbstractCreature abstractCreature = obj as AbstractCreature;
                if (abstractCreature.creatureTemplate.type == CreatureTemplate.Type.VultureGrub)
                    return LaserPunch;
            }

            return NormalPunch;
        }
    }

    #region punchs
    public class NormalPunch : PunchFunc
    {
        public NormalPunch() : base(PunchType.NormalPunch)
        {
        }
    }

    public class FastPunch : PunchFunc
    {
        public override int coolDown => 5;
        public FastPunch() : base(PunchType.FastPunch)
        {
        }
    }

    public class BombPunch : PunchFunc
    {
        public override float VelMulti => 5f;
        public BombPunch() : base(PunchType.BombPunch)
        {
        }


        public override void Punch(Player player, TargetPackage targetPackage)
        {
            Vector2 newPos = player.mainBodyChunk.pos + targetPackage.punchVec;
            Color explodeColor = new Color(1f, 0.4f, 0.3f);

            float damage = PunchConfig.GetFloatValConfig(punchType).Value;

            //与ScavengerBomb内的代码相同
            Explosion explosion = new Explosion(player.room, player, newPos, 7, 100, damage / 1.5f, damage, damage * 10f, 0.25f, player, 1f, 10, 1f);

            ExplosionAndBombModuleManager.AddToModule(explosion, player);

            player.room.AddObject(explosion);
            player.room.AddObject(new SootMark(player.room, newPos, 80f, true));
            player.room.AddObject(new ShockWave(newPos, 330f, 0.045f, 5));
            player.room.AddObject(new Explosion.ExplosionLight(newPos, 280f, 1f, 7, explodeColor));
            player.room.AddObject(new Explosion.ExplosionLight(newPos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
            player.room.AddObject(new ExplosionSpikes(player.room, newPos, 14, 30f, 9f, 7f, 170f, explodeColor));
            player.room.PlaySound(SoundID.Bomb_Explode, player.mainBodyChunk);

            for (int i = 0; i < 25; i++)
            {
                Vector2 a = Custom.RNV();
                if (player.room.GetTile(newPos + a * 20f).Solid)
                {
                    if (!player.room.GetTile(newPos - a * 20f).Solid)
                    {
                        a *= -1f;
                    }
                    else
                    {
                        a = Custom.RNV();
                    }
                }
                for (int j = 0; j < 3; j++)
                {
                    player.room.AddObject(new Spark(newPos + a * Mathf.Lerp(30f, 60f, UnityEngine.Random.value), a * Mathf.Lerp(7f, 38f, UnityEngine.Random.value) + Custom.RNV() * 20f * UnityEngine.Random.value, Color.Lerp(explodeColor, new Color(1f, 1f, 1f), UnityEngine.Random.value), null, 11, 28));
                }
                player.room.AddObject(new Explosion.FlashingSmoke(newPos + a * 40f * UnityEngine.Random.value, a * Mathf.Lerp(4f, 20f, Mathf.Pow(UnityEngine.Random.value, 2f)), 1f + 0.05f * UnityEngine.Random.value, new Color(1f, 1f, 1f), explodeColor, UnityEngine.Random.Range(3, 11)));
            }
        }

        public override void PunchAnimation(Player player, PlayerGraphics playerGraphics, int attackHand, Vector2 PunchVec)
        {
            base.PunchAnimation(player, playerGraphics, attackHand, PunchVec);

            Vector2 newPos = player.mainBodyChunk.pos + PunchVec * 1.2f;
            BombSmoke smoke = new BombSmoke(player.room, newPos, null, Color.black);

            //添加module
            ExplosionAndBombModuleManager.AddToModule(smoke, playerGraphics != null ? playerGraphics.hands[attackHand] : null, (int)Random.Range(30, 90));
            player.room.AddObject(smoke);

            //点燃烟雾
            for (int k = 0; k < 4; k++)
            {
                smoke.EmitWithMyLifeTime(newPos + Custom.RNV(), Custom.RNV() * UnityEngine.Random.value * 17f);
            }
            //晃动镜头
            player.room.ScreenMovement(new Vector2?(newPos), default, 1.3f);

            //玩家加速
            if (player.canJump > 0 && player.room.gravity != 0) { }
            {
                player.mainBodyChunk.vel += 20f * Vector2.up;
            }
        }
    }

    public class FriendlyPunch : PunchFunc
    {
        public override int coolDown => 80;

        public FriendlyPunch(PunchType punchType) : base(punchType)
        {
        }

        public override void Punch(Player player, TargetPackage targetPackage)
        {
            float heal = PunchConfig.GetFloatValConfig(punchType).Value;

            Creature target = null;
            BodyChunk bodyChunk = null;
            PhysicalObject.Appendage.Pos pos = null;
            if (targetPackage.targetChunck != null)
            {
                target = targetPackage.targetChunck.owner as Creature;
                bodyChunk = targetPackage.targetChunck;
            }
            else if (targetPackage.targetAppendage != null)
            {
                target = targetPackage.targetAppendage.owner as Creature;
                pos = new PhysicalObject.Appendage.Pos(targetPackage.targetAppendage, 0, 0.5f);
                heal *= 0.5f;
            }

            if (target == null)
                return;

            EntityID id = player.abstractCreature.ID;
            if (target.abstractCreature.state.socialMemory.GetOrInitiateRelationship(id).like >= 1)
            {
                if (target.dead)
                {
                    target.dead = false;
                    player.room.AddObject(new FriendlyPunchEffect(target, Color.white, 2f));
                }
                else
                {
                    player.room.AddObject(new FriendlyPunchEffect(target, Color.green, 0.6f));
                }
                (target.State as HealthState).health = Mathf.Clamp((target.State as HealthState).health + heal, 0, 1f);

            }
            else
            {
                player.room.AddObject(new FriendlyPunchEffect(target, Color.yellow, 1f));
                target.abstractCreature.state.socialMemory.GetOrInitiateRelationship(id).InfluenceLike(heal);
                target.abstractCreature.state.socialMemory.GetOrInitiateRelationship(id).InfluenceTempLike(heal);
                target.abstractCreature.state.socialMemory.GetOrInitiateRelationship(id).InfluenceKnow(heal / 5f);
            }
            target.Stun(40);
        } 
    }

    public class FriendlyPunch_HighLevel : FriendlyPunch
    {
        public override int coolDown => 100;
        public FriendlyPunch_HighLevel() : base(PunchType.FriendlyPunch_HighLevel)
        {
        }

        public override void Punch(Player player, TargetPackage targetPackage)
        {
            base.Punch(player, targetPackage);
            Creature target = null;
            BodyChunk bodyChunk = null;
            PhysicalObject.Appendage.Pos pos = null;
            if (targetPackage.targetChunck != null)
            {
                target = targetPackage.targetChunck.owner as Creature;
                bodyChunk = targetPackage.targetChunck;
            }
            else if (targetPackage.targetAppendage != null)
            {
                target = targetPackage.targetAppendage.owner as Creature;
                pos = new PhysicalObject.Appendage.Pos(targetPackage.targetAppendage, 0, 0.5f);
            }

            if (target == null)
                return;

            float multi = PunchConfig.GetFloatValConfig(punchType).Value;
            if (target.abstractCreature.creatureTemplate.communityID != null)
            {
                player.room.world.game.session.creatureCommunities.InfluenceLikeOfPlayer(target.abstractCreature.creatureTemplate.communityID, player.room.world.RegionNumber, player.playerState.playerNumber, 0.5f, 0.75f, 0f);

                Scavenger scavenger = (target as Scavenger);
                if (scavenger != null)
                {
                    if (scavenger.AI.outpostModule != null && scavenger.AI.outpostModule.outpost != null)
                    {
                        if (scavenger.AI.outpostModule.outpost.worldOutpost.feePayed < 10)
                        {
                            scavenger.AI.outpostModule.outpost.worldOutpost.feePayed += (int)(multi * 10);

                            player.room.AddObject(new FriendlyPunchEffect(target, new Color(1f, 0, 1f), 0.4f, Vector2.right * 20f));
                        }
                    }
                }
            }
        }
    }

    public class NeedlePunch : PunchFunc
    {
        public override int coolDown => 120;

        public NeedlePunch() : base(PunchType.NeedlePunch)
        {
        }

        public override void Punch(Player player, TargetPackage targetPackage)
        {
            base.Punch(player, targetPackage);

            Creature target = null;
            BodyChunk bodyChunk = null;
            PhysicalObject.Appendage.Pos pos = null;
            if (targetPackage.targetChunck != null)
            {
                target = targetPackage.targetChunck.owner as Creature;
                bodyChunk = targetPackage.targetChunck;
            }
            else if (targetPackage.targetAppendage != null)
            {
                target = targetPackage.targetAppendage.owner as Creature;
                pos = new PhysicalObject.Appendage.Pos(targetPackage.targetAppendage, 0, 0.5f);
            }

            if (target == null || target.dead)
                return;

            if (!player.inShortcut)
            {
                if (player.room == null) return;

                for(int i = 0;i < PunchConfig.GetIntValConfig(punchType).Value;i++)
                {
                    var collection = from shortcut in player.room.shortcuts where shortcut.shortCutType == ShortcutData.Type.RoomExit select shortcut;
                    var randomExit = collection.ToArray()[Random.Range(0, collection.Count())];

                    AbstractCreature abstractCreature = new AbstractCreature(player.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.BigNeedleWorm), null, player.room.GetWorldCoordinate(randomExit.StartTile), player.room.game.GetNewID());
                    player.room.abstractRoom.AddEntity(abstractCreature);
                    abstractCreature.RealizeInRoom();
                    abstractCreature.realizedCreature.SpitOutOfShortCut(randomExit.StartTile, player.room, true);

                    BigNeedleWormAI bigNeedleWormAI = abstractCreature.abstractAI.RealAI as BigNeedleWormAI;
                    BigNeedleWorm bigNeedleWorm = abstractCreature.realizedCreature as BigNeedleWorm;

                    bigNeedleWormAI.BigRespondCry();
                    bigNeedleWorm.State.socialMemory.GetOrInitiateRelationship(target.abstractCreature.ID).like = -1f;
                    bigNeedleWorm.State.socialMemory.GetOrInitiateRelationship(target.abstractCreature.ID).tempLike = -1f;

                    bigNeedleWorm.State.socialMemory.GetOrInitiateRelationship(player.abstractCreature.ID).like = 1f;
                    bigNeedleWorm.State.socialMemory.GetOrInitiateRelationship(player.abstractCreature.ID).tempLike = 1f;
                    abstractCreature.abstractAI.followCreature = target.abstractCreature;

                    NeedleModuleManager.AddToModule(bigNeedleWormAI);
                }

                player.room.PlaySound(SoundID.Small_Needle_Worm_Little_Trumpet, player.DangerPos);
            }
        }
    }

    public class LaserPunch : PunchFunc
    {
        public override float VelMulti => 8f; 
        public LaserPunch() : base(PunchType.LaserPunch)
        {
        }

        public override TargetPackage SearchTarget(Player player, float maxColliderDistanceSq, float actualPunchDistanceSq, bool ignoreDead = false, bool ignoreAppendage = false, IEnumerable<Creature> creaturePackage = null)
        {
            var result = base.SearchTarget(player, maxColliderDistanceSq * 10f, actualPunchDistanceSq, true);
            result.punchVec = result.punchVec.normalized * 1.2f;
            return result;
        }

        public override void Punch(Player player, TargetPackage targetPackage)
        {
        }

        public override void PunchAnimation(Player player, PlayerGraphics playerGraphics, int attackHand, Vector2 PunchVec)
        {
            base.PunchAnimation(player, playerGraphics, attackHand, PunchVec);
            player.room.AddObject(new LaserEmitter(player.room, player, PunchVec, playerGraphics.hands[attackHand], PunchConfig.GetFloatValConfig(punchType).Value, PunchConfig.GetStringConfig(punchType).Value == PunchConfig.GetConfigSetting(punchType).stringValOptions[1]));
        }
    }

    public class BeePunch : PunchFunc
    {
        public override float VelMulti => 8f;
        public BeePunch() : base(PunchType.BeePunch)
        {
        }
        public override TargetPackage SearchTarget(Player player, float maxColliderDistanceSq, float actualPunchDistanceSq, bool ignoreDead = false, bool ignoreAppendage = false, IEnumerable<Creature> creaturePackage = null)
        {
            var result = base.SearchTarget(player, maxColliderDistanceSq * 10f, actualPunchDistanceSq, true);
            result.punchVec = result.punchVec.normalized * 1.2f;
            return result;
        }

        public override void Punch(Player player, TargetPackage targetPackage)
        {
        }

        public override void PunchAnimation(Player player, PlayerGraphics playerGraphics, int attackHand, Vector2 PunchVec)
        {
            base.PunchAnimation(player, playerGraphics, attackHand, PunchVec);

            int beeArmy = PunchConfig.GetIntValConfig(punchType).Value;
            beeArmy += Random.Range(-(int)(beeArmy / 3f), (int)(beeArmy / 3f));

            for (int i = 0; i < beeArmy; i++)
            {
                SporePlant.Bee bee = new SporePlant.Bee(null, true, playerGraphics.hands[attackHand].pos, PunchVec * VelMulti, SporePlant.Bee.Mode.Hunt);
                BeeModuleManager.AddToModule(bee, player.abstractCreature.ID);

                player.room.AddObject(bee);
            }
        }
    }

    public class BlackHolePunch : PunchFunc
    {
        public override float VelMulti => 8f;
        public BlackHolePunch() : base(PunchType.BlackHolePunch)
        {
        }

        public override TargetPackage SearchTarget(Player player, float maxColliderDistanceSq, float actualPunchDistanceSq, bool ignoreDead = false, bool ignoreAppendage = false, IEnumerable<Creature> creaturePackage = null)
        {
            var result = base.SearchTarget(player, maxColliderDistanceSq * 10f, actualPunchDistanceSq, true ,true);
            result.punchVec = result.punchVec.normalized * 1.2f;
            return result;
        }

        public override void Punch(Player player, TargetPackage targetPackage)
        {
        }

        public override void PunchAnimation(Player player, PlayerGraphics playerGraphics, int attackHand, Vector2 PunchVec)
        {
            base.PunchAnimation(player, playerGraphics, attackHand, PunchVec);
            player.room.AddObject(new BlackHole(player.room, PunchVec * 40f, playerGraphics.hands[attackHand].pos, PunchConfig.GetIntValConfig(punchType).Value));
        }
    }

    public class PuffPunch : PunchFunc
    {
        public PuffPunch() : base(PunchType.PuffPunch)
        {
        }

        public override TargetPackage SearchTarget(Player player, float maxColliderDistanceSq, float actualPunchDistanceSq, bool ignoreDead = false, bool ignoreAppendage = false, IEnumerable<Creature> creaturePackage = null)
        {
            var replaceCreatures = (from physicalObject in player.room.physicalObjects[player.collisionLayer]
                                    where physicalObject is InsectoidCreature && !(physicalObject as Creature).dead
                                    select physicalObject as Creature);
            return base.SearchTarget(player, maxColliderDistanceSq, actualPunchDistanceSq, true, true, replaceCreatures);
        }

        public override void Punch(Player player, TargetPackage targetPackage)
        {
        }

        public override void PunchAnimation(Player player, PlayerGraphics playerGraphics, int attackHand, Vector2 PunchVec)
        {
            base.PunchAnimation(player, playerGraphics, attackHand, PunchVec);

            InsectCoordinator smallInsects = null;
            int total = PunchConfig.GetIntValConfig(punchType).Value;
            float length = total / 30f;

            Color color = Color.Lerp(new Color(0.9f, 1f, 0.8f), player.room.game.cameras[0].paletteTexture.GetPixel(11, 4), 0.5f);
            color = Color.Lerp(color, new Color(0.02f, 0.1f, 0.08f), 0.85f);

            for (int i = 0; i < player.room.updateList.Count; i++)
            {
                if (player.room.updateList[i] is InsectCoordinator)
                {
                    smallInsects = (player.room.updateList[i] as InsectCoordinator);
                    break;
                }
            }

            //fix beastMaster
            foreach(var creature in (from physicalObject in player.room.physicalObjects[player.collisionLayer]
                                     where physicalObject is Creature
                                     select physicalObject as Creature))
            {
                if(!player.room.abstractRoom.creatures.Contains(creature.abstractCreature))
                    player.room.abstractRoom.creatures.Add(creature.abstractCreature);
            }


            for (int j = 0; j < total; j++)
            {
                player.room.AddObject(new SporeCloud(playerGraphics.hands[attackHand].pos, (Custom.RNV() + PunchVec) * Mathf.Lerp(0, length, (float)j / (float)total), color, Mathf.Lerp(length / 3f, length / 1.5f, Random.value), player.abstractCreature, 0, smallInsects) { nonToxic = false});
            }

            //Call dear
            WorldCoordinate worldCoordinate = player.room.GetWorldCoordinate(player.DangerPos);
            if (!player.room.aimap.TileAccessibleToCreature(worldCoordinate.Tile, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Deer)))
            {
                for (int i = 0; i < 7; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        if (player.room.aimap.TileAccessibleToCreature(worldCoordinate.Tile + Custom.eightDirections[j] * i, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Deer)))
                        {
                            worldCoordinate.Tile += Custom.eightDirections[j] * i;
                            i = 1000;
                            break;
                        }
                    }
                }
            }
            CreatureSpecificAImap creatureSpecificAImap = player.room.aimap.CreatureSpecificAImap(StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Deer));
            int num = int.MaxValue;
            int num2 = -1;
            for (int k = 0; k < creatureSpecificAImap.numberOfNodes; k++)
            {
                if (player.room.abstractRoom.nodes[player.room.abstractRoom.CreatureSpecificToCommonNodeIndex(k, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Deer))].entranceWidth > 4 && creatureSpecificAImap.GetDistanceToExit(worldCoordinate.x, worldCoordinate.y, k) > 0 && creatureSpecificAImap.GetDistanceToExit(worldCoordinate.x, worldCoordinate.y, k) < num)
                {
                    num = creatureSpecificAImap.GetDistanceToExit(worldCoordinate.x, worldCoordinate.y, k);
                    num2 = k;
                }
            }
            if (num2 > -1)
            {
                worldCoordinate.abstractNode = player.room.abstractRoom.CreatureSpecificToCommonNodeIndex(num2, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Deer));
            }
            List<AbstractCreature> list = new List<AbstractCreature>();
            for (int l = 0; l < player.room.abstractRoom.creatures.Count; l++)
            {
                if (player.room.abstractRoom.creatures[l].creatureTemplate.type == CreatureTemplate.Type.Deer && player.room.abstractRoom.creatures[l].realizedCreature != null && player.room.abstractRoom.creatures[l].realizedCreature.Consious && (player.room.abstractRoom.creatures[l].realizedCreature as Deer).AI.goToPuffBall == null && (player.room.abstractRoom.creatures[l].realizedCreature as Deer).AI.pathFinder.CoordinateReachableAndGetbackable(worldCoordinate))
                {
                    list.Add(player.room.abstractRoom.creatures[l]);
                }
            }
            if (list.Count > 0)
            {
                (list[Random.Range(0, list.Count)].abstractAI as DeerAbstractAI).AttractToSporeCloud(worldCoordinate);
                Debug.Log("A DEER IN THE ROOM WAS ATTRACTED!");
            }

            PunchPlugin.Log($"PuffPunch : {total}");
        }
    }

    #endregion
}
