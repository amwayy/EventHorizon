
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
}