using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Net.Http.Headers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager instance;
    
    [PropertySpace(10)]
    [Title("Island Settings")]



    [ValueDropdown("WorldSizes")]
    public Vector2 worldSize;
    private IEnumerable WorldSizes = new ValueDropdownList<Vector2>()
    {
    { "Small", new Vector2(4, 4)},
    { "Medium", new Vector2(8, 8) },
    { "Large", new Vector2(16, 16) },
    };


    [InfoBox("Change the resolution value to control the amount of vertices in the terrain mesh.")]
    [ValueDropdown("ResolutionSizes")]
    public int resolution = 16;
    private IEnumerable ResolutionSizes = new ValueDropdownList<int>()
    {
    { "Low", 16 },
    { "Medium", 64 },
    { "High", 128 },
    };

    [PropertyRange(0f, "maxIslandRadius")]
    public float islandRadius = 900f;
    public float maxIslandRadius = 1000f;

    [PropertySpace(10)]
    [Title("Noise Settings")]


    [PropertyOrder(1)]
    [InfoBox("Change the Noise intensity value to control the impact that the noise has on the terrain.")]
    [Range(0f, 0.015f)]
    public float noiseIntensity = 0.01f;


    [PropertyOrder(2)]
    [MinValue(0f)]
    [InlineButton("RandomSeed", SdfIconType.Dice6Fill, "Randomize Seed")]
    public int seed = 0;
    private void RandomSeed()
    {
        seed = Random.Range(0, 10000);
    }

    [PropertyOrder(3)]
    [MinValue(0f)]
    [PropertyRange(0f, 1000f)]
    public float height = 150f;

    [HideInInspector]
    [SerializeField] private bool useSecondaryNoise = false;

    float islandPositionY = 0f;
    [PropertyOrder(4)]
    [ShowIf("useSecondaryNoise")]
    [Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    private void SecondaryNoiseON()
    {
        islandPositionY = 0;
        transform.position = new Vector3(0f, islandPositionY, 0f);
        useSecondaryNoise = !useSecondaryNoise;
    }
    [PropertyOrder(4)]
    [HideIf("useSecondaryNoise")]
    [Button(ButtonSizes.Large), GUIColor(1, 0.2f, 0)]
    private void SecondaryNoiseOFF()
    {
        islandPositionY = height - (height + (height * 0.045f));
        transform.position = new Vector3(0f, islandPositionY, 0f);
        useSecondaryNoise = !useSecondaryNoise;
    }

    [PropertySpace(10)]
    [Title("Terrain Material")]
    public Material terrainMat;

    [HideInInspector]
    public Vector2 worldCentre;

    private void Awake()
    {
        instance = this;        
        if (useSecondaryNoise)
        {
            islandPositionY = height - (height + (height * 0.045f));
            transform.position = new Vector3(0f, islandPositionY, 0f);            
            height *= 0.6f;
        }    
    }

    private void Start()
    {
        worldCentre = new Vector2((worldSize.x / 2f) * 128f, (worldSize.y / 2f) * 128f);
        GenerateChunks();
    }

    void GenerateChunks()
    {
        for (int x = 0; x < worldSize.x; x++)
        {
            for (int y = 0; y < worldSize.y; y++)
            {
                TerrainGenerator terrainGenerator = new TerrainGenerator();
                GameObject current = new GameObject("Terrain " + (x * y), typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                current.transform.parent = transform;
                current.transform.localPosition = new Vector3(x * 128f, 0, y * 128f);

                terrainGenerator.Init(current);
                terrainGenerator.Generate(terrainMat, noiseIntensity, seed, height, useSecondaryNoise);
            }
        }
    }
}

class TerrainGenerator
{
    //public static TerrainGenerator instance;
    MeshFilter filter;
    MeshRenderer renderer;
    MeshCollider collider;  
    Mesh mesh;

    Vector3[] verts;
    int[] triangles;
    Vector2[] uvs;


    public void Init (GameObject current)
    {
        filter = current.GetComponent<MeshFilter>();
        renderer = current.GetComponent<MeshRenderer>();
        collider = current.GetComponent<MeshCollider>();
        mesh = new Mesh();
    }

    public void Generate (Material terrainMat, float noiseIntensity, float seed, float height, bool useSecondaryNoise)
    {
        Vector3 worldPos = new Vector2(filter.gameObject.transform.localPosition.x, filter.gameObject.transform.localPosition.z);
        int resolution = ChunkManager.instance.resolution;

        verts = new Vector3[(resolution + 1) * (resolution + 1)];
        uvs = new Vector2[verts.Length];

        Vector2 worldCentre = ChunkManager.instance.worldCentre;

        for (int i = 0, x = 0; x <= resolution; x++)
        {
            for (int z = 0; z <= resolution; z++)
            {   
                Vector2 vertexWorldPos = new Vector2(worldPos.x + (x * 128 / resolution), worldPos.y + (z * 128 / resolution));

                float islandRadius = ChunkManager.instance.islandRadius;
                
                float distance = Vector2.Distance(worldCentre, vertexWorldPos);
                
                float sin = Mathf.Sin(Mathf.Clamp(((1 + distance) / islandRadius), 0f, 1f)+ 90f);

                float PerlinNoise = Mathf.PerlinNoise(vertexWorldPos.x * noiseIntensity + seed, vertexWorldPos.y * noiseIntensity + seed) * sin;

                float islandMultiplier = PerlinNoise * sin * PerlinNoise;
                
                if (useSecondaryNoise)
                {
                    islandMultiplier += Mathf.PerlinNoise(vertexWorldPos.x * .01f, vertexWorldPos.y * .01f) * 0.5f * sin;
                    islandMultiplier += Mathf.PerlinNoise(vertexWorldPos.x * .02f, vertexWorldPos.y * .02f) * 0.3f * sin;
                    islandMultiplier += Mathf.PerlinNoise(vertexWorldPos.x * .007f, vertexWorldPos.y * .007f) * 0.3f * sin;
                }

                float y = islandMultiplier * height;

                verts[i] = new Vector3(x * (128f / resolution), y, z * (128f / resolution));

                i++;
            }
        }
        

        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(verts[i].x + worldPos.x, verts[i].z + worldPos.y);
        }

        triangles = new int[resolution * resolution * 6];
        int tris = 0;
        int vert = 0;
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + 1;
                triangles[tris + 2] = (int)(vert + resolution + 1);
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = (int)(vert + resolution + 2);
                triangles[tris + 5] = (int)(vert + resolution + 1);
                
                vert++;
                tris += 6;
            }
            vert++;
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        collider.sharedMesh = mesh;

        filter.mesh = mesh;
        renderer.material = terrainMat;
    }
}
