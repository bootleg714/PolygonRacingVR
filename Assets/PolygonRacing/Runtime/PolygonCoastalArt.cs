using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PolygonRacing
{
    // Editor-generated, static scenery. No update loop, shaders or physics required.
    public static class PolygonCoastalArt
    {
        static Material sand,sea,shallow,foam,grass,coral,ochre,cream,teal,pink,bark,leaf,lime,navy;
        // Scenery pass v01 meshes (PolygonSceneryMeshes).
        static Mesh[] palmTrunks,palmCrownsA,palmCrownsB,palmNuts,boulders,mesas,islands;
        static Mesh boatHull,boatCabin,boatRigging,boatSails;
        static Material nut;
        static readonly List<Vector3> vertices=new List<Vector3>();
        static readonly List<int> indices=new List<int>();
        static readonly string[] replaced={"Coastal land west","Coastal land east","Coastal land north of bridge","Coastal land south of bridge",
            "Open Atlantic","Wide sandy shoreline","Wet sand at waterline","Blue water beneath and beside bridge","Wide flat estuary bridge",
            "Low-poly coastal palm","Low-poly coastal rock","Distant low-poly island","Start finish spectator stand"};

        public static void Build(Transform world)
        {
            vertices.Clear();indices.Clear();
            for(int i=world.childCount-1;i>=0;i--)
            {
                var child=world.GetChild(i);
                foreach(string old in replaced)if(child.name==old){Object.DestroyImmediate(child.gameObject);break;}
            }
            sand=Mat("Sunlit sand",new Color(1,.83f,.54f));
            sea=Mat("Deep turquoise",new Color(.025f,.43f,.63f));
            shallow=Mat("Lagoon turquoise",new Color(.10f,.76f,.76f));
            foam=Mat("Sea foam",new Color(.83f,.98f,.91f));
            grass=Mat("Coastal mint grass",new Color(.39f,.65f,.43f));
            coral=Mat("Terracotta cliffs",new Color(.86f,.43f,.30f));
            ochre=Mat("Golden sandstone",new Color(.98f,.65f,.39f));
            cream=Mat("Warm ivory",new Color(1,.94f,.78f));
            teal=Mat("Festival teal",new Color(.025f,.43f,.48f));
            pink=Mat("Festival coral",new Color(.98f,.32f,.32f));
            bark=Mat("Honey palm bark",new Color(.58f,.35f,.19f));
            leaf=Mat("Emerald palm",new Color(.075f,.48f,.30f));
            lime=Mat("Sunlit palm",new Color(.32f,.68f,.31f));
            navy=Mat("Deep blue trim",new Color(.075f,.18f,.27f));
            nut=Mat("Coconut brown",new Color(.36f,.22f,.12f));
            int palmCount=PolygonSceneryMeshes.PalmVariantCount;
            palmTrunks=new Mesh[palmCount];palmCrownsA=new Mesh[palmCount];palmCrownsB=new Mesh[palmCount];palmNuts=new Mesh[palmCount];
            for(int v=0;v<palmCount;v++)
            {
                palmTrunks[v]=PolygonSceneryMeshes.PalmTrunk(v);palmCrownsA[v]=PolygonSceneryMeshes.PalmCrown(v,0);
                palmCrownsB[v]=PolygonSceneryMeshes.PalmCrown(v,1);palmNuts[v]=PolygonSceneryMeshes.PalmCoconuts(v);
            }
            boulders=new[]{PolygonSceneryMeshes.Boulder(3),PolygonSceneryMeshes.Boulder(7),PolygonSceneryMeshes.Boulder(11)};
            mesas=new[]{PolygonSceneryMeshes.Mesa(5),PolygonSceneryMeshes.Mesa(9),PolygonSceneryMeshes.Mesa(13)};
            islands=new[]{PolygonSceneryMeshes.Boulder(21,.22f,.75f,"Faceted island"),PolygonSceneryMeshes.Boulder(29,.22f,.75f,"Faceted island")};
            boatHull=PolygonSceneryMeshes.BoatHull();boatCabin=PolygonSceneryMeshes.BoatCabin();
            boatRigging=PolygonSceneryMeshes.BoatRigging();boatSails=PolygonSceneryMeshes.BoatSails();
            var root=new GameObject("Coastal art - palms beach cliffs and festival").transform;root.SetParent(world,false);
            BuildLandAndSea(root);BuildPalms(root);BuildCliffs(root);BuildCliffPassage(root);BuildIslands(root);BuildFestival(root);BuildCoastGantry(root);BuildBridge(root);BuildLighthouse(root);BuildBoats(root);BuildBridgeWaterfront(root);BuildCornerGuidance(root);PolygonMarinaPit.BuildScenery(world);SeparateSignText(world);
            foreach(var renderer in world.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                if(renderer.name=="Low roadside barrier")
                {
                    var t=renderer.transform;var scale=t.localScale;scale.y=.5f;t.localScale=scale;
                    var p=t.position;p.y=.25f;t.position=p;renderer.sharedMaterial=cream;
                }
            }
            PolygonBanking.BuildVergeAndAlignScenery(world);
        }
        static void SeparateSignText(Transform world)
        {
            foreach(var text in world.GetComponentsInChildren<TextMesh>())
            {
                if(Quaternion.Angle(text.transform.localRotation,Quaternion.identity)>.1f)continue;
                float front=float.PositiveInfinity;
                foreach(Transform sibling in text.transform.parent)
                {
                    var filter=sibling.GetComponent<MeshFilter>();
                    if(filter&&filter.sharedMesh&&filter.sharedMesh.name=="Cube")
                        front=Mathf.Min(front,sibling.localPosition.z-sibling.localScale.z*.5f);
                }
                if(float.IsPositiveInfinity(front))continue;
                var p=text.transform.localPosition;p.z=Mathf.Min(p.z,front-.12f);text.transform.localPosition=p;
            }
        }
        static Material Mat(string name,Color color)=>PolygonWorld.Paint("CE Art "+name,color);
        static Transform Group(Transform parent,string name,Vector3 p)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
        static GameObject Box(Transform p,string name,Vector3 at,Vector3 size,Material mat)=>PolygonWorld.Box(name,p,at,size,mat);
        static void MeshObject(Transform parent,string name,Mesh mesh,Vector3 p,Vector3 scale,Material material,float yaw=0)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.transform.localPosition=p;go.transform.localScale=scale;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        static void Tri(Vector3 a,Vector3 b,Vector3 c)
        {int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);indices.Add(n);indices.Add(n+1);indices.Add(n+2);}
        static void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){Tri(a,b,c);Tri(a,c,d);}
        static Mesh Finish(string name)
        {
            var mesh=new Mesh{name=name};if(vertices.Count>65000)mesh.indexFormat=IndexFormat.UInt32;
            mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();vertices.Clear();indices.Clear();return mesh;
        }
        static void GroundQuad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {if(Vector3.Cross(b-a,c-a).y<0)Quad(d,c,b,a);else Quad(a,b,c,d);}
        static float Shore(float x)=>708+Mathf.Sin(x*.009f)*7+Mathf.Sin(x*.021f)*3;
        static void BuildLandAndSea(Transform root)
        {
            // Low ocean is continuous underneath land and the bridge; opaque land masks it.
            Box(root,"Endless turquoise sea",new Vector3(300,-1.05f,400),new Vector3(6500,.1f,6500),sea);
            const float minX=-720,maxX=1360,minZ=-530,maxZ=675,bridgeX=336,half=108;
            Box(root,"Western headland",new Vector3((minX+bridgeX-half)/2,-.64f,(minZ+maxZ)/2),new Vector3(bridgeX-half-minX,1.2f,maxZ-minZ),grass);
            Box(root,"Eastern headland",new Vector3((bridgeX+half+maxX)/2,-.64f,(minZ+maxZ)/2),new Vector3(maxX-bridgeX-half,1.2f,maxZ-minZ),grass);
            Box(root,"Inland behind estuary",new Vector3(bridgeX,-.64f,(108+maxZ)/2),new Vector3(half*2,1.2f,maxZ-108),grass);
            BuildEstuaryCove(root);
            // Faceted sloping beach, bright shallows and irregular white shoreline bands.
            for(int i=0;i<72;i++)
            {
                float x=-650+i*28,x2=x+28,z=Shore(x),z2=Shore(x2);
                GroundQuad(new Vector3(x,-.045f,674),new Vector3(x2,-.045f,674),new Vector3(x2,-.95f,z2),new Vector3(x,-.95f,z));
            }
            MeshObject(root,"Golden scalloped beach",Finish("Scalloped beach mesh"),Vector3.zero,Vector3.one,sand);
            for(int i=0;i<72;i++)
            {
                float x=-650+i*28,x2=x+28,z=Shore(x),z2=Shore(x2);
                GroundQuad(new Vector3(x,-.94f,z),new Vector3(x2,-.94f,z2),new Vector3(x2,-.94f,z2+100),new Vector3(x,-.94f,z+100));
            }
            MeshObject(root,"Bright shallow water",Finish("Shallow water mesh"),Vector3.zero,Vector3.one,shallow);
            for(int i=0;i<140;i++)
            {
                float x=-620+i*14,z=Shore(x)+2+Mathf.Sin(i*1.7f)*2;
                GroundQuad(new Vector3(x,-.925f,z),new Vector3(x+11,-.925f,Shore(x+11)+2),new Vector3(x+11,-.925f,Shore(x+11)+3.4f),new Vector3(x,-.925f,z+1.4f));
            }
            MeshObject(root,"Broken white surf",Finish("Surf ribbon mesh"),Vector3.zero,Vector3.one,foam);
            Box(root,"Azure estuary",new Vector3(336,-.92f,26.5f),new Vector3(216,.05f,163),shallow);
            for(int i=0;i<24;i++)
            {
                float x=-420+(i*137)%1630,z=770+(i*73)%380;
                Box(root,"Sunlit water streak",new Vector3(x,-.88f,z),new Vector3(8+i%4*3,.012f,.65f),foam);
            }
        }

        // Carved from the old land footprint, rather than laid on top of grass.
        static float CoveRim(float x)=>-108-45*Mathf.Sin(Mathf.Clamp01((x-228)/216)*Mathf.PI);
        static void BuildEstuaryCove(Transform root)
        {
            for(int i=0;i<24;i++)
            {
                float x=228+i*9,b=x+9;
                GroundQuad(new Vector3(x,-.04f,-530),new Vector3(b,-.04f,-530),
                    new Vector3(b,-.04f,CoveRim(b)),new Vector3(x,-.04f,CoveRim(x)));
            }
            MeshObject(root,"Cove sandy headland",Finish("Carved south bank"),Vector3.zero,Vector3.one,sand);
            for(int i=0;i<24;i++)
            {
                float x=228+i*9,b=x+9;
                GroundQuad(new Vector3(x,-.04f,CoveRim(x)),new Vector3(b,-.04f,CoveRim(b)),
                    new Vector3(b,-.895f,CoveRim(b)+16),new Vector3(x,-.895f,CoveRim(x)+16));
            }
            MeshObject(root,"Crescent cove beach",Finish("Sloping cove sand"),Vector3.zero,Vector3.one,sand);
            // Surf segments replace the water polygons they occupy, with shared edges.
            for(int color=0;color<2;color++)
            {
                for(int i=0;i<24;i++)
                {
                    float x=228+i*9,b=x+9,a=CoveRim(x)+16,c=CoveRim(b)+16;
                    for(int band=0;band<3;band++)
                    {
                        bool white=band==1&&i%4<2;if(white!=(color==1))continue;
                        float za=band==0?a:band==1?a+3:a+4.3f;
                        float zb=band==0?c:band==1?c+3:c+4.3f;
                        float ea=band==0?a+3:band==1?a+4.3f:-55;
                        float eb=band==0?c+3:band==1?c+4.3f:-55;
                        GroundQuad(new Vector3(x,-.895f,za),new Vector3(b,-.895f,zb),new Vector3(b,-.895f,eb),new Vector3(x,-.895f,ea));
                    }
                }
                MeshObject(root,color==0?"Sheltered cove turquoise water":"Cove inlaid surf",
                    Finish("Cove water bands "+color),Vector3.zero,Vector3.one,color==0?shallow:foam);
            }
        }
        static void BuildBridgeWaterfront(Transform root)
        {
            var village=Group(root,"Bridge approach - Sunbay waterfront",Vector3.zero);
            // Houses step back from the bridge; the nearest footprint is 36 m off road.
            for(int i=0;i<6;i++)
            {
                float x=470+(i%2)*24,z=-48-(i/2)*31,h=8+(i%3)*2;
                var house=Group(village,"Sunbay waterfront house "+(i+1),new Vector3(x,0,z));
                Box(house,"Ivory foundation",new Vector3(0,.3f,0),new Vector3(18,.6f,20),cream);
                Box(house,"Color washed walls",new Vector3(0,.6f+h/2,0),new Vector3(16,h,18),i%3==0?pink:i%3==1?ochre:shallow);
                // Solid pitched roof, including gables. No coplanar decorative roof layer.
                var a=new Vector3(-9,h+.6f,-10);var b=new Vector3(9,h+.6f,-10);
                var c=new Vector3(9,h+.6f,10);var d=new Vector3(-9,h+.6f,10);
                var e=new Vector3(0,h+4,-10);var f=new Vector3(0,h+4,10);
                Quad(a,d,f,e);Quad(b,e,f,c);Tri(a,e,b);Tri(d,c,f);
                MeshObject(house,"Broad terracotta pitched roof",Finish("Sunbay roof "+i),Vector3.zero,Vector3.one,i%2==0?coral:teal);
                // West-facing balconies look across the sheltered water.
                Box(house,"Shaded doorway",new Vector3(-8.08f,2.3f,0),new Vector3(.16f,3.4f,2.2f),navy);
                foreach(int end in new[]{-1,1})
                {
                    Box(house,"Recess colored window",new Vector3(-8.08f,h-1,end*5),new Vector3(.16f,2.2f,2.6f),navy);
                    Box(house,"Ivory window sill",new Vector3(-8.25f,h-2.2f,end*5),new Vector3(.5f,.2f,3),cream);
                    Box(house,"Balcony side rail",new Vector3(-10,h-1.5f,end*6.8f),new Vector3(4,.65f,.16f),cream);
                }
                Box(house,"Waterfront balcony",new Vector3(-10,h-2.7f,0),new Vector3(4,.35f,14),cream);
                Box(house,"Teal balcony rail",new Vector3(-11.9f,h-1.5f,0),new Vector3(.2f,.65f,13.6f),teal);
                // Striped awnings face the road on the north side.
                for(int stripe=0;stripe<5;stripe++)
                    Box(house,"Alternating cafe awning",new Vector3(-6.4f+stripe*3.2f,4.4f,10.5f),new Vector3(3.2f,.25f,3),stripe%2==0?cream:teal);
            }
            Box(village,"Waterside promenade",new Vector3(450,.14f,-91),new Vector3(10,.36f,108),cream);
            Box(village,"Timber landing",new Vector3(432,.08f,-82),new Vector3(26,.24f,6),bark);
            foreach(float x in new[]{421f,441f})foreach(float z in new[]{-84f,-80f})
                Box(village,"Landing mooring post",new Vector3(x,.7f,z),new Vector3(.5f,2,.5f),teal);
            foreach(var p in new[]{new Vector3(520,0,-48),new Vector3(523,0,-111),new Vector3(455,0,-158),new Vector3(248,0,-147)})
                Palm(village,p,.83f,p.x,1);
            for(int i=0;i<3;i++)
            {
                float x=280+i*48,z=i==1?-91:-66;
                var boat=Group(village,"Moored Sunbay sailboat "+(i+1),new Vector3(x,-.895f,z));
                boat.localRotation=Quaternion.Euler(0,20+i*39,0);
                // Low tapered hull with a visible flat deck rather than a rock silhouette.
                var p0=new Vector3(-1.8f,.7f,-5);var p1=new Vector3(1.8f,.7f,-5);
                var p2=new Vector3(2,.7f,3);var p3=new Vector3(0,.7f,6);var p4=new Vector3(-2,.7f,3);
                var keel=new Vector3(0,-.3f,0);
                Tri(p0,p1,keel);Tri(p1,p2,keel);Tri(p2,p3,keel);Tri(p3,p4,keel);Tri(p4,p0,keel);
                Tri(p0,p4,p3);Tri(p0,p3,p2);Tri(p0,p2,p1);
                MeshObject(boat,"Faceted ivory sailboat hull",Finish("Sunbay hull "+i),Vector3.zero,Vector3.one,cream);
                Box(boat,"Slim sailboat mast",new Vector3(0,6.8f,0),new Vector3(.13f,12.2f,.13f),navy);
                Box(boat,"Small teal cabin",new Vector3(0,1.1f,-2),new Vector3(2.3f,.8f,2.8f),teal);
                // Two-sided single sail sheet. No layered color panels.
                var a=new Vector3(.09f,2,.2f);var b=new Vector3(.09f,12.7f,.2f);var c=new Vector3(.09f,2,5.1f);
                Tri(a,b,c);Tri(c,b,a);
                MeshObject(boat,"Full triangular sail",Finish("Sunbay sail "+i),Vector3.zero,Vector3.one,i==1?pink:cream);
                // Separate little foresail stays clear of the main sheet.
                a=new Vector3(-.09f,2,-.4f);b=new Vector3(-.09f,10,-.4f);c=new Vector3(-.09f,2,-4.6f);
                Tri(a,b,c);Tri(c,b,a);
                MeshObject(boat,"Teal foresail",Finish("Sunbay foresail "+i),Vector3.zero,Vector3.one,teal);
            }
            MergeLandmarkMeshes(village,"Sunbay");
        }
        static void MergeLandmarkMeshes(Transform root,string label)
        {
            // Bake this small landmark to one mesh per palette material at generation time.
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!filter.sharedMesh)continue;
                var mat=renderer.sharedMaterial;
                if(!groups.TryGetValue(mat,out var list)){list=new List<CombineInstance>();groups.Add(mat,list);}
                list.Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix});
            }
            for(int i=root.childCount-1;i>=0;i--)Object.DestroyImmediate(root.GetChild(i).gameObject);
            foreach(var entry in groups)
            {
                var mesh=new Mesh{name=label+" merged "+entry.Key.name,indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(entry.Value.ToArray(),true,true);mesh.RecalculateBounds();
                MeshObject(root,label+" - "+entry.Key.name,mesh,Vector3.zero,Vector3.one,entry.Key);
            }
        }

        static bool Clear(Vector3 p,float radius)
        {PolygonCircuit.ProjectLap(p,out float offset);return offset>PolygonCircuit.RoadHalfWidth+radius+8;}
        static void Palm(Transform parent,Vector3 p,float scale,float yaw,int tint)
        {
            if(!Clear(p,scale*6))return;
            var root=Group(parent,"Sculpted palm cluster tree",p);root.localRotation=Quaternion.Euler(0,yaw,0);root.localScale=Vector3.one*scale;
            int v=Mathf.Abs(Mathf.RoundToInt(p.x*7+p.z*13))%PolygonSceneryMeshes.PalmVariantCount;
            MeshObject(root,"Curved honey trunk",palmTrunks[v],Vector3.zero,Vector3.one,bark);
            MeshObject(root,"Broad emerald crown",palmCrownsA[v],Vector3.zero,Vector3.one,tint%3==0?lime:leaf);
            MeshObject(root,"Sunlit crown fronds",palmCrownsB[v],Vector3.zero,Vector3.one,tint%3==0?leaf:lime);
            MeshObject(root,"Coconuts",palmNuts[v],Vector3.zero,Vector3.one,nut);
        }
        static void BuildPalms(Transform root)
        {
            var rng=new System.Random(71420);
            // Beach groves sit near the racing line, not hundreds of metres away.
            for(int group=0;group<21;group++)for(int tree=0;tree<4;tree++)
            {
                float x=-245+group*63+tree*5+(float)rng.NextDouble()*9;
                float z=681+tree%2*10+(float)rng.NextDouble()*6;
                Palm(root,new Vector3(x,-.05f,z),.87f+(float)rng.NextDouble()*.45f,(float)rng.NextDouble()*360,group+tree);
            }
            for(int group=0;group<24;group++)
            {
                float d=group*PolygonCircuit.Length/24+38;float angle=PolygonCircuit.AngleAtDistance(d);
                for(int tree=0;tree<3;tree++)
                {
                    float lateral=(group%2==0?1:-1)*(31+tree*8);
                    var p=PolygonWorld.Point(angle)+PolygonWorld.Side(angle)*lateral+PolygonWorld.Tangent(angle)*tree*10;
                    if(p.z>707||p.z<-470||Mathf.Abs(p.x-336)<115&&Mathf.Abs(p.z)<120)continue;
                    Palm(root,p,.85f+(float)rng.NextDouble()*.4f,(float)rng.NextDouble()*360,group+tree);
                }
            }
        }
        static void Rock(Transform root,string name,Vector3 p,Vector3 scale,Material mat,float yaw=0)
        {
            if(!Clear(p,Mathf.Max(scale.x,scale.z)))return;
            // Big formations become stepped sandstone cliffs; small ones become boulders.
            bool small=name.Contains("outcrop")||name.Contains("islet");
            var set=small?boulders:mesas;
            int v=Mathf.Abs(Mathf.RoundToInt(p.x*3+p.z*5+yaw))%set.Length;
            MeshObject(root,name,set[v],p,scale,mat,yaw);
        }
        static void BuildCliffs(Transform root)
        {
            for(int i=0;i<16;i++)
            {
                float d=Mathf.Lerp(PolygonCircuit.DistanceAtKnot(5.5f),PolygonCircuit.DistanceAtKnot(10.2f),i/15f);
                float angle=PolygonCircuit.AngleAtDistance(d);var p=PolygonWorld.Point(angle)-PolygonWorld.Side(angle)*(64+i%3*12);
                Rock(root,"Coral headland cliff",p,new Vector3(22+i%3*6,21+i%4*8,22+i%2*9),i%3==0?ochre:coral,i*29);
            }
            // Low inland bluffs frame the ocean straight, instead of an empty grass horizon.
            for(int i=0;i<9;i++)
            {
                float d=Mathf.Lerp(PolygonCircuit.DistanceAtKnot(11.7f),PolygonCircuit.DistanceAtKnot(15.4f),i/8f);
                float angle=PolygonCircuit.AngleAtDistance(d);
                var p=PolygonWorld.Point(angle)-PolygonWorld.Side(angle)*(70+i%3*8);
                Rock(root,"Warm inland coastal bluff",p,new Vector3(25,18+i%4*7,23+i%3*4),i%3==0?ochre:coral,i*41);
            }
            for(int i=0;i<14;i++)
            {
                float angle=PolygonCircuit.AngleAtDistance(PolygonCircuit.DistanceAtKnot(18)+i*22);
                var p=PolygonWorld.Point(angle)-PolygonWorld.Side(angle)*(52+i%3*9);
                Rock(root,"Golden canyon wall",p,new Vector3(15+i%3*4,14+i%4*7,18),i%3==0?coral:ochre,i*33);
            }
            for(int i=0;i<14;i++)Rock(root,"Beach sandstone outcrop",new Vector3(-270+i*114,-.8f,721+i%3*8),new Vector3(6+i%3*2,4+i%4*2,6),ochre,i*51);
        }

        static Vector3 CliffPoint(float distance,float lateral,float height)
        {
            float a=PolygonCircuit.AngleAtDistance(distance);
            return PolygonWorld.Point(a)+PolygonWorld.Side(a)*lateral+Vector3.up*height;
        }
        static Vector2[] CliffProfile(float t)
        {
            float wave=Mathf.Sin(t*1.73f)*2.1f+Mathf.Sin(t*.61f)*1.3f;
            return new[]{new Vector2(-22,-.2f),new Vector2(-23,11+wave*.4f),
                new Vector2(-28,25+wave),new Vector2(-45,31+wave),new Vector2(-58,-.2f)};
        }
        static void CliffCap(float d,Vector2[] profile,bool end)
        {
            var center=Vector2.zero;foreach(var p in profile)center+=p;center/=profile.Length;
            var c=CliffPoint(d,center.x,center.y);
            for(int i=0;i<profile.Length;i++)
            {
                var a=profile[i];var b=profile[(i+1)%profile.Length];
                if(end)Tri(c,CliffPoint(d,a.x,a.y),CliffPoint(d,b.x,b.y));
                else Tri(c,CliffPoint(d,b.x,b.y),CliffPoint(d,a.x,a.y));
            }
        }
        static void BuildCliffPassage(Transform root)
        {
            // The inland side of the final coastal sweeper; ocean side stays open.
            float center=PolygonCircuit.DistanceAtKnot(10.2f),start=center-80,end=center+80;
            var group=Group(root,"Amber Point - open coastal rock gallery",Vector3.zero);
            // Replace only the old rocks/palms occupying this new rock footprint.
            foreach(var child in root.GetComponentsInChildren<Transform>())
            {
                if(!child)continue;
                bool palm=child.name=="Sculpted palm cluster tree";
                bool rock=child.name=="Coral headland cliff"||child.name=="Warm inland coastal bluff";
                if(!palm&&!rock)continue;
                float lap=PolygonCircuit.ProjectLap(child.position,out _),d=lap*PolygonCircuit.Length;
                if(d<start-35||d>end+35)continue;
                float a=PolygonCircuit.AngleAtDistance(d);
                float side=Vector3.Dot(child.position-PolygonWorld.Point(a),PolygonWorld.Side(a));
                if(side<-18&&side>-110)Object.DestroyImmediate(child.gameObject);
            }
            // Joined faceted wall, no separate decorative faces laid over its surface.
            for(int color=0;color<3;color++)
            {
                for(int step=0;step<16;step++)
                {
                    float d=start+step*10;var a=CliffProfile(step);var b=CliffProfile(step+1);
                    for(int band=0;band<a.Length;band++)
                    {
                        int tint=band==0?(step%4==0?1:0):band==1?1:2;
                        if(tint!=color)continue;
                        int next=(band+1)%a.Length;
                        Quad(CliffPoint(d,a[band].x,a[band].y),CliffPoint(d,a[next].x,a[next].y),
                            CliffPoint(d+10,b[next].x,b[next].y),CliffPoint(d+10,b[band].x,b[band].y));
                    }
                }
                if(color==0){CliffCap(start,CliffProfile(0),false);CliffCap(end,CliffProfile(16),true);}
                MeshObject(group,"Amber Point faceted cliff "+color,Finish("Amber Point cliff band "+color),Vector3.zero,Vector3.one,color==0?coral:color==1?ochre:sand);
            }
            // Three short stone fingers reach overhead from the inland wall.
            // Lowest underside is 14 m above the route datum; no ocean-side pillars.
            var profile=new[]{new Vector2(9,17),new Vector2(0,22),new Vector2(-20,28),
                new Vector2(-34,27),new Vector2(-34,15),new Vector2(6,14)};
            for(int color=0;color<3;color++)
            {
                for(int finger=0;finger<3;finger++)
                {
                    float d=center-42+finger*42,depth=7+finger%2*2;
                    for(int band=0;band<profile.Length;band++)
                    {
                        int tint=band<=2?1:band==3?0:2;if(tint!=color)continue;
                        var a=profile[band];var b=profile[(band+1)%profile.Length];
                        Quad(CliffPoint(d,a.x,a.y),CliffPoint(d,b.x,b.y),
                            CliffPoint(d+depth,b.x,b.y),CliffPoint(d+depth,a.x,a.y));
                    }
                    if(color==0){CliffCap(d,profile,false);CliffCap(d+depth,profile,true);}
                }
                MeshObject(group,"Amber Point overhead stone "+color,Finish("Amber Point stone fingers "+color),Vector3.zero,Vector3.one,color==0?coral:color==1?ochre:sand);
            }
        }

        static void BuildIslands(Transform root)
        {
            // Distances are within the coastal camera range; skyline mass is deliberate.
            MeshObject(root,"Emerald island near bridge",islands[0],new Vector3(140,-1,910),new Vector3(90,48,45),teal,18);
            MeshObject(root,"Emerald island east",islands[1],new Vector3(790,-1,980),new Vector3(135,82,60),teal,-22);
            MeshObject(root,"Warm island ridge",islands[0],new Vector3(1120,-1,890),new Vector3(100,60,46),coral,30);
            MeshObject(root,"Island west peak",islands[1],new Vector3(-360,-1,940),new Vector3(85,68,50),teal,10);
        }
        static Transform Frame(Transform root,string name,float distance,float sideOffset)
        {
            float angle=PolygonCircuit.AngleAtDistance(distance);var t=Group(root,name,PolygonWorld.Point(angle)+PolygonWorld.Side(angle)*sideOffset);
            t.rotation=Quaternion.LookRotation(PolygonWorld.Tangent(angle));return t;
        }

        static void FestivalSail(Transform stand,int sign,float z,int bay)
        {
            // Four joined color panels form a thick asymmetric sail, not stacked paint.
            var corners=new[]{new Vector3(sign*-9,9.4f,z-13),new Vector3(sign*10,12,z-12),
                new Vector3(sign*10,12,z+12),new Vector3(sign*-9,9.4f,z+13)};
            var peak=new Vector3(sign*1,14.2f,z);
            for(int color=0;color<3;color++)
            {
                for(int side=0;side<4;side++)
                {
                    int tint=side==0?(bay%2==0?1:2):side==2?2:0;if(tint!=color)continue;
                    var a=corners[side];var b=corners[(side+1)%4];
                    if(Vector3.Cross(b-a,peak-a).y<0){var t=a;a=b;b=t;}
                    Tri(a,b,peak);
                    var down=Vector3.down*.14f;
                    Tri(peak+down,b+down,a+down);
                    Quad(b,a,a+down,b+down);
                }
                MeshObject(stand,"Sail canopy panel "+bay+" - "+color,Finish("Festival sail "+bay+" - "+color),Vector3.zero,Vector3.one,color==0?cream:color==1?teal:pink);
            }
            foreach(var corner in corners)
            {
                float h=corner.y-.1f;
                Box(stand,"Slim ivory sail mast",new Vector3(corner.x,h/2,corner.z),new Vector3(.19f,h,.19f),cream);
                Box(stand,"Teal mast footing",new Vector3(corner.x,.3f,corner.z),new Vector3(.5f,.6f,.5f),teal);
            }
        }
        static void BuildSeasideGrandstands(Transform root)
        {
            // Clear the new stand footprints before baking: no palm crowns through roofs.
            foreach(var child in root.GetComponentsInChildren<Transform>())
            {
                if(!child||child.name!="Sculpted palm cluster tree")continue;
                float lap=PolygonCircuit.ProjectLap(child.position,out _),d=lap*PolygonCircuit.Length;
                float delta=Mathf.Repeat(d-50+PolygonCircuit.Length*.5f,PolygonCircuit.Length)-PolygonCircuit.Length*.5f;
                float a=PolygonCircuit.AngleAtDistance(d);
                float lateral=Vector3.Dot(child.position-PolygonWorld.Point(a),PolygonWorld.Side(a));
                if(Mathf.Abs(delta)<61&&Mathf.Abs(lateral)>20&&Mathf.Abs(lateral)<54)Object.DestroyImmediate(child.gameObject);
            }
            var shirts=new[]{cream,teal,pink,ochre,navy,shallow};
            var skin=new[]{sand,bark,ochre};
            foreach(int sign in new[]{-1,1})
            {
                var stand=Frame(root,"Coral Coast sail grandstand",50,sign*36);
                for(int bay=0;bay<3;bay++)
                {
                    float z=(bay-1)*30;
                    for(int row=0;row<6;row++)
                    {
                        float x=sign*(-5.5f+row*1.9f),top=.65f+row*.78f;
                        Box(stand,"Solid stepped seating",new Vector3(x,top/2,z),new Vector3(1.9f,top,26),row%2==0?teal:cream);
                        // Two narrow aisle gaps between bays keep the mass readable.
                        for(int seat=0;seat<18;seat++)
                        {
                            int id=bay*108+row*18+seat+(sign>0?5:0);if(id%11==0)continue;
                            float at=z-11.9f+seat*1.4f,jitter=(id%5-2)*.045f;
                            float px=x+jitter;var shirt=shirts[(id*7+row)%shirts.Length];
                            Box(stand,"Spectator shirt",new Vector3(px,top+.47f,at),new Vector3(.53f,.68f,.49f),shirt);
                            Box(stand,"Spectator head",new Vector3(px-sign*.07f,top+.99f,at),new Vector3(.34f,.34f,.34f),skin[id%skin.Length]);
                            if(id%5==0)Box(stand,"Spectator sun cap",new Vector3(px-sign*.1f,top+1.19f,at),new Vector3(.43f,.12f,.4f),id%2==0?cream:teal);
                            if(id%7==0)
                            {
                                var arm=Box(stand,"Raised cheering arm",new Vector3(px,top+.96f,at+.38f),new Vector3(.17f,.8f,.17f),shirt);
                                arm.transform.localRotation=Quaternion.Euler(sign*18,0,0);
                            }
                        }
                    }
                    FestivalSail(stand,sign,z,bay);
                    // A coral fascia faces the road below each bay, clear of seated heads.
                    Box(stand,"Coral front fascia",new Vector3(sign*-6.5f,.55f,z),new Vector3(.16f,1.1f,26),pink);
                }
                MergeLandmarkMeshes(stand,"Seaside grandstand");
            }
        }

        static void BuildFestival(Transform root)
        {
            BuildSeasideGrandstands(root);
            foreach(int sign in new[]{-1,1})for(int i=0;i<4;i++)
            {
                var flag=Frame(root,"Coral race pennant",12+i*24,sign*23);
                Box(flag,"Flagpole",new Vector3(0,4,0),new Vector3(.12f,8,.12f),cream);
                Box(flag,"Vertical festival flag",new Vector3(0,6.4f,1),new Vector3(.06f,2.6f,1.6f),i%2==0?pink:teal);
            }
            BuildCoastalPaddock(root);
        }


        static Mesh PaddockWheelMesh()
        {
            for(int i=0;i<10;i++)
            {
                float a=i*Mathf.PI*2/10,b=(i+1)*Mathf.PI*2/10;
                var p=new Vector3(-.14f,Mathf.Cos(a)*.36f,Mathf.Sin(a)*.36f);
                var q=new Vector3(-.14f,Mathf.Cos(b)*.36f,Mathf.Sin(b)*.36f);
                var width=Vector3.right*.28f;
                Tri(new Vector3(-.14f,0,0),q,p);Tri(new Vector3(.14f,0,0),p+width,q+width);
                Quad(p,q,q+width,p+width);
            }
            return Finish("Paddock ten sided tire");
        }
        static void ParkedTeamCar(Transform parent,Material team,Mesh wheel)
        {
            var car=Group(parent,"Static team coupe",new Vector3(2,.12f,0));car.localRotation=Quaternion.Euler(0,90,0);
            // Low wedge body with a solid dark cabin and four separate tires.
            var zs=new[]{-2.25f,-1.4f,.8f,2.25f};var widths=new[]{.88f,1.03f,1.03f,.82f};
            var tops=new[]{.78f,.98f,.86f,.56f};
            for(int i=0;i<3;i++)
            {
                var a=new Vector3(-widths[i],.35f,zs[i]);var b=new Vector3(widths[i],.35f,zs[i]);
                var c=new Vector3(widths[i],tops[i],zs[i]);var d=new Vector3(-widths[i],tops[i],zs[i]);
                var aa=new Vector3(-widths[i+1],.35f,zs[i+1]);var bb=new Vector3(widths[i+1],.35f,zs[i+1]);
                var cc=new Vector3(widths[i+1],tops[i+1],zs[i+1]);var dd=new Vector3(-widths[i+1],tops[i+1],zs[i+1]);
                Quad(a,aa,dd,d);Quad(b,c,cc,bb);Quad(d,dd,cc,c);Quad(a,b,bb,aa);
                if(i==0)Quad(a,d,c,b);if(i==2)Quad(aa,bb,cc,dd);
            }
            MeshObject(car,"Team color wedge body",Finish("Parked race coupe body"),Vector3.zero,Vector3.one,team);
            var p0=new Vector3(-.81f,.98f,-1.25f);var p1=new Vector3(.81f,.98f,-1.25f);
            var p2=new Vector3(.81f,.9f,.9f);var p3=new Vector3(-.81f,.9f,.9f);
            var q0=new Vector3(-.66f,1.55f,-.8f);var q1=new Vector3(.66f,1.55f,-.8f);
            var q2=new Vector3(.66f,1.55f,.15f);var q3=new Vector3(-.66f,1.55f,.15f);
            Quad(p0,q0,q1,p1);Quad(p1,q1,q2,p2);Quad(p2,q2,q3,p3);Quad(p3,q3,q0,p0);
            MeshObject(car,"Dark faceted cockpit glass",Finish("Parked coupe glazing"),Vector3.zero,Vector3.one,navy);
            Box(car,"Ivory coupe roof",new Vector3(0,1.59f,-.325f),new Vector3(1.34f,.08f,.97f),cream);
            foreach(int side in new[]{-1,1})foreach(float z in new[]{-1.4f,1.4f})
            {
                MeshObject(car,"Parked racing tire",wheel,new Vector3(side*1.01f,.36f,z),Vector3.one,navy);
                Box(car,"Ivory wheel hub",new Vector3(side*1.16f,.36f,z),new Vector3(.04f,.26f,.26f),cream);
            }
            Box(car,"Rear wing",new Vector3(0,1.18f,-1.95f),new Vector3(2.25f,.13f,.4f),navy);
            foreach(int side in new[]{-1,1})
            {
                Box(car,"Wing support",new Vector3(side*.65f,1,-1.95f),new Vector3(.1f,.3f,.15f),team);
                Box(car,"Ivory front lamp",new Vector3(side*.52f,.51f,2.28f),new Vector3(.4f,.15f,.1f),cream);
                Box(car,"Coral rear lamp",new Vector3(side*.5f,.6f,-2.29f),new Vector3(.4f,.15f,.1f),pink);
            }
        }
        static void BuildCoastalPaddock(Transform root)
        {
            var paddock=Group(root,"Coastal race paddock and timing tower",Vector3.zero);
            foreach(var child in root.GetComponentsInChildren<Transform>())
            {
                if(!child||child.name!="Sculpted palm cluster tree")continue;
                float lap=PolygonCircuit.ProjectLap(child.position,out _),d=lap*PolygonCircuit.Length;
                float a=PolygonCircuit.AngleAtDistance(d),l=Vector3.Dot(child.position-PolygonWorld.Point(a),PolygonWorld.Side(a));
                if(d>65&&d<205&&l<-34&&l>-102)Object.DestroyImmediate(child.gameObject);
            }
            var apron=Frame(paddock,"Sandy paddock apron",134,-67);
            Box(apron,"Pale paddock hardstanding",new Vector3(0,.015f,0),new Vector3(50,.1f,116),sand);
            var tower=Frame(paddock,"Ivory and teal race control tower",116,-53);
            Box(tower,"Tower base",new Vector3(0,.3f,0),new Vector3(12,.6f,14),cream);
            Box(tower,"Ivory tower shaft",new Vector3(0,5.8f,0),new Vector3(8,11,10),cream);
            Box(tower,"Race control entrance",new Vector3(4.09f,2.1f,0),new Vector3(.18f,3,1.8f),navy);
            foreach(float y in new[]{4.8f,9.5f})foreach(int side in new[]{-1,1})
                Box(tower,"Tower stairwell window",new Vector3(4.09f,y,side*2.8f),new Vector3(.18f,1.3f,1.8f),navy);
            Box(tower,"Coral tower belt",new Vector3(0,7.5f,0),new Vector3(8.3f,.5f,10.3f),pink);
            Box(tower,"Teal observation floor",new Vector3(0,11.6f,0),new Vector3(12,.6f,14),teal);
            Box(tower,"Dark observation glazing",new Vector3(0,13.2f,0),new Vector3(10,2.6f,12),navy);
            foreach(int side in new[]{-1,1})
            {
                foreach(float z in new[]{-6f,-2f,2f,6f})Box(tower,"Ivory window mullion",new Vector3(side*5.1f,13.2f,z),new Vector3(.22f,2.6f,.22f),cream);
                Box(tower,"Observation end mullion",new Vector3(0,13.2f,side*6.1f),new Vector3(.22f,2.6f,.22f),cream);
            }
            Box(tower,"Broad teal shade roof",new Vector3(0,14.8f,0),new Vector3(14,.6f,16),teal);
            Box(tower,"Coral roof crown",new Vector3(0,15.2f,0),new Vector3(9,.2f,11),pink);
            Box(tower,"Roadside observation balcony",new Vector3(7,11.6f,0),new Vector3(2,.6f,14),cream);
            Box(tower,"Balcony top rail",new Vector3(7.9f,13,0),new Vector3(.16f,.18f,14),teal);
            for(int i=0;i<8;i++)Box(tower,"Balcony upright",new Vector3(7.9f,12.4f,-6.5f+i*13/7f),new Vector3(.12f,1.2f,.12f),cream);
            Box(tower,"Radio mast",new Vector3(-2,18,0),new Vector3(.16f,5.6f,.16f),cream);
            foreach(float y in new[]{18f,19.4f,20.6f})Box(tower,"Radio antenna crossbar",new Vector3(-2,y,0),new Vector3(2.5f,.1f,.12f),teal);
            var wheel=PaddockWheelMesh();
            for(int team=0;team<3;team++)
            {
                var tint=team==0?pink:team==1?teal:ochre;
                var tent=Frame(paddock,"Coastal team bay "+(team+1),96+team*36,-77);
                // Open shelters face the road. No walls obscure the parked cars.
                var a=new Vector3(-7,4.5f,-11);var b=new Vector3(7,4.5f,-11);
                var c=new Vector3(7,4.5f,11);var d=new Vector3(-7,4.5f,11);
                var e=new Vector3(0,6.5f,-11);var f=new Vector3(0,6.5f,11);
                Quad(a,d,f,e);Quad(e,f,c,b);Tri(a,e,b);Tri(d,c,f);
                Quad(e,f,d,a);Quad(b,c,f,e);
                MeshObject(tent,"Peaked team shelter",Finish("Coastal team roof "+team),Vector3.zero,Vector3.one,tint);
                foreach(int x in new[]{-7,7})foreach(int z in new[]{-11,11})Box(tent,"Ivory shelter pole",new Vector3(x,2.25f,z),new Vector3(.18f,4.5f,.18f),cream);
                ParkedTeamCar(tent,tint,wheel);
                Box(tent,"Team tool chest",new Vector3(-4,1,-7),new Vector3(2.4f,1.8f,1.4f),tint);
                Box(tent,"Ivory workbench",new Vector3(-4,1.1f,7),new Vector3(3,.22f,1.6f),cream);
                foreach(float z in new[]{6.4f,7.6f})Box(tent,"Workbench leg",new Vector3(-4,.55f,z),new Vector3(.2f,1.1f,.2f),navy);
                for(int tire=0;tire<3;tire++)
                {
                    MeshObject(tent,"Stacked spare tire",wheel,new Vector3(-4,.24f+tire*.3f,-3),Vector3.one,navy);
                    tent.GetChild(tent.childCount-1).localRotation=Quaternion.Euler(0,0,90);
                }
                Box(tent,"Team flag mast",new Vector3(9,4.5f,-10),new Vector3(.12f,9,.12f),cream);
                // Original diagonal maritime flags: adjacent color fields, no text overlays.
                var p=new Vector3(9.1f,6.3f,-10);var q=new Vector3(9.1f,8.7f,-10);
                var r=new Vector3(9.1f,8.1f,-6);var t=new Vector3(9.1f,5.7f,-6);
                Tri(p,q,r);Tri(r,q,p);
                MeshObject(tent,"Team flag color",Finish("Team diagonal flag color "+team),Vector3.zero,Vector3.one,tint);
                Tri(p,r,t);Tri(t,r,p);
                MeshObject(tent,"Team flag ivory",Finish("Team diagonal flag ivory "+team),Vector3.zero,Vector3.one,cream);
            }
            MergeLandmarkMeshes(paddock,"Coastal paddock");
        }

        static float GantryCrown(float x)=>9.1f+.8f*(1-x*x/(22*22));
        static Mesh SignalLensMesh()
        {
            var center=Vector3.zero;var back=new Vector3(0,0,.24f);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI*2/12,b=(i+1)*Mathf.PI*2/12;
                var p=new Vector3(Mathf.Cos(a)*.5f,Mathf.Sin(a)*.5f,0);
                var q=new Vector3(Mathf.Cos(b)*.5f,Mathf.Sin(b)*.5f,0);
                Tri(center,q,p);Tri(back,p+back,q+back);Quad(p,q,q+back,p+back);
            }
            return Finish("Twelve sided starting signal lens");
        }
        static void BuildCoastGantry(Transform root)
        {
            // Replace the original narrow sign. Lap timing and start checks stay put.
            var world=root.parent;
            foreach(var child in world.GetComponentsInChildren<Transform>())
                if(child&&child.name=="Expressway start gantry")Object.DestroyImmediate(child.gameObject);
            var gate=Frame(root,"POLYGON COAST - festival start gantry",32,0);
            foreach(int side in new[]{-1,1})
            {
                Box(gate,"Ivory gantry footing",new Vector3(side*21,.35f,0),new Vector3(2,.7f,2.2f),cream);
                Box(gate,"Ivory coastal upright",new Vector3(side*21,4.1f,0),new Vector3(1,8.2f,1.2f),cream);
                Box(gate,"Coral upright collar",new Vector3(side*21,1.05f,0),new Vector3(1.2f,.7f,1.4f),pink);
            }
            // The faceted teal header sweeps upward in the center.
            var breaks=new[]{-22f,-14f,-7f,0,7f,14f,22f};
            for(int i=0;i<breaks.Length-1;i++)
            {
                float x=breaks[i],b=breaks[i+1],y=GantryCrown(x),h=GantryCrown(b);
                var a=new Vector3(x,y-1,-.6f);var bb=new Vector3(b,h-1,-.6f);
                var c=new Vector3(b,h+1,-.6f);var d=new Vector3(x,y+1,-.6f);
                Quad(d,c,bb,a);Quad(a+Vector3.forward*1.2f,bb+Vector3.forward*1.2f,c+Vector3.forward*1.2f,d+Vector3.forward*1.2f);
                Quad(a,bb,bb+Vector3.forward*1.2f,a+Vector3.forward*1.2f);
                Quad(d,d+Vector3.forward*1.2f,c+Vector3.forward*1.2f,c);
            }
            foreach(int side in new[]{-1,1})
            {
                float x=side*22,y=GantryCrown(x);
                var a=new Vector3(x,y-1,-.6f);var b=new Vector3(x,y+1,-.6f);
                if(side<0)Quad(a,a+Vector3.forward*1.2f,b+Vector3.forward*1.2f,b);
                else Quad(a,b,b+Vector3.forward*1.2f,a+Vector3.forward*1.2f);
            }
            MeshObject(gate,"Sweeping teal header",Finish("Coastal gantry crown"),Vector3.zero,Vector3.one,teal);
            // Checkered blocks are adjacent solids; there is no layered check texture.
            foreach(int side in new[]{-1,1})for(int row=0;row<3;row++)for(int col=0;col<4;col++)
                Box(gate,"Ivory navy checkered end panel",new Vector3(side*17+(col-1.5f)*.65f,9.4f+(row-1)*.65f,-.72f),new Vector3(.65f,.65f,.3f),(row+col)%2==0?cream:navy);
            // Keep text in a flat clear zone, forward of the complete header face.
            var frontTitle=PolygonWorld.Label("Polygon Coast race lettering",gate,"POLYGON COAST",new Vector3(0,9.9f,-.76f),.22f,cream.color);
            var rearTitle=PolygonWorld.Label("Polygon Coast reverse lettering",gate,"POLYGON COAST",new Vector3(0,9.9f,.76f),.22f,cream.color);
            rearTitle.transform.localRotation=Quaternion.Euler(0,180,0);
            var letteringShader=Resources.Load<Shader>("PolygonGantryLettering");
            if(!letteringShader)throw new System.InvalidOperationException("Copy PolygonGantryLettering.shader into Assets/PolygonRacing/Resources, let Unity import it, then refresh scenery again.");
            var letteringMaterial=new Material(letteringShader){name="Polygon Coast depth tested lettering"};
            letteringMaterial.mainTexture=frontTitle.font.material.mainTexture;
            frontTitle.GetComponent<MeshRenderer>().sharedMaterial=letteringMaterial;
            rearTitle.GetComponent<MeshRenderer>().sharedMaterial=letteringMaterial;
            var lettering=gate.gameObject.AddComponent<PolygonGantryLettering>();
            lettering.FrontText=frontTitle;lettering.RearText=rearTitle;lettering.DepthMaterial=letteringMaterial;
            foreach(int side in new[]{-1,1})Box(gate,"Signal bar suspension",new Vector3(side*4,8.55f,0),new Vector3(.14f,.9f,.18f),cream);
            var dark=Mat("Starting lens dark",new Color(.065f,.018f,.025f));
            var lit=Mat("Starting lens red",new Color(1,.025f,.012f));
            if(lit.HasProperty("_EmissionColor")){lit.SetColor("_EmissionColor",new Color(1,.02f,.008f));lit.EnableKeyword("_EMISSION");}
            // Both signal materials are assigned to renderers so editor asset persistence saves them.
            Box(gate,"Charcoal starting signal housing",new Vector3(0,7.3f,-.1f),new Vector3(9.3f,1.6f,1.2f),dark);
            var lens=SignalLensMesh();var lenses=new MeshRenderer[5];
            for(int i=0;i<5;i++)
            {
                string name="Starting red lens "+(i+1);
                MeshObject(gate,name,lens,new Vector3((i-2)*1.7f,7.3f,-.91f),Vector3.one,lit);
                lenses[i]=gate.Find(name).GetComponent<MeshRenderer>();
            }
            var controller=gate.gameObject.AddComponent<PolygonStartLights>();
            controller.Lenses=lenses;controller.DarkMaterial=dark;controller.RedMaterial=lit;
        }

        static void BuildBridge(Transform root)
        {
            float distance=PolygonCircuit.DistanceAtKnot(2);
            var bridge=Frame(root,"Ivory estuary crossing",distance,0);
            // An open-top structural shell: the road and footpaths supply the deck top.
            // There is no second slab face just beneath the road paint.
            const float x=18.5f,z=110,top=.045f,bottom=-.55f;
            var a=new Vector3(-x,bottom,-z);var b=new Vector3(x,bottom,-z);var c=new Vector3(x,bottom,z);var d=new Vector3(-x,bottom,z);
            var aa=new Vector3(-x,top,-z);var bb=new Vector3(x,top,-z);var cc=new Vector3(x,top,z);var dd=new Vector3(-x,top,z);
            Quad(a,aa,bb,b);Quad(d,c,cc,dd);Quad(a,d,dd,aa);Quad(b,bb,cc,c);Quad(a,b,c,d);
            MeshObject(bridge,"Bridge shell without duplicate deck top",Finish("Open-top bridge shell"),Vector3.zero,Vector3.one,cream);
            foreach(int side in new[]{-1,1})
            {
                float lo=side<0?-18.5f:16,hi=side<0?-16:18.5f;
                GroundQuad(new Vector3(lo,top,-110),new Vector3(hi,top,-110),new Vector3(hi,top,110),new Vector3(lo,top,110));
            }
            MeshObject(bridge,"Adjoining bridge footpaths",Finish("Bridge footpath tops"),Vector3.zero,Vector3.one,cream);
            var cuts=new SortedSet<float>{-110,110};
            for(int n=-4;n<=4;n++){cuts.Add(n*25-.3f);cuts.Add(n*25+.3f);}
            foreach(int end in new[]{-1,1}){cuts.Add(end*88-.9f);cuts.Add(end*88+.9f);}
            var breaks=new List<float>(cuts);
            foreach(int side in new[]{-1,1})
            {
                for(int i=0;i<breaks.Count-1;i++)
                {
                    float mid=(breaks[i]+breaks[i+1])*.5f;bool blocked=false;
                    for(int n=-4;n<=4;n++)if(Mathf.Abs(mid-n*25)<.3f)blocked=true;
                    if(Mathf.Abs(mid-88)<.9f||Mathf.Abs(mid+88)<.9f)blocked=true;
                    if(!blocked)Box(bridge,"Adjoining ivory bridge parapet",new Vector3(side*18,.3f,mid),new Vector3(.4f,.6f,breaks[i+1]-breaks[i]),cream);
                }
                for(int n=-4;n<=4;n++)
                {
                    Box(bridge,"Teal bridge marker",new Vector3(side*18,.7f,n*25),new Vector3(.6f,1.4f,.6f),teal);
                    Box(bridge,"Bridge support pier",new Vector3(side*16,-3.55f,n*25),new Vector3(1.3f,6,1.3f),cream);
                }
            }
            foreach(int end in new[]{-1,1})
            {
                // Post tops meet the beam underside at 11.3 m; their faces never overlap.
                Box(bridge,"Bridge gate left",new Vector3(-18,5.65f,end*88),new Vector3(1.4f,11.3f,1.8f),teal);
                Box(bridge,"Bridge gate right",new Vector3(18,5.65f,end*88),new Vector3(1.4f,11.3f,1.8f),teal);
                Box(bridge,"Bridge gate lintel",new Vector3(0,12,end*88),new Vector3(37.4f,1.4f,1.8f),cream);
            }
            // The generic road rails were a second set of barriers beside this bridge.
            var world=root.parent;
            foreach(var child in world.GetComponentsInChildren<Transform>())
            {
                if(!child||(child.name!="Low roadside barrier"&&child.name!="Roadside delineator"))continue;
                float lap=PolygonCircuit.ProjectLap(child.position,out _);
                float delta=Mathf.Repeat(lap*PolygonCircuit.Length-distance+PolygonCircuit.Length*.5f,PolygonCircuit.Length)-PolygonCircuit.Length*.5f;
                if(Mathf.Abs(delta)<134)Object.DestroyImmediate(child.gameObject);
            }
        }
        static void BuildLighthouse(Transform root)
        {
            var p=new Vector3(615,-.8f,746);Rock(root,"Lighthouse rocky islet",p,new Vector3(24,7,17),ochre,12);
            var t=Group(root,"Coral Coast lighthouse landmark",p+Vector3.up*5.8f);
            MeshObject(t,"Striped lighthouse tiers cream",PolygonSceneryMeshes.LighthouseTiers(0),Vector3.zero,Vector3.one,cream);
            MeshObject(t,"Striped lighthouse tiers coral",PolygonSceneryMeshes.LighthouseTiers(1),Vector3.zero,Vector3.one,pink);
            MeshObject(t,"Lighthouse observation gallery",PolygonSceneryMeshes.LighthouseGallery(),Vector3.zero,Vector3.one,teal);
            MeshObject(t,"Lantern glazing",PolygonSceneryMeshes.LighthouseLantern(),Vector3.zero,Vector3.one,navy);
        }
        // Physical signs and paint follow the same route as the race. No HUD overlays.
        static void BuildCornerGuidance(Transform root)
        {
            var group=Group(root,"Coastal corner guidance and Coral Sweep",Vector3.zero);
            Mesh left=CreateDirectionFace(-1,true),right=CreateDirectionFace(1,true);
            Mesh leftBackground=CreateDirectionFace(-1,false),rightBackground=CreateDirectionFace(1,false);
            foreach(float knot in new[]{5.8f,8.5f,17.7f,19.3f,22.2f,24.8f,26.4f})
            {
                float apex=PolygonCircuit.DistanceAtKnot(knot);
                float turn=PolygonCircuit.Curvature(apex);
                int direction=turn>=0?1:-1;float outside=-direction;
                // Early approach board, then repeated confirmation near the bend.
                foreach(float before in new[]{90f,55f,20f,-15f})
                {
                    var board=Frame(group,"Ivory chevrons - "+(direction>0?"right":"left"),apex-before,outside*(PolygonWorld.RoadHalfWidth+13));
                    if(!Clear(board.position,3)){Object.DestroyImmediate(board.gameObject);continue;}
                    foreach(int sign in new[]{-1,1})
                    {
                        Box(board,"Coral horizontal frame",new Vector3(0,2.55f+sign*.9f,-.14f),new Vector3(4.9f,.15f,.08f),pink);
                        Box(board,"Coral vertical frame",new Vector3(sign*2.375f,2.55f,-.14f),new Vector3(.15f,1.65f,.08f),pink);
                    }
                    MeshObject(board,"Teal inlaid direction face",direction>0?rightBackground:leftBackground,new Vector3(0,2.55f,-.18f),Vector3.one,teal);
                    foreach(int side in new[]{-1,1})Box(board,"Coral sign support",new Vector3(side*1.65f,.85f,0),new Vector3(.16f,1.7f,.16f),pink);
                    MeshObject(board,"Bold ivory double chevron",direction>0?right:left,new Vector3(0,2.55f,-.18f),Vector3.one,cream);
                }
            }
            BuildCoralSweep(group);
        }
        static void FrontQuad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            // Sign faces look back toward approaching cars (local negative Z).
            if(Vector3.Cross(b-a,c-a).z>0)Quad(d,c,b,a);else Quad(a,b,c,d);
        }
        static float ChevronEdge(float y,float center,int direction,bool right)
        {
            float tip=.64f-Mathf.Abs(y)*(.79f/.58f),start=tip-.57f;
            return center+(direction>0?(right?tip:start):(right?-start:-tip));
        }
        static Mesh CreateDirectionFace(int direction,bool arrows)
        {
            // Arrow and background are adjoining polygons on one plane, never overlays.
            if(!arrows)
            {
                FrontQuad(new Vector3(-2.3f,-.825f,0),new Vector3(2.3f,-.825f,0),new Vector3(2.3f,-.58f,0),new Vector3(-2.3f,-.58f,0));
                FrontQuad(new Vector3(-2.3f,.58f,0),new Vector3(2.3f,.58f,0),new Vector3(2.3f,.825f,0),new Vector3(-2.3f,.825f,0));
            }
            for(int half=0;half<2;half++)
            {
                float ya=half==0?-.58f:0,yb=half==0?0:.58f;
                var a=new[]{-2.3f,ChevronEdge(ya,-.95f,direction,false),ChevronEdge(ya,-.95f,direction,true),ChevronEdge(ya,.95f,direction,false),ChevronEdge(ya,.95f,direction,true),2.3f};
                var b=new[]{-2.3f,ChevronEdge(yb,-.95f,direction,false),ChevronEdge(yb,-.95f,direction,true),ChevronEdge(yb,.95f,direction,false),ChevronEdge(yb,.95f,direction,true),2.3f};
                for(int band=0;band<5;band++)if((band%2==1)==arrows)
                    FrontQuad(new Vector3(a[band],ya,0),new Vector3(a[band+1],ya,0),new Vector3(b[band+1],yb,0),new Vector3(b[band],yb,0));
            }
            if(!arrows)Quad(new Vector3(-2.3f,-.825f,0),new Vector3(2.3f,-.825f,0),new Vector3(2.3f,.825f,0),new Vector3(-2.3f,.825f,0));
            return Finish((direction>0?"Right":"Left")+(arrows?" inlaid arrows":" inlaid background"));
        }
        static void BuildCornerCurbs(Transform root)
        {
            // Three merged meshes share the existing palette. Stripes are measured in metres.
            for(int color=0;color<3;color++)
            {
                for(float d=0;d<PolygonCircuit.Length;d+=4)
                {
                    float k=PolygonCircuit.Curvature(d+2);if(Mathf.Abs(k)<.0028f)continue;
                    float outside=k>0?-1:1;int stripe=Mathf.FloorToInt(d/4);
                    foreach(int edge in new[]{-1,1})
                    {
                        int tint=stripe%2==1?1:edge==1?0:2;if(tint!=color)continue;
                        float side=outside*edge;
                        float a=PolygonCircuit.AngleAtDistance(d),b=PolygonCircuit.AngleAtDistance(Mathf.Min(d+4,PolygonCircuit.Length));
                        float inner=side*(PolygonWorld.RoadHalfWidth+.25f),outer=side*(PolygonWorld.RoadHalfWidth+1.45f);
                        GroundQuad(PolygonWorld.Point(a)+PolygonWorld.Side(a)*inner+Vector3.up*.065f,
                            PolygonWorld.Point(a)+PolygonWorld.Side(a)*outer+Vector3.up*.065f,
                            PolygonWorld.Point(b)+PolygonWorld.Side(b)*outer+Vector3.up*.065f,
                            PolygonWorld.Point(b)+PolygonWorld.Side(b)*inner+Vector3.up*.065f);
                    }
                }
                MeshObject(root,color==0?"Coral outside curb stripes":color==1?"Ivory curb stripes":"Teal inside curb stripes",
                    Finish("Merged coastal curb stripes "+color),Vector3.zero,Vector3.one,color==0?pink:color==1?cream:teal);
            }
        }
        static void BuildCoralSweep(Transform root)
        {
            float center=PolygonCircuit.DistanceAtKnot(9.4f);
            float outside=PolygonCircuit.Curvature(center)>0?-1:1;
            var name=Frame(root,"Coral Sweep corner name",center-110,outside*27);
            Box(name,"Coral corner plaque",new Vector3(0,3,0),new Vector3(6,2.6f,.22f),teal);
            foreach(int side in new[]{-1,1})Box(name,"Plaque upright",new Vector3(side*2.2f,1.3f,0),new Vector3(.18f,2.6f,.18f),pink);
            PolygonWorld.Label("Coral Sweep lettering",name,"CORAL\nSWEEP",new Vector3(0,3,-.13f),.075f,cream.color);
            for(int i=0;i<6;i++)
            {
                var banner=Frame(root,"Coral Sweep festival pennant",center-100+i*33,outside*26);
                if(!Clear(banner.position,2)){Object.DestroyImmediate(banner.gameObject);continue;}
                Box(banner,"Ivory pennant pole",new Vector3(0,3.5f,0),new Vector3(.14f,7,.14f),cream);
                Box(banner,"Coral and teal pennant",new Vector3(0,5.3f,.9f),new Vector3(.07f,2.8f,1.5f),i%2==0?pink:teal);
            }
            var lookout=Frame(root,"Coral Sweep spectator lookout",center+5,outside*55);
            if(!Clear(lookout.position,18)){Object.DestroyImmediate(lookout.gameObject);return;}
            Box(lookout,"Sandy lookout plinth",new Vector3(0,.3f,0),new Vector3(17,.6f,28),sand);
            for(int row=0;row<3;row++)
            {
                float x=outside*(-4+row*3);
                Box(lookout,"Low lookout seating",new Vector3(x,.9f+row*.8f,0),new Vector3(3.2f,.8f,23),row%2==0?teal:cream);
                for(int person=0;person<10;person++)Box(lookout,"Lookout spectator",new Vector3(x,1.8f+row*.8f,-10+person*2.2f),new Vector3(.48f,.65f,.42f),(person+row)%2==0?pink:navy);
            }
            for(int stripe=0;stripe<7;stripe++)
                Box(lookout,"Striped lookout canopy",new Vector3(0,5.8f,-12+stripe*4),new Vector3(16,.3f,4),stripe%2==0?pink:cream);
            foreach(int z in new[]{-12,12})Box(lookout,"Lookout canopy support",new Vector3(outside*6,2.9f,z),new Vector3(.22f,5.8f,.22f),teal);
        }
        static void BuildBoats(Transform root)
        {
            for(int i=0;i<5;i++)
            {
                var boat=Group(root,"Little offshore sailboat",new Vector3(-100+i*245,-.8f,810+i%2*43));boat.localRotation=Quaternion.Euler(0,i*37,0);
                boat.localScale=Vector3.one*1.3f;
                MeshObject(boat,"Angular boat hull",boatHull,Vector3.zero,Vector3.one,cream);
                MeshObject(boat,"Boat cabin",boatCabin,Vector3.zero,Vector3.one,teal);
                MeshObject(boat,"Mast",boatRigging,Vector3.zero,Vector3.one,navy);
                MeshObject(boat,"Triangular sail",boatSails,Vector3.zero,Vector3.one,i%2==0?pink:cream);
            }
        }
    }
}
