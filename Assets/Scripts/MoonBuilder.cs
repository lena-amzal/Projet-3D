// MoonBuilder.cs - Option C : relief lunaire (cratère Copernic) + calque des cratères détectés.
// Fichiers lus dans Assets/StreamingAssets/lune/ (copie de export/lune/).
// Échelle : 1 unité Unity = 100 m (la zone fait ~240 km -> 2 425 unités).
using System.IO;
using UnityEngine;

public class MoonBuilder : MonoBehaviour
{
    public Material groundMaterial;          // le même Shader Graph "GroundOverlay"
    public Light sun;
    public string folder = "lune";
    public float verticalExaggeration = 2f;
    [Range(0f, 1f)] public float overlayStrength = 1f;
    [Range(0f, 360f)] public float sunAzimuth = 90f;      // soleil levant à l'est
    [Range(1f, 60f)] public float sunElevation = 8f;      // soleil rasant = ombres spectaculaires

    const float Scale = 0.01f;               // m -> unités Unity
    Material mat;

    [System.Serializable]
    class MoonMeta { public int terrainN; public float terrainRes, minAlt, maxAlt, precision, recall; }
    MoonMeta meta;

    void Start()
    {
        string dir = Path.Combine(Application.streamingAssetsPath, folder);
        meta = JsonUtility.FromJson<MoonMeta>(File.ReadAllText(Path.Combine(dir, "meta.json")));
        byte[] raw = File.ReadAllBytes(Path.Combine(dir, "terrain.raw"));
        var h = new float[meta.terrainN * meta.terrainN];
        System.Buffer.BlockCopy(raw, 0, h, 0, raw.Length);

        mat = new Material(groundMaterial);
        mat.SetTexture("_BaseMap", CityBuilder.LoadTexture(Path.Combine(dir, "base.png"), false));
        mat.SetTexture("_OverlayMap", CityBuilder.LoadTexture(Path.Combine(dir, "craters.png"), false));

        var mesh = MeshFactory.Terrain(h, meta.terrainN, meta.terrainRes * Scale,
                                       Scale * verticalExaggeration, meta.minAlt);
        var go = new GameObject("Lune");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    void Update()
    {
        if (mat != null) mat.SetFloat("_OverlayStrength", overlayStrength);
        if (sun != null) sun.transform.rotation = Quaternion.Euler(sunElevation, sunAzimuth + 180f, 0f);
    }

    void OnGUI()
    {
        if (meta == null) return;
        GUI.Box(new Rect(10, 10, 330, 120), "");
        GUI.Label(new Rect(20, 15, 310, 20), $"Précision {meta.precision:F2}   Rappel {meta.recall:F2}");
        GUI.Label(new Rect(20, 35, 310, 20), "Calque des cratères");
        overlayStrength = GUI.HorizontalSlider(new Rect(20, 55, 300, 20), overlayStrength, 0f, 1f);
        GUI.Label(new Rect(20, 70, 310, 20), $"Hauteur du soleil : {sunElevation:F0}°");
        sunElevation = GUI.HorizontalSlider(new Rect(20, 90, 300, 20), sunElevation, 1f, 60f);
        sunAzimuth = GUI.HorizontalSlider(new Rect(20, 110, 300, 20), sunAzimuth, 0f, 360f);
    }
}
