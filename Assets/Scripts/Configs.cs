using DefaultNamespace;

public static class Configs
{
    public const float ScreenshotModeGameSpeed = 0.05f;
    
    public const int HubLevelId = 0;
    
    public const int CanBackToHubLevelIdMin = 200;

    public const int InitialLevelId = 101;
    
    public const int ViewportWidth = 480;
    
    public const int ViewportHeight = 270;

    public static float GetShapeCompareThreshold(string shapeName)
    {
        return shapeName switch
        {
            "1I" => 0.75f,
            "I+O" => 0.95f,
            "I+O_Mirrored" => 0.95f,
            _ => 0.85f,
        };
    }

    public static float GetVfxVolume(string groupName)
    {
        return groupName switch
        {
            SoundGroup.Put => 0.8f,
            _ => 0.5f,
        };
    }
}