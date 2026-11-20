// FloodController.cs - Option A : curseur du niveau de la Seine + animation de la crue.
// À poser sur le même objet que CityBuilder (mode = Crue).
using UnityEngine;

public class FloodController : MonoBehaviour
{
    public CityBuilder city;
    public float maxLevel = 37f;
    public float metersPerSecond = 0.4f;
    public bool animate = false;
    const float Crue1910 = 34.6f;

    float level = -1f;

    void Update()
    {
        if (city == null || city.Data == null) return;
        float normal = city.Data.meta.waterLevel + 0.05f;
        if (level < 0f) level = normal;
        if (animate)
        {
            level += metersPerSecond * Time.deltaTime;
            if (level > maxLevel) level = normal;
        }
        city.SetWaterLevel(level);
    }

    void OnGUI()
    {
        if (city == null || city.Data == null) return;
        float normal = city.Data.meta.waterLevel + 0.05f;
        GUI.Box(new Rect(10, 90, 330, 95), "");
        GUI.Label(new Rect(20, 95, 310, 20), $"Niveau de la Seine : {level:F2} m NGF  (1910 : {Crue1910} m)");
        level = GUI.HorizontalSlider(new Rect(20, 120, 300, 20), level, normal, maxLevel);
        GUI.Label(new Rect(20, 135, 310, 20), $"Bâtiments touchés : {city.CountFlooded(level)} / {city.Data.buildings.Length}");
        animate = GUI.Toggle(new Rect(20, 158, 150, 20), animate, " animer la crue");
        if (GUI.Button(new Rect(180, 156, 140, 22), "Crue de 1910")) { animate = false; level = Crue1910; }
    }
}
