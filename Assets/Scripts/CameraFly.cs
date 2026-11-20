// CameraFly.cs - Caméra libre (clic droit + souris pour regarder, ZQSD pour avancer)
// et mode "orbite" automatique, pratique pour filmer la vidéo de rendu.
// Utilise le nouveau "Input System" (installé par défaut dans les projets Unity 6).
// Les touches sont repérées par leur POSITION : Key.W = touche Z sur un clavier AZERTY.
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFly : MonoBehaviour
{
    public float speed = 60f;            // m/s (x4 avec Maj)
    public float lookSpeed = 0.15f;
    public bool orbit = true;            // touche O pour basculer
    public Vector3 orbitCenter = new Vector3(500f, 30f, 500f);
    public float orbitRadius = 650f, orbitHeight = 320f, orbitDegPerSec = 5f;

    float yaw, pitch, angle;

    void Start()
    {
        var e = transform.eulerAngles;
        yaw = e.y; pitch = e.x;
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null) return;
        if (kb.oKey.wasPressedThisFrame) orbit = !orbit;

        if (orbit)
        {
            angle += orbitDegPerSec * Time.deltaTime;
            transform.position = orbitCenter + Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, orbitHeight, -orbitRadius);
            transform.LookAt(orbitCenter);
            var e = transform.eulerAngles;
            yaw = e.y; pitch = e.x > 180f ? e.x - 360f : e.x;
            return;
        }

        if (mouse != null && mouse.rightButton.isPressed)
        {
            Vector2 d = mouse.delta.ReadValue();
            yaw += d.x * lookSpeed;
            pitch = Mathf.Clamp(pitch - d.y * lookSpeed, -89f, 89f);
        }
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 move = Vector3.zero;
        if (kb.wKey.isPressed) move += Vector3.forward;   // Z en AZERTY
        if (kb.sKey.isPressed) move += Vector3.back;
        if (kb.aKey.isPressed) move += Vector3.left;      // Q en AZERTY
        if (kb.dKey.isPressed) move += Vector3.right;
        if (kb.spaceKey.isPressed) move += Vector3.up;
        if (kb.leftCtrlKey.isPressed) move += Vector3.down;
        float s = speed * (kb.leftShiftKey.isPressed ? 4f : 1f);
        transform.position += transform.rotation * move * s * Time.deltaTime;
    }
}
