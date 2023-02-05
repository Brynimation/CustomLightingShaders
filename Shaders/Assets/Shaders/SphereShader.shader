// This shader draws a texture on the mesh.
Shader "Custom/Sphere"
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
                o.uv = i.uv;
                o.radius = 1.0;
                o.positionWS = mul(unity_ObjectToWorld, i.positionOS);
                o.positionHCS = TransformObjectToHClip(i.positionOS);
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
               
                float3 rayOrigin = _CameraPosition;
                float3 rayDir = _CameraPosition - i.positionWS;
                float3 toSphere = rayOrigin - i.positionWS;
                float4 colour = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float a = dot(rayDir, rayDir);
                float b = 2 * dot(toSphere, rayDir);
                float c = dot(toSphere, toSphere) - i.radius * i.radius;
                float D = b * b - 4 * a * c;
                if(D >= 0)
                {
                    return colour;
                }else{
                    return 0x000000;
                }


                return colour;
            }
            ENDHLSL
        }
    }
}