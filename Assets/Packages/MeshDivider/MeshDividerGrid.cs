using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using System;

namespace MeshDivider
{

    public static class MeshDividerGrid
    {
        class VtxData
        {
            public Mesh mesh;
            public int idx;

            public Vector3 vtx { get { return meshData.vertices[idx]; } }
            public Vector2 uv  { get { return meshData.uvs[idx]; } }

            public override int GetHashCode()
            {
                return mesh.GetHashCode() ^ idx.GetHashCode();
            }

            #region private
            class MeshData
            {
                public Vector3[] vertices;
                public Vector2[] uvs; 
            }

            static Dictionary<Mesh, MeshData> _meshToVtxs = new Dictionary<Mesh, MeshData>();

            MeshData meshData
            {
                get
                {
                    MeshData md;
                    if (!_meshToVtxs.TryGetValue(mesh, out md))
                    {
                        _meshToVtxs[mesh] = md = new MeshData()
                        {
                            vertices = mesh.vertices,
                            uvs = mesh.uv
                        };
                    }
                    return md;
                }
            }
            #endregion
        }

        public static Bounds CalcTotalBounds(List<Mesh> _meshs)
        {
            return _meshs.Where(mesh => mesh !=null).Aggregate(default(Bounds), (b, mesh) => { b.Encapsulate(mesh.bounds); return b; });
        }

        public static Vector3 CalcGridNum(List<Mesh> _meshs, Vector3 gridSize)
        {
            var bounds = CalcTotalBounds(_meshs);
            var size = bounds.size;

            return new Vector3(
                Mathf.Floor(size.x / gridSize.x) + 1f,
                Mathf.Floor(size.y / gridSize.y) + 1f,
                Mathf.Floor(size.z / gridSize.z) + 1f
                );
        }

        public static List<Mesh> CreateDivideMesh(List<Mesh> _meshs, Vector3 gridSize)
        {
            Assert.IsTrue(_meshs.SelectMany(mesh => Enumerable.Range(0, mesh.subMeshCount).Select((i) => mesh.GetTopology(i))).All(topology => topology == MeshTopology.Triangles), "MeshDivederGrid allows Only MeshTopology.Triangels.");

            var bounds = CalcTotalBounds(_meshs);
            var gridNum = CalcGridNum(_meshs, gridSize);

            Func<Vector3, int> PosToId = (pos_orig) =>
            {
                var pos = pos_orig - bounds.min;
                return (int)(Mathf.Floor(pos.x / gridSize.x) * gridNum.y * gridNum.z
                + Mathf.Floor(pos.y / gridSize.y) * gridNum.z
                + Mathf.Floor(pos.z / gridSize.z));
            };

            var gridTotalNum = (int)(gridNum.x * gridNum.y * gridNum.z);


            var gridDatas = new List<VtxData>[gridTotalNum];
            {
                for (var i = 0; i < gridTotalNum; ++i) gridDatas[i] = new List<VtxData>();

                _meshs.ForEach(mesh =>
                {
                    for (var subMeshIdx = 0; subMeshIdx < mesh.subMeshCount; ++subMeshIdx)
                    {
                        var vtxs = mesh.vertices;
                        var orig_idxs = mesh.GetIndices(subMeshIdx);
                        for (var i = 0; i < orig_idxs.Length; i += 3)
                        {
                            var idx = orig_idxs[i];
                            var vtx0 = vtxs[idx];

                            var id = PosToId(vtx0); // vtx0が属してるgridに三角形を含める

                            var gridData = gridDatas[id];

                            gridData.Add(new VtxData() { mesh = mesh, idx = orig_idxs[i] });
                            gridData.Add(new VtxData() { mesh = mesh, idx = orig_idxs[i + 1] });
                            gridData.Add(new VtxData() { mesh = mesh, idx = orig_idxs[i + 2] });

                        }
                    }
                });
            }

            return gridDatas
                .Where((gridData) => gridData.Any())
                .Select(gridData =>
                {
                    var vtxs = new List<Vector3>();
                    var uvs = new List<Vector2>();

                    var idxs = new List<int>();
                    var posToIdx = new Dictionary<VtxData, int>();

                    for (var i = 0; i < gridData.Count; ++i)
                    {
                        var vtxData = gridData[i];
                        int idx = -1;
                        if ( !posToIdx.TryGetValue(vtxData, out idx) )
                        {
                            vtxs.Add(vtxData.vtx);
                            uvs.Add(vtxData.uv);

                            idx = vtxs.Count - 1;

                            posToIdx[vtxData] = idx;
                        }

                        idxs.Add(idx);
                    }


                    var mesh = new Mesh();
                    mesh.vertices = vtxs.ToArray();
                    mesh.uv = uvs.ToArray();
                    mesh.SetIndices(idxs.ToArray(), MeshTopology.Triangles, 0, true);

                    return mesh;
                })
                .ToList();
        }
    }
}