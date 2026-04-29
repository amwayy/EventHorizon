using UnityEngine;

public class ShapeComparor : MonoBehaviour
{
    [SerializeField] private Texture2D referenceMask;
    [SerializeField] [Range(0f, 1f)] private float similarityThreshold = 0.85f;

    private ComputeBuffer _momentsBuffer;
    private ComputeBuffer _referenceMomentsBuffer;
    private ComputeBuffer _groupMomentsBuffer;
    private ComputeBuffer _referenceGroupMomentsBuffer;
    private Texture2D _normalizedReferenceTex;

    private void Start()
    {
        _momentsBuffer = new ComputeBuffer(10, sizeof(float));
        _referenceMomentsBuffer = new ComputeBuffer(10, sizeof(float));

        if (referenceMask != null)
        {
            ComputeReferenceMoments();
        }
    }

    private void OnDestroy()
    {
        _momentsBuffer?.Release();
        _referenceMomentsBuffer?.Release();
        _groupMomentsBuffer?.Release();
        _referenceGroupMomentsBuffer?.Release();
        if (_normalizedReferenceTex != null)
            Destroy(_normalizedReferenceTex);
    }

    private void ComputeReferenceMoments()
    {
        var refRT = new RenderTexture(referenceMask.width, referenceMask.height, 0, RenderTextureFormat.RFloat);
        refRT.enableRandomWrite = true;
        refRT.Create();

        Graphics.Blit(referenceMask, refRT);

        // Normalize reference mask the same way as game regions
        var normalizedRef = NormalizeRegion(refRT, 256, 256);
        // SaveMaskVisualization(normalizedRef, "normalized_reference.png");

        // Store normalized reference as Texture2D for template matching
        RenderTexture.active = normalizedRef;
        _normalizedReferenceTex = new Texture2D(256, 256, TextureFormat.RFloat, false);
        _normalizedReferenceTex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        _normalizedReferenceTex.Apply();
        RenderTexture.active = null;

        // Debug.Log("Reference texture normalized and stored for template matching");

        refRT.Release();
        normalizedRef.Release();
    }

    private float CompareShape(RenderTexture regionMask)
    {
        // Normalize the region to standard size before computing moments
        var normalizedRegion = NormalizeRegion(regionMask, 256, 256);

        // Save for debugging
        // SaveMaskVisualization(normalizedRegion, "normalized_region.png");

        // Use template matching instead of Hu moments for more accurate comparison
        var similarity = CompareTemplates(normalizedRegion);

        Debug.Log($"Shape Similarity: {similarity:F3} (threshold: {similarityThreshold})");

        normalizedRegion.Release();

        return similarity;
    }

