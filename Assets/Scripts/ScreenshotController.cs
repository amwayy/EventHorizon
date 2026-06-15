using DefaultNamespace;
using GameEvent;
using GameEvent.Args;
using UnityEngine;
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
    
    public bool IsInScreenshot { get; private set; }
    
    private RenderTexture _maskTexture;
    private RenderTexture _screenCapture;
    private readonly RenderTexture[] _seedBuffers = new RenderTexture[2];
    private RenderTexture _originalMask;
    private Camera _cam;

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
        
        Screen.fullScreenMode = FullScreenMode.Windowed;
        
        InitTextures();
    }

    private void InitTextures()
    {
        _screenCapture?.Release();
        _screenCapture = new RenderTexture(Configs.ViewportWidth, Configs.ViewportHeight, 24, RenderTextureFormat.ARGB32);
        _screenCapture.enableRandomWrite = true;
        _screenCapture.Create();

        _maskTexture?.Release();
        _maskTexture = new RenderTexture(Configs.ViewportWidth, Configs.ViewportHeight, 0, RenderTextureFormat.RFloat);
        _maskTexture.enableRandomWrite = true;
        _maskTexture.Create();

        _seedBuffers[0]?.Release();
        _seedBuffers[0] = new RenderTexture(Configs.ViewportWidth, Configs.ViewportHeight, 0, RenderTextureFormat.RFloat);
        _seedBuffers[0].enableRandomWrite = true;
        _seedBuffers[0].Create();

        _seedBuffers[1]?.Release();
        _seedBuffers[1] = new RenderTexture(Configs.ViewportWidth, Configs.ViewportHeight, 0, RenderTextureFormat.RFloat);
        _seedBuffers[1].enableRandomWrite = true;
        _seedBuffers[1].Create();

        _originalMask?.Release();
        _originalMask = new RenderTexture(Configs.ViewportWidth, Configs.ViewportHeight, 0, RenderTextureFormat.RFloat);
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
        var isInMenu = GameManager.Instance.IsInMenu;
        if (Input.GetMouseButtonDown(1) && !isInMenu)
        {
            ToggleScreenshotState();
        }
    }

    private void LateUpdate()
    {
        if (IsInScreenshot && !GameManager.Instance.IsInMenu)
        {
            ClearPreviousOutline();
            
            DoColorFloodFill(GameManager.Instance.GetViewportMousePosition());

            if (Input.GetMouseButtonUp(0))
            {
                var hoveringJigsawUI = Utility.GetHoveringJigsawUI();
                if (hoveringJigsawUI)
                {
                    if (!hoveringJigsawUI.IsConnectedWithWorld()) return;
                }
                
                TryCaptureMouseRegion();   
            }
        }
    }

    public JigsawRuntimeData GetSameColorRegionShape(Vector2 position, out Rect rect, out RenderTexture rt,
        out int targetAngle, bool clearOutline, LayerMask layer = default)
    {
        ClearPreviousOutline();
        var originalCullingMask = _cam.cullingMask;
        if (layer != 0)
        {
            _cam.cullingMask = layer;   
        }
        
        rect = Rect.zero;
        rt = null;
        targetAngle = 0;
        DoColorFloodFill(position);
        
        var isShapeMatched = shapeComparor.IsShapeSimilar(_maskTexture, out var angle,
            out var jigsawData, out var capturedRegionRT, releaseRt: false);
        var bboxCenter = shapeComparor.GetBBoxCenter();
        if (!capturedRegionRT)
        {
            if (clearOutline)
            {
                ClearPreviousOutline();   
            }
            _cam.cullingMask = originalCullingMask;
            return new JigsawRuntimeData
            {
                Source = null,
            };  
        } 
        rect = new Rect(
            bboxCenter.x - capturedRegionRT.width / 2f,  bboxCenter.y - capturedRegionRT.height / 2f, 
            capturedRegionRT.width, capturedRegionRT.height);
        rt = capturedRegionRT;
        targetAngle = angle;
        if (!isShapeMatched)
        {
            if (clearOutline)
            {
                ClearPreviousOutline();   
            }
            _cam.cullingMask = originalCullingMask;
            return new JigsawRuntimeData
            {
                Source = null,
            };  
        } 
        if (clearOutline)
        {
            ClearPreviousOutline();   
        }
        _cam.cullingMask = originalCullingMask;
        return Utility.Rotate(jigsawData, angle);
    }

    private void TryCaptureMouseRegion()
    {
        var isOnSlot = false;
        
        var ray = _cam.ScreenPointToRay(GameManager.Instance.GetViewportMousePosition());
        GameObject hitGameObject = null;
        var layerMask = LayerMask.GetMask("Collective") | LayerMask.GetMask("Cuttable");
        JigsawSO[] templateJigsawSos = null;
        if (Physics.Raycast(ray, out var hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore))
        { 
            hitGameObject = hit.collider.gameObject;
            if (hitGameObject.TryGetComponent(out SlotJigsaw _))
            {
                isOnSlot = true;
            }
            else
            {
                var collective = hitGameObject.GetComponentInParent<JigsawCollective>();
                if (collective)
                {
                    templateJigsawSos = collective.TargetJigsawData;   
                }
            }
        }
        
        var isShapeMatched = shapeComparor.IsShapeSimilar(_maskTexture, out var angle, 
            out var jigsawData, out var capturedRegionRT, templateSos: templateJigsawSos);
        if (!isShapeMatched) return;
        if (!jigsawData.canCapture) return;
        if (!capturedRegionRT) return;
        
        ToggleScreenshotState();

        if (isOnSlot)
        {
            AudioManager.Instance.Play(SoundGroup.Put);
            return;
        }

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
        var color = GetColorFromRT(_screenCapture, GameManager.Instance.GetViewportMousePosition());
        EventComponent.Instance.Fire(this,
            CapturedJigsawEventArgs.Create(angle, jigsawData, displayRT, bBoxCenter, color, hitGameObject));
        
        AudioManager.Instance.Play(SoundGroup.Capture);
    }

    private void DoColorFloodFill(Vector2 position)
    {
        var originalRt = _cam.targetTexture;
        _cam.targetTexture = _screenCapture;
        _cam.Render();
        _cam.targetTexture = originalRt;
        
        FloodFillGPU(position);

        hoverOutlineMaterial.SetTexture(FloodFillMask, _maskTexture);
        hoverOutlineMaterial.SetVector(FloodFillMaskTexelSize, 
            new Vector4(1.0f / Configs.ViewportWidth, 1.0f / Configs.ViewportHeight, 
                Configs.ViewportWidth, Configs.ViewportHeight));
        hoverOutlineMaterial.SetInt(OutlineWidth, outlineWidth);
        hoverOutlineMaterial.SetColor(OutlineColor, outlineColor);
    }
    
    private Color GetColorFromRT(RenderTexture rt, Vector2 mousePos)
    {
        // 👉 屏幕坐标 → RT坐标
        int x = Mathf.Clamp((int)(mousePos.x / Configs.ViewportWidth * rt.width), 0, rt.width - 1);
        int y = Mathf.Clamp((int)(mousePos.y / Configs.ViewportHeight * rt.height), 0, rt.height - 1);

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
        var propagateKernel = floodFillShader.FindKernel("WavefrontPropagate");
        var maskKernel = floodFillShader.FindKernel("GenerateMask");

        floodFillShader.SetTexture(initKernel, Source, _screenCapture);
        floodFillShader.SetTexture(initKernel, SeedsOut, _seedBuffers[0]);
        floodFillShader.SetTexture(initKernel, OriginalMask, _originalMask);
        floodFillShader.SetVector(SeedPos, seedPos);
        floodFillShader.SetFloat(Tolerance, tolerance);
        floodFillShader.SetInts(TexSize, Configs.ViewportWidth, Configs.ViewportHeight);
        floodFillShader.Dispatch(initKernel, 
            Mathf.CeilToInt(Configs.ViewportWidth / 8f), 
            Mathf.CeilToInt(Configs.ViewportHeight / 8f), 1);

        // Wavefront propagation: max iterations = max dimension (worst case: diagonal)
        var maxDim = Mathf.Max(Configs.ViewportWidth, Configs.ViewportHeight);
        var read = 0;

        for (var i = 0; i < maxDim; i++)
        {
            var write = 1 - read;

            floodFillShader.SetTexture(propagateKernel, SeedsIn, _seedBuffers[read]);
            floodFillShader.SetTexture(propagateKernel, SeedsOut, _seedBuffers[write]);
            floodFillShader.SetTexture(propagateKernel, OriginalMask, _originalMask);
            floodFillShader.Dispatch(propagateKernel, 
                Mathf.CeilToInt(Configs.ViewportWidth / 8f), 
                Mathf.CeilToInt(Configs.ViewportHeight / 8f), 1);

            read = write;
        }

        floodFillShader.SetTexture(maskKernel, SeedsIn, _seedBuffers[read]);
        floodFillShader.SetTexture(maskKernel, Mask, _maskTexture);
        floodFillShader.Dispatch(maskKernel, 
            Mathf.CeilToInt(Configs.ViewportWidth / 8f), 
            Mathf.CeilToInt(Configs.ViewportHeight / 8f), 1);
    }

    public void ToggleScreenshotState()
    {
        IsInScreenshot = !IsInScreenshot;

        if (!IsInScreenshot)
        {
            ClearPreviousOutline();
        }
        else
        {
            AudioManager.Instance.Play(SoundGroup.Screenshot);
        }

        GameManager.Instance.SetGameSpeed(IsInScreenshot ? Configs.ScreenshotModeGameSpeed : 1f);
        
        EventComponent.Instance.Fire(this, ScreenshotModeToggleEventArgs.Create(IsInScreenshot));
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
