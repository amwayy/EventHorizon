
using UnityEngine;

public static class Utility
{
    public static bool WorldToUI(
        Vector3 worldPos,
        Camera cam,
        RectTransform canvas,
        out Vector2 uiPos)
    {
        var screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0)
        {
            uiPos = Vector2.zero;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas,
            screenPos,
            cam,
            out uiPos
        );
    }
}