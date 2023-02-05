// This shader draws a texture on the mesh.
Shader "Custom/Billboard"
{
    // The _BaseMap variable is visible in the Material's Inspector, as a field
    // called Base Map.
    Properties
    { 
        _MainTex("Base Map", 2D) = "white"{}//Texture declarations must end with a {}
        _Tint("Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        _Alpha("Alpha", Float) = 1.0
    }

    SubShader
    {
    /*By setting the queue to trasnparent + 500, we ensure that the billboard shader is drawn after even the 
    transparent objects, which are themselves drawn after the opaque objects.*/
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalRenderPipeline" "Queue" = "Transparent"}

        Pass
        {
            ZTest off //Always draw the sprite to the screen
            /*The Blend keyword determines how the GPU combines the output of the fragment shader with the render 
            target */
            Blend srcAlpha OneMinusSrcAlpha
            cull off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"            

            struct Attributes
            {
                float4 positionOS : POSITION;
                // The uv variable contains the UV coordinate on the texture for the
                // given vertex.
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                // The uv variable contains the UV coordinate on the texture for the
                // given vertex.
                float2 uv : TEXCOORD0;
            };

            // This macro declares _BaseMap as a Texture2D object.
            TEXTURE2D(_MainTex);
            // This macro declares the sampler for the _BaseMap texture.
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                // The following line declares the _BaseMap_ST variable, so that you
                // can use the _BaseMap variable in the fragment shader. The _ST 
                // suffix is necessary for the tiling and offset function to work.
                float4 _MainTex_ST;
            CBUFFER_END
            uniform float4 _Tint;
            uniform float4 _Alpha;

            Varyings vert(Attributes i)
            {
                Varyings o;
                float4 ObjectWSPos = mul(unity_ObjectToWorld, float4(0,0,0,1));
                float4 ObjectVSPos = mul(UNITY_MATRIX_MV, float4(0,0,0,1));
                //float4 ObjectVSPos = float4(UnityObjectToViewPos(float3(0,0,0), 1));

                float4 worldPos = mul(unity_ObjectToWorld, i.positionOS);
                float4 flippedWorldPos = float4(-1, 1, -1, 1) * (worldPos - ObjectWSPos) + ObjectWSPos;
                float4 viewPos = (flippedWorldPos - ObjectWSPos) + ObjectVSPos;

                float4 clipPos = mul(UNITY_MATRIX_P, viewPos);

                o.positionHCS = clipPos;//TransformObjectToHClip(i.positionOS);
                // The TRANSFORM_TEX macro performs the tiling and offset
                // transformation.
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                // The SAMPLE_TEXTURE2D marco samples the texture with the given
                // sampler.
                float4 colour = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                //colour.a = _Alpha;
                return colour;
            }
            ENDHLSL
        }
    }
}