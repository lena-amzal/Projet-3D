// E4FD_Functions.hlsl - Fonctions appelées par les noeuds "Custom Function" des Shader Graphs.
// Dans le noeud : Type = File, Source = ce fichier, Name = BuildingColor (ou GroundOverlay, FloodWater).
// Unity ajoute lui-même le suffixe _float au nom : ne le tapez pas dans le champ Name.
#ifndef E4FD_FUNCTIONS_INCLUDED
#define E4FD_FUNCTIONS_INCLUDED

// Nombre pseudo-aléatoire stable entre 0 et 1 pour une cellule de la grille
float E4FD_Hash(float2 p)
{
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

// ---------------------------------------------------------------- Bâtiments
// Tint  : couleur de sommet (teinte de la façade)
// UV    : canal UV0 = (distance le long de la façade, hauteur au-dessus du sol), en mètres
// Data  : canal UV1 = (niveau d'inondation du bâtiment, heures de soleil sur le toit)
// Mode  : 0 = normal, 1 = crue, 2 = soleil
void BuildingColor_float(float4 Tint, float2 UV, float2 Data, float Mode, float WaterLevel,
                         float SunMax, float Night, out float3 Color, out float3 Emission)
{
    // Fenêtres : une travée tous les 3 m, un étage tous les 3,2 m, rien au rez-de-chaussée
    float2 cell = UV / float2(3.0, 3.2);
    float2 f = frac(cell);
    float win = step(0.35, f.x) * step(f.x, 0.75) * step(0.30, f.y) * step(f.y, 0.80) * step(3.2, UV.y);

    float3 c = lerp(Tint.rgb, float3(0.10, 0.12, 0.15), win);

    // La nuit, environ 60 % des fenêtres sont allumées (choix aléatoire par fenêtre)
    float lit = step(0.4, E4FD_Hash(floor(cell)));
    Emission = win * lit * Night * float3(1.0, 0.72, 0.38) * 3.0;

    if (Mode > 0.5 && Mode < 1.5)            // crue : bâtiment touché = rouge
    {
        float flooded = step(Data.x, WaterLevel);
        c = lerp(c, float3(0.9, 0.08, 0.06), flooded * 0.85);
    }
    else if (Mode > 1.5)                     // soleil : rampe sombre -> rouge -> jaune
    {
        float t = saturate(Data.y / max(SunMax, 0.001));
        float3 lo = float3(0.08, 0.02, 0.20), mid = float3(0.85, 0.20, 0.10), hi = float3(1.0, 0.95, 0.45);
        c = t < 0.5 ? lerp(lo, mid, t * 2.0) : lerp(mid, hi, t * 2.0 - 1.0);
    }
    Color = c;
}

// ---------------------------------------------------------------- Sol et toits
// Base : couleur de l'orthophoto ; Over : calque de données (RGBA) ; Strength : 0..1
void GroundOverlay_float(float4 Base, float4 Over, float Strength, out float3 Color)
{
    Color = lerp(Base.rgb, Over.rgb, saturate(Over.a * Strength));
}

// ---------------------------------------------------------------- Eau
// Arrival01 : canal R de la carte flood.png (0..1) ; un pixel est sous l'eau si le
// niveau courant est >= au niveau qui l'inonde. Alpha = 0 ailleurs : avec
// Alpha Clip Threshold = 0.5 dans le Shader Graph, ces pixels ne sont pas dessinés.
void FloodWater_float(float Arrival01, float Level, float MinLevel, float MaxLevel, out float Alpha)
{
    float arrival = MinLevel + Arrival01 * (MaxLevel - MinLevel);
    Alpha = step(arrival, Level) * 0.85;     // 0.85 = eau légèrement transparente
}

#endif
