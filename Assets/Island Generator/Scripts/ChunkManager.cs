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
    // makes it so that this class can be referenced from other classes without a reference
    public static ChunkManager instance;
    
    //title for the inspector
    [PropertySpace(10)]
    [Title("Island Settings")]

    //change the size of the terrain between three options (4x4, 8x8, 16x16)
    [InfoBox("Change the world size value to control the size of the terrain.")]
    [ValueDropdown("WorldSizes")]
    public Vector2 worldSize;
    private IEnumerable WorldSizes = new ValueDropdownList<Vector2>()
    {
    { "Small", new Vector2(4, 4)},
    { "Medium", new Vector2(8, 8) },
    { "Large", new Vector2(16, 16) },
    };


    [InfoBox("Change the resolution to control the amount of vertices in the terrain mesh.")]
    [ValueDropdown("ResolutionSizes")]
    public int resolution = 16;
    private IEnumerable ResolutionSizes = new ValueDropdownList<int>()
    {
    { "Low", 16 },
    { "Medium", 64 },
    { "High", 128 },
    };

    //change the radius of the island and the max radius
    [InfoBox("Make sure to set the radius of the island to the same size as the world size.")]
    [ValueDropdown("IslandSize")]
    public float islandRadius = 300f;
    private IEnumerable IslandSize = new ValueDropdownList<float>()
    {
    { "Small", 300f },
    { "Medium", 600f },
    { "Large", 1200f },
    };

    [PropertySpace(10)]
    [Title("Noise Settings")]

    //change the intensity of the noise
    [PropertyOrder(1)]
    [InfoBox("Change the Noise intensity value to control the impact that the noise has on the terrain.")]
    [Range(0f, 0.015f)]
    public float noiseIntensity = 0.01f;

    //change the seed of the noise, I have also used the odin inspector to create a button in the inspector that allows users of this package to randomize the seed
    [PropertyOrder(2)]
    [InfoBox("Change the Seed value to generate a different terrain. Also use the Randomize Seed button to randomize the seed.")]
    [MinValue(0f)]
    [InlineButton("RandomSeed", SdfIconType.Dice6Fill, "Randomize Seed")]
    public int seed = 0;
    private void RandomSeed()
    {
        seed = Random.Range(0, 10000);
    }

    //change the height of the terrain
    [PropertyOrder(3)]
    [InfoBox("Change the Height value to control the height of the island.")]
    [MinValue(0f)]
    [PropertyRange(0f, 1000f)]
    public float height = 150f;

    //change whether or not to use the secondary noise. through odin ispector i have set up a color changing button that allows users of this package to toggle the use of the secondary noise
    [HideInInspector]
    [SerializeField] private bool useSecondaryNoise = false;
    float islandPositionY = 0f;

    [PropertyOrder(4)]
    [InfoBox("Click on this button to toggle the use of the secondary noise. This will make the island more or less bumpy.")]
    [ShowIf("useSecondaryNoise")]
    [Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    private void SecondaryNoiseON()
    {
        islandPositionY = 0;
        transform.position = new Vector3(0f, islandPositionY, 0f);
        useSecondaryNoise = !useSecondaryNoise;
    }
    [PropertyOrder(4)]
    [InfoBox("Click on this button to toggle the use of the secondary noise. This will make the island more or less bumpy.")]
    [HideIf("useSecondaryNoise")]
    [Button(ButtonSizes.Large), GUIColor(1, 0.2f, 0)]
    private void SecondaryNoiseOFF()
    {
        islandPositionY = height - (height + (height * 0.045f));
        transform.position = new Vector3(0f, islandPositionY, 0f);
        useSecondaryNoise = !useSecondaryNoise;
    }

    //assigns the terrain material
    [PropertySpace(10)]
    [Title("Terrain Material")]
    [InfoBox("Click on this material to tweak the parameters for the terrain shader. And to assign your own textures")]
    public Material terrainMat;

    //getting the world centre so that it can be used to create a sine wave that is used to create an island shape
    [HideInInspector]
    public Vector2 worldCentre;

    //assigns the chunk manager to be used from other classes
    private void Awake()
    {
        instance = this;       
        //sets the y position of the island so that it lines up correctly with the water. 
        if (useSecondaryNoise)
        {
            islandPositionY = height - (height + (height * 0.045f));
            transform.position = new Vector3(0f, islandPositionY, 0f);
            height *= 0.6f;
        }    
    }

    private void Start()
    {
        //sets the world centre
        worldCentre = new Vector2((worldSize.x / 2f) * 128f, (worldSize.y / 2f) * 128f);
        GenerateChunks();
    }

    // Generates chunks of terrain based on the world size.
    void GenerateChunks()
    {
        // Loop through the world size and create a new terrain generator for each chunk
        for (int x = 0; x < worldSize.x; x++)
        {
            for (int y = 0; y < worldSize.y; y++)
            {
                // Create a new instance of the TerrainGenerator class
                TerrainGenerator terrainGenerator = new TerrainGenerator();

                // Create a new GameObject and assign the MeshFilter, MeshRenderer, and MeshCollider components to it
                GameObject current = new GameObject("Terrain " + (x * y), typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));

                // Set the parent of the current GameObject to the transform of the ChunkManager
                current.transform.parent = transform;

                // Set the local position of the current GameObject to (x * 128f, 0, y * 128f)
                current.transform.localPosition = new Vector3(x * 128f, 0, y * 128f);

                // Call the Init method of the TerrainGenerator class, passing the current GameObject as a parameter
                terrainGenerator.Init(current);

                // Call the Generate method of the TerrainGenerator class, passing the terrainMat, noiseIntensity, seed, height, and useSecondaryNoise as parameters
                terrainGenerator.Generate(terrainMat, noiseIntensity, seed, height, useSecondaryNoise);
            }
        }
    }
}

