// CityBuilder.cs - Génère toute la scène au lancement (Play) à partir des fichiers
// produits par Python et copiés dans Assets/StreamingAssets/city/.
using System.IO;
using UnityEngine;

public enum DataMode { Normal = 0, Crue = 1, Soleil = 2 }

public class CityBuilder : MonoBehaviour
{
    [Header("Matériaux (à créer dans le projet, voir annexe)")]
    public Material groundMaterial;   // Shader Graph "GroundOverlay"
    public Material buildingMaterial; // Shader Graph "Building"
    public Material roadMaterial;     // URP/Lit gris foncé
    public Material waterMaterial;    // Shader Graph "FloodWater"
    public GameObject treePrefab;     // arbre de 1 m de haut et 1 m de diamètre, pivot au pied

    [Header("Options")]
    public string folder = "city";
    public DataMode mode = DataMode.Normal;

    public SceneData Data { get; private set; }
    public float WaterLevel { get; private set; }

    Material groundMat, buildingMat, waterMat;   // copies modifiables à l'exécution
    Transform water;

    void Start()
    {
        string dir = Path.Combine(Application.streamingAssetsPath, folder);
        Data = JsonUtility.FromJson<SceneData>(File.ReadAllText(Path.Combine(dir, "scene.json")));
        var meta = Data.meta;
        Debug.Log($"Scène : {Data.buildings.Length} bâtiments, {Data.roads.Length} routes, {Data.trees.Length} arbres");

        // Copies des matériaux : on ne modifie jamais les fichiers du projet pendant le Play
        groundMat = new Material(groundMaterial);
        buildingMat = new Material(buildingMaterial);
        waterMat = new Material(waterMaterial);

        // ---- Terrain ----
        byte[] raw = File.ReadAllBytes(Path.Combine(dir, "terrain.raw"));
        var heights = new float[meta.terrainN * meta.terrainN];
        System.Buffer.BlockCopy(raw, 0, heights, 0, raw.Length);
        groundMat.SetTexture("_BaseMap", LoadTexture(Path.Combine(dir, "ortho.jpg"), false));
        string overlay = Path.Combine(dir, "sun_color.png");
        if (mode == DataMode.Soleil && File.Exists(overlay))
        {
            groundMat.SetTexture("_OverlayMap", LoadTexture(overlay, false));
            groundMat.SetFloat("_OverlayStrength", 0.75f);
        }
        else
        {
            groundMat.SetTexture("_OverlayMap", Texture2D.blackTexture);
            groundMat.SetFloat("_OverlayStrength", 0f);
        }
        CreateObject("Terrain", MeshFactory.Terrain(heights, meta.terrainN, meta.terrainRes), groundMat);

        // ---- Bâtiments (murs + toits) ----
        var bld = CreateObject("Batiments", MeshFactory.Buildings(Data.buildings, meta.size), buildingMat);
        bld.GetComponent<MeshRenderer>().sharedMaterials = new[] { buildingMat, groundMat };
        buildingMat.SetFloat("_Mode", (float)mode);
        buildingMat.SetFloat("_SunMax", meta.sunMax);

        // ---- Routes ----
        CreateObject("Routes", MeshFactory.Roads(Data.roads), roadMaterial);

        // ---- Arbres ----
        if (treePrefab != null)
        {
            var parent = new GameObject("Arbres").transform;
            var rnd = new System.Random(42);
            foreach (var t in Data.trees)
            {
                var go = Instantiate(treePrefab, new Vector3(t.x, t.y, t.z),
                                     Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), parent);
                go.transform.localScale = new Vector3(2f * t.r, t.h, 2f * t.r);
            }
        }

        // ---- Eau : un carré horizontal couvrant la zone, découpé par le shader ----
        string flood = Path.Combine(dir, "flood.png");
        waterMat.SetTexture("_FloodMap", File.Exists(flood) ? LoadTexture(flood, true) : Texture2D.blackTexture);
        waterMat.SetFloat("_FloodMin", meta.floodMin);
        waterMat.SetFloat("_FloodMax", meta.floodMax);
        var quad = MeshFactory.Terrain(new float[] { 0, 0, 0, 0 }, 2, meta.size);
        water = CreateObject("Eau", quad, waterMat).transform;
        SetWaterLevel(meta.waterLevel + 0.05f);
    }

    public void SetWaterLevel(float level)
    {
        WaterLevel = level;
        if (water != null) water.position = new Vector3(0f, level, 0f);
        if (waterMat != null) waterMat.SetFloat("_WaterLevel", level);
        if (buildingMat != null) buildingMat.SetFloat("_WaterLevel", level);
    }

    public void SetNight(float night)          // 0 = jour, 1 = nuit (fenêtres allumées)
    {
        if (buildingMat != null) buildingMat.SetFloat("_Night", night);
    }

    public int CountFlooded(float level)
    {
        int n = 0;
        foreach (var b in Data.buildings) if (b.floodLevel <= level) n++;
        return n;
    }

    GameObject CreateObject(string name, Mesh mesh, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    // linear = true pour les textures de DONNÉES (pas de correction gamma sur les valeurs)
    public static Texture2D LoadTexture(string path, bool linear)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear);
        tex.LoadImage(File.ReadAllBytes(path));
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.anisoLevel = 8;
        return tex;
    }
}
