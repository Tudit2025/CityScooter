using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CityScooter.Rendering;

namespace CityScooter.EditorTools.Art
{
    /// <summary>Deterministic flat-shaded geometry. One vertex-color material for the entire kit.</summary>
    internal sealed class ComicMesh
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<ComicSurface> surfaces = new List<ComicSurface>();
        public ComicSurface Surface = ComicSurface.Plain;
        // xy = quad coordinates, z = ink enabled. No triangle diagonals are inked.
        readonly List<Vector3> inkUV = new List<Vector3>();
        readonly List<int> triangles = new List<int>();
        public int TriangleCount => triangles.Count / 3;

        public void ClearCreaseInk()
        {
            for(int i=0;i<inkUV.Count;i++) inkUV[i]=Vector3.zero;
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            int n = vertices.Count;
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            for (int i = 0; i < 3; i++) { normals.Add(normal); colors.Add(color.linear); triangles.Add(n + i); inkUV.Add(Vector3.zero); surfaces.Add(Surface); }
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, bool ink = true)
        {
            int start = vertices.Count;
            Tri(a, b, c, color); Tri(a, c, d, color);
            if (!ink) return;
            inkUV[start] = inkUV[start + 3] = new Vector3(0, 0, 1);
            inkUV[start + 1] = new Vector3(1, 0, 1);
            inkUV[start + 2] = inkUV[start + 4] = new Vector3(1, 1, 1);
            inkUV[start + 5] = new Vector3(0, 1, 1);
        }

