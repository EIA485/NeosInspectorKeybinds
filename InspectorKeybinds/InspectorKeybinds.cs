using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using NeosModLoader;
using BaseX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace InspectorKeybinds
{
    public class InspectorKeybinds : NeosMod
    {
        public override string Name => "InspectorKeybinds";
        public override string Author => "eia485";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/EIA485/NeosInspectorKeybinds/";
        public override void OnEngineInit()
        {
            config = GetConfiguration();
            Harmony harmony = new Harmony("net.eia485.InspectorKeybinds");
            harmony.PatchAll();
        }
        static ModConfiguration config;
        public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
        {
            foreach (var bind in binds)
            {
                builder.Key(bind.configKey);
            }
        }
        static Keybind[] binds = new Keybind[] {
            new(new("create child", computeDefault: ()=>new(){new(){{Key.C, true}, {Key.Alt, false}}}), AccessTools.Method(typeof(SceneInspector), "OnAddChildPressed")),
            new(new("create child under view root", computeDefault: ()=>new(){new(){{Key.C, true}, {Key.Alt, true}}}), AccessTools.Method(typeof(ExtraBinds), nameof(ExtraBinds.CreateObjUnderViewRoot))),
            new(new("create parent", computeDefault: ()=>new(){new(){{Key.J, true}, {Key.Control, false}, {Key.Alt, false}}}), AccessTools.Method(typeof(SceneInspector), "OnInsertParentPressed")),
            new(new("duplicate", computeDefault: ()=>new(){new(){{Key.G, true}}}), AccessTools.Method(typeof(SceneInspector), "OnDuplicatePressed")),
            new(new("object root", computeDefault: ()=>new(){new(){{Key.Y, true}}}), AccessTools.Method(typeof(SceneInspector), "OnObjectRootPressed")),
            new(new("up one root", computeDefault: ()=>new(){new(){{Key.U, true}}}), AccessTools.Method(typeof(SceneInspector), "OnRootUpPressed")),
            new(new("focus", computeDefault: ()=>new(){new(){{Key.H, true}}}), AccessTools.Method(typeof(SceneInspector), "OnSetRootPressed")),
            new(new("destroy", computeDefault: ()=>new(){new(){{Key.Backspace, true}, {Key.Alt, false}}}), AccessTools.Method(typeof(SceneInspector), "OnDestroyPreservingAssetsPressed")),
            new(new("destroy No Preserve Assets", computeDefault: ()=>new(){new(){{Key.Backspace, true},{Key.Alt, true}}}), AccessTools.Method(typeof(SceneInspector), "OnDestroyPressed")),
            new(new("open component attacher", computeDefault: ()=>new(){new(){{Key.V, true}}}), AccessTools.Method(typeof(ExtraBinds), nameof(ExtraBinds.OpenAttacher))),
            new(new("bring to", computeDefault: ()=>new(){new(){{Key.B, true}, {Key.Control, false}}}), AccessTools.Method(typeof(Slot), "BringTo")),
            new(new("jump to", computeDefault: ()=>new(){new(){{Key.B, true}, {Key.Control, true}}}), AccessTools.Method(typeof(Slot), "JumpTo")),
            new(new("reset position", computeDefault: ()=>new(){new(){{Key.R, true}, {Key.Control, true}, {Key.Alt, false}}}), AccessTools.Method(typeof(Slot), "ResetPosition")),
            new(new("reset rotation", computeDefault: ()=>new(){new(){{Key.R, true}, {Key.Control, true}, {Key.Alt, true}}}), AccessTools.Method(typeof(Slot), "ResetRotation")),
            new(new("reset scale", computeDefault: ()=>new(){new(){{Key.R, true}, {Key.Control, false}, {Key.Alt, true}}}), AccessTools.Method(typeof(Slot), "ResetScale")),
            new(new("parent under world root", computeDefault: ()=>new(){new(){{Key.J, true}, {Key.Control, true}}}), AccessTools.Method(typeof(Slot), "ParentUnderWorldRoot")),
            new(new("parent under local user space", computeDefault: ()=>new(){new(){{Key.J, true}, {Key.Alt, true}}}), AccessTools.Method(typeof(Slot), "ParentUnderLocalUserSpace")),
            new(new("create pivot", computeDefault: ()=>new(){new(){{Key.P, true}}}), AccessTools.Method(typeof(Slot), "OnCreatePivotAtCenter"))
        };

        static object[] nullargs = new object[2];
        [HarmonyPatch(typeof(Userspace), "OnCommonUpdate")]
        class InspectorKeybindsPatch
        {
            static void Postfix(Userspace __instance)
            {
                try
                {
                    var input = __instance.InputInterface;
                    if (!input.GetAnyKey() || Userspace.HasFocus || __instance.Engine.WorldManager.FocusedWorld?.LocalUser.HasActiveFocus() == true) return;

                    var primaryHand = Userspace.GetControllerData(input.PrimaryHand);
                    var tool = primaryHand.userspaceController;
                    bool userSpaceHit = primaryHand.userspaceLaserHitTarget;
                    if (!userSpaceHit && input.VR_Active)
                    {
                        var secondaryHand = Userspace.GetControllerData(GetOther(input.PrimaryHand));
                        if (secondaryHand.userspaceLaserHitTarget)
                        {
                            tool = secondaryHand.userspaceController;
                            userSpaceHit = true;
                        }
                    }
                    if (!userSpaceHit)
                    {
                        bool hit = false;
                        var localUserRoot = __instance.Engine.WorldManager.FocusedWorld?.LocalUser.Root;
                        var primaryTool = GetCommonTool(localUserRoot, input.PrimaryHand);
                        hit = primaryTool.Laser.CurrentInteractionTarget != null && typeof(Canvas).IsAssignableFrom(primaryTool.Laser.CurrentInteractionTarget.GetType());
                        if (hit) tool = primaryTool;
                        else if (input.VR_Active)
                        {
                            var secondaryTool = GetCommonTool(localUserRoot, GetOther(input.PrimaryHand));
                            hit = secondaryTool.Laser.CurrentInteractionTarget != null && typeof(Canvas).IsAssignableFrom(secondaryTool.Laser.CurrentInteractionTarget.GetType());
                            if (hit) tool = secondaryTool;
                            else return;
                        }
                        else return;
                    }
                    var inspector = tool.Laser.CurrentInteractionTarget.Slot.GetComponentInChildrenOrParents<SceneInspector>();

                    if (inspector == null) return;
                    List<MethodInfo> runOnMain = new();

                    foreach (var bind in binds)
                    {
                        foreach (var keyset in config.GetValue(bind.configKey))
                        {
                            if (keyset.Count == 0) continue;
                            var enu = keyset.GetEnumerator();
                            enu.MoveNext();
                            if (input.GetKeyDown(enu.Current.Key) != enu.Current.Value) continue;
                            bool ded = false;
                            while (enu.MoveNext())
                            {
                                if (input.GetKey(enu.Current.Key) != enu.Current.Value) { ded = true; break; }
                            }
                            if (ded) continue;

                            runOnMain.Add(bind.target);
                            break;
                        }
                    }


                    inspector.World.RunSynchronously(() =>
                    {
                        foreach (MethodInfo method in runOnMain)
                        {
                            if (method.DeclaringType == typeof(SceneInspector)) //cant do a type switch ):::
                            {
                                method.Invoke(inspector, nullargs);
                            }
                            else if (method.DeclaringType == typeof(ExtraBinds))
                            {
                                method.Invoke(null, new object[] { inspector });
                            }
                            else if(method.DeclaringType == typeof(Slot))
                            {
                                if (inspector.ComponentView.Target == null) return;
                                method.Invoke(inspector.ComponentView.Target, nullargs);
                            }
                        }
                    });
                }
                catch (Exception e) { Error(e); } // we dont want to disable the userspace component if we throw an exception
            }

        }
        static CommonTool GetCommonTool(UserRoot userRoot, Chirality side) => userRoot.GetRegisteredComponent((CommonTool t) => t.Side.Value == side);
        static Chirality GetOther(Chirality cur) => cur == Chirality.Right ? Chirality.Left : Chirality.Right;
        static class ExtraBinds
        {
            static MethodInfo OnAttachComponentPressed = AccessTools.Method(typeof(SceneInspector), "OnAttachComponentPressed");
            internal static void OpenAttacher(SceneInspector instance)
                => OnAttachComponentPressed.Invoke(instance, new object[] { null, new ButtonEventData(null, instance.Slot.GlobalPosition, float2.Zero, float2.Zero) });
            internal static void CreateObjUnderViewRoot(SceneInspector instance) => instance.Root.Target?.AddSlot(instance.Root.Target.Name + " - Child");
        }



        struct Keybind
        {
            public MethodInfo target;
            public ModConfigurationKey<List<Dictionary<Key, bool>>> configKey;
            internal Keybind(ModConfigurationKey<List<Dictionary<Key, bool>>> configKey, MethodInfo target)
            {
                this.configKey = configKey;
                this.target = target;
            }
        }
    }
}