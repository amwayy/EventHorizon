using UnityEngine;
using UnityEngine.AI;

public class ViewMaskUI : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform target;
    [SerializeField] private RectTransform holeRect;

    private const float HoleMaxScaleFactor = 25f;

    private float _player2TargetDistanceMax;
    private Vector3 _playerStartPosition;

    private void OnEnable()
    {
        _playerStartPosition = player.position;
        _player2TargetDistanceMax = GetWalkedDistance(_playerStartPosition, target.position);
        holeRect.localScale = Vector3.one * HoleMaxScaleFactor;
    }

    private void Update()
    {
        UpdateScale();
    }

    private void UpdateScale()
    {
        var walkedDistance = GetWalkedDistance(_playerStartPosition, player.position);
        var t = Mathf.Clamp01(walkedDistance / _player2TargetDistanceMax);
        t = Mathf.Pow(1 - t, 2f);
        var scaleFactor = Mathf.Lerp(1f, HoleMaxScaleFactor, t);
        holeRect.localScale = Vector3.one * scaleFactor;   
    }

    private static float GetWalkedDistance(Vector3 start, Vector3 end, int areaMask = NavMesh.AllAreas)
    {
        var path = new NavMeshPath();

        if (!NavMesh.CalculatePath(start, end, areaMask, path))
        {
            return Mathf.Infinity;   
        }

        if (path.status != NavMeshPathStatus.PathComplete)
        {
            return Mathf.Infinity;   
        }

        var length = 0f;
        var corners = path.corners;

        for (var i = 1; i < corners.Length; i++)
        {
            length += Vector3.Distance(corners[i - 1], corners[i]);
        }

        return length;
    }
}
