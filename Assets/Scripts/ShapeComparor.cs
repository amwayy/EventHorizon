using UnityEngine;

public class ShapeComparor : MonoBehaviour
{
    [SerializeField] private Texture2D referenceMask;
    [SerializeField] [Range(0f, 1f)] private float similarityThreshold = 0.85f;
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

    private RenderTexture _normalizedReferenceRT;
    private ComputeBuffer _resultBuffer;
    private Material _blitMaterial;

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

        if (referenceMask != null)
        {
            ComputeReferenceMoments();
        }
    }

    private void OnDestroy()
    {
        _resultBuffer?.Release();
        if (_normalizedReferenceRT != null)
            _normalizedReferenceRT.Release();
        if (_blitMaterial != null)
            Destroy(_blitMaterial);
    }

    private void ComputeReferenceMoments()
    {
        var refRT = new RenderTexture(referenceMask.width, referenceMask.height, 0, RenderTextureFormat.RFloat);
        refRT.enableRandomWrite = true;
        refRT.Create();

        Graphics.Blit(referenceMask, refRT);

        _normalizedReferenceRT = NormalizeRegion(refRT, 256, 256);

        refRT.Release();
    }

    private float CompareShape(RenderTexture regionMask, float rotateAngle = 0f)
    {
        var normalizedRegion = NormalizeRegion(regionMask, 256, 256);

        RenderTexture rotatedRegion = normalizedRegion;
        if (Mathf.Abs(rotateAngle) > 0.01f)
        {
            rotatedRegion = RotateTexture(normalizedRegion, rotateAngle);
            normalizedRegion.Release();
        }

        var similarity = CompareTemplates(rotatedRegion);
        rotatedRegion.Release();

        return similarity;
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

    private float CompareTemplates(RenderTexture region)
    {
        if (!_normalizedReferenceRT)
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
        shapeCompareShader.SetTexture(kernel, ReferenceTex, _normalizedReferenceRT);
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

    private RenderTexture NormalizeRegion(RenderTexture source, int targetWidth, int targetHeight)
    {
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

        var bboxWidth = maxX - minX + 1;
        var bboxHeight = maxY - minY + 1;

        var cropped = new RenderTexture(bboxWidth, bboxHeight, 0, RenderTextureFormat.RFloat);
        cropped.enableRandomWrite = true;
        cropped.Create();

        var cropKernel = shapeCompareShader.FindKernel("CropRegion");
        shapeCompareShader.SetTexture(cropKernel, SourceTex, source);
        shapeCompareShader.SetTexture(cropKernel, CroppedTex, cropped);
        shapeCompareShader.SetInt(MinX, minX);
        shapeCompareShader.SetInt(MinY, minY);

        shapeCompareShader.Dispatch(cropKernel, Mathf.CeilToInt(bboxWidth / 8f), Mathf.CeilToInt(bboxHeight / 8f), 1);

        var scale = Mathf.Min((float)targetWidth / bboxWidth, (float)targetHeight / bboxHeight);
        var scaledWidth = Mathf.RoundToInt(bboxWidth * scale);
        var scaledHeight = Mathf.RoundToInt(bboxHeight * scale);

        var scaledRT = new RenderTexture(scaledWidth, scaledHeight, 0, RenderTextureFormat.RFloat);
        scaledRT.Create();
        Graphics.Blit(cropped, scaledRT);

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

        cropped.Release();
        scaledRT.Release();

        return normalized;
    }

    public bool IsShapeSimilar(RenderTexture regionMask, out float similarity, float rotateAngle = 0f)
    {
        similarity = CompareShape(regionMask, rotateAngle);
        return similarity >= similarityThreshold;
    }
}
