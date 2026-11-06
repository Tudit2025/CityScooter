#ifndef CITYSCOOTER_COMIC_PANORAMA
#define CITYSCOOTER_COMIC_PANORAMA
TEXTURE2D(_ComicSkyTexture);
SAMPLER(sampler_ComicSkyTexture);
// yaw radians, horizon elevation radians, vertical angular scale, enabled.
float4 _ComicSkyMapping;
half4 _ComicSkyHaze;
half3 ComicPanorama(float3 direction)
{
    float3 d=normalize(direction);
    float u=frac(.5+(atan2(d.x,d.z)+_ComicSkyMapping.x)/(2*PI));
    float v=saturate(.5+(asin(clamp(d.y,-1,1))-_ComicSkyMapping.y)/(PI*max(.1,_ComicSkyMapping.z)));
    half3 color=SAMPLE_TEXTURE2D(_ComicSkyTexture,sampler_ComicSkyTexture,float2(u,v)).rgb;
    // Blend a narrow rear seam and uniform polar caps; no scene dome geometry.
    float seam=1-smoothstep(0,.012,min(u,1-u));
    if(seam>0)color=lerp(color,.5*(color+SAMPLE_TEXTURE2D(_ComicSkyTexture,sampler_ComicSkyTexture,float2(1-u,v)).rgb),seam);
    float pole=1-smoothstep(0,.035,min(v,1-v));
    if(pole>0)color=lerp(color,SAMPLE_TEXTURE2D(_ComicSkyTexture,sampler_ComicSkyTexture,float2(.5,v)).rgb,pole);
    return lerp(color,_ComicSkyHaze.rgb,_ComicSkyHaze.a);
}
#endif
