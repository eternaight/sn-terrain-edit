Shader "Triplanar Mapped"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _BlendSharpness ("Sharpness", Range(0, 1)) = 1
        _Scale("Scale", int) = 1
        _AmbColor("Ambient Color", Color) = (0, 0, 0, 1.0)
        _Color("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members worldPos)
//#pragma exclude_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL; 
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : NORMAL;
            };

            sampler2D _MainTex;
            sampler2D _BumpMap;
            fixed4 _AmbColor;
            float4 _MainTex_ST;
            float _BlendSharpness;
            float _Scale;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = v.normal;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float2 uvX = i.worldPos.zy;
                float2 uvY = i.worldPos.xz;
                float2 uvZ = i.worldPos.xy;

                half3 tnormalX = UnpackNormal(tex2D(_BumpMap, uvX));
                half3 tnormalY = UnpackNormal(tex2D(_BumpMap, uvY));
                half3 tnormalZ = UnpackNormal(tex2D(_BumpMap, uvZ));

                // Swizzle world normals into tangent space and apply Whiteout blend
                tnormalX = half3(
                    tnormalX.xy + i.normal.zy,
                    abs(tnormalX.z) * i.normal.x
                    );
                tnormalY = half3(
                    tnormalY.xy + i.normal.xz,
                    abs(tnormalY.z) * i.normal.y
                    );
                tnormalZ = half3(
                    tnormalZ.xy + i.normal.xy,
                    abs(tnormalZ.z) * i.normal.z
                    );

                float3 blendWeight = pow(abs(i.normal), _BlendSharpness);
                blendWeight /= dot(blendWeight, 1);
                    
                // Swizzle tangent normals to match world orientation and triblend
                half3 worldNormal = normalize(
                    tnormalX.zyx * blendWeight.x +
                    tnormalY.xzy * blendWeight.y +
                    tnormalZ.xyz * blendWeight.z
                    );

                fixed4 colX = tex2D(_MainTex, uvX * _Scale);
                fixed4 colY = tex2D(_MainTex, uvY * _Scale);
                fixed4 colZ = tex2D(_MainTex, uvZ * _Scale);

                float4 col = colX * blendWeight.x + colY * blendWeight.y + colZ * blendWeight.z;
                float lightPercent = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));

                return (_AmbColor + lightPercent * col) * _Color;
            }
            ENDCG
        }
    }
}
