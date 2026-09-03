using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class Scaler : MonoBehaviour
{
    public enum ScaleMode { FitWidth, FitHeight, Cover }

    [Tooltip("FitWidth: scale so sprite width matches screen width (touch vertical edges).\nFitHeight: scale so sprite height matches screen height.\nCover: scale to cover the screen while preserving aspect (like CSS cover).")]
    public ScaleMode mode = ScaleMode.FitWidth;

    public float spriteWidth = 1080f;
    public float spriteHeight = 1920f;
    public bool forceCenterAnchors = true;

    private RectTransform rectTransform;
    private float lastScreenWidth;
    private float lastScreenHeight;
    private float lastScale = -1f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (forceCenterAnchors)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        UpdateScale();
    }

    void Update()
    {
        // Ricalcola solo se lo schermo si è ridimensionato
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            UpdateScale();
        }
    }

    private void UpdateScale()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null || Screen.height <= 0 || Screen.width <= 0)
            return;

        float screenAspect = (float)Screen.width / Screen.height;
        float spriteAspect = spriteWidth / spriteHeight;

        float scale;
        switch (mode)
        {
            case ScaleMode.FitWidth:
                scale = (float)Screen.width / spriteWidth;
                break;
            case ScaleMode.FitHeight:
                scale = (float)Screen.height / spriteHeight;
                break;
            case ScaleMode.Cover:
            default:
                // Cover: scale so sprite covers the screen, preserving aspect
                // use the larger scale so both axes are covered
                float scaleW = (float)Screen.width / spriteWidth;
                float scaleH = (float)Screen.height / spriteHeight;
                scale = Mathf.Max(scaleW, scaleH);
                break;
        }

        // Assegna solo se il valore è effettivamente cambiato
        if (Mathf.Abs(scale - lastScale) > 0.0001f)
        {
            rectTransform.localScale = new Vector3(scale, scale, 1f);
            lastScale = scale;
        }
    }
}
