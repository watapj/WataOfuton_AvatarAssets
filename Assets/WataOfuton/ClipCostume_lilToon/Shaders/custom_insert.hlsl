
void ClipCostume(lilFragData fd)
{
#if defined(LIL_OUTLINE)
    float mask = LIL_SAMPLE_2D(_ClipMask, sampler_OutlineTex, fd.uvMain).r;
#else
    float mask = LIL_SAMPLE_2D(_ClipMask, sampler_MainTex, fd.uvMain).r;
#endif
    float dist = distance(_WorldSpaceCameraPos.xyz, fd.positionWS.xyz);

    float c = (mask * _EyeDist <= dist) ? 1.0 : -1.0;
    
    // https://docs.vrchat.com/docs/vrchat-202231#features-1
    c = (_VRChatCameraMode==0.0) ? c : 1.0; // _VRChatCameraMode:0 - Rendering normally
    c = (_VRChatMirrorMode==0.0) ? c : 1.0; // _VRChatMirrorMode:0 - Rendering normally, not in a mirror
    
    c = _ClipOn>0.5 ? c : 1.0;
    clip(c);
}