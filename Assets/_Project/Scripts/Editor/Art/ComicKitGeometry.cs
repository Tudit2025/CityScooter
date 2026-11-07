using CityScooter.Rendering;
using UnityEngine;

namespace CityScooter.EditorTools.Art
{
    internal static class ComicKitGeometry
    {
        public static ComicMesh Prop(ComicAssetSpec spec, ComicCityRecipe r)
        {
            ComicSurface primary=ComicSurfaceAssets.ForAsset(spec,false);
            var m = new ComicMesh{Surface=primary};
            Color c=spec.color, ink=r.ink, cream=r.cream;
            ComicSurface Part(Color col)=>col==ink&&primary!=ComicSurface.Concrete?ComicSurface.PaintedMetal:primary;
            void B(float x,float y,float z,float w,float h,float d,Color col,ComicSurface? surface=null)
            {m.Surface=surface??Part(col);m.Box(new Vector3(x,y,z),new Vector3(w,h,d),col);}
            void C(float x,float y,float z,float radius,float height,Color col,float top=1,ComicSurface? surface=null)
            {m.Surface=surface??Part(col);m.Cylinder(new Vector3(x,y,z),radius,height,col,8,top);}
            void Plant(float x,float y,float z,float size)
            {
                m.Surface=ComicSurface.Foliage;
                m.Ellipsoid(new Vector3(x,y,z),new Vector3(size,size*.7f,size),r.green);
                m.Ellipsoid(new Vector3(x+size*.23f,y+size*.15f,z),new Vector3(size*.65f,size*.6f,size*.65f),Color.Lerp(r.green,r.yellow,.25f));
                m.Surface=primary;
            }
            switch(spec.style)
            {
                case "hydrant":
                    C(0,.12f,0,.36f,.24f,ink); C(0,.51f,0,.25f,.7f,c); C(0,.9f,0,.33f,.12f,c,.6f);
                    B(0,.65f,0,1,.17f,.2f,c); B(0,.66f,.26f,.2f,.2f,.16f,cream); break;
                case "bin":
                    C(0,.46f,0,.44f,.88f,c); C(0,.93f,0,.5f,.14f,ink);
                    for(int i=0;i<8;i++) {float a=i*Mathf.PI/4;B(Mathf.Cos(a)*.43f,.45f,Mathf.Sin(a)*.43f,.035f,.65f,.035f,ink);} break;
                case "recycle":
                    for(int i=-1;i<=1;i+=2) { B(i*.25f,.43f,0,.46f,.85f,.9f,i<0?c:r.green); B(i*.25f,.91f,0,.48f,.14f,1,ink);B(i*.25f,.6f,.46f,.18f,.18f,.02f,cream); } break;
                case "newspaper": case "mail":
                    B(0,.12f,0,.8f,.24f,.8f,ink); B(0,.53f,0,.9f,.82f,.8f,c); B(0,.72f,.41f,.65f,.35f,.03f,spec.style=="mail"?ink:cream);
                    B(0,.98f,0,1,.08f,1,ink);B(0,.72f,.44f,.42f,.06f,.02f,ink); break;
                case "locker": case "utility":
                    B(0,.5f,0,1,1,1,c);
                    int cols=spec.style=="locker"?4:2, rows=spec.style=="locker"?4:1;
                    for(int x=0;x<cols;x++) for(int y=0;y<rows;y++) {B(-.5f+(x+.5f)/cols,(y+.5f)/rows,.51f,.9f/cols,.9f/rows,.025f,ink); B(-.5f+(x+.5f)/cols,(y+.5f)/rows,.53f,.84f/cols,.84f/rows,.02f,c);}
                    break;
                case "ac":
                    B(0,.5f,0,1,.85f,.85f,cream);m.Cylinder(new Vector3(-.12f,.5f,.46f),.28f,.05f,ink,12,1,Quaternion.Euler(90,0,0));
                    for(int i=0;i<4;i++) B(.3f,.3f+i*.13f,.44f,.2f,.04f,.03f,ink);break;
                case "tree":
                    B(0,.04f,0,1,.08f,1,ink);B(0,.08f,0,.86f,.05f,.86f,r.pavement);
                    C(0,.34f,0,.075f,.55f,new Color(.35f,.23f,.16f),surface:ComicSurface.Wood);Plant(0,.72f,0,.85f);break;
                case "planter":
                    B(0,.2f,0,1,.4f,1,c);B(0,.42f,0,1.03f,.08f,1.03f,cream);Plant(-.22f,.7f,0,.6f);Plant(.22f,.7f,0,.6f);break;
                case "pot":
                    C(0,.25f,0,.32f,.5f,c,1.4f);C(0,.48f,0,.46f,.08f,cream);Plant(0,.75f,0,.8f);break;
                case "stone": B(0,.48f,0,.85f,.96f,.85f,c);B(0,.96f,0,1,.08f,1,cream);break;
                case "bollard":
                    C(0,.08f,0,.5f,.16f,ink);C(0,.54f,0,.26f,.9f,c);C(0,.83f,0,.28f,.12f,cream);break;
                case "rail":
                    B(-.45f,.5f,0,.08f,1,.2f,ink);B(.45f,.5f,0,.08f,1,.2f,ink);
                    B(0,.85f,0,1,.1f,.13f,c);B(0,.35f,0,1,.08f,.13f,c);break;
                case "lamp":
                    C(0,.06f,0,.22f,.12f,ink);B(0,.45f,0,.07f,.9f,.14f,ink);
                    B(0,.93f,.2f,.08f,.08f,.7f,ink);B(0,.91f,.47f,.42f,.05f,.5f,ink);B(0,.88f,.47f,.34f,.025f,.4f,cream);break;
                case "sign":
                    B(0,.46f,0,.06f,.92f,.2f,ink);B(0,.83f,0,1,.29f,.16f,ink);B(0,.83f,.1f,.92f,.23f,.05f,c);B(0,.83f,.14f,.65f,.035f,.02f,cream);break;
                case "meter":
                    C(0,.32f,0,.1f,.64f,ink);B(0,.79f,0,.9f,.42f,.8f,c);B(0,.85f,.41f,.65f,.16f,.02f,ink);break;
                case "bench":
                    B(-.35f,.23f,0,.1f,.46f,.65f,ink);B(.35f,.23f,0,.1f,.46f,.65f,ink);B(0,.49f,0,1,.12f,.85f,c);
                    B(0,.79f,-.36f,1,.4f,.12f,c);B(0,.77f,-.28f,1,.035f,.025f,ink);break;
                case "bike":
                    for(int i=-1;i<=1;i++) {B(i*.33f,.5f,-.3f,.045f,1,.08f,ink);B(i*.33f,.5f,.3f,.045f,1,.08f,ink);B(i*.33f,.98f,0,.045f,.04f,.67f,c);}break;
                case "board":
                    m.Surface=ComicSurface.Wood;
                    m.Box(new Vector3(0,.5f,.08f),new Vector3(.9f,.96f,.09f),c,Quaternion.Euler(-15,0,0));
                    m.Box(new Vector3(0,.5f,-.08f),new Vector3(.9f,.96f,.09f),c,Quaternion.Euler(15,0,0));
                    B(0,.62f,.18f,.65f,.4f,.02f,ink);B(0,.65f,.2f,.42f,.06f,.02f,cream);break;
                case "vending":
                    B(0,.5f,0,1,1,1,c);B(-.12f,.59f,.51f,.63f,.63f,.025f,ink);B(.34f,.6f,.52f,.16f,.17f,.03f,cream);
                    for(int x=0;x<3;x++)for(int y=0;y<3;y++)B(-.32f+x*.2f,.36f+y*.21f,.54f,.12f,.13f,.02f,y%2==0?r.yellow:cream);
                    B(0,.13f,.51f,.63f,.12f,.03f,ink);break;
                case "crates":
                    for(int i=0;i<3;i++){float x=i==0?-.25f:.22f,y=i==2?.75f:.25f;B(x,y,0,.46f,.48f,.85f,c);B(x,y,.44f,.05f,.47f,.025f,cream);B(x,y,.45f,.44f,.035f,.02f,ink);}break;
                case "cones":
                    for(int i=-1;i<=1;i+=2){B(i*.25f,.04f,0,.46f,.08f,.9f,ink,ComicSurface.Rubber);m.Surface=ComicSurface.Plastic;m.Cylinder(new Vector3(i*.25f,.51f,0),.2f,.94f,c,8,.08f);C(i*.25f,.5f,0,.115f,.16f,cream,.8f);}break;
                case "barrier":
                    B(-.37f,.14f,0,.12f,.28f,1,ink);B(.37f,.14f,0,.12f,.28f,1,ink);B(0,.63f,0,1,.65f,.2f,c);
                    for(int i=-2;i<=2;i++)m.Box(new Vector3(i*.18f,.63f,.11f),new Vector3(.09f,.58f,.015f),ink,Quaternion.Euler(0,0,-18));break;
                case "concrete":
                    B(0,.16f,0,1,.32f,1,c);B(0,.63f,0,1,.74f,.5f,c);B(0,.8f,.26f,.8f,.15f,.02f,r.yellow);break;
                case "shelter":
                    B(-.45f,.48f,-.3f,.06f,.96f,.12f,ink);B(.45f,.48f,-.3f,.06f,.96f,.12f,ink);
                    B(0,.98f,0,1,.08f,1,c);B(0,.54f,-.36f,.9f,.7f,.07f,r.glass,ComicSurface.Glass);B(0,.28f,-.15f,.7f,.06f,.3f,cream,ComicSurface.Wood);break;
                default: B(0,.5f,0,1,1,1,c);break;
            }
            var scaled=new ComicMesh();scaled.Add(m,Vector3.zero,Quaternion.identity,new Vector3(spec.width,spec.height,spec.depth));return scaled;
        }

