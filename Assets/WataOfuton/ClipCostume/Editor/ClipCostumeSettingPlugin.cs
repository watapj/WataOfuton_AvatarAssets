using nadena.dev.ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(WataOfuton.Tools.ClipCostumeSetting.Editor.ClipCostumeSettingPlugin))]

namespace WataOfuton.Tools.ClipCostumeSetting.Editor
{
    public class ClipCostumeSettingPlugin : Plugin<ClipCostumeSettingPlugin>
    {
        public override string DisplayName => nameof(ClipCostumeSetting);
        public ClipCostumeSetting[] comp;

        protected override void Configure()
        {
            InPhase(BuildPhase.Generating).BeforePlugin("nadena.dev.modular-avatar").Run(nameof(ClipCostumeSetting), ctx =>
            {
                comp = ctx.AvatarRootObject.GetComponentsInChildren<ClipCostumeSetting>();

                foreach (var c in comp)
                {
                    c.GenController();
                    Object.DestroyImmediate(c);
                }
            });

            // Shader の変更も非破壊にやりたかったけどできないっぽい...
            // InPhase(BuildPhase.Optimizing).Run(nameof(ClipCostumeSetting), ctx =>
            // {
            //     var comp = ctx.AvatarRootObject.GetComponentsInChildren<ClipCostumeSetting>();

            //     foreach (var c in comp)
            //     {
            //         if (c._targets == null || c._targets.Count == 0)
            //         {
            //             Object.DestroyImmediate(c);
            //             return;
            //         }

            //         if (c._replaceShader)
            //         {
            //             Debug.Log($"[CC] Start Replace Shader");

            //             for (int i = 0; i < c._targets.Count; i++)
            //             {
            //                 var mats = c._targets[i].sharedMaterials;
            //                 foreach (var mat in mats)
            //                 {
            //                     Debug.Log($"[CC] ConvertMaterialToCustomShaderMenu {mat.name}");

            //                     lilToon_CC.ConvertMaterialToCustomShaderMenu(mat);
            //                     mat.SetFloat("_EyeDist", c._EyeDist);
            //                     mat.SetTexture("_ClipMask", c._ClipMask);
            //                 }
            //             }

            //         }

            //         Object.DestroyImmediate(c);
            //     }
            // });
        }
    }
}


// namespace lilToon
// {
//     public class lilToon_CC : lilToonInspector
//     {
//         public static void ConvertMaterialToCustomShaderMenu(Material mat)
//         {
//             var inspector = new lilToon_CC();
//             inspector.ConvertMaterialToCustomShader(mat);

//         }
//     }
// }