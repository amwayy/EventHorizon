using StarterAssets;
using UnityEngine;

public class ScreenshotController : MonoBehaviour
{
    [SerializeField] private Material colorReplaceMaterial;
    [SerializeField] private ComputeShader floodFillShader;
    [SerializeField] private Color replaceColor = Color.red;
    [SerializeField] [Range(0f, 1f)] private float tolerance = 0.1f;
    [SerializeField] [Range(1, 10)] private int outlineWidth = 1;
    [SerializeField] private StarterAssetsInputs inputs;
    [SerializeField] private GameObject screenshotBorder;
    [SerializeField] private ShapeComparor shapeComparor;
    
    private static readonly int ReplaceColor = Shader.PropertyToID("_ReplaceColor");
    private static readonly int OutlineWidth = Shader.PropertyToID("_OutlineWidth");
    private static readonly int FloodFillMaskTexelSize = Shader.PropertyToID("_FloodFillMask_TexelSize");
    private static readonly int FloodFillMask = Shader.PropertyToID("_FloodFillMask");
    private static readonly int Source = Shader.PropertyToID("Source");
    private static readonly int SeedsOut = Shader.PropertyToID("SeedsOut");
    private static readonly int OriginalMask = Shader.PropertyToID("OriginalMask");
    private static readonly int SeedPos = Shader.PropertyToID("SeedPos");
    private static readonly int Tolerance = Shader.PropertyToID("Tolerance");
    private static readonly int TexSize = Shader.PropertyToID("TexSize");
    private static readonly int StepSize = Shader.PropertyToID("StepSize");
    private static readonly int SeedsIn = Shader.PropertyToID("SeedsIn");
    private static readonly int Mask = Shader.PropertyToID("Mask");

    private RenderTexture _maskTexture;
    private RenderTexture _screenCapture;
    private readonly RenderTexture[] _seedBuffers = new RenderTexture[2];
    private RenderTexture _originalMask;
    private Camera _cam;
    private int _viewportWidth = 1920;
    private int _viewportHeight = 1080;

    private bool _inScreenshot;

    private void Start()
    {
        _cam = Camera.main;
        _viewportWidth = Screen.width;
        _viewportHeight = Screen.height;

        _screenCapture = new RenderTexture(_viewportWidth, _viewportHeight, 24, RenderTextureFormat.ARGB32);
        _screenCapture.enableRandomWrite = true;
        _screenCapture.Create();

        _maskTexture = new RenderTexture(_viewportWidth, _viewportHeight, 0, RenderTextureFormat.RFloat);
        _maskTexture.enableRandomWrite = true;
        _maskTexture.Create();

        _seedBuffers[0] = new RenderTexture(_viewportWidth, _viewportHeight, 0, RenderTextureFormat.RGFloat);
        _seedBuffers[0].enableRandomWrite = true;
        _seedBuffers[0].Create();

        _seedBuffers[1] = new RenderTexture(_viewportWidth, _viewportHeight, 0, RenderTextureFormat.RGFloat);
        _seedBuffers[1].enableRandomWrite = true;
        _seedBuffers[1].Create();

        _originalMask = new RenderTexture(_viewportWidth, _viewportHeight, 0, RenderTextureFormat.RFloat);
        _originalMask.enableRandomWrite = true;
        _originalMask.Create();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            ToggleScreenshotState();
        }