        public static ComicMesh Building(ComicAssetSpec s,ComicCityRecipe r)
        {
            ComicSurface primary=ComicSurfaceAssets.ForAsset(s,true);
            var m=new ComicMesh{Surface=primary};float w=s.width,d=s.depth,h=s.height,front=d/2;Color c=s.color;
            if(s.style=="background")
            {
                for(int i=0;i<3;i++)
                {
                    float bh=h*(i==1?1f:i==0?.65f:.8f);
                    var block=new ComicAssetSpec{id=s.id+i,style="office",width=w/3,height=bh,depth=d*(i==1?1:.8f),floors=Mathf.Max(1,Mathf.RoundToInt(bh/3)),color=Color.Lerp(c,r.glass,i*.12f)};
                    m.Add(Building(block,r),new Vector3((i-1)*w/3,0,0),Quaternion.identity,Vector3.one);
                }
                return m;
            }
            void B(float x,float y,float z,float sx,float sy,float sz,Color col,ComicSurface? surface=null)
            {
                m.Surface=surface??(col==c?primary:col==r.glass?ComicSurface.Glass:col==r.cream?ComicSurface.Concrete:ComicSurface.PaintedMetal);
                m.Box(new Vector3(x,y,z),new Vector3(sx,sy,sz),col);
            }
            B(0,h/2,0,w,h,d,c); // fully closed volume; never a facade card
            B(0,.35f,0,w+.08f,.7f,d+.08f,r.ink,ComicSurface.Concrete);
            B(0,h-.12f,0,w+.28f,.24f,d+.28f,r.ink);
            B(0,h+.05f,0,w+.12f,.12f,d+.12f,r.cream);
            int floors=Mathf.Max(1,s.floors),cols=Mathf.Max(2,Mathf.FloorToInt(w/2));
            float floor=h/floors;
            bool warehouse=s.style=="warehouse",parking=s.style=="parking";
            for(int level=1;level<floors;level++)
            {
                float y=level*floor+floor*.5f;
                if(parking){B(0,y,front+.04f,w-.7f,floor*.48f,.08f,r.ink);B(0,y-.3f,front+.1f,w-.7f,.1f,.08f,r.cream);continue;}
                for(int j=0;j<cols;j++)
                {
                    float x=-w/2+(j+.5f)*w/cols;
                    Window(m,r,new Vector3(x,y,front+.03f),w/cols*.56f,floor*.52f,Quaternion.identity);
                    if(j%2==0)B(x,y-floor*.28f,front+.17f,w/cols*.68f,.13f,.34f,r.cream);
                }
                B(0,level*floor,front+.05f,w,.12f,.1f,r.cream);
            }
            // Side windows remain visible at setbacks and corners; no detailed hidden interiors.
            for(int side=-1;side<=1;side+=2)
                for(int level=1;level<floors;level++)
                    for(int j=0;j<Mathf.Min(4,Mathf.FloorToInt(d/3));j++)
                        Window(m,r,new Vector3(side*(w/2+.03f),level*floor+floor*.5f,-d/2+1.6f+j*3),1.2f,floor*.5f,Quaternion.Euler(0,side*90,0));
            bool garage=s.style=="garage"||warehouse||parking;
            if(garage)
            {
                for(int j=0;j<2;j++)
                {
                    float x=(j==0?-1:1)*w*.25f;
                    B(x,1.4f,front+.03f,w*.42f,2.5f,.08f,r.ink);B(x,1.4f,front+.09f,w*.39f,2.3f,.04f,Color.Lerp(c,r.cream,.4f),ComicSurface.Metal);
                    for(int k=0;k<6;k++)B(x,.4f+k*.35f,front+.12f,w*.39f,.035f,.02f,r.ink);
                }
            }
            else
            {
                for(int j=0;j<cols;j++) Window(m,r,new Vector3(-w/2+(j+.5f)*w/cols,1.45f,front+.04f),w/cols*.82f,2.15f,Quaternion.identity);
                B(0,1.25f,front+.17f,1.05f,2.5f,.08f,r.ink);B(0,1.38f,front+.22f,.85f,2.12f,.04f,r.glass);B(.3f,1.1f,front+.27f,.06f,.32f,.05f,r.cream);
            }
            if(s.style!="tower"&&s.style!="background")
            {
                B(0,2.95f,front+.18f,w-.35f,.58f,.2f,r.ink);
                B(0,2.95f,front+.3f,w-.6f,.4f,.045f,Color.Lerp(c,r.yellow,.45f));
                // Geometric brand-free symbols; no generated text texture.
                int symbol=Mathf.Abs(StableHash(s.id))%4;
                for(int j=-1;j<=1;j++) B(j*.6f,2.95f,front+.34f,.3f,.15f+(j==0?symbol*.025f:0),.03f,r.cream);
            }
            if(s.style=="shop"||s.style=="cafe"||s.style=="corner")
            {
                int stripes=Mathf.RoundToInt(w/.5f);
                m.Surface=ComicSurface.Fabric;
                for(int j=0;j<stripes;j++)m.Box(new Vector3(-w/2+(j+.5f)*w/stripes,2.55f,front+.62f),new Vector3(w/stripes,.12f,1.2f),j%2==0?c:r.cream,Quaternion.Euler(12,0,0));
                B(0,2.35f,front+1.22f,w,.18f,.08f,r.ink);
            }
            if(s.style=="cinema"||s.style=="hotel")
            {
                B(w*.35f,h*.65f,front+.65f,.8f,Mathf.Min(5,h*.5f),1.1f,r.ink);
                B(w*.35f,h*.65f,front+1.22f,.6f,Mathf.Min(4.7f,h*.47f),.06f,r.red);
                for(int i=0;i<4;i++) B(w*.35f,h*.65f-1.3f+i*.8f,front+1.26f,.35f,.22f,.03f,r.cream);
            }
            if(s.style=="bank"||s.style=="civic")
                for(int side=-1;side<=1;side+=2) B(side*(w/2-.5f),1.6f,front+.3f,.45f,3.2f,.5f,r.cream);
            if(s.style=="tower")
            {
                B(0,h+1,0,w*.7f,2,d*.7f,Color.Lerp(c,r.cream,.25f));B(0,h+2.6f,0,.18f,1.4f,.18f,r.ink);
            }
            else
            {
                B(-w*.23f,h+.45f,-d*.16f,1.5f,.8f,1.3f,r.ink,ComicSurface.PaintedMetal);
                B(w*.25f,h+.3f,-d*.2f,.6f,.6f,.6f,r.cream,ComicSurface.Metal);
            }
            return m;
        }

