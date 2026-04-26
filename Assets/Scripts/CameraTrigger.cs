using GameEvent;
using GameEvent.Args;
using TMPro;
using UnityEngine;

public class CameraTrigger : MonoBehaviour
{
    [SerializeField] private int index;
    [SerializeField] private TMP_Text textIndex;

    private void Awake()
    {
        textIndex.text = "#" + index;
    }

    private void OnTriggerEnter(Collider other)
    {
        EventComponent.Instance.Fire(this, SwitchCameraEventArgs.Create(index));
    }
}