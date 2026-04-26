using StarterAssets;
public class ThirdPersonController : FirstPersonController
{
    protected override void CameraRotation()
    {
        if (!MultiCameraManager.Instance.IsInThirdPersonCamera())
        {
            base.CameraRotation();
            return;
        }
        
        var activeCameraTransform = MultiCameraManager.Instance.GetActiveCameraTransform();
        transform.forward = activeCameraTransform.forward;
    }
}