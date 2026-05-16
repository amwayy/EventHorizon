using DefaultNamespace;

public static class Configs
{
    public const float ScreenshotModeGameSpeed = 0.05f;
    
    public const int HubLevelId = 0;

    public static float GetShapeCompareThreshold(string shapeName)
    {
        return shapeName switch
        {
            "1I" => 0.75f,
            "IO" => 0.85f,
            _ => 0.95f,
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