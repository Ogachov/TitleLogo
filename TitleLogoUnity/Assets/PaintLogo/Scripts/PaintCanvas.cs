using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[RequireComponent(typeof(MeshRenderer))]
public class PaintCanvas : MonoBehaviour
{
    [SerializeField]Texture2D stencilTexture;
    [SerializeField]Shader paintShader;

    [SerializeField]float paintRadius = 10f;
    [SerializeField]float paintIntensity = 1f;

    enum BufferType
    {
        Front,
        Back,
    } 
    
    private BufferType sourceBuffer = BufferType.Front;
    private BufferType destinationBuffer = BufferType.Back;
    // BufferTypeの配列を定義する
    public RenderTexture[] BufferTextures = new RenderTexture[System.Enum.GetValues(typeof(BufferType)).Length];
    
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
        
        for (var i = 0; i < BufferTextures.Length; i++)
        {
            if (BufferTextures[i] != null)
            {
                BufferTextures[i].Release();
                BufferTextures[i] = null;
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
        
        Blur();
    }

    private void OnGUI()
    {
        // DrawBufferForDebug();
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
        
        for (var i = 0; i < Enum.GetValues(typeof(BufferType)).Length; i++)
        {
            // RenderTextureを生成する
            BufferTextures[i] = new RenderTexture(width, height, 0, GraphicsFormat.R8G8B8A8_UNorm);
            // BufferTextures[i] = new RenderTexture(width, height, 0, GraphicsFormat.R16G16_SFloat);
            // BufferTextures[i].enableRandomWrite = true;
            BufferTextures[i].Create();
        }
        
        ClearBuffer(BufferType.Front);
        ClearBuffer(BufferType.Back);
    }

    // デバッグ用にバッファを描画する
    private void DrawBufferForDebug()
    {
        var rect = new Rect(0, 0, 512, 512);
        GUI.DrawTexture(rect, BufferTextures[(int)0], ScaleMode.ScaleToFit, false, 1);
        // rect.y += 272;
        rect.x += 512;
        GUI.DrawTexture(rect, BufferTextures[(int)1], ScaleMode.ScaleToFit, false, 1);
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
            ClearBuffer(BufferType.Front);
            ClearBuffer(BufferType.Back);
        }

        paintedBoardMaterial.SetTexture("_DensityTex", BufferTextures[(int)destinationBuffer]);
    }
    
    private void ClearBuffer(BufferType bufferType)
    {
        var currentRT = RenderTexture.active;
        Graphics.SetRenderTarget(BufferTextures[(int)bufferType]);
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = currentRT;
    }
    
    private void SwapBuffer()
    {
        (sourceBuffer, destinationBuffer) = (destinationBuffer, sourceBuffer);
    }
    
    private void Paint(Vector2 position)
    {
        paintShaderMaterial.SetTexture("_StencilTex", stencilTexture);
        paintShaderMaterial.SetFloat("_Intensity", paintIntensity);
        paintShaderMaterial.SetFloat("_Radius", paintRadius);
        paintShaderMaterial.SetVector("_Center", position);
        paintShaderMaterial.SetVector("_DestinationTexelSize", stencilTexelSize);
        Graphics.Blit(BufferTextures[(int)sourceBuffer], BufferTextures[(int)destinationBuffer], paintShaderMaterial, 0);
        
        SwapBuffer();
    }
    
    private void Blur()
    {
        paintShaderMaterial.SetTexture("_StencilTex", stencilTexture);
        paintShaderMaterial.SetVector("_DestinationTexelSize", stencilTexelSize);
        Graphics.Blit(BufferTextures[(int)sourceBuffer], BufferTextures[(int)destinationBuffer], paintShaderMaterial, 1);
        
        SwapBuffer();
    }
}
