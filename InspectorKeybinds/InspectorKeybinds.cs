using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.NET.Common;
using BepInExResoniteShim;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using Renderite.Shared;
using System.Reflection;
using System.Text.Json;

namespace InspectorKeybinds
{
    [ResonitePlugin(PluginMetadata.GUID, PluginMetadata.NAME, PluginMetadata.VERSION, PluginMetadata.AUTHORS, PluginMetadata.REPOSITORY_URL)]
    [BepInDependency(BepInExResoniteShim.PluginMetadata.GUID)]
    public class InspectorKeybinds : BasePlugin
    {
        static ConfigFile config;
        static ManualLogSource log;
        static Keybind[] binds;

        public override void Load()
        {
            config = Config;
            log = Log;
            var tc = new TypeConverter();
            tc.ConvertToString = (obj, type) => JsonSerializer.Serialize(obj);
            tc.ConvertToObject = (str, type) => str.IsNullOrWhiteSpace() ? Activator.CreateInstance(type) : JsonSerializer.Deserialize(str, type);
            TomlTypeConverter.AddConverter(typeof(List<Dictionary<Key, bool>>), tc);

            binds = new Keybind[] {
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "create child", new(){new(){{Key.C, true}, {Key.Alt, false}}}), AccessTools.Method(typeof(SceneInspector), "OnAddChildPressed")),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "create child under view root",new(){new(){{Key.C, true}, {Key.Alt, true}}}), AccessTools.Method(typeof(ExtraBinds), nameof(ExtraBinds.CreateObjUnderViewRoot))),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "create parent",new(){new(){{Key.J, true}, {Key.Control, false}, {Key.Alt, false}}}), AccessTools.Method(typeof(SceneInspector), "OnInsertParentPressed")),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "duplicate",new(){new(){{Key.G, true}}}), AccessTools.Method(typeof(SceneInspector), "OnDuplicatePressed")),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "object root",new(){new(){{Key.Y, true}}}), AccessTools.Method(typeof(SceneInspector), "OnObjectRootPressed")),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "up one root",new(){new(){{Key.U, true}}}), AccessTools.Method(typeof(SceneInspector), "OnRootUpPressed")),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "focus",new(){new(){{Key.H, true}}}), AccessTools.Method(typeof(SceneInspector), "OnSetRootPressed")),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "destroy",new(){new(){{Key.Backspace, true}, {Key.Alt, false}}}), AccessTools.Method(typeof(SceneInspector), "OnDestroyPreservingAssetsPressed")),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "destroy No Preserve Assets",new(){new(){{Key.Backspace, true},{Key.Alt, true}}}), AccessTools.Method(typeof(SceneInspector), "OnDestroyPressed")),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "open component attacher",new(){new(){{Key.V, true}}}), AccessTools.Method(typeof(ExtraBinds), nameof(ExtraBinds.OpenAttacher))),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "bring to",new(){new(){{Key.B, true}, {Key.Control, false}}}), AccessTools.Method(typeof(Slot), "BringTo")),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "jump to",new(){new(){{Key.B, true}, {Key.Control, true}}}), AccessTools.Method(typeof(Slot), "JumpTo")),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "reset position",new(){new(){{Key.R, true}, {Key.Control, true}, {Key.Alt, false}}}), AccessTools.Method(typeof(Slot), "ResetPosition")),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "reset rotation",new(){new(){{Key.R, true}, {Key.Control, true}, {Key.Alt, true}}}), AccessTools.Method(typeof(Slot), "ResetRotation")),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "reset scale",new(){new(){{Key.R, true}, {Key.Control, false}, {Key.Alt, true}}}), AccessTools.Method(typeof(Slot), "ResetScale")),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "parent under world root",new(){new(){{Key.J, true}, {Key.Control, true}}}), AccessTools.Method(typeof(Slot), "ParentUnderWorldRoot")),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "parent under local user space",new(){new(){{Key.J, true}, {Key.Alt, true}}}), AccessTools.Method(typeof(Slot), "ParentUnderLocalUserSpace")),
            new(config.Bind<List<Dictionary<Key, bool>>>(PluginMetadata.NAME, "create pivot",new(){new(){{Key.P, true}}}), AccessTools.Method(typeof(Slot), "OnCreatePivotAtCenter"))
            };

            HarmonyInstance.PatchAll();
        }


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
                        hit = primaryTool != null && primaryTool.Laser.CurrentInteractionTarget != null && typeof(Canvas).IsAssignableFrom(primaryTool.Laser.CurrentInteractionTarget.GetType());
                        if (hit) tool = primaryTool;
                        else if (input.VR_Active)
                        {
                            var secondaryTool = GetCommonTool(localUserRoot, GetOther(input.PrimaryHand));
                            hit = secondaryTool != null && secondaryTool.Laser.CurrentInteractionTarget != null && typeof(Canvas).IsAssignableFrom(secondaryTool.Laser.CurrentInteractionTarget.GetType());
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
                        foreach (var keyset in bind.configKey.Value)
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
                            else if (method.DeclaringType == typeof(Slot))
                            {
                                if (inspector.ComponentView.Target == null) return;
                                method.Invoke(inspector.ComponentView.Target, nullargs);
                            }
                        }
                    });
                }
                catch (Exception e) { log.LogError(e); } // we dont want to disable the userspace component if we throw an exception
            }

        }
        static InteractionHandler? GetCommonTool(UserRoot userRoot, Chirality side)
        {
            try
            {
                return userRoot.GetRegisteredComponent((InteractionHandler t) => t.Side.Value == side);
            }
            catch
            {
                return null;
            }
        }
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
            public ConfigEntry<List<Dictionary<Key, bool>>> configKey;
            internal Keybind(ConfigEntry<List<Dictionary<Key, bool>>> configKey, MethodInfo target)
            {
                this.configKey = configKey;
                this.target = target;
            }
        }
    }
}