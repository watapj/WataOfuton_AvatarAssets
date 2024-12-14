using nadena.dev.ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(WataOfuton.Tools.ClothTransformApplier.Editor.ClothTransformApplierPlugin))]

namespace WataOfuton.Tools.ClothTransformApplier.Editor
{
    public class ClothTransformApplierPlugin : Plugin<ClothTransformApplierPlugin>
    {
        public override string DisplayName => nameof(ClothTransformApplier);

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming).BeforePlugin("com.anatawa12.avatar-optimizer").Run(nameof(ClothTransformApplier), ctx =>
            {
                var comp = ctx.AvatarRootObject.GetComponentsInChildren<ClothTransformApplier>();

                foreach (var c in comp)
                    Object.DestroyImmediate(c);
            });
        }

    }
}
