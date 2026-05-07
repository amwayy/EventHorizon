
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class Utility
{
    public static Rect GetUIRectScreenRect(RectTransform rectTransform, Camera cam)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        foreach (var corner in corners)
        {
            Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, corner);

            min = Vector2.Min(min, screenPoint);
            max = Vector2.Max(max, screenPoint);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    
    public static bool IsPointerOverCollectiveUI()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            if (r.gameObject.TryGetComponent(out JigsawUI _)) return true;
        }

        return false;
    }
    
    private static readonly Dictionary<Texture2D, Sprite> SpriteCache = new();

    public static Sprite GetOrCreateSprite(Texture2D tex)
    {
        if (tex == null) return null;

        if (SpriteCache.TryGetValue(tex, out var sprite))
            return sprite;

        sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f)
        );

        SpriteCache[tex] = sprite;
        return sprite;
    }
    
    public static T GetOrAdd<T>(T prefab, Transform container) where T : Component
    {
        for (var i = 0; i < container.childCount; i++)
        {
            var child = container.GetChild(i);
            if (!child.gameObject.activeSelf && child.TryGetComponent(out T target))
            {
                child.gameObject.SetActive(true);
                return target;
            }
        }

        var instance = Object.Instantiate(prefab, container);
        return instance;
    }
    
    public static float IoU(Rect a, Rect b)
    {
        if (!a.Overlaps(b))
            return 0f;

        float xMin = Mathf.Max(a.xMin, b.xMin);
        float xMax = Mathf.Min(a.xMax, b.xMax);
        float yMin = Mathf.Max(a.yMin, b.yMin);
        float yMax = Mathf.Min(a.yMax, b.yMax);

        float overlap = (xMax - xMin) * (yMax - yMin);
        float union = a.width * a.height + b.width * b.height - overlap;

        return overlap / union;
    }
    
    private const float JigsawKnobRatio = 0.3f;
    
    public static Rect GetJigsawCoreRect(
        Rect fullRect,
        JigsawSO jigsawData,
        int rotation
    )
    {
        rotation = ((rotation % 360) + 360) % 360;

        JigsawEdgeType left = jigsawData.leftEdgeType;
        JigsawEdgeType right = jigsawData.rightEdgeType;
        JigsawEdgeType up = jigsawData.upEdgeType;
        JigsawEdgeType down = jigsawData.downEdgeType;

        // 根据旋转重新映射边
        switch (rotation)
        {
            case 90:
                (left, up, right, down) = (down, left, up, right);
                break;

            case 180:
                (left, right) = (right, left);
                (up, down) = (down, up);
                break;

            case 270:
                (left, up, right, down) = (up, right, down, left);
                break;
        }

        int outX = 0;
        if (left == JigsawEdgeType.Out) outX++;
        if (right == JigsawEdgeType.Out) outX++;

        int outY = 0;
        if (up == JigsawEdgeType.Out) outY++;
        if (down == JigsawEdgeType.Out) outY++;

        // 反推主体尺寸
        float coreWidth = fullRect.width / (1f + outX * JigsawKnobRatio);
        float coreHeight = fullRect.height / (1f + outY * JigsawKnobRatio);

        // 实际凸起尺寸
        float knobWidth = coreWidth * JigsawKnobRatio;
        float knobHeight = coreHeight * JigsawKnobRatio;

        float cutLeft = (left == JigsawEdgeType.Out) ? knobWidth : 0f;
        float cutRight = (right == JigsawEdgeType.Out) ? knobWidth : 0f;
        float cutBottom = (down == JigsawEdgeType.Out) ? knobHeight : 0f;
        float cutTop = (up == JigsawEdgeType.Out) ? knobHeight : 0f;

        float xMin = fullRect.xMin + cutLeft;
        float xMax = fullRect.xMax - cutRight;
        float yMin = fullRect.yMin + cutBottom;
        float yMax = fullRect.yMax - cutTop;

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }
}