        // Continuous UVs for a cloth strip: only its outside boundary gets ink, never each segment.
        public void RibbonQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, float start, float end, bool reverse = false)
        {
            int n = vertices.Count;
            if (reverse) Quad(d, c, b, a, color); else Quad(a, b, c, d, color);
            float left = reverse ? 1f : 0f, right = 1f - left;
            inkUV[n] = inkUV[n + 3] = new Vector3(left, start, 1);
            inkUV[n + 1] = new Vector3(left, end, 1);
            inkUV[n + 2] = inkUV[n + 4] = new Vector3(right, end, 1);
            inkUV[n + 5] = new Vector3(right, start, 1);
        }

        public void Box(Vector3 center, Vector3 size, Color color, Quaternion rotation = default)
        {
            if (rotation == default) rotation = Quaternion.identity;
            Vector3 P(float x, float y, float z) => center + rotation * Vector3.Scale(new Vector3(x,y,z), size) * 0.5f;
            Quad(P(-1,-1,1), P(1,-1,1), P(1,1,1), P(-1,1,1), color);
            Quad(P(1,-1,-1), P(-1,-1,-1), P(-1,1,-1), P(1,1,-1), color);
            Quad(P(1,-1,1), P(1,-1,-1), P(1,1,-1), P(1,1,1), color);
            Quad(P(-1,-1,-1), P(-1,-1,1), P(-1,1,1), P(-1,1,-1), color);
            Quad(P(-1,1,1), P(1,1,1), P(1,1,-1), P(-1,1,-1), color);
            Quad(P(-1,-1,-1), P(1,-1,-1), P(1,-1,1), P(-1,-1,1), color);
        }

        public void BoxStrip(Vector3 center, Vector3 size, Color color)
        {
            // Small longitudinal spans make the road, curb and markings follow the same GPU curve.
            int count=Mathf.CeilToInt(size.z/4f);float step=size.z/count;
            for(int i=0;i<count;i++)Box(center+new Vector3(0,0,-size.z/2+(i+.5f)*step),new Vector3(size.x,size.y,step),color);
        }

        public void Cylinder(Vector3 center, float radius, float height, Color color, int sides = 8, float topScale = 1f, Quaternion rotation = default)
        {
            if (rotation == default) rotation = Quaternion.identity;
            Vector3 P(float angle, float y, float r) => center + rotation * new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r);
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides, b = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 p = P(a,-height/2,radius), q = P(b,-height/2,radius), r = P(b,height/2,radius*topScale), s = P(a,height/2,radius*topScale);
                Quad(q,p,s,r,color);
                Tri(center + rotation * Vector3.up * height/2, r,s,color);
                Tri(center - rotation * Vector3.up * height/2,p,q,color);
            }
        }

        public void Ellipsoid(Vector3 center, Vector3 size, Color color)
        {
            const int sides = 8, rings = 4;
            Vector3 P(int ring, int side)
            {
                float a = Mathf.PI * ring / rings, b = Mathf.PI * 2f * side / sides;
                return center + Vector3.Scale(new Vector3(Mathf.Sin(a)*Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a)*Mathf.Sin(b)), size)*0.5f;
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < sides; s++) Quad(P(r,s),P(r,s+1),P(r+1,s+1),P(r+1,s),color,false);
        }

        public void Beam(Vector3 start, Vector3 end, float thickness, Color color)
        { Box((start+end)*0.5f, new Vector3(thickness,thickness,(end-start).magnitude), color, Quaternion.LookRotation(end-start)); }

        public void Add(ComicMesh other, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            int n = vertices.Count;
            for (int i = 0; i < other.vertices.Count; i++)
            {
                vertices.Add(position + rotation * Vector3.Scale(other.vertices[i], scale));
                normals.Add((rotation * new Vector3(other.normals[i].x/scale.x,other.normals[i].y/scale.y,other.normals[i].z/scale.z)).normalized);
                colors.Add(other.colors[i]);
                inkUV.Add(other.inkUV[i]);
                surfaces.Add(other.surfaces[i]);
            }
            foreach (int i in other.triangles) triangles.Add(n+i);
        }

        // Surface mesh only: shear road and sidewalk vertices, never the upright building volumes.
        public void Shear(float grade)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 p = vertices[i]; p.y -= p.z * grade; vertices[i] = p;
            }
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int a=triangles[i], b=triangles[i+1], c=triangles[i+2];
                Vector3 n=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).normalized;
                normals[a]=normals[b]=normals[c]=n;
            }
        }

        public Mesh Bake(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colors); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds();
            mesh.SetUVs(0, inkUV);
            // Metric dominant-face projection is baked after assembly/scaling, before GPU bending.
            // UV2: metres U/V, stable material slice, repetitions per metre. UV1 remains outline normals.
            var surfaceUV=new List<Vector4>(vertices.Count);
            for(int i=0;i<vertices.Count;i++)
            {
                Vector3 p=vertices[i],n=normals[i];Vector2 uv;
                if(Mathf.Abs(n.y)>=Mathf.Max(Mathf.Abs(n.x),Mathf.Abs(n.z)))uv=new Vector2(p.x,p.z);
                else if(Mathf.Abs(n.x)>Mathf.Abs(n.z))uv=new Vector2(p.z,p.y);
                else uv=new Vector2(p.x,p.y);
                surfaceUV.Add(new Vector4(uv.x,uv.y,(int)surfaces[i],ComicSurfaceAssets.Tiling(surfaces[i])));
            }
            mesh.SetUVs(2,surfaceUV);
            // Weld by position only for silhouette extrusion; keep original flat normals for cel shading.
            var groups = new Dictionary<Vector3Int, HashSet<Vector3Int>>();
            Vector3Int Key(Vector3 v) => new Vector3Int(Mathf.RoundToInt(v.x*10000), Mathf.RoundToInt(v.y*10000), Mathf.RoundToInt(v.z*10000));
            for (int i=0;i<vertices.Count;i++)
            {
                Vector3Int key=Key(vertices[i]);
                if(!groups.TryGetValue(key,out var directions)) groups[key]=directions=new HashSet<Vector3Int>();
                directions.Add(Key(normals[i]));
            }
            var smooth = new Dictionary<Vector3Int, Vector3>();
            foreach(var group in groups)
            {
                Vector3 sum=Vector3.zero;
                foreach(var direction in group.Value) sum+=(Vector3)direction;
                smooth[group.Key]=sum.normalized;
            }
            var outlineNormals=new List<Vector3>(vertices.Count);
            for(int i=0;i<vertices.Count;i++) outlineNormals.Add(smooth[Key(vertices[i])]);
            mesh.SetUVs(1,outlineNormals);
            // Cover the maximum small screen-space shell expansion during culling.
            Bounds bounds=mesh.bounds; bounds.Expand(.3f); mesh.bounds=bounds;
            return mesh;
        }
    }
}
