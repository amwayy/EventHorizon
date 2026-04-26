using UnityEngine;

public class LevelTarget : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Level Complete!");
    }
}