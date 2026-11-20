// SceneData.cs - Structure du fichier scene.json produit par Python.
// JsonUtility exige : des classes [Serializable], des champs publics portant
// EXACTEMENT les noms des clés JSON, et pas de tableaux de tableaux
// (d'où la petite classe HoleData qui enveloppe un float[]).
using System;

[Serializable]
public class SceneData
{
    public MetaData meta;
    public BuildingData[] buildings;
    public RoadData[] roads;
    public TreeData[] trees;
}

[Serializable]
public class MetaData
{
    public float size;          // côté de la zone (m)
    public int terrainN;        // nombre de sommets par côté de la grille du terrain
    public float terrainRes;    // distance entre deux sommets (m)
    public float waterLevel;    // niveau normal de l'eau (m NGF)
    public double xMin, yMin;   // coin sud-ouest en Lambert-93
    public float floodMin = 26f, floodMax = 40f;   // option Crue
    public float sunMax = 16f;                     // option Soleil
}

[Serializable]
public class BuildingData
{
    public string id;
    public float ground;        // altitude du sol au pied du bâtiment (m NGF)
    public float height;        // hauteur mesurée par le LiDAR (m)
    public float[] outer;       // contour extérieur [x0, z0, x1, z1, ...] (sens trigonométrique)
    public HoleData[] holes;    // cours intérieures (sens horaire)
    public int[] roofTris;      // triangles du toit (indices dans outer puis holes)
    public float floodLevel;    // option Crue : niveau qui inonde le bâtiment
    public float sunHours;      // option Soleil : heures de soleil sur le toit
}

[Serializable]
public class HoleData
{
    public float[] pts;
}

[Serializable]
public class RoadData
{
    public string kind;         // valeur du tag OSM highway=*
    public float width;         // largeur (m)
    public int bridge;          // 1 si pont
    public float[] pts;         // [x0, y0, z0, x1, y1, z1, ...] (y = altitude)
}

[Serializable]
public class TreeData
{
    public float x, y, z;       // position du pied de l'arbre
    public float h, r;          // hauteur et rayon du houppier (m)
}
