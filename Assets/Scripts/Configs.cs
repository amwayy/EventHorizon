using System.Collections.Generic;
using DefaultNamespace;

public static class Configs
{
    public const float ScreenshotModeGameSpeed = 0.05f;
    
    public const int HubLevelId = 0;
    
    public const int CanBackToHubLevelIdMin = 200;

    public const int InitialLevelId = 100;
    
    public const int ViewportWidth = 480;
    
    public const int ViewportHeight = 270;

    public const float ShapeCompareDistanceThreshold = 15000;

    public static readonly Dictionary<(int, int), SlotJigsawData> InitialPutJigsaws = new()
    {
        {(100, 1), new SlotJigsawData
        {
            CollectiveIndexes = new []{ (100, 1) },
            RotationAngle = 180,
            JigsawName = "1O",
            JigsawColor = Colors.GetJigsawColor(ColorType.Green),
        }},
        {(101, 0), new SlotJigsawData
        {
            CollectiveIndexes = new []{ (101, 1) },
            RotationAngle = 0,
            JigsawName = "1I",
            JigsawColor = Colors.GetJigsawColor(ColorType.Green),
        }},
    };

    public static float GetVfxVolume(string groupName)
    {
        return groupName switch
        {
            SoundGroup.Put => 0.8f,
            _ => 0.5f,
        };
    }
}