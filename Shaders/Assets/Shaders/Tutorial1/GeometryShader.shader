// This shader draws a texture on the mesh.
Shader "Tutorial/GeometryShader"
{
    /*Properties are attributes of a shader that are set per material, meaning that multiple
    materials with the same shader can be customised to look different.*/
    Properties
    { 
        [MainTexture]   _BaseMap("Base Map", 2D) = "white"{}
        //MainColor attribute allows us to use the material.color property
        [MainColor] _Tint("Tint", Color) = (1, 1, 1, 1)
        _Radius("Radius", float) = 1.0
    }
    SubShader
    {

        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }

        Pass
        {
            Name "ForwardLit" //This pass will be for rendering the colour of an object.
            Tags{"LightMode" = "UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            float _Radius;
            //input to vertex shader
            struct Attributes
            {
                float4 positionOS: POSITION; //position in object space
                float4 diffuseColour : COLOR;
                float2 uv : TEXCOORD0;
            };

            //input to geometry shader
            struct GeomData
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };
            //input to fragment shader
            struct Interpolators
            {
                float4 positionHCS  : SV_POSITION; //position in clip space.
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
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
            GeomData vert(Attributes input)
            {
                GeomData output;
                input.positionOS = input.positionOS + float4(10, 0.0, 0.0, 0.0)* _Time;
                output.positionOS = input.positionOS;
                float4 worldSpacePos = mul(unity_ObjectToWorld, input.positionOS); //Transform vertex position to world space
                output.positionWS = worldSpacePos;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            [maxvertexcount(8)]
            void geom2(point GeomData inputs[1], inout PointStream<Interpolators> outputstream)
            {
                float numPoints = 8;
                float angleIncrement = radians(360)/(float) numPoints;
                float3 centreWS = inputs[0].positionWS;
                float2 centreUV = inputs[0].uv;
                for(int i = 0; i < numPoints; i++)
                {
                    Interpolators o;
                    float angle = angleIncrement * i;
                    float r = 1.0;
                    float x = r * cos(angle);
                    float y = r * sin(angle);
                    float3 os = centreWS + float3(x,y,0);
                    o.positionWS = mul(unity_ObjectToWorld, os);//centreWS + os;
                    o.positionHCS = TransformObjectToHClip(os);
                    o.uv = centreUV;

                    outputstream.Append(o);
                }
                outputstream.RestartStrip();
            }
            [maxvertexcount(100)]
            void geom(point GeomData inputs[1], inout TriangleStream<Interpolators> outputstream)
            {
        
                const int numPoints = 8;
                float angleIncrement = radians(360)/(float) numPoints;

                float3 centreOS = inputs[0].positionOS;
                float3 centreWS = inputs[0].positionWS;
                float2 centreUV = inputs[0].uv;


                Interpolators centre;
                centre.positionWS = centreWS;
                centre.uv = centreUV;
                centre.positionHCS = TransformObjectToHClip(centreOS);
                Interpolators positions[numPoints];
                for(int i = 0; i < numPoints; i++)
                {
                    Interpolators o;
                    float angle = angleIncrement * i;
                    float r = 2.0;
                    float x = r * cos(angle);
                    float y = r * sin(angle);
                    float3 os = centreOS + float3(x,y,0);
                    o.positionWS = mul(unity_ObjectToWorld, os);//centreWS + os;
                    o.positionHCS = TransformObjectToHClip(os);
                    o.uv = centreUV;

                    positions[i] = o;
                }
                for(int i = 0; i < numPoints; i++)
                {
                    outputstream.Append(positions[i]);
                    outputstream.Append(positions[(i + 1)%numPoints]);
                    outputstream.Append(centre);
                    
                    outputstream.RestartStrip();
                }
                
            }
            
            half4 frag(Interpolators input) : SV_Target
            {
                // The SAMPLE_TEXTURE2D marco samples the texture with the given
                // sampler.
                half4 colour = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                return colour;
            }
            ENDHLSL
        }
    }
}