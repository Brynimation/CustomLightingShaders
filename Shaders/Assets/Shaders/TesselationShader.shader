/*Tessellation can be expesnive. One reason for this is that it does not 
automatically take advantage of frustrum or winding culling, as this is performed
in the rasterization stage of the graphics pipeline, which is after the hull and
domain functions are run.
However, we can implement this ourselves. We just set the tessellation factor to
zero so that the tessellator ignores that specific patch(remember a patch is just
a primitive - a triangle).*/
Shader "Custom/Tesselation"
{
    // The _BaseMap variable is visible in the Material's Inspector, as a field
    // called Base Map.
    Properties
    { 
        _MainTex("Base Map", 2D) = "white"{}//Texture declarations must end with a {}
        _Tint("Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        _Alpha("Alpha", Float) = 1.0
        /*The compiler splits the patch constant function and calculates each edge Factor
        in parallel in a bid to speed things up. This sometimes causes issues, which is
        why we use a vector3 to hold all 3 edge factors.*/
        _EdgeFactor("Edge Factors", Vector) = (1.0, 1.0, 1.0, 0.0)
        _InsideFactor("Inside Factor", Float) = 1.0
        [KeywordEnum(INTEGER, FRAC_EVEN, FRAC_ODD, POW2)] _PARTITIONING("Partition algoritm", Float) = 0
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

        #ifndef TESSELLATION_FACTORS_INCLUDED
        #define TESSELLATION_FACTORS_INCLUDED

        #pragma shader_feature_local _PARTITIONING_INTEGER _PARTITIONING_FRAC_EVEN _PARTITIONING_FRAC_ODD _PARTITIONING_POW2
            
        //Input to vertex shader
        struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS: NORMAL0;
            float2 uv : TEXCOORD0;
            float4 positionCS : TEXCOORD1;
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
        bool IsOutOfBounds(float3 p, float3 lower, float3 upper)
        {
            return p.x < lower.x || p.x > upper.x
            ||     p.y < lower.y || p.y > upper.y
            ||     p.z < lower.z || p.z > upper.z;
        }
        bool IsPointOutOfFrustum(float4 positionCS)
        {
            /*The w component of a clipspace position contains the outer bounds of
            the camera viewing frustum. The UNITY_RAW_FAR_CLIP_PLANE variable
            ensures that this works correctly on all grapics APIs.*/
            float3 culling = positionCS.xyz;
            float w = positionCS.w;
            float3 lower = float3(-w, -w, -w * UNITY_RAW_FAR_CLIP_PLANE);
            float3 upper = float3(w, w, w);
            return IsOutOfBounds(culling, lower, higher);
        }

        bool ShouldCullPatch(float4 vertACS, float4 vertBCS, float4 vertCCS)
        {
            bool isOutside = IsPointOutOfFrustum(vertACS) && 
            IsPointOutOfFrustum(vertBCS) &&
            IsPointOutOfFrustum(vertCCS);
            return isOutside && shouldBackFaceCull(vertACS, vertBCS, vertCCS);
        }
        bool shouldBackFaceCull(float4 posACS, float4 posBCS, float4 posCCS)
        {
            /*To determine if we should backface cull, we need to determine the
            normal direction of the plane containing the triangle. We achieve 
            this by taking the cross product of two vectors that are parallel to
            the plane containing the triangle.*/
            float3 a = posACS.xyz / posACS.w; //Since we're in clip space, we divide by w to apply perspective and to normalize
            float3 b = posBCS.xyz / posBCS.w;
            float3 c = posCCS.xyz / posCCS.w;
            float3 normalToFace = cross(b-a, c-a);
            /*If the dot product of the normal and the camera's view direction, 
            which is always along the forward z axis in clip space, is less than zero,
            then the direction vectors are more than 90 degrees apart and hence we cull
            the face'*/
            return dot(normalToFace, float3(0, 0, 0) < 0);
        }
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
        uniform float3 _EdgeFactor;
        uniform float _InsideFactor;
        #endif
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
                o.positionCS = posInputs.CS;
                o.uv = i.uv;
                return o;
            }

             TessellationFactors PatchConstantFunction(InputPatch<TessellationControlPoint, 3> patch)
            {
                UNITY_SETUP_INSTANCE_ID(patch[0]);
                TessellationFactors o;
                if(ShouldCullPatch(patch[0], patch[1], patch[2])){
                    o.edge[0] = o.edge[1] = o.edge[2] = o.inside = 0; 
                }else{
                    o.edge[0] = _EdgeFactor.x;
                    o.edge[1] = _EdgeFactor.y;
                    o.edge[2] = _EdgeFactor.z;
                    o.inside = _InsideFactor;
               }

                return o;
            }

            /*The hull function receives data as patches; lists of vertices
            that make up some defined primitive, ie: a triangle. It runs once per
            vertex per patch. Can be used to modify data based on values in the entire primitive*/
            [domain("tri")] // Signal we're inputting triangles
            [outputcontrolpoints(3)] // Triangles have three points
            [outputtopology("triangle_cw")] // Signal we're outputting triangles
            [patchconstantfunc("PatchConstantFunction")] // Register the patch constant function
            // Select a partitioning mode based on keywords
            #if defined(_PARTITIONING_INTEGER)
            [partitioning("integer")]
            #elif defined(_PARTITIONING_FRAC_EVEN)
            [partitioning("fractional_even")]
            #elif defined(_PARTITIONING_FRAC_ODD)
            [partitioning("fractional_odd")]
            #elif defined(_PARTITIONING_POW2)
            [partitioning("pow2")]
            #else 
            [partitioning("fractional_odd")]
            #endif

            /*Partitioning modes explained:
                Integer - subdivides a number of times equal to the ceilling of the tessellation factor. 
                The ceilling of a number is the smallest integer value greater than or equal to the number.
                Fractional odd and fractional even modes allow for smooth transitions between different
                tessellation factors.*/
            TessellationControlPoint Hull(
                InputPatch<TessellationControlPoint, 3> patch, //input triangle
                uint id : SV_OutputControlPointID //vertex index on triangle - signals which vertex on the patch to output data for
            )
            {
                return patch[id];    
            }
            //runs once per patch

         /*This allows us to use barycentric_interpolation
         to interpolate any property in the patch structure*/
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