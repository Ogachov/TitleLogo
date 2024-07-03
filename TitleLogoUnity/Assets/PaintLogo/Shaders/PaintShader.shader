Shader "Logo/PaintShader"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }
    
    SubShader {
        // Brush
        Pass {
            ZTest Always Cull Off ZWrite Off
            
            ColorMask R

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Intensity;   // 0~1
            float _Radius;      // pixel単位
            float2 _Center;     // uv座標
            float4 _DestinationTexelSize;   // width, height

            sampler2D _MainTex;
            sampler2D _StencilTex;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            float noise(float2 p)
            {
                return hash(p.x + p.y * 57.0);
            }

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float n = noise(i.uv * _Time.y);
                float stencil = tex2D(_StencilTex, i.uv).r * step(0.99, n);
                float src = tex2D(_MainTex, i.uv).r;
                
                float2 center = _Center * _DestinationTexelSize;
                float2 uv = i.uv * _DestinationTexelSize;
                float dist = length(uv - center);
                float alpha = src + saturate((1.0 - dist / _Radius) * stencil * _Intensity);

                return half4(alpha, 0, 0, 1);
            }
            ENDCG

        }

        // Smoothing
        Pass {
            ZTest Always Cull Off ZWrite Off
            ColorMask R

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _DestinationTexelSize;   // width, height
            
            sampler2D _MainTex;
            sampler2D _StencilTex;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float stencil = tex2D(_StencilTex, i.uv).r;
                float src = tex2D(_MainTex, i.uv).r * 4.;

                // _MainTexの周囲のテクスチャをサンプリングして平滑化する
                src += tex2D(_MainTex, i.uv + float2(0, 1) * _DestinationTexelSize.zw).r * 2.;
                src += tex2D(_MainTex, i.uv + float2(0, -1) * _DestinationTexelSize.zw).r * 2.;
                src += tex2D(_MainTex, i.uv + float2(1, 0) * _DestinationTexelSize.zw).r * 2.;
                src += tex2D(_MainTex, i.uv + float2(-1, 0) * _DestinationTexelSize.zw).r * 2.;
                src += tex2D(_MainTex, i.uv + float2(1, 1) * _DestinationTexelSize.zw).r;
                src += tex2D(_MainTex, i.uv + float2(-1, -1) * _DestinationTexelSize.zw).r;
                src += tex2D(_MainTex, i.uv + float2(1, -1) * _DestinationTexelSize.zw).r;
                src += tex2D(_MainTex, i.uv + float2(-1, 1) * _DestinationTexelSize.zw).r;
                src /= 16.;
                
                return half4(src * stencil, 0, 0, 1);
            }
            ENDCG

        }

        // Velocity
        Pass {
            ZTest Always Cull Off ZWrite Off
            ColorMask RG

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _DestinationTexelSize;   // width, height
            float3 _Gravity;
            
            sampler2D _MainTex;
            sampler2D _StencilTex;
            sampler2D _DensityTex;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Velocityはスタッガード格子法で計算する
            half4 frag (v2f i) : SV_Target
            {
                float stencil = tex2D(_StencilTex, i.uv).r;
                float2 v = tex2D(_MainTex, i.uv).rg * 2 - 1;
                float d = tex2D(_DensityTex, i.uv).r;
                float dl = tex2D(_DensityTex, i.uv - float2(1,0) * _DestinationTexelSize.zw).r;
                float dt = tex2D(_DensityTex, i.uv - float2(0,1) * _DestinationTexelSize.zw).r;
                
                v.x += (d + dl) * 0.5 * _Gravity.x;
                v.y += (d + dt) * 0.5 * -_Gravity.y;
                v = v * stencil * 0.5 + 0.5;
                
                return half4(v, 0, 1);
            }
            ENDCG

        }

        // Advection
        Pass {
            ZTest Always Cull Off ZWrite Off
            ColorMask RG

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _DestinationTexelSize;   // width, height
            
            sampler2D _MainTex;
            sampler2D _StencilTex;
            sampler2D _VelocityTex;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Velocityはスタッガード格子法で計算する
            half4 frag (v2f i) : SV_Target
            {
                float stencil = tex2D(_StencilTex, i.uv).r;
                float d = tex2D(_MainTex, i.uv).r;
                // dが0では無い場所は以後０にしないための値(=1/255) 
                float painted = (1 - step(0, d)) * (1/255);
                float dl = tex2D(_MainTex, i.uv - float2(1,0) * _DestinationTexelSize.zw).r;
                float dr = tex2D(_MainTex, i.uv + float2(1,0) * _DestinationTexelSize.zw).r;
                float dt = tex2D(_MainTex, i.uv - float2(0,1) * _DestinationTexelSize.zw).r;
                float db = tex2D(_MainTex, i.uv + float2(0,1) * _DestinationTexelSize.zw).r;

                float2 v = tex2D(_VelocityTex, i.uv).rg * 2 - 1;    // 左と上の速度
                float2 vr = tex2D(_VelocityTex, i.uv + float2(1,0) * _DestinationTexelSize.zw).rg * 2 - 1;    // r 右の速度
                float2 vb = tex2D(_VelocityTex, i.uv + float2(0,1) * _DestinationTexelSize.zw).rg * 2 - 1;    // b 下の速度
                
                d = saturate(d + dl * v.x - dr * vr.x + dt * v.y - db * vb.y + painted) * stencil;
                
                return half4(d, 0, 0, 1);
            }
            ENDCG

        }
    }
    Fallback Off
}
