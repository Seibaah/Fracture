using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    Color orange = new Color(1f, 0.5f, 0f);

    void Update()
    {
        var fps = 1.0f / Time.unscaledDeltaTime;
        Color color;
        if (fps <= 10) color = Color.red;
        else if (fps <= 20) color = Color.yellow;
        else if (fps <= 30) color = orange;
        else if (fps <= 60) color = Color.green;
        else color = Color.blue;
        fpsText.text = "FPS: " + Mathf.Round(fps);
        fpsText.color = color;        
    }
}
