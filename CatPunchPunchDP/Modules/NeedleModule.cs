using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CatPunchPunchDP.Modules
{
    public static class NeedleModuleManager
    {
        public static ConditionalWeakTable<BigNeedleWormAI, NeedleModule> modules = new ConditionalWeakTable<BigNeedleWormAI, NeedleModule>();
        public static void HookOn()
        {
            On.BigNeedleWormAI.Update += BigNeedleWormAI_Update;
            On.BigNeedleWormAI.IUseARelationshipTracker_UpdateDynamicRelationship += BigNeedleWormAI_IUseARelationshipTracker_UpdateDynamicRelationship;
            On.Creature.SuckedIntoShortCut += Creature_SuckedIntoShortCut;
        }

        private static void Creature_SuckedIntoShortCut(On.Creature.orig_SuckedIntoShortCut orig, Creature self, RWCustom.IntVector2 entrancePos, bool carriedByOther)
        {
            if(self is BigNeedleWorm)
            {
                if(modules.TryGetValue(((self as BigNeedleWorm).AI as BigNeedleWormAI),out var module))
                {
                    if(module.leave)
                    {
                        PunchPlugin.Log($"{self} destroy itself");
                        self.Destroy();
                        return;
                    }
                }
            }
            orig.Invoke(self,entrancePos,carriedByOther);
        }

        private static CreatureTemplate.Relationship BigNeedleWormAI_IUseARelationshipTracker_UpdateDynamicRelationship(On.BigNeedleWormAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, BigNeedleWormAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            var result = orig.Invoke(self, dRelation);
            if(modules.TryGetValue(self, out var module))
            {
                result = module.UpdateDynamicRelationship(self, dRelation, result);
            }
            return result;
        }

        public static void AddToModule(BigNeedleWormAI bigNeedleWormAI)
        {
            modules.Add(bigNeedleWormAI, new NeedleModule(bigNeedleWormAI));
        }

        private static void BigNeedleWormAI_Update(On.BigNeedleWormAI.orig_Update orig, BigNeedleWormAI self)
        {
            orig.Invoke(self);
            if(modules.TryGetValue(self,out var needleModule))
            {
                needleModule.Update(self);
            }
        }
    }
    public class NeedleModule
    {
        public WeakReference<BigNeedleWormAI> wormAIRef;

        public bool leave;
        int setRoomCounter = 0;
        public NeedleModule(BigNeedleWormAI bigNeedleWormAI)
        {
            wormAIRef = new WeakReference<BigNeedleWormAI>(bigNeedleWormAI);
        }

        public void Update(BigNeedleWormAI bigNeedleWormAI)
        {
            if(leave)
            {
                if((bigNeedleWormAI.worm.room == null && !bigNeedleWormAI.worm.inShortcut) || (!bigNeedleWormAI.worm.room.BeingViewed && !bigNeedleWormAI.worm.inShortcut))
                {
                    PunchPlugin.Log($"{bigNeedleWormAI.worm} destroy itself");
                    bigNeedleWormAI.worm.Destroy();
                }
            }
            else
            {
                if (bigNeedleWormAI.worm.abstractCreature.abstractAI.followCreature == null || bigNeedleWormAI.worm.abstractCreature.abstractAI.followCreature.state.dead)
                {
                    leave = true;
                    PunchPlugin.Log($"{bigNeedleWormAI.worm} finish target {bigNeedleWormAI.worm.abstractCreature.abstractAI.followCreature},trying to leave");
                    bigNeedleWormAI.worm.abstractCreature.abstractAI.followCreature = null;

                    bigNeedleWormAI.utilityComparer.GetUtilityTracker(bigNeedleWormAI.rainTracker).exponent = 0f;
                    bigNeedleWormAI.utilityComparer.GetUtilityTracker(bigNeedleWormAI.rainTracker).weight += 100000000f;
                }
            }
        }

        public CreatureTemplate.Relationship UpdateDynamicRelationship(BigNeedleWormAI bigNeedleWormAI, RelationshipTracker.DynamicRelationship dRelation, CreatureTemplate.Relationship orig)
        {
            if((dRelation.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.BigNeedleWorm && dRelation.trackerRep.representedCreature != bigNeedleWormAI.worm.abstractCreature.abstractAI.followCreature) || dRelation.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat)
                orig.type = CreatureTemplate.Relationship.Type.Ignores;
            return orig;
        }
    }
}
