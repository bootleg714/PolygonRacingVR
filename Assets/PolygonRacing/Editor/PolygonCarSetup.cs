#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PolygonRacing.Editor
{
    // Car and cockpit pass: replaces the player car's box body with a faceted open-wheel
    // body, steering wheel, tyres and rims. Keeps the car root, PolygonDriver settings,
    // seated tracking space/camera, tuned Dashboard box and instrument text, steering-wheel
    // and wheel pivots (so steering, wheel spin and rival copies keep working).
    public static class PolygonCarSetup
    {
        const string ForestPath="Assets/PolygonRacing/Scenes/ForestCircuit.unity";
        const string CoastalPath="Assets/PolygonRacing/Scenes/CoastalExpressway.unity";
        const string CarPath="Assets/PolygonRacing/Generated/Car";
        const string BodyName="Car body - faceted",WheelName="Steering wheel - faceted",RimName="Wheel rim - faceted";

        // Submesh / material slots.
        const int Red=0,Ivory=1,Carbon=2,Metal=3,Yellow=4,Mirror=5,Cockpit=6,SlotCount=7;

        static readonly HashSet<string> OldBodyParts=new HashSet<string>{
            "Wedge nose","Nose stripe","Chassis","Side pod","White side stripe","Cockpit rim","Seat back","Seat cushion",
            "Engine cover","Front wing","Rear wing","Rear wing support","Wing endplate","Suspension arm",BodyName};
        static readonly HashSet<string> OldWheelParts=new HashSet<string>{
            "Wheel top","Wheel lower","Wheel grip","Wheel hub","Wheel center stripe",WheelName};

        [MenuItem("Polygon Racing/10 - Rebuild Player Car (Faceted)")]
        static void RebuildBothCircuits()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play Mode before rebuilding the car.");return;}
            if(!Resources.Load<Shader>(PolygonLook.FlatShaderName))
            {
                EditorUtility.DisplayDialog("Polygon Racing car","PolygonFlat.shader is missing from Assets/PolygonRacing/Resources. Nothing was changed.","OK");
                return;
            }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EnsureFolder();
            var materials=CarMaterials();
            var meshes=BuildMeshes();
            int done=0;
            foreach(var path in new[]{ForestPath,CoastalPath})
            {
                if(!File.Exists(path)){Debug.LogWarning("Skipped missing scene "+path);continue;}
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                string backup="PolygonRacingBackups/"+Path.GetFileNameWithoutExtension(path)+"_before_car_"+
                    System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity";
                try{Directory.CreateDirectory("PolygonRacingBackups");File.Copy(path,backup,false);}
                catch(System.Exception ex){Debug.LogException(ex);Debug.LogError("Could not back up "+path+". It was not changed.");continue;}
                var driver=Object.FindFirstObjectByType<PolygonDriver>();
                if(!driver){Debug.LogError("No PolygonDriver in "+path+". It was not changed.");continue;}
                ApplyToCar(driver,materials,meshes);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene)){Debug.LogError("Unity could not save "+path+".");continue;}
                done++;
                Debug.Log($"Faceted car applied in {path}. Backup: {backup}");
            }
            AssetDatabase.SaveAssets();
            if(done>0)Debug.Log("Player car rebuilt in "+done+" circuit(s). Seat, tracking, dashboard and handling are unchanged. Rivals copy the new body automatically.");
        }

        struct CarMeshes{public Mesh Body,Steering,Tire,RimLeft,RimRight;}

        static void ApplyToCar(PolygonDriver driver,Material[] materials,CarMeshes meshes)
        {
            var car=driver.transform;
            for(int i=car.childCount-1;i>=0;i--)
            {
                var child=car.GetChild(i);
                if(OldBodyParts.Contains(child.name))Object.DestroyImmediate(child.gameObject);
            }
            var body=new GameObject(BodyName,typeof(MeshFilter),typeof(MeshRenderer));
            body.transform.SetParent(car,false);
            body.transform.SetSiblingIndex(0);
            Assign(body,meshes.Body,materials);

            if(driver.steeringWheel)
            {
                var wheel=driver.steeringWheel;
                for(int i=wheel.childCount-1;i>=0;i--)
                    if(OldWheelParts.Contains(wheel.GetChild(i).name))Object.DestroyImmediate(wheel.GetChild(i).gameObject);
                var go=new GameObject(WheelName,typeof(MeshFilter),typeof(MeshRenderer));
                go.transform.SetParent(wheel,false);
                Assign(go,meshes.Steering,materials);
            }

            if(driver.wheels!=null)
                foreach(var tire in driver.wheels)
                {
                    if(!tire)continue;
                    for(int i=tire.childCount-1;i>=0;i--)
                    {
                        var child=tire.GetChild(i);
                        if(child.name=="Wheel hub"||child.name==RimName)Object.DestroyImmediate(child.gameObject);
                    }
                    var filter=tire.GetComponent<MeshFilter>();if(!filter)filter=tire.gameObject.AddComponent<MeshFilter>();
                    var renderer=tire.GetComponent<MeshRenderer>();if(!renderer)renderer=tire.gameObject.AddComponent<MeshRenderer>();
                    filter.sharedMesh=meshes.Tire;
                    renderer.sharedMaterials=materials;
                    renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                    float side=car.InverseTransformPoint(tire.position).x<0?-1:1;
                    var rim=new GameObject(RimName,typeof(MeshFilter),typeof(MeshRenderer));
                    rim.transform.SetParent(tire,false);
                    Assign(rim,side<0?meshes.RimLeft:meshes.RimRight,materials);
                    EditorUtility.SetDirty(tire.gameObject);
                }
            EditorUtility.SetDirty(car.gameObject);
        }

        static void Assign(GameObject go,Mesh mesh,Material[] materials)
        {
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>();
            renderer.sharedMaterials=materials;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        }

        // ------------------------------------------------------------ materials
        static Material[] CarMaterials()
        {
            var shader=Resources.Load<Shader>(PolygonLook.FlatShaderName);
            var livery=new Color(.9f,.055f,.045f);
            var existing=AssetDatabase.LoadAssetAtPath<Material>("Assets/PolygonRacing/Generated/PR_Vermilion.mat");
            if(existing&&existing.HasProperty("_BaseColor"))livery=existing.GetColor("_BaseColor");
            // The livery name must start with PR_Vermilion: rivals recolour those materials.
            return new[]{
                CarMaterial("PR_Vermilion car body",livery,shader),
                CarMaterial("PR_Car ivory",new Color(.94f,.95f,.9f),shader),
                CarMaterial("PR_Car carbon",new Color(.055f,.07f,.09f),shader),
                CarMaterial("PR_Car metal",new Color(.30f,.35f,.40f),shader),
                CarMaterial("PR_Car yellow",new Color(1,.78f,.05f),shader),
                CarMaterial("PR_Car mirror",new Color(.78f,.86f,.92f),shader),
                CarMaterial("PR_Car cockpit",new Color(.16f,.19f,.24f),shader)};
        }

        static Material CarMaterial(string name,Color color,Shader shader)
        {
            string path=$"{CarPath}/{name}.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created=!mat;
            if(created){mat=new Material(shader){name=name};AssetDatabase.CreateAsset(mat,path);}
            mat.shader=shader;
            if(created)mat.SetColor("_BaseColor",color);   // keep any colour tuned in the Inspector
            mat.SetColor("_EmissionColor",Color.black);
            mat.SetFloat("_Cull",0);                         // double-sided: open panels, wings and walls
            mat.SetColor("_SkyAmbient",new Color(.60f,.74f,.86f));mat.SetColor("_GroundAmbient",new Color(.50f,.42f,.33f));
            mat.SetFloat("_AmbientStrength",.75f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------ meshes
        static CarMeshes BuildMeshes()
        {
            var body=new CarMesh();BuildBody(body);
            var steering=new CarMesh();BuildSteering(steering);
            var tire=new CarMesh();BuildTire(tire);
            var rimLeft=new CarMesh();BuildRim(rimLeft,-1);
            var rimRight=new CarMesh();BuildRim(rimRight,1);
            return new CarMeshes{
                Body=SaveMesh(body,"Car body"),Steering=SaveMesh(steering,"Steering wheel"),
                Tire=SaveMesh(tire,"Tire"),RimLeft=SaveMesh(rimLeft,"Rim left"),RimRight=SaveMesh(rimRight,"Rim right")};
        }

        static Mesh SaveMesh(CarMesh source,string name)
        {
            string path=$"{CarPath}/{name}.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,path);}
            source.Fill(mesh);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static Vector2[] NoseRing(float w,float y0,float h,float ridge)
        {
            return new[]{
                new Vector2(.70f*w,y0),new Vector2(w,y0+.30f*h),new Vector2(.92f*w,y0+.75f*h),new Vector2(.50f*w,y0+h),
                new Vector2(0,y0+h+ridge),
                new Vector2(-.50f*w,y0+h),new Vector2(-.92f*w,y0+.75f*h),new Vector2(-w,y0+.30f*h),new Vector2(-.70f*w,y0)};
        }
        static readonly int[] NoseMats={Red,Red,Red,Ivory,Ivory,Red,Red,Red,Carbon};

        static Vector2[] Pod(float xi,float xo,float yb,float yt)
        {
            return new[]{
                new Vector2(xi,yb),new Vector2(xo-.06f,yb),new Vector2(xo,yb+.25f*(yt-yb)),
                new Vector2(xo-.02f,yt-.08f),new Vector2(xo-.10f,yt),new Vector2(xi,yt+.02f)};
        }

        static Vector2[] Flip(Vector2[] points)
        {
            var result=new Vector2[points.Length];
            for(int i=0;i<points.Length;i++)result[i]=new Vector2(-points[i].x,points[i].y);
            return result;
        }

        static void BuildBody(CarMesh b)
        {
            // Nose and monocoque front, from the cockpit bulkhead (z .48) to the tip (z 3.05).
            float[,] nose={{.48f,.42f,.14f,.56f,.06f},{1.00f,.38f,.15f,.45f,.05f},{1.60f,.32f,.17f,.35f,.04f},
                           {2.20f,.25f,.19f,.25f,.03f},{2.70f,.17f,.21f,.17f,.02f},{3.05f,.10f,.24f,.10f,.01f}};
            b.LoftZ(Sections(nose),NoseMats,Carbon,Red,true);

            // Cockpit tub side walls (open profile), floor, bulkhead and seat.
            var wall=new[]{new Vector2(.30f,.20f),new Vector2(.31f,.72f),new Vector2(.37f,.79f),
                           new Vector2(.44f,.74f),new Vector2(.46f,.45f),new Vector2(.31f,.14f)};
            var wallMats=new[]{Cockpit,Cockpit,Red,Red,Carbon};
            foreach(var profile in new[]{wall,Flip(wall)})
                b.LoftZ(new List<(float,Vector2[])>{(-1.0f,profile),(-0.2f,profile),(0.62f,profile)},wallMats,-1,-1,false);
            b.Box(new Vector3(0,.16f,-.19f),new Vector3(.62f,.04f,1.62f),Cockpit);
            b.Quad(new Vector3(-.31f,.14f,-1.0f),new Vector3(.31f,.14f,-1.0f),new Vector3(.31f,.86f,-1.0f),new Vector3(-.31f,.86f,-1.0f),Carbon);
            b.Box(new Vector3(0,.58f,-.90f),new Vector3(.56f,.62f,.08f),Carbon);
            b.Box(new Vector3(0,.26f,-.62f),new Vector3(.56f,.10f,.50f),Carbon);

            // Engine cover and airbox behind the driver.
            float[,] engine={{-0.95f,.44f,.14f,.70f,.10f},{-1.35f,.40f,.14f,.78f,.14f},
                             {-1.80f,.30f,.14f,.55f,.08f},{-2.25f,.16f,.16f,.25f,.03f}};
            b.LoftZ(Sections(engine),NoseMats,Carbon,Carbon,true);

            // Sidepods with dark intakes, shaped and tapering toward the rear.
            float[,] pods={{-1.95f,.34f,.44f,.20f,.30f},{-1.50f,.40f,.66f,.18f,.42f},{-0.90f,.42f,.90f,.18f,.56f},
                           {-0.10f,.42f,.95f,.18f,.62f},{0.30f,.40f,.86f,.18f,.58f}};
            var podMats=new[]{Carbon,Red,Red,Ivory,Red,Carbon};
            foreach(float s in new[]{1f,-1f})
            {
                var sections=new List<(float,Vector2[])>();
                for(int i=0;i<pods.GetLength(0);i++)
                {
                    var ring=Pod(pods[i,1],pods[i,2],pods[i,3],pods[i,4]);
                    sections.Add((pods[i,0],s>0?ring:Flip(ring)));
                }
                b.LoftZ(sections,podMats,Red,Carbon,true);
            }

            // Floor, diffuser and strakes.
            b.Box(new Vector3(0,.12f,-.55f),new Vector3(1.84f,.05f,3.2f),Carbon);
            b.Quad(new Vector3(-.62f,.145f,-2.15f),new Vector3(.62f,.145f,-2.15f),new Vector3(.62f,.32f,-2.50f),new Vector3(-.62f,.32f,-2.50f),Carbon);
            foreach(float x in new[]{-.4f,0f,.4f})
                b.ExtrudeX(ZY(-2.10f,.145f,-2.52f,.145f,-2.52f,.34f),x-.015f,x+.015f,Metal);

            // Front wing: ivory main plane, livery flap, carbon endplates and nose pylons.
            b.ExtrudeX(ZY(2.95f,.13f,2.80f,.165f,2.55f,.17f,2.35f,.15f,2.55f,.115f,2.80f,.11f),-1.33f,1.33f,Ivory);
            b.ExtrudeX(ZY(2.55f,.20f,2.40f,.27f,2.30f,.29f,2.38f,.24f,2.52f,.185f),-1.25f,1.25f,Red);
            var frontPlate=ZY(2.98f,.08f,2.98f,.20f,2.70f,.34f,2.28f,.34f,2.28f,.08f);
            foreach(float s in new[]{1f,-1f})
            {
                b.ExtrudeX(frontPlate,s*1.33f,s*1.37f,Carbon);
                b.Box(new Vector3(s*.11f,.19f,2.60f),new Vector3(.03f,.08f,.30f),Carbon);
            }

            // Rear wing: livery main plane, ivory flap, carbon endplates, metal swan-neck pylons.
            b.ExtrudeX(ZY(-2.05f,.98f,-2.15f,1.02f,-2.40f,1.03f,-2.55f,1.00f,-2.40f,.97f,-2.15f,.96f),-.95f,.95f,Red);
            b.ExtrudeX(ZY(-2.38f,1.08f,-2.48f,1.15f,-2.58f,1.18f,-2.55f,1.13f,-2.45f,1.08f),-.95f,.95f,Ivory);
            var rearPlate=ZY(-2.0f,.62f,-2.0f,1.10f,-2.25f,1.24f,-2.65f,1.24f,-2.65f,.62f);
            var pylon=ZY(-1.95f,.35f,-2.10f,.35f,-2.30f,.97f,-2.18f,.97f);
            foreach(float s in new[]{1f,-1f})
            {
                b.ExtrudeX(rearPlate,s*.95f,s*.99f,Carbon);
                b.ExtrudeX(pylon,s*.10f,s*.13f,Metal);
            }
            b.ExtrudeX(ZY(-2.45f,.45f,-2.65f,.47f,-2.65f,.50f,-2.45f,.49f),-.55f,.55f,Carbon);

            // Wishbones and pushrods to each wheel centre (static; wheels steer and spin separately).
            foreach(var (zc,xb) in new[]{(1.40f,.38f),(-1.45f,.36f)})
                foreach(float s in new[]{1f,-1f})
                {
                    var upper=new Vector3(s*.95f,.50f,zc);var lower=new Vector3(s*.97f,.28f,zc);
                    b.Tube(new Vector3(s*xb,.52f,zc+.25f),upper,.022f,Carbon);
                    b.Tube(new Vector3(s*xb,.50f,zc-.25f),upper,.022f,Carbon);
                    b.Tube(new Vector3(s*xb,.22f,zc+.25f),lower,.022f,Carbon);
                    b.Tube(new Vector3(s*xb,.22f,zc-.25f),lower,.022f,Carbon);
                    b.Tube(new Vector3(s*.95f,.30f,zc-.02f),new Vector3(s*(xb+.04f),.60f,zc-.2f),.018f,Metal);
                }

            // Mirrors on short stalks, glass facing the driver.
            foreach(float s in new[]{1f,-1f})
            {
                b.Tube(new Vector3(s*.44f,.74f,.25f),new Vector3(s*.58f,.84f,.20f),.015f,Carbon);
                b.Box(new Vector3(s*.64f,.86f,.19f),new Vector3(.17f,.075f,.06f),Red,Mirror);
            }
        }

        static void BuildSteering(CarMesh b)
        {
            // Built around the existing "Steering wheel" pivot; its top stays below the dashboard sight line.
            var rim=new[]{new Vector2(-.19f,.11f),new Vector2(-.12f,.135f),new Vector2(.12f,.135f),new Vector2(.19f,.11f),
                          new Vector2(.20f,-.02f),new Vector2(.17f,-.11f),new Vector2(.10f,-.13f),new Vector2(-.10f,-.13f),
                          new Vector2(-.17f,-.11f),new Vector2(-.20f,-.02f)};
            for(int i=0;i<rim.Length;i++)
            {
                var a=rim[i];var c=rim[(i+1)%rim.Length];
                bool grip=Mathf.Abs(a.x)>.18f&&Mathf.Abs(c.x)>.16f;
                b.Tube(new Vector3(a.x,a.y,0),new Vector3(c.x,c.y,0),grip?.03f:.018f,Carbon);
            }
            b.Box(new Vector3(0,0,.005f),new Vector3(.22f,.14f,.03f),Metal);
            foreach(float s in new[]{1f,-1f})b.Box(new Vector3(s*.15f,-.01f,.005f),new Vector3(.08f,.035f,.02f),Carbon);
            b.Box(new Vector3(-.06f,.035f,-.017f),new Vector3(.03f,.025f,.014f),Yellow);
            b.Box(new Vector3(.06f,.035f,-.017f),new Vector3(.03f,.025f,.014f),Mirror);
            b.Box(new Vector3(-.06f,-.035f,-.017f),new Vector3(.03f,.025f,.014f),Red);
            b.Box(new Vector3(.06f,-.035f,-.017f),new Vector3(.03f,.025f,.014f),Ivory);
            b.Box(new Vector3(0,.135f,-.005f),new Vector3(.04f,.045f,.04f),Yellow);
        }

        static void BuildTire(CarMesh b)
        {
            // 18-sided tyre around the X axle with chamfered shoulders and a metal barrel.
            const int sides=18;
            var profile=new[]{new Vector2(-.20f,.26f),new Vector2(-.20f,.345f),new Vector2(-.17f,.39f),
                              new Vector2(.17f,.39f),new Vector2(.20f,.345f),new Vector2(.20f,.26f)};
            for(int k=0;k<sides;k++)
            {
                float a0=k*Mathf.PI*2/sides,a1=(k+1)*Mathf.PI*2/sides;
                for(int i=0;i<profile.Length-1;i++)
                    b.Quad(Axle(profile[i],a0),Axle(profile[i],a1),Axle(profile[i+1],a1),Axle(profile[i+1],a0),Carbon);
                b.Quad(Axle(new Vector2(-.20f,.26f),a0),Axle(new Vector2(.20f,.26f),a0),
                       Axle(new Vector2(.20f,.26f),a1),Axle(new Vector2(-.20f,.26f),a1),Metal);
            }
        }

        static Vector3 Axle(Vector2 xr,float angle)=>new Vector3(xr.x,xr.y*Mathf.Cos(angle),xr.y*Mathf.Sin(angle));

        static void BuildRim(CarMesh b,float s)
        {
            // Recessed dark wheel faces, five raised ivory spokes on the outer side and a metal nut.
            float x0=s*.17f;const int n=18;
            var outer=new Vector3[n];var inner=new Vector3[n];
            for(int k=0;k<n;k++)
            {
                float a=k*Mathf.PI*2/n;
                outer[k]=new Vector3(x0,.26f*Mathf.Cos(a),.26f*Mathf.Sin(a));
                inner[k]=new Vector3(-x0,outer[k].y,outer[k].z);
            }
            b.Poly(outer,Carbon);b.Poly(inner,Carbon);
            for(int k=0;k<5;k++)
            {
                float a=k*Mathf.PI*2/5;
                var d=new Vector3(0,Mathf.Cos(a),Mathf.Sin(a));var t=new Vector3(0,-Mathf.Sin(a),Mathf.Cos(a));
                var q=new[]{d*.05f-t*.025f,d*.25f-t*.02f,d*.25f+t*.02f,d*.05f+t*.025f};
                b.Prism(q,x0,x0+s*.02f,Ivory);
            }
            var hex=new Vector3[6];
            for(int k=0;k<6;k++)hex[k]=new Vector3(0,.05f*Mathf.Cos(k*Mathf.PI/3),.05f*Mathf.Sin(k*Mathf.PI/3));
            b.Prism(hex,x0,x0+s*.04f,Metal);
        }

        static List<(float,Vector2[])> Sections(float[,] rows)
        {
            var list=new List<(float,Vector2[])>();
            for(int i=0;i<rows.GetLength(0);i++)list.Add((rows[i,0],NoseRing(rows[i,1],rows[i,2],rows[i,3],rows[i,4])));
            return list;
        }

        // Pairs of (z, y) values -> profile points.
        static Vector2[] ZY(params float[] zy)
        {
            var points=new Vector2[zy.Length/2];
            for(int i=0;i<points.Length;i++)points[i]=new Vector2(zy[i*2],zy[i*2+1]);
            return points;
        }

        static void EnsureFolder()
        {
            if(AssetDatabase.IsValidFolder(CarPath))return;
            if(!AssetDatabase.IsValidFolder("Assets/PolygonRacing/Generated"))AssetDatabase.CreateFolder("Assets/PolygonRacing","Generated");
            AssetDatabase.CreateFolder("Assets/PolygonRacing/Generated","Car");
        }

        // Small multi-material mesh builder. Materials are double-sided and the flat shader
        // derives facet normals, so triangle winding does not matter.
        sealed class CarMesh
        {
            readonly List<Vector3> vertices=new List<Vector3>();
            readonly List<int>[] indices;
            public CarMesh(){indices=new List<int>[SlotCount];for(int i=0;i<SlotCount;i++)indices[i]=new List<int>();}

            public void Tri(Vector3 a,Vector3 b,Vector3 c,int m)
            {
                int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
                indices[m].Add(n);indices[m].Add(n+1);indices[m].Add(n+2);
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int m){Tri(a,b,c,m);Tri(a,c,d,m);}
            public void Poly(IList<Vector3> points,int m){for(int i=1;i<points.Count-1;i++)Tri(points[0],points[i],points[i+1],m);}

            // Rings of (x, y) points at increasing or decreasing z; mats[i] colours segment i -> i+1.
            public void LoftZ(List<(float z,Vector2[] ring)> sections,int[] mats,int capFirst,int capLast,bool closed)
            {
                int count=sections[0].ring.Length,segments=closed?count:count-1;
                for(int r=0;r<sections.Count-1;r++)
                {
                    var (za,ra)=sections[r];var (zb,rb)=sections[r+1];
                    for(int i=0;i<segments;i++)
                    {
                        int j=(i+1)%count;
                        Quad(P(ra[i],za),P(ra[j],za),P(rb[j],zb),P(rb[i],zb),mats[i]);
                    }
                }
                if(capFirst>=0)Cap(sections[0],capFirst);
                if(capLast>=0)Cap(sections[sections.Count-1],capLast);
            }
            void Cap((float z,Vector2[] ring) section,int m)
            {
                var points=new Vector3[section.ring.Length];
                for(int i=0;i<points.Length;i++)points[i]=P(section.ring[i],section.z);
                Poly(points,m);
            }
            static Vector3 P(Vector2 xy,float z)=>new Vector3(xy.x,xy.y,z);

            // 2D profile in (z, y) extruded across x0..x1, with end caps.
            public void ExtrudeX(Vector2[] zy,float x0,float x1,int m)
            {
                var a=new Vector3[zy.Length];var b=new Vector3[zy.Length];
                for(int i=0;i<zy.Length;i++){a[i]=new Vector3(x0,zy[i].y,zy[i].x);b[i]=new Vector3(x1,zy[i].y,zy[i].x);}
                for(int i=0;i<zy.Length;i++){int j=(i+1)%zy.Length;Quad(a[i],a[j],b[j],b[i],m);}
                Poly(a,m);Poly(b,m);
            }

            // Polygon given in the YZ plane (x ignored), extruded along x from x0 to x1.
            public void Prism(Vector3[] yz,float x0,float x1,int m)
            {
                var a=new Vector3[yz.Length];var b=new Vector3[yz.Length];
                for(int i=0;i<yz.Length;i++){a[i]=new Vector3(x0,yz[i].y,yz[i].z);b[i]=new Vector3(x1,yz[i].y,yz[i].z);}
                for(int i=0;i<yz.Length;i++){int j=(i+1)%yz.Length;Quad(a[i],a[j],b[j],b[i],m);}
                Poly(b,m);
            }

            // Axis-aligned box; the face toward -z (the driver) can use its own material.
            public void Box(Vector3 c,Vector3 size,int m,int back=-1)
            {
                var h=size*.5f;
                Vector3 V(float x,float y,float z)=>new Vector3(c.x+x*h.x,c.y+y*h.y,c.z+z*h.z);
                Quad(V(-1,-1,-1),V(1,-1,-1),V(1,1,-1),V(-1,1,-1),back>=0?back:m);
                Quad(V(-1,-1,1),V(-1,1,1),V(1,1,1),V(1,-1,1),m);
                Quad(V(-1,1,-1),V(1,1,-1),V(1,1,1),V(-1,1,1),m);
                Quad(V(-1,-1,-1),V(-1,-1,1),V(1,-1,1),V(1,-1,-1),m);
                Quad(V(-1,-1,-1),V(-1,1,-1),V(-1,1,1),V(-1,-1,1),m);
                Quad(V(1,-1,-1),V(1,-1,1),V(1,1,1),V(1,1,-1),m);
            }

            // Thin square tube between two points.
            public void Tube(Vector3 from,Vector3 to,float radius,int m,int sides=4)
            {
                var d=(to-from).normalized;
                var up=Mathf.Abs(d.y)<.9f?Vector3.up:Vector3.right;
                var u=Vector3.Cross(d,up).normalized;var v=Vector3.Cross(u,d);
                var a=new Vector3[sides];var b=new Vector3[sides];
                for(int k=0;k<sides;k++)
                {
                    float angle=k*Mathf.PI*2/sides+Mathf.PI/sides;
                    var offset=(u*Mathf.Cos(angle)+v*Mathf.Sin(angle))*radius;
                    a[k]=from+offset;b[k]=to+offset;
                }
                for(int k=0;k<sides;k++){int j=(k+1)%sides;Quad(a[k],a[j],b[j],b[k],m);}
            }

            public void Fill(Mesh mesh)
            {
                mesh.Clear();
                if(vertices.Count>65000)mesh.indexFormat=IndexFormat.UInt32;
                mesh.SetVertices(vertices);
                mesh.subMeshCount=SlotCount;
                for(int i=0;i<SlotCount;i++)mesh.SetTriangles(indices[i],i);
                mesh.RecalculateNormals();mesh.RecalculateBounds();
            }
        }
    }
}
#endif
