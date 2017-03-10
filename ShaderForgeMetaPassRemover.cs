using System.Collections.Generic;
using System.IO;
using Global;
using Localisation;
using ShaderForge;
using UnityEditor;
using UnityEngine;

namespace Helpers
{
    [InitializeOnLoad]
    class ShaderForgeMetaPassRemover
    {
        static ShaderForgeMetaPassRemover()
        {
            OnAssetPostProcess.OnAssetImported += OnAssetImported;
        }

        private static void OnAssetImported(string filePath)
        {
            if (ShaderForgeMetaPassRemoverUtility.IsShaderForge(filePath))
            {

                string guid = AssetDatabase.AssetPathToGUID(filePath);
                if (ShaderForgeMetaPassRemoverUtility.IsMetaPassStrippingEnabled(guid))
                {
                    Debug.Log(filePath);
                    string shaderText = File.ReadAllText(filePath);
                    int lastPass = shaderText.LastIndexOf("Pass {");

                    string lassPassText = shaderText.Substring(lastPass);
                    if (lassPassText.Contains("Name \"Meta\""))
                    {
                        int check = shaderText.IndexOf("{", lastPass) + 1;
                        int openBracketCount = 1;
                        while (openBracketCount > 0)
                        {
                            int openBracket = shaderText.IndexOf("{", check);
                            if (openBracket >= 0)
                            {
                                openBracketCount++;
                                check = openBracket + 1;
                            }

                            int closedBracketPosition = shaderText.IndexOf("}", check);
                            if (closedBracketPosition >= 0)
                            {
                                check = closedBracketPosition + 1;
                            }
                            openBracketCount--;
                        }
                        string firstPart = shaderText.Substring(0, lastPass);
                        string lastPart = shaderText.Substring(check);
                        File.WriteAllText(filePath, firstPart + lastPart);
                        AssetDatabase.Refresh();
                    }
                }
                
            }
        }
    }

    internal class ShaderForgeMetaPassRemoverWindow:EditorWindow
    {
        [MenuItem("Window/ShaderForge Meta Remover")]
        public static void ShowWindow()
        {
            GetWindow(typeof(ShaderForgeMetaPassRemoverWindow),false,"Shader Forge Meta Pass Remover Settings");
        }

        private ShaderForgeObject[] shaderForgeObjects;

        private void OnEnable()
        {
         
            string[] guids   = AssetDatabase.FindAssets("t:Shader");
            //Shader[] shaders=Resources.FindObjectsOfTypeAll<Shader>();
            List<ShaderForgeObject> validShaderForge=new List<ShaderForgeObject>();
            for (int i = 0; i < guids.Length; i++)
            {
                string guid = guids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                

                if (ShaderForgeMetaPassRemoverUtility.IsShaderForge(path))
                {
                    
                    validShaderForge.Add(new ShaderForgeObject()
                    {
                        Shader = shader,
                        GUID = guid,
                        IsEnabled = ShaderForgeMetaPassRemoverUtility.IsMetaPassStrippingEnabled(guid)
                    });
                }
            }
            validShaderForge.Sort(SortObjects);
            shaderForgeObjects = validShaderForge.ToArray();
        }

        private int SortObjects(ShaderForgeObject a, ShaderForgeObject b)
        {
            if (a.IsEnabled&&!b.IsEnabled)
                return -1;
            else if (!a.IsEnabled && b.IsEnabled)
                return 1;
            return 0;
        }

        private void OnGUI()
        {
            GUILayoutOption nameWidth = GUILayout.Width(position.width * .55f);
            GUILayoutOption toggleWidth = GUILayout.Width(120);
            GUILayoutOption buttonWidth = GUILayout.Width(position.width * .45f - 120);


            GUILayout.BeginHorizontal();
            GUILayout.Label("Shader name", nameWidth);
            GUILayout.Label("Stripping Enabled");
            GUILayout.EndHorizontal();
            for (int i = 0; i < shaderForgeObjects.Length; i++)
            {
                ShaderForgeObject shaderForgeObject = shaderForgeObjects[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label(shaderForgeObject.Shader.name, nameWidth);
                bool currentToggle = GUILayout.Toggle(shaderForgeObject.IsEnabled,string.Empty,toggleWidth);
                if (currentToggle != shaderForgeObject.IsEnabled)
                {
                    shaderForgeObject.IsEnabled = currentToggle;
                    ShaderForgeMetaPassRemoverUtility.SetMetaPassStripping(shaderForgeObject);
                }
                if(GUILayout.Button("Open", buttonWidth))
                {
                    SF_Editor.Init(shaderForgeObject.Shader);
                }
                GUILayout.EndHorizontal();
            }
        }
    }

    internal class ShaderForgeObject
    {
        public Shader Shader;
        public bool IsEnabled;
        public string GUID;
    }
    internal class ShaderForgeMetaPassRemoverUtility
    {
        public static bool IsShaderForge(string path)
        {
            //file extension check
            string lowerCasePath=path.ToLower();
            if (lowerCasePath.Substring(lowerCasePath.Length - 7) == ".shader")
            {
                //check if shader forge file by reading it's meta data
                string shaderText = File.ReadAllText(path);
                return shaderText.Contains("// Shader created with Shader Forge");

            }
            return false;
        }

        
        public static bool IsShaderForge(Shader shader)
        {
            return IsShaderForge(AssetDatabase.GetAssetPath(shader));
        }


        public static bool IsMetaPassStrippingEnabled(string guid)
        {
            return EditorPrefs.GetBool(string.Format("meta_pass_{0}", guid),true);
        }

        public static void SetMetaPassStripping(ShaderForgeObject obj)
        {
            SetMetaPassStripping(obj.GUID,obj.IsEnabled);
        }
        public static void SetMetaPassStripping(string guid,bool isEnabled)
        {
            EditorPrefs.SetBool(string.Format("meta_pass_{0}", guid),isEnabled);
        }
    }
}
