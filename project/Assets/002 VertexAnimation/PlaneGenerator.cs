using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class PlaneGenerator : MonoBehaviour
{
    public int Grid = 10;
    public float Size = 1.0f;
    public float Period = 1.0f;
    public float Speed = 1.0f;
    public float Amplitude = 1.0f;

    public bool MeshUpdate;

    public enum DeformMethod
    {
        Ripple,
        PerlinNoise,
        VoronoiNoise,
    }

    public DeformMethod Method;
    // Start is called before the first frame update
    void Start()
    {
        // GenerateMesh();
        GenerateMeshJob();
    }

    // Update is called once per frame
    void Update()
    {
        if (MeshUpdate)
        {
            var inputMesh = Mesh.AcquireReadOnlyMeshData(GetComponent<MeshFilter>().sharedMesh);
            var tempVertices = new NativeArray<Vector3>(inputMesh[0].vertexCount, Allocator.TempJob);
            inputMesh[0].GetVertices(tempVertices);
            
            switch (Method)
            {
                case DeformMethod.PerlinNoise:
                    new PNoiseJob()
                    {
                        Vertices = tempVertices,
                        Time = Time.time,
                        Period = Period,
                        Speed = Speed,
                        Amplitude = Amplitude,
                    }.Schedule(inputMesh[0].vertexCount, 64).Complete();
                    break;
                case DeformMethod.Ripple:
                    new RippleJob()
                    {
                        Vertices = tempVertices,
                        Time = Time.time,
                        Period = Period,
                        Speed = Speed,
                        Amplitude = Amplitude,
                    }.Schedule(inputMesh[0].vertexCount, 64).Complete();
                    break;
                case DeformMethod.VoronoiNoise:
                    new VNoiseJob()
                    {
                        Vertices = tempVertices,
                        Time = Time.time,
                        Period = Period,
                        Speed = Speed,
                        Amplitude = Amplitude,
                    }.Schedule(inputMesh[0].vertexCount, 64).Complete();
                    break;
            }

            GetComponent<MeshFilter>().sharedMesh.SetVertices(tempVertices);
            GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
            inputMesh.Dispose();
            tempVertices.Dispose();
        }
    }

    private void GenerateMesh()
    {
        var mesh = new Mesh();
        var vertices = new Vector3[(Grid + 1) * (Grid + 1)];
        var uvs = new Vector2[(Grid + 1) * (Grid + 1)];
        var indices = new int[3 * Grid * Grid * 2];

        float step = 1.0f / Grid;
        for (int i = 0; i < Grid+1; i++)
        {
            for (int j = 0; j < Grid+1; j++)
            {
                vertices[i * (Grid + 1) + j] = new Vector3(i * step, 0, j * step);
                uvs[i * (Grid + 1) + j] = new Vector2(i * step, j * step);
            }
        }
        for (int i = 0; i < Grid; i++)
        {
            for (int j = 0; j < Grid; j++)
            {
                int baseIndices = i * Grid + j;
                var indice0 = (Grid + 1) * i + j;
                var indice1 = (Grid + 1) * i + (j + 1);
                var indice2 = (Grid + 1) * (i + 1) + (j + 1);
                var indice3 = (Grid + 1) * (i + 1) + j;

                indices[6 * baseIndices] = indice0;
                indices[6 * baseIndices + 1] = indice1;
                indices[6 * baseIndices + 2] = indice2;
                indices[6 * baseIndices + 3] = indice2;
                indices[6 * baseIndices + 4] = indice3;
                indices[6 * baseIndices + 5] = indice0;
            }
        }
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    private Mesh.MeshDataArray InitializeMesh(out int indicesCount, out int vertexCount)
    {
        var meshArray = Mesh.AllocateWritableMeshData(1);
        var meshTemp = meshArray[0];
        var descriptor =
            new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp,
                NativeArrayOptions.ClearMemory);

        descriptor[0] = new VertexAttributeDescriptor(dimension: 3);
        descriptor[1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 2);

        vertexCount = (Grid + 1) * (Grid + 1);
        indicesCount = 3 * Grid * Grid * 2;

        meshTemp.SetVertexBufferParams(vertexCount, descriptor);
        descriptor.Dispose();
        meshTemp.SetIndexBufferParams(indicesCount, IndexFormat.UInt32);

        meshTemp.subMeshCount = 1;
        meshTemp.SetSubMesh(0, new SubMeshDescriptor(0, indicesCount),
            MeshUpdateFlags.DontRecalculateBounds |
            MeshUpdateFlags.DontValidateIndices);

        return meshArray;
    }

    private void GenerateMeshJob()
    {
        var meshArray = InitializeMesh(out var incidesCount, out var verticesCount);

        var job = new PlaneJob()
        {
            OutMesh = meshArray[0],
            Grid = Grid,
            Size = Size
        };
        job.Schedule(verticesCount, 64).Complete();

        var mesh = new Mesh();
        Mesh.ApplyAndDisposeWritableMeshData(meshArray, mesh);
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    public struct Vertex
    {
        public float3 Position;
        public float2 Uv;
    }

    //[BurstCompile()]
    public struct PlaneJob : IJobParallelFor
    {
        public Mesh.MeshData OutMesh;
        public int Grid;
        public float Size;
        public void Execute(int id)
        {
            var sourceVt = OutMesh.GetVertexData<Vertex>();
            var sourceIndex = OutMesh.GetIndexData<int>();
            int i = (int)math.floor((float)id / (Grid + 1));
            int j = (int)(id - i * (Grid + 1));

            float3 stepx = new float3(Size, 0,0) / Grid;
            float3 stepz = new float3(0, 0, Size) / Grid;

            float2 stepu = new float2(1, 0) / Grid;
            float2 stepv = new float2(0, 1) / Grid;

            var vt = new Vertex()
            {
                Position = stepx * i + stepz * j,
                Uv = stepu * i + stepv * j,
            };

            sourceVt[id] = vt;

            if (i < Grid && j < Grid)
            {
                int faceStartIt = i * Grid + j;
                var indice0 = (Grid + 1) * i + j;
                var indice1 = (Grid + 1) * i + (j + 1);
                var indice2 = (Grid + 1) * (i + 1) + (j + 1);
                var indice3 = (Grid + 1) * (i + 1) + j;

                sourceIndex[6 * faceStartIt] = indice0;
                sourceIndex[6 * faceStartIt + 1] = indice1;
                sourceIndex[6 * faceStartIt + 2] = indice2;
                sourceIndex[6 * faceStartIt + 3] = indice2;
                sourceIndex[6 * faceStartIt + 4] = indice3;
                sourceIndex[6 * faceStartIt + 5] = indice0;
            }

            sourceVt.Dispose();
            sourceIndex.Dispose();
        }
    }

    public struct RippleJob : IJobParallelFor
    {
        public NativeArray<Vector3> Vertices;
        public float Time;
        public float Period;
        public float Speed;
        public float Amplitude;
        public void Execute(int id)
        {
            Vertices[id] = Ripple(Vertices[id], Time, Period, Speed, Amplitude);
        }
    }

    public struct PNoiseJob : IJobParallelFor
    {
        public NativeArray<Vector3> Vertices;
        public float Time;
        public float Period;
        public float Speed;
        public float Amplitude;
        public void Execute(int id)
        {
            Vertices[id] = PerlinNoise(Vertices[id], Time, Period, Speed, Amplitude);
        }
    }

    public struct VNoiseJob : IJobParallelFor
    {
        public NativeArray<Vector3> Vertices;
        public float Time;
        public float Period;
        public float Speed;
        public float Amplitude;
        public void Execute(int id)
        {
            Vertices[id] = VoronoiNoise(Vertices[id], Time, Period, Speed, Amplitude);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Ripple(float3 position, float time, float period, float speed, float amplitude)
    {
        return new float3(
            position.x,
            amplitude * math.sin(speed * time + period * math.length(position.xz)),
            position.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 PerlinNoise(float3 position, float time, float period, float speed, float amplitude)
    {
        return new float3(
            position.x,
            amplitude * noise.cnoise(period * position.xz + speed * time),
            position.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 VoronoiNoise(float3 position, float time, float period, float speed, float amplitude)
    {
        return new float3(
            position.x,
            amplitude * noise.cellular(period * position.xz + speed * time).x,
            position.z);
    }
}
