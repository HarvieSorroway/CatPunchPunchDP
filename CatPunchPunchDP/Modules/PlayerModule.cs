using BepInEx;
using CatPunchPunchDP.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static CatPunchPunchDP.Modules.PunchFunc;

public static class PlayerModuleManager
{
    public static ConditionalWeakTable<Player, PlayerModule> modules = new ConditionalWeakTable<Player, PlayerModule>();
    public static void HookOn()
    {
        On.Player.ctor += Player_ctor;
        On.Player.Update += Player_Update;
        On.Player.CanBeSwallowed += Player_CanBeSwallowed;
    }

    private static bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
    {
        return orig.Invoke(self, testObj) || testObj is NeedleEgg;
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        bool canPunch = true;

        if (self.grasps[0] != null && self.grasps[0].grabbed != null)
        {
            canPunch = false;
        }
        if (self.grasps[1] != null && self.grasps[1].grabbed != null)
        {
            canPunch = false;
        }


        orig.Invoke(self, eu);
        if(modules.TryGetValue(self,out var module))
        {
            module.PunchUpdate(self, canPunch);
        }
        PunchHUD.PlayerUpdate(self);
    }

    private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig.Invoke(self, abstractCreature, world);
        modules.Add(self, new PlayerModule(self));
        PunchHUD.AddPunchHUD(self);
    }


}

public class PlayerModule
{
    WeakReference<Player> playerRef;

    public Dictionary<PunchType, PunchFunc> punchs = new Dictionary<PunchType, PunchFunc>();

    public int coolDown = 0;
    int hand = 0;
    public Player Player
    {
        get
        {
            if(playerRef.TryGetTarget(out var result))
            {
                return result;
            }
            return null;
        }
    }
    public PlayerModule(Player player)
    {
        playerRef = new WeakReference<Player>(player);

        foreach(var punch in ExtEnum<PunchType>.values.entries)
        {
            var punchType = new PunchType(punch);
            punchs.Add(punchType, PunchFunc.CreatePunchFunc(punchType));
            PunchPlugin.Log($"Create punch func for {punchType} -> {PunchFunc.CreatePunchFunc(punchType)}");
        }
    }

    public void PunchUpdate(Player player,bool canPuch)
    {
        if(coolDown > 0)
        {
            coolDown--;
            return;
        }

        if(PunchHUD.circles.TryGetValue(player, out var circles))
        {
            circles.SyncCoolDown();
        }


        if (!player.input[0].thrw || !player.Consious || !canPuch || player.dontGrabStuff > 0)
            return;

        var punchType = PunchType.ParseObjectType(player.objectInStomach);

        TargetPackage targetPackage = punchs[punchType].SearchTarget(player, 55f, 35f);
        punchs[punchType].Punch(player, targetPackage);
        coolDown = punchs[punchType].coolDown;

        if (player.graphicsModule != null)
        {
            punchs[punchType].PunchAnimation(player, player.graphicsModule as PlayerGraphics, hand, targetPackage.punchVec);
            hand = 1 - hand;
        }
    }
}