using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class RawImageAlphaRaycast : MonoBehaviour, ICanvasRaycastFilter
{
    [SerializeField] [Range(0f, 1f)] private float alphaThreshold = 0.1f;

    private RawImage _rawImage;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rawImage = GetComponent<RawImage>();
        _rectTransform = GetComponent<RectTransform>();
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (_rawImage.texture == null)
            return true;

        // Convert screen point to local point in RectTransform
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform,
                screenPoint,
                eventCamera,
                out var localPoint))
        {
            return false;
        }

        // Get the rect of the RawImage
        var rect = _rectTransform.rect;

        // Check if the point is within the rect bounds
        if (!rect.Contains(localPoint))
            return false;

        // Convert local point to UV coordinates (0-1 range)
        var uvRect = _rawImage.uvRect;
        var normalizedX = (localPoint.x - rect.x) / rect.width;
        var normalizedY = (localPoint.y - rect.y) / rect.height;

        // Apply UV rect transformation
        var u = uvRect.x + normalizedX * uvRect.width;
        var v = uvRect.y + normalizedY * uvRect.height;

        // Get texture and read pixel
        var texture = _rawImage.texture;

        if (texture is RenderTexture renderTexture)
        {
            return CheckRenderTextureAlpha(renderTexture, u, v);
        }
        else if (texture is Texture2D texture2D)
        {
            return CheckTexture2DAlpha(texture2D, u, v);
        }

        return true;
    }

    private bool CheckRenderTextureAlpha(RenderTexture renderTexture, float u, float v)
    {
        // Create a temporary Texture2D to read the pixel
        var temp = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        RenderTexture.active = renderTexture;

        var x = Mathf.FloorToInt(u * renderTexture.width);
        var y = Mathf.FloorToInt(v * renderTexture.height);

        temp.ReadPixels(new Rect(x, y, 1, 1), 0, 0);
        temp.Apply();

        RenderTexture.active = previous;

        var pixel = temp.GetPixel(0, 0);
        Destroy(temp);

        return pixel.a >= alphaThreshold;
    }

    private bool CheckTexture2DAlpha(Texture2D texture2D, float u, float v)
    {
        try
        {
            var x = Mathf.FloorToInt(u * texture2D.width);
            var y = Mathf.FloorToInt(v * texture2D.height);

            var pixel = texture2D.GetPixel(x, y);
            return pixel.a >= alphaThreshold;
        }
        catch
        {
            // If texture is not readable, allow raycast
            return true;
        }
    }
}
