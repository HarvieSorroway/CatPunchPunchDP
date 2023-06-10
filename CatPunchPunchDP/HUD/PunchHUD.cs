using CatPunchPunchDP.Modules;
using HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class PunchHUDHooks
{
    public static void HookOn()
    {
        On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
        On.HUD.HUD.InitMultiplayerHud += HUD_InitMultiplayerHud;
    }

    private static void HUD_InitMultiplayerHud(On.HUD.HUD.orig_InitMultiplayerHud orig, HUD.HUD self, ArenaGameSession session)
    {
        orig.Invoke(self, session);
        self.AddPart(new PunchHUD(self, session.game.cameras[0]));
    }

    private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
    {
        orig.Invoke(self, cam);
        self.AddPart(new PunchHUD(self, cam));
    }
}

public class PunchHUD : HudPart
{
    public static PunchHUD instance;
    public static ConditionalWeakTable<Player, PunchCircle> circles = new ConditionalWeakTable<Player, PunchCircle>();


    RoomCamera roomCamera;
    List<PunchCircle> punchCircles = new List<PunchCircle>();

    public PunchHUD(HUD.HUD hud, RoomCamera cam) : base(hud)
    {
        instance = this;
        roomCamera = cam;

        foreach(var player in cam.room.game.Players)
        {
            if(player.realizedCreature != null)
            {
                AddPunchHUD(player.realizedCreature as Player);
            }
        }
    }

    public static void AddPunchHUD(Player player)
    {
        if (instance == null)
            return;
        if (circles.TryGetValue(player, out var circle))
            return;

        var cirlcle = new PunchCircle(instance, player);
        circles.Add(player, cirlcle);
        instance.punchCircles.Add(cirlcle);

        PunchPlugin.Log($"Init hud for {player}");
    }

    public override void Draw(float timeStacker)
    {
        base.Draw(timeStacker);

        for(int i = punchCircles.Count - 1; i >= 0; i--)
        {
            punchCircles[i].DrawSprites(roomCamera, timeStacker);
        }
    }

    public override void ClearSprites()
    {
        for (int i = punchCircles.Count - 1; i >= 0; i--)
        {
            punchCircles[i].ClearSprites();
        }
        instance = null;
    }

    public static void PlayerUpdate(Player player)
    {
        if(circles.TryGetValue(player, out var circle))
        {
            circle.PlayerUpdate(player);
        }
    }

    public class PunchCircle
    {
        PunchHUD owner;
        WeakReference<Player> playerRef;

        PunchType currentType;

        FSprite circle;
        FSprite punchTypeSprite;

        bool hide;
        int currentCoolDown;
        int maxCoolDown;
        int setMaxCoolDown;

        float typeAlpha;

        float aimAlpha;
        float currentAlpha;
        float lastAlpha;

        Vector2 aimPos;
        Vector2 currentPos;
        Vector2 lastPos;

        Player Player
        {
            get
            {
                if (playerRef.TryGetTarget(out var result))
                    return result;
                return null;
            }
        }

        public PunchCircle(PunchHUD owner,Player player)
        {
            this.owner = owner;
            playerRef = new WeakReference<Player>(player);

            InitiateSprites(owner.hud.fContainers[0]);
            SetPunchType(PunchType.ParseObjectType(player.objectInStomach));
        }

        public void InitiateSprites(FContainer container)
        {
            circle = new FSprite("Futile_White", true)
            {
                shader = owner.hud.rainWorld.Shaders["CircleHUD_Punch"],
                scale = 1.7f
            };

            punchTypeSprite = new FSprite("pixel", true);
            container.AddChild(punchTypeSprite);
            container.AddChild(circle);
        }

        public void DrawSprites(RoomCamera roomCamera,float timeStacker)
        {
            float hideAlpha = hide ? 0f : 1f;
            currentPos = Vector2.Lerp(lastPos, aimPos, 0.2f);
            lastPos = currentPos;

            typeAlpha = Mathf.Lerp(typeAlpha, 0f, 0.05f);
            if (typeAlpha < 0.001f)
                typeAlpha = 0f;

            currentAlpha = Mathf.Lerp(lastAlpha, aimAlpha, 0.15f);
            lastAlpha = currentAlpha;

            Vector2 camPos = Vector2.Lerp(roomCamera.lastPos,roomCamera.pos, timeStacker);
            Vector2 hoverPos = currentPos - camPos;

            punchTypeSprite.SetPosition(hoverPos);
            punchTypeSprite.alpha = Mathf.Clamp01(typeAlpha + currentAlpha) * hideAlpha;

            circle.SetPosition(hoverPos);
            Color param = Color.white;
            param.r = 1f - Mathf.Clamp01((float)currentCoolDown / maxCoolDown);
            param.g = 1f - Mathf.Clamp01((float)currentCoolDown / maxCoolDown);
            param.b = currentAlpha * hideAlpha;
            param.a = 0f;
            circle.color = param;
        }

        public void Update()
        {
        }

        public void PlayerUpdate(Player player)
        {
            hide = player.room == null;
            aimPos = player.DangerPos + Vector2.up * 40f;
            if (PlayerModuleManager.modules.TryGetValue(player, out var module))
            {
                currentCoolDown = module.coolDown;
                aimAlpha = module.coolDown > 0 ? 1f : 0f;
            }
            SetPunchType(PunchType.ParseObjectType(player.objectInStomach));
        }

        public void SyncCoolDown()
        {
            maxCoolDown = setMaxCoolDown;
        }

        public void SetPunchType(PunchType punchType)
        {
            if (currentType == punchType)
                return;

            currentType = punchType;
            if (PunchConfig.configSettings.TryGetValue(punchType, out var setting))
            {
                if(setting.elementName != "")
                {
                    punchTypeSprite.element = Futile.atlasManager.GetElementWithName(setting.elementName);
                    typeAlpha = 1f;
                }
                else
                {
                    punchTypeSprite.element = Futile.atlasManager.GetElementWithName("pixel");
                    typeAlpha = 0f;
                }
                punchTypeSprite.color = setting.color;
            }
            setMaxCoolDown = PunchConfig.GetCoolDownConfig(punchType).Value;
        }

        public void ClearSprites()
        {
            circle.RemoveFromContainer();
            punchTypeSprite.RemoveFromContainer();
            owner.punchCircles.Remove(this);
        }
    }
}
