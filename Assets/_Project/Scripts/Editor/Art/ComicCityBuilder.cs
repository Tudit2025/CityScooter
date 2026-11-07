using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CityScooter.Gameplay;
using CityScooter.Rendering;
using CityScooter.Track;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace CityScooter.EditorTools.Art
{
    /// <summary>Generated objects are edited by stable names. Recipe data and prefab variants are never overwritten.</summary>
    public static class ComicCityBuilder
    {
        public const string Root="Assets/_Project/Art/ComicCity";
        public const string Prefabs="Assets/_Project/Prefabs/ComicCity";
        public const string RecipePath=Root+"/ComicCityRecipe.asset";
        public const string ThemePath="Assets/_Project/Data/Theme_ComicDownhill.asset";
        public const string MainPath="Assets/_Project/Scenes/Main.unity";
        public static readonly string[] ChunkIds={"NearShops","OpenSquare","NearApartments","OpenTransit"};
        static ComicCityRecipe recipe;
        static Material material;
        static Material outlineMaterial;
        static readonly Dictionary<string,ComicMesh> geometry=new Dictionary<string,ComicMesh>();
        static readonly List<string> report=new List<string>();
        static readonly List<string> dressingReport=new List<string>();
        static HashSet<int> roadsideSelections;
        const int RoadsideBaseline = 22;

        [MenuItem("CityScooter/Comic City/1 - Rebuild Kit and Street")]
        public static void Build()
        {
            PrefabBuildUtility.EnsureFolder(Root);
            PrefabBuildUtility.EnsureFolder(Root+"/Meshes");
            PrefabBuildUtility.EnsureFolder(Prefabs);
            recipe=AssetDatabase.LoadAssetAtPath<ComicCityRecipe>(RecipePath);
            if(recipe==null){recipe=ScriptableObject.CreateInstance<ComicCityRecipe>();ComicCityDefaults.Populate(recipe);AssetDatabase.CreateAsset(recipe,RecipePath);}
            ConfigurePanorama();
            ValidateRecipe(recipe);
            Shader shader=Shader.Find("CityScooter/Comic Vertex");
            if(shader==null)throw new InvalidOperationException("Comic Vertex shader has not imported.");
            material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/ComicPalette.mat");
            if(material==null){material=new Material(shader){name="ComicPalette"};AssetDatabase.CreateAsset(material,Root+"/ComicPalette.mat");}
            material.SetColor("_ShadowTint",recipe.shadowTint);
            material.SetColor("_InkColor",recipe.outlineColor);
            material.SetFloat("_CreasePixels",recipe.creasePixels);
            material.SetFloat("_Halftone",recipe.halftoneStrength);
            material.SetFloat("_WorldBend",1);
            ComicSurfaceAssets.Build(recipe,material);
            EditorUtility.SetDirty(material);
            outlineMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/ComicInk.mat");
            if(outlineMaterial==null)
            {
                Shader inkShader=Shader.Find("CityScooter/Comic Outline");
                if(inkShader==null)throw new InvalidOperationException("Comic Outline shader has not imported.");
                outlineMaterial=new Material(inkShader){name="ComicInk"};AssetDatabase.CreateAsset(outlineMaterial,Root+"/ComicInk.mat");
            }
            outlineMaterial.SetColor("_InkColor",recipe.outlineColor);
            outlineMaterial.SetFloat("_OutlinePixels",recipe.outlinePixels);
            EditorUtility.SetDirty(outlineMaterial);
            geometry.Clear();report.Clear();report.Add("id,label,category,width_m,height_m,depth_m,triangles,renderers,colliders");
            PrepareRoadsideLayouts();
            foreach(ComicAssetSpec s in recipe.props)BuildAsset(s,false);
            foreach(ComicAssetSpec s in recipe.buildings)BuildAsset(s,true);
            BuildRider();
            BuildBackdrop();
            for(int i=0;i<ChunkIds.Length;i++)BuildChunk(i);
            BuildTheme();
            ComicSurfaceAssets.SaveCoverage();
            ComicCityAudioBuilder.Build();
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("art-src/comic-city");
            File.WriteAllLines("art-src/comic-city/catalog.csv",report, new UTF8Encoding(false));
            File.WriteAllText("art-src/comic-city/recipe.snapshot.json",JsonUtility.ToJson(recipe,true),new UTF8Encoding(false));
            File.WriteAllLines("art-src/comic-city/roadside-layouts.csv",dressingReport,new UTF8Encoding(false));
            int baseline=RoadsideBaseline*ChunkIds.Length*recipe.roadsideLayoutVariants;
            if(dressingReport.Count-1!=roadsideSelections.Count)throw new InvalidOperationException("Roadside layout count does not match density budget.");
            File.WriteAllText("art-src/comic-city/roadside-density.txt",
                "Baseline small props across layout bank: "+baseline+"\nGenerated small props: "+roadsideSelections.Count+
                "\nDensity ratio: "+((float)roadsideSelections.Count/baseline).ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+
                "\nLayout choices per chunk: "+recipe.roadsideLayoutVariants+"\nSeed: "+recipe.seed+"\n");
            Debug.Log("[ComicCity] Built "+recipe.props.Length+" props, "+recipe.buildings.Length+" buildings, rider, 4 street chunks. Recipe retained: "+RecipePath);
        }

        static void ValidateRecipe(ComicCityRecipe r)
        {
            if(r.chunkLength!=40||r.roadHalfWidth!=4)throw new InvalidOperationException("Prototype layout requires 40 m chunks and 8 m road. Update lane slots/layout before changing these.");
            if(r.sidewalkWidth<4||r.downhillGrade<=0||r.downhillGrade>.35f||r.openSetback<0)throw new InvalidOperationException("Invalid sidewalk, grade or setback.");
            if(r.cameraFov<40||r.cameraMaxFov<r.cameraFov||r.cameraMaxFov>110||r.cameraOffset.z>=-1||r.cameraLookAhead<=0)throw new InvalidOperationException("Invalid chase camera recipe.");
            if(r.roadsidePropDensity<0||r.roadsidePropDensity>1||r.roadsideLayoutVariants<1||r.roadsideLayoutVariants>16)throw new InvalidOperationException("Invalid roadside density/layout count.");
            if(r.cityBendStart<20||r.cityBendStrength<0||r.cityBendStrength>.003f||r.horizonTaperStart<100||r.horizonTaperEnd<=r.horizonTaperStart+40||r.horizonTaperEnd>600||r.horizonMinimumScale<.001f||r.horizonMinimumScale>.1f||r.curveTransitionDistance<r.cityBendStart||r.curveEaseLength<=0||r.skyVerticalScale<.2f||r.skyPanorama==null)
                throw new InvalidOperationException("Invalid city convergence/image sky configuration.");
            if(r.skyApparentScale<.4f||r.skyApparentScale>1f)throw new InvalidOperationException("Sky apparent scale must be between 0.4 and 1.");
            var ids=new HashSet<string>();
            foreach(var s in r.props.Concat(r.buildings))
                if(string.IsNullOrEmpty(s.id)||!ids.Add(s.id)||s.width<=0||s.height<=0||s.depth<=0||s.id.Any(c=>!char.IsLetterOrDigit(c)))throw new InvalidOperationException("Invalid or duplicate asset ID/dimensions: "+s.id);
        }

        static Mesh SaveMesh(string id,ComicMesh data)
        {
            string path=Root+"/Meshes/"+id+".asset";Mesh mesh=data.Bake(id),existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            ComicSurfaceAssets.Record(id,mesh);
            if(id.StartsWith("CHN_")||id=="HorizonRoad")
            {
                // Unity culls on CPU, before the vertex shader. Cover the full 320 m build range + one chunk.
                Bounds b=mesh.bounds;
                float lift=ComicCityProjection.Rise(recipe,id=="HorizonRoad"?recipe.horizonTaperEnd+40:360)+1;
                b.Encapsulate(b.max+Vector3.up*lift);mesh.bounds=b;
            }
            if(existing==null){AssetDatabase.CreateAsset(mesh,path);existing=mesh;}
            else
            {
                // CopySerialized can leave a loaded Mesh's GPU buffers stale during same-session previews.
                // Use the Mesh setters so CPU data, GPU data and bounds all change together; keep the asset GUID.
                var writable=new SerializedObject(existing);writable.FindProperty("m_IsReadable").boolValue=true;writable.ApplyModifiedPropertiesWithoutUndo();
                existing.Clear();existing.indexFormat=mesh.indexFormat;
                existing.SetVertices(mesh.vertices);existing.SetNormals(mesh.normals);existing.SetColors(mesh.colors);
                var uv=new List<Vector3>();mesh.GetUVs(0,uv);existing.SetUVs(0,uv);
                mesh.GetUVs(1,uv);existing.SetUVs(1,uv);
                var surfaceUV=new List<Vector4>();mesh.GetUVs(2,surfaceUV);existing.SetUVs(2,surfaceUV);
                existing.SetTriangles(mesh.triangles,0);existing.bounds=mesh.bounds;
                existing.UploadMeshData(false);
                Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);
            }
            var so=new SerializedObject(existing);var readable=so.FindProperty("m_IsReadable");
            if(readable!=null){readable.boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();}
            return existing;
        }

        static void Renderer(GameObject go,string id,ComicMesh data,bool shadows=true)
        {
            Mesh mesh=SaveMesh(id,data);
            PrefabBuildUtility.GetOrAdd<MeshFilter>(go).sharedMesh=mesh;
            var mr=PrefabBuildUtility.GetOrAdd<MeshRenderer>(go);mr.sharedMaterial=material;
            mr.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;
            mr.receiveShadows=true;
            var children=new ChildSync(go.transform);
            if(shadows)
            {
                GameObject outline=children.Plain("InkOutline");
                PrefabBuildUtility.GetOrAdd<MeshFilter>(outline).sharedMesh=mesh;
                var ink=PrefabBuildUtility.GetOrAdd<MeshRenderer>(outline);ink.sharedMaterial=outlineMaterial;
                ink.shadowCastingMode=ShadowCastingMode.Off;ink.receiveShadows=false;
            }
            children.Finish();
        }

        static string AssetPath(string id,bool building)=>Prefabs+"/"+(building?"Buildings/BLD_":"Props/PRP_")+id+".prefab";
        public static string ChunkPath(string id)=>Prefabs+"/Street/CHN_"+id+".prefab";

        static void PrepareRoadsideLayouts()
        {
            int total=RoadsideBaseline*ChunkIds.Length*recipe.roadsideLayoutVariants;
            var candidates=Enumerable.Range(0,total).ToArray();
            var rng=new System.Random(recipe.seed ^ 0x4d275f31);
            for(int i=candidates.Length-1;i>0;i--)
            {
                int j=rng.Next(i+1);(candidates[i],candidates[j])=(candidates[j],candidates[i]);
            }
            // A fixed population budget gives exactly 10% over the whole bank, rather than a noisy small sample.
            roadsideSelections=new HashSet<int>(candidates.Take(Mathf.RoundToInt(total*recipe.roadsidePropDensity)));
            dressingReport.Clear();dressingReport.Add("chunk,layout,side,asset,x_m,y_m,z_m,yaw_deg");
        }

        static void ScatterRoadside(ComicMesh mesh,int variant,int layout,List<DressingObstacle> collisions)
        {
            string[] curbPool={"Hydrant","Bin","RecycleBins","NewspaperBox","Mailbox","TreePit","Planter","FlowerPot","StonePost","Bollard","Rail","StreetLamp","StreetSign","ParkingMeter","Bench","BikeRack","ShopBoard","Crates","Cones","WorkBarrier","ConcreteBarrier"};
            string[] wallPool={"ParcelLocker","UtilityBox","AirConditioner","VendingMachine","FlowerPot","Planter","ShopBoard","Crates"};
            var rng=new System.Random(unchecked(recipe.seed*486187739 ^ variant*16777619 ^ layout*104729));
            float Random(float min,float max)=>Mathf.Lerp(min,max,(float)rng.NextDouble());
            float half=recipe.roadHalfWidth,edge=half+recipe.sidewalkWidth;
            int group=(variant*recipe.roadsideLayoutVariants+layout)*RoadsideBaseline;
            for(int sideIndex=0;sideIndex<2;sideIndex++)
            {
                int side=sideIndex==0?-1:1;
                var occupied=new List<Vector2>();
                string lastId=null;
                for(int slot=0;slot<11;slot++)
                {
                    if(!roadsideSelections.Contains(group+sideIndex*11+slot))continue;
                    bool atWall=slot>=8;
                    string[] pool=atWall?wallPool:curbPool;
                    int pick=rng.Next(pool.Length);
                    if(pool[pick]==lastId)pick=(pick+1+rng.Next(pool.Length-1))%pool.Length;
                    string id=pool[pick];lastId=id;
                    ComicAssetSpec spec=recipe.props.First(s=>s.id==id);
                    float jitter=id=="TreePit"?0:Random(-recipe.roadsideYawJitter,recipe.roadsideYawJitter);
                    float yaw=(side<0?90:-90)+jitter;
                    float sin=Mathf.Abs(Mathf.Sin(yaw*Mathf.Deg2Rad)),cos=Mathf.Abs(Mathf.Cos(yaw*Mathf.Deg2Rad));
                    float extentX=(spec.width*cos+spec.depth*sin)*.5f,extentZ=(spec.width*sin+spec.depth*cos)*.5f;
                    if(extentX>1.001f)throw new InvalidOperationException(id+" is too wide for the 2 m roadside furniture strip.");
                    float x=side*(atWall?Random(edge-2+extentX,edge-extentX):Random(half+extentX,half+2-extentX));
                    float minZ=1+extentZ,maxZ=recipe.chunkLength-1-extentZ,z=0;
                    bool Free(float candidate)=>occupied.All(p=>Mathf.Abs(p.x-candidate)>=p.y+extentZ+.5f);
                    bool placed=false;
                    for(int attempt=0;attempt<96;attempt++)
                    {
                        z=Random(minZ,maxZ);if(Free(z)){placed=true;break;}
                    }
                    if(!placed)
                        for(z=minZ;z<=maxZ;z+=.5f)if(Free(z)){placed=true;break;}
                    if(!placed)throw new InvalidOperationException("Roadside props no longer fit: reduce density or dimensions.");
                    occupied.Add(new Vector2(z,extentZ));
                    float y=-recipe.downhillGrade*z+recipe.curbHeight;
                    mesh.Add(geometry[id],new Vector3(x,y,z),Quaternion.Euler(0,yaw,0),Vector3.one);
                    collisions.Add(PropCollision(spec,new Vector3(x,y,z),yaw));
                    dressingReport.Add(FormattableString.Invariant($"{ChunkIds[variant]},{layout},{side},{id},{x:F3},{y:F3},{z:F3},{yaw:F2}"));
                }
            }
        }

        static void BuildAsset(ComicAssetSpec s,bool building)
        {
            ComicMesh data=building?ComicKitGeometry.Building(s,recipe):ComicKitGeometry.Prop(s,recipe);
            geometry[s.id]=data;
            string path=AssetPath(s.id,building);GameObject root=PrefabBuildUtility.OpenPrefab(path);
            var sync=new ChildSync(root.transform);GameObject visual=sync.Plain("Visual");Renderer(visual,(building?"BLD_":"PRP_")+s.id,data);
            var collider=PrefabBuildUtility.GetOrAdd<BoxCollider>(root);
            collider.center=new Vector3(0,s.height*.5f,0);collider.size=new Vector3(s.width,s.height,s.depth);
            if(s.style=="tree"||s.style=="lamp"||s.style=="sign"){collider.size=new Vector3(.35f,s.height,.35f);}
            sync.Finish();PrefabBuildUtility.SaveAndClose(root,path);
            report.Add(string.Join(",",s.id,s.label,building?"building":"prop",s.width.ToString(System.Globalization.CultureInfo.InvariantCulture),s.height.ToString(System.Globalization.CultureInfo.InvariantCulture),s.depth.ToString(System.Globalization.CultureInfo.InvariantCulture),data.TriangleCount*2,2,1));
        }

        static DressingObstacle PropCollision(ComicAssetSpec spec,Vector3 position,float yaw)
        {
            var collider=AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath(spec.id,false)).GetComponent<BoxCollider>();
            return new DressingObstacle{id=spec.id,position=position,rotation=new Vector3(0,yaw,0),centre=collider.center,size=collider.size,canJumpOver=spec.height<=1.1f};
        }

        static void BuildRider()
        {
            string path=Prefabs+"/Rider/Rider_Comic.prefab";GameObject root=PrefabBuildUtility.OpenPrefab(path);
            var sync=new ChildSync(root.transform);Renderer(sync.Plain("RiderAndScooter"),"Rider_Comic",ComicKitGeometry.Rider(recipe));
            GameObject scarf=sync.Plain("ScarfAnchor");scarf.transform.localPosition=new Vector3(0,1.63f,.02f);
            var rest=new ComicMesh{Surface=ComicSurface.Fabric};
            for(int i=0;i<18;i++)
            {
                Vector3 Point(int j,float side){float t=j/18f;return new Vector3(Mathf.Sin(t*5)*.45f+side*.14f,-t*.25f,-t*3.2f);}
                rest.RibbonQuad(Point(i,-1),Point(i+1,-1),Point(i+1,1),Point(i,1),recipe.red,i/18f,(i+1)/18f);
                rest.RibbonQuad(Point(i,-1),Point(i+1,-1),Point(i+1,1),Point(i,1),recipe.red,i/18f,(i+1)/18f,true);
            }
            Renderer(scarf,"Scarf_Rest",rest,false);
            var ribbon=PrefabBuildUtility.GetOrAdd<ScarfRibbon>(scarf);ribbon.color=recipe.red;
            ribbon.surfaceTiling=ComicSurfaceAssets.Tiling(ComicSurface.Fabric);
            scarf.GetComponent<MeshRenderer>().sharedMaterial=material;
            scarf.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            sync.Finish();PrefabBuildUtility.SaveAndClose(root,path);
        }

        static void BuildBackdrop()
        {
            string path=Prefabs+"/Environment/CityBackdrop.prefab";GameObject root=PrefabBuildUtility.OpenPrefab(path);var sync=new ChildSync(root.transform);
            // The old city rings and cloud meshes are removed by ChildSync. The sky is RenderSettings.skybox.
            GameObject visual=sync.Plain("VisualRoadExtension");Renderer(visual,"HorizonRoad",ComicHorizonGeometry.Road(recipe),false);
            string materialPath=Root+"/ComicHorizonRoad.mat";
            Material horizon=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(horizon==null){horizon=new Material(material){name="ComicHorizonRoad"};AssetDatabase.CreateAsset(horizon,materialPath);}
            horizon.CopyPropertiesFromMaterial(material);horizon.SetFloat("_HorizonExtension",1);
            horizon.SetFloat("_FogOverride",.65f);horizon.SetFloat("_CreasePixels",0);
            visual.GetComponent<MeshRenderer>().sharedMaterial=horizon;EditorUtility.SetDirty(horizon);
            PrefabBuildUtility.GetOrAdd<ComicCityBackdrop>(root).recipe=recipe;
            sync.Finish();PrefabBuildUtility.SaveAndClose(root,path);
        }

        static void ConfigurePanorama()
        {
            if(recipe.skyPanorama==null)recipe.skyPanorama=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Perth_Comic_Panorama.png");
            if(recipe.skyPanorama==null)throw new InvalidOperationException("Missing Perth image panorama. Restore the saved texture before rebuilding.");
            string path=AssetDatabase.GetAssetPath(recipe.skyPanorama);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer!=null)
            {
                importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
                importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.isReadable=false;
                importer.wrapModeU=TextureWrapMode.Repeat;importer.wrapModeV=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Trilinear;importer.maxTextureSize=4096;
                importer.npotScale=TextureImporterNPOTScale.None;importer.alphaSource=TextureImporterAlphaSource.None;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            }
            recipe.schemaVersion=7;EditorUtility.SetDirty(recipe);
        }

        static void BuildChunk(int variant)
        {
            float length=recipe.chunkLength,grade=recipe.downhillGrade,half=recipe.roadHalfWidth,edge=half+recipe.sidewalkWidth;
            bool open=variant%2==1;string id=ChunkIds[variant],path=ChunkPath(id);
            GameObject root=PrefabBuildUtility.OpenPrefab(path);var sync=new ChildSync(root.transform);
            var chunk=PrefabBuildUtility.GetOrAdd<RoadChunk>(root);chunk.length=length;chunk.halfWidth=half;chunk.endHeight=-length*grade;
            chunk.sidewalkWidth=recipe.sidewalkWidth;chunk.sidewalkHeight=recipe.curbHeight;chunk.curbTransitionWidth=.3f;
            var road=new ComicMesh();var buildings=new ComicMesh();var props=new ComicMesh();var background=new ComicMesh();
            var fixedCollisions=new List<DressingObstacle>();
            Color ink=recipe.ink,cream=recipe.cream;
            void B(ComicMesh mesh,Vector3 p,Vector3 s,Color c)
            {
                if(mesh==road)mesh.Surface=c==recipe.asphalt?ComicSurface.Asphalt:c==recipe.pavement?ComicSurface.Paving:ComicSurface.Plain;
                else mesh.Surface=ComicSurface.Concrete;
                if(mesh==road&&s.z>4)mesh.BoxStrip(p,s,c);else mesh.Box(p,s,c);
            }
            B(road,new Vector3(0,-.4f,length/2),new Vector3(half*2,.8f,length),recipe.asphalt);
            for(int side=-1;side<=1;side+=2)
            {
                B(road,new Vector3(side*(half+recipe.sidewalkWidth/2),recipe.curbHeight/2-.2f,length/2),new Vector3(recipe.sidewalkWidth,recipe.curbHeight+.4f,length),recipe.pavement);
                B(road,new Vector3(side*(half+.12f),recipe.curbHeight+.01f,length/2),new Vector3(.24f,.06f,length),cream);
                B(road,new Vector3(side*(half-.15f),.018f,length/2),new Vector3(.08f,.025f,length),cream);
                for(int z=0;z<40;z+=2)B(road,new Vector3(side*(half+recipe.sidewalkWidth/2),recipe.curbHeight+.015f,z),new Vector3(recipe.sidewalkWidth,.02f,.025f),Color.Lerp(ink,recipe.pavement,.5f));
                for(int x=1;x<4;x++)B(road,new Vector3(side*(half+x),recipe.curbHeight+.015f,length/2),new Vector3(.025f,.02f,length),Color.Lerp(ink,recipe.pavement,.5f));
            }
            for(int z=0;z<40;z+=5)
                for(int side=-1;side<=1;side+=2)B(road,new Vector3(side*.12f,.02f,z+1.5f),new Vector3(.10f,.03f,3),recipe.yellow);
            if(open)
                for(int x=-3;x<=3;x++)B(road,new Vector3(x,.035f,20),new Vector3(.55f,.035f,3.5f),cream);
            road.Shear(grade);
            road.ClearCreaseInk(); // Recycled slab ends are not architectural edges; do not ink chunk seams.

            var boundaryGroup=sync.Plain("BoundaryColliders");var boundaries=new ChildSync(boundaryGroup.transform);
            string[][] rows={new[]{"CornerStore","Cafe","Bakery","ScooterShop","ShopApartment"},new[]{"Garage","Cafe","Diner","PostOffice","Clinic"},new[]{"CornerApartment","Bookshop","Laundry","PizzaShop","Hotel"},new[]{"Hardware","Clinic","Cafe","Garage","Bakery"}};
            for(int side=-1;side<=1;side+=2)
            {
                float setback=open?recipe.openSetback:0;
                // The setback square has a closed low retaining edge, never an opening to the map boundary.
                var terrace=new ComicMesh{Surface=ComicSurface.Concrete};
                terrace.BoxStrip(new Vector3(side*(edge+setback/2),-.3f,length/2),new Vector3(Mathf.Max(.1f,setback),1,length),recipe.pavement);
                terrace.BoxStrip(new Vector3(side*(edge+.15f),.55f,length/2),new Vector3(.3f,1.1f,length),ink);
                if(open){terrace.Shear(grade);buildings.Add(terrace,Vector3.zero,Quaternion.identity,Vector3.one);}
                for(int j=0;j<5;j++)
                {
                    string bid=rows[variant][(j+(side>0?2:0))%5];ComicAssetSpec spec=recipe.buildings.First(s=>s.id==bid);
                    float z=4+j*8,x=side*(edge+setback+spec.depth/2);
                    Vector3 p=new Vector3(x,-grade*z+recipe.curbHeight,z);Quaternion q=Quaternion.Euler(0,side<0?90:-90,0);
                    buildings.Add(geometry[bid],p,q,new Vector3(8/spec.width,1,1));
                    // Deep foundation fills the entire downhill gap below each upright building.
                    B(buildings,new Vector3(x,p.y-1.1f,z),new Vector3(spec.depth,2.2f,8),ink);
                    GameObject wall=boundaries.Plain((side<0?"L":"R")+j);
                    wall.transform.localPosition=new Vector3(side*(edge+setback+spec.depth/2),p.y+spec.height/2-1,z);
                    var box=PrefabBuildUtility.GetOrAdd<BoxCollider>(wall);box.center=Vector3.zero;box.size=new Vector3(spec.depth,spec.height+2,8);

                    float bx=side*(edge+setback+spec.depth+7),by=-grade*z;
                    // Three layers of closed volumes; simplified distant blocks save draw calls.
                    for(int layer=0;layer<2;layer++)
                    {
                        float bh=12+((j*7+variant*3+layer*5+(side+1))%5)*4;
                        Color bc=Color.Lerp(spec.color,new Color(.49f,.64f,.75f),.25f+layer*.22f);
                        B(background,new Vector3(bx+side*layer*13,by+bh/2-2,z),new Vector3(14,bh+4,8.2f),bc);
                        B(background,new Vector3(bx+side*layer*13,by+bh,z),new Vector3(14.2f,.3f,8.3f),Color.Lerp(bc,ink,.2f));
                    }
                }
                if(open)
                {
                    props.Add(geometry["BusShelter"],new Vector3(side*(edge+2.5f),-grade*20+recipe.curbHeight,20),Quaternion.Euler(0,side<0?90:-90,0),Vector3.one);
                    props.Add(ComicKitGeometry.Car(recipe,side<0?recipe.yellow:recipe.glass),new Vector3(side*(edge+2.5f),-grade*32+.25f,32),Quaternion.identity,Vector3.one);
                    fixedCollisions.Add(PropCollision(recipe.props.First(s=>s.id=="BusShelter"),new Vector3(side*(edge+2.5f),-grade*20+recipe.curbHeight,20),side<0?90:-90));
                    fixedCollisions.Add(new DressingObstacle{id="ParkedCar",position=new Vector3(side*(edge+2.5f),-grade*32+.25f,32),centre=new Vector3(0,.9f,0),size=new Vector3(1.7f,1.8f,3.4f),canJumpOver=false});
                }
            }
            boundaries.Finish();
            Renderer(sync.Plain("RoadAndSidewalk"),"CHN_"+id+"_Road",road,false);
            Renderer(sync.Plain("Frontage"),"CHN_"+id+"_Frontage",buildings);
            var layouts=new Mesh[recipe.roadsideLayoutVariants];
            var collisionLayouts=new DressingCollisionLayout[layouts.Length];
            GameObject furniture=sync.Plain("StreetFurniture");
            for(int layout=0;layout<layouts.Length;layout++)
            {
                var dressing=new ComicMesh();dressing.Add(props,Vector3.zero,Quaternion.identity,Vector3.one);
                var collisions=new List<DressingObstacle>(fixedCollisions);
                ScatterRoadside(dressing,variant,layout,collisions);
                collisionLayouts[layout]=new DressingCollisionLayout{obstacles=collisions.ToArray()};
                if(layout==0)
                {
                    Renderer(furniture,"CHN_"+id+"_Props",dressing);
                    layouts[layout]=furniture.GetComponent<MeshFilter>().sharedMesh;
                }
                else layouts[layout]=SaveMesh("CHN_"+id+"_Props_"+layout.ToString("D2"),dressing);
            }
            var selection=PrefabBuildUtility.GetOrAdd<StreetDressing>(root);
            selection.surface=furniture.GetComponent<MeshFilter>();selection.outline=furniture.transform.Find("InkOutline").GetComponent<MeshFilter>();selection.layouts=layouts;
            selection.collisionLayouts=collisionLayouts;
            Renderer(sync.Plain("DeepCity"),"CHN_"+id+"_Background",background,false);
            var slotsGo=sync.Plain("SpawnSlots");var slots=new ChildSync(slotsGo.transform);
            foreach(float z in new[]{10f,25f})foreach(float x in new[]{-2.6f,0f,2.6f})
            {
                GameObject slot=slots.Plain("Row"+z+"_Lane"+x);slot.transform.localPosition=new Vector3(x,-grade*z,z);
                PrefabBuildUtility.GetOrAdd<SpawnSlot>(slot).kind=SpawnKind.Any;
            }
            slots.Finish();sync.Finish();
            int tris=PrefabBuildUtility.CountTriangles(root),renderers=root.GetComponentsInChildren<MeshRenderer>(true).Length;
            tris+=2*(layouts.Max(mesh=>(int)mesh.GetIndexCount(0)/3)-(int)layouts[0].GetIndexCount(0)/3);
            PrefabBuildUtility.SaveAndClose(root,path);
            report.Add(id+",街区模块,chunk,8,"+(length*grade).ToString(System.Globalization.CultureInfo.InvariantCulture)+",40,"+tris+","+renderers+",10");
            if(tris>25000)throw new InvalidOperationException(id+" exceeds 25k triangle chunk budget: "+tris);
        }

        static void BuildTheme()
        {
            TrackTheme theme=AssetDatabase.LoadAssetAtPath<TrackTheme>(ThemePath);
            if(theme==null){theme=ScriptableObject.CreateInstance<TrackTheme>();AssetDatabase.CreateAsset(theme,ThemePath);}
            theme.orderedChunks=true;theme.chunks=ChunkIds.Select(id=>AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath(id)).GetComponent<RoadChunk>()).ToArray();
            var obstacles=new List<Obstacle>();
            foreach(string id in new[]{"Crates","Cones","WorkBarrier","Bin"})
            {
                string path=Prefabs+"/Gameplay/OBS_"+id+".prefab";GameObject root=PrefabBuildUtility.OpenPrefab(path);var sync=new ChildSync(root.transform);
                GameObject instance=sync.Instance("Art",AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath(id,false)));
                var obstacle=PrefabBuildUtility.GetOrAdd<Obstacle>(root);obstacle.canJumpOver=true;obstacle.spansRoad=false;
                sync.Finish();PrefabBuildUtility.SaveAndClose(root,path);obstacles.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Obstacle>());
            }
            theme.obstacles=obstacles.ToArray();theme.jumpObstacles=Array.Empty<Obstacle>();
            var previous=AssetDatabase.LoadAssetAtPath<TrackTheme>("Assets/_Project/Data/Theme_PerthStreet.asset");
            theme.gifts=previous!=null?previous.gifts:Array.Empty<Gift>();
            HazardKitBuilder.ApplyTo(theme); // v0.2: comic gifts and hazard patterns (Scripts/Editor/Gameplay)
            BoulevardBuilder.ApplyTo(theme); // v0.2: 4-lane boulevard sections (Scripts/Editor/Gameplay)
            EditorUtility.SetDirty(theme);
        }

        [MenuItem("CityScooter/Comic City/2 - Apply to Main and Save Preview")]
        public static void ApplyToMain()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(AssetDatabase.LoadAssetAtPath<TrackTheme>(ThemePath)==null)Build();
            recipe=AssetDatabase.LoadAssetAtPath<ComicCityRecipe>(RecipePath);
            string backup="Assets/_Project/Scenes/Main_PreComicPrototype.unity";
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(backup)==null)AssetDatabase.CopyAsset(MainPath,backup);
            var scene=EditorSceneManager.OpenScene(MainPath,OpenSceneMode.Single);
            var game=Object.FindAnyObjectByType<GameController>();
            if(game==null)throw new InvalidOperationException("Main has no GameController");
            game.track.theme=AssetDatabase.LoadAssetAtPath<TrackTheme>(ThemePath);game.track.seed=recipe.seed;game.track.buildAhead=320;game.track.keepBehind=40;game.track.buildLeadIn=true;
            var rs=new ChildSync(game.rider.transform);
            GameObject model=rs.Instance("ComicRider",AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/Rider/Rider_Comic.prefab"));
            game.rider.model=model.transform; // keep unrelated rider children, including existing blob shadow
            GameTuning tuning=AssetDatabase.LoadAssetAtPath<GameTuning>("Assets/_Project/Data/Tuning_ComicDownhill.asset");
            if(tuning==null)
            {
                tuning=Object.Instantiate(game.tuning!=null?game.tuning:GameTuning.Default);tuning.name="Tuning_ComicDownhill";
                tuning.safeStartDistance=50;tuning.jumpObstacleChance=0; // single-lane obstacles keep an escape route
                tuning.maxSpeed=44;tuning.maxSpeedMultiplier=5;tuning.speedRampSeconds=600;
                tuning.acceleration=9;
                tuning.postRampMultiplierPerMinute=.1f;
                AssetDatabase.CreateAsset(tuning,"Assets/_Project/Data/Tuning_ComicDownhill.asset");
            }
            game.tuning=tuning;game.track.tuning=tuning;game.rider.tuning=tuning;
            ComicCityAudioBuilder.Apply(game);
            foreach(var component in new Component[]{game,game.track,game.rider}){EditorUtility.SetDirty(component);PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
            var follow=PrefabBuildUtility.GetOrAdd<FollowCamera>(Camera.main.gameObject);follow.target=game.rider.transform;
            follow.offset=recipe.cameraOffset;follow.lookAhead=recipe.cameraLookAhead;follow.lookHeight=recipe.cameraLookHeight;
            follow.baseFov=recipe.cameraFov;follow.maxFov=recipe.cameraMaxFov;follow.fovPerMetrePerSecond=recipe.cameraFovPerSpeed;
            follow.directionalFollow=recipe.cameraDirectionalFollow;follow.maxYaw=recipe.cameraYawLimit;follow.rotationResponse=recipe.cameraRotationResponse;
            EditorUtility.SetDirty(follow);PrefabUtility.RecordPrefabInstancePropertyModifications(follow);
            Camera.main.fieldOfView=recipe.cameraFov;Camera.main.farClipPlane=700;
            foreach(var skyline in Object.FindObjectsByType<SkylineParallax>(FindObjectsSortMode.None))skyline.gameObject.SetActive(false);
            ApplyComicLighting();
            var backdrop=Object.FindAnyObjectByType<ComicCityBackdrop>();
            if(backdrop==null)backdrop=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/Environment/CityBackdrop.prefab"))).GetComponent<ComicCityBackdrop>();
            backdrop.rider=game.rider;backdrop.recipe=recipe;EditorUtility.SetDirty(backdrop);PrefabUtility.RecordPrefabInstancePropertyModifications(backdrop);
            Transform old=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="ComicStreet_EditPreview")?.transform;
            if(old!=null)Object.DestroyImmediate(old.gameObject);
            var preview=new GameObject("ComicStreet_EditPreview");preview.AddComponent<ComicScenePreview>();
            float y=recipe.downhillGrade*40;
            for(int i=-1;i<8;i++)
            {
                var chunk=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath(ChunkIds[(i+4)%4])),preview.transform);
                chunk.transform.localPosition=new Vector3(0,y,i*40);y+=chunk.GetComponent<RoadChunk>().endHeight;
                chunk.GetComponent<StreetDressing>()?.ApplyLayout(recipe.seed,i);
            }
            // Preview has no live generator in edit mode. Position the actual rider/camera analytically.
            game.rider.transform.position=new Vector3(0,-recipe.downhillGrade*2,2);
            model.transform.localRotation=Quaternion.Euler(Mathf.Atan(recipe.downhillGrade)*Mathf.Rad2Deg,0,0);
            Camera.main.transform.position=game.rider.transform.position+recipe.cameraOffset;
            Camera.main.transform.LookAt(new Vector3(0,recipe.cameraLookHeight-recipe.downhillGrade*(2+recipe.cameraLookAhead),2+recipe.cameraLookAhead));
            backdrop.Refresh();PrefabUtility.RecordPrefabInstancePropertyModifications(backdrop.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(game.rider.transform);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            EditorSceneManager.SaveScene(scene,"Assets/_Project/Scenes/ComicCityPreview.unity",true);
            AssetDatabase.SaveAssets();Debug.Log("[ComicCity] Main updated; original scene saved to "+backup);
        }

        static void ApplyComicLighting()
        {
            var apply=Object.FindAnyObjectByType<ApplySceneLook>();
            if(apply!=null&&apply.look!=null)
            {
                string path=Root+"/Look_ComicCity.asset";
                var look=AssetDatabase.LoadAssetAtPath<SceneLook>(path);
                if(look==null)
                {
                    look=Object.Instantiate(apply.look);look.name="Look_ComicCity";look.fogStart=120;look.fogEnd=310;look.fogColor=new Color(.60f,.74f,.83f);
                    look.sunIntensity=1;look.shadowStrength=.7f;
                    AssetDatabase.CreateAsset(look,path);
                }
                Material sky=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Sky_ComicCity.mat");
                Shader skyShader=Shader.Find("CityScooter/Comic Panorama Sky");
                if(skyShader==null)throw new InvalidOperationException("Panorama sky shader has not imported.");
                if(sky==null)
                {
                    sky=new Material(skyShader){name="Sky_ComicCity"};AssetDatabase.CreateAsset(sky,Root+"/Sky_ComicCity.mat");
                }
                else sky.shader=skyShader;
                sky.SetTexture("_ComicSkyTexture",recipe.skyPanorama);
                sky.SetVector("_ComicSkyMapping",ComicCityProjection.SkyMapping(recipe));
                sky.SetFloat("_ComicSkyScale",recipe.skyApparentScale);
                sky.SetColor("_ComicSkyHaze",ComicCityProjection.SkyHaze(recipe));EditorUtility.SetDirty(sky);
                look.skyboxMaterial=sky;EditorUtility.SetDirty(look);
                apply.look=look;apply.Apply();EditorUtility.SetDirty(apply);PrefabUtility.RecordPrefabInstancePropertyModifications(apply);
            }
        }

        public static void BuildBatch()
        {
            try {Build();ApplyToMain();ComicCityPreview.Render();AssetDatabase.SaveAssets();EditorApplication.Exit(0);}
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
