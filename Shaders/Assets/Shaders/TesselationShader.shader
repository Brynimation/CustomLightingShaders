// This shader draws a texture on the mesh.
Shader "Custom/Tesselation"
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
            HLSLPROGRAM
            #pragma target 5.0
            #pragma hull hull
            #pragma domain domain
            #pragma vertex vert
            #pragma fragment frag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
                #define BARYCENTRIC_INTERPOLATE(fieldName)\
                    patch[0].fieldName * barycentricCoordinates.x + \
                    patch[1].fieldName * barycentricCoordinates.y + \
                    patch[2].fieldName * barycentricCoordinates.z
            
            //Input to vertex shader
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS: NORMAL0;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            //Output of vertex shader, input to domain shader
            struct TessellationControlPoint
            {
                float4 positionWS : INTERNALTESSPOS;
                float3 normalWS: NORMAL0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            /*Output of patch constant shader, input to domain shader. The edge array stores
            The tessellation factors of each edge, which determines how many times to subdivide
            each edge. Each edge is opposite its corresponding vertex (ie, edge 0 is between)
            vertex 1 and 2*/
            struct TessellationFactors
            {
                float edge[3] :SV_TessFactor; //stores the number of times each edge is subdivided
                float inside : SV_InsideTessFactor; //the square of this number roughly equals the number of divisions made to the triangle.
            };
            //output of fragment shader
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
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

            /*In the vertex shader, we just convert the object space 
            positions and normals to world space*/
            TessellationControlPoint vert(Attributes i)
            {
                TessellationControlPoint o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                
                o.positionWS = mul(unity_ObjectToWorld, i.positionOS);
                o.normalWS = mul(unity_ObjectToWorld, i.normalOS);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }



            /*The hull function receives data as patches; lists of vertices
            that make up some defined primitive, ie: a triangle. It runs once per
            vertex per patch*/
            [domain("tri")] //signals that we're inputting triangles - determines the input patch type
            [outputcontrolpoints(3)] //Triangles have 3 points
            [outputtopology("triangle_cw")] //signals that we're outputting three triangles - determines the output patch type
            [patchconstantfunc("PatchConstantFunction")] //register the patch constant function
            [partitioning("integer")] //select a partitioning algorithm: Integer, fractional_odd, fractional_even or pow2
            TessellationControlPoint hull(
                InputPatch<TessellationControlPoint, 3> patch, //input triangle
                uint id : SV_OutputControlPointID //vertex index on triangle - signals which vertex on the patch to output data for
            )
            {
                return patch[id];    
            }
            //runs once per patch
            TessellationFactors PatchConstantFunction(InputPatch<TessellationControlPoint, 3> patch)
            {
                UNITY_SETUP_INSTANCE_ID(patch[0]);
                TessellationFactors o;
                o.edge[0] = 1;
                o.edge[1] = 1;
                o.edge[2] = 1;
                o.inside = 1;
                return o;
            }

            [domain("tri")]

            Varyings domain
            (
                TessellationFactors factors, //output of the patch constant function
                OutputPatch<TessellationControlPoint, 3> patch,//input triangle; output of hull function
                float3 barycentricCoordinates : SV_DomainLocation //The barycentric coordinates of the vertex on the triangle
            )
            {
                Varyings o;
                //Setup instancing and stereo support for VR.
                UNITY_SETUP_INSTANCE_ID(patch[0]);
                UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 positionWS = BARYCENTRIC_INTERPOLATE(positionWS);
                float3 normalWS = BARYCENTRIC_INTERPOLATE(normalWS);
                o.positionHCS = TransformWorldToHClip(positionWS);
                o.normalWS = normalWS;
                o.positionWS = positionWS;
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