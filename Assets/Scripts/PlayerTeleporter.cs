using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerTeleporter : MonoBehaviour
{
    [SerializeField] private Transform startPosition;
    [SerializeField] private Transform hub;

    [ContextMenu("Reset position")]
    public void ResetPosition()
    {
        SetPosition(startPosition);
    }

    [ContextMenu("Go to hub")]
    public void GoToHub()
    {
        SetPosition(hub);
    }

    private void SetPosition(Transform point)
    {
#if UNITY_EDITOR
        Undo.RecordObject(transform, "Reset Position");
#endif

        transform.SetPositionAndRotation(
            point.position,
            point.rotation
        );

#if UNITY_EDITOR
        EditorUtility.SetDirty(transform);
#endif
    }
}