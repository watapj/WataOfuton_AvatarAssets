using nadena.dev.ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(WataOfuton.Tools.MCP_MergeNeck.Editor.MCP_MergeNeckNDPlugin))]

namespace WataOfuton.Tools.MCP_MergeNeck.Editor
{
    public class MCP_MergeNeckNDPlugin : Plugin<MCP_MergeNeckNDPlugin>
    {
        // public override string DisplayName => nameof(MCP_MergeNeckND);

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming).BeforePlugin("com.anatawa12.avatar-optimizer").Run(nameof(MCP_MergeNeckND), ctx =>
            {
                var comp = ctx.AvatarRootObject.GetComponentsInChildren<MCP_MergeNeckND>();
                foreach (var c in comp)
                {
                    c.TryApplyDiffDataNDMF();
                    Object.DestroyImmediate(c);
                }
            });
        }
    }
}
