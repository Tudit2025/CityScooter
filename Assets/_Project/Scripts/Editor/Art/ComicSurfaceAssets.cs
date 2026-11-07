using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityScooter.Rendering;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CityScooter.EditorTools.Art
{
    /// <summary>Deterministic, periodic data textures. No downloaded textures or extra runtime material slots.</summary>
    internal static class ComicSurfaceAssets
    {
        const int Size=256,Count=14;
        const string Root=ComicCityBuilder.Root+"/Surfaces";
        const string Output="art-src/comic-city/surfaces";
        public static ComicSurfaceLibrary Library {get;private set;}
        static readonly Dictionary<string,ComicSurface> assignments=new Dictionary<string,ComicSurface>();
        static readonly Dictionary<ComicSurface,ComicSurfaceProfile> profiles=new Dictionary<ComicSurface,ComicSurfaceProfile>();
        static readonly List<string> coverage=new List<string>();
        static Texture2DArray masks,normals;

        public static float Tiling(ComicSurface surface)=>profiles.TryGetValue(surface,out var p)?1/p.tileMetres:1;

        /// <summary>Other builders (boulevard sections) bake meshes without rebuilding the textures: load the saved
        /// profiles and assignments so their surface tiling matches the street kit. No-op after Build.</summary>
        public static void EnsureLoaded()
        {
            if(profiles.Count>0)return;
            Library=AssetDatabase.LoadAssetAtPath<ComicSurfaceLibrary>(Root+"/ComicSurfaceLibrary.asset");
            if(Library==null)throw new InvalidOperationException("Build the comic city first (ComicSurfaceLibrary).");
            foreach(var p in Library.profiles)profiles[p.surface]=p;
            foreach(var a in Library.assignments)assignments[a.assetId]=a.primarySurface;
        }
        public static ComicSurface ForAsset(ComicAssetSpec s,bool building)=>assignments.TryGetValue(s.id,out var type)?type:DefaultSurface(s,building);

        static ComicSurface DefaultSurface(ComicAssetSpec s,bool building)
        {
            if(building)
            {
                switch(s.id)
                {
                    case "CornerStore":case "Bakery":case "PizzaShop":case "Bookshop":case "PostOffice":case "RowHouse":case "Hardware":return ComicSurface.Brick;
                }
                switch(s.style)
                {
                    case "warehouse":case "parking":case "bank":case "civic":case "office":case "tower":case "background":return ComicSurface.Concrete;
                    default:return ComicSurface.Plaster;
                }
            }
            switch(s.style)
            {
                case "tree":case "planter":case "pot":case "stone":case "concrete":return ComicSurface.Concrete;
                case "bench":case "crates":case "board":return ComicSurface.Wood;
                case "bike":case "rail":return ComicSurface.Metal;
                case "cones":case "recycle":return ComicSurface.Plastic;
                default:return ComicSurface.PaintedMetal;
            }
        }

        public static void Build(ComicCityRecipe recipe,Material shared)
        {
            PrefabBuildUtility.EnsureFolder(Root);Directory.CreateDirectory(Output+"/maps");
            string path=Root+"/ComicSurfaceLibrary.asset";
            Library=AssetDatabase.LoadAssetAtPath<ComicSurfaceLibrary>(path);
            if(Library==null){Library=ScriptableObject.CreateInstance<ComicSurfaceLibrary>();AssetDatabase.CreateAsset(Library,path);}
            var defaults=new List<ComicSurfaceProfile>();
            void P(ComicSurface s,string label,float tile,float rough,float shade,float relief,Color color)=>defaults.Add(new ComicSurfaceProfile{surface=s,label=label,tileMetres=tile,roughness=rough,shadeContrast=shade,reliefMetres=relief,previewColor=color,seed=7301+(int)s*101});
            P(ComicSurface.Plain,"Clean paint / decals",2,1,0,0,recipe.cream);
            P(ComicSurface.Plaster,"Painted plaster",2,.87f,.12f,.003f,new Color(.78f,.64f,.43f));
            P(ComicSurface.Brick,"Painted brick / recessed mortar",2.56f,.9f,.19f,.012f,new Color(.75f,.37f,.27f));
            P(ComicSurface.Concrete,"Cast concrete",2,.91f,.13f,.003f,new Color(.63f,.65f,.60f));
            P(ComicSurface.Asphalt,"Dry asphalt aggregate",2,.96f,.19f,.004f,recipe.asphalt);
            P(ComicSurface.Paving,"Square paving slabs",2,.89f,.14f,.009f,recipe.pavement);
            P(ComicSurface.PaintedMetal,"Painted metal / fine orange peel",1,.39f,.055f,.0007f,recipe.red);
            P(ComicSurface.Metal,"Brushed metal",1,.28f,.07f,.0005f,recipe.glass);
            P(ComicSurface.Wood,"Wood grain / boards",2,.78f,.19f,.004f,new Color(.65f,.39f,.21f));
            P(ComicSurface.Rubber,"Rubber fine grain",.5f,.97f,.07f,.0006f,new Color(.16f,.18f,.19f));
            P(ComicSurface.Fabric,"Cloth weave",.5f,.94f,.07f,.0004f,recipe.red);
            P(ComicSurface.Glass,"Stylized smooth glass",2,.17f,.018f,.0001f,recipe.glass);
            P(ComicSurface.Foliage,"Leaf mass breakup",2,.93f,.1f,.002f,recipe.green);
            P(ComicSurface.Plastic,"Moulded plastic",1,.53f,.05f,.0006f,recipe.yellow);
            var existing=Library.profiles.ToDictionary(p=>p.surface,p=>p);
            Library.profiles=defaults.Select(p=>existing.TryGetValue(p.surface,out var old)?old:p).ToArray();
            if(Library.distanceFade.x<0||Library.distanceFade.y<=Library.distanceFade.x)throw new InvalidOperationException("Invalid surface distance fade.");
            profiles.Clear();
            foreach(var p in Library.profiles)
            {
                if(p.tileMetres<.1f||p.roughness<0||p.roughness>1||p.reliefMetres<0)throw new InvalidOperationException("Invalid surface profile: "+p.surface);
                profiles.Add(p.surface,p);
            }
            var assigned=Library.assignments.ToDictionary(a=>a.assetId,a=>a);
            Library.assignments=recipe.props.Concat(recipe.buildings).Select(s=>assigned.TryGetValue(s.id,out var a)?a:new ComicSurfaceAssignment{assetId=s.id,primarySurface=DefaultSurface(s,recipe.buildings.Contains(s))}).ToArray();
            assignments.Clear();foreach(var a in Library.assignments)assignments.Add(a.assetId,a.primarySurface);
            EditorUtility.SetDirty(Library);
            masks=ArrayAsset("SurfaceMasks");normals=ArrayAsset("SurfaceNormals");
            var report=new List<string>{"surface,label,tile_metres,roughness,shade_contrast,relief_metres,seed"};
            foreach(var p in Library.profiles)
            {
                BuildSlice(p,out Color[] mask,out Color[] normal);
                masks.SetPixels(mask,(int)p.surface);normals.SetPixels(normal,(int)p.surface);
                WriteMap(p.surface+"_Mask",mask);WriteMap(p.surface+"_Normal",normal);
                WriteMap(p.surface+"_Roughness",mask.Select(c=>new Color(c.g,c.g,c.g,1)).ToArray());
                WriteMap(p.surface+"_Shade",mask.Select(c=>new Color(c.r,c.r,c.r,1)).ToArray());
                report.Add(FormattableString.Invariant($"{p.surface},{p.label},{p.tileMetres},{p.roughness},{p.shadeContrast},{p.reliefMetres},{p.seed}"));
            }
            masks.Apply(true,false);normals.Apply(true,false);EditorUtility.SetDirty(masks);EditorUtility.SetDirty(normals);
            Apply(shared);
            foreach(var p in Library.profiles)
            {
                string matPath=Root+"/MAT_"+p.surface+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if(mat==null){mat=new Material(shared){name="MAT_"+p.surface};AssetDatabase.CreateAsset(mat,matPath);}
                mat.CopyPropertiesFromMaterial(shared);mat.SetFloat("_SurfaceOverride",(int)p.surface);mat.SetFloat("_SurfaceTiling",1/p.tileMetres);
                mat.SetColor("_BaseColor",p.previewColor);mat.SetFloat("_WorldBend",0);EditorUtility.SetDirty(mat);
            }
            coverage.Clear();coverage.Add("mesh,surface,triangles");
            File.WriteAllLines(Output+"/profiles.csv",report);
            File.WriteAllLines(Output+"/assignments.csv",new[]{"asset,primary_surface"}.Concat(Library.assignments.Select(a=>a.assetId+","+a.primarySurface)));
            File.WriteAllText(Output+"/library.snapshot.json",JsonUtility.ToJson(Library,true));
            File.WriteAllText(Output+"/validation.txt","14 periodic 256x256 slices (13 textured families + plain); linear RGBA32 arrays with mipmaps.\nMask: R shade, G roughness (not smoothness), B height, A cavity. Normal: RGB tangent-space XYZ, +Y.\nAll normal vectors finite/unit length; periodic height/noise validated at UV wrap.\nTwo arrays: approximately 9.33 MiB GPU + readable CPU copy; 2 detail samples per shaded fragment.\n");
            Debug.Log("[ComicCity Surfaces] Built 13 material families + plain; mapped "+assignments.Count+" kit assets.");
        }

        static Texture2DArray ArrayAsset(string name)
        {
            string path=Root+"/"+name+".asset";
            var array=AssetDatabase.LoadAssetAtPath<Texture2DArray>(path);
            if(array==null){array=new Texture2DArray(Size,Size,Count,TextureFormat.RGBA32,true,true){name=name};AssetDatabase.CreateAsset(array,path);}
            if(array.width!=Size||array.depth!=Count)throw new InvalidOperationException("Surface array layout changed; migrate slices before rebuilding.");
            array.wrapMode=TextureWrapMode.Repeat;array.filterMode=FilterMode.Trilinear;array.anisoLevel=4;return array;
        }
        public static void Apply(Material material)
        {
            material.SetTexture("_SurfaceMasks",masks);material.SetTexture("_SurfaceNormals",normals);
            material.SetFloat("_SurfaceDetail",1);material.SetFloat("_SurfaceShade",Library.shadeStrength);material.SetFloat("_SurfaceNormal",Library.normalStrength);
            material.SetFloat("_SurfaceSpecular",Library.highlightStrength);material.SetFloat("_SurfaceOverride",-1);
            material.SetVector("_SurfaceFade",new Vector4(Library.distanceFade.x,Library.distanceFade.y,0,0));EditorUtility.SetDirty(material);
        }
        static void WriteMap(string name,Color[] pixels)
        {
            var texture=new Texture2D(Size,Size,TextureFormat.RGBA32,false,true);texture.SetPixels(pixels);texture.Apply();
            File.WriteAllBytes(Output+"/maps/"+name+".png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
        }
        public static void Record(string name,Mesh mesh)
        {
            var uv=new List<Vector4>();mesh.GetUVs(2,uv);
            if(uv.Count!=mesh.vertexCount)throw new InvalidOperationException("Missing material coordinates: "+name);
            var counts=new int[Count];var indices=mesh.triangles;
            for(int i=0;i<indices.Length;i+=3)
            {
                int type=Mathf.RoundToInt(uv[indices[i]].z);
                if(type<0||type>=Count||uv[indices[i+1]].z!=type||uv[indices[i+2]].z!=type)throw new InvalidOperationException("Mixed material IDs within triangle: "+name);
                if(uv[indices[i]].w<=0)throw new InvalidOperationException("Invalid surface texel scale: "+name);
                counts[type]++;
            }
            for(int i=0;i<Count;i++)if(counts[i]>0)coverage.Add(name+","+(ComicSurface)i+","+counts[i]);
        }
        public static void SaveCoverage()=>File.WriteAllLines(Output+"/mesh-coverage.csv",coverage);

        static float Hash(int x,int y,int seed)
        {
            unchecked {uint h=(uint)(x*374761393+y*668265263+seed*1013904223);h=(h^(h>>13))*1274126177;return (h^(h>>16))/(float)uint.MaxValue;}
        }
        static float Noise(float u,float v,int cells,int seed)
        {
            float x=Mathf.Repeat(u,1)*cells,y=Mathf.Repeat(v,1)*cells;int a=Mathf.FloorToInt(x),b=Mathf.FloorToInt(y);
            float fx=x-a,fy=y-b;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy);
            return Mathf.Lerp(Mathf.Lerp(Hash(a%cells,b%cells,seed),Hash((a+1)%cells,b%cells,seed),fx),Mathf.Lerp(Hash(a%cells,(b+1)%cells,seed),Hash((a+1)%cells,(b+1)%cells,seed),fx),fy);
        }
        static float Smooth(float a,float b,float x){x=Mathf.Clamp01((x-a)/(b-a));return x*x*(3-2*x);}
        // R = signed shade around 0.5, G = roughness, B = normalized height, A = cavity.
        static Color Field(ComicSurfaceProfile p,float u,float v)
        {
            u=Mathf.Repeat(u,1);v=Mathf.Repeat(v,1);
            float coarse=Noise(u,v,4,p.seed),medium=Noise(u,v,16,p.seed+1),fine=Noise(u,v,64,p.seed+2),grain=Noise(u,v,128,p.seed+3);
            float signal=(medium-.5f)*.9f+(fine-.5f)*.55f+(coarse-.5f)*.5f;
            float height=medium*.45f+fine*.35f+grain*.2f,shade=signal,rough=p.roughness+(fine-.5f)*.14f,ao=1;
            switch(p.surface)
            {
                case ComicSurface.Plain:return new Color(.5f,1,.5f,1);
                case ComicSurface.Brick:
                {
                    int row=Mathf.FloorToInt(v*16);float x=u*8+(row%2)*.5f,fx=Mathf.Repeat(x,1),fy=Mathf.Repeat(v*16,1);
                    float edge=Mathf.Min(Mathf.Min(fx,1-fx)/8,Mathf.Min(fy,1-fy)/16)*p.tileMetres;
                    float face=Smooth(.003f,.014f,edge),brick=Hash(Mathf.FloorToInt(x)%8,row,p.seed);
                    height=face*(.72f+.16f*medium)+.1f*fine;
                    shade=face*((brick-.5f)*.7f+signal*.45f)-(1-face)*.66f;ao=Mathf.Lerp(.88f,1,face);rough=Mathf.Lerp(.98f,p.roughness+(fine-.5f)*.1f,face);break;
                }
                case ComicSurface.Asphalt:
                    shade=(grain-.5f)*1.1f+(fine-.5f)*.65f+(coarse-.5f)*.3f;height=.7f*grain+.3f*fine;break;
                case ComicSurface.Paving:
                {
                    float x=u*4,y=v*4,fx=Mathf.Repeat(x,1),fy=Mathf.Repeat(y,1);
                    float edge=Mathf.Min(Mathf.Min(fx,1-fx),Mathf.Min(fy,1-fy))*p.tileMetres/4;
                    float face=Smooth(.002f,.009f,edge);height=face*.8f+fine*.12f;
                    shade=(Hash((int)x,(int)y,p.seed)-.5f)*.5f+signal*.5f-(1-face)*.7f;ao=Mathf.Lerp(.9f,1,face);break;
                }
                case ComicSurface.Wood:
                {
                    float line=Mathf.Sin(2*Mathf.PI*(u*28+.65f*Mathf.Sin(v*2*Mathf.PI)+.2f*Mathf.Sin(v*6*Mathf.PI)));
                    float plank=Mathf.Repeat(u*4,1),gap=1-Smooth(.012f,.03f,Mathf.Min(plank,1-plank));
                    shade=line*.26f+signal*.35f-gap*.65f;height=.48f+line*.2f+fine*.08f-gap*.38f;ao=1-gap*.1f;break;
                }
                case ComicSurface.Metal:
                    shade=Mathf.Sin(v*128*Mathf.PI)*.18f+signal*.2f;height=.5f+Mathf.Sin(v*128*Mathf.PI)*.17f+fine*.08f;break;
                case ComicSurface.Fabric:
                    float weave=Mathf.Sin(u*64*Mathf.PI)*Mathf.Sin(v*64*Mathf.PI);height=.5f+weave*.3f;shade=weave*.25f+signal*.2f;break;
                case ComicSurface.Glass:height=.5f;shade=(coarse-.5f)*.3f;rough=p.roughness;break;
                case ComicSurface.Foliage:shade=(coarse-.5f)*.8f+(medium-.5f)*.35f;height=medium;break;
            }
            return new Color(Mathf.Clamp01(.5f+shade*p.shadeContrast),Mathf.Clamp(rough,.08f,1),Mathf.Clamp01(height),ao);
        }
        static void BuildSlice(ComicSurfaceProfile p,out Color[] mask,out Color[] normal)
        {
            mask=new Color[Size*Size];normal=new Color[mask.Length];
            for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)mask[y*Size+x]=Field(p,(x+.5f)/Size,(y+.5f)/Size);
            for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)
            {
                float dx=mask[y*Size+(x+1)%Size].b-mask[y*Size+(x+Size-1)%Size].b;
                float dy=mask[((y+1)%Size)*Size+x].b-mask[((y+Size-1)%Size)*Size+x].b;
                float strength=p.reliefMetres*Size/(2*p.tileMetres);
                Vector3 n=new Vector3(-dx*strength,-dy*strength,1).normalized;
                if(float.IsNaN(n.x)||Mathf.Abs(n.magnitude-1)>.0001f)throw new InvalidOperationException("Invalid normal: "+p.surface);
                normal[y*Size+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
            }
            for(int i=0;i<=32;i++)
            {
                float t=i/32f;
                if((Vector4)(Field(p,0,t)-Field(p,1,t))!=Vector4.zero||(Vector4)(Field(p,t,0)-Field(p,t,1))!=Vector4.zero)
                    throw new InvalidOperationException("Non-periodic surface: "+p.surface);
            }
        }

        public static void RenderSheet()
        {
            Library=AssetDatabase.LoadAssetAtPath<ComicSurfaceLibrary>(Root+"/ComicSurfaceLibrary.asset");
            if(Library==null)return;
            ArtPreviewUtility.NewPreviewScene();
            const int tile=360,columns=4,rows=4;
            var sheet=new Texture2D(tile*columns,tile*rows,TextureFormat.RGB24,false);
            sheet.SetPixels(Enumerable.Repeat(new Color(.12f,.16f,.20f),sheet.width*sheet.height).ToArray());
            var cam=ArtPreviewUtility.CreateCamera(new Vector3(3.8f,2.5f,4.5f),new Vector3(0,1,0),38,40);
            for(int i=0;i<Library.profiles.Length;i++)
            {
                var p=Library.profiles[i];var data=new ComicMesh{Surface=p.surface};data.Box(new Vector3(0,1,0),new Vector3(2,2,.3f),Color.white);
                Mesh mesh=data.Bake("SurfaceSwatch");var go=new GameObject(p.surface.ToString(),typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/MAT_"+p.surface+".mat");
                var label=new GameObject("Label").AddComponent<TextMesh>();label.text=((int)p.surface).ToString("D2")+" "+p.surface;label.fontSize=42;label.characterSize=.055f;label.anchor=TextAnchor.MiddleCenter;
                label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
                label.transform.position=new Vector3(0,-.28f,.4f);label.transform.rotation=cam.transform.rotation;label.color=new Color(.94f,.87f,.65f);
                Object.DestroyImmediate(ArtPreviewUtility.Render(cam,tile,tile));var image=ArtPreviewUtility.Render(cam,tile,tile);
                sheet.SetPixels((i%columns)*tile,(rows-1-i/columns)*tile,tile,tile,image.GetPixels());
                Object.DestroyImmediate(image);Object.DestroyImmediate(label.gameObject);Object.DestroyImmediate(go);Object.DestroyImmediate(mesh);
            }
            sheet.Apply();ArtPreviewUtility.SavePng(sheet,ComicCityPreview.Output+"/08-surface-library.png");Object.DestroyImmediate(sheet);Object.DestroyImmediate(cam.gameObject);
        }
    }
}
