using nadena.dev.ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(WataOfuton.Tools.ReverseMeshND.Editor.ReverseMeshNDPlugin))]

namespace WataOfuton.Tools.ReverseMeshND.Editor
{
    public class ReverseMeshNDPlugin : Plugin<ReverseMeshNDPlugin>
    {
        // public override string DisplayName => nameof(ReverseMeshND);

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming).AfterPlugin("com.anatawa12.avatar-optimizer").Run(nameof(ReverseMeshND), ctx =>
            {
                var comp = ctx.AvatarRootObject.GetComponentsInChildren<ReverseMeshND>();
                foreach (var c in comp)
                {
                    if (!c._isReversed)
                    {
                        c.ExecuteReverseMeshND();
                    }
                    Object.DestroyImmediate(c);
                }
            });
        }

    }
}
