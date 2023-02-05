// This shader draws a texture on the mesh.
Shader "Custom/Sphere2"
{
    // The _BaseMap variable is visible in the Material's Inspector, as a field
    // called Base Map.
    Properties
    { 
        _MainTex("Base Map", 2D) = "white"{}//Texture declarations must end with a {}
        _Tint("Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        _Alpha("Alpha", Float) = 1.0
        _CameraPosition("Camera Position", Vector) = (0.0,0.0,0.0)
        _CameraDirection("CameraDirection", Vector) = (0.0, 0.0, 1.0)
    }

    SubShader
    {
    /*By setting the queue to trasnparent + 500, we ensure that the billboard shader is drawn after even the 
    transparent objects, which are themselves drawn after the opaque objects.*/
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalRenderPipeline" "Queue" = "Transparent"}

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Ray{
                float3 origin;
                float3 direction;
            };

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint id : SV_VERTEXID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float radius : TEXCOORD1;
                float4 positionWS : TEXCOORD2;
            };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END
            uniform float4 _Tint;
            uniform float4 _Alpha;
            uniform float3 _CameraPosition;

            Varyings vert(Attributes i)
            {
                Varyings o;
                float f = i.id;
                float v = f - 6.0 * floor(f/6.0);
                f = (f - v) / 6.;
                float a = f - 64.0 * floor(f/64.0);
                f = (f-a) / 64.;
                float b = f-16.;
                a += (v - 2.0 * floor(v/2.0));
                b += v==2. || v>=4. ? 1.0 : 0.0;
                a = a/64.*6.28318;
                b = b/64.*6.28318;
                o.uv = i.uv;
                float3 p = float3(cos(a)*cos(b), sin(b), sin(a)*cos(b));
                o.positionWS = mul(unity_ObjectToWorld, p);
                o.radius = 1.0;
                o.positionHCS = TransformObjectToHClip(float4(p, 1.0));
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float3 dx = ddx_fine(i.positionHCS);
                float3 dy = ddy_fine(i.positionHCS);
                float3 light1 = normalize(float3(5,0,100));
                float3 light2 = normalize(float3(-100,0,-102));
                float3 light3 = normalize(float3(100,0,-100));  
                float3 normal = normalize(cross(dx,dy));
                float3 diffuse1 = max(dot(light1,normal),0.0) * float3(0.9,0.0,0.0);
                float3 diffuse2 = max(dot(light2,normal),0.0) * float3(0.0,0.9,0.0);
                float3 diffuse3 = max(dot(light3,normal),0.0) * float3(0.0,0.0,1.0);
                return float4(diffuse1+diffuse2+diffuse3,1.0);
            }
            ENDHLSL
        }
    }
}