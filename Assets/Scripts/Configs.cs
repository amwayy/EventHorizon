using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

public static class Configs
{
    public const float ScreenshotModeGameSpeed = 0.05f;
    
    public const int HubLevelId = 0;
    
    public const int CanBackToHubLevelIdMin = 200;

    public const int InitialLevelId = 101;
    
    public const int ViewportWidth = 480;
    
    public const int ViewportHeight = 270;

    public static readonly Dictionary<(int, int), SlotJigsawData> InitialPutJigsaws = new()
    {
        {(101, 1), new SlotJigsawData
        {
            CollectiveIndexes = new []{ (101, 1) },
            RotationAngle = 180,
            JigsawName = "1O",
            JigsawColor = new Color(71 / 255f, 200 / 255f, 78 / 255f),
        }}
    };

    public static float GetShapeCompareThreshold(string shapeName)
    {
        return shapeName switch
        {
            "1I" => 0.75f,
            "1O" => 0.9f,
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