        if (_inScreenshot)
        {
            ClearPreviousOutline();

            _cam.targetTexture = _screenCapture;
            _cam.Render();
            _cam.targetTexture = null;

            Vector2 mousePos = Input.mousePosition;

            FloodFillGPU(mousePos);

            colorReplaceMaterial.SetTexture(FloodFillMask, _maskTexture);
            colorReplaceMaterial.SetVector(FloodFillMaskTexelSize, new Vector4(1.0f / _viewportWidth, 1.0f / _viewportHeight, _viewportWidth, _viewportHeight));
            colorReplaceMaterial.SetInt(OutlineWidth, outlineWidth);
            colorReplaceMaterial.SetColor(ReplaceColor, replaceColor);

            if (Input.GetMouseButtonDown(0) && shapeComparor)
            {
                shapeComparor.IsShapeSimilar(_maskTexture);
            }
        }
    }

    private void FloodFillGPU(Vector2 seedPos)
    {
        var initKernel = floodFillShader.FindKernel("InitSeeds");
        var jfaKernel = floodFillShader.FindKernel("JumpFlood");
        var maskKernel = floodFillShader.FindKernel("GenerateMask");
        
        floodFillShader.SetTexture(initKernel, Source, _screenCapture);
        floodFillShader.SetTexture(initKernel, SeedsOut, _seedBuffers[0]);
        floodFillShader.SetTexture(initKernel, OriginalMask, _originalMask);
        floodFillShader.SetVector(SeedPos, seedPos);
        floodFillShader.SetFloat(Tolerance, tolerance);
        floodFillShader.SetInts(TexSize, _viewportWidth, _viewportHeight);
        floodFillShader.Dispatch(initKernel, Mathf.CeilToInt(_viewportWidth / 8f), Mathf.CeilToInt(_viewportHeight / 8f), 1);
        
        var maxDim = Mathf.Max(_viewportWidth, _viewportHeight);
        var steps = Mathf.CeilToInt(Mathf.Log(maxDim, 2));
        var read = 0;

        for (int i = 0; i < steps; i++)
        {
            var write = 1 - read;
            var stepSize = 1 << (steps - i - 1);

            floodFillShader.SetInt(StepSize, stepSize);
            floodFillShader.SetTexture(jfaKernel, SeedsIn, _seedBuffers[read]);
            floodFillShader.SetTexture(jfaKernel, SeedsOut, _seedBuffers[write]);
            floodFillShader.SetTexture(jfaKernel, OriginalMask, _originalMask);
            floodFillShader.Dispatch(jfaKernel, Mathf.CeilToInt(_viewportWidth / 8f), Mathf.CeilToInt(_viewportHeight / 8f), 1);

            read = write;
        }

        // Extra pass with step size 1 to fill gaps
        var finalWrite = 1 - read;
        floodFillShader.SetInt(StepSize, 1);
        floodFillShader.SetTexture(jfaKernel, SeedsIn, _seedBuffers[read]);
        floodFillShader.SetTexture(jfaKernel, SeedsOut, _seedBuffers[finalWrite]);
        floodFillShader.SetTexture(jfaKernel, OriginalMask, _originalMask);
        floodFillShader.Dispatch(jfaKernel, Mathf.CeilToInt(_viewportWidth / 8f), Mathf.CeilToInt(_viewportHeight / 8f), 1);
        read = finalWrite;
        
        floodFillShader.SetTexture(maskKernel, SeedsIn, _seedBuffers[read]);
        floodFillShader.SetTexture(maskKernel, Mask, _maskTexture);
        floodFillShader.SetTexture(maskKernel, OriginalMask, _originalMask);
        floodFillShader.Dispatch(maskKernel, Mathf.CeilToInt(_viewportWidth / 8f), Mathf.CeilToInt(_viewportHeight / 8f), 1);
    }

    private void ToggleScreenshotState()
    {
        _inScreenshot = !_inScreenshot;

        if (!_inScreenshot)
        {
            ClearPreviousOutline();
        }

        inputs.cursorLocked = !_inScreenshot;
        inputs.SetCursorState(!_inScreenshot);
        Time.timeScale = _inScreenshot ? 0f : 1f;

        screenshotBorder.SetActive(_inScreenshot);
    }

    private void ClearPreviousOutline()
    {
        Graphics.SetRenderTarget(_maskTexture);
        GL.Clear(true, true, Color.clear);
        Graphics.SetRenderTarget(_originalMask);
        GL.Clear(true, true, Color.clear);
        Graphics.SetRenderTarget(null);
    }
}
