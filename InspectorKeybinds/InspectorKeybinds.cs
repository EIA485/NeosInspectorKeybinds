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

		static MethodInfo OnObjectRootPressed = AccessTools.Method(typeof(SceneInspector), "OnObjectRootPressed");
		static MethodInfo OnRootUpPressed = AccessTools.Method(typeof(SceneInspector), "OnRootUpPressed");
		static MethodInfo OnSetRootPressed = AccessTools.Method(typeof(SceneInspector), "OnSetRootPressed");
		static MethodInfo OnAddChildPressed = AccessTools.Method(typeof(SceneInspector), "OnAddChildPressed");
		static MethodInfo OnInsertParentPressed = AccessTools.Method(typeof(SceneInspector), "OnInsertParentPressed");
		static MethodInfo OnAttachComponentPressed = AccessTools.Method(typeof(SceneInspector), "OnAttachComponentPressed");
		static MethodInfo OnDestroyPressed = AccessTools.Method(typeof(SceneInspector), "OnDestroyPressed");
		static MethodInfo OnDestroyPreservingAssetsPressed = AccessTools.Method(typeof(SceneInspector), "OnDestroyPreservingAssetsPressed");
		static MethodInfo OnDuplicatePressed = AccessTools.Method(typeof(SceneInspector), "OnDuplicatePressed");
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
					inspector.World.RunSynchronously(() =>
					{
						if (input.GetKeyDown(Key.C))
							if (input.GetKey(Key.Alt)) inspector.Root.Target?.AddSlot(inspector.Root.Target.Name + " - Child");
							else OnAddChildPressed.Invoke(inspector, nullargs);
						if (input.GetKeyDown(Key.H)) OnInsertParentPressed.Invoke(inspector, nullargs);
						if (input.GetKeyDown(Key.G)) OnDuplicatePressed.Invoke(inspector, nullargs);
						if (input.GetKeyDown(Key.Y)) OnObjectRootPressed.Invoke(inspector, nullargs);
						if (input.GetKeyDown(Key.U)) OnRootUpPressed.Invoke(inspector, nullargs);
						if (input.GetKeyDown(Key.J)) OnSetRootPressed.Invoke(inspector, nullargs);
						if (input.GetKeyDown(Key.Backspace))
							if (input.GetKey(Key.Alt)) OnDestroyPressed.Invoke(inspector, nullargs);
							else OnDestroyPreservingAssetsPressed.Invoke(inspector, nullargs);
						if (input.GetKeyDown(Key.V)) OnAttachComponentPressed.Invoke(inspector, new object[] { null, new ButtonEventData(null, inspector.Slot.GlobalPosition, float2.Zero, float2.Zero) });
					});
				}
				catch (Exception e) { Error(e); } // we dont want to disable the userspace component if we throw an exception
			}

		}
		static CommonTool GetCommonTool(UserRoot userRoot, Chirality side) => userRoot.GetRegisteredComponent((CommonTool t) => t.Side.Value == side);
		static Chirality GetOther(Chirality cur) => cur == Chirality.Right ? Chirality.Left : Chirality.Right;
	}
}