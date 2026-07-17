
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
    
    public static Rect GetScreenRect(Renderer renderer, Camera cam)
    {
        if (renderer == null || cam == null)
            return new Rect();

        Bounds bounds = renderer.bounds;

        // 8个角
        Vector3[] corners = new Vector3[8];
        corners[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
        corners[1] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[3] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[4] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[6] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        corners[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        bool hasPointInFront = false;

        foreach (var corner in corners)
        {
            Vector3 screenPoint = cam.WorldToScreenPoint(corner);

            // 在相机前才算有效
            if (screenPoint.z < 0)
                continue;

            hasPointInFront = true;

            min = Vector2.Min(min, screenPoint);
            max = Vector2.Max(max, screenPoint);
        }

        if (!hasPointInFront)
            return new Rect(); // 完全在背面

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    
    public static JigsawUI GetHoveringJigsawUI()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = GameManager.Instance.GetViewportMousePosition();

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            if (r.gameObject.TryGetComponent(out JigsawUI jigsawUI))
            {
                return jigsawUI;
            }
        }

        return null;
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

        // counter-clockwise
        switch (rotation)
        {
            case 90:
                (left, up, right, down) = (up, right, down, left);
                break;

            case 180:
                (left, right) = (right, left);
                (up, down) = (down, up);
                break;

            case 270:
                (left, up, right, down) = (down, left, up, right);
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
    
    public static bool IsHitTarget(Camera cam, Vector2 screenPos, GameObject target)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.collider.gameObject == target;
        }

        return false;
    }

    public static bool IsSameColor(Color c1, Color c2, float tolerance = 0.01f)
    {
        return Mathf.Abs(c1.r - c2.r) <= tolerance &&
               Mathf.Abs(c1.g - c2.g) <= tolerance &&
               Mathf.Abs(c1.b - c2.b) <= tolerance &&
               Mathf.Abs(c1.a - c2.a) <= tolerance;
    }
    
    public static Rect Union(Rect a, Rect b)
    {
        var xMin = Mathf.Min(a.xMin, b.xMin);
        var yMin = Mathf.Min(a.yMin, b.yMin);
        var xMax = Mathf.Max(a.xMax, b.xMax);
        var yMax = Mathf.Max(a.yMax, b.yMax);

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }
    
    public static JigsawRuntimeData Rotate(JigsawSO data, int angle)
    {
        int steps = ((angle % 360) + 360) % 360 / 90;

        var result = new JigsawRuntimeData
        {
            UpEdgeType = data.upEdgeType,
            DownEdgeType = data.downEdgeType,
            LeftEdgeType = data.leftEdgeType,
            RightEdgeType = data.rightEdgeType,
            Source = data
        };

        for (int i = 0; i < steps; i++)
        {
            result = Rotate90(result);
        }
        
        result.RotateAngle = angle;

        return result;
    }
    
    private static JigsawRuntimeData Rotate90(JigsawRuntimeData d)
    {
        return new JigsawRuntimeData
        {
            UpEdgeType = d.RightEdgeType,
            RightEdgeType = d.DownEdgeType,
            DownEdgeType = d.LeftEdgeType,
            LeftEdgeType = d.UpEdgeType,
            Source = d.Source
        };
    }
    
    public static List<Rect> Subtract(Rect a, Rect b)
    {
        List<Rect> result = new List<Rect>();

        // 求交集
        if (!a.Overlaps(b))
        {
            result.Add(a);
            return result;
        }

        Rect intersection = new Rect(
            Mathf.Max(a.xMin, b.xMin),
            Mathf.Max(a.yMin, b.yMin),
            Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin),
            Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin)
        );

        // 如果完全覆盖
        if (intersection.width <= 0 || intersection.height <= 0)
        {
            result.Add(a);
            return result;
        }

        // 上
        if (intersection.yMax < a.yMax)
        {
            result.Add(new Rect(
                a.xMin,
                intersection.yMax,
                a.width,
                a.yMax - intersection.yMax
            ));
        }

        // 下
        if (intersection.yMin > a.yMin)
        {
            result.Add(new Rect(
                a.xMin,
                a.yMin,
                a.width,
                intersection.yMin - a.yMin
            ));
        }

        // 左
        if (intersection.xMin > a.xMin)
        {
            result.Add(new Rect(
                a.xMin,
                intersection.yMin,
                intersection.xMin - a.xMin,
                intersection.height
            ));
        }

        // 右
        if (intersection.xMax < a.xMax)
        {
            result.Add(new Rect(
                intersection.xMax,
                intersection.yMin,
                a.xMax - intersection.xMax,
                intersection.height
            ));
        }

        return result;
    }
    
    public static bool SubtractAndMerge(Rect a, Rect b, out Rect result)
    {
        var pieces = Subtract(a, b);

        if (pieces.Count == 0)
        {
            result = default;
            return false;
        }

        float xMin = float.MaxValue;
        float yMin = float.MaxValue;
        float xMax = float.MinValue;
        float yMax = float.MinValue;

        foreach (var r in pieces)
        {
            xMin = Mathf.Min(xMin, r.xMin);
            yMin = Mathf.Min(yMin, r.yMin);
            xMax = Mathf.Max(xMax, r.xMax);
            yMax = Mathf.Max(yMax, r.yMax);
        }

        result = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        return true;
    }
    
    public static bool IsNotFullyInsideScreen(Rect rect)
    {
        var screen = new Rect(0, 0, Configs.ViewportWidth, Configs.ViewportHeight);
        return !screen.Contains(rect.min) || !screen.Contains(rect.max);
    }

    public static bool IsUIOnTop(Vector2 screenPoint, GameObject uiGameObject)
    {
        var eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPoint;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count > 0)
        {
            var top = results[0].gameObject;

            if (top != uiGameObject)
            {
                return false;
            }
        }
        return true;
    }
}