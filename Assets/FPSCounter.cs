using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;

    void Update()
    {
        float fps = 1.0f / Time.unscaledDeltaTime;
        fpsText.text = "FPS: " + Mathf.Round(fps);
    }
}
