using System.Collections;
using GameEvent;
using GameEvent.Args;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ScreenshotController : MonoBehaviour
{
    [SerializeField] private Material hoverOutlineMaterial;
    [SerializeField] private ComputeShader floodFillShader;
    [SerializeField] private Color outlineColor = Color.red;
    [SerializeField] [Range(0f, 1f)] private float tolerance = 0.1f;
    [SerializeField] [Range(1, 10)] private int outlineWidth = 1;
    [SerializeField] private ShapeComparor shapeComparor;
    
    private static readonly int OutlineColor = Shader.PropertyToID("_ReplaceColor");
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

    public static ScreenshotController Instance { get; private set; }
    
    private RenderTexture _maskTexture;
    private RenderTexture _screenCapture;
    private readonly RenderTexture[] _seedBuffers = new RenderTexture[2];
    private RenderTexture _originalMask;
    private Camera _cam;
    private int _viewportWidth = 1920;
    private int _viewportHeight = 1080;
    private bool _inScreenshot;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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

            if (Utility.IsPointerOverCollectiveUI()) return;
            
            _cam.targetTexture = _screenCapture;
            _cam.Render();
            _cam.targetTexture = null;

            Vector2 mousePos = Input.mousePosition;

            FloodFillGPU(mousePos);

            hoverOutlineMaterial.SetTexture(FloodFillMask, _maskTexture);
            hoverOutlineMaterial.SetVector(FloodFillMaskTexelSize, new Vector4(1.0f / _viewportWidth, 1.0f / _viewportHeight, _viewportWidth, _viewportHeight));
            hoverOutlineMaterial.SetInt(OutlineWidth, outlineWidth);
            hoverOutlineMaterial.SetColor(OutlineColor, outlineColor);

            if (Input.GetMouseButtonDown(0) && shapeComparor)
            {
                for (var angle = 0; angle < 360; angle += 90)
                {
                    var isShapeMatched = shapeComparor.IsShapeSimilar(_maskTexture, angle, 
                        out var jigsawData, out var capturedRegionRT);
                    if (!isShapeMatched) continue;
                    
                    ToggleScreenshotState();

                    // Convert RFloat mask to ARGB32 with transparency
                    var displayRT = new RenderTexture(capturedRegionRT.width, capturedRegionRT.height, 0, RenderTextureFormat.ARGB32);
                    displayRT.enableRandomWrite = true;
                    displayRT.Create();

                    // Use MaskExtract shader to convert: white foreground, transparent background
                    var maskMaterial = new Material(Shader.Find("Hidden/MaskToTransparent"));
                    Graphics.Blit(capturedRegionRT, displayRT, maskMaterial);
                    Destroy(maskMaterial);

                    // Release the original RFloat mask
                    capturedRegionRT.Release();

                    var bBoxCenter = shapeComparor.GetBBoxCenter();
                    var color = GetColorFromRT(_screenCapture, mousePos);
                    var ray = _cam.ScreenPointToRay(Input.mousePosition);
                    GameObject hitGameObject = null;
                    if (Physics.Raycast(ray, out var hit))
                    { 
                        hitGameObject = hit.collider.gameObject;
                    }
                    EventComponent.Instance.Fire(this,
                        CapturedJigsawEventArgs.Create(angle, jigsawData, displayRT, bBoxCenter, color, hitGameObject));
                    break;
                }
            }
        }
    }
    
    private Color GetColorFromRT(RenderTexture rt, Vector2 mousePos)
    {
        // 👉 屏幕坐标 → RT坐标
        int x = Mathf.Clamp((int)(mousePos.x / Screen.width * rt.width), 0, rt.width - 1);
        int y = Mathf.Clamp((int)(mousePos.y / Screen.height * rt.height), 0, rt.height - 1);

        RenderTexture current = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(x, y, 1, 1), 0, 0);
        tex.Apply();

        Color col = tex.GetPixel(0, 0);

        RenderTexture.active = current;
        Destroy(tex);

        return col;
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

        for (var i = 0; i < steps; i++)
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

    public void ToggleScreenshotState()
    {
        _inScreenshot = !_inScreenshot;

        if (!_inScreenshot)
        {
            ClearPreviousOutline();
        }

        Time.timeScale = _inScreenshot ? 0f : 1f;
        
        EventComponent.Instance.Fire(this, ScreenshotModeToggleEventArgs.Create(_inScreenshot));
    }

    private void ClearPreviousOutline()
    {
        Graphics.SetRenderTarget(_maskTexture);
        GL.Clear(true, true, Color.clear);
        Graphics.SetRenderTarget(_originalMask);
        GL.Clear(true, true, Color.clear);
        Graphics.SetRenderTarget(null);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _cam = Camera.main;
    }
}
