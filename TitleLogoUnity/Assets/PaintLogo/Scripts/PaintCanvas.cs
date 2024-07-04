using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[RequireComponent(typeof(MeshRenderer))]
public class PaintCanvas : MonoBehaviour
{
    [SerializeField]Texture2D stencilTexture;
    [SerializeField]Shader paintShader;
    
    [SerializeField]Vector3 gravity = new Vector3(0, -9.8f, 0);

    [SerializeField]float paintRadius = 10f;
    [SerializeField]float paintIntensity = 1f;

    enum SurfaceType
    {
        Front,
        Back,
    }
    
    enum BufferType
    {
        Density,
        Velocity,
        Divergence,
    }
    
    private SurfaceType[][] _surface = new SurfaceType[][]
    {
        new SurfaceType[] {SurfaceType.Front, SurfaceType.Back},
        new SurfaceType[] {SurfaceType.Front, SurfaceType.Back},
        new SurfaceType[] {SurfaceType.Front, SurfaceType.Back},
    };
    
    private RenderTexture[][] _bufferTextures = new RenderTexture[][]
    {
        new RenderTexture[] {null, null},
        new RenderTexture[] {null, null},
        new RenderTexture[] {null, null},
    };
    
    private readonly Color[] _clearColors = new Color[]
    {
        new Color(0, 0, 0, 0),
        new Color(0.5f, 0.5f, 0.5f, 0.5f),
        new Color(0.5f, 0.5f, 0.5f, 0.5f),
    };
    
    // private SurfaceType _densitySource = SurfaceType.Front;
    // private SurfaceType _densityDestination = SurfaceType.Back;
    // private SurfaceType _velocitySource = SurfaceType.Front;
    // private SurfaceType _velocityDestination = SurfaceType.Back;
    // private SurfaceType _divergenceSource = SurfaceType.Front;
    // private SurfaceType _divergenceDestination = SurfaceType.Back;
    // public RenderTexture[] DensityTextures = new RenderTexture[System.Enum.GetValues(typeof(SurfaceType)).Length];
    // public RenderTexture[] VelocityTextures = new RenderTexture[System.Enum.GetValues(typeof(SurfaceType)).Length];
    // public RenderTexture[] DivergenceTextures = new RenderTexture[System.Enum.GetValues(typeof(SurfaceType)).Length];
    
    private Material paintShaderMaterial;
    private Vector4 stencilTexelSize;
    public Material paintedBoardMaterial;
    private MeshRenderer meshRenderer;
    
    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        paintedBoardMaterial = new Material(meshRenderer.sharedMaterial);
        meshRenderer.sharedMaterial = paintedBoardMaterial;
        
