public static class Configs
{
    public const float ScreenshotModeGameSpeed = 0.05f;

    public static float GetShapeCompareThreshold(string shapeName)
    {
        return shapeName switch
        {
            "1I" => 0.75f,
            "IO" => 0.85f,
            _ => 0.95f,
        };
    }
}