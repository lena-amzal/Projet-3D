// MeshFactory.cs - Construction des maillages (terrain, bâtiments, routes).
// Conventions : X = est, Z = nord, Y = altitude, 1 unité = 1 mètre.
// Unity affiche une face si ses 3 sommets tournent dans le sens HORAIRE vus de la caméra.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class MeshFactory
{
    // ------------------------------------------------------------------ Terrain
    // h : altitudes, ligne 0 = SUD, index = j * n + i (i vers l'est, j vers le nord)
    public static Mesh Terrain(float[] h, int n, float res, float yScale = 1f, float yOffset = 0f)
    {
        var v = new Vector3[n * n];
        var uv = new Vector2[n * n];
        float size = (n - 1) * res;
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                int k = j * n + i;
                v[k] = new Vector3(i * res, (h[k] - yOffset) * yScale, j * res);
                uv[k] = new Vector2(i * res / size, j * res / size);   // (0,0) = coin sud-ouest
            }
        var t = new int[(n - 1) * (n - 1) * 6];
        int p = 0;
        for (int j = 0; j < n - 1; j++)
            for (int i = 0; i < n - 1; i++)
            {
                int a = j * n + i;      // (i  , j  )
                int b = a + 1;          // (i+1, j  )
                int c = a + n;          // (i  , j+1)
                int d = c + 1;          // (i+1, j+1)
                t[p++] = a; t[p++] = c; t[p++] = d;     // sens horaire vu du ciel
                t[p++] = a; t[p++] = d; t[p++] = b;
            }
        var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };   // > 65535 sommets
        mesh.vertices = v;
        mesh.uv = uv;
        mesh.triangles = t;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ---------------------------------------------------------------- Bâtiments
    // Un seul maillage pour tous les bâtiments, avec 2 sous-maillages :
    // 0 = murs (matériau "Building"), 1 = toits (matériau du sol : orthophoto).
    static readonly Color32[] Palette =
    {
        new Color32(222, 205, 178, 255), new Color32(214, 196, 168, 255),
        new Color32(230, 216, 190, 255), new Color32(200, 182, 158, 255),
        new Color32(236, 226, 206, 255), new Color32(190, 170, 150, 255),
    };

    public static Mesh Buildings(BuildingData[] list, float size)
    {
        var V = new List<Vector3>();
        var UV0 = new List<Vector2>();     // murs : (distance le long de la façade, hauteur) en m
        var UV1 = new List<Vector2>();     // données : (floodLevel, sunHours)
        var C = new List<Color32>();       // teinte de la façade
        var walls = new List<int>();
        var roofs = new List<int>();

        foreach (var b in list)
        {
            float y0 = b.ground - 1f;            // on enfonce un peu les murs dans le sol
            float y1 = b.ground + b.height;
            int hash = 0;
            foreach (char ch in b.id) hash = hash * 31 + ch;
            Color32 tint = Palette[(hash & 0x7fffffff) % Palette.Length];
            var data = new Vector2(b.floodLevel, b.sunHours);

            // Murs : anneau extérieur puis cours intérieures
            AddWalls(b.outer, b.ground, y0, y1, tint, data, V, UV0, UV1, C, walls);
            if (b.holes != null)
                foreach (var hole in b.holes)
                    AddWalls(hole.pts, b.ground, y0, y1, tint, data, V, UV0, UV1, C, walls);

            // Toit : mêmes points (extérieur puis trous), triangles calculés par Python
            int start = V.Count;
            AddRoofPoints(b.outer, y1, size, data, V, UV0, UV1, C);
            if (b.holes != null)
                foreach (var hole in b.holes)
                    AddRoofPoints(hole.pts, y1, size, data, V, UV0, UV1, C);
            foreach (int idx in b.roofTris) roofs.Add(start + idx);
        }

        var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(V);
        mesh.SetUVs(0, UV0);
        mesh.SetUVs(1, UV1);
        mesh.SetColors(C);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(walls, 0);
        mesh.SetTriangles(roofs, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void AddWalls(float[] pts, float ground, float y0, float y1, Color32 tint, Vector2 data,
                         List<Vector3> V, List<Vector2> UV0, List<Vector2> UV1, List<Color32> C, List<int> T)
    {
        int m = pts.Length / 2;
        float u = 0f;
        for (int i = 0; i < m; i++)
        {
            int k = (i + 1) % m;
            var A = new Vector2(pts[2 * i], pts[2 * i + 1]);
            var B = new Vector2(pts[2 * k], pts[2 * k + 1]);
            float len = Vector2.Distance(A, B);
            int s = V.Count;
            // 4 sommets propres à chaque pan de mur -> arêtes vives (normales non lissées)
            V.Add(new Vector3(A.x, y0, A.y)); UV0.Add(new Vector2(u, y0 - ground));
            V.Add(new Vector3(A.x, y1, A.y)); UV0.Add(new Vector2(u, y1 - ground));
            V.Add(new Vector3(B.x, y1, B.y)); UV0.Add(new Vector2(u + len, y1 - ground));
            V.Add(new Vector3(B.x, y0, B.y)); UV0.Add(new Vector2(u + len, y0 - ground));
            for (int q = 0; q < 4; q++) { UV1.Add(data); C.Add(tint); }
            // Anneau dans le sens trigonométrique => ces triangles regardent vers l'extérieur
            T.Add(s); T.Add(s + 1); T.Add(s + 2);
            T.Add(s); T.Add(s + 2); T.Add(s + 3);
            u += len;
        }
    }

    static void AddRoofPoints(float[] pts, float y, float size, Vector2 data,
                              List<Vector3> V, List<Vector2> UV0, List<Vector2> UV1, List<Color32> C)
    {
        for (int i = 0; i < pts.Length / 2; i++)
        {
            float x = pts[2 * i], z = pts[2 * i + 1];
            V.Add(new Vector3(x, y, z));
            UV0.Add(new Vector2(x / size, z / size));     // même projection que l'orthophoto
            UV1.Add(data);
            C.Add(new Color32(255, 255, 255, 255));
        }
    }

    // ------------------------------------------------------------------- Routes
    public static Mesh Roads(RoadData[] list)
    {
        var V = new List<Vector3>();
        var UV = new List<Vector2>();
        var T = new List<int>();
        int index = 0;
        foreach (var r in list)
        {
            int n = r.pts.Length / 3;
            if (n < 2) continue;
            var P = new Vector3[n];
            for (int i = 0; i < n; i++) P[i] = new Vector3(r.pts[3 * i], r.pts[3 * i + 1], r.pts[3 * i + 2]);
            float lift = 0.3f + (index++ % 7) * 0.01f;      // évite le scintillement (z-fighting)
            float half = r.width * 0.5f;
            var L = new Vector3[n];
            var R = new Vector3[n];
            float dist = 0f;
            int s = V.Count;
            for (int i = 0; i < n; i++)
            {
                Vector3 a = P[Mathf.Max(i - 1, 0)], b = P[Mathf.Min(i + 1, n - 1)];
                Vector3 dir = new Vector3(b.x - a.x, 0f, b.z - a.z).normalized;
                Vector3 left = new Vector3(-dir.z, 0f, dir.x);           // perpendiculaire à gauche
                Vector3 c = P[i] + Vector3.up * lift;
                L[i] = c + left * half;
                R[i] = c - left * half;
                if (i > 0) dist += Vector3.Distance(P[i], P[i - 1]);
                V.Add(L[i]); UV.Add(new Vector2(dist, 0f));
                V.Add(R[i]); UV.Add(new Vector2(dist, 1f));
            }
            for (int i = 0; i < n - 1; i++)
            {
                int l0 = s + 2 * i, r0 = l0 + 1, l1 = l0 + 2, r1 = l0 + 3;
                T.Add(l0); T.Add(l1); T.Add(r1);       // sens horaire vu du ciel
                T.Add(l0); T.Add(r1); T.Add(r0);
            }
            if (r.bridge == 1)
            {
                // Flancs du tablier (1,5 m d'épaisseur) pour que le pont ne soit pas une simple feuille
                for (int i = 0; i < n - 1; i++)
                {
                    AddSkirt(L[i + 1], L[i], 1.5f, V, UV, T);    // flanc gauche (parcouru à l'envers)
                    AddSkirt(R[i], R[i + 1], 1.5f, V, UV, T);    // flanc droit
                }
            }
        }
        var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(V);
        mesh.SetUVs(0, UV);
        mesh.SetTriangles(T, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Pan vertical sous le segment A->B, visible depuis la droite du sens de parcours
    static void AddSkirt(Vector3 A, Vector3 B, float depth, List<Vector3> V, List<Vector2> UV, List<int> T)
    {
        int s = V.Count;
        V.Add(A - Vector3.up * depth); V.Add(A); V.Add(B); V.Add(B - Vector3.up * depth);
        UV.Add(Vector2.zero); UV.Add(Vector2.up); UV.Add(Vector2.one); UV.Add(Vector2.right);
        T.Add(s); T.Add(s + 1); T.Add(s + 2);
        T.Add(s); T.Add(s + 2); T.Add(s + 3);
    }
}