        static void Window(ComicMesh m,ComicCityRecipe r,Vector3 p,float w,float h,Quaternion q)
        {
            ComicSurface previous=m.Surface;
            void B(Vector3 offset,Vector3 size,Color color,ComicSurface surface=ComicSurface.PaintedMetal)
            {
                m.Surface=surface;
                Vector3 P(float x,float y)=>p+q*(offset+new Vector3(x*size.x/2,y*size.y/2,size.z/2));
                m.Quad(P(-1,-1),P(1,-1),P(1,1),P(-1,1),color);
            }
            B(Vector3.zero,new Vector3(w+.14f,h+.14f,.08f),r.ink);
            B(new Vector3(0,0,.055f),new Vector3(w,h,.04f),r.glass,ComicSurface.Glass);
            B(new Vector3(0,0,.085f),new Vector3(.06f,h,.025f),r.cream);
            B(new Vector3(-w*.18f,h*.3f,.08f),new Vector3(w*.25f,.08f,.02f),Color.Lerp(r.glass,r.cream,.6f),ComicSurface.Plain);
            m.Surface=previous;
        }

        public static int StableHash(string s) {int h=17;unchecked {foreach(char c in s)h=h*31+c;}return h==int.MinValue?0:h;}

        public static ComicMesh Rider(ComicCityRecipe r)
        {
            var m=new ComicMesh{Surface=ComicSurface.PaintedMetal};Color pants=new Color(.38f,.42f,.27f),shoe=r.cream;
            m.Box(new Vector3(0,.22f,0),new Vector3(.34f,.12f,1.2f),r.ink);
            m.Box(new Vector3(0,.29f,0),new Vector3(.28f,.04f,1),r.red);
            m.Surface=ComicSurface.Rubber;
            for(int i=-1;i<=1;i+=2)m.Cylinder(new Vector3(0,.18f,i*.52f),.18f,.12f,r.ink,12,1,Quaternion.Euler(0,0,90));
            m.Surface=ComicSurface.Metal;m.Beam(new Vector3(0,.3f,.48f),new Vector3(0,1.22f,.65f),.055f,r.cream);
            m.Surface=ComicSurface.Rubber;
            m.Box(new Vector3(0,1.23f,.65f),new Vector3(.85f,.065f,.065f),r.ink);
            m.Box(new Vector3(-.11f,.37f,.1f),new Vector3(.18f,.14f,.36f),shoe);
            m.Box(new Vector3(.12f,.37f,-.27f),new Vector3(.18f,.14f,.36f),shoe);
            m.Surface=ComicSurface.Fabric;
            m.Beam(new Vector3(-.11f,.45f,.1f),new Vector3(-.14f,.77f,.25f),.23f,pants);
            m.Beam(new Vector3(-.14f,.77f,.25f),new Vector3(-.13f,1.03f,-.12f),.26f,pants);
            m.Beam(new Vector3(.12f,.45f,-.27f),new Vector3(.17f,.8f,-.1f),.23f,pants);
            m.Beam(new Vector3(.17f,.8f,-.1f),new Vector3(.13f,1.03f,-.12f),.26f,pants);
            m.Box(new Vector3(0,1.02f,-.08f),new Vector3(.5f,.16f,.3f),r.cream);
            m.Box(new Vector3(0,1.32f,.05f),new Vector3(.53f,.62f,.32f),r.ink,Quaternion.Euler(20,0,0));
            for(int side=-1;side<=1;side+=2)
            {
                m.Beam(new Vector3(side*.28f,1.52f,.13f),new Vector3(side*.38f,1.2f,.33f),.17f,r.ink);
                m.Beam(new Vector3(side*.38f,1.2f,.33f),new Vector3(side*.35f,1.23f,.65f),.15f,r.ink);
                m.Ellipsoid(new Vector3(side*.35f,1.23f,.65f),Vector3.one*.16f,shoe);
            }
            m.Cylinder(new Vector3(0,1.63f,.14f),.2f,.13f,r.red);
            m.Surface=ComicSurface.Plastic;
            m.Ellipsoid(new Vector3(0,1.89f,.19f),new Vector3(.49f,.49f,.52f),r.cream);
            m.Box(new Vector3(0,1.84f,-.064f),new Vector3(.28f,.055f,.03f),r.ink);
            return m;
        }

        public static ComicMesh Car(ComicCityRecipe r,Color color)
        {
            var m=new ComicMesh{Surface=ComicSurface.PaintedMetal};m.Box(new Vector3(0,.65f,0),new Vector3(1.7f,.65f,3.4f),color);
            m.Box(new Vector3(0,1.16f,-.1f),new Vector3(1.45f,.65f,1.7f),r.ink);
            m.Surface=ComicSurface.Glass;
            m.Box(new Vector3(0,1.18f,.77f),new Vector3(1.2f,.42f,.04f),r.glass);
            m.Box(new Vector3(0,1.18f,-.97f),new Vector3(1.2f,.42f,.04f),r.glass);
            m.Surface=ComicSurface.Rubber;
            for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)m.Cylinder(new Vector3(side*.82f,.35f,end*1.05f),.35f,.16f,r.ink,8,1,Quaternion.Euler(0,0,90));
            m.Surface=ComicSurface.Glass;
            for(int side=-1;side<=1;side+=2)m.Box(new Vector3(side*.57f,.7f,1.73f),new Vector3(.35f,.18f,.05f),r.cream);
            return m;
        }
    }
}
