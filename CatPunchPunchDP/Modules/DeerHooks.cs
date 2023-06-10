using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatPunchPunchDP.Modules
{
    public static class DeerHooks
    {
        public static void HookOn()
        {
            On.Deer.Update += Deer_Update;
        }

        private static void Deer_Update(On.Deer.orig_Update orig, Deer self, bool eu)
        {
            orig.Invoke(self, eu);
            if(self.playersInAntlers.Count > 0)
            {
                if (PlayerModuleManager.modules.TryGetValue(self.playersInAntlers[0].player,out var module))
                {
                    self.abstractCreature.controlled = module.coolDown > 0 && PunchType.ParseObjectType(self.playersInAntlers[0].player.objectInStomach) == PunchType.PuffPunch;
                }
            }
            else
            {
                if(self.abstractCreature.controlled && self.room != null && !self.room.game.rainWorld.safariMode)
                {
                    self.abstractCreature.controlled = false;
                }
            }
        }
    }
}
