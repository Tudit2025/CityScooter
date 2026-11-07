#ifndef CITYSCOOTER_COMIC_WORLD_BEND
#define CITYSCOOTER_COMIC_WORLD_BEND
// x: rider Z, y: unaffected distance, z: rise per squared metre, w: physical ground Y.
// Set once for the scene, never from the shadow camera's position.
float4 _ComicWorldBend;
// x/y: taper interval, z: minimum cross-section scale, w: physical downhill grade.
float4 _ComicHorizon;
// x: transition distance, y: final visual rise slope, z: easing length.
float4 _ComicCurveTail;
void ComicCurve(float distanceZ, out float rise, out float slope)
{
    float d=max(0,distanceZ-_ComicWorldBend.y);
    float a=max(0,_ComicCurveTail.x-_ComicWorldBend.y);
    if(_ComicCurveTail.z<.01 || d<=a)
    {
        rise=d*d*_ComicWorldBend.z;slope=2*d*_ComicWorldBend.z;
    }
    else
    {
        // Match value and tangent, then approach a constant direction instead of rising forever.
        float t=d-a, finalSlope=_ComicCurveTail.y+_ComicHorizon.w;
        float delta=2*a*_ComicWorldBend.z-finalSlope;
        float decay=exp(-t/_ComicCurveTail.z);
        rise=a*a*_ComicWorldBend.z+finalSlope*t+delta*_ComicCurveTail.z*(1-decay);
        slope=finalSlope+delta*decay;
    }
}
void ComicTaper(float distanceZ, out float scale, out float derivative)
{
    float span=max(1,_ComicHorizon.y-_ComicHorizon.x);
    float t=saturate((distanceZ-_ComicHorizon.x)/span);
    float strength=_ComicHorizon.y>.01?1-_ComicHorizon.z:0;
    scale=1-strength*t*t*(3-2*t);
    derivative=-strength*6*t*(1-t)/span;
}
float3 ComicBendPosition(float3 positionWS, float amount)
{
    float d=positionWS.z-_ComicWorldBend.x,rise,slope,scale,derivative;
    ComicCurve(d,rise,slope);ComicTaper(d,scale,derivative);
    float ground=_ComicWorldBend.w-d*_ComicHorizon.w;
    float3 result=float3(positionWS.x*scale,ground+rise+(positionWS.y-ground)*scale,positionWS.z);
    return lerp(positionWS,result,amount);
}
float3 ComicBendNormal(float3 positionWS, float3 normalWS, float amount)
{
    float d=positionWS.z-_ComicWorldBend.x,rise,slope,scale,derivative;
    ComicCurve(d,rise,slope);ComicTaper(d,scale,derivative);
    scale=lerp(1,scale,amount);derivative*=amount;
    float height=positionWS.y-(_ComicWorldBend.w-d*_ComicHorizon.w);
    float dy=slope*amount-_ComicHorizon.w*(1-scale)+height*derivative;
    // Inverse transpose includes the shrinking width AND building height.
    normalWS.xy/=max(scale,.001);
    normalWS.z-=positionWS.x*derivative*normalWS.x+dy*normalWS.y;
    return normalize(normalWS);
}
#endif
