using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace MeshDivider
{

    public static class MeshDividerGrid
    {
        class VtxData
        {
            public Mesh mesh;
            public int idx;

            public Vector3 vtx => meshCache.vertices[idx];
            public Vector3 normal => meshCache.normals[idx];
            public Vector2 uv => meshCache.uvs[idx];
            public Vector2 uv2 => meshCache.uv2s[idx];

            public override int GetHashCode()
            {
                return mesh.GetHashCode() ^ idx.GetHashCode();
            }

            #region MeshCache

            class MeshCache
            {
                public Vector3[] vertices;
                public Vector3[] normals;
                public Vector2[] uvs;
                public Vector2[] uv2s;
            }

            static Dictionary<Mesh, MeshCache> meshToCache = new Dictionary<Mesh, MeshCache>();

            MeshCache meshCache
            {
                get
                {
                    if (!meshToCache.TryGetValue(mesh, out var cache))
                    {
                        meshToCache[mesh] = cache = new MeshCache()
                        {
                            vertices = mesh.vertices,
                            normals = mesh.normals,
                            uvs = mesh.uv,
                            uv2s = mesh.uv2
                        };
                    }
                    return cache;
                }
            }

            #endregion
        }


        public static Bounds CalcTotalBounds(List<Mesh> _meshs)
        {
            return _meshs.Where(mesh => mesh != null).Aggregate(default(Bounds), (b, mesh) => { b.Encapsulate(mesh.bounds); return b; });
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

        public static IEnumerable<Mesh> CreateDivideMesh(List<Mesh> meshs, Vector3 gridSize)
        {
            Assert.IsTrue(meshs.SelectMany(mesh => Enumerable.Range(0, mesh.subMeshCount).Select((i) => mesh.GetTopology(i))).All(topology => topology == MeshTopology.Triangles), "MeshDivederGrid allows Only MeshTopology.Triangels.");

            var bounds = CalcTotalBounds(meshs);
            var gridNum = CalcGridNum(meshs, gridSize);

            Func<Vector3, int> PosToId = (pos_orig) =>
            {
                var pos = pos_orig - bounds.min;
                return (int)(Mathf.Floor(pos.x / gridSize.x) * gridNum.y * gridNum.z
                + Mathf.Floor(pos.y / gridSize.y) * gridNum.z
                + Mathf.Floor(pos.z / gridSize.z));
            };

            var gridTotalNum = (int)(gridNum.x * gridNum.y * gridNum.z);


            var gridDatas = new List<VtxData>[gridTotalNum];
            for (var i = 0; i < gridTotalNum; ++i) gridDatas[i] = new List<VtxData>();

            meshs.ForEach(mesh =>
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

            var hasNormal = meshs.All(m => m.normals.Any());
            var hasUV = meshs.All(m => m.uv.Any());
            var hasUV2 = meshs.All(m => m.uv2.Any());

            return gridDatas
                .Where((gridData) => gridData.Any())
                .Select(gridData =>
                {
                    var vtxs = new List<Vector3>();
                    var normals = new List<Vector3>();
                    var uvs = new List<Vector2>();
                    var uv2s = new List<Vector2>();


                    var idxs = new List<int>();
                    var posToIdx = new Dictionary<VtxData, int>();

                    for (var i = 0; i < gridData.Count; ++i)
                    {
                        var vtxData = gridData[i];
                        int idx = -1;
                        if (!posToIdx.TryGetValue(vtxData, out idx))
                        {
                            vtxs.Add(vtxData.vtx);
                            if (hasNormal) normals.Add(vtxData.normal);
                            if (hasUV) uvs.Add(vtxData.uv);
                            if (hasUV2) uv2s.Add(vtxData.uv2);

                            idx = vtxs.Count - 1;

                            posToIdx[vtxData] = idx;
                        }

                        idxs.Add(idx);
                    }


                    var mesh = new Mesh();
                    mesh.vertices = vtxs.ToArray();
                    if (hasNormal) mesh.normals = normals.ToArray();
                    if ( hasUV ) mesh.uv = uvs.ToArray();
                    if (hasUV2) mesh.uv2 = uv2s.ToArray();
                    mesh.SetIndices(idxs.ToArray(), MeshTopology.Triangles, 0, true);

                    return mesh;
                });
        }
    }
}