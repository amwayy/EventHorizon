using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;

public class ShapeComparor : MonoBehaviour
{
    [SerializeField] private JigsawDatabase jigsawDatabase;
    [SerializeField] private ComputeShader shapeCompareShader;

    private static ShapeComparor _instance;
    private static readonly int SourceTex = Shader.PropertyToID("SourceTex");
    private static readonly int EdgeTex = Shader.PropertyToID("EdgeTex");
    private static readonly int EdgeOutput = Shader.PropertyToID("EdgeOutput");
    private static readonly int DistanceField = Shader.PropertyToID("DistanceField");
    private static readonly int RegionDistanceField = Shader.PropertyToID("RegionDistanceField");
    private static readonly int ReferenceDistanceField = Shader.PropertyToID("ReferenceDistanceField");
    private static readonly int RegionEdge = Shader.PropertyToID("RegionEdge");
    private static readonly int ReferenceEdge = Shader.PropertyToID("ReferenceEdge");
    private static readonly int Result = Shader.PropertyToID("Result");
    private static readonly int Width = Shader.PropertyToID("Width");
    private static readonly int Height = Shader.PropertyToID("Height");
    private static readonly int StepSize = Shader.PropertyToID("StepSize");
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

    private class ReferenceData
    {
        public RenderTexture normalizedRT;
        public RenderTexture edgeRT;
        public RenderTexture distanceFieldRT;
        public JigsawSO jigsawSO;
    }

    private List<ReferenceData> _referenceData = new();
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
        _resultBuffer = new ComputeBuffer(1, sizeof(float));

        ComputeReferenceMoments();
    }

    private void OnDestroy()
    {
        _resultBuffer?.Release();
        foreach (var data in _referenceData)
        {
            data.normalizedRT?.Release();
            data.edgeRT?.Release();
            data.distanceFieldRT?.Release();
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

            RenderTexture.active = refRT;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = null;

            _blitMaterial.SetFloat("_AlphaAware", 1f);
            Graphics.Blit(referenceMask, refRT, _blitMaterial);
            _blitMaterial.SetFloat("_AlphaAware", 0f);

            var normalizedRT = NormalizeRegion(refRT, 256, 256, out var croppedRT);
            croppedRT.Release();

            var edgeRT = ExtractEdges(normalizedRT);
            var distanceFieldRT = ComputeDistanceField(edgeRT, 256, 256);

            _referenceData.Add(new ReferenceData
            {
                normalizedRT = normalizedRT,
                edgeRT = edgeRT,
                distanceFieldRT = distanceFieldRT,
                jigsawSO = data
            });

            refRT.Release();
        }
    }

    private float GetShapeDistance(RenderTexture regionMask, int rotateAngle,
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

        var regionEdge = ExtractEdges(rotatedRegion);
        var regionDistanceField = ComputeDistanceField(regionEdge, 256, 256);

        var minDistance = float.MaxValue;
        targetJigsawData = null;

        foreach (var refData in _referenceData)
        {
            if (templateJigsawSos != null && templateJigsawSos.Length > 0 && !templateJigsawSos.Contains(refData.jigsawSO))
            {
                continue;
            }

            var distance = ComputeChamferDistance(regionDistanceField, refData.distanceFieldRT,
                regionEdge, refData.edgeRT);
            
            // // ------ debug ------
            // var tempSimilarity = 1f / (1f + distance * 0.01f);
            // Debug.Log($"ref jigsaw type: {refData.jigsawSO.jigsawName}; rotate angle: {rotateAngle}; distance: {distance}; similarity: {tempSimilarity}");
            // // ------ debug ------

            if (distance < minDistance)
            {
                minDistance = distance;
                targetJigsawData = refData.jigsawSO;
            }
        }

        rotatedRegion.Release();
        regionEdge.Release();
        regionDistanceField.Release();

        return minDistance;
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

    private RenderTexture ExtractEdges(RenderTexture source)
    {
        var edgeRT = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.RFloat);
        edgeRT.enableRandomWrite = true;
        edgeRT.Create();

        var edgeKernel = shapeCompareShader.FindKernel("ExtractEdges");

        shapeCompareShader.SetTexture(edgeKernel, SourceTex, source);
        shapeCompareShader.SetTexture(edgeKernel, EdgeOutput, edgeRT);
        shapeCompareShader.SetInt(Width, source.width);
        shapeCompareShader.SetInt(Height, source.height);

        shapeCompareShader.Dispatch(edgeKernel, Mathf.CeilToInt(source.width / 8f), Mathf.CeilToInt(source.height / 8f), 1);

        return edgeRT;
    }

    private RenderTexture ComputeDistanceField(RenderTexture edgeRT, int width, int height)
    {
        var distanceFieldRT = new RenderTexture(width, height, 0, RenderTextureFormat.RGFloat);
        distanceFieldRT.enableRandomWrite = true;
        distanceFieldRT.Create();

        var initKernel = shapeCompareShader.FindKernel("JumpFloodInit");
        shapeCompareShader.SetTexture(initKernel, EdgeTex, edgeRT);
        shapeCompareShader.SetTexture(initKernel, DistanceField, distanceFieldRT);
        shapeCompareShader.SetInt(Width, width);
        shapeCompareShader.SetInt(Height, height);
        shapeCompareShader.Dispatch(initKernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);

        var jfaKernel = shapeCompareShader.FindKernel("JumpFlood");
        var maxDim = Mathf.Max(width, height);
        var stepSize = Mathf.NextPowerOfTwo(maxDim) / 2;

        while (stepSize >= 1)
        {
            shapeCompareShader.SetTexture(jfaKernel, DistanceField, distanceFieldRT);
            shapeCompareShader.SetInt(Width, width);
            shapeCompareShader.SetInt(Height, height);
            shapeCompareShader.SetInt(StepSize, stepSize);
            shapeCompareShader.Dispatch(jfaKernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);

            stepSize /= 2;
        }

        return distanceFieldRT;
    }

    private float ComputeChamferDistance(RenderTexture regionDistanceField, RenderTexture referenceDistanceField,
        RenderTexture regionEdge, RenderTexture referenceEdge)
    {
        var kernel = shapeCompareShader.FindKernel("ComputeChamferDistance");

        var clearData = new float[1];
        _resultBuffer.SetData(clearData);

        shapeCompareShader.SetTexture(kernel, RegionDistanceField, regionDistanceField);
        shapeCompareShader.SetTexture(kernel, ReferenceDistanceField, referenceDistanceField);
        shapeCompareShader.SetTexture(kernel, RegionEdge, regionEdge);
        shapeCompareShader.SetTexture(kernel, ReferenceEdge, referenceEdge);
        shapeCompareShader.SetBuffer(kernel, Result, _resultBuffer);
        shapeCompareShader.SetInt(Width, regionDistanceField.width);
        shapeCompareShader.SetInt(Height, regionDistanceField.height);

        shapeCompareShader.Dispatch(kernel, Mathf.CeilToInt(regionDistanceField.width / 8f), Mathf.CeilToInt(regionDistanceField.height / 8f), 1);

        var results = new float[1];
        _resultBuffer.GetData(results);

        return results[0];
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
        var distance = GetShapeDistance(regionMask, rotateAngle, out jigsawData, out capturedRegionRT, templateSos);
        var isSimilar = distance < Configs.ShapeCompareDistanceThreshold;
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
