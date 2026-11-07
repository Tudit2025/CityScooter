#ifndef CITYSCOOTER_COMIC_PANORAMA
#define CITYSCOOTER_COMIC_PANORAMA
TEXTURE2D(_ComicSkyTexture);
SAMPLER(sampler_ComicSkyTexture);
// yaw radians, horizon elevation radians, vertical angular scale, enabled.
float4 _ComicSkyMapping;
float _ComicSkyScale;
half4 _ComicSkyHaze;
#ifndef CITYSCOOTER_COMIC_TURN_DECL
#define CITYSCOOTER_COMIC_TURN_DECL
// Fake turn (FakeTurnDirector): x: sideways metres per squared metre (+ = right), y: sky heading radians.
float4 _ComicTurn;
#endif
half3 ComicPanorama(float3 direction)
{
    float3 d=normalize(direction);
    // Widen the sampled front view; atan2 keeps a continuous 360-degree wrap,
    // unlike multiplying U, which would repeat buildings and add visible seams.
    float apparentScale=max(.1,_ComicSkyScale);
    float u=frac(.5+(atan2(d.x,d.z*apparentScale)+_ComicSkyMapping.x+_ComicTurn.y)/(2*PI));
    float v=saturate(.5+(asin(clamp(d.y,-1,1))-_ComicSkyMapping.y)/(PI*max(.1,_ComicSkyMapping.z)*apparentScale));
    half3 color=SAMPLE_TEXTURE2D(_ComicSkyTexture,sampler_ComicSkyTexture,float2(u,v)).rgb;
    // Blend a narrow rear seam and uniform polar caps; no scene dome geometry.
    float seam=1-smoothstep(0,.012,min(u,1-u));
    if(seam>0)color=lerp(color,.5*(color+SAMPLE_TEXTURE2D(_ComicSkyTexture,sampler_ComicSkyTexture,float2(1-u,v)).rgb),seam);
    float pole=1-smoothstep(0,.035,min(v,1-v));
    if(pole>0)color=lerp(color,SAMPLE_TEXTURE2D(_ComicSkyTexture,sampler_ComicSkyTexture,float2(.5,v)).rgb,pole);
    return lerp(color,_ComicSkyHaze.rgb,_ComicSkyHaze.a);
}
#endif
