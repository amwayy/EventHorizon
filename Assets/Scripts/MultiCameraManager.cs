using System;
using DG.Tweening;
using GameEvent;
using GameEvent.Args;
using Unity.Cinemachine;
using UnityEngine;

public class MultiCameraManager : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera[] cameras;
    [SerializeField] private CinemachineBrain cinemachineBrain;

    public static MultiCameraManager Instance { get; private set; }
    
    private const int ActiveCameraPriority = 10;
    private const int InactiveCameraPriority = 0;

    private int _activeIndex;
    public bool IsInSwitch { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        EventComponent.Instance.Subscribe(SwitchCameraEventArgs.EventId, OnSwitchCamera);
    }

    private void OnDisable()
    {
        EventComponent.Instance.Unsubscribe(SwitchCameraEventArgs.EventId, OnSwitchCamera);
    }

    private void OnSwitchCamera(object sender, GameEventArgs e)
    {
        if (e is not SwitchCameraEventArgs args) return;
        
        SwitchCamera(args.CameraIndex);
    }

    private void SwitchCamera(int index)
    {
        if (_activeIndex == index) return;
        
        _activeIndex = index;
        for (var i = 0; i < cameras.Length; i++)
        {
            var cam = cameras[i];
            var isActive = i == index;
            cam.gameObject.SetActive(isActive);
            cam.Priority = isActive ? ActiveCameraPriority : InactiveCameraPriority;
        }

        IsInSwitch = true;
        DOVirtual.DelayedCall(cinemachineBrain.DefaultBlend.BlendTime, () => IsInSwitch = false);
    }

    public Transform GetActiveCameraTransform()
    {
        return cameras[_activeIndex].transform;
    }

    public bool IsInThirdPersonCamera()
    {
        return _activeIndex != 0;
    }
}