        CreateBufferTextures();
        paintShaderMaterial = new Material(paintShader);
    }

    private void OnDestroy()
    {
        if (stencilTexture == null)
        {
            return;
        }
        
        // for (var i = 0; i < DensityTextures.Length; i++)
        // {
        //     if (DensityTextures[i] != null)
        //     {
        //         DensityTextures[i].Release();
        //         DensityTextures[i] = null;
        //     }
        //     if (VelocityTextures[i] != null)
        //     {
        //         VelocityTextures[i].Release();
        //         VelocityTextures[i] = null;
        //     }
        //     if (DivergenceTextures[i] != null)
        //     {
        //         DivergenceTextures[i].Release();
        //         DivergenceTextures[i] = null;
        //     }
        // }
        for (var i = _bufferTextures.Length - 1; i >= 0; i--)
        {
            for (var j = _bufferTextures[i].Length - 1; j >= 0; j--)
            {
                if (_bufferTextures[i][j] != null)
                {
                    _bufferTextures[i][j].Release();
                    _bufferTextures[i][j] = null;
                }
            }
        }
        
        Destroy(paintShaderMaterial);
        Destroy(paintedBoardMaterial);
    }

    void Update()
    {
        if (stencilTexture == null)
        {
            return;
        }
        
        PaintByMouse();
        
        // Blur();  // これはこれで面白いが。
        
        CalcVelocity();
        
        Advect();
    }

    private void OnGUI()
    {
        DrawBufferForDebug();
    }

    private void CreateBufferTextures()
    {
        if (stencilTexture == null)
        {
            Debug.LogError("StencilTexture is not set.");
            return;
        }
        
        var width = stencilTexture.width;
        var height = stencilTexture.height;
        
        stencilTexelSize = new Vector4(width, height, 1f / width, 1f / height);
        
        for (var i = 0; i < Enum.GetValues(typeof(SurfaceType)).Length; i++)
        {
            // _bufferTextures[(int)BufferType.Density][i] = new RenderTexture(width, height, 0, GraphicsFormat.R8_UNorm);
            _bufferTextures[(int)BufferType.Density][i] = new RenderTexture(width, height, 0, GraphicsFormat.R16_SFloat);
            _bufferTextures[(int)BufferType.Density][i].Create();
            
            
            _bufferTextures[(int)BufferType.Velocity][i] = new RenderTexture(width, height, 0, GraphicsFormat.R8G8_UNorm);
            _bufferTextures[(int)BufferType.Velocity][i].Create();
            
            _bufferTextures[(int)BufferType.Divergence][i] = new RenderTexture(width, height, 0, GraphicsFormat.R8G8_SNorm);
            _bufferTextures[(int)BufferType.Divergence][i].Create();
        }
        
        ClearBuffer();
    }

    // デバッグ用にバッファを描画する
    private void DrawBufferForDebug()
    {
        // スクリーン左上
        var rect = new Rect(0, 0, 512, 512);
        GUI.DrawTexture(rect, _bufferTextures[(int)BufferType.Density][(int)_surface[(int)BufferType.Density][0]], ScaleMode.ScaleToFit, false, 1);

        // スクリーン右上
        var w = Screen.width - 512;
        rect = new Rect(w, 0, 512, 512);
        GUI.DrawTexture(rect, _bufferTextures[(int)BufferType.Velocity][(int)_surface[(int)BufferType.Velocity][0]], ScaleMode.ScaleToFit, false, 1);
    }
    
    private void PaintByMouse()
    {
        if (Input.GetMouseButton(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                var uv = hit.textureCoord;
                
                Paint(uv);
            }
        }
        
        // Paint(new Vector2(0.5f,0.5f));
        
        // 右クリックでバッファクリア
        if (Input.GetMouseButtonDown(1))
        {
            ClearBuffer();
        }

        // paintedBoardMaterial.SetTexture("_DensityTex", DensityTextures[(int)_densityDestination]);
        paintedBoardMaterial.SetTexture("_DensityTex", _bufferTextures[(int)BufferType.Density][(int)_surface[(int)BufferType.Density][1]]);
    }
    
    private void ClearBuffer()
    {
        var currentRT = RenderTexture.active;
        
        for (var i = 0; i < _bufferTextures.Length; i++)
        {
            for (var j = 0; j < _bufferTextures[i].Length; j++)
            {
                Graphics.SetRenderTarget(_bufferTextures[i][j]);
                GL.Clear(true, true, _clearColors[i]);
            }
        }
        
        RenderTexture.active = currentRT;
    }
    
    private void SwapBuffer(BufferType bufferType)
    {
        (_surface[(int)bufferType][0], _surface[(int)bufferType][1]) = (_surface[(int)bufferType][1], _surface[(int)bufferType][0]);
    }
    
    private (RenderTexture, RenderTexture) GetBuffer(BufferType bufferType)
    {
        return (_bufferTextures[(int)bufferType][(int)_surface[(int)bufferType][0]], _bufferTextures[(int)bufferType][(int)_surface[(int)bufferType][1]]);
    }
    
    private void Paint(Vector2 position)
    {
        var (src, dst) = GetBuffer(BufferType.Density);
        paintShaderMaterial.SetTexture("_StencilTex", stencilTexture);
        paintShaderMaterial.SetFloat("_Intensity", paintIntensity);
        paintShaderMaterial.SetFloat("_Radius", paintRadius);
        paintShaderMaterial.SetVector("_Center", position);
        paintShaderMaterial.SetVector("_DestinationTexelSize", stencilTexelSize);
        Graphics.Blit(src, dst, paintShaderMaterial, 0);
        
        SwapBuffer(BufferType.Density);
    }
    
    private void Blur()
    {
        var (src, dst) = GetBuffer(BufferType.Density);
        paintShaderMaterial.SetTexture("_StencilTex", stencilTexture);
        paintShaderMaterial.SetVector("_DestinationTexelSize", stencilTexelSize);
        Graphics.Blit(src, dst, paintShaderMaterial, 1);
        
        SwapBuffer(BufferType.Density);
    }

    private void CalcVelocity()
    {
        var (src, dst) = GetBuffer(BufferType.Velocity);
        var (densitySrc, _) = GetBuffer(BufferType.Density);
        
        paintShaderMaterial.SetTexture("_StencilTex", stencilTexture);
        paintShaderMaterial.SetVector("_DestinationTexelSize", stencilTexelSize);
        paintShaderMaterial.SetTexture("_DensityTex", densitySrc);
        paintShaderMaterial.SetVector("_Gravity", gravity * Time.deltaTime);
        Graphics.Blit(src, dst, paintShaderMaterial, 2);
        
        SwapBuffer(BufferType.Velocity);
    }
    
    private void Advect()
    {
        var (src, dst) = GetBuffer(BufferType.Density);
        var (velocitySrc, _) = GetBuffer(BufferType.Velocity);
        
        paintShaderMaterial.SetTexture("_StencilTex", stencilTexture);
        paintShaderMaterial.SetVector("_DestinationTexelSize", stencilTexelSize);
        paintShaderMaterial.SetTexture("_VelocityTex", velocitySrc);
        Graphics.Blit(src, dst, paintShaderMaterial, 3);
        
        SwapBuffer(BufferType.Density);
    }
}
