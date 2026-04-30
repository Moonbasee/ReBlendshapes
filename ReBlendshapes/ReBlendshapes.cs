using ResoniteModLoader;
using HarmonyLib;
using FrooxEngine;
using FrooxEngine.UIX;
using SkinnedMeshRenderer = FrooxEngine.SkinnedMeshRenderer;
using Elements.Core;

namespace ReBlendshapes;
//More info on creating mods can be found https://github.com/resonite-modding-group/ResoniteModLoader/wiki/Creating-Mods
public class ReBlendshapes : ResoniteMod {
	internal const string VERSION_CONSTANT = "1.0.1"; //Changing the version here updates it in all locations needed
	public override string Name => "ReBlendshapes";
	public override string Author => "Moonbase__";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/Moonbasee/ReBlendshapes/";

	public override void OnEngineInit() {
		Harmony harmony = new("com.Moonbase.ReBlendshapes");
		harmony.PatchAll();
	}

	/// <summary>
	/// This Patch Is Based Off The Re-Armature Mod's UI (https://github.com/CatSharkShin/ReArmature)
	/// </summary>
	[HarmonyPatch(typeof(SkinnedMeshRenderer))]
	class CreateUI
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SkinnedMeshRenderer), nameof(SkinnedMeshRenderer.BuildInspectorUI))]
        public static void Postfix(SkinnedMeshRenderer __instance, UIBuilder ui)
        {
            ui.Style.MinHeight = 24 + 36;

            ui.NestInto(ui.Empty("Re_Blendshapes_Helper"));
            {
                ui.HorizontalHeader(36, out RectTransform header, out RectTransform content);

                ui.Style.MinHeight = 24;

                ui.NestInto(header);
                ui.Text("Re-Blendshapes", alignment: Alignment.MiddleCenter);
                ui.NestOut();

                ui.NestInto(content);
                {
                    Slot slotRefHolder = ui.Empty("Slot Reader");
                    ui.NestInto(slotRefHolder);
                    {
                        ui.HorizontalLayout(4f);
                        {
                            ReferenceField<SkinnedMeshRenderer> slotField = slotRefHolder.AttachComponent<ReferenceField<SkinnedMeshRenderer>>();

                            const int index = 3;
                            SyncMemberEditorBuilder.Build(
                                slotField.GetSyncMember(index),
                                "Skinned Mesh Renderer",
                                slotField.GetSyncMemberFieldInfo(index),
                                ui);

                            var btn = ui.Button("Create Value Copies");
                            btn.LocalPressed += (button, _) =>
                            {
                                __instance.Slot.StartCoroutine(Process(button, __instance, slotField.Reference.Target));
                            };
                        }
                        ui.NestOut();
                    }
                    ui.NestOut();
                }
                ui.NestOut();
            }
            ui.NestOut();
        }

		private static IEnumerator<Context> Process(IButton button, SkinnedMeshRenderer instance, SkinnedMeshRenderer target) 
		{
			// get a list of blendshapeweight elements for the instance and target
			var instanceWeightElements = instance.BlendShapeWeights.Elements;
			var targetWeightElements = target.BlendShapeWeights.Elements;

			// convert to lists
			List<Sync<float>> instanceWeightElementsList = [.. instanceWeightElements];
			List<Sync<float>> targetWeightElementsList = [.. targetWeightElements];

			// does the instance and target both contains weights?
			if(instanceWeightElementsList.Count <= 0 || targetWeightElementsList.Count <= 0)
			{
				Warn("The Instance Mesh Or Target Mesh Do Not Have Blendshapes.");
				button.LabelText = "Cannot Setup Value Copies. Do Both Meshes Have Matching Shapes?";
			}

			// create copies
			button.LabelText = CreateValueCopies(instance, target, instanceWeightElementsList, targetWeightElementsList);

			yield return Context.WaitForSeconds(5);
			button.LabelText = "Create Value Copies";
		}

		private static string CreateValueCopies(SkinnedMeshRenderer instance, 
				SkinnedMeshRenderer target, 
				List<Sync<float>> instanceWeightFields, List<Sync<float>> targetWeightFields
		)
		{
			Debug("Creating Slot To Contain Value Copies...");

			var instanceParent = instance.Slot.Parent;
			var copyRoot = instanceParent.AddSlot($"{target.Slot.Name} VC's");

			Debug("Creating Value Copies...");

			// compare lists
			int matches = 0;
			foreach(var element in instanceWeightFields)
			{
				if(element != null) 
				{
					Debug($"Processing Blendshape '{element.NameWithPath}'...");

					// sometimes, avatar/accessory creators make the names of the blendshapes on accessories slightly different from eachother, we should
					// accomadate for that here
					var targetElement = targetWeightFields.FirstOrDefault(e => e.NameWithPath.Contains(element.NameWithPath));

					if(targetElement != null)
					{
						Debug($"Source BlendShape '{element.NameWithPath}' Matches Up With Target BlendShape '{targetElement.NameWithPath}'");
						// we have a match! create value copy component for that shape (there is probably a better way to do this)
						var valueCopy = copyRoot.AttachComponent<ValueCopy<float>>();

						valueCopy.Source.Value = element.ReferenceID;
						valueCopy.Target.Value = targetElement.ReferenceID;

						matches++;
					}
				}
			}

			if(matches > 1)
				return "Created!";
			else
				return "Cannot Setup Value Copies. Do Both Meshes Have Matching Shapes?";
		}

	}
}
