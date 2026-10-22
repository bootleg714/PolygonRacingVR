#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PolygonRacing.Editor
{
    // Creates (first run, copied from the Forest scene) or refreshes the Sakura Hills figure-eight scene.
    public static class PolygonSakuraSetup
    {
        const string ForestPath="Assets/PolygonRacing/Scenes/ForestCircuit.unity";
        const string CoastalPath="Assets/PolygonRacing/Scenes/CoastalExpressway.unity";
        public const string SakuraPath="Assets/PolygonRacing/Scenes/SakuraHills.unity";
        const string DowntownPath=PolygonDowntownSetup.DowntownPath;
        const string RivieraPath=PolygonRivieraSetup.RivieraPath;
        const string GeneratedPath="Assets/PolygonRacing/Generated/Sakura";

        [MenuItem("Polygon Racing/14 - Create or Refresh Sakura Hills")]
        public static void CreateOrRefresh()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play Mode before building the Sakura circuit.");return;}
            if(!Resources.Load<Shader>(PolygonLook.FlatShaderName))
            {
                EditorUtility.DisplayDialog("Sakura circuit","PolygonFlat.shader is missing from Assets/PolygonRacing/Resources. Nothing was changed.","OK");
                return;
            }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            bool created=false;
            if(!File.Exists(SakuraPath))
            {
                if(!File.Exists(ForestPath)){EditorUtility.DisplayDialog("Sakura circuit","ForestCircuit.unity was not found. Nothing was changed.","OK");return;}
                if(!AssetDatabase.CopyAsset(ForestPath,SakuraPath)){Debug.LogError("Could not copy the Forest Circuit scene for Sakura.");return;}
                AssetDatabase.Refresh();created=true;
            }
            else
            {
                string backup="PolygonRacingBackups/SakuraHills_before_refresh_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity";
                try{Directory.CreateDirectory("PolygonRacingBackups");File.Copy(SakuraPath,backup,false);}
                catch(System.Exception ex){Debug.LogException(ex);Debug.LogError("Could not back up the Sakura scene. Nothing was rebuilt.");return;}
            }

            var scene=EditorSceneManager.OpenScene(SakuraPath,OpenSceneMode.Single);
            var player=Object.FindFirstObjectByType<PolygonDriver>();
            if(!player||!player.transform.parent){Debug.LogError("The Sakura scene has no player car. Nothing was rebuilt.");return;}
            var root=player.transform.parent;
            var selector=root.GetComponent<PolygonCircuitSelector>();
            if(!selector)selector=root.gameObject.AddComponent<PolygonCircuitSelector>();
            selector.Layout=PolygonCircuitLayout.Sakura;EditorUtility.SetDirty(selector);
            PolygonCircuit.SetLayout(PolygonCircuitLayout.Sakura);

            try
            {
                var oldScenery=root.Find(PolygonSakuraArt.SceneryName);
                if(oldScenery)oldScenery.name="Old scenery (being replaced)";
                var oldAnimated=root.Find(PolygonSakuraArt.AnimatedName);
                if(oldAnimated)Object.DestroyImmediate(oldAnimated.gameObject);
                var timer=System.Diagnostics.Stopwatch.StartNew();
                var scenery=PolygonSakuraArt.Build(root);
                PolygonCarSetup.FillParkedCarSlots(scenery);
                if(oldScenery)Object.DestroyImmediate(oldScenery.gameObject);

                // Start the car on the grid and give the camera the valley's view distance.
                player.transform.position=PolygonWorld.Point(0)+Vector3.up*PolygonCircuit.Elevation(0);
                player.transform.rotation=Quaternion.LookRotation(PolygonWorld.Tangent(0));
                if(player.view){player.view.farClipPlane=1600;player.view.nearClipPlane=.1f;EditorUtility.SetDirty(player.view);}
                EditorUtility.SetDirty(player.transform);

                PersistGeneratedAssets(scenery,root.Find(PolygonSakuraArt.AnimatedName));
                PolygonLookSetup.ApplyToOpenScene(PolygonCircuitLayout.Sakura);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene,SakuraPath))throw new IOException("Unity could not save the Sakura scene.");
                AddBuildScenes();
                AssetDatabase.SaveAssets();
                Report(scenery,timer.Elapsed.TotalSeconds,created);
            }
            catch(System.Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("Sakura circuit build failed. Forest and Coastal scenes are unchanged.");
            }
        }

        static void Report(Transform scenery,double seconds,bool created)
        {
            long triangles=0;int renderers=0;
            foreach(var filter in scenery.GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.sharedMesh)continue;renderers++;
                for(int i=0;i<filter.sharedMesh.subMeshCount;i++)triangles+=filter.sharedMesh.GetIndexCount(i)/3;
            }
            Debug.Log($"Sakura circuit {(created?"created":"refreshed")}: {PolygonCircuit.Length:0} m lap, {renderers} scenery renderers, " +
                      $"{triangles:N0} scenery triangles, built in {seconds:0.0} s. Saved {SakuraPath} and added it to the build scenes.");
        }

        static void PersistGeneratedAssets(Transform scenery,Transform animated)
        {
            // The folder is replaced on every build (older Sakura backups lose their meshes; the
            // live scene is always complete).
            if(!AssetDatabase.IsValidFolder("Assets/PolygonRacing/Generated"))AssetDatabase.CreateFolder("Assets/PolygonRacing","Generated");
            if(AssetDatabase.IsValidFolder(GeneratedPath))AssetDatabase.DeleteAsset(GeneratedPath);
            AssetDatabase.CreateFolder("Assets/PolygonRacing/Generated","Sakura");
            var saved=new HashSet<Object>();int index=0;
            var filters=new List<MeshFilter>(scenery.GetComponentsInChildren<MeshFilter>(true));
            var renderers=new List<Renderer>(scenery.GetComponentsInChildren<Renderer>(true));
            if(animated){filters.AddRange(animated.GetComponentsInChildren<MeshFilter>(true));renderers.AddRange(animated.GetComponentsInChildren<Renderer>(true));}
            foreach(var filter in filters)
            {
                var mesh=filter.sharedMesh;
                if(!mesh||AssetDatabase.Contains(mesh)||!saved.Add(mesh))continue;
                AssetDatabase.CreateAsset(mesh,$"{GeneratedPath}/Mesh_{index++}.asset");
            }
            foreach(var renderer in renderers)
                foreach(var material in renderer.sharedMaterials)
                {
                    if(!material||AssetDatabase.Contains(material)||!saved.Add(material))continue;
                    AssetDatabase.CreateAsset(material,AssetDatabase.GenerateUniqueAssetPath($"{GeneratedPath}/{material.name}.mat"));
                }
        }

        static void AddBuildScenes()
        {
            var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach(var path in new[]{ForestPath,CoastalPath,DowntownPath,RivieraPath,SakuraPath})
            {
                if(!File.Exists(path))continue;
                bool found=false;
                foreach(var s in scenes)if(s.path==path){s.enabled=true;found=true;}
                if(!found)scenes.Add(new EditorBuildSettingsScene(path,true));
            }
            EditorBuildSettings.scenes=scenes.ToArray();
        }
    }
}
#endif
