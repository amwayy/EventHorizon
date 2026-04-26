using UnityEngine;

public class LevelTarget : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        GameManager.Instance.MarkLevelComplete();
    }
}