#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PolygonRacing.Editor
{
    // Scenery pass v01 for the Forest Circuit: swaps the old box-and-cone pines for tiered
    // faceted pines (three variants) and the pyramid hills for rounded faceted mountains.
    // Positions, heights and materials are read from the existing objects; the route,
    // car and gameplay are untouched. (Coastal models update through menu 7.)
    public static class PolygonSceneryModelsSetup
    {
        const string ForestPath="Assets/PolygonRacing/Scenes/ForestCircuit.unity";
        const string MeshPath="Assets/PolygonRacing/Generated/Scenery";

        [MenuItem("Polygon Racing/11 - Refresh Forest Scenery Models")]
        static void RefreshForest()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play Mode before refreshing scenery models.");return;}
            if(!File.Exists(ForestPath)){EditorUtility.DisplayDialog("Forest scenery","ForestCircuit.unity was not found. Nothing was changed.","OK");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene(ForestPath,OpenSceneMode.Single);
            string backup="PolygonRacingBackups/ForestCircuit_before_scenery_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity";
            try{Directory.CreateDirectory("PolygonRacingBackups");File.Copy(ForestPath,backup,false);}
            catch(System.Exception ex){Debug.LogException(ex);Debug.LogError("Could not back up the Forest Circuit. It was not changed.");return;}

            EnsureFolder();
            var trunk=Save(PolygonSceneryMeshes.PineTrunk(),"Pine trunk");
            var crowns=new Mesh[PolygonSceneryMeshes.PineVariants.Length];
            for(int v=0;v<crowns.Length;v++)crowns[v]=Save(PolygonSceneryMeshes.PineCrown(v),"Pine crown "+v);
            var mountains=new[]{Save(PolygonSceneryMeshes.Mountain(40),"Mountain 0"),Save(PolygonSceneryMeshes.Mountain(41),"Mountain 1"),
                                Save(PolygonSceneryMeshes.Mountain(42),"Mountain 2")};

            int pines=0,hills=0;
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!t)continue;
                if(t.name=="Polygon pine"&&RebuildPine(t,trunk,crowns,pines))pines++;
                else if(t.name=="Distant faceted hill"&&RebuildHill(t,mountains,hills))hills++;
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)){Debug.LogError("Unity could not save the Forest Circuit.");return;}
            Debug.Log($"Forest scenery refreshed: {pines} pines and {hills} mountains. Backup: {backup}");
        }

        static bool RebuildPine(Transform pine,Mesh trunkMesh,Mesh[] crowns,int index)
        {
            // Variant comes from the position so a re-run picks the same one.
            var at=pine.position;
            int variant=Mathf.Abs(Mathf.RoundToInt(at.x*3.1f+at.z*1.7f))%crowns.Length;
            float variantScale=PolygonSceneryMeshes.PineVariants[variant].x;
            var oldTrunk=pine.Find("Trunk");var oldCrown=pine.Find("Crown");
            float height=0;Material bark=null,needles=null;
            if(oldTrunk){height=oldTrunk.localScale.y/.3f;bark=MaterialOf(oldTrunk);}
            if(oldCrown)needles=MaterialOf(oldCrown);
            // Already rebuilt: keep the stored height and materials.
            var newTrunk=pine.Find("Pine trunk");var newCrown=pine.Find("Pine crown");
            if(height<=0&&newTrunk){height=newTrunk.localScale.y/variantScale;bark=MaterialOf(newTrunk);}
            if(!needles&&newCrown)needles=MaterialOf(newCrown);
            if(height<=0||!bark||!needles)return false;

            for(int i=pine.childCount-1;i>=0;i--)Object.DestroyImmediate(pine.GetChild(i).gameObject);
            float scale=height*variantScale;
            pine.localRotation=Quaternion.Euler(0,Mathf.Abs(Mathf.RoundToInt(at.x*7+at.z*11))%360,0);
            Child(pine,"Pine trunk",trunkMesh,bark,scale);
            Child(pine,"Pine crown",crowns[variant],needles,scale);
            return true;
        }

        static bool RebuildHill(Transform hill,Mesh[] mountains,int index)
        {
            var old=hill.Find("Hill mesh");var current=hill.Find("Faceted mountain");
            float height;Material material;
            if(old)
            {
                var filter=old.GetComponent<MeshFilter>();material=MaterialOf(old);
                if(!filter||!filter.sharedMesh)return false;
                height=filter.sharedMesh.bounds.max.y;
            }
            else if(current)
            {
                height=current.localScale.y;material=MaterialOf(current);
            }
            else return false;
            if(!material||height<=0)return false;
            for(int i=hill.childCount-1;i>=0;i--)Object.DestroyImmediate(hill.GetChild(i).gameObject);
            var go=Child(hill,"Faceted mountain",mountains[index%mountains.Length],material,1);
            go.transform.localScale=new Vector3(95,height,95);
            go.transform.localRotation=Quaternion.Euler(0,index*37,0);
            return true;
        }

        static Material MaterialOf(Transform t)
        {
            var renderer=t?t.GetComponent<Renderer>():null;
            return renderer?renderer.sharedMaterial:null;
        }

        static GameObject Child(Transform parent,string name,Mesh mesh,Material material,float scale)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(parent,false);go.transform.localScale=Vector3.one*scale;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return go;
        }

        static Mesh Save(Mesh mesh,string name)
        {
            string path=$"{MeshPath}/{name}.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing)
            {
                existing.Clear();
                existing.SetVertices(mesh.vertices);existing.SetTriangles(mesh.triangles,0);
                existing.RecalculateNormals();existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(mesh);
                return existing;
            }
            mesh.name=name;AssetDatabase.CreateAsset(mesh,path);return mesh;
        }

        static void EnsureFolder()
        {
            if(AssetDatabase.IsValidFolder(MeshPath))return;
            if(!AssetDatabase.IsValidFolder("Assets/PolygonRacing/Generated"))AssetDatabase.CreateFolder("Assets/PolygonRacing","Generated");
            AssetDatabase.CreateFolder("Assets/PolygonRacing/Generated","Scenery");
        }
    }
}
#endif
