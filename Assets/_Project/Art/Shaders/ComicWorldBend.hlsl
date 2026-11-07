#ifndef CITYSCOOTER_COMIC_WORLD_BEND
#define CITYSCOOTER_COMIC_WORLD_BEND
// x: rider Z, y: unaffected distance, z: rise per squared metre, w: physical ground Y.
// Set once for the scene, never from the shadow camera's position.
float4 _ComicWorldBend;
// x/y: taper interval, z: minimum cross-section scale, w: physical downhill grade.
float4 _ComicHorizon;
// x: transition distance, y: final visual rise slope, z: easing length.
float4 _ComicCurveTail;
#ifndef CITYSCOOTER_COMIC_TURN_DECL
#define CITYSCOOTER_COMIC_TURN_DECL
// Fake turns (FakeTurnDirector). A turn is a stretch of road fixed in the world, from z start to z end, that
// curves sideways by x metres per squared metre (+ = right). _ComicTurn: x curvature, y start z, z end z,
// w sky heading radians. _ComicTurn2: the next turn (x, y, z), or zero.
float4 _ComicTurn;
float4 _ComicTurn2;
#endif
// Only the part of a turn beyond the unaffected distance bends: a turn far ahead curves the road from its start;
// once the rider reaches it, the bend starts at the unaffected distance and the rest of the turn shrinks until the
// road ahead is straight again. Beyond the turn's end the road continues straight in the new direction.
float ComicTurnSegment(float z, float4 turn, out float slope)
{
    float from=max(turn.y,_ComicWorldBend.x+_ComicWorldBend.y);
    float span=max(0,turn.z-from);
    float q=clamp(z-from,0,span);
    slope=2*turn.x*q;
    return turn.x*(q*q+2*span*max(0,z-turn.z));
}
void ComicTurnOffset(float distanceZ, out float offset, out float slope)
{
    float z=distanceZ+_ComicWorldBend.x,s0,s1;
    offset=ComicTurnSegment(z,_ComicTurn,s0)+ComicTurnSegment(z,_ComicTurn2,s1);
    slope=s0+s1;
}
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
    float side,sideSlope;ComicTurnOffset(d,side,sideSlope);
    float3 result=float3(positionWS.x*scale+side,ground+rise+(positionWS.y-ground)*scale,positionWS.z);
    return lerp(positionWS,result,amount);
}
float3 ComicBendNormal(float3 positionWS, float3 normalWS, float amount)
{
    float d=positionWS.z-_ComicWorldBend.x,rise,slope,scale,derivative;
    ComicCurve(d,rise,slope);ComicTaper(d,scale,derivative);
    scale=lerp(1,scale,amount);derivative*=amount;
    float height=positionWS.y-(_ComicWorldBend.w-d*_ComicHorizon.w);
    float dy=slope*amount-_ComicHorizon.w*(1-scale)+height*derivative;
    float side,sideSlope;ComicTurnOffset(d,side,sideSlope);
    // Inverse transpose includes the shrinking width AND building height.
    normalWS.xy/=max(scale,.001);
    normalWS.z-=(positionWS.x*derivative+sideSlope*amount)*normalWS.x+dy*normalWS.y;
    return normalize(normalWS);
}
#endif
