// This shader draws a texture on the mesh.
Shader "Tutorial/PerFragmentLighting"
{
    /*Properties are attributes of a shader that are set per material, meaning that multiple
    materials with the same shader can be customised to look different.*/
    Properties
    { 
        [MainTexture]   _BaseMap("Base Map", 2D) = "white"{}
        //MainColor attribute allows us to use the material.color property
        [MainColor] _Tint("Tint", Color) = (1, 1, 1, 1)
        _LightPosition("LightPosition", Vector) = (0.0,0.0,0.0)
        _LightIntensity("Light intensity", Color) = (1, 0.5, 0.0, 1.0)
        _AmbientLightIntensity("Ambient light intensity", Color) = (0.1, 0.0, 0.5, 0.05)
    }

    /*A shader may define any number of subshaders. Subshaders allow different code
    to be run depending on the platform/render pipeline in use.*/
    SubShader
    {
    /*Tags are used to hold user-defined metadata.*/
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
        /*A pass is the fundamental element of a shader object. It contains instructions for
        setting the state of the gpu.
        Each pass has a specific purpose in rendering a scene to the screen. For instance, one 
        pass may be used to calculate lighting, one for shadows, etc.
        */
        Pass
        {
            Name "ForwardLit" //This pass will be for rendering the colour of an object.
            Tags{"LightMode" = "UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            /*Any properties we define in ShaderLab (in the properties block) we must also define
            within the HLSL program.*/
            float4 _Tint;
            float3 _LightPosition;
            float4 _LightIntensity;
            float4 _AmbientLightIntensity;
            struct Attributes
            {
                float4 positionOS:POSITION; //position in object space
                float4 diffuseColour :COLOR;
                float3 normal : NORMAL;
                float2 uv:TEXCOORD0;
            };

            struct Interpolators
            {
                float4 positionHCS  : SV_POSITION; //position in clip space.
                float3 normalWS : NORMAL;
                float3 positionWS : POSITION1;
                float4 diffuseColour : COLOR;
                float2 uv : TEXCOORD0;
            };

            // This macro declares _BaseMap as a Texture2D object.
            TEXTURE2D(_BaseMap);
            // This macro declares the sampler for the _BaseMap texture. The sampler defines how to read the texture..
            SAMPLER(sampler_BaseMap); //The naming is important: sample_TextureName

            CBUFFER_START(UnityPerMaterial)
                // The following line declares the _BaseMap_ST variable, so that you
                // can use the _BaseMap variable in the fragment shader. The _ST 
                // suffix is necessary for the tiling and offset function to work.
                // The xy components of the _BaseMap_ST variable store the x and y scales. The zw components hold the x and y offsets.
                float4 _BaseMap_ST;
            CBUFFER_END

            /*Vertex function. Runs for every vertex on a mesh. Converts vertex object space positions to clip space positions.*/
            Interpolators vert(Attributes input)
            {
                Interpolators output;
                float4 worldSpacePos = mul(unity_ObjectToWorld, input.positionOS); //Transform vertex position to world space
                float3 normWorldSpace = mul(unity_ObjectToWorld, input.normal); //transform vertex normal to world space
                //We transform the normal and vertex to world space so we can do calculations with the dirToLight vector, 
                //which is defined in terms of the world space position of the light source.
                output.positionHCS = TransformObjectToHClip(input.positionOS);
                output.normalWS = normWorldSpace;
                output.positionWS = worldSpacePos;
                output.diffuseColour = input.diffuseColour;
                // The TRANSFORM_TEX macro performs the tiling and offset
                // transformation.
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            /*Fragment function. Runs for every pixel on the screen. Returns a pixel colour.
            The SV_Target semantic represents the final pixel colour. When tagging a function with
            a semantic, the compiler interprets the return value as having this semantic.*/
            half4 frag(Interpolators input) : SV_Target
            {
                // The SAMPLE_TEXTURE2D marco samples the texture with the given
                // sampler.
                float3 dirToLight = normalize(_LightPosition - input.positionWS);
                float3 normalWS = normalize(input.normalWS);
                half4 colour = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float4 lightingColour = (input.diffuseColour * _AmbientLightIntensity) + (_LightIntensity * input.diffuseColour * clamp(dot(normalWS, dirToLight), 0, 1));
                return lightingColour * colour *_Tint;
            }
            ENDHLSL
        }
    }
}