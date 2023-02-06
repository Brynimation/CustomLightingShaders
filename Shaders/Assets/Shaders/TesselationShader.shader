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
        _EdgeFactorA("Edge 1 Factor", Float) = 1.0
        _EdgeFactorB("Edge 2 Factor", Float) = 1.0
        _EdgeFactorC("Edge 3 Factor", Float) = 1.0
        _InsideFactor("Inside Factor", Float) = 1.0
        _OtherFloat("Other float", Float) = 2.0
    }

    SubShader
    {
    /*By setting the queue to trasnparent + 500, we ensure that the billboard shader is drawn after even the 
    transparent objects, which are themselves drawn after the opaque objects.*/
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent"}
        HLSLINCLUDE
        #pragma target 5.0


        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"            
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
        #pragma vertex vert
        #pragma hull Hull
        #pragma domain Domain
        #pragma fragment frag


            
        //Input to vertex shader
        struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS: NORMAL0;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct TessellationFactors
        {
            float edge[3] :SV_TessFactor; //stores the number of times each edge is subdivided
            float inside : SV_InsideTessFactor; //the square of this number roughly equals the number of divisions made to the triangle.
        };
        //Output of vertex shader, input to hull shader. Can contain any data needed for later on in the pipeline
        struct TessellationControlPoint
        {
            float3 positionWS : INTERNALTESSPOS;
            float3 normalWS: NORMAL0;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        /*Output of patch constant shader, input to domain shader. The edge array stores
        The tessellation factors of each edge, which determines how many times to subdivide
        each edge. Each edge is opposite its corresponding vertex (ie, edge 0 is between)
        vertex 1 and 2*/

        //output of fragment shader
        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float3 positionWS : TEXCOORD1;
            float3 normalWS : TEXCOORD2;
            float2 uv: TEXCOORD3;
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
        uniform float _EdgeFactorA;
        uniform float _EdgeFactorB;
        uniform float _EdgeFactorC;
        uniform float _InsideFactor;
        uniform float _OtherFloat;
        ENDHLSL
        Pass
        {
            /*In the vertex shader, we just convert the object space 
            positions and normals to world space*/
            HLSLPROGRAM
            TessellationControlPoint vert(Attributes i)
            {
                TessellationControlPoint o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(i.positionOS); //This is a built in function that gets the position of a vertex in different spaces
                VertexNormalInputs normalInputs = GetVertexNormalInputs(i.normalOS); //same as above but for normals
                o.positionWS = posInputs.positionWS;
                o.normalWS = normalInputs.normalWS;
                o.uv = i.uv;
                return o;
            }

             TessellationFactors PatchConstantFunction(InputPatch<TessellationControlPoint, 3> patch)
            {
                UNITY_SETUP_INSTANCE_ID(patch[0]);
                TessellationFactors o;
                o.edge[0] = _OtherFloat;
                o.edge[1] = _OtherFloat;
                o.edge[2] = _OtherFloat;
                o.inside = _OtherFloat;
                return o;
            }



            /*The hull function receives data as patches; lists of vertices
            that make up some defined primitive, ie: a triangle. It runs once per
            vertex per patch. Can be used to modify data based on values in the entire primitive*/
            [domain("tri")] //signals that we're inputting triangles - determines the input patch type
            [outputcontrolpoints(3)] //Triangles have 3 points
            [outputtopology("triangle_cw")] //signals that we're outputting three triangles - determines the output patch type
            [patchconstantfunc("PatchConstantFunction")] //register the patch constant function
            [partitioning("integer")] //select a partitioning algorithm: Integer, fractional_odd, fractional_even or pow2
            TessellationControlPoint Hull(
                InputPatch<TessellationControlPoint, 3> patch, //input triangle
                uint id : SV_OutputControlPointID //vertex index on triangle - signals which vertex on the patch to output data for
            )
            {
                return patch[id];    
            }
            //runs once per patch


        #define BARYCENTRIC_INTERPOLATE(fieldName) \
		        patch[0].fieldName * barycentricCoordinates.x + \
		        patch[1].fieldName * barycentricCoordinates.y + \
		        patch[2].fieldName * barycentricCoordinates.z

            [domain("tri")]
            Varyings Domain
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
                o.uv = patch[0].uv;
                return o;

            }

            /* struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            }; */
            
            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return baseTex;
            }
            ENDHLSL
        }
    }
}