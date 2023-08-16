using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Image = UnityEngine.UI.Image;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class ReplaceMaterials : EditorWindow
{
    [MenuItem("TechArtTool/Replace Materials")]
    static void Init()
    {
        ReplaceMaterials window = (ReplaceMaterials)GetWindow(typeof(ReplaceMaterials));
        window.Show();
        window.position = new Rect(200, 800, 1000, 500);
    }

    List<string> listPrefabs, listScenes;
    bool recursionVal;
    int editorMode;
    Material targetMaterial, changedMaterial;
    string[] modes = new string[] { "Prefabs", "Scene" };

    Vector2 scroll, scroll1;

    void OnGUI()
    {
        GUILayout.Label(position + "");
        GUILayout.Space(2);
        int oldValue = GUI.skin.window.padding.bottom;
        GUI.skin.window.padding.bottom = -20;
        Rect windowRect = GUILayoutUtility.GetRect(1, 17);
        windowRect.x += 4;
        windowRect.width -= 7;
        GUI.skin.window.padding.bottom = oldValue;

        recursionVal = GUILayout.Toggle(recursionVal, "Search all dependencies");
        //recursionVal = false;

        GUILayout.Space(2);
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Label("Target material");
        targetMaterial = (Material)EditorGUILayout.ObjectField(targetMaterial, typeof(Material), false);
        GUILayout.EndVertical();
                                   
        GUILayout.BeginVertical();
        GUILayout.Label("Changed material");
        changedMaterial = (Material)EditorGUILayout.ObjectField(changedMaterial, typeof(Material), false);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUI.enabled = true;

        if (GUILayout.Button("Find material usage") && targetMaterial != null)
        {
            //AssetDatabase.SaveAssets();

            listPrefabs = GetRefPrefabs(targetMaterial);
            listScenes = GetRefScenes(targetMaterial);

            listPrefabs.Sort(SortAlphabetically);
            listScenes.Sort(SortAlphabetically);
            
        }
        
        if(listPrefabs != null && listPrefabs.Count == 0)
        {
            editorMode = 1;
        } else if (listScenes != null && listScenes.Count == 0)
        {
            editorMode= 0;
        } else
        {
            editorMode = GUI.SelectionGrid(GUILayoutUtility.GetRect(2, 20), editorMode, modes, 2, "Button");
        }

        switch (editorMode)
        {
            case 0:
                if (listPrefabs != null && targetMaterial != null)
                {
                    if (listPrefabs.Count == 0)
                    {
                        GUILayout.Label(targetMaterial.name == "" ? "Choose a material" : "No prefabs use material " + targetMaterial.name);
                    }
                    else
                    {
                        GUILayout.Label("The following " + listPrefabs.Count + " prefabs use material " + targetMaterial.name + ":");
                        if (changedMaterial != null && listPrefabs.Count > 0)
                        {
                            if (GUILayout.Button("Change all from " + targetMaterial.name + " to " + changedMaterial.name, GUILayout.Width(position.width / 3)))
                            {
                                foreach (string path in listPrefabs)
                                {
                                    var prefab = PrefabUtility.LoadPrefabContents(path);
                                    SpriteRenderer[] srList = prefab.GetComponentsInChildren<SpriteRenderer>(true).Where(a => a.sharedMaterial == targetMaterial).ToArray();
                                    ParticleSystemRenderer[] psList = prefab.GetComponentsInChildren<ParticleSystemRenderer>(true).Where(a => a.sharedMaterial == targetMaterial).ToArray();
                                    Image[] imList = prefab.GetComponentsInChildren<Image>(true).Where(a => a.material == targetMaterial).ToArray();
                                    if (srList.Length > 0)
                                    {
                                        foreach (SpriteRenderer sR in srList)
                                        {
                                            if (sR.sharedMaterial.name == targetMaterial.name)
                                            {
                                                sR.sharedMaterial = changedMaterial;
                                            }
                                        }
                                    }
                                    if (psList.Length > 0)
                                    {
                                        foreach (ParticleSystemRenderer pS in psList)
                                        {
                                            if (pS.sharedMaterial == targetMaterial)
                                            {
                                                pS.sharedMaterial = changedMaterial;
                                            }
                                        }
                                    }
                                    if (imList.Length > 0)
                                    {
                                        foreach (ParticleSystemRenderer pS in psList)
                                        {
                                            if (pS.sharedMaterial == targetMaterial)
                                            {
                                                pS.sharedMaterial = changedMaterial;
                                            }
                                        }
                                    }
                                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                                    PrefabUtility.UnloadPrefabContents(prefab);
                                }

                            }
                        }

                        scroll = GUILayout.BeginScrollView(scroll);
                        foreach (string path in listPrefabs)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(path, GUILayout.Width(position.width / 2f));

                            GameObject go = (GameObject)AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
                            int srCount, psCount, imCount;

                            srCount = go.GetComponentsInChildren<SpriteRenderer>(true).Where(a => a.sharedMaterial == targetMaterial).Count();
                            psCount = go.GetComponentsInChildren<ParticleSystemRenderer>(true).Where(a => a.sharedMaterial == targetMaterial).Count();
                            imCount = go.GetComponentsInChildren<Image>(true).Where(a => a.material == targetMaterial).Count();


                            if (srCount > 0)
                            {
                                if (GUILayout.Button("Select Renderer", GUILayout.Width(position.width / 12)))
                                {
                                    AssetDatabase.OpenAsset(go);

                                    SpriteRenderer[] srList = go.GetComponentsInChildren<SpriteRenderer>(true).Where(a => a.sharedMaterial == targetMaterial).ToArray();
                                    Selection.objects = srList;
                                    Debug.Log("Prefab: " + path.Split("/").Last() + " || Number Influence Sprites: " + srList.Length);
                                    foreach (var sr in srList)
                                    {
                                        Debug.Log("Sprite.name: " + sr.name);
                                        Debug.Log("Sprite.material: " + sr.sharedMaterial.name);
                                    }
                                }
                                if (changedMaterial != null)
                                {
                                    if (GUILayout.Button("Change Renderer", GUILayout.Width(position.width / 12)))
                                    {
                                        var prefab = PrefabUtility.LoadPrefabContents(path);
                                        SpriteRenderer[] srList = prefab.GetComponentsInChildren<SpriteRenderer>(true).Where(a => a.sharedMaterial == targetMaterial).ToArray();
                                        foreach (SpriteRenderer sR in srList)
                                        {
                                            if (sR.sharedMaterial.name == targetMaterial.name)
                                            {
                                                sR.sharedMaterial = changedMaterial;
                                            }
                                        }
                                        PrefabUtility.SaveAsPrefabAsset(prefab, path);
                                        PrefabUtility.UnloadPrefabContents(prefab);
                                    }
                                }
                            }

                            if (psCount > 0)
                            {
                                if (GUILayout.Button("Select VFX", GUILayout.Width(position.width / 12)))
                                {
                                    AssetDatabase.OpenAsset(go);
                                    ParticleSystemRenderer[] psList = go.GetComponentsInChildren<ParticleSystemRenderer>(true).Where(a => a.sharedMaterial == targetMaterial).ToArray();
                                    Selection.objects = psList;
                                    Debug.Log("LoadMainAssetAtPath: " + path.Split("/").Last() + " || Number Influence Particles: " + psList.Length);
                                    foreach (var ps in psList)
                                    {
                                        Debug.Log("Particle.name: " + ps.name);
                                        Debug.Log("Particle.material: " + ps.sharedMaterial.name);
                                    }
                                }
                                if (changedMaterial != null)
                                {
                                    if (GUILayout.Button("Change VFX", GUILayout.Width(position.width / 12)))
                                    {
                                        var prefab = PrefabUtility.LoadPrefabContents(path);
                                        ParticleSystemRenderer[] psList = prefab.GetComponentsInChildren<ParticleSystemRenderer>(true).Where(a => a.sharedMaterial == targetMaterial).ToArray();
                                        foreach (ParticleSystemRenderer pS in psList)
                                        {
                                            if (pS.sharedMaterial == targetMaterial)
                                            {
                                                pS.sharedMaterial = changedMaterial;
                                            }
                                        }
                                        PrefabUtility.SaveAsPrefabAsset(prefab, path);
                                        PrefabUtility.UnloadPrefabContents(prefab);
                                    }
                                }
                            }

                            if (imCount > 0)
                            {
                                if (GUILayout.Button("Select Image", GUILayout.Width(position.width / 12)))
                                {
                                    AssetDatabase.OpenAsset(go);
                                    Image[] imList = go.GetComponentsInChildren<Image>(true).Where(a => a.material == targetMaterial).ToArray();
                                    Selection.objects = imList;
                                    Debug.Log("LoadMainAssetAtPath: " + path.Split("/").Last() + " || Number Influence Particles: " + imList.Length);
                                    foreach (var im in imList)
                                    {
                                        Debug.Log("Particle.name: " + im.name);
                                        Debug.Log("Particle.material: " + im.material.name);
                                    }
                                }
                                if (changedMaterial != null)
                                {
                                    if (GUILayout.Button("Change Image", GUILayout.Width(position.width / 12)))
                                    {
                                        var prefab = PrefabUtility.LoadPrefabContents(path);
                                        Image[] imList = prefab.GetComponentsInChildren<Image>(true).Where(a => a.material == targetMaterial).ToArray();
                                        foreach (var pS in imList)
                                        {
                                            if (pS.material == targetMaterial)
                                            {
                                                pS.material = changedMaterial;
                                            }
                                        }
                                        PrefabUtility.SaveAsPrefabAsset(prefab, path);
                                        PrefabUtility.UnloadPrefabContents(prefab);
                                    }
                                }
                            }

                            Debug.Log("PrefabUtility.UnloadPrefabContents(prefab)");

                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndScrollView();
                    }
                }
                AssetDatabase.SaveAssets();

                break;
            case 1:
                if (listScenes != null && listScenes.Count > 0)
                {
                    GUILayout.Label("The following " + listScenes.Count + " scenes use material " + targetMaterial.name + ":");

                    scroll1 = GUILayout.BeginScrollView(scroll1);
                    foreach (string path in listScenes)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(path, GUILayout.Width(position.width / 1.5f));
                        if (GUILayout.Button("Select", GUILayout.Width(position.width)))
                        {
                            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
                            var currentScene = EditorSceneManager.OpenScene(path);

                            if (1 > 0)
                            {
                                if (GUILayout.Button("Select Renderer", GUILayout.Width(position.width / 12)))
                                {
                                    List<GameObject> rootObjectsInScene = currentScene.GetRootGameObjects().ToList();

                                    Debug.Log("root game object size : " + rootObjectsInScene.Count);
                                    SpriteRenderer[] srList;
                                    foreach (GameObject obj in rootObjectsInScene)
                                    {
                                        srList = obj.GetComponentsInChildren<SpriteRenderer>(true).Where(a => a.sharedMaterial == targetMaterial).ToArray();
                                        Selection.objects = srList;
                                        Debug.Log("Prefab: " + path.Split("/").Last() + " || Number Influence Sprites: " + srList.Length);
                                        foreach (var sr in srList)
                                        {
                                            Debug.Log("Sprite.name: " + sr.name);
                                            Debug.Log("Sprite.material: " + sr.sharedMaterial.name);
                                        }
                                    }
                                }
                                if (changedMaterial != null)
                                {
                                    if (GUILayout.Button("Change Renderer", GUILayout.Width(position.width / 12)))
                                    {
                                        var prefab = PrefabUtility.LoadPrefabContents(path);
                                        SpriteRenderer[] srList = prefab.GetComponentsInChildren<SpriteRenderer>(true).Where(a => a.sharedMaterial == targetMaterial).ToArray();
                                        foreach (SpriteRenderer sR in srList)
                                        {
                                            if (sR.sharedMaterial.name == targetMaterial.name)
                                            {
                                                sR.sharedMaterial = changedMaterial;
                                            }
                                        }
                                        PrefabUtility.SaveAsPrefabAsset(prefab, path);
                                        PrefabUtility.UnloadPrefabContents(prefab);
                                    }
                                }
                            }

                            EditorSceneManager.CloseScene(currentScene, true);
                            //AssetDatabase.SaveAssets();

                        }

                        if (GUILayout.Button("Change"))
                        {
                            var currentScene = EditorSceneManager.OpenScene(path);

                            //var[] imList = go.GetComponentsInChildren<Image>(true).Where(a => a.material == targetMaterial).ToArray();

                            EditorSceneManager.SaveScene(currentScene);
                            EditorSceneManager.CloseScene(currentScene, true);

                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();

                } else
                {
                    GUILayout.Label("No scene here!!");
                }
                
                break;
        }
    }

    int SortAlphabetically(string a, string b)
    {
        return a.CompareTo(b);
    }

    public static string[] GetAllPrefabs()
    {
        string[] temp = AssetDatabase.GetAllAssetPaths();
        List<string> result = new();
        foreach (string s in temp)
        {
            if (s.Contains(".prefab")) result.Add(s);
        }
        return result.ToArray();
    }
    public List<string> GetRefPrefabs(Material targetMaterial)
    {
        string targetPath = AssetDatabase.GetAssetPath(targetMaterial);
        string[] allPrefabs = GetAllPrefabs();
        List<string> result = new();

        foreach (string prefab in allPrefabs)
        {
            string[] single = new string[] { prefab };
            string[] dependencies = AssetDatabase.GetDependencies(single, recursionVal);
            foreach (string dependedAsset in dependencies)
            {
                if (dependedAsset == targetPath)
                {
                    result.Add(prefab);
                }
            }
        }
        return result.ToList();
    }
    public static List<string> GetRefScenes(Material targetMaterial)
    {
        List<string> result = new();
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        var referenceCache = new Dictionary<string, List<string>>();

        string[] guids = AssetDatabase.FindAssets("");
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);

            foreach (var dependency in dependencies)
            {
                if (referenceCache.ContainsKey(dependency))
                {
                    if (!referenceCache[dependency].Contains(assetPath))
                    {
                        referenceCache[dependency].Add(assetPath);
                    }
                }
                else
                {
                    referenceCache[dependency] = new List<string>() { assetPath };
                }
            }
        }

        Debug.Log("Build index takes " + sw.ElapsedMilliseconds + " milliseconds");

        string path = AssetDatabase.GetAssetPath(targetMaterial);
        Debug.Log("Find: " + path, targetMaterial);
        if (referenceCache.ContainsKey(path))
        {
            foreach (var reference in referenceCache[path])
            {
                //Debug.Log(reference, AssetDatabase.LoadMainAssetAtPath(reference));
                if (reference.Split(".").Last() == "unity")
                {
                    result.Add(reference);
                }
            }
        }
        else
        {
            Debug.LogWarning("No references");
        }

        referenceCache.Clear();

        return result;
    }
}
