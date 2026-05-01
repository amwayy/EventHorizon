using DG.Tweening;
using StarterAssets;
using UnityEngine;

public class ThirdPersonController : FirstPersonController
{
    [SerializeField] private Transform playerCapsule;

    private Vector2 _lastMoveDir;
    
    protected override void CameraRotation()
    {
        if (!MultiCameraManager.Instance.IsInThirdPersonCamera())
        {
            base.CameraRotation();

            if (playerCapsule.rotation != Quaternion.identity)
            {
                transform.rotation = playerCapsule.rotation;
                playerCapsule.localRotation = Quaternion.identity;
            }
            return;
        }

        var capsuleRotationBefore = playerCapsule.rotation;
        var activeCameraTransform = MultiCameraManager.Instance.GetActiveCameraTransform();
        transform.forward = activeCameraTransform.forward;
        playerCapsule.rotation = capsuleRotationBefore;
    }

    protected override void Move()
    {
        base.Move();

        if (MultiCameraManager.Instance.IsInSwitch) return;
        
        if (MultiCameraManager.Instance.IsInThirdPersonCamera() && 
            _input.move != Vector2.zero && _input.move != _lastMoveDir)
        {
            _lastMoveDir = _input.move;
            var forwardDir = new Vector3(_lastMoveDir.x, 0, _lastMoveDir.y).normalized;

            playerCapsule.DOLocalRotateQuaternion(Quaternion.LookRotation(forwardDir), 0.3f);
        }
    }
}