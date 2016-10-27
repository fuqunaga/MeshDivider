using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace MeshDivider
{

    public class MeshDividerWindow : ScriptableWizard
    {
        public string _outputPath = "Assets/MeshDivider";
        public string _outputName = "MeshDivided";
        public List<Mesh> _meshs = new List<Mesh>();
        public Vector3 _gridSize = new Vector3(100f, 100f, 100f);

        [MenuItem("Custom/MeshDivider")]
        static void Open()
        {
            DisplayWizard<MeshDividerWindow>("MeshDivider");
        }

        protected override bool DrawWizardGUI()
        {
            var ret = base.DrawWizardGUI();

            var gridNum = (_meshs != null) ? MeshDividerGrid.CalcGridNum(_meshs, _gridSize) : Vector3.zero;

            var enable = GUI.enabled;
            GUI.enabled = false;
            EditorGUILayout.IntField("GridNumTotal", (int)(gridNum.x * gridNum.y * gridNum.z));
            EditorGUILayout.Vector3Field("GridNum", gridNum);
            GUI.enabled = enable;

            return ret;
        }


        void OnWizardCreate()
        {
            if (_meshs.Any())
            {
                const string MESH_FOLDER_NAME = "Mesh";

                var root = new GameObject(_outputName);
                var standardMat = GetStandardMaterial();

                var meshPath = _outputPath + "/" + MESH_FOLDER_NAME;
                CreateFolder(meshPath);

                var meshs = MeshDividerGrid.CreateDivideMesh(_meshs, _gridSize);
                foreach(var mesh in meshs)
                {
                    var go = new GameObject("Mesh");
                    var mf = go.AddComponent<MeshFilter>();
                    mf.mesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = standardMat;
                    go.transform.SetParent(root.transform);
                    
                    var meshPathUniq = AssetDatabase.GenerateUniqueAssetPath(meshPath + "/ " + _outputName + ".asset");
                    AssetDatabase.CreateAsset(mesh, meshPathUniq);
                }


                PrefabUtility.SaveAsPrefabAsset(root, _outputPath + "/" + _outputName + ".prefab");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.FocusProjectWindow();
            }
        }


        Material GetStandardMaterial()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var ret = go.GetComponent<MeshRenderer>().sharedMaterial;
            DestroyImmediate(go);
            return ret;
        }

        void CreateFolder(string path)
        {
            path.Split('/').Aggregate("", (currentPath, folderName) =>
            {
                var ret = currentPath + (string.IsNullOrEmpty(currentPath) ? "" : "/") + folderName;
                if (!AssetDatabase.IsValidFolder(ret))
                    AssetDatabase.CreateFolder(currentPath, folderName);
                return ret;
            });
        }

#if false
        void OnFocus()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        void OnLostFocus()
        {
            OnDestroy();
        }

        void OnDestroy()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        void OnSceneGUI(SceneView sceneView)
        {
            var bounds = new Bounds(Vector3.zero, Vector3.one * 10f);
            _meshs.ForEach((mesh) => {
                if (mesh)
                {
                    bounds.Encapsulate(mesh.bounds);
                    Graphics.DrawMeshNow(mesh, Vector3.zero, Quaternion.identity);
                }
            });

            var size = bounds.size;
            var gridNum = new Vector3(
                Mathf.Floor(size.x / _gridSize.x) + 1f,
                Mathf.Floor(size.y / _gridSize.y) + 1f,
                Mathf.Floor(size.z / _gridSize.z) + 1f
                );

            var isGirdNumClamped = false;
            for (var i = 0; i < 3; ++i)
            {
                // 多いと重いので適当に少なくする
                var max = 100f;
                if (gridNum[i] > max)
                {
                    gridNum[i] = Mathf.Clamp(gridNum[i], 0f, max);
                    isGirdNumClamped = true;
                }
            }

			var col = isGirdNumClamped ? Color.red : Color.white;

            for(var i=0; i<3; ++i)
            {
                var xRank = i;
                var yRank = (i + 1) % 3;
                var zRank = (i + 2) % 3;

                for(var xIdx=0; xIdx<gridNum[xRank]; ++xIdx)
                {
                    var x = xIdx * _gridSize[xRank] + bounds.min.x;
                    for (var yIdy = 0; yIdy < gridNum[yRank]; ++yIdy)
                    {
                        var y = yIdy * _gridSize[yRank] + bounds.min.y;
                        var start = new Vector3();
                        var end = new Vector3();
                        start[xRank] = end[xRank] = x;
                        start[yRank] = end[yRank] = y;
                        start[zRank] = bounds.min[zRank];
                        end[zRank]   = bounds.max[zRank];
                        Debug.DrawLine(start, end, col);
                    }
                }
            }
        }
#endif
    }
}
