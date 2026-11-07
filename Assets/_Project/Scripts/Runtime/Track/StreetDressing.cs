using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityScooter.Track
{
    [Serializable]
    public sealed class DressingObstacle
    {
        public string id;
        public Vector3 position,rotation,centre,size;
        public bool canJumpOver;
    }
    [Serializable]
    public sealed class DressingCollisionLayout { public DressingObstacle[] obstacles=Array.Empty<DressingObstacle>(); }

    /// <summary>Switches baked roadside meshes and reuses matching simple obstacle colliders.</summary>
    public sealed class StreetDressing : MonoBehaviour
    {
        public MeshFilter surface;
        public MeshFilter outline;
        public Mesh[] layouts;
        public DressingCollisionLayout[] collisionLayouts;
        public int CurrentLayout {get;private set;}
        readonly List<BoxCollider> collisionPool=new List<BoxCollider>();
        public int CollisionPoolSize=>collisionPool.Count;

        public void ApplyLayout(int seed, int chunkIndex)
        {
            if(layouts==null||layouts.Length==0)return;
            uint hash;
            unchecked
            {
                hash=(uint)seed ^ ((uint)chunkIndex*0x9e3779b9u);
                hash^=hash>>16;hash*=0x7feb352du;hash^=hash>>15;hash*=0x846ca68bu;hash^=hash>>16;
            }
            CurrentLayout=(int)(hash%(uint)layouts.Length);
            Mesh mesh=layouts[CurrentLayout];
            if(surface!=null)surface.sharedMesh=mesh;
            if(outline!=null)outline.sharedMesh=mesh;
            if(Application.isPlaying)ApplyCollisions();
        }

        void ApplyCollisions()
        {
            var list=collisionLayouts!=null&&CurrentLayout<collisionLayouts.Length?collisionLayouts[CurrentLayout].obstacles:Array.Empty<DressingObstacle>();
            for(int i=collisionPool.Count;i<list.Length;i++)
            {
                var child=new GameObject("SidewalkObstacle_"+i);child.transform.SetParent(transform,false);
                collisionPool.Add(child.AddComponent<BoxCollider>());child.AddComponent<Obstacle>();
            }
            for(int i=0;i<collisionPool.Count;i++)
            {
                var collider=collisionPool[i];bool active=i<list.Length;collider.gameObject.SetActive(active);
                if(!active)continue;
                var data=list[i];collider.name="SidewalkObstacle_"+i+"_"+data.id;
                collider.transform.localPosition=data.position;collider.transform.localRotation=Quaternion.Euler(data.rotation);
                collider.center=data.centre;collider.size=data.size;collider.GetComponent<Obstacle>().canJumpOver=data.canJumpOver;
            }
        }
    }
}
