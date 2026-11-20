// SunController.cs - Place la lumière directionnelle à la position réelle du soleil
// (mêmes formules NOAA que option_b_soleil.py) et gère le passage jour / nuit.
// À poser sur la "Directional Light" de la scène.
using System;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Light))]
public class SunController : MonoBehaviour
{
    public CityBuilder city;
    public float latitude = 48.852f, longitude = 2.355f;
    public int year = 2026, month = 6, day = 21;
    [Range(0f, 24f)] public float hourUTC = 16f;
    public int utcOffset = 2;                 // heure légale de Paris : +2 en été, +1 en hiver
    public bool animate = false;
    public float hoursPerSecond = 0.5f;
    public bool showGUI = true;

    Light sun;

    void Start() { sun = GetComponent<Light>(); }

    void Update()
    {
        if (animate) hourUTC = Mathf.Repeat(hourUTC + hoursPerSecond * Time.deltaTime, 24f);
        var t = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc).AddHours(hourUTC);
        SunPosition(latitude, longitude, t, out float az, out float el);

        // Direction VERS le soleil (x = est, y = haut, z = nord) ; la lumière va dans l'autre sens
        float a = az * Mathf.Deg2Rad, e = el * Mathf.Deg2Rad;
        var toSun = new Vector3(Mathf.Sin(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Cos(a) * Mathf.Cos(e));
        transform.rotation = Quaternion.LookRotation(-toSun);

        float day01 = Mathf.Clamp01((el + 4f) / 10f);          // 0 la nuit, 1 dès 6° de hauteur
        sun.intensity = 1.6f * Mathf.Clamp01(el / 8f);
        sun.color = Color.Lerp(new Color(1f, 0.55f, 0.3f), new Color(1f, 0.96f, 0.9f), Mathf.Clamp01(el / 25f));
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.03f, 0.04f, 0.09f), new Color(0.55f, 0.65f, 0.8f), day01);
        RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.02f, 0.02f, 0.05f), new Color(0.45f, 0.45f, 0.45f), day01);
        RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.01f, 0.01f, 0.02f), new Color(0.25f, 0.22f, 0.2f), day01);
        if (city != null) city.SetNight(1f - day01);
    }

    void OnGUI()
    {
        if (!showGUI) return;
        GUI.Box(new Rect(10, 10, 330, 70), "");
        float local = Mathf.Repeat(hourUTC + utcOffset, 24f);
        GUI.Label(new Rect(20, 15, 310, 20), $"{day:00}/{month:00}/{year}  heure légale {(int)local:00}h{(int)(local % 1 * 60):00}");
        hourUTC = GUI.HorizontalSlider(new Rect(20, 40, 300, 20), hourUTC, 0f, 24f);
        animate = GUI.Toggle(new Rect(20, 55, 200, 20), animate, " animer la journée");
    }

    // Position du soleil : azimut (depuis le nord, vers l'est) et hauteur, en degrés
    public static void SunPosition(float latDeg, float lonDeg, DateTime utc, out float azimuth, out float elevation)
    {
        double hour = utc.Hour + utc.Minute / 60.0 + utc.Second / 3600.0;
        double g = 2 * Math.PI / 365 * (utc.DayOfYear - 1 + (hour - 12) / 24);
        double eqtime = 229.18 * (0.000075 + 0.001868 * Math.Cos(g) - 0.032077 * Math.Sin(g)
                        - 0.014615 * Math.Cos(2 * g) - 0.040849 * Math.Sin(2 * g));
        double decl = 0.006918 - 0.399912 * Math.Cos(g) + 0.070257 * Math.Sin(g)
                      - 0.006758 * Math.Cos(2 * g) + 0.000907 * Math.Sin(2 * g)
                      - 0.002697 * Math.Cos(3 * g) + 0.00148 * Math.Sin(3 * g);
        double tst = hour * 60 + eqtime + 4 * lonDeg;
        double ha = (tst / 4 - 180) * Math.PI / 180;
        double lat = latDeg * Math.PI / 180;
        double cosZen = Math.Sin(lat) * Math.Sin(decl) + Math.Cos(lat) * Math.Cos(decl) * Math.Cos(ha);
        double zen = Math.Acos(Math.Max(-1, Math.Min(1, cosZen)));
        elevation = (float)(90 - zen * 180 / Math.PI);
        double az = Math.Atan2(Math.Sin(ha), Math.Cos(ha) * Math.Sin(lat) - Math.Tan(decl) * Math.Cos(lat)) * 180 / Math.PI + 180;
        azimuth = (float)((az % 360 + 360) % 360);
    }
}