class TerrainGenerator
{
    //creates the mesh filter, mesh renderer and mesh collider
    MeshFilter filter;
    MeshRenderer renderer;
    MeshCollider collider;  
    Mesh mesh;

    //creates the vertices, triangles and uvs arrays
    Vector3[] verts;
    int[] triangles;
    Vector2[] uvs;

    //initialises the mesh filter, mesh renderer and mesh collider
    public void Init (GameObject current)
    {
        filter = current.GetComponent<MeshFilter>();
        renderer = current.GetComponent<MeshRenderer>();
        collider = current.GetComponent<MeshCollider>();
        mesh = new Mesh();
    }

    //generates the terrain
    public void Generate(Material terrainMat, float noiseIntensity, float seed, float height, bool useSecondaryNoise)
    {
        // Calculate the world position of the current game object
        Vector3 worldPos = new Vector2(filter.gameObject.transform.localPosition.x, filter.gameObject.transform.localPosition.z);

        // Get the resolution of the terrain from the ChunkManager instance
        int resolution = ChunkManager.instance.resolution;

        // Initialize the vertex, triangle, and UV arrays based on the resolution
        verts = new Vector3[(resolution + 1) * (resolution + 1)];
        uvs = new Vector2[verts.Length];

        // Get the center of the world from the ChunkManager instance
        Vector2 worldCentre = ChunkManager.instance.worldCentre;

        // Generate vertices for each point in the terrain grid
        for (int i = 0, x = 0; x <= resolution; x++)
        {
            for (int z = 0; z <= resolution; z++)
            {   
                // Calculate the world position of the current vertex
                Vector2 vertexWorldPosition = new Vector2(worldPos.x + (x * 128 / resolution), worldPos.y + (z * 128 / resolution));

                // Get the island radius from the ChunkManager instance
                float islandRadius = ChunkManager.instance.islandRadius;
                
                // Calculate the distance from the vertex to the world centre
                float distance = Vector2.Distance(worldCentre, vertexWorldPosition);
                
                // Calculate the value of sin based on the distance to the world centre
                float sinWave = Mathf.Sin(Mathf.Clamp(((1 + distance) / islandRadius), 0f, 1f) + 90f);

                // Generate Perlin noise for the current vertex position and add it to the island multiplier
                float PerlinNoise = Mathf.PerlinNoise(vertexWorldPosition.x * noiseIntensity + seed, vertexWorldPosition.y * noiseIntensity + seed) * sinWave;
                float islandMultiplier = PerlinNoise * sinWave * PerlinNoise;
                
                // If secondary noise is enabled, this generates more Perlin noise and adds it to the island
                if (useSecondaryNoise)
                {
                    islandMultiplier += Mathf.PerlinNoise(vertexWorldPosition.x * .01f, vertexWorldPosition.y * .01f) * 0.5f * sinWave;
                    islandMultiplier += Mathf.PerlinNoise(vertexWorldPosition.x * .02f, vertexWorldPosition.y * .02f) * 0.3f * sinWave;
                    islandMultiplier += Mathf.PerlinNoise(vertexWorldPosition.x * .007f, vertexWorldPosition.y * .007f) * 0.3f * sinWave;
                }

                // Calculate the y value of the vertex based on the island multiplier and the maximum height of the terrain
                float y = islandMultiplier * height;

                // Add the vertex to the vertex array
                verts[i] = new Vector3(x * (128f / resolution), y, z * (128f / resolution));

                i++;
            }
        }
        
        // Calculate UV coordinates for each vertex based on its world position
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(verts[i].x + worldPos.x, verts[i].z + worldPos.y);
        }

        // Initialize the triangle array based on the resolution
        triangles = new int[resolution * resolution * 6];
        int tris = 0;
        int vert = 0;

        // Generate triangles for each quad in the terrain grid
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                // Add the vertices and triangles for the current quad to the arrays
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

        // Clear the mesh. Sets its vertices, triangles, UVs, normals, and bounds, and assign the mesh to the collider
        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        collider.sharedMesh = mesh;

        // Assign the mesh to the filter and apply the terrain material to the renderer
        filter.mesh = mesh;
        renderer.material = terrainMat;
    }
}
