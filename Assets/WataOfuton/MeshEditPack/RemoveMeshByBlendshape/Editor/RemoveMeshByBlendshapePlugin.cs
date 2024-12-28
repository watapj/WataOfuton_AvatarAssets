using nadena.dev.ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(WataOfuton.Tools.RemoveMeshByBlendshape.Editor.RemoveMeshByBlendshapePlugin))]

namespace WataOfuton.Tools.RemoveMeshByBlendshape.Editor
{
    public class RemoveMeshByBlendshapePlugin : Plugin<RemoveMeshByBlendshapePlugin>
    {
        // public override string DisplayName => nameof(RemoveMeshByBlendshape);

        protected override void Configure()
        {
            InPhase(BuildPhase.Generating).BeforePlugin("com.anatawa12.avatar-optimizer").Run(nameof(RemoveMeshByBlendshape), ctx =>
            {
                var comp = ctx.AvatarRootObject.GetComponentsInChildren<RemoveMeshByBlendshape>();
                foreach (var c in comp)
                {
                    c.TryRemoveMeshByBlendshape();
                    Object.DestroyImmediate(c);
                }
            });
        }

    }
}
