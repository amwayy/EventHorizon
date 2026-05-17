using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShapeComparor : MonoBehaviour
{
    [SerializeField] private JigsawDatabase jigsawDatabase;
    [SerializeField] private ComputeShader shapeCompareShader;

    private static ShapeComparor _instance;
    private static readonly int RegionTex = Shader.PropertyToID("RegionTex");
    private static readonly int ReferenceTex = Shader.PropertyToID("ReferenceTex");
    private static readonly int Result = Shader.PropertyToID("Result");
    private static readonly int Width = Shader.PropertyToID("Width");
    private static readonly int Height = Shader.PropertyToID("Height");
    private static readonly int SourceTex = Shader.PropertyToID("SourceTex");
    private static readonly int BoundingBox = Shader.PropertyToID("BoundingBox");
    private static readonly int SourceWidth = Shader.PropertyToID("SourceWidth");
    private static readonly int SourceHeight = Shader.PropertyToID("SourceHeight");
    private static readonly int CroppedTex = Shader.PropertyToID("CroppedTex");
    private static readonly int MinX = Shader.PropertyToID("MinX");
    private static readonly int MinY = Shader.PropertyToID("MinY");
    private static readonly int RotatedTex = Shader.PropertyToID("RotatedTex");
    private static readonly int RotationAngle = Shader.PropertyToID("RotationAngle");
    private static readonly int CenterX = Shader.PropertyToID("CenterX");
    private static readonly int CenterY = Shader.PropertyToID("CenterY");

    private Dictionary<RenderTexture, JigsawSO> _normalizedReferenceRTs = new();
    private ComputeBuffer _resultBuffer;
    private Material _blitMaterial;
    private Vector2Int _bBoxCenter = new();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _resultBuffer = new ComputeBuffer(5, sizeof(uint));
        
        ComputeReferenceMoments();
    }

    private void OnDestroy()
    {
        _resultBuffer?.Release();
        foreach (var renderTexture in _normalizedReferenceRTs.Keys)
        {
            renderTexture.Release();
        }
        if (_blitMaterial != null)
            Destroy(_blitMaterial);
    }

    private void ComputeReferenceMoments()
    {
        if (!_blitMaterial)
            _blitMaterial = new Material(Shader.Find("Hidden/BlitCopy"));

        foreach (var data in jigsawDatabase.allJigsaws)
        {
            var referenceMask = data.texture;
            var refRT = new RenderTexture(referenceMask.width, referenceMask.height, 0, RenderTextureFormat.RFloat);
            refRT.enableRandomWrite = true;
            refRT.Create();

            // Clear to black first
            RenderTexture.active = refRT;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = null;

            // Enable alpha-aware mode: opaque pixels -> 1, transparent -> 0
            _blitMaterial.SetFloat("_AlphaAware", 1f);
            Graphics.Blit(referenceMask, refRT, _blitMaterial);
            _blitMaterial.SetFloat("_AlphaAware", 0f);

            var rt = NormalizeRegion(refRT, 256, 256, out var croppedRT);
            croppedRT.Release();
            _normalizedReferenceRTs[rt] = data;

            refRT.Release();
        }
    }

    private float CompareShape(RenderTexture regionMask, int rotateAngle, 
        out JigsawSO targetJigsawData, out RenderTexture croppedRT, JigsawSO[] templateJigsawSos)
    {
        var normalizedRegion = NormalizeRegion(regionMask, 256, 256, out croppedRT);

        RenderTexture rotatedRegion = normalizedRegion;
        var hasRotation = Mathf.Abs(rotateAngle) > 0;
        if (hasRotation)
        {
            rotatedRegion = RotateTexture(normalizedRegion, rotateAngle);
            normalizedRegion.Release();
        }

        var maxSimilarity = 0f;
        targetJigsawData = null;

        foreach (var (referenceRT, data) in _normalizedReferenceRTs)
        {
            if (templateJigsawSos != null && templateJigsawSos.Length > 0 && !templateJigsawSos.Contains(data))
            {
                continue;
            }
            var similarity = CompareTemplates(rotatedRegion, referenceRT);
            // Debug.Log($"similarity between {data.jigsawName} (angle {rotateAngle}): {similarity}");
            if (similarity > maxSimilarity)
            {
                maxSimilarity = similarity;
                targetJigsawData = data;
            }
        }
        rotatedRegion.Release();

        return maxSimilarity;
    }

    private RenderTexture RotateTexture(RenderTexture source, float angle)
    {
        var rotated = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.RFloat);
        rotated.enableRandomWrite = true;
        rotated.Create();

        var rotateKernel = shapeCompareShader.FindKernel("RotateTexture");

        shapeCompareShader.SetTexture(rotateKernel, SourceTex, source);
        shapeCompareShader.SetTexture(rotateKernel, RotatedTex, rotated);
        shapeCompareShader.SetInt(Width, source.width);
        shapeCompareShader.SetInt(Height, source.height);
        shapeCompareShader.SetFloat(RotationAngle, angle);
        shapeCompareShader.SetFloat(CenterX, source.width * 0.5f);
        shapeCompareShader.SetFloat(CenterY, source.height * 0.5f);

        shapeCompareShader.Dispatch(rotateKernel, Mathf.CeilToInt(source.width / 8f), Mathf.CeilToInt(source.height / 8f), 1);

        return rotated;
    }

    private float CompareTemplates(RenderTexture region, RenderTexture normalizedReferenceRT)
    {
        if (!normalizedReferenceRT)
            return 0f;

        if (!shapeCompareShader)
        {
            // Debug.LogError("ShapeCompareShader not assigned");
            return 0f;
        }

        var kernel = shapeCompareShader.FindKernel("ComputeNCC");

        var clearData = new uint[5];
        _resultBuffer.SetData(clearData);

        shapeCompareShader.SetTexture(kernel, RegionTex, region);
        shapeCompareShader.SetTexture(kernel, ReferenceTex, normalizedReferenceRT);
        shapeCompareShader.SetBuffer(kernel, Result, _resultBuffer);
        shapeCompareShader.SetInt(Width, region.width);
        shapeCompareShader.SetInt(Height, region.height);

        shapeCompareShader.Dispatch(kernel, Mathf.CeilToInt(region.width / 8f), Mathf.CeilToInt(region.height / 8f), 1);

        var results = new uint[5];
        _resultBuffer.GetData(results);

        var sumRegion = System.BitConverter.ToSingle(System.BitConverter.GetBytes(results[0]), 0);
        var sumRef = System.BitConverter.ToSingle(System.BitConverter.GetBytes(results[1]), 0);
        var sumRegionSq = System.BitConverter.ToSingle(System.BitConverter.GetBytes(results[2]), 0);
        var sumRefSq = System.BitConverter.ToSingle(System.BitConverter.GetBytes(results[3]), 0);
        var sumProduct = System.BitConverter.ToSingle(System.BitConverter.GetBytes(results[4]), 0);
        var count = region.width * region.height;

        var meanRegion = sumRegion / count;
        var meanRef = sumRef / count;

        var numerator = sumProduct - count * meanRegion * meanRef;
        var denominator = Mathf.Sqrt((sumRegionSq - count * meanRegion * meanRegion) *
                                     (sumRefSq - count * meanRef * meanRef));

        var ncc = (denominator > 1e-10f) ? numerator / denominator : 0f;
        var similarity = (ncc + 1f) / 2f;

        // Debug.Log($"NCC: {ncc:F4}, Similarity: {similarity:F4}, sumRegion: {sumRegion:F2}, sumRef: {sumRef:F2}, count: {count}");

        return similarity;
    }

    private RenderTexture NormalizeRegion(RenderTexture source, int targetWidth, int targetHeight, out RenderTexture croppedRT)
    {
        croppedRT = null;
        if (!shapeCompareShader)
        {
            // Debug.LogError("ShapeCompareShader not assigned");
            var empty = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.RFloat);
            empty.Create();
            return empty;
        }

        var bboxBuffer = new ComputeBuffer(4, sizeof(int));
        var initBbox = new int[] { source.width, source.height, 0, 0 };
        bboxBuffer.SetData(initBbox);

        var bboxKernel = shapeCompareShader.FindKernel("ComputeBoundingBox");

        shapeCompareShader.SetTexture(bboxKernel, SourceTex, source);
        shapeCompareShader.SetBuffer(bboxKernel, BoundingBox, bboxBuffer);
        shapeCompareShader.SetInt(SourceWidth, source.width);
        shapeCompareShader.SetInt(SourceHeight, source.height);

        shapeCompareShader.Dispatch(bboxKernel, Mathf.CeilToInt(source.width / 8f), Mathf.CeilToInt(source.height / 8f), 1);

        var bbox = new int[4];
        bboxBuffer.GetData(bbox);
        bboxBuffer.Release();

        var minX = bbox[0];
        var minY = bbox[1];
        var maxX = bbox[2];
        var maxY = bbox[3];

        if (minX > maxX || minY > maxY)
        {
            var empty = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.RFloat);
            empty.Create();
            return empty;
        }

        var bBoxWidth = maxX - minX + 1;
        var bBoxHeight = maxY - minY + 1;
        _bBoxCenter.x = (minX + maxX) / 2;
        _bBoxCenter.y = (minY + maxY) / 2;

        croppedRT = new RenderTexture(bBoxWidth, bBoxHeight, 0, RenderTextureFormat.RFloat);
        croppedRT.enableRandomWrite = true;
        croppedRT.Create();

        var cropKernel = shapeCompareShader.FindKernel("CropRegion");
        shapeCompareShader.SetTexture(cropKernel, SourceTex, source);
        shapeCompareShader.SetTexture(cropKernel, CroppedTex, croppedRT);
        shapeCompareShader.SetInt(MinX, minX);
        shapeCompareShader.SetInt(MinY, minY);

        shapeCompareShader.Dispatch(cropKernel, Mathf.CeilToInt(bBoxWidth / 8f), Mathf.CeilToInt(bBoxHeight / 8f), 1);

        var scale = Mathf.Min((float)targetWidth / bBoxWidth, (float)targetHeight / bBoxHeight);
        var scaledWidth = Mathf.RoundToInt(bBoxWidth * scale);
        var scaledHeight = Mathf.RoundToInt(bBoxHeight * scale);

        var scaledRT = new RenderTexture(scaledWidth, scaledHeight, 0, RenderTextureFormat.RFloat);
        scaledRT.Create();
        Graphics.Blit(croppedRT, scaledRT);

        var normalized = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.RFloat);
        normalized.enableRandomWrite = true;
        normalized.Create();

        RenderTexture.active = normalized;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = null;

        if (!_blitMaterial)
            _blitMaterial = new Material(Shader.Find("Hidden/BlitCopy"));

        var offsetX = (targetWidth - scaledWidth) / 2;
        var offsetY = (targetHeight - scaledHeight) / 2;

        Graphics.SetRenderTarget(normalized);
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, targetWidth, targetHeight, 0);

        Graphics.DrawTexture(new Rect(offsetX, offsetY, scaledWidth, scaledHeight), scaledRT, _blitMaterial);

        GL.PopMatrix();
        Graphics.SetRenderTarget(null);

        scaledRT.Release();

        return normalized;
    }

    public bool IsShapeSimilar(RenderTexture regionMask, int rotateAngle,
        out JigsawSO jigsawData, out RenderTexture capturedRegionRT, bool releaseRt = true, JigsawSO[] templateSos = null)
    {
        var similarity = CompareShape(regionMask, rotateAngle, out jigsawData, out capturedRegionRT, templateSos);
        var similarityThreshold = Configs.GetShapeCompareThreshold(jigsawData.jigsawName);
        var isSimilar = similarity >= similarityThreshold;
        if (!isSimilar && releaseRt) capturedRegionRT?.Release();
        return isSimilar;
    }
    
    public Vector2Int GetBBoxCenter()
    {
        return _bBoxCenter;
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
