using System;
using System.IO;
using System.Linq;
using CityScooter.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CityScooter.EditorTools.Art
{
    public static class ComicCityPreview
    {
        public const string Output="art-src/comic-city/previews";

        [MenuItem("CityScooter/Comic City/3 - Render Previews and Validate")]
        public static void Render()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            SceneSetup[] setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(ComicCityBuilder.MainPath,OpenSceneMode.Single);
                Save(Camera.main,"01-gameplay.png",1600,900);
                WriteFramingReport(Camera.main);
                WriteConvergenceReport(Camera.main);
                Camera cam=Camera.main;
                cam.transform.position=new Vector3(-5,65,8);cam.transform.LookAt(new Vector3(0,-12,80));cam.fieldOfView=65;
                Save(cam,"02-street-layout.png",1600,1000);
                RenderSky();RenderSheet(false);RenderSheet(true);ComicSurfaceAssets.RenderSheet();
                Validate();
                Debug.Log("[ComicCity] Previews: "+Path.GetFullPath(Output));
            }
            finally
            {
                if(setup.Length>0&&setup.All(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        static void Save(Camera cam,string name,int width,int height)
        {
            Object.DestroyImmediate(ArtPreviewUtility.Render(cam,width,height));
            var texture=ArtPreviewUtility.Render(cam,width,height);
            ArtPreviewUtility.SavePng(texture,Output+"/"+name);Object.DestroyImmediate(texture);
        }

        static void WriteFramingReport(Camera cam)
        {
            var recipe=AssetDatabase.LoadAssetAtPath<ComicCityRecipe>(ComicCityBuilder.RecipePath);
            var rider=Object.FindAnyObjectByType<CityScooter.Gameplay.RiderController>();
            Vector2 Measure(Camera camera,Vector3 root,Quaternion rotation)
            {
                Vector3 feet=camera.WorldToViewportPoint(root+rotation*new Vector3(0,.2f,-.4f));
                Vector3 head=camera.WorldToViewportPoint(root+rotation*new Vector3(0,2.135f,.19f));
                return new Vector2(head.y-feet.y,(head.y+feet.y)*.5f);
            }
            Vector2 current=Measure(cam,rider.transform.position,rider.model.localRotation);
            var original=new GameObject("FramingReference").AddComponent<Camera>();original.enabled=false;
            Vector2 before;
            try
            {
                original.fieldOfView=58;original.aspect=cam.aspect;
                original.transform.position=new Vector3(0,4.2f-.24f*2,-5.5f);
                original.transform.LookAt(new Vector3(0,1-.24f*22,22));
                before=Measure(original,new Vector3(0,-.24f*2,2),Quaternion.Euler(Mathf.Atan(.24f)*Mathf.Rad2Deg,0,0));
            }
            finally{Object.DestroyImmediate(original.gameObject);}
            string F(float n)=>n.ToString("F4",System.Globalization.CultureInfo.InvariantCulture);
            File.WriteAllLines("art-src/comic-city/framing.txt",new[]{
                "Original slope angle: "+F(Mathf.Atan(.24f)*Mathf.Rad2Deg)+" deg",
                "Current slope angle: "+F(Mathf.Atan(recipe.downhillGrade)*Mathf.Rad2Deg)+" deg",
                "Vertical FOV: 58 -> "+F(cam.fieldOfView)+" deg",
                "Camera offset: "+recipe.cameraOffset,
                "Rider viewport height (reference/current): "+F(before.x)+" / "+F(current.x),
                "Rider viewport centre Y from bottom (reference/current): "+F(before.y)+" / "+F(current.y),
                "Height ratio: "+F(current.x/before.x),
                "Centre delta: "+F(current.y-before.y)
            });
        }

        static void RenderSheet(bool buildings)
        {
            ArtPreviewUtility.NewPreviewScene();
            var recipe=AssetDatabase.LoadAssetAtPath<ComicCityRecipe>(ComicCityBuilder.RecipePath);
            var specs=buildings?recipe.buildings:recipe.props;
            const int tile=300,cols=7;int rows=Mathf.CeilToInt(specs.Length/(float)cols);
            var sheet=new Texture2D(cols*tile,rows*tile,TextureFormat.RGB24,false);
            var fill=Enumerable.Repeat(new Color(.82f,.87f,.9f),sheet.width*sheet.height).ToArray();sheet.SetPixels(fill);
            for(int i=0;i<specs.Length;i++)
            {
                string path=ComicCityBuilder.Prefabs+(buildings?"/Buildings/BLD_":"/Props/PRP_")+specs[i].id+".prefab";
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                Bounds b=ArtPreviewUtility.WorldBounds(go);float extent=Mathf.Max(b.size.x,b.size.y,b.size.z);
                Vector3 aim=b.center;var cam=ArtPreviewUtility.CreateCamera(aim+new Vector3(1,.65f,1.4f).normalized*extent*2,aim,32,250);
                cam.orthographic=true;cam.orthographicSize=Mathf.Max(.6f,extent*.7f);
                Object.DestroyImmediate(ArtPreviewUtility.Render(cam,tile,tile));
                Texture2D tex=ArtPreviewUtility.Render(cam,tile,tile);
                sheet.SetPixels((i%cols)*tile,(rows-1-i/cols)*tile,tile,tile,tex.GetPixels());
                Object.DestroyImmediate(tex);Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(go);
            }
            sheet.Apply();ArtPreviewUtility.SavePng(sheet,Output+(buildings?"/04-buildings.png":"/03-props.png"));Object.DestroyImmediate(sheet);
        }

        static void RenderSky()
        {
            ArtPreviewUtility.NewPreviewScene();
            RenderSettings.skybox=AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root+"/Sky_ComicCity.mat");
            var cam=ArtPreviewUtility.CreateCamera(new Vector3(0,4,0),new Vector3(0,100,350),68,700);
            cam.clearFlags=CameraClearFlags.Skybox;
            Save(cam,"05-city-sky.png",1600,900);
            cam.transform.rotation=Quaternion.Euler(0,180,0);Save(cam,"06-sky-rear-seam.png",1600,900);
            Object.DestroyImmediate(cam.gameObject);
        }

        static void WriteConvergenceReport(Camera cam)
        {
            var r=AssetDatabase.LoadAssetAtPath<ComicCityRecipe>(ComicCityBuilder.RecipePath);
            var rider=Object.FindAnyObjectByType<CityScooter.Gameplay.RiderController>();
            var rows=new System.Collections.Generic.List<string>{"distance_m,road_width_px_at_1600x900,viewport_y_from_bottom,visual_cross_section_scale"};
            float aspect=cam.aspect;cam.aspect=1600f/900;
            try
            {
                foreach(float d in new[]{100f,180f,240f,320f,400f,r.horizonTaperEnd,r.horizonTaperEnd+40})
                {
                    Vector3 origin=rider.transform.position;
                    Vector3 left=origin+new Vector3(-r.roadHalfWidth,-d*r.downhillGrade,d);
                    Vector3 right=origin+new Vector3(r.roadHalfWidth,-d*r.downhillGrade,d);
                    Vector3 a=cam.WorldToViewportPoint(ComicCityProjection.Position(r,left,origin));
                    Vector3 b=cam.WorldToViewportPoint(ComicCityProjection.Position(r,right,origin));
                    float pixels=Mathf.Abs(a.x-b.x)*1600;
                    rows.Add(FormattableString.Invariant($"{d:F1},{pixels:F4},{a.y:F4},{ComicCityProjection.Scale(r,d):F5}"));
                    if(d>=r.horizonTaperEnd&&(pixels>1||a.y<.65f||a.y>.85f))
                        throw new InvalidOperationException("Road end does not converge inside the upper-centre view.");
                }
            }
            finally{cam.aspect=aspect;}
            File.WriteAllLines("art-src/comic-city/convergence.csv",rows);
        }

        public static void Validate()
        {
            var lines=new System.Collections.Generic.List<string>();
            Material sky=AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root+"/Sky_ComicCity.mat");
            if(sky==null||sky.shader.name!="CityScooter/Comic Panorama Sky"||ShaderUtil.ShaderHasError(sky.shader)||sky.GetTexture("_ComicSkyTexture")==null)
                throw new InvalidOperationException("Missing/error image skybox.");
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{ComicCityBuilder.Prefabs}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);GameObject go=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach(var mr in go.GetComponentsInChildren<MeshRenderer>(true))
                    if(mr.sharedMaterial==null||mr.sharedMaterial.shader==null||ShaderUtil.ShaderHasError(mr.sharedMaterial.shader))throw new InvalidOperationException("Missing/error material in "+path);
                if(go.transform.localScale!=Vector3.one)throw new InvalidOperationException("Non-unit root scale: "+path);
                int tris=PrefabBuildUtility.CountTriangles(go);int renderers=go.GetComponentsInChildren<MeshRenderer>(true).Length;
                var dressing=go.GetComponent<CityScooter.Track.StreetDressing>();
                if(dressing!=null)
                {
                    if(dressing.surface==null||dressing.outline==null||dressing.layouts==null||dressing.layouts.Length==0||dressing.layouts.Any(m=>m==null))
                        throw new InvalidOperationException("Missing roadside layout reference: "+path);
                    int current=(int)dressing.surface.sharedMesh.GetIndexCount(0)/3;
                    int peak=dressing.layouts.Max(m=>(int)m.GetIndexCount(0)/3);
                    tris+=2*(peak-current);
                }
                bool chunk=go.GetComponent<CityScooter.Track.RoadChunk>()!=null;
                if(go.GetComponent<ComicCityBackdrop>()!=null&&(renderers!=1||go.transform.Find("VisualRoadExtension")==null||go.transform.Find("DistantCity")!=null||go.transform.Find("ComicCloudShell")!=null))
                    throw new InvalidOperationException("Backdrop must only contain a road extension; city sky must use the image skybox.");
                int limit=chunk?25000:path.Contains("/Environment/")?12000:path.Contains("/Buildings/")?8000:path.Contains("/Rider/")?5000:3000;
                if(tris>limit)throw new InvalidOperationException("Triangle budget exceeded: "+path+" ("+tris+")");
                if(chunk&&renderers>15)throw new InvalidOperationException("Chunk renderer budget exceeded: "+path);
                lines.Add(path+": "+tris+" triangles, "+renderers+" renderers");
            }
            lines.Add("Validated: unit roots, material/shader references, triangle and renderer budgets. Web frame time still requires device profiling.");
            Directory.CreateDirectory("art-src/comic-city");File.WriteAllLines("art-src/comic-city/validation.txt",lines);
        }
    }
}