    private float CompareTemplates(RenderTexture region)
    {
        if (!referenceMask)
        {
            // Debug.LogError("Reference mask is null");
            return 0f;
        }

        // Read both textures
        RenderTexture.active = region;
        var regionTex = new Texture2D(region.width, region.height, TextureFormat.RFloat, false);
        regionTex.ReadPixels(new Rect(0, 0, region.width, region.height), 0, 0);
        regionTex.Apply();
        RenderTexture.active = null;

        // Get reference texture (already normalized in Start)
        var refTex = _normalizedReferenceTex;

        if (!refTex || regionTex.width != refTex.width || regionTex.height != refTex.height)
        {
            // Debug.LogError("Template size mismatch");
            Destroy(regionTex);
            return 0f;
        }

        var width = regionTex.width;
        var height = regionTex.height;

        // Compute normalized cross-correlation
        double sumRegion = 0, sumRef = 0;
        double sumRegionSq = 0, sumRefSq = 0;
        double sumProduct = 0;
        var count = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var regionVal = regionTex.GetPixel(x, y).r;
                var refVal = refTex.GetPixel(x, y).r;

                sumRegion += regionVal;
                sumRef += refVal;
                sumRegionSq += regionVal * regionVal;
                sumRefSq += refVal * refVal;
                sumProduct += regionVal * refVal;
                count++;
            }
        }

        // Normalized cross-correlation coefficient
        var meanRegion = sumRegion / count;
        var meanRef = sumRef / count;

        var numerator = sumProduct - count * meanRegion * meanRef;
        var denominator = System.Math.Sqrt((sumRegionSq - count * meanRegion * meanRegion) *
                                           (sumRefSq - count * meanRef * meanRef));

        var ncc = (denominator > 1e-10) ? (float)(numerator / denominator) : 0f;

        // NCC ranges from -1 to 1, convert to 0-1 similarity
        var similarity = (ncc + 1f) / 2f;

        // Debug.Log($"NCC: {ncc:F4}, Similarity: {similarity:F4}");

        Destroy(regionTex);

        return similarity;
    }

    private static RenderTexture NormalizeRegion(RenderTexture source, int targetWidth, int targetHeight)
    {
        // Read texture to find bounding box
        RenderTexture.active = source;
        var tex = new Texture2D(source.width, source.height, TextureFormat.RFloat, false);
        tex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        int minX = source.width, maxX = 0;
        int minY = source.height, maxY = 0;
        var foundPixel = false;

        // Find bounding box and count pixels
        for (var y = 0; y < source.height; y++)
        {
            for (var x = 0; x < source.width; x++)
            {
                if (tex.GetPixel(x, y).r > 0.5f)
                {
                    foundPixel = true;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (!foundPixel)
        {
            // Debug.LogWarning("No pixels found in region mask");
            Destroy(tex);
            var empty = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.RFloat);
            empty.Create();
            return empty;
        }

        var bboxWidth = maxX - minX + 1;
        var bboxHeight = maxY - minY + 1;

        // Debug.Log($"Bounding box: ({minX}, {minY}) to ({maxX}, {maxY}), size: {bboxWidth}x{bboxHeight}");
        // Debug.Log($"Total pixels: {totalPixels}, bbox area: {bboxArea}, fill ratio: {(float)totalPixels / bboxArea:F2}");

        // Remove noise: if a connected component is too small, ignore it
        // Simple approach: only keep pixels in the bounding box
        var cleaned = new Texture2D(bboxWidth, bboxHeight, TextureFormat.RFloat, false);

        for (var y = 0; y < bboxHeight; y++)
        {
            for (var x = 0; x < bboxWidth; x++)
            {
                var pixel = tex.GetPixel(minX + x, minY + y);
                cleaned.SetPixel(x, y, pixel);
            }
        }
        cleaned.Apply();

        // Calculate scaling to fit in target size while maintaining aspect ratio
        var scale = Mathf.Min((float)targetWidth / bboxWidth, (float)targetHeight / bboxHeight);
        var scaledWidth = Mathf.RoundToInt(bboxWidth * scale);
        var scaledHeight = Mathf.RoundToInt(bboxHeight * scale);

        // Debug.Log($"Scaled size: {scaledWidth}x{scaledHeight}");

        // Create normalized render texture (centered in target size)
        var normalized = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.RFloat);
        normalized.enableRandomWrite = true;
        normalized.Create();

        // Clear to black
        RenderTexture.active = normalized;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = null;

        // Resize cleaned texture to scaled size
        var scaledRT = new RenderTexture(scaledWidth, scaledHeight, 0, RenderTextureFormat.RFloat);
        scaledRT.Create();
        Graphics.Blit(cleaned, scaledRT);

        // Copy scaled texture to center of normalized texture
        var offsetX = (targetWidth - scaledWidth) / 2;
        var offsetY = (targetHeight - scaledHeight) / 2;

        // Read scaled texture
        RenderTexture.active = scaledRT;
        var scaledTex = new Texture2D(scaledWidth, scaledHeight, TextureFormat.RFloat, false);
        scaledTex.ReadPixels(new Rect(0, 0, scaledWidth, scaledHeight), 0, 0);
        scaledTex.Apply();
        RenderTexture.active = null;

        // Write to normalized texture at offset position
        RenderTexture.active = normalized;
        var normalizedTex = new Texture2D(targetWidth, targetHeight, TextureFormat.RFloat, false);

        // Fill with black
        var blackPixels = new Color[targetWidth * targetHeight];
        for (var i = 0; i < blackPixels.Length; i++)
            blackPixels[i] = Color.black;
        normalizedTex.SetPixels(blackPixels);

        // Copy scaled pixels to center
        normalizedTex.SetPixels(offsetX, offsetY, scaledWidth, scaledHeight, scaledTex.GetPixels());
        normalizedTex.Apply();

        Graphics.Blit(normalizedTex, normalized);
        RenderTexture.active = null;

        Destroy(tex);
        Destroy(cleaned);
        Destroy(scaledTex);
        Destroy(normalizedTex);
        scaledRT.Release();

        return normalized;
    }

    public bool IsShapeSimilar(RenderTexture regionMask)
    {
        var similarity = CompareShape(regionMask);
        return similarity >= similarityThreshold;
    }

    private void SaveMaskVisualization(RenderTexture mask, string filename)
    {
        RenderTexture.active = mask;
        var tex = new Texture2D(mask.width, mask.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, mask.width, mask.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        var bytes = tex.EncodeToPNG();
        var path = System.IO.Path.Combine(Application.dataPath, "..", filename);
        System.IO.File.WriteAllBytes(path, bytes);
        // Debug.Log($"Saved mask visualization to: {path}");

        Destroy(tex);
    }
}
