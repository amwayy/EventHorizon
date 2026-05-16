using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class RawImageAlphaRaycast : MonoBehaviour, ICanvasRaycastFilter
{
    [SerializeField] [Range(0f, 1f)] private float alphaThreshold = 0.1f;

    private RawImage _rawImage;
    private RectTransform _rectTransform;
    private Texture2D _cachedTexture2D;
    private RenderTexture _cachedRenderTexture;
    private Texture2D _renderTextureReadBuffer;

    private void Awake()
    {
        _rawImage = GetComponent<RawImage>();
        _rectTransform = GetComponent<RectTransform>();
    }

    private void OnDestroy()
    {
        if (_renderTextureReadBuffer != null)
        {
            Destroy(_renderTextureReadBuffer);
            _renderTextureReadBuffer = null;
        }
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        var texture = _rawImage.texture;
        if (texture == null)
            return true;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform,
                screenPoint,
                eventCamera,
                out var localPoint))
        {
            return false;
        }

        var rect = _rectTransform.rect;
        if (!rect.Contains(localPoint))
            return false;

        var uvRect = _rawImage.uvRect;
        var normalizedX = (localPoint.x - rect.x) / rect.width;
        var normalizedY = (localPoint.y - rect.y) / rect.height;

        var u = uvRect.x + normalizedX * uvRect.width;
        var v = uvRect.y + normalizedY * uvRect.height;

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
        if (_cachedRenderTexture != renderTexture || _renderTextureReadBuffer == null)
        {
            _cachedRenderTexture = renderTexture;

            if (_renderTextureReadBuffer != null)
                Destroy(_renderTextureReadBuffer);

            _renderTextureReadBuffer = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        }

        var previous = RenderTexture.active;
        RenderTexture.active = renderTexture;

        var x = Mathf.Clamp(Mathf.FloorToInt(u * renderTexture.width), 0, renderTexture.width - 1);
        var y = Mathf.Clamp(Mathf.FloorToInt(v * renderTexture.height), 0, renderTexture.height - 1);

        _renderTextureReadBuffer.ReadPixels(new Rect(x, y, 1, 1), 0, 0, false);
        RenderTexture.active = previous;

        var pixel = _renderTextureReadBuffer.GetPixel(0, 0);
        return pixel.a >= alphaThreshold;
    }

    private bool CheckTexture2DAlpha(Texture2D texture2D, float u, float v)
    {
        if (_cachedTexture2D != texture2D)
        {
            _cachedTexture2D = texture2D;
        }

        try
        {
            var x = Mathf.Clamp(Mathf.FloorToInt(u * texture2D.width), 0, texture2D.width - 1);
            var y = Mathf.Clamp(Mathf.FloorToInt(v * texture2D.height), 0, texture2D.height - 1);

            var pixel = texture2D.GetPixel(x, y);
            return pixel.a >= alphaThreshold;
        }
        catch
        {
            return true;
        }
    }

    public bool TryGetAnyVisibleScreenPosition(out Vector2 result)
    {
        result = Vector2.zero;
        var texture = _rawImage.texture;
        if (texture == null)
            return false;

        var canvas = _rawImage.canvas;
        if (canvas == null)
            return false;

        var eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var rect = _rectTransform.rect;
        var uvRect = _rawImage.uvRect;

        // Sample in a grid pattern (e.g., 10x10)
        const int samples = 10;
        for (int y = 0; y < samples; y++)
        {
            for (int x = 0; x < samples; x++)
            {
                var normalizedX = (x + 0.5f) / samples;
                var normalizedY = (y + 0.5f) / samples;

                var localPoint = new Vector2(
                    rect.x + normalizedX * rect.width,
                    rect.y + normalizedY * rect.height
                );

                var worldPoint = _rectTransform.TransformPoint(localPoint);
                var screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPoint);

                // Check if this point is opaque
                var u = uvRect.x + normalizedX * uvRect.width;
                var v = uvRect.y + normalizedY * uvRect.height;

                bool isOpaque = false;
                if (texture is RenderTexture renderTexture)
                {
                    isOpaque = CheckRenderTextureAlpha(renderTexture, u, v);
                }
                else if (texture is Texture2D texture2D)
                {
                    isOpaque = CheckTexture2DAlpha(texture2D, u, v);
                }

                if (!isOpaque)
                    continue;

                // Check if blocked by other RawImages
                if (!IsPointBlocked(screenPoint))
                {
                    result = screenPoint;
                    return true;
                }
            }
        }

        return false;
    }
    
    private static readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

    private bool IsPointBlocked(Vector2 screenPoint)
    {
        var canvas = _rawImage.canvas;
        if (canvas == null)
            return false;

        var raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            return false;

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPoint
        };

        _raycastResults.Clear();
        raycaster.Raycast(eventData, _raycastResults);

        if (_raycastResults.Count == 0)
            return false;

        var top = _raycastResults[0].gameObject;

        return top != gameObject;
    }
}
