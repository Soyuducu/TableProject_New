// Copyright (c) 2026 Enigma 23. All rights reserved.

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace e23.TrainController
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SplineMeshRepeater : MonoBehaviour
    {
        [SerializeField] protected string _visualName = "RenameMe";
        [SerializeField] protected SplineContainer _splineContainer;
        [SerializeField] protected Mesh _sourceMesh;
        [SerializeField] protected bool _rebuildOnEnable = false;
        [SerializeField] protected bool _autoCalculateCount = true;
        [SerializeField] protected int _repeatCount = 10;
        [SerializeField][Range(1, 32)] protected int _samplesPerTile = 8;
#if UNITY_EDITOR
        [SerializeField] protected ModelImporterMeshCompression _meshCompression = ModelImporterMeshCompression.Medium;
#endif
        [SerializeField] protected Mesh _cachedMesh;
        [SerializeField] protected float _meshScale = 1f;

        protected MeshFilter _meshFilter;

        public virtual void AssignSplineContainer(SplineContainer splineContainer) => _splineContainer = splineContainer;

        protected virtual void OnEnable()
        {
            GetRequiredComponents();

            if (_cachedMesh != null)
            {
                _meshFilter.sharedMesh = _cachedMesh;
                return;
            }

            if (_rebuildOnEnable == true) { Rebuild(); }
        }

        protected virtual void GetRequiredComponents() => _meshFilter = GetComponent<MeshFilter>();

        [ContextMenu("Rebuild")]
        public virtual void Rebuild()
        {
            if (_splineContainer == null || _sourceMesh == null) { return; }
            if (_meshFilter == null) { GetRequiredComponents(); }

            var mesh = BuildMesh();

#if UNITY_EDITOR
            SaveMeshAsset(mesh);
#else
            _meshFilter.sharedMesh = mesh;
#endif
        }

        protected virtual Mesh BuildMesh()
        {
            var allVertices = new List<Vector3>();
            var allNormals = new List<Vector3>();
            var allUVs = new List<Vector2>();
            var allTriangles = new List<int>();

            foreach (var spline in _splineContainer.Splines)
            {
                BuildSplineMesh(spline, _splineContainer.transform, allVertices, allNormals, allUVs, allTriangles);
            }

            var mesh = new Mesh { name = $"{_visualName}_SplineMesh" };
            if (allVertices.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(allVertices);
            mesh.SetNormals(allNormals);
            mesh.SetUVs(0, allUVs);
            mesh.SetTriangles(allTriangles, 0);
            mesh.RecalculateBounds();

            WeldVertices(mesh);

            return mesh;
        }

        protected virtual void BuildSplineMesh(Spline spline, Transform containerTransform, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles)
        {
            float meshLength = _sourceMesh.bounds.size.z * _meshScale;
            float meshMin = _sourceMesh.bounds.min.z * _meshScale;
            float meshMax = _sourceMesh.bounds.max.z * _meshScale;
            float splineLength = SplineUtility.CalculateLength(spline, containerTransform.localToWorldMatrix);

            int tileCount = _autoCalculateCount ? Mathf.Max(1, Mathf.RoundToInt(splineLength / meshLength)) : _repeatCount;
            
            var sourceVerts = _sourceMesh.vertices;
            var sourceNormals = _sourceMesh.normals;
            var sourceUVs = _sourceMesh.uv;
            var sourceTris = _sourceMesh.triangles;
            int vertCount = sourceVerts.Length;

            int totalSamples = tileCount * _samplesPerTile + 1;
            var sampleFrames = new SplineFrame[totalSamples];

            for (int s = 0; s < totalSamples; s++)
            {
                float t = (float)s / (totalSamples - 1);
                t = Mathf.Clamp(t, 0f, spline.Closed ? 1f : 0.9999f);

                SplineUtility.Evaluate
                (
                    spline, 
                    t,
                    out float3 pos,
                    out float3 tan,
                    out float3 up
                );

                GetFrameLocal(pos, tan, up, out Vector3 lPos, out Vector3 lFwd, out Vector3 lUp, out Vector3 lRight);

                sampleFrames[s] = new SplineFrame(lPos, lFwd, lUp, lRight);
            }

            for (int tile = 0; tile < tileCount; tile++)
            {
                int baseIndex = vertices.Count;

                for (int v = 0; v < vertCount; v++)
                {
                    Vector3 src = sourceVerts[v] * _meshScale;
                    Vector3 srcNorm = sourceNormals.Length > v ? sourceNormals[v] : -Vector3.up;

                    float blend = Mathf.InverseLerp(meshMin, meshMax, src.z);
                    float globalBlend = ((float)tile + blend) / tileCount;
                    float sampleF = globalBlend * (totalSamples - 1);
                    int sampleA = Mathf.Clamp(Mathf.FloorToInt(sampleF), 0, totalSamples - 2);
                    int sampleB = Mathf.Min(sampleA + 1, totalSamples - 1);
                    float sampleT = sampleF - sampleA;

                    SplineFrame frameA = sampleFrames[sampleA];
                    SplineFrame frameB = sampleFrames[sampleB];

                    Vector3 pos = Vector3.Lerp(frameA.Position, frameB.Position, sampleT);
                    Vector3 fwd = Vector3.Slerp(frameA.Forward, frameB.Forward, sampleT).normalized;
                    Vector3 up = Vector3.Slerp(frameA.Up, frameB.Up, sampleT).normalized;
                    Vector3 right = Vector3.Slerp(frameA.Right, frameB.Right, sampleT).normalized;

                    right = Vector3.Cross(up, fwd).normalized;
                    up = Vector3.Cross(fwd, right).normalized;

                    Vector3 localVertex = pos + right * src.x + up * src.y;
                    Vector3 localNormal = (right * srcNorm.x + up * srcNorm.y + fwd * srcNorm.z).normalized;

                    vertices.Add(localVertex);
                    normals.Add(localNormal);
                    uvs.Add(sourceUVs.Length > v ? sourceUVs[v] : Vector2.zero);
                }

                for (int t = 0; t < sourceTris.Length; t++) { triangles.Add(baseIndex + sourceTris[t]); }
            }
        }

        private static void GetFrameLocal(float3 localPos, float3 localTan, float3 localUp, out Vector3 pos, out Vector3 fwd, out Vector3 up, out Vector3 right)
        {
            pos = localPos;
            fwd = math.normalize(localTan);
            up = localUp;
            right = Vector3.Cross(up, fwd).normalized;
            up = Vector3.Cross(fwd, right).normalized;
        }

        private static void WeldVertices(Mesh mesh, float posThreshold = 0.0001f, float normalAngleThreshold = 30f)
        {
            var srcVerts = mesh.vertices;
            var srcNormals = mesh.normals;
            var srcTris = mesh.triangles;
            var remap = new int[srcVerts.Length];
            var uniqueV = new List<Vector3>(srcVerts.Length);
            var uniqueN = new List<Vector3>(srcVerts.Length);
            var buckets = new Dictionary<long, List<int>>(srcVerts.Length);
            float snap = 1f / posThreshold;
            float cosThreshold = Mathf.Cos(normalAngleThreshold * Mathf.Deg2Rad);

            for (int i = 0; i < srcVerts.Length; i++)
            {
                Vector3 v = srcVerts[i];
                Vector3 n = srcNormals.Length > i ? srcNormals[i] : Vector3.up;

                long key = HashVec(Mathf.RoundToInt(v.x * snap), Mathf.RoundToInt(v.y * snap), Mathf.RoundToInt(v.z * snap));

                int found = -1;
                if (buckets.TryGetValue(key, out List<int> bucket))
                {
                    foreach (int candidate in bucket)
                    {
                        if (Vector3.Dot(uniqueN[candidate], n) >= cosThreshold)
                        {
                            found = candidate;
                            break;
                        }
                    }
                }
                else
                {
                    bucket = new List<int>(2);
                    buckets[key] = bucket;
                }

                if (found < 0)
                {
                    found = uniqueV.Count;
                    uniqueV.Add(v);
                    uniqueN.Add(n);
                    bucket.Add(found);
                }

                remap[i] = found;
            }

            var newTris = new int[srcTris.Length];
            for (int i = 0; i < srcTris.Length; i++) { newTris[i] = remap[srcTris[i]]; }

            mesh.Clear();
            mesh.SetVertices(uniqueV);
            mesh.SetNormals(uniqueN);
            mesh.SetTriangles(newTris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
        }

        private static long HashVec(int x, int y, int z)
        {
            const int BITS = 20;
            const int MASK = (1 << BITS) - 1;
            return ((long)(x & MASK)) | ((long)(y & MASK) << BITS) | ((long)(z & MASK) << (BITS * 2));
        }

#if UNITY_EDITOR
        protected virtual void SaveMeshAsset(Mesh mesh)
        {
            string scenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
            string sceneDir = System.IO.Path.GetDirectoryName(scenePath);
            string meshDir = System.IO.Path.Combine(sceneDir, "e23.GeneratedMeshes");

            if (!System.IO.Directory.Exists(meshDir))
            { System.IO.Directory.CreateDirectory(meshDir); }

            string meshName = $"{_visualName}_SplineMesh.asset";
            string meshPath = System.IO.Path.Combine(meshDir, meshName).Replace("\\", "/");

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing != null)
            {
                existing.Clear();
                existing.SetVertices(mesh.vertices);
                existing.SetNormals(mesh.normals);
                existing.SetUVs(0, mesh.uv);
                existing.SetTriangles(mesh.triangles, 0);
                existing.RecalculateBounds();
                existing.name = mesh.name;

                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();

                _cachedMesh = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, meshPath);
                AssetDatabase.SaveAssets();

                _cachedMesh = mesh;
            }

            OptimiseMesh(_cachedMesh);

            _meshFilter.sharedMesh = _cachedMesh;

            EditorUtility.SetDirty(_meshFilter);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif
        protected virtual void OptimiseMesh(Mesh mesh)
        {
            MeshUtility.Optimize(mesh);
            MeshUtility.SetMeshCompression(mesh, _meshCompression);

            mesh.uv2 = null;
            mesh.uv3 = null;
            mesh.uv4 = null;
            mesh.colors = null;
            mesh.colors32 = null;
            mesh.tangents = null;

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }

        protected readonly struct SplineFrame
        {
            public readonly Vector3 Position;
            public readonly Vector3 Forward;
            public readonly Vector3 Up;
            public readonly Vector3 Right;

            public SplineFrame(Vector3 position, Vector3 forward, Vector3 up, Vector3 right)
            {
                Position = position;
                Forward = forward;
                Up = up;
                Right = right;
            }
        }
    }
}