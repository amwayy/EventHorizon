using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class GPUComputeHelper : MonoBehaviour
    {
        [SerializeField] private ComputeShader pixelCountShader;
        
        public static GPUComputeHelper Instance { get; private set; }
        
        private ComputeBuffer _pixelCountResultBuffer;

        private void Awake()
        {
            Instance = this;
            
            _pixelCountResultBuffer = new ComputeBuffer(1, sizeof(uint));
        }

        private void OnDestroy()
        {
            _pixelCountResultBuffer?.Release();
        }

        public int GetPixelCount(RenderTexture rt)
        {
            int kernel = pixelCountShader.FindKernel("CSMain");

            uint[] init = new uint[1] { 0 };
            _pixelCountResultBuffer.SetData(init);

            pixelCountShader.SetTexture(kernel, "Input", rt);
            pixelCountShader.SetInt("Width", rt.width);
            pixelCountShader.SetInt("Height", rt.height);
            pixelCountShader.SetFloat("AlphaThreshold", 0.01f);

            pixelCountShader.SetBuffer(kernel, "Result", _pixelCountResultBuffer);

            int threadGroupX = Mathf.CeilToInt(rt.width / 8.0f);
            int threadGroupY = Mathf.CeilToInt(rt.height / 8.0f);

            pixelCountShader.Dispatch(kernel, threadGroupX, threadGroupY, 1);

            uint[] result = new uint[1];
            _pixelCountResultBuffer.GetData(result);

            return (int)result[0];
        }
    }
}