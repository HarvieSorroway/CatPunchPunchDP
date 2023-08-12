using BepInEx;
using CatPunchPunchDP;
using CatPunchPunchDP.Modules;
using Menu.Remix.MixedUI.ValueTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
[BepInPlugin(ModID,"CatPunchPunch","1.0.0")]
public class PunchPlugin : BaseUnityPlugin
{
    public const string ModID = "harvie.catpunchpunch";
    bool inited;
    bool postInited;

    void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        On.RainWorld.PostModsInit += RainWorld_PostModsInit;
    }

    private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
    {
        orig.Invoke(self);
        if (postInited)
            return;
        postInited = true;
        StartCoroutine(VeryLateRegisterOI());
    }

    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig.Invoke(self);

        if (inited)
            return;

        PunchType.Init();

        ExplosionAndBombModuleManager.HookOn();
        NeedleModuleManager.HookOn();
        PlayerModuleManager.HookOn();
        BeeModuleManager.HookOn();
        PunchHUDHooks.HookOn();
        DeerHooks.HookOn();


        //thanks pukuyo
        try
        {
            string path = AssetManager.ResolveFilePath("punchbundle/punchbundle");
            AssetBundle ab = AssetBundle.LoadFromFile(path);

            var hudcircleShader = ab.LoadAsset<Shader>("assets/myshader/circlehud.shader");
            self.Shaders.Add("CircleHUD_Punch", FShader.CreateShader("CircleHUD_Punch", hudcircleShader));
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

        inited = true;
    }

    public static void Log(object obj)
    {
        Log($"{obj}");
    }

    public static void Log(string msg)
    {
        Debug.Log($"[CatPunchPunch]{msg}");
    }

    public static void Log(string pattern, params object[] vars)
    {
        Debug.Log($"[CatPunchPunch]" + string.Format(pattern, vars));
    }

    IEnumerator VeryLateRegisterOI()
    {
        yield return new WaitForSeconds(5);
        try
        {
            MachineConnector.SetRegisteredOI(ModID, new PunchConfig());
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}