Shader "Shader Graphs/EnemyStencilWrite"
{
    Properties
    {
        [NoScaleOffset]_BaseMap("BaseMap", 2D) = "white" {}
        [NoScaleOffset]_EmissiveMask("EmissiveMask", 2D) = "black" {}
        _EmissiveIntensity("EmissiveIntensity", Range(0, 10)) = 1
        [NoScaleOffset]_RoughnessMap("RoughnessMap", 2D) = "white" {}
        [NoScaleOffset]_MetalicMAp("MetalicMAp", 2D) = "white" {}
        [NoScaleOffset]_AlphaMap("AlphaMap", 2D) = "white" {}
        _LightingSmoothness("LightingSmoothness", Float) = 0.5
        _Steps("Steps", Float) = 3
        _SpecularSteps("SpecularSteps", Float) = 3
        [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector]_QueueControl("_QueueControl", Float) = -1
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "UniversalMaterialType" = "Lit"
            "Queue"="AlphaTest"
            "DisableBatching"="False"
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="UniversalLitSubTarget"
        }
         Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
        }
        Pass
        {
            Name "Universal Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
        
        // Render State
        Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On
        AlphaToMask On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma instancing_options renderinglayer
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DYNAMICLIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
        #pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
        #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
        #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
        #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile _ SHADOWS_SHADOWMASK
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ _LIGHT_LAYERS
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma multi_compile_fragment _ _LIGHT_COOKIES
        #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
        #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD1
        #define VARYINGS_NEED_TEXCOORD2
        #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        #define VARYINGS_NEED_SHADOW_COORD
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_FORWARD
        #define _ALPHATEST_ON 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
             float4 texCoord1;
             float4 texCoord2;
            #if defined(LIGHTMAP_ON)
             float2 staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
             float2 dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
             float4 probeOcclusion;
            #endif
             float4 fogFactorAndVertexLight;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             float4 shadowCoord;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpaceNormal;
             float3 TangentSpaceNormal;
             float3 WorldSpaceViewDirection;
             float3 WorldSpacePosition;
             float2 NDCPosition;
             float2 PixelPosition;
             float4 uv0;
             float4 uv1;
             float4 uv2;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if defined(LIGHTMAP_ON)
             float2 staticLightmapUV : INTERP0;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
             float2 dynamicLightmapUV : INTERP1;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh : INTERP2;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
             float4 probeOcclusion : INTERP3;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             float4 shadowCoord : INTERP4;
            #endif
             float4 tangentWS : INTERP5;
             float4 texCoord0 : INTERP6;
             float4 texCoord1 : INTERP7;
             float4 texCoord2 : INTERP8;
             float4 fogFactorAndVertexLight : INTERP9;
             float3 positionWS : INTERP10;
             float3 normalWS : INTERP11;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
            output.probeOcclusion = input.probeOcclusion;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.shadowCoord = input.shadowCoord;
            #endif
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.texCoord1.xyzw = input.texCoord1;
            output.texCoord2.xyzw = input.texCoord2;
            output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
            output.probeOcclusion = input.probeOcclusion;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.shadowCoord = input.shadowCoord;
            #endif
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.texCoord1 = input.texCoord1.xyzw;
            output.texCoord2 = input.texCoord2.xyzw;
            output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _EmissiveIntensity;
        float4 _BaseMap_TexelSize;
        float4 _EmissiveMask_TexelSize;
        float4 _RoughnessMap_TexelSize;
        float4 _MetalicMAp_TexelSize;
        float4 _AlphaMap_TexelSize;
        float _LightingSmoothness;
        float _Steps;
        float _SpecularSteps;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissiveMask);
        SAMPLER(sampler_EmissiveMask);
        TEXTURE2D(_RoughnessMap);
        SAMPLER(sampler_RoughnessMap);
        TEXTURE2D(_MetalicMAp);
        SAMPLER(sampler_MetalicMAp);
        TEXTURE2D(_AlphaMap);
        SAMPLER(sampler_AlphaMap);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void MainLightDirection_float(out float3 Direction)
        {
            #if SHADERGRAPH_PREVIEW
            Direction = half3(-0.5, -0.5, 0);
            #else
            Direction = SHADERGRAPH_MAIN_LIGHT_DIRECTION();
            #endif
        }
        
        void Unity_Negate_float3(float3 In, out float3 Out)
        {
            Out = -1 * In;
        }
        
        void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
        Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        struct Bindings_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float
        {
        float3 WorldSpaceNormal;
        };
        
        void SG_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float(float3 _NormalWS, bool _NormalWS_a616d67d9a9241cf9d4230dbfe62563f_IsConnected, Bindings_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float IN, out float Diffuse_1)
        {
        float3 _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3 = _NormalWS;
        bool _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3_IsConnected = _NormalWS_a616d67d9a9241cf9d4230dbfe62563f_IsConnected;
        float3 _BranchOnInputConnection_565230d8358148bfac6929645968ec6d_Out_3_Vector3 = _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3_IsConnected ? _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3 : IN.WorldSpaceNormal;
        float3 _MainLightDirection_46d8c4b675334560b12bb67c4e1ab4ca_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_46d8c4b675334560b12bb67c4e1ab4ca_Direction_0_Vector3);
        float3 _Negate_cefa58b1986e43aa934b4f687b6018fd_Out_1_Vector3;
        Unity_Negate_float3(_MainLightDirection_46d8c4b675334560b12bb67c4e1ab4ca_Direction_0_Vector3, _Negate_cefa58b1986e43aa934b4f687b6018fd_Out_1_Vector3);
        float _DotProduct_b0063238199f49058ba189bac4da970d_Out_2_Float;
        Unity_DotProduct_float3(_BranchOnInputConnection_565230d8358148bfac6929645968ec6d_Out_3_Vector3, _Negate_cefa58b1986e43aa934b4f687b6018fd_Out_1_Vector3, _DotProduct_b0063238199f49058ba189bac4da970d_Out_2_Float);
        float _Multiply_d11fc76f5fe1410e894c6ac565e2d937_Out_2_Float;
        Unity_Multiply_float_float(_DotProduct_b0063238199f49058ba189bac4da970d_Out_2_Float, 0.5, _Multiply_d11fc76f5fe1410e894c6ac565e2d937_Out_2_Float);
        float _Add_1a63a84f8dfc4d8ca825f5717c5a9d07_Out_2_Float;
        Unity_Add_float(_Multiply_d11fc76f5fe1410e894c6ac565e2d937_Out_2_Float, float(0.5), _Add_1a63a84f8dfc4d8ca825f5717c5a9d07_Out_2_Float);
        float _Saturate_95b5072011dc4a2c899250596471f236_Out_1_Float;
        Unity_Saturate_float(_Add_1a63a84f8dfc4d8ca825f5717c5a9d07_Out_2_Float, _Saturate_95b5072011dc4a2c899250596471f236_Out_1_Float);
        Diffuse_1 = _Saturate_95b5072011dc4a2c899250596471f236_Out_1_Float;
        }
        
        void Unity_Exponential2_float(float In, out float Out)
        {
            Out = exp2(In);
        }
        
        struct Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float
        {
        };
        
        void SG_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float(float _In, Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float IN, out float Out_1)
        {
        float _Property_ad239b68c11f4fcb920425661841c000_Out_0_Float = _In;
        float _Multiply_187758eae33846cb912e3d80a692d9b0_Out_2_Float;
        Unity_Multiply_float_float(_Property_ad239b68c11f4fcb920425661841c000_Out_0_Float, 10, _Multiply_187758eae33846cb912e3d80a692d9b0_Out_2_Float);
        float _Add_023529f132584452b7ef9ffb6f67eb4b_Out_2_Float;
        Unity_Add_float(_Multiply_187758eae33846cb912e3d80a692d9b0_Out_2_Float, float(1), _Add_023529f132584452b7ef9ffb6f67eb4b_Out_2_Float);
        float _Exponential_b2744bb952e44d0cb0d813283e357f8c_Out_1_Float;
        Unity_Exponential2_float(_Add_023529f132584452b7ef9ffb6f67eb4b_Out_2_Float, _Exponential_b2744bb952e44d0cb0d813283e357f8c_Out_1_Float);
        Out_1 = _Exponential_b2744bb952e44d0cb0d813283e357f8c_Out_1_Float;
        }
        
        void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
        {
            Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
        }
        
        void Unity_Lerp_float(float A, float B, float T, out float Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        struct Bindings_Reflectance_3dd134828a90d7446802ac63dbc0221d_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        };
        
        void SG_Reflectance_3dd134828a90d7446802ac63dbc0221d_float(float3 _Base_Color, bool _Base_Color_57260f26a3c14234b948dd74141df9c7_IsConnected, float3 _NormalWS, bool _NormalWS_ab3d33962fc64884948809ad8eaf7aa5_IsConnected, float _Metallic, Bindings_Reflectance_3dd134828a90d7446802ac63dbc0221d_float IN, out float3 Reflectance_1)
        {
        float3 _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3 = _NormalWS;
        bool _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3_IsConnected = _NormalWS_ab3d33962fc64884948809ad8eaf7aa5_IsConnected;
        float3 _BranchOnInputConnection_2b572462617842bf85ea171e93239787_Out_3_Vector3 = _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3_IsConnected ? _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3 : IN.WorldSpaceNormal;
        float _FresnelEffect_c130e1f911b140299ff5363626d5f22b_Out_3_Float;
        Unity_FresnelEffect_float(_BranchOnInputConnection_2b572462617842bf85ea171e93239787_Out_3_Vector3, IN.WorldSpaceViewDirection, float(3), _FresnelEffect_c130e1f911b140299ff5363626d5f22b_Out_3_Float);
        float _Lerp_3ed50cf0a0a94d98a7de54752c2f2581_Out_3_Float;
        Unity_Lerp_float(float(0.04), float(1), _FresnelEffect_c130e1f911b140299ff5363626d5f22b_Out_3_Float, _Lerp_3ed50cf0a0a94d98a7de54752c2f2581_Out_3_Float);
        float3 _Property_0d65a942fa4c458ebf1d5b61426a6680_Out_0_Vector3 = _Base_Color;
        bool _Property_0d65a942fa4c458ebf1d5b61426a6680_Out_0_Vector3_IsConnected = _Base_Color_57260f26a3c14234b948dd74141df9c7_IsConnected;
        float _Property_0899e6b3a7dd4ecb9b9de1f735daed5e_Out_0_Float = _Metallic;
        float3 _Lerp_e80ebb2ee2ec40c2a06b11a4bab428cc_Out_3_Vector3;
        Unity_Lerp_float3((_Lerp_3ed50cf0a0a94d98a7de54752c2f2581_Out_3_Float.xxx), _Property_0d65a942fa4c458ebf1d5b61426a6680_Out_0_Vector3, (_Property_0899e6b3a7dd4ecb9b9de1f735daed5e_Out_0_Float.xxx), _Lerp_e80ebb2ee2ec40c2a06b11a4bab428cc_Out_3_Vector3);
        Reflectance_1 = _Lerp_e80ebb2ee2ec40c2a06b11a4bab428cc_Out_3_Vector3;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Normalize_float3(float3 In, out float3 Out)
        {
            Out = normalize(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
        Out = A * B;
        }
        
        struct Bindings_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        };
        
        void SG_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float(float3 _NormalWS, bool _NormalWS_764643e0455a4ece932285a0241516e7_IsConnected, float _Smoothness, float3 _Reflectance, Bindings_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float IN, out float3 Specular_1)
        {
        float3 _MainLightDirection_436ef0cdefe04605b70adabe76102606_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_436ef0cdefe04605b70adabe76102606_Direction_0_Vector3);
        float3 _Negate_6a9ea0a4355445dcb9ea2b983008a197_Out_1_Vector3;
        Unity_Negate_float3(_MainLightDirection_436ef0cdefe04605b70adabe76102606_Direction_0_Vector3, _Negate_6a9ea0a4355445dcb9ea2b983008a197_Out_1_Vector3);
        float3 _Add_fec1db3bbef240b8b1b7d4f12ea2c97f_Out_2_Vector3;
        Unity_Add_float3(IN.WorldSpaceViewDirection, _Negate_6a9ea0a4355445dcb9ea2b983008a197_Out_1_Vector3, _Add_fec1db3bbef240b8b1b7d4f12ea2c97f_Out_2_Vector3);
        float3 _Normalize_5df6e3535e5842a988f33d859b57987a_Out_1_Vector3;
        Unity_Normalize_float3(_Add_fec1db3bbef240b8b1b7d4f12ea2c97f_Out_2_Vector3, _Normalize_5df6e3535e5842a988f33d859b57987a_Out_1_Vector3);
        float3 _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3 = _NormalWS;
        bool _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3_IsConnected = _NormalWS_764643e0455a4ece932285a0241516e7_IsConnected;
        float3 _BranchOnInputConnection_c0a0900e17284fa980ff63a7456c2199_Out_3_Vector3 = _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3_IsConnected ? _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3 : IN.WorldSpaceNormal;
        float _DotProduct_fdf0af8bd536429fa4b57dc806a67c04_Out_2_Float;
        Unity_DotProduct_float3(_Normalize_5df6e3535e5842a988f33d859b57987a_Out_1_Vector3, _BranchOnInputConnection_c0a0900e17284fa980ff63a7456c2199_Out_3_Vector3, _DotProduct_fdf0af8bd536429fa4b57dc806a67c04_Out_2_Float);
        float _Saturate_716ed2bdf36e4e60a35f74d186b278e9_Out_1_Float;
        Unity_Saturate_float(_DotProduct_fdf0af8bd536429fa4b57dc806a67c04_Out_2_Float, _Saturate_716ed2bdf36e4e60a35f74d186b278e9_Out_1_Float);
        float _Saturate_1d23f425974541b381f4fa80cf5a00b1_Out_1_Float;
        Unity_Saturate_float(_Saturate_716ed2bdf36e4e60a35f74d186b278e9_Out_1_Float, _Saturate_1d23f425974541b381f4fa80cf5a00b1_Out_1_Float);
        float _Property_1766606f1117460388cc77e7505598dd_Out_0_Float = _Smoothness;
        Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4;
        half _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4_Out_1_Float;
        SG_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float(_Property_1766606f1117460388cc77e7505598dd_Out_0_Float, _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4, _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4_Out_1_Float);
        float _Power_c507d979651749238e129118bb4c252f_Out_2_Float;
        Unity_Power_float(_Saturate_1d23f425974541b381f4fa80cf5a00b1_Out_1_Float, _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4_Out_1_Float, _Power_c507d979651749238e129118bb4c252f_Out_2_Float);
        float3 _Property_ab8c048e4ae44d56bf8dcb1bbc8ada89_Out_0_Vector3 = _Reflectance;
        float3 _Multiply_3c43a7f74b6c4c35a534990268ada20a_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Power_c507d979651749238e129118bb4c252f_Out_2_Float.xxx), _Property_ab8c048e4ae44d56bf8dcb1bbc8ada89_Out_0_Vector3, _Multiply_3c43a7f74b6c4c35a534990268ada20a_Out_2_Vector3);
        Specular_1 = _Multiply_3c43a7f74b6c4c35a534990268ada20a_Out_2_Vector3;
        }
        
        // unity-custom-func-begin
        void MainLightString_float(float3 worldPos, out float3 direction, out float3 color, out float shadowAtt){
        #ifdef SHADERGRAPH_PREVIEW
          direction = normalize(float3(-0.7,0.7,-0.7));
          color = float3(1,1,1);
          shadowAtt = 1;
        #else
          #if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)
            float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
            Light mainLight = GetMainLight(shadowCoord);
             direction = mainLight.direction;
        
             color = mainLight.color;
        
             shadowAtt = mainLight.shadowAttenuation;
          #else
            direction = normalize(float3(-0.7,0.7,-0.7));
            color = float3(1,1,1);
            shadowAtt = 1;
          #endif
        #endif
        }
        // unity-custom-func-end
        
        struct Bindings_MainLight_0157caa4ea90d88499c9fdb016582871_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_MainLight_0157caa4ea90d88499c9fdb016582871_float(Bindings_MainLight_0157caa4ea90d88499c9fdb016582871_float IN, out float3 Direction_1, out float3 Color_2, out float ShadowAtt_3)
        {
        float3 _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_direction_1_Vector3;
        float3 _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_color_2_Vector3;
        float _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_shadowAtt_3_Float;
        MainLightString_float(IN.WorldSpacePosition, _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_direction_1_Vector3, _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_color_2_Vector3, _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_shadowAtt_3_Float);
        Direction_1 = _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_direction_1_Vector3;
        Color_2 = _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_color_2_Vector3;
        ShadowAtt_3 = _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_shadowAtt_3_Float;
        }
        
        void Unity_ViewVectorWorld_float(out float3 Out, float3 WorldSpacePosition)
        {
            Out = _WorldSpaceCameraPos.xyz - GetAbsolutePositionWS(WorldSpacePosition);
            if(!IsPerspectiveProjection())
            {
                Out = GetViewForwardDir() * dot(Out, GetViewForwardDir());
            }
        }
        
        // unity-custom-func-begin
        void AddAdditionalLights_float(float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView, float MainDiffuse, float3 MainSpecular, float3 MainColor, out float Diffuse, out float3 Specular, out float3 Color){
        // Initialize outputs
        
        Diffuse = 0;
        
        Specular = 0;
        
        Color = 0;
        
        
        
        #ifndef SHADERGRAPH_PREVIEW
        
        
        
           // --- MAIN LIGHT ---
        
          Light mainLight = GetMainLight();
        
          mainLight.shadowAttenuation = MainLightRealtimeShadow(float4(WorldPosition, 1.0));
        
          float mainAtten = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
        
         
        
        
        
          float mainNdotL = saturate(dot(WorldNormal, mainLight.direction));
        
          float mainDiffuse = mainAtten * mainNdotL;
        
          float3 mainSpecular = LightingSpecular(mainDiffuse, mainLight.direction, WorldNormal, WorldView, 1, Smoothness);
        
        
        
          Diffuse += mainDiffuse;
        
          Specular += mainSpecular;
        
          Color += mainLight.color * (mainDiffuse + mainSpecular);
        
        
        
            // --- ADDITIONAL LIGHTS ---
        
            uint pixelLightCount = GetAdditionalLightsCount();
        
        
        
            LIGHT_LOOP_BEGIN(pixelLightCount)
        
                
        
                Light light = GetAdditionalPerObjectLight(lightIndex, WorldPosition);
        
        
        
                light.shadowAttenuation = AdditionalLightRealtimeShadow(lightIndex, WorldPosition, light.direction);
        
                float atten = light.distanceAttenuation * light.shadowAttenuation;
        
        
        
                float NdotL = saturate(dot(WorldNormal, light.direction) * 0.5 + 0.5);
        
                float thisDiffuse = atten * NdotL;
        
                float3 thisSpecular = LightingSpecular(thisDiffuse, light.direction, WorldNormal, WorldView, 1, Smoothness);
        
        
        
                Diffuse += thisDiffuse;
        
                Specular += thisSpecular;
        
                     #if defined(_LIGHT_COOKIES)
        
        	      float3 cookieColor = SampleAdditionalLightCookie(lightIndex, WorldPosition);
        
        	      light.color *= cookieColor;
        
                     #endif
        
                Color += light.color * (thisDiffuse + thisSpecular);
        
            LIGHT_LOOP_END
        
        
        
            // Normalize total lighting
        
            //Color = MainColor * Color;
        
        float totalWeight = Diffuse + dot(Specular, float3(0.333, 0.333, 0.333));
        
            Color = totalWeight > 0 ? MainColor * (Color / totalWeight) : MainColor;
        
        
        
        #endif
        }
        // unity-custom-func-end
        
        struct Bindings_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float(float _MainLightDiffuse, float3 _MainLightSpecular, float3 _MainLightColor, float3 _NormalWS, float _Smoothness, Bindings_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float IN, out float Diffuse_1, out float3 Specular_2, out float3 Color_3)
        {
        float _Property_f5ac72f32dc84ae988391b7d51172380_Out_0_Float = _Smoothness;
        float3 _Property_85d85c5164d5483787fecbcecfd6a519_Out_0_Vector3 = _NormalWS;
        float3 _ViewVector_0d331f2b495741bcb8795caa9549f016_Out_0_Vector3;
        Unity_ViewVectorWorld_float(_ViewVector_0d331f2b495741bcb8795caa9549f016_Out_0_Vector3, IN.WorldSpacePosition);
        float _Property_51c13cc19f414dacb86745cec42d9ab6_Out_0_Float = _MainLightDiffuse;
        float3 _Property_f1e96792c3cc4a1e90396147ef9f7160_Out_0_Vector3 = _MainLightColor;
        float _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Diffuse_7_Float;
        float3 _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Specular_8_Vector3;
        float3 _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Color_9_Vector3;
        AddAdditionalLights_float(_Property_f5ac72f32dc84ae988391b7d51172380_Out_0_Float, IN.WorldSpacePosition, _Property_85d85c5164d5483787fecbcecfd6a519_Out_0_Vector3, _ViewVector_0d331f2b495741bcb8795caa9549f016_Out_0_Vector3, _Property_51c13cc19f414dacb86745cec42d9ab6_Out_0_Float, _Property_f1e96792c3cc4a1e90396147ef9f7160_Out_0_Vector3, _Property_f1e96792c3cc4a1e90396147ef9f7160_Out_0_Vector3, _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Diffuse_7_Float, _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Specular_8_Vector3, _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Color_9_Vector3);
        Diffuse_1 = _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Diffuse_7_Float;
        Specular_2 = _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Specular_8_Vector3;
        Color_3 = _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Color_9_Vector3;
        }
        
        void Unity_Posterize_float3(float3 In, float3 Steps, out float3 Out)
        {
            Out = floor(In / (1 / Steps)) * (1 / Steps);
        }
        
        void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
        {
            float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
            Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
        }
        
        // unity-custom-func-begin
        void GetSSAO_float(float2 ScreenPos, out float DirectAO, out float IndirectAO){
         #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT) && !defined(SHADERGRAPH_PREVIEW)
        
         
          float ssao = saturate(SampleAmbientOcclusion(ScreenPos) + (1.0 - _AmbientOcclusionParam.x));
        
           IndirectAO = ssao;
        
           DirectAO = lerp(1.0, ssao, _AmbientOcclusionParam.w);
        
        #else
        
           DirectAO = 1.0;
        
           IndirectAO = 1.0;
        
        #endif
        }
        // unity-custom-func-end
        
        struct Bindings_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float
        {
        float2 NDCPosition;
        };
        
        void SG_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float(float2 _ScreenPos, bool _ScreenPos_72149ac21c784cc8b4614750ec683413_IsConnected, Bindings_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float IN, out float DirectAO_1, out float IndirectAO_2)
        {
        float2 _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2 = _ScreenPos;
        bool _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2_IsConnected = _ScreenPos_72149ac21c784cc8b4614750ec683413_IsConnected;
        float4 _ScreenPosition_a193bed040594170a08ae04fed7ea8a7_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
        float2 _BranchOnInputConnection_8e8119e37af64e9a957c820002cea48c_Out_3_Vector2 = _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2_IsConnected ? _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2 : (_ScreenPosition_a193bed040594170a08ae04fed7ea8a7_Out_0_Vector4.xy);
        float _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_DirectAO_1_Float;
        float _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_IndirectAO_2_Float;
        GetSSAO_float(_BranchOnInputConnection_8e8119e37af64e9a957c820002cea48c_Out_3_Vector2, _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_DirectAO_1_Float, _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_IndirectAO_2_Float);
        DirectAO_1 = _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_DirectAO_1_Float;
        IndirectAO_2 = _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_IndirectAO_2_Float;
        }
        
        void Unity_Minimum_float(float A, float B, out float Out)
        {
            Out = min(A, B);
        };
        
        void Unity_ReflectionProbe_float(float3 ViewDir, float3 Normal, float LOD, out float3 Out)
        {
            Out = SHADERGRAPH_REFLECTION_PROBE(ViewDir, Normal, LOD);
        }
        
        struct Bindings_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        float3 WorldSpacePosition;
        float2 NDCPosition;
        float2 PixelPosition;
        half4 uv1;
        half4 uv2;
        };
        
        void SG_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float(float3 _Base_Color, bool _Base_Color_87794c9dccf9430d8ee1585f52796223_IsConnected, float3 _NormalWS, bool _NormalWS_d8d26d45dd0b4d4a8e8426edcd206b0f_IsConnected, float _AO, float _Smoothness, float _Metallic, float _Reflectance, Bindings_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float IN, out float3 Ambient_1, out float3 Metallic_2, out float DdirectAO_3)
        {
        float3 _Property_50e51e65f64b40c29fa7b10305f0a7b4_Out_0_Vector3 = _Base_Color;
        bool _Property_50e51e65f64b40c29fa7b10305f0a7b4_Out_0_Vector3_IsConnected = _Base_Color_87794c9dccf9430d8ee1585f52796223_IsConnected;
        float _Property_b967758667b74ff5acb2a8af3ff58c39_Out_0_Float = _Metallic;
        float3 _Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3;
        Unity_Lerp_float3(_Property_50e51e65f64b40c29fa7b10305f0a7b4_Out_0_Vector3, float3(0, 0, 0), (_Property_b967758667b74ff5acb2a8af3ff58c39_Out_0_Float.xxx), _Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3);
        float3 _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3 = _NormalWS;
        bool _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3_IsConnected = _NormalWS_d8d26d45dd0b4d4a8e8426edcd206b0f_IsConnected;
        float3 _BranchOnInputConnection_dae192f178d24a5a9c9c540fd5488461_Out_3_Vector3 = _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3_IsConnected ? _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3 : IN.WorldSpaceNormal;
        float3 _BakedGI_1e2ac029f59d497abdc2a7bcf1eb3346_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, _BranchOnInputConnection_dae192f178d24a5a9c9c540fd5488461_Out_3_Vector3, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
        float _Property_26a27af7a1144ea0a77aad0b49f12697_Out_0_Float = _AO;
        Bindings_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float _SSAO_f6107fe543294e7f932b8c5cfc86bdd5;
        _SSAO_f6107fe543294e7f932b8c5cfc86bdd5.NDCPosition = IN.NDCPosition;
        half _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_DirectAO_1_Float;
        half _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_IndirectAO_2_Float;
        SG_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float(half2 (0, 0), false, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_DirectAO_1_Float, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_IndirectAO_2_Float);
        float _Minimum_3ac81a32823a439c8b51e89edf949c26_Out_2_Float;
        Unity_Minimum_float(_Property_26a27af7a1144ea0a77aad0b49f12697_Out_0_Float, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_IndirectAO_2_Float, _Minimum_3ac81a32823a439c8b51e89edf949c26_Out_2_Float);
        float3 _Multiply_423df8bc24d94add8f44f6db0bf46798_Out_2_Vector3;
        Unity_Multiply_float3_float3(_BakedGI_1e2ac029f59d497abdc2a7bcf1eb3346_Out_1_Vector3, (_Minimum_3ac81a32823a439c8b51e89edf949c26_Out_2_Float.xxx), _Multiply_423df8bc24d94add8f44f6db0bf46798_Out_2_Vector3);
        float3 _Multiply_4ee1636476de4393a1b2e9162c2328b9_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3, _Multiply_423df8bc24d94add8f44f6db0bf46798_Out_2_Vector3, _Multiply_4ee1636476de4393a1b2e9162c2328b9_Out_2_Vector3);
        float _Property_76583d84384b44cbb79843c917044c1b_Out_0_Float = _Reflectance;
        float _Property_f511aceb4377409690a68e8d8a16a032_Out_0_Float = _Smoothness;
        float _Lerp_496af92f7e424d5586ca49ffe949aff6_Out_3_Float;
        Unity_Lerp_float(float(8), float(0), _Property_f511aceb4377409690a68e8d8a16a032_Out_0_Float, _Lerp_496af92f7e424d5586ca49ffe949aff6_Out_3_Float);
        float3 _ReflectionProbe_d9eb69c9b54641068d948dd37fec342f_Out_3_Vector3;
        Unity_ReflectionProbe_float(IN.WorldSpaceViewDirection, _BranchOnInputConnection_dae192f178d24a5a9c9c540fd5488461_Out_3_Vector3, _Lerp_496af92f7e424d5586ca49ffe949aff6_Out_3_Float, _ReflectionProbe_d9eb69c9b54641068d948dd37fec342f_Out_3_Vector3);
        float3 _Multiply_25ab7f2171954fc4a710fe8c8c2cc402_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Property_76583d84384b44cbb79843c917044c1b_Out_0_Float.xxx), _ReflectionProbe_d9eb69c9b54641068d948dd37fec342f_Out_3_Vector3, _Multiply_25ab7f2171954fc4a710fe8c8c2cc402_Out_2_Vector3);
        float3 _Add_475a3a92ac834f51b869dedfbba66e44_Out_2_Vector3;
        Unity_Add_float3(_Multiply_4ee1636476de4393a1b2e9162c2328b9_Out_2_Vector3, _Multiply_25ab7f2171954fc4a710fe8c8c2cc402_Out_2_Vector3, _Add_475a3a92ac834f51b869dedfbba66e44_Out_2_Vector3);
        Ambient_1 = _Add_475a3a92ac834f51b869dedfbba66e44_Out_2_Vector3;
        Metallic_2 = _Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3;
        DdirectAO_3 = _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_DirectAO_1_Float;
        }
        
        void Unity_Saturate_float3(float3 In, out float3 Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Posterize_float(float In, float Steps, out float Out)
        {
            Out = floor(In / (1 / Steps)) * (1 / Steps);
        }
        
        struct Bindings_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        float3 WorldSpacePosition;
        float2 NDCPosition;
        float2 PixelPosition;
        half4 uv1;
        half4 uv2;
        };
        
        void SG_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float(float3 _Base_Color, bool _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected, float3 _NormalWS, bool _NormalWS_d2c1afa83f4f4d20ac34f445f069483d_IsConnected, float _Metallic, float _Smoothness, float _AO, float _Steps, float _Specular_Steps, float _Smoothness_Strength, Bindings_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float IN, out float3 Lit_1)
        {
        float3 _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3 = _NormalWS;
        bool _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3_IsConnected = _NormalWS_d2c1afa83f4f4d20ac34f445f069483d_IsConnected;
        float3 _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3 = _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3_IsConnected ? _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3 : IN.WorldSpaceNormal;
        Bindings_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91;
        _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91.WorldSpaceNormal = IN.WorldSpaceNormal;
        half _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float;
        SG_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float(_BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91, _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float);
        float _Property_af2c0fb6df7d4dc9abade557540f3ed7_Out_0_Float = _Smoothness;
        Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e;
        half _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float;
        SG_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float(_Property_af2c0fb6df7d4dc9abade557540f3ed7_Out_0_Float, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float);
        float3 _Property_b3987ddafd854a0a85863bc89dcde720_Out_0_Vector3 = _Base_Color;
        bool _Property_b3987ddafd854a0a85863bc89dcde720_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float _Property_3c53a42fedba46ba9fae794424fbe85e_Out_0_Float = _Metallic;
        Bindings_Reflectance_3dd134828a90d7446802ac63dbc0221d_float _Reflectance_0d5aeeef450744cf885ab04f87797c7b;
        _Reflectance_0d5aeeef450744cf885ab04f87797c7b.WorldSpaceNormal = IN.WorldSpaceNormal;
        _Reflectance_0d5aeeef450744cf885ab04f87797c7b.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3;
        SG_Reflectance_3dd134828a90d7446802ac63dbc0221d_float(_Property_b3987ddafd854a0a85863bc89dcde720_Out_0_Vector3, true, _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _Property_3c53a42fedba46ba9fae794424fbe85e_Out_0_Float, _Reflectance_0d5aeeef450744cf885ab04f87797c7b, _Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3);
        Bindings_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8;
        _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8.WorldSpaceNormal = IN.WorldSpaceNormal;
        _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8_Specular_1_Vector3;
        SG_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float(_BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float, _Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3, _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8, _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8_Specular_1_Vector3);
        float3 _Multiply_577b66af727b4e43be242c88de8def99_Out_2_Vector3;
        Unity_Multiply_float3_float3((_HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float.xxx), _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8_Specular_1_Vector3, _Multiply_577b66af727b4e43be242c88de8def99_Out_2_Vector3);
        Bindings_MainLight_0157caa4ea90d88499c9fdb016582871_float _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a;
        _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a.WorldSpacePosition = IN.WorldSpacePosition;
        float3 _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Direction_1_Vector3;
        float3 _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Color_2_Vector3;
        float _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_ShadowAtt_3_Float;
        SG_MainLight_0157caa4ea90d88499c9fdb016582871_float(_MainLight_1fffd9ee56744cde8cd3210b8fc0a55a, _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Direction_1_Vector3, _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Color_2_Vector3, _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_ShadowAtt_3_Float);
        float3 _Multiply_e3a50f2fbf214e64931956c0b1e7f194_Out_2_Vector3;
        Unity_Multiply_float3_float3(_MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Color_2_Vector3, (_MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_ShadowAtt_3_Float.xxx), _Multiply_e3a50f2fbf214e64931956c0b1e7f194_Out_2_Vector3);
        Bindings_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021;
        _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021.WorldSpacePosition = IN.WorldSpacePosition;
        float _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Diffuse_1_Float;
        float3 _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Specular_2_Vector3;
        float3 _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3;
        SG_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float(_HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float, _Multiply_577b66af727b4e43be242c88de8def99_Out_2_Vector3, _Multiply_e3a50f2fbf214e64931956c0b1e7f194_Out_2_Vector3, _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Diffuse_1_Float, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Specular_2_Vector3, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3);
        float _Property_7fabb829117947b8956799cbdf3be9e0_Out_0_Float = _Steps;
        float3 _Posterize_8f26285a65784a038d6d9d2087181b46_Out_2_Vector3;
        Unity_Posterize_float3(_AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, (_Property_7fabb829117947b8956799cbdf3be9e0_Out_0_Float.xxx), _Posterize_8f26285a65784a038d6d9d2087181b46_Out_2_Vector3);
        float3 _Property_3668f71779994b3a9fcc7095f880cb12_Out_0_Vector3 = _Base_Color;
        bool _Property_3668f71779994b3a9fcc7095f880cb12_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float3 _Saturation_e12bfe3d861c4f1fa4c1297d0dd08cc0_Out_2_Vector3;
        Unity_Saturation_float(_Property_3668f71779994b3a9fcc7095f880cb12_Out_0_Vector3, float(1.6), _Saturation_e12bfe3d861c4f1fa4c1297d0dd08cc0_Out_2_Vector3);
        float3 _Multiply_77c53f8596dd487fa3009d208e99c0fd_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Saturation_e12bfe3d861c4f1fa4c1297d0dd08cc0_Out_2_Vector3, float3(0.1, 0.1, 0.1), _Multiply_77c53f8596dd487fa3009d208e99c0fd_Out_2_Vector3);
        float3 _Property_494170212d604ba0a52a5b39f34e4c3f_Out_0_Vector3 = _Base_Color;
        bool _Property_494170212d604ba0a52a5b39f34e4c3f_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float3 _Multiply_e22a63bb0da045618a0202e45051407e_Out_2_Vector3;
        Unity_Multiply_float3_float3((_AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Diffuse_1_Float.xxx), _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, _Multiply_e22a63bb0da045618a0202e45051407e_Out_2_Vector3);
        float3 _Property_53d2f61f22684efca5bbd024184799a4_Out_0_Vector3 = _Base_Color;
        bool _Property_53d2f61f22684efca5bbd024184799a4_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float _Property_5204695d7d6d4d03b28eba481d372192_Out_0_Float = _AO;
        float _Property_aa169c45708b4343b76a3b52b483b8e2_Out_0_Float = _Smoothness;
        float _Property_1d116579c59d45ccaa1bdcb07a5ec5d5_Out_0_Float = _Metallic;
        Bindings_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.WorldSpaceNormal = IN.WorldSpaceNormal;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.WorldSpacePosition = IN.WorldSpacePosition;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.NDCPosition = IN.NDCPosition;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.PixelPosition = IN.PixelPosition;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.uv1 = IN.uv1;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.uv2 = IN.uv2;
        half3 _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Ambient_1_Vector3;
        half3 _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Metallic_2_Vector3;
        half _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_DdirectAO_3_Float;
        SG_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float(_Property_53d2f61f22684efca5bbd024184799a4_Out_0_Vector3, true, _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _Property_5204695d7d6d4d03b28eba481d372192_Out_0_Float, _Property_aa169c45708b4343b76a3b52b483b8e2_Out_0_Float, _Property_1d116579c59d45ccaa1bdcb07a5ec5d5_Out_0_Float, (_Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3).x, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Ambient_1_Vector3, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Metallic_2_Vector3, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_DdirectAO_3_Float);
        float3 _Add_96c33650335248caa7429346ca897bb7_Out_2_Vector3;
        Unity_Add_float3(_Multiply_e22a63bb0da045618a0202e45051407e_Out_2_Vector3, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Ambient_1_Vector3, _Add_96c33650335248caa7429346ca897bb7_Out_2_Vector3);
        float3 _Saturate_b993ba602dea4759baa643682158093c_Out_1_Vector3;
        Unity_Saturate_float3(_Add_96c33650335248caa7429346ca897bb7_Out_2_Vector3, _Saturate_b993ba602dea4759baa643682158093c_Out_1_Vector3);
        float3 _Saturation_559bfc31eca847d3acd46b2fd284dc13_Out_2_Vector3;
        Unity_Saturation_float(_Saturate_b993ba602dea4759baa643682158093c_Out_1_Vector3, float(1), _Saturation_559bfc31eca847d3acd46b2fd284dc13_Out_2_Vector3);
        float _Swizzle_3952ca1d9d274331b6a4a49630c606ae_Out_1_Float = _Saturation_559bfc31eca847d3acd46b2fd284dc13_Out_2_Vector3.x;
        float _Posterize_268e85bcc1d5424485e214740b6468e3_Out_2_Float;
        Unity_Posterize_float(_Swizzle_3952ca1d9d274331b6a4a49630c606ae_Out_1_Float, _Property_7fabb829117947b8956799cbdf3be9e0_Out_0_Float, _Posterize_268e85bcc1d5424485e214740b6468e3_Out_2_Float);
        float3 _Lerp_8c017a3a82434ee38c4432bf457e582b_Out_3_Vector3;
        Unity_Lerp_float3(_Multiply_77c53f8596dd487fa3009d208e99c0fd_Out_2_Vector3, _Property_494170212d604ba0a52a5b39f34e4c3f_Out_0_Vector3, (_Posterize_268e85bcc1d5424485e214740b6468e3_Out_2_Float.xxx), _Lerp_8c017a3a82434ee38c4432bf457e582b_Out_3_Vector3);
        float3 _Multiply_5c248bf4893d4acd9b10421fae8923d1_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Posterize_8f26285a65784a038d6d9d2087181b46_Out_2_Vector3, _Lerp_8c017a3a82434ee38c4432bf457e582b_Out_3_Vector3, _Multiply_5c248bf4893d4acd9b10421fae8923d1_Out_2_Vector3);
        float _Property_48b9a97103af41f3b6cafeceb8a6a0b7_Out_0_Float = _Smoothness_Strength;
        float3 _Add_57faecf3e31346a68db61c3e666b2975_Out_2_Vector3;
        Unity_Add_float3((_Property_48b9a97103af41f3b6cafeceb8a6a0b7_Out_0_Float.xxx), _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, _Add_57faecf3e31346a68db61c3e666b2975_Out_2_Vector3);
        float3 _Multiply_b90f73fc105c4da3b7586428b54d0c03_Out_2_Vector3;
        Unity_Multiply_float3_float3(_AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Specular_2_Vector3, _Add_57faecf3e31346a68db61c3e666b2975_Out_2_Vector3, _Multiply_b90f73fc105c4da3b7586428b54d0c03_Out_2_Vector3);
        float3 _Saturation_c81f01b7b0de4608a25fde9890c55880_Out_2_Vector3;
        Unity_Saturation_float(_Multiply_b90f73fc105c4da3b7586428b54d0c03_Out_2_Vector3, float(0), _Saturation_c81f01b7b0de4608a25fde9890c55880_Out_2_Vector3);
        float _Swizzle_fbfd922c5f7d42d399186d59a86d5660_Out_1_Float = _Saturation_c81f01b7b0de4608a25fde9890c55880_Out_2_Vector3.x;
        float _Property_62c311d0a3534ad1ad3a7b66c978a36c_Out_0_Float = _Specular_Steps;
        float _Posterize_1027f91210d94108ae1c593d0fb61d8b_Out_2_Float;
        Unity_Posterize_float(_Swizzle_fbfd922c5f7d42d399186d59a86d5660_Out_1_Float, _Property_62c311d0a3534ad1ad3a7b66c978a36c_Out_0_Float, _Posterize_1027f91210d94108ae1c593d0fb61d8b_Out_2_Float);
        float3 _Multiply_c92faf2f8cc04ff29ec2ba172ebefa59_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Posterize_1027f91210d94108ae1c593d0fb61d8b_Out_2_Float.xxx), _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, _Multiply_c92faf2f8cc04ff29ec2ba172ebefa59_Out_2_Vector3);
        float3 _Add_321e04b0f3bb47558352363c94828b10_Out_2_Vector3;
        Unity_Add_float3(_Multiply_5c248bf4893d4acd9b10421fae8923d1_Out_2_Vector3, _Multiply_c92faf2f8cc04ff29ec2ba172ebefa59_Out_2_Vector3, _Add_321e04b0f3bb47558352363c94828b10_Out_2_Vector3);
        Lit_1 = _Add_321e04b0f3bb47558352363c94828b10_Out_2_Vector3;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float3 Emission;
            float Metallic;
            float Smoothness;
            float Occlusion;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_BaseMap);
            float4 _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D.tex, _Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D.samplerstate, _Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_R_4_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.r;
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_G_5_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.g;
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_B_6_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.b;
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_A_7_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.a;
            float _Property_7ab914554b2b477db515a77874d1d4cd_Out_0_Float = _LightingSmoothness;
            float _Property_9e7bc9fe055c4e4db532299787c0e3ed_Out_0_Float = _Steps;
            float _Property_9d34386f48e54d9eaeda9d04da1ad72f_Out_0_Float = _SpecularSteps;
            Bindings_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.WorldSpaceNormal = IN.WorldSpaceNormal;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.WorldSpacePosition = IN.WorldSpacePosition;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.NDCPosition = IN.NDCPosition;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.PixelPosition = IN.PixelPosition;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.uv1 = IN.uv1;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.uv2 = IN.uv2;
            float3 _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3_Lit_1_Vector3;
            SG_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float((_SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.xyz), true, float3 (0, 0, 0), false, float(0), _Property_7ab914554b2b477db515a77874d1d4cd_Out_0_Float, float(0), _Property_9e7bc9fe055c4e4db532299787c0e3ed_Out_0_Float, _Property_9d34386f48e54d9eaeda9d04da1ad72f_Out_0_Float, float(0), _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3, _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3_Lit_1_Vector3);
            UnityTexture2D _Property_242f17afd4494f1eae45e57743744c8c_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_EmissiveMask);
            float4 _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_242f17afd4494f1eae45e57743744c8c_Out_0_Texture2D.tex, _Property_242f17afd4494f1eae45e57743744c8c_Out_0_Texture2D.samplerstate, _Property_242f17afd4494f1eae45e57743744c8c_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_R_4_Float = _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_RGBA_0_Vector4.r;
            float _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_G_5_Float = _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_RGBA_0_Vector4.g;
            float _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_B_6_Float = _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_RGBA_0_Vector4.b;
            float _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_A_7_Float = _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_RGBA_0_Vector4.a;
            float4 _Multiply_ea903ee047b5477c855ed2b94b6227f2_Out_2_Vector4;
            Unity_Multiply_float4_float4(_SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4, (_SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_R_4_Float.xxxx), _Multiply_ea903ee047b5477c855ed2b94b6227f2_Out_2_Vector4);
            float _Property_21326e76ba4e4c4c99157b1ec259c331_Out_0_Float = _EmissiveIntensity;
            float4 _Multiply_3aa49a8e912d45d09c831299a6a9a550_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Multiply_ea903ee047b5477c855ed2b94b6227f2_Out_2_Vector4, (_Property_21326e76ba4e4c4c99157b1ec259c331_Out_0_Float.xxxx), _Multiply_3aa49a8e912d45d09c831299a6a9a550_Out_2_Vector4);
            UnityTexture2D _Property_4dd97d0be9684f7c9b36d6893a49cc04_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MetalicMAp);
            float4 _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_4dd97d0be9684f7c9b36d6893a49cc04_Out_0_Texture2D.tex, _Property_4dd97d0be9684f7c9b36d6893a49cc04_Out_0_Texture2D.samplerstate, _Property_4dd97d0be9684f7c9b36d6893a49cc04_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_R_4_Float = _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_RGBA_0_Vector4.r;
            float _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_G_5_Float = _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_RGBA_0_Vector4.g;
            float _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_B_6_Float = _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_RGBA_0_Vector4.b;
            float _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_A_7_Float = _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_RGBA_0_Vector4.a;
            UnityTexture2D _Property_7854e5ede0844f91bd8de3bafec90eb0_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_RoughnessMap);
            float4 _SampleTexture2D_3a075f150d404985850c2dcacec42009_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7854e5ede0844f91bd8de3bafec90eb0_Out_0_Texture2D.tex, _Property_7854e5ede0844f91bd8de3bafec90eb0_Out_0_Texture2D.samplerstate, _Property_7854e5ede0844f91bd8de3bafec90eb0_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_3a075f150d404985850c2dcacec42009_R_4_Float = _SampleTexture2D_3a075f150d404985850c2dcacec42009_RGBA_0_Vector4.r;
            float _SampleTexture2D_3a075f150d404985850c2dcacec42009_G_5_Float = _SampleTexture2D_3a075f150d404985850c2dcacec42009_RGBA_0_Vector4.g;
            float _SampleTexture2D_3a075f150d404985850c2dcacec42009_B_6_Float = _SampleTexture2D_3a075f150d404985850c2dcacec42009_RGBA_0_Vector4.b;
            float _SampleTexture2D_3a075f150d404985850c2dcacec42009_A_7_Float = _SampleTexture2D_3a075f150d404985850c2dcacec42009_RGBA_0_Vector4.a;
            UnityTexture2D _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_AlphaMap);
            float4 _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.tex, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.samplerstate, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_R_4_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.r;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_G_5_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.g;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_B_6_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.b;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.a;
            surface.BaseColor = _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3_Lit_1_Vector3;
            surface.NormalTS = IN.TangentSpaceNormal;
            surface.Emission = (_Multiply_3aa49a8e912d45d09c831299a6a9a550_Out_2_Vector4.xyz);
            surface.Metallic = _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_R_4_Float;
            surface.Smoothness = _SampleTexture2D_3a075f150d404985850c2dcacec42009_R_4_Float;
            surface.Occlusion = float(1);
            surface.Alpha = float(1);
            surface.AlphaClipThreshold = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
            // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            float3 unnormalizedNormalWS = input.normalWS;
            const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        
        
            output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
            output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
            output.WorldSpacePosition = input.positionWS;
        
            #if UNITY_UV_STARTS_AT_TOP
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #else
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #endif
        
            output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
            output.NDCPosition.y = 1.0f - output.NDCPosition.y;
        
            output.uv0 = input.texCoord0;
            output.uv1 = input.texCoord1;
            output.uv2 = input.texCoord2;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }
        
        // Render State
        Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma exclude_renderers gles3 glcore
        #pragma multi_compile_instancing
        #pragma instancing_options renderinglayer
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DYNAMICLIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
        #pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
        #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
        #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile _ SHADOWS_SHADOWMASK
        #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
        #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD1
        #define VARYINGS_NEED_TEXCOORD2
        #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        #define VARYINGS_NEED_SHADOW_COORD
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_GBUFFER
        #define _FOG_FRAGMENT 1
        #define _ALPHATEST_ON 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
             float4 texCoord1;
             float4 texCoord2;
            #if defined(LIGHTMAP_ON)
             float2 staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
             float2 dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
             float4 probeOcclusion;
            #endif
             float4 fogFactorAndVertexLight;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             float4 shadowCoord;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpaceNormal;
             float3 TangentSpaceNormal;
             float3 WorldSpaceViewDirection;
             float3 WorldSpacePosition;
             float2 NDCPosition;
             float2 PixelPosition;
             float4 uv0;
             float4 uv1;
             float4 uv2;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if defined(LIGHTMAP_ON)
             float2 staticLightmapUV : INTERP0;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
             float2 dynamicLightmapUV : INTERP1;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh : INTERP2;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
             float4 probeOcclusion : INTERP3;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             float4 shadowCoord : INTERP4;
            #endif
             float4 tangentWS : INTERP5;
             float4 texCoord0 : INTERP6;
             float4 texCoord1 : INTERP7;
             float4 texCoord2 : INTERP8;
             float4 fogFactorAndVertexLight : INTERP9;
             float3 positionWS : INTERP10;
             float3 normalWS : INTERP11;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
            output.probeOcclusion = input.probeOcclusion;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.shadowCoord = input.shadowCoord;
            #endif
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.texCoord1.xyzw = input.texCoord1;
            output.texCoord2.xyzw = input.texCoord2;
            output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
            output.probeOcclusion = input.probeOcclusion;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.shadowCoord = input.shadowCoord;
            #endif
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.texCoord1 = input.texCoord1.xyzw;
            output.texCoord2 = input.texCoord2.xyzw;
            output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _EmissiveIntensity;
        float4 _BaseMap_TexelSize;
        float4 _EmissiveMask_TexelSize;
        float4 _RoughnessMap_TexelSize;
        float4 _MetalicMAp_TexelSize;
        float4 _AlphaMap_TexelSize;
        float _LightingSmoothness;
        float _Steps;
        float _SpecularSteps;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissiveMask);
        SAMPLER(sampler_EmissiveMask);
        TEXTURE2D(_RoughnessMap);
        SAMPLER(sampler_RoughnessMap);
        TEXTURE2D(_MetalicMAp);
        SAMPLER(sampler_MetalicMAp);
        TEXTURE2D(_AlphaMap);
        SAMPLER(sampler_AlphaMap);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void MainLightDirection_float(out float3 Direction)
        {
            #if SHADERGRAPH_PREVIEW
            Direction = half3(-0.5, -0.5, 0);
            #else
            Direction = SHADERGRAPH_MAIN_LIGHT_DIRECTION();
            #endif
        }
        
        void Unity_Negate_float3(float3 In, out float3 Out)
        {
            Out = -1 * In;
        }
        
        void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
        Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        struct Bindings_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float
        {
        float3 WorldSpaceNormal;
        };
        
        void SG_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float(float3 _NormalWS, bool _NormalWS_a616d67d9a9241cf9d4230dbfe62563f_IsConnected, Bindings_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float IN, out float Diffuse_1)
        {
        float3 _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3 = _NormalWS;
        bool _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3_IsConnected = _NormalWS_a616d67d9a9241cf9d4230dbfe62563f_IsConnected;
        float3 _BranchOnInputConnection_565230d8358148bfac6929645968ec6d_Out_3_Vector3 = _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3_IsConnected ? _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3 : IN.WorldSpaceNormal;
        float3 _MainLightDirection_46d8c4b675334560b12bb67c4e1ab4ca_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_46d8c4b675334560b12bb67c4e1ab4ca_Direction_0_Vector3);
        float3 _Negate_cefa58b1986e43aa934b4f687b6018fd_Out_1_Vector3;
        Unity_Negate_float3(_MainLightDirection_46d8c4b675334560b12bb67c4e1ab4ca_Direction_0_Vector3, _Negate_cefa58b1986e43aa934b4f687b6018fd_Out_1_Vector3);
        float _DotProduct_b0063238199f49058ba189bac4da970d_Out_2_Float;
        Unity_DotProduct_float3(_BranchOnInputConnection_565230d8358148bfac6929645968ec6d_Out_3_Vector3, _Negate_cefa58b1986e43aa934b4f687b6018fd_Out_1_Vector3, _DotProduct_b0063238199f49058ba189bac4da970d_Out_2_Float);
        float _Multiply_d11fc76f5fe1410e894c6ac565e2d937_Out_2_Float;
        Unity_Multiply_float_float(_DotProduct_b0063238199f49058ba189bac4da970d_Out_2_Float, 0.5, _Multiply_d11fc76f5fe1410e894c6ac565e2d937_Out_2_Float);
        float _Add_1a63a84f8dfc4d8ca825f5717c5a9d07_Out_2_Float;
        Unity_Add_float(_Multiply_d11fc76f5fe1410e894c6ac565e2d937_Out_2_Float, float(0.5), _Add_1a63a84f8dfc4d8ca825f5717c5a9d07_Out_2_Float);
        float _Saturate_95b5072011dc4a2c899250596471f236_Out_1_Float;
        Unity_Saturate_float(_Add_1a63a84f8dfc4d8ca825f5717c5a9d07_Out_2_Float, _Saturate_95b5072011dc4a2c899250596471f236_Out_1_Float);
        Diffuse_1 = _Saturate_95b5072011dc4a2c899250596471f236_Out_1_Float;
        }
        
        void Unity_Exponential2_float(float In, out float Out)
        {
            Out = exp2(In);
        }
        
        struct Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float
        {
        };
        
        void SG_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float(float _In, Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float IN, out float Out_1)
        {
        float _Property_ad239b68c11f4fcb920425661841c000_Out_0_Float = _In;
        float _Multiply_187758eae33846cb912e3d80a692d9b0_Out_2_Float;
        Unity_Multiply_float_float(_Property_ad239b68c11f4fcb920425661841c000_Out_0_Float, 10, _Multiply_187758eae33846cb912e3d80a692d9b0_Out_2_Float);
        float _Add_023529f132584452b7ef9ffb6f67eb4b_Out_2_Float;
        Unity_Add_float(_Multiply_187758eae33846cb912e3d80a692d9b0_Out_2_Float, float(1), _Add_023529f132584452b7ef9ffb6f67eb4b_Out_2_Float);
        float _Exponential_b2744bb952e44d0cb0d813283e357f8c_Out_1_Float;
        Unity_Exponential2_float(_Add_023529f132584452b7ef9ffb6f67eb4b_Out_2_Float, _Exponential_b2744bb952e44d0cb0d813283e357f8c_Out_1_Float);
        Out_1 = _Exponential_b2744bb952e44d0cb0d813283e357f8c_Out_1_Float;
        }
        
        void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
        {
            Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
        }
        
        void Unity_Lerp_float(float A, float B, float T, out float Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        struct Bindings_Reflectance_3dd134828a90d7446802ac63dbc0221d_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        };
        
        void SG_Reflectance_3dd134828a90d7446802ac63dbc0221d_float(float3 _Base_Color, bool _Base_Color_57260f26a3c14234b948dd74141df9c7_IsConnected, float3 _NormalWS, bool _NormalWS_ab3d33962fc64884948809ad8eaf7aa5_IsConnected, float _Metallic, Bindings_Reflectance_3dd134828a90d7446802ac63dbc0221d_float IN, out float3 Reflectance_1)
        {
        float3 _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3 = _NormalWS;
        bool _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3_IsConnected = _NormalWS_ab3d33962fc64884948809ad8eaf7aa5_IsConnected;
        float3 _BranchOnInputConnection_2b572462617842bf85ea171e93239787_Out_3_Vector3 = _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3_IsConnected ? _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3 : IN.WorldSpaceNormal;
        float _FresnelEffect_c130e1f911b140299ff5363626d5f22b_Out_3_Float;
        Unity_FresnelEffect_float(_BranchOnInputConnection_2b572462617842bf85ea171e93239787_Out_3_Vector3, IN.WorldSpaceViewDirection, float(3), _FresnelEffect_c130e1f911b140299ff5363626d5f22b_Out_3_Float);
        float _Lerp_3ed50cf0a0a94d98a7de54752c2f2581_Out_3_Float;
        Unity_Lerp_float(float(0.04), float(1), _FresnelEffect_c130e1f911b140299ff5363626d5f22b_Out_3_Float, _Lerp_3ed50cf0a0a94d98a7de54752c2f2581_Out_3_Float);
        float3 _Property_0d65a942fa4c458ebf1d5b61426a6680_Out_0_Vector3 = _Base_Color;
        bool _Property_0d65a942fa4c458ebf1d5b61426a6680_Out_0_Vector3_IsConnected = _Base_Color_57260f26a3c14234b948dd74141df9c7_IsConnected;
        float _Property_0899e6b3a7dd4ecb9b9de1f735daed5e_Out_0_Float = _Metallic;
        float3 _Lerp_e80ebb2ee2ec40c2a06b11a4bab428cc_Out_3_Vector3;
        Unity_Lerp_float3((_Lerp_3ed50cf0a0a94d98a7de54752c2f2581_Out_3_Float.xxx), _Property_0d65a942fa4c458ebf1d5b61426a6680_Out_0_Vector3, (_Property_0899e6b3a7dd4ecb9b9de1f735daed5e_Out_0_Float.xxx), _Lerp_e80ebb2ee2ec40c2a06b11a4bab428cc_Out_3_Vector3);
        Reflectance_1 = _Lerp_e80ebb2ee2ec40c2a06b11a4bab428cc_Out_3_Vector3;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Normalize_float3(float3 In, out float3 Out)
        {
            Out = normalize(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
        Out = A * B;
        }
        
        struct Bindings_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        };
        
        void SG_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float(float3 _NormalWS, bool _NormalWS_764643e0455a4ece932285a0241516e7_IsConnected, float _Smoothness, float3 _Reflectance, Bindings_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float IN, out float3 Specular_1)
        {
        float3 _MainLightDirection_436ef0cdefe04605b70adabe76102606_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_436ef0cdefe04605b70adabe76102606_Direction_0_Vector3);
        float3 _Negate_6a9ea0a4355445dcb9ea2b983008a197_Out_1_Vector3;
        Unity_Negate_float3(_MainLightDirection_436ef0cdefe04605b70adabe76102606_Direction_0_Vector3, _Negate_6a9ea0a4355445dcb9ea2b983008a197_Out_1_Vector3);
        float3 _Add_fec1db3bbef240b8b1b7d4f12ea2c97f_Out_2_Vector3;
        Unity_Add_float3(IN.WorldSpaceViewDirection, _Negate_6a9ea0a4355445dcb9ea2b983008a197_Out_1_Vector3, _Add_fec1db3bbef240b8b1b7d4f12ea2c97f_Out_2_Vector3);
        float3 _Normalize_5df6e3535e5842a988f33d859b57987a_Out_1_Vector3;
        Unity_Normalize_float3(_Add_fec1db3bbef240b8b1b7d4f12ea2c97f_Out_2_Vector3, _Normalize_5df6e3535e5842a988f33d859b57987a_Out_1_Vector3);
        float3 _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3 = _NormalWS;
        bool _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3_IsConnected = _NormalWS_764643e0455a4ece932285a0241516e7_IsConnected;
        float3 _BranchOnInputConnection_c0a0900e17284fa980ff63a7456c2199_Out_3_Vector3 = _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3_IsConnected ? _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3 : IN.WorldSpaceNormal;
        float _DotProduct_fdf0af8bd536429fa4b57dc806a67c04_Out_2_Float;
        Unity_DotProduct_float3(_Normalize_5df6e3535e5842a988f33d859b57987a_Out_1_Vector3, _BranchOnInputConnection_c0a0900e17284fa980ff63a7456c2199_Out_3_Vector3, _DotProduct_fdf0af8bd536429fa4b57dc806a67c04_Out_2_Float);
        float _Saturate_716ed2bdf36e4e60a35f74d186b278e9_Out_1_Float;
        Unity_Saturate_float(_DotProduct_fdf0af8bd536429fa4b57dc806a67c04_Out_2_Float, _Saturate_716ed2bdf36e4e60a35f74d186b278e9_Out_1_Float);
        float _Saturate_1d23f425974541b381f4fa80cf5a00b1_Out_1_Float;
        Unity_Saturate_float(_Saturate_716ed2bdf36e4e60a35f74d186b278e9_Out_1_Float, _Saturate_1d23f425974541b381f4fa80cf5a00b1_Out_1_Float);
        float _Property_1766606f1117460388cc77e7505598dd_Out_0_Float = _Smoothness;
        Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4;
        half _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4_Out_1_Float;
        SG_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float(_Property_1766606f1117460388cc77e7505598dd_Out_0_Float, _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4, _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4_Out_1_Float);
        float _Power_c507d979651749238e129118bb4c252f_Out_2_Float;
        Unity_Power_float(_Saturate_1d23f425974541b381f4fa80cf5a00b1_Out_1_Float, _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4_Out_1_Float, _Power_c507d979651749238e129118bb4c252f_Out_2_Float);
        float3 _Property_ab8c048e4ae44d56bf8dcb1bbc8ada89_Out_0_Vector3 = _Reflectance;
        float3 _Multiply_3c43a7f74b6c4c35a534990268ada20a_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Power_c507d979651749238e129118bb4c252f_Out_2_Float.xxx), _Property_ab8c048e4ae44d56bf8dcb1bbc8ada89_Out_0_Vector3, _Multiply_3c43a7f74b6c4c35a534990268ada20a_Out_2_Vector3);
        Specular_1 = _Multiply_3c43a7f74b6c4c35a534990268ada20a_Out_2_Vector3;
        }
        
        // unity-custom-func-begin
        void MainLightString_float(float3 worldPos, out float3 direction, out float3 color, out float shadowAtt){
        #ifdef SHADERGRAPH_PREVIEW
          direction = normalize(float3(-0.7,0.7,-0.7));
          color = float3(1,1,1);
          shadowAtt = 1;
        #else
          #if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)
            float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
            Light mainLight = GetMainLight(shadowCoord);
             direction = mainLight.direction;
        
             color = mainLight.color;
        
             shadowAtt = mainLight.shadowAttenuation;
          #else
            direction = normalize(float3(-0.7,0.7,-0.7));
            color = float3(1,1,1);
            shadowAtt = 1;
          #endif
        #endif
        }
        // unity-custom-func-end
        
        struct Bindings_MainLight_0157caa4ea90d88499c9fdb016582871_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_MainLight_0157caa4ea90d88499c9fdb016582871_float(Bindings_MainLight_0157caa4ea90d88499c9fdb016582871_float IN, out float3 Direction_1, out float3 Color_2, out float ShadowAtt_3)
        {
        float3 _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_direction_1_Vector3;
        float3 _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_color_2_Vector3;
        float _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_shadowAtt_3_Float;
        MainLightString_float(IN.WorldSpacePosition, _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_direction_1_Vector3, _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_color_2_Vector3, _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_shadowAtt_3_Float);
        Direction_1 = _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_direction_1_Vector3;
        Color_2 = _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_color_2_Vector3;
        ShadowAtt_3 = _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_shadowAtt_3_Float;
        }
        
        void Unity_ViewVectorWorld_float(out float3 Out, float3 WorldSpacePosition)
        {
            Out = _WorldSpaceCameraPos.xyz - GetAbsolutePositionWS(WorldSpacePosition);
            if(!IsPerspectiveProjection())
            {
                Out = GetViewForwardDir() * dot(Out, GetViewForwardDir());
            }
        }
        
        // unity-custom-func-begin
        void AddAdditionalLights_float(float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView, float MainDiffuse, float3 MainSpecular, float3 MainColor, out float Diffuse, out float3 Specular, out float3 Color){
        // Initialize outputs
        
        Diffuse = 0;
        
        Specular = 0;
        
        Color = 0;
        
        
        
        #ifndef SHADERGRAPH_PREVIEW
        
        
        
           // --- MAIN LIGHT ---
        
          Light mainLight = GetMainLight();
        
          mainLight.shadowAttenuation = MainLightRealtimeShadow(float4(WorldPosition, 1.0));
        
          float mainAtten = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
        
         
        
        
        
          float mainNdotL = saturate(dot(WorldNormal, mainLight.direction));
        
          float mainDiffuse = mainAtten * mainNdotL;
        
          float3 mainSpecular = LightingSpecular(mainDiffuse, mainLight.direction, WorldNormal, WorldView, 1, Smoothness);
        
        
        
          Diffuse += mainDiffuse;
        
          Specular += mainSpecular;
        
          Color += mainLight.color * (mainDiffuse + mainSpecular);
        
        
        
            // --- ADDITIONAL LIGHTS ---
        
            uint pixelLightCount = GetAdditionalLightsCount();
        
        
        
            LIGHT_LOOP_BEGIN(pixelLightCount)
        
                
        
                Light light = GetAdditionalPerObjectLight(lightIndex, WorldPosition);
        
        
        
                light.shadowAttenuation = AdditionalLightRealtimeShadow(lightIndex, WorldPosition, light.direction);
        
                float atten = light.distanceAttenuation * light.shadowAttenuation;
        
        
        
                float NdotL = saturate(dot(WorldNormal, light.direction) * 0.5 + 0.5);
        
                float thisDiffuse = atten * NdotL;
        
                float3 thisSpecular = LightingSpecular(thisDiffuse, light.direction, WorldNormal, WorldView, 1, Smoothness);
        
        
        
                Diffuse += thisDiffuse;
        
                Specular += thisSpecular;
        
                     #if defined(_LIGHT_COOKIES)
        
        	      float3 cookieColor = SampleAdditionalLightCookie(lightIndex, WorldPosition);
        
        	      light.color *= cookieColor;
        
                     #endif
        
                Color += light.color * (thisDiffuse + thisSpecular);
        
            LIGHT_LOOP_END
        
        
        
            // Normalize total lighting
        
            //Color = MainColor * Color;
        
        float totalWeight = Diffuse + dot(Specular, float3(0.333, 0.333, 0.333));
        
            Color = totalWeight > 0 ? MainColor * (Color / totalWeight) : MainColor;
        
        
        
        #endif
        }
        // unity-custom-func-end
        
        struct Bindings_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float(float _MainLightDiffuse, float3 _MainLightSpecular, float3 _MainLightColor, float3 _NormalWS, float _Smoothness, Bindings_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float IN, out float Diffuse_1, out float3 Specular_2, out float3 Color_3)
        {
        float _Property_f5ac72f32dc84ae988391b7d51172380_Out_0_Float = _Smoothness;
        float3 _Property_85d85c5164d5483787fecbcecfd6a519_Out_0_Vector3 = _NormalWS;
        float3 _ViewVector_0d331f2b495741bcb8795caa9549f016_Out_0_Vector3;
        Unity_ViewVectorWorld_float(_ViewVector_0d331f2b495741bcb8795caa9549f016_Out_0_Vector3, IN.WorldSpacePosition);
        float _Property_51c13cc19f414dacb86745cec42d9ab6_Out_0_Float = _MainLightDiffuse;
        float3 _Property_f1e96792c3cc4a1e90396147ef9f7160_Out_0_Vector3 = _MainLightColor;
        float _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Diffuse_7_Float;
        float3 _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Specular_8_Vector3;
        float3 _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Color_9_Vector3;
        AddAdditionalLights_float(_Property_f5ac72f32dc84ae988391b7d51172380_Out_0_Float, IN.WorldSpacePosition, _Property_85d85c5164d5483787fecbcecfd6a519_Out_0_Vector3, _ViewVector_0d331f2b495741bcb8795caa9549f016_Out_0_Vector3, _Property_51c13cc19f414dacb86745cec42d9ab6_Out_0_Float, _Property_f1e96792c3cc4a1e90396147ef9f7160_Out_0_Vector3, _Property_f1e96792c3cc4a1e90396147ef9f7160_Out_0_Vector3, _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Diffuse_7_Float, _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Specular_8_Vector3, _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Color_9_Vector3);
        Diffuse_1 = _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Diffuse_7_Float;
        Specular_2 = _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Specular_8_Vector3;
        Color_3 = _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Color_9_Vector3;
        }
        
        void Unity_Posterize_float3(float3 In, float3 Steps, out float3 Out)
        {
            Out = floor(In / (1 / Steps)) * (1 / Steps);
        }
        
        void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
        {
            float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
            Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
        }
        
        // unity-custom-func-begin
        void GetSSAO_float(float2 ScreenPos, out float DirectAO, out float IndirectAO){
         #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT) && !defined(SHADERGRAPH_PREVIEW)
        
         
          float ssao = saturate(SampleAmbientOcclusion(ScreenPos) + (1.0 - _AmbientOcclusionParam.x));
        
           IndirectAO = ssao;
        
           DirectAO = lerp(1.0, ssao, _AmbientOcclusionParam.w);
        
        #else
        
           DirectAO = 1.0;
        
           IndirectAO = 1.0;
        
        #endif
        }
        // unity-custom-func-end
        
        struct Bindings_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float
        {
        float2 NDCPosition;
        };
        
        void SG_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float(float2 _ScreenPos, bool _ScreenPos_72149ac21c784cc8b4614750ec683413_IsConnected, Bindings_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float IN, out float DirectAO_1, out float IndirectAO_2)
        {
        float2 _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2 = _ScreenPos;
        bool _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2_IsConnected = _ScreenPos_72149ac21c784cc8b4614750ec683413_IsConnected;
        float4 _ScreenPosition_a193bed040594170a08ae04fed7ea8a7_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
        float2 _BranchOnInputConnection_8e8119e37af64e9a957c820002cea48c_Out_3_Vector2 = _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2_IsConnected ? _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2 : (_ScreenPosition_a193bed040594170a08ae04fed7ea8a7_Out_0_Vector4.xy);
        float _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_DirectAO_1_Float;
        float _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_IndirectAO_2_Float;
        GetSSAO_float(_BranchOnInputConnection_8e8119e37af64e9a957c820002cea48c_Out_3_Vector2, _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_DirectAO_1_Float, _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_IndirectAO_2_Float);
        DirectAO_1 = _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_DirectAO_1_Float;
        IndirectAO_2 = _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_IndirectAO_2_Float;
        }
        
        void Unity_Minimum_float(float A, float B, out float Out)
        {
            Out = min(A, B);
        };
        
        void Unity_ReflectionProbe_float(float3 ViewDir, float3 Normal, float LOD, out float3 Out)
        {
            Out = SHADERGRAPH_REFLECTION_PROBE(ViewDir, Normal, LOD);
        }
        
        struct Bindings_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        float3 WorldSpacePosition;
        float2 NDCPosition;
        float2 PixelPosition;
        half4 uv1;
        half4 uv2;
        };
        
        void SG_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float(float3 _Base_Color, bool _Base_Color_87794c9dccf9430d8ee1585f52796223_IsConnected, float3 _NormalWS, bool _NormalWS_d8d26d45dd0b4d4a8e8426edcd206b0f_IsConnected, float _AO, float _Smoothness, float _Metallic, float _Reflectance, Bindings_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float IN, out float3 Ambient_1, out float3 Metallic_2, out float DdirectAO_3)
        {
        float3 _Property_50e51e65f64b40c29fa7b10305f0a7b4_Out_0_Vector3 = _Base_Color;
        bool _Property_50e51e65f64b40c29fa7b10305f0a7b4_Out_0_Vector3_IsConnected = _Base_Color_87794c9dccf9430d8ee1585f52796223_IsConnected;
        float _Property_b967758667b74ff5acb2a8af3ff58c39_Out_0_Float = _Metallic;
        float3 _Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3;
        Unity_Lerp_float3(_Property_50e51e65f64b40c29fa7b10305f0a7b4_Out_0_Vector3, float3(0, 0, 0), (_Property_b967758667b74ff5acb2a8af3ff58c39_Out_0_Float.xxx), _Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3);
        float3 _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3 = _NormalWS;
        bool _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3_IsConnected = _NormalWS_d8d26d45dd0b4d4a8e8426edcd206b0f_IsConnected;
        float3 _BranchOnInputConnection_dae192f178d24a5a9c9c540fd5488461_Out_3_Vector3 = _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3_IsConnected ? _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3 : IN.WorldSpaceNormal;
        float3 _BakedGI_1e2ac029f59d497abdc2a7bcf1eb3346_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, _BranchOnInputConnection_dae192f178d24a5a9c9c540fd5488461_Out_3_Vector3, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
        float _Property_26a27af7a1144ea0a77aad0b49f12697_Out_0_Float = _AO;
        Bindings_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float _SSAO_f6107fe543294e7f932b8c5cfc86bdd5;
        _SSAO_f6107fe543294e7f932b8c5cfc86bdd5.NDCPosition = IN.NDCPosition;
        half _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_DirectAO_1_Float;
        half _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_IndirectAO_2_Float;
        SG_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float(half2 (0, 0), false, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_DirectAO_1_Float, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_IndirectAO_2_Float);
        float _Minimum_3ac81a32823a439c8b51e89edf949c26_Out_2_Float;
        Unity_Minimum_float(_Property_26a27af7a1144ea0a77aad0b49f12697_Out_0_Float, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_IndirectAO_2_Float, _Minimum_3ac81a32823a439c8b51e89edf949c26_Out_2_Float);
        float3 _Multiply_423df8bc24d94add8f44f6db0bf46798_Out_2_Vector3;
        Unity_Multiply_float3_float3(_BakedGI_1e2ac029f59d497abdc2a7bcf1eb3346_Out_1_Vector3, (_Minimum_3ac81a32823a439c8b51e89edf949c26_Out_2_Float.xxx), _Multiply_423df8bc24d94add8f44f6db0bf46798_Out_2_Vector3);
        float3 _Multiply_4ee1636476de4393a1b2e9162c2328b9_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3, _Multiply_423df8bc24d94add8f44f6db0bf46798_Out_2_Vector3, _Multiply_4ee1636476de4393a1b2e9162c2328b9_Out_2_Vector3);
        float _Property_76583d84384b44cbb79843c917044c1b_Out_0_Float = _Reflectance;
        float _Property_f511aceb4377409690a68e8d8a16a032_Out_0_Float = _Smoothness;
        float _Lerp_496af92f7e424d5586ca49ffe949aff6_Out_3_Float;
        Unity_Lerp_float(float(8), float(0), _Property_f511aceb4377409690a68e8d8a16a032_Out_0_Float, _Lerp_496af92f7e424d5586ca49ffe949aff6_Out_3_Float);
        float3 _ReflectionProbe_d9eb69c9b54641068d948dd37fec342f_Out_3_Vector3;
        Unity_ReflectionProbe_float(IN.WorldSpaceViewDirection, _BranchOnInputConnection_dae192f178d24a5a9c9c540fd5488461_Out_3_Vector3, _Lerp_496af92f7e424d5586ca49ffe949aff6_Out_3_Float, _ReflectionProbe_d9eb69c9b54641068d948dd37fec342f_Out_3_Vector3);
        float3 _Multiply_25ab7f2171954fc4a710fe8c8c2cc402_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Property_76583d84384b44cbb79843c917044c1b_Out_0_Float.xxx), _ReflectionProbe_d9eb69c9b54641068d948dd37fec342f_Out_3_Vector3, _Multiply_25ab7f2171954fc4a710fe8c8c2cc402_Out_2_Vector3);
        float3 _Add_475a3a92ac834f51b869dedfbba66e44_Out_2_Vector3;
        Unity_Add_float3(_Multiply_4ee1636476de4393a1b2e9162c2328b9_Out_2_Vector3, _Multiply_25ab7f2171954fc4a710fe8c8c2cc402_Out_2_Vector3, _Add_475a3a92ac834f51b869dedfbba66e44_Out_2_Vector3);
        Ambient_1 = _Add_475a3a92ac834f51b869dedfbba66e44_Out_2_Vector3;
        Metallic_2 = _Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3;
        DdirectAO_3 = _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_DirectAO_1_Float;
        }
        
        void Unity_Saturate_float3(float3 In, out float3 Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Posterize_float(float In, float Steps, out float Out)
        {
            Out = floor(In / (1 / Steps)) * (1 / Steps);
        }
        
        struct Bindings_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        float3 WorldSpacePosition;
        float2 NDCPosition;
        float2 PixelPosition;
        half4 uv1;
        half4 uv2;
        };
        
        void SG_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float(float3 _Base_Color, bool _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected, float3 _NormalWS, bool _NormalWS_d2c1afa83f4f4d20ac34f445f069483d_IsConnected, float _Metallic, float _Smoothness, float _AO, float _Steps, float _Specular_Steps, float _Smoothness_Strength, Bindings_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float IN, out float3 Lit_1)
        {
        float3 _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3 = _NormalWS;
        bool _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3_IsConnected = _NormalWS_d2c1afa83f4f4d20ac34f445f069483d_IsConnected;
        float3 _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3 = _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3_IsConnected ? _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3 : IN.WorldSpaceNormal;
        Bindings_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91;
        _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91.WorldSpaceNormal = IN.WorldSpaceNormal;
        half _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float;
        SG_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float(_BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91, _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float);
        float _Property_af2c0fb6df7d4dc9abade557540f3ed7_Out_0_Float = _Smoothness;
        Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e;
        half _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float;
        SG_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float(_Property_af2c0fb6df7d4dc9abade557540f3ed7_Out_0_Float, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float);
        float3 _Property_b3987ddafd854a0a85863bc89dcde720_Out_0_Vector3 = _Base_Color;
        bool _Property_b3987ddafd854a0a85863bc89dcde720_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float _Property_3c53a42fedba46ba9fae794424fbe85e_Out_0_Float = _Metallic;
        Bindings_Reflectance_3dd134828a90d7446802ac63dbc0221d_float _Reflectance_0d5aeeef450744cf885ab04f87797c7b;
        _Reflectance_0d5aeeef450744cf885ab04f87797c7b.WorldSpaceNormal = IN.WorldSpaceNormal;
        _Reflectance_0d5aeeef450744cf885ab04f87797c7b.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3;
        SG_Reflectance_3dd134828a90d7446802ac63dbc0221d_float(_Property_b3987ddafd854a0a85863bc89dcde720_Out_0_Vector3, true, _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _Property_3c53a42fedba46ba9fae794424fbe85e_Out_0_Float, _Reflectance_0d5aeeef450744cf885ab04f87797c7b, _Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3);
        Bindings_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8;
        _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8.WorldSpaceNormal = IN.WorldSpaceNormal;
        _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8_Specular_1_Vector3;
        SG_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float(_BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float, _Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3, _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8, _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8_Specular_1_Vector3);
        float3 _Multiply_577b66af727b4e43be242c88de8def99_Out_2_Vector3;
        Unity_Multiply_float3_float3((_HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float.xxx), _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8_Specular_1_Vector3, _Multiply_577b66af727b4e43be242c88de8def99_Out_2_Vector3);
        Bindings_MainLight_0157caa4ea90d88499c9fdb016582871_float _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a;
        _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a.WorldSpacePosition = IN.WorldSpacePosition;
        float3 _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Direction_1_Vector3;
        float3 _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Color_2_Vector3;
        float _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_ShadowAtt_3_Float;
        SG_MainLight_0157caa4ea90d88499c9fdb016582871_float(_MainLight_1fffd9ee56744cde8cd3210b8fc0a55a, _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Direction_1_Vector3, _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Color_2_Vector3, _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_ShadowAtt_3_Float);
        float3 _Multiply_e3a50f2fbf214e64931956c0b1e7f194_Out_2_Vector3;
        Unity_Multiply_float3_float3(_MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Color_2_Vector3, (_MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_ShadowAtt_3_Float.xxx), _Multiply_e3a50f2fbf214e64931956c0b1e7f194_Out_2_Vector3);
        Bindings_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021;
        _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021.WorldSpacePosition = IN.WorldSpacePosition;
        float _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Diffuse_1_Float;
        float3 _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Specular_2_Vector3;
        float3 _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3;
        SG_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float(_HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float, _Multiply_577b66af727b4e43be242c88de8def99_Out_2_Vector3, _Multiply_e3a50f2fbf214e64931956c0b1e7f194_Out_2_Vector3, _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Diffuse_1_Float, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Specular_2_Vector3, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3);
        float _Property_7fabb829117947b8956799cbdf3be9e0_Out_0_Float = _Steps;
        float3 _Posterize_8f26285a65784a038d6d9d2087181b46_Out_2_Vector3;
        Unity_Posterize_float3(_AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, (_Property_7fabb829117947b8956799cbdf3be9e0_Out_0_Float.xxx), _Posterize_8f26285a65784a038d6d9d2087181b46_Out_2_Vector3);
        float3 _Property_3668f71779994b3a9fcc7095f880cb12_Out_0_Vector3 = _Base_Color;
        bool _Property_3668f71779994b3a9fcc7095f880cb12_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float3 _Saturation_e12bfe3d861c4f1fa4c1297d0dd08cc0_Out_2_Vector3;
        Unity_Saturation_float(_Property_3668f71779994b3a9fcc7095f880cb12_Out_0_Vector3, float(1.6), _Saturation_e12bfe3d861c4f1fa4c1297d0dd08cc0_Out_2_Vector3);
        float3 _Multiply_77c53f8596dd487fa3009d208e99c0fd_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Saturation_e12bfe3d861c4f1fa4c1297d0dd08cc0_Out_2_Vector3, float3(0.1, 0.1, 0.1), _Multiply_77c53f8596dd487fa3009d208e99c0fd_Out_2_Vector3);
        float3 _Property_494170212d604ba0a52a5b39f34e4c3f_Out_0_Vector3 = _Base_Color;
        bool _Property_494170212d604ba0a52a5b39f34e4c3f_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float3 _Multiply_e22a63bb0da045618a0202e45051407e_Out_2_Vector3;
        Unity_Multiply_float3_float3((_AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Diffuse_1_Float.xxx), _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, _Multiply_e22a63bb0da045618a0202e45051407e_Out_2_Vector3);
        float3 _Property_53d2f61f22684efca5bbd024184799a4_Out_0_Vector3 = _Base_Color;
        bool _Property_53d2f61f22684efca5bbd024184799a4_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float _Property_5204695d7d6d4d03b28eba481d372192_Out_0_Float = _AO;
        float _Property_aa169c45708b4343b76a3b52b483b8e2_Out_0_Float = _Smoothness;
        float _Property_1d116579c59d45ccaa1bdcb07a5ec5d5_Out_0_Float = _Metallic;
        Bindings_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.WorldSpaceNormal = IN.WorldSpaceNormal;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.WorldSpacePosition = IN.WorldSpacePosition;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.NDCPosition = IN.NDCPosition;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.PixelPosition = IN.PixelPosition;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.uv1 = IN.uv1;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.uv2 = IN.uv2;
        half3 _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Ambient_1_Vector3;
        half3 _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Metallic_2_Vector3;
        half _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_DdirectAO_3_Float;
        SG_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float(_Property_53d2f61f22684efca5bbd024184799a4_Out_0_Vector3, true, _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _Property_5204695d7d6d4d03b28eba481d372192_Out_0_Float, _Property_aa169c45708b4343b76a3b52b483b8e2_Out_0_Float, _Property_1d116579c59d45ccaa1bdcb07a5ec5d5_Out_0_Float, (_Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3).x, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Ambient_1_Vector3, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Metallic_2_Vector3, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_DdirectAO_3_Float);
        float3 _Add_96c33650335248caa7429346ca897bb7_Out_2_Vector3;
        Unity_Add_float3(_Multiply_e22a63bb0da045618a0202e45051407e_Out_2_Vector3, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Ambient_1_Vector3, _Add_96c33650335248caa7429346ca897bb7_Out_2_Vector3);
        float3 _Saturate_b993ba602dea4759baa643682158093c_Out_1_Vector3;
        Unity_Saturate_float3(_Add_96c33650335248caa7429346ca897bb7_Out_2_Vector3, _Saturate_b993ba602dea4759baa643682158093c_Out_1_Vector3);
        float3 _Saturation_559bfc31eca847d3acd46b2fd284dc13_Out_2_Vector3;
        Unity_Saturation_float(_Saturate_b993ba602dea4759baa643682158093c_Out_1_Vector3, float(1), _Saturation_559bfc31eca847d3acd46b2fd284dc13_Out_2_Vector3);
        float _Swizzle_3952ca1d9d274331b6a4a49630c606ae_Out_1_Float = _Saturation_559bfc31eca847d3acd46b2fd284dc13_Out_2_Vector3.x;
        float _Posterize_268e85bcc1d5424485e214740b6468e3_Out_2_Float;
        Unity_Posterize_float(_Swizzle_3952ca1d9d274331b6a4a49630c606ae_Out_1_Float, _Property_7fabb829117947b8956799cbdf3be9e0_Out_0_Float, _Posterize_268e85bcc1d5424485e214740b6468e3_Out_2_Float);
        float3 _Lerp_8c017a3a82434ee38c4432bf457e582b_Out_3_Vector3;
        Unity_Lerp_float3(_Multiply_77c53f8596dd487fa3009d208e99c0fd_Out_2_Vector3, _Property_494170212d604ba0a52a5b39f34e4c3f_Out_0_Vector3, (_Posterize_268e85bcc1d5424485e214740b6468e3_Out_2_Float.xxx), _Lerp_8c017a3a82434ee38c4432bf457e582b_Out_3_Vector3);
        float3 _Multiply_5c248bf4893d4acd9b10421fae8923d1_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Posterize_8f26285a65784a038d6d9d2087181b46_Out_2_Vector3, _Lerp_8c017a3a82434ee38c4432bf457e582b_Out_3_Vector3, _Multiply_5c248bf4893d4acd9b10421fae8923d1_Out_2_Vector3);
        float _Property_48b9a97103af41f3b6cafeceb8a6a0b7_Out_0_Float = _Smoothness_Strength;
        float3 _Add_57faecf3e31346a68db61c3e666b2975_Out_2_Vector3;
        Unity_Add_float3((_Property_48b9a97103af41f3b6cafeceb8a6a0b7_Out_0_Float.xxx), _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, _Add_57faecf3e31346a68db61c3e666b2975_Out_2_Vector3);
        float3 _Multiply_b90f73fc105c4da3b7586428b54d0c03_Out_2_Vector3;
        Unity_Multiply_float3_float3(_AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Specular_2_Vector3, _Add_57faecf3e31346a68db61c3e666b2975_Out_2_Vector3, _Multiply_b90f73fc105c4da3b7586428b54d0c03_Out_2_Vector3);
        float3 _Saturation_c81f01b7b0de4608a25fde9890c55880_Out_2_Vector3;
        Unity_Saturation_float(_Multiply_b90f73fc105c4da3b7586428b54d0c03_Out_2_Vector3, float(0), _Saturation_c81f01b7b0de4608a25fde9890c55880_Out_2_Vector3);
        float _Swizzle_fbfd922c5f7d42d399186d59a86d5660_Out_1_Float = _Saturation_c81f01b7b0de4608a25fde9890c55880_Out_2_Vector3.x;
        float _Property_62c311d0a3534ad1ad3a7b66c978a36c_Out_0_Float = _Specular_Steps;
        float _Posterize_1027f91210d94108ae1c593d0fb61d8b_Out_2_Float;
        Unity_Posterize_float(_Swizzle_fbfd922c5f7d42d399186d59a86d5660_Out_1_Float, _Property_62c311d0a3534ad1ad3a7b66c978a36c_Out_0_Float, _Posterize_1027f91210d94108ae1c593d0fb61d8b_Out_2_Float);
        float3 _Multiply_c92faf2f8cc04ff29ec2ba172ebefa59_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Posterize_1027f91210d94108ae1c593d0fb61d8b_Out_2_Float.xxx), _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, _Multiply_c92faf2f8cc04ff29ec2ba172ebefa59_Out_2_Vector3);
        float3 _Add_321e04b0f3bb47558352363c94828b10_Out_2_Vector3;
        Unity_Add_float3(_Multiply_5c248bf4893d4acd9b10421fae8923d1_Out_2_Vector3, _Multiply_c92faf2f8cc04ff29ec2ba172ebefa59_Out_2_Vector3, _Add_321e04b0f3bb47558352363c94828b10_Out_2_Vector3);
        Lit_1 = _Add_321e04b0f3bb47558352363c94828b10_Out_2_Vector3;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float3 Emission;
            float Metallic;
            float Smoothness;
            float Occlusion;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_BaseMap);
            float4 _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D.tex, _Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D.samplerstate, _Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_R_4_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.r;
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_G_5_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.g;
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_B_6_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.b;
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_A_7_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.a;
            float _Property_7ab914554b2b477db515a77874d1d4cd_Out_0_Float = _LightingSmoothness;
            float _Property_9e7bc9fe055c4e4db532299787c0e3ed_Out_0_Float = _Steps;
            float _Property_9d34386f48e54d9eaeda9d04da1ad72f_Out_0_Float = _SpecularSteps;
            Bindings_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.WorldSpaceNormal = IN.WorldSpaceNormal;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.WorldSpacePosition = IN.WorldSpacePosition;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.NDCPosition = IN.NDCPosition;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.PixelPosition = IN.PixelPosition;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.uv1 = IN.uv1;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.uv2 = IN.uv2;
            float3 _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3_Lit_1_Vector3;
            SG_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float((_SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.xyz), true, float3 (0, 0, 0), false, float(0), _Property_7ab914554b2b477db515a77874d1d4cd_Out_0_Float, float(0), _Property_9e7bc9fe055c4e4db532299787c0e3ed_Out_0_Float, _Property_9d34386f48e54d9eaeda9d04da1ad72f_Out_0_Float, float(0), _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3, _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3_Lit_1_Vector3);
            UnityTexture2D _Property_242f17afd4494f1eae45e57743744c8c_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_EmissiveMask);
            float4 _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_242f17afd4494f1eae45e57743744c8c_Out_0_Texture2D.tex, _Property_242f17afd4494f1eae45e57743744c8c_Out_0_Texture2D.samplerstate, _Property_242f17afd4494f1eae45e57743744c8c_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_R_4_Float = _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_RGBA_0_Vector4.r;
            float _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_G_5_Float = _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_RGBA_0_Vector4.g;
            float _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_B_6_Float = _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_RGBA_0_Vector4.b;
            float _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_A_7_Float = _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_RGBA_0_Vector4.a;
            float4 _Multiply_ea903ee047b5477c855ed2b94b6227f2_Out_2_Vector4;
            Unity_Multiply_float4_float4(_SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4, (_SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_R_4_Float.xxxx), _Multiply_ea903ee047b5477c855ed2b94b6227f2_Out_2_Vector4);
            float _Property_21326e76ba4e4c4c99157b1ec259c331_Out_0_Float = _EmissiveIntensity;
            float4 _Multiply_3aa49a8e912d45d09c831299a6a9a550_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Multiply_ea903ee047b5477c855ed2b94b6227f2_Out_2_Vector4, (_Property_21326e76ba4e4c4c99157b1ec259c331_Out_0_Float.xxxx), _Multiply_3aa49a8e912d45d09c831299a6a9a550_Out_2_Vector4);
            UnityTexture2D _Property_4dd97d0be9684f7c9b36d6893a49cc04_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MetalicMAp);
            float4 _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_4dd97d0be9684f7c9b36d6893a49cc04_Out_0_Texture2D.tex, _Property_4dd97d0be9684f7c9b36d6893a49cc04_Out_0_Texture2D.samplerstate, _Property_4dd97d0be9684f7c9b36d6893a49cc04_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_R_4_Float = _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_RGBA_0_Vector4.r;
            float _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_G_5_Float = _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_RGBA_0_Vector4.g;
            float _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_B_6_Float = _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_RGBA_0_Vector4.b;
            float _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_A_7_Float = _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_RGBA_0_Vector4.a;
            UnityTexture2D _Property_7854e5ede0844f91bd8de3bafec90eb0_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_RoughnessMap);
            float4 _SampleTexture2D_3a075f150d404985850c2dcacec42009_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7854e5ede0844f91bd8de3bafec90eb0_Out_0_Texture2D.tex, _Property_7854e5ede0844f91bd8de3bafec90eb0_Out_0_Texture2D.samplerstate, _Property_7854e5ede0844f91bd8de3bafec90eb0_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_3a075f150d404985850c2dcacec42009_R_4_Float = _SampleTexture2D_3a075f150d404985850c2dcacec42009_RGBA_0_Vector4.r;
            float _SampleTexture2D_3a075f150d404985850c2dcacec42009_G_5_Float = _SampleTexture2D_3a075f150d404985850c2dcacec42009_RGBA_0_Vector4.g;
            float _SampleTexture2D_3a075f150d404985850c2dcacec42009_B_6_Float = _SampleTexture2D_3a075f150d404985850c2dcacec42009_RGBA_0_Vector4.b;
            float _SampleTexture2D_3a075f150d404985850c2dcacec42009_A_7_Float = _SampleTexture2D_3a075f150d404985850c2dcacec42009_RGBA_0_Vector4.a;
            UnityTexture2D _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_AlphaMap);
            float4 _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.tex, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.samplerstate, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_R_4_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.r;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_G_5_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.g;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_B_6_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.b;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.a;
            surface.BaseColor = _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3_Lit_1_Vector3;
            surface.NormalTS = IN.TangentSpaceNormal;
            surface.Emission = (_Multiply_3aa49a8e912d45d09c831299a6a9a550_Out_2_Vector4.xyz);
            surface.Metallic = _SampleTexture2D_d0455797d15e4d3787a030c0de2e30cc_R_4_Float;
            surface.Smoothness = _SampleTexture2D_3a075f150d404985850c2dcacec42009_R_4_Float;
            surface.Occlusion = float(1);
            surface.Alpha = float(1);
            surface.AlphaClipThreshold = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
            // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            float3 unnormalizedNormalWS = input.normalWS;
            const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        
        
            output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
            output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
            output.WorldSpacePosition = input.positionWS;
        
            #if UNITY_UV_STARTS_AT_TOP
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #else
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #endif
        
            output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
            output.NDCPosition.y = 1.0f - output.NDCPosition.y;
        
            output.uv0 = input.texCoord0;
            output.uv1 = input.texCoord1;
            output.uv2 = input.texCoord2;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
        
        // Render State
        Cull Back
        ZTest LEqual
        ZWrite On
        ColorMask 0
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_SHADOWCASTER
        #define _ALPHATEST_ON 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0 : INTERP0;
             float3 normalWS : INTERP1;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _EmissiveIntensity;
        float4 _BaseMap_TexelSize;
        float4 _EmissiveMask_TexelSize;
        float4 _RoughnessMap_TexelSize;
        float4 _MetalicMAp_TexelSize;
        float4 _AlphaMap_TexelSize;
        float _LightingSmoothness;
        float _Steps;
        float _SpecularSteps;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissiveMask);
        SAMPLER(sampler_EmissiveMask);
        TEXTURE2D(_RoughnessMap);
        SAMPLER(sampler_RoughnessMap);
        TEXTURE2D(_MetalicMAp);
        SAMPLER(sampler_MetalicMAp);
        TEXTURE2D(_AlphaMap);
        SAMPLER(sampler_AlphaMap);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_AlphaMap);
            float4 _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.tex, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.samplerstate, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_R_4_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.r;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_G_5_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.g;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_B_6_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.b;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.a;
            surface.Alpha = float(1);
            surface.AlphaClipThreshold = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.uv0 = input.texCoord0;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "MotionVectors"
            Tags
            {
                "LightMode" = "MotionVectors"
            }
        
        // Render State
        Cull Back
        ZTest LEqual
        ZWrite On
        ColorMask RG
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.5
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_MOTION_VECTORS
        #define _ALPHATEST_ON 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _EmissiveIntensity;
        float4 _BaseMap_TexelSize;
        float4 _EmissiveMask_TexelSize;
        float4 _RoughnessMap_TexelSize;
        float4 _MetalicMAp_TexelSize;
        float4 _AlphaMap_TexelSize;
        float _LightingSmoothness;
        float _Steps;
        float _SpecularSteps;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissiveMask);
        SAMPLER(sampler_EmissiveMask);
        TEXTURE2D(_RoughnessMap);
        SAMPLER(sampler_RoughnessMap);
        TEXTURE2D(_MetalicMAp);
        SAMPLER(sampler_MetalicMAp);
        TEXTURE2D(_AlphaMap);
        SAMPLER(sampler_AlphaMap);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_AlphaMap);
            float4 _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.tex, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.samplerstate, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_R_4_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.r;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_G_5_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.g;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_B_6_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.b;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.a;
            surface.Alpha = float(1);
            surface.AlphaClipThreshold = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpacePosition =                        input.positionOS;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.uv0 = input.texCoord0;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/MotionVectorPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
        
        // Render State
        Cull Back
        ZTest LEqual
        ZWrite On
        ColorMask R
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define _ALPHATEST_ON 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _EmissiveIntensity;
        float4 _BaseMap_TexelSize;
        float4 _EmissiveMask_TexelSize;
        float4 _RoughnessMap_TexelSize;
        float4 _MetalicMAp_TexelSize;
        float4 _AlphaMap_TexelSize;
        float _LightingSmoothness;
        float _Steps;
        float _SpecularSteps;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissiveMask);
        SAMPLER(sampler_EmissiveMask);
        TEXTURE2D(_RoughnessMap);
        SAMPLER(sampler_RoughnessMap);
        TEXTURE2D(_MetalicMAp);
        SAMPLER(sampler_MetalicMAp);
        TEXTURE2D(_AlphaMap);
        SAMPLER(sampler_AlphaMap);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_AlphaMap);
            float4 _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.tex, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.samplerstate, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_R_4_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.r;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_G_5_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.g;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_B_6_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.b;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.a;
            surface.Alpha = float(1);
            surface.AlphaClipThreshold = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.uv0 = input.texCoord0;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }
        
        // Render State
        Cull Back
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHNORMALS
        #define _ALPHATEST_ON 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 tangentWS : INTERP0;
             float4 texCoord0 : INTERP1;
             float3 normalWS : INTERP2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _EmissiveIntensity;
        float4 _BaseMap_TexelSize;
        float4 _EmissiveMask_TexelSize;
        float4 _RoughnessMap_TexelSize;
        float4 _MetalicMAp_TexelSize;
        float4 _AlphaMap_TexelSize;
        float _LightingSmoothness;
        float _Steps;
        float _SpecularSteps;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissiveMask);
        SAMPLER(sampler_EmissiveMask);
        TEXTURE2D(_RoughnessMap);
        SAMPLER(sampler_RoughnessMap);
        TEXTURE2D(_MetalicMAp);
        SAMPLER(sampler_MetalicMAp);
        TEXTURE2D(_AlphaMap);
        SAMPLER(sampler_AlphaMap);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 NormalTS;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_AlphaMap);
            float4 _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.tex, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.samplerstate, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_R_4_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.r;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_G_5_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.g;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_B_6_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.b;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.a;
            surface.NormalTS = IN.TangentSpaceNormal;
            surface.Alpha = float(1);
            surface.AlphaClipThreshold = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.uv0 = input.texCoord0;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma shader_feature _ EDITOR_VISUALIZATION
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #define ATTRIBUTES_NEED_INSTANCEID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD1
        #define VARYINGS_NEED_TEXCOORD2
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_META
        #define _FOG_FRAGMENT 1
        #define _ALPHATEST_ON 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 texCoord0;
             float4 texCoord1;
             float4 texCoord2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpaceNormal;
             float3 WorldSpaceViewDirection;
             float3 WorldSpacePosition;
             float2 NDCPosition;
             float2 PixelPosition;
             float4 uv0;
             float4 uv1;
             float4 uv2;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0 : INTERP0;
             float4 texCoord1 : INTERP1;
             float4 texCoord2 : INTERP2;
             float3 positionWS : INTERP3;
             float3 normalWS : INTERP4;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            output.texCoord1.xyzw = input.texCoord1;
            output.texCoord2.xyzw = input.texCoord2;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            output.texCoord1 = input.texCoord1.xyzw;
            output.texCoord2 = input.texCoord2.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _EmissiveIntensity;
        float4 _BaseMap_TexelSize;
        float4 _EmissiveMask_TexelSize;
        float4 _RoughnessMap_TexelSize;
        float4 _MetalicMAp_TexelSize;
        float4 _AlphaMap_TexelSize;
        float _LightingSmoothness;
        float _Steps;
        float _SpecularSteps;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissiveMask);
        SAMPLER(sampler_EmissiveMask);
        TEXTURE2D(_RoughnessMap);
        SAMPLER(sampler_RoughnessMap);
        TEXTURE2D(_MetalicMAp);
        SAMPLER(sampler_MetalicMAp);
        TEXTURE2D(_AlphaMap);
        SAMPLER(sampler_AlphaMap);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void MainLightDirection_float(out float3 Direction)
        {
            #if SHADERGRAPH_PREVIEW
            Direction = half3(-0.5, -0.5, 0);
            #else
            Direction = SHADERGRAPH_MAIN_LIGHT_DIRECTION();
            #endif
        }
        
        void Unity_Negate_float3(float3 In, out float3 Out)
        {
            Out = -1 * In;
        }
        
        void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
        Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        struct Bindings_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float
        {
        float3 WorldSpaceNormal;
        };
        
        void SG_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float(float3 _NormalWS, bool _NormalWS_a616d67d9a9241cf9d4230dbfe62563f_IsConnected, Bindings_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float IN, out float Diffuse_1)
        {
        float3 _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3 = _NormalWS;
        bool _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3_IsConnected = _NormalWS_a616d67d9a9241cf9d4230dbfe62563f_IsConnected;
        float3 _BranchOnInputConnection_565230d8358148bfac6929645968ec6d_Out_3_Vector3 = _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3_IsConnected ? _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3 : IN.WorldSpaceNormal;
        float3 _MainLightDirection_46d8c4b675334560b12bb67c4e1ab4ca_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_46d8c4b675334560b12bb67c4e1ab4ca_Direction_0_Vector3);
        float3 _Negate_cefa58b1986e43aa934b4f687b6018fd_Out_1_Vector3;
        Unity_Negate_float3(_MainLightDirection_46d8c4b675334560b12bb67c4e1ab4ca_Direction_0_Vector3, _Negate_cefa58b1986e43aa934b4f687b6018fd_Out_1_Vector3);
        float _DotProduct_b0063238199f49058ba189bac4da970d_Out_2_Float;
        Unity_DotProduct_float3(_BranchOnInputConnection_565230d8358148bfac6929645968ec6d_Out_3_Vector3, _Negate_cefa58b1986e43aa934b4f687b6018fd_Out_1_Vector3, _DotProduct_b0063238199f49058ba189bac4da970d_Out_2_Float);
        float _Multiply_d11fc76f5fe1410e894c6ac565e2d937_Out_2_Float;
        Unity_Multiply_float_float(_DotProduct_b0063238199f49058ba189bac4da970d_Out_2_Float, 0.5, _Multiply_d11fc76f5fe1410e894c6ac565e2d937_Out_2_Float);
        float _Add_1a63a84f8dfc4d8ca825f5717c5a9d07_Out_2_Float;
        Unity_Add_float(_Multiply_d11fc76f5fe1410e894c6ac565e2d937_Out_2_Float, float(0.5), _Add_1a63a84f8dfc4d8ca825f5717c5a9d07_Out_2_Float);
        float _Saturate_95b5072011dc4a2c899250596471f236_Out_1_Float;
        Unity_Saturate_float(_Add_1a63a84f8dfc4d8ca825f5717c5a9d07_Out_2_Float, _Saturate_95b5072011dc4a2c899250596471f236_Out_1_Float);
        Diffuse_1 = _Saturate_95b5072011dc4a2c899250596471f236_Out_1_Float;
        }
        
        void Unity_Exponential2_float(float In, out float Out)
        {
            Out = exp2(In);
        }
        
        struct Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float
        {
        };
        
        void SG_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float(float _In, Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float IN, out float Out_1)
        {
        float _Property_ad239b68c11f4fcb920425661841c000_Out_0_Float = _In;
        float _Multiply_187758eae33846cb912e3d80a692d9b0_Out_2_Float;
        Unity_Multiply_float_float(_Property_ad239b68c11f4fcb920425661841c000_Out_0_Float, 10, _Multiply_187758eae33846cb912e3d80a692d9b0_Out_2_Float);
        float _Add_023529f132584452b7ef9ffb6f67eb4b_Out_2_Float;
        Unity_Add_float(_Multiply_187758eae33846cb912e3d80a692d9b0_Out_2_Float, float(1), _Add_023529f132584452b7ef9ffb6f67eb4b_Out_2_Float);
        float _Exponential_b2744bb952e44d0cb0d813283e357f8c_Out_1_Float;
        Unity_Exponential2_float(_Add_023529f132584452b7ef9ffb6f67eb4b_Out_2_Float, _Exponential_b2744bb952e44d0cb0d813283e357f8c_Out_1_Float);
        Out_1 = _Exponential_b2744bb952e44d0cb0d813283e357f8c_Out_1_Float;
        }
        
        void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
        {
            Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
        }
        
        void Unity_Lerp_float(float A, float B, float T, out float Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        struct Bindings_Reflectance_3dd134828a90d7446802ac63dbc0221d_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        };
        
        void SG_Reflectance_3dd134828a90d7446802ac63dbc0221d_float(float3 _Base_Color, bool _Base_Color_57260f26a3c14234b948dd74141df9c7_IsConnected, float3 _NormalWS, bool _NormalWS_ab3d33962fc64884948809ad8eaf7aa5_IsConnected, float _Metallic, Bindings_Reflectance_3dd134828a90d7446802ac63dbc0221d_float IN, out float3 Reflectance_1)
        {
        float3 _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3 = _NormalWS;
        bool _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3_IsConnected = _NormalWS_ab3d33962fc64884948809ad8eaf7aa5_IsConnected;
        float3 _BranchOnInputConnection_2b572462617842bf85ea171e93239787_Out_3_Vector3 = _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3_IsConnected ? _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3 : IN.WorldSpaceNormal;
        float _FresnelEffect_c130e1f911b140299ff5363626d5f22b_Out_3_Float;
        Unity_FresnelEffect_float(_BranchOnInputConnection_2b572462617842bf85ea171e93239787_Out_3_Vector3, IN.WorldSpaceViewDirection, float(3), _FresnelEffect_c130e1f911b140299ff5363626d5f22b_Out_3_Float);
        float _Lerp_3ed50cf0a0a94d98a7de54752c2f2581_Out_3_Float;
        Unity_Lerp_float(float(0.04), float(1), _FresnelEffect_c130e1f911b140299ff5363626d5f22b_Out_3_Float, _Lerp_3ed50cf0a0a94d98a7de54752c2f2581_Out_3_Float);
        float3 _Property_0d65a942fa4c458ebf1d5b61426a6680_Out_0_Vector3 = _Base_Color;
        bool _Property_0d65a942fa4c458ebf1d5b61426a6680_Out_0_Vector3_IsConnected = _Base_Color_57260f26a3c14234b948dd74141df9c7_IsConnected;
        float _Property_0899e6b3a7dd4ecb9b9de1f735daed5e_Out_0_Float = _Metallic;
        float3 _Lerp_e80ebb2ee2ec40c2a06b11a4bab428cc_Out_3_Vector3;
        Unity_Lerp_float3((_Lerp_3ed50cf0a0a94d98a7de54752c2f2581_Out_3_Float.xxx), _Property_0d65a942fa4c458ebf1d5b61426a6680_Out_0_Vector3, (_Property_0899e6b3a7dd4ecb9b9de1f735daed5e_Out_0_Float.xxx), _Lerp_e80ebb2ee2ec40c2a06b11a4bab428cc_Out_3_Vector3);
        Reflectance_1 = _Lerp_e80ebb2ee2ec40c2a06b11a4bab428cc_Out_3_Vector3;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Normalize_float3(float3 In, out float3 Out)
        {
            Out = normalize(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
        Out = A * B;
        }
        
        struct Bindings_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        };
        
        void SG_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float(float3 _NormalWS, bool _NormalWS_764643e0455a4ece932285a0241516e7_IsConnected, float _Smoothness, float3 _Reflectance, Bindings_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float IN, out float3 Specular_1)
        {
        float3 _MainLightDirection_436ef0cdefe04605b70adabe76102606_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_436ef0cdefe04605b70adabe76102606_Direction_0_Vector3);
        float3 _Negate_6a9ea0a4355445dcb9ea2b983008a197_Out_1_Vector3;
        Unity_Negate_float3(_MainLightDirection_436ef0cdefe04605b70adabe76102606_Direction_0_Vector3, _Negate_6a9ea0a4355445dcb9ea2b983008a197_Out_1_Vector3);
        float3 _Add_fec1db3bbef240b8b1b7d4f12ea2c97f_Out_2_Vector3;
        Unity_Add_float3(IN.WorldSpaceViewDirection, _Negate_6a9ea0a4355445dcb9ea2b983008a197_Out_1_Vector3, _Add_fec1db3bbef240b8b1b7d4f12ea2c97f_Out_2_Vector3);
        float3 _Normalize_5df6e3535e5842a988f33d859b57987a_Out_1_Vector3;
        Unity_Normalize_float3(_Add_fec1db3bbef240b8b1b7d4f12ea2c97f_Out_2_Vector3, _Normalize_5df6e3535e5842a988f33d859b57987a_Out_1_Vector3);
        float3 _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3 = _NormalWS;
        bool _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3_IsConnected = _NormalWS_764643e0455a4ece932285a0241516e7_IsConnected;
        float3 _BranchOnInputConnection_c0a0900e17284fa980ff63a7456c2199_Out_3_Vector3 = _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3_IsConnected ? _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3 : IN.WorldSpaceNormal;
        float _DotProduct_fdf0af8bd536429fa4b57dc806a67c04_Out_2_Float;
        Unity_DotProduct_float3(_Normalize_5df6e3535e5842a988f33d859b57987a_Out_1_Vector3, _BranchOnInputConnection_c0a0900e17284fa980ff63a7456c2199_Out_3_Vector3, _DotProduct_fdf0af8bd536429fa4b57dc806a67c04_Out_2_Float);
        float _Saturate_716ed2bdf36e4e60a35f74d186b278e9_Out_1_Float;
        Unity_Saturate_float(_DotProduct_fdf0af8bd536429fa4b57dc806a67c04_Out_2_Float, _Saturate_716ed2bdf36e4e60a35f74d186b278e9_Out_1_Float);
        float _Saturate_1d23f425974541b381f4fa80cf5a00b1_Out_1_Float;
        Unity_Saturate_float(_Saturate_716ed2bdf36e4e60a35f74d186b278e9_Out_1_Float, _Saturate_1d23f425974541b381f4fa80cf5a00b1_Out_1_Float);
        float _Property_1766606f1117460388cc77e7505598dd_Out_0_Float = _Smoothness;
        Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4;
        half _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4_Out_1_Float;
        SG_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float(_Property_1766606f1117460388cc77e7505598dd_Out_0_Float, _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4, _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4_Out_1_Float);
        float _Power_c507d979651749238e129118bb4c252f_Out_2_Float;
        Unity_Power_float(_Saturate_1d23f425974541b381f4fa80cf5a00b1_Out_1_Float, _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4_Out_1_Float, _Power_c507d979651749238e129118bb4c252f_Out_2_Float);
        float3 _Property_ab8c048e4ae44d56bf8dcb1bbc8ada89_Out_0_Vector3 = _Reflectance;
        float3 _Multiply_3c43a7f74b6c4c35a534990268ada20a_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Power_c507d979651749238e129118bb4c252f_Out_2_Float.xxx), _Property_ab8c048e4ae44d56bf8dcb1bbc8ada89_Out_0_Vector3, _Multiply_3c43a7f74b6c4c35a534990268ada20a_Out_2_Vector3);
        Specular_1 = _Multiply_3c43a7f74b6c4c35a534990268ada20a_Out_2_Vector3;
        }
        
        // unity-custom-func-begin
        void MainLightString_float(float3 worldPos, out float3 direction, out float3 color, out float shadowAtt){
        #ifdef SHADERGRAPH_PREVIEW
          direction = normalize(float3(-0.7,0.7,-0.7));
          color = float3(1,1,1);
          shadowAtt = 1;
        #else
          #if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)
            float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
            Light mainLight = GetMainLight(shadowCoord);
             direction = mainLight.direction;
        
             color = mainLight.color;
        
             shadowAtt = mainLight.shadowAttenuation;
          #else
            direction = normalize(float3(-0.7,0.7,-0.7));
            color = float3(1,1,1);
            shadowAtt = 1;
          #endif
        #endif
        }
        // unity-custom-func-end
        
        struct Bindings_MainLight_0157caa4ea90d88499c9fdb016582871_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_MainLight_0157caa4ea90d88499c9fdb016582871_float(Bindings_MainLight_0157caa4ea90d88499c9fdb016582871_float IN, out float3 Direction_1, out float3 Color_2, out float ShadowAtt_3)
        {
        float3 _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_direction_1_Vector3;
        float3 _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_color_2_Vector3;
        float _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_shadowAtt_3_Float;
        MainLightString_float(IN.WorldSpacePosition, _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_direction_1_Vector3, _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_color_2_Vector3, _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_shadowAtt_3_Float);
        Direction_1 = _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_direction_1_Vector3;
        Color_2 = _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_color_2_Vector3;
        ShadowAtt_3 = _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_shadowAtt_3_Float;
        }
        
        void Unity_ViewVectorWorld_float(out float3 Out, float3 WorldSpacePosition)
        {
            Out = _WorldSpaceCameraPos.xyz - GetAbsolutePositionWS(WorldSpacePosition);
            if(!IsPerspectiveProjection())
            {
                Out = GetViewForwardDir() * dot(Out, GetViewForwardDir());
            }
        }
        
        // unity-custom-func-begin
        void AddAdditionalLights_float(float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView, float MainDiffuse, float3 MainSpecular, float3 MainColor, out float Diffuse, out float3 Specular, out float3 Color){
        // Initialize outputs
        
        Diffuse = 0;
        
        Specular = 0;
        
        Color = 0;
        
        
        
        #ifndef SHADERGRAPH_PREVIEW
        
        
        
           // --- MAIN LIGHT ---
        
          Light mainLight = GetMainLight();
        
          mainLight.shadowAttenuation = MainLightRealtimeShadow(float4(WorldPosition, 1.0));
        
          float mainAtten = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
        
         
        
        
        
          float mainNdotL = saturate(dot(WorldNormal, mainLight.direction));
        
          float mainDiffuse = mainAtten * mainNdotL;
        
          float3 mainSpecular = LightingSpecular(mainDiffuse, mainLight.direction, WorldNormal, WorldView, 1, Smoothness);
        
        
        
          Diffuse += mainDiffuse;
        
          Specular += mainSpecular;
        
          Color += mainLight.color * (mainDiffuse + mainSpecular);
        
        
        
            // --- ADDITIONAL LIGHTS ---
        
            uint pixelLightCount = GetAdditionalLightsCount();
        
        
        
            LIGHT_LOOP_BEGIN(pixelLightCount)
        
                
        
                Light light = GetAdditionalPerObjectLight(lightIndex, WorldPosition);
        
        
        
                light.shadowAttenuation = AdditionalLightRealtimeShadow(lightIndex, WorldPosition, light.direction);
        
                float atten = light.distanceAttenuation * light.shadowAttenuation;
        
        
        
                float NdotL = saturate(dot(WorldNormal, light.direction) * 0.5 + 0.5);
        
                float thisDiffuse = atten * NdotL;
        
                float3 thisSpecular = LightingSpecular(thisDiffuse, light.direction, WorldNormal, WorldView, 1, Smoothness);
        
        
        
                Diffuse += thisDiffuse;
        
                Specular += thisSpecular;
        
                     #if defined(_LIGHT_COOKIES)
        
        	      float3 cookieColor = SampleAdditionalLightCookie(lightIndex, WorldPosition);
        
        	      light.color *= cookieColor;
        
                     #endif
        
                Color += light.color * (thisDiffuse + thisSpecular);
        
            LIGHT_LOOP_END
        
        
        
            // Normalize total lighting
        
            //Color = MainColor * Color;
        
        float totalWeight = Diffuse + dot(Specular, float3(0.333, 0.333, 0.333));
        
            Color = totalWeight > 0 ? MainColor * (Color / totalWeight) : MainColor;
        
        
        
        #endif
        }
        // unity-custom-func-end
        
        struct Bindings_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float(float _MainLightDiffuse, float3 _MainLightSpecular, float3 _MainLightColor, float3 _NormalWS, float _Smoothness, Bindings_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float IN, out float Diffuse_1, out float3 Specular_2, out float3 Color_3)
        {
        float _Property_f5ac72f32dc84ae988391b7d51172380_Out_0_Float = _Smoothness;
        float3 _Property_85d85c5164d5483787fecbcecfd6a519_Out_0_Vector3 = _NormalWS;
        float3 _ViewVector_0d331f2b495741bcb8795caa9549f016_Out_0_Vector3;
        Unity_ViewVectorWorld_float(_ViewVector_0d331f2b495741bcb8795caa9549f016_Out_0_Vector3, IN.WorldSpacePosition);
        float _Property_51c13cc19f414dacb86745cec42d9ab6_Out_0_Float = _MainLightDiffuse;
        float3 _Property_f1e96792c3cc4a1e90396147ef9f7160_Out_0_Vector3 = _MainLightColor;
        float _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Diffuse_7_Float;
        float3 _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Specular_8_Vector3;
        float3 _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Color_9_Vector3;
        AddAdditionalLights_float(_Property_f5ac72f32dc84ae988391b7d51172380_Out_0_Float, IN.WorldSpacePosition, _Property_85d85c5164d5483787fecbcecfd6a519_Out_0_Vector3, _ViewVector_0d331f2b495741bcb8795caa9549f016_Out_0_Vector3, _Property_51c13cc19f414dacb86745cec42d9ab6_Out_0_Float, _Property_f1e96792c3cc4a1e90396147ef9f7160_Out_0_Vector3, _Property_f1e96792c3cc4a1e90396147ef9f7160_Out_0_Vector3, _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Diffuse_7_Float, _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Specular_8_Vector3, _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Color_9_Vector3);
        Diffuse_1 = _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Diffuse_7_Float;
        Specular_2 = _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Specular_8_Vector3;
        Color_3 = _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Color_9_Vector3;
        }
        
        void Unity_Posterize_float3(float3 In, float3 Steps, out float3 Out)
        {
            Out = floor(In / (1 / Steps)) * (1 / Steps);
        }
        
        void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
        {
            float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
            Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
        }
        
        // unity-custom-func-begin
        void GetSSAO_float(float2 ScreenPos, out float DirectAO, out float IndirectAO){
         #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT) && !defined(SHADERGRAPH_PREVIEW)
        
         
          float ssao = saturate(SampleAmbientOcclusion(ScreenPos) + (1.0 - _AmbientOcclusionParam.x));
        
           IndirectAO = ssao;
        
           DirectAO = lerp(1.0, ssao, _AmbientOcclusionParam.w);
        
        #else
        
           DirectAO = 1.0;
        
           IndirectAO = 1.0;
        
        #endif
        }
        // unity-custom-func-end
        
        struct Bindings_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float
        {
        float2 NDCPosition;
        };
        
        void SG_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float(float2 _ScreenPos, bool _ScreenPos_72149ac21c784cc8b4614750ec683413_IsConnected, Bindings_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float IN, out float DirectAO_1, out float IndirectAO_2)
        {
        float2 _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2 = _ScreenPos;
        bool _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2_IsConnected = _ScreenPos_72149ac21c784cc8b4614750ec683413_IsConnected;
        float4 _ScreenPosition_a193bed040594170a08ae04fed7ea8a7_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
        float2 _BranchOnInputConnection_8e8119e37af64e9a957c820002cea48c_Out_3_Vector2 = _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2_IsConnected ? _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2 : (_ScreenPosition_a193bed040594170a08ae04fed7ea8a7_Out_0_Vector4.xy);
        float _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_DirectAO_1_Float;
        float _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_IndirectAO_2_Float;
        GetSSAO_float(_BranchOnInputConnection_8e8119e37af64e9a957c820002cea48c_Out_3_Vector2, _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_DirectAO_1_Float, _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_IndirectAO_2_Float);
        DirectAO_1 = _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_DirectAO_1_Float;
        IndirectAO_2 = _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_IndirectAO_2_Float;
        }
        
        void Unity_Minimum_float(float A, float B, out float Out)
        {
            Out = min(A, B);
        };
        
        void Unity_ReflectionProbe_float(float3 ViewDir, float3 Normal, float LOD, out float3 Out)
        {
            Out = SHADERGRAPH_REFLECTION_PROBE(ViewDir, Normal, LOD);
        }
        
        struct Bindings_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        float3 WorldSpacePosition;
        float2 NDCPosition;
        float2 PixelPosition;
        half4 uv1;
        half4 uv2;
        };
        
        void SG_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float(float3 _Base_Color, bool _Base_Color_87794c9dccf9430d8ee1585f52796223_IsConnected, float3 _NormalWS, bool _NormalWS_d8d26d45dd0b4d4a8e8426edcd206b0f_IsConnected, float _AO, float _Smoothness, float _Metallic, float _Reflectance, Bindings_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float IN, out float3 Ambient_1, out float3 Metallic_2, out float DdirectAO_3)
        {
        float3 _Property_50e51e65f64b40c29fa7b10305f0a7b4_Out_0_Vector3 = _Base_Color;
        bool _Property_50e51e65f64b40c29fa7b10305f0a7b4_Out_0_Vector3_IsConnected = _Base_Color_87794c9dccf9430d8ee1585f52796223_IsConnected;
        float _Property_b967758667b74ff5acb2a8af3ff58c39_Out_0_Float = _Metallic;
        float3 _Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3;
        Unity_Lerp_float3(_Property_50e51e65f64b40c29fa7b10305f0a7b4_Out_0_Vector3, float3(0, 0, 0), (_Property_b967758667b74ff5acb2a8af3ff58c39_Out_0_Float.xxx), _Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3);
        float3 _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3 = _NormalWS;
        bool _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3_IsConnected = _NormalWS_d8d26d45dd0b4d4a8e8426edcd206b0f_IsConnected;
        float3 _BranchOnInputConnection_dae192f178d24a5a9c9c540fd5488461_Out_3_Vector3 = _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3_IsConnected ? _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3 : IN.WorldSpaceNormal;
        float3 _BakedGI_1e2ac029f59d497abdc2a7bcf1eb3346_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, _BranchOnInputConnection_dae192f178d24a5a9c9c540fd5488461_Out_3_Vector3, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
        float _Property_26a27af7a1144ea0a77aad0b49f12697_Out_0_Float = _AO;
        Bindings_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float _SSAO_f6107fe543294e7f932b8c5cfc86bdd5;
        _SSAO_f6107fe543294e7f932b8c5cfc86bdd5.NDCPosition = IN.NDCPosition;
        half _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_DirectAO_1_Float;
        half _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_IndirectAO_2_Float;
        SG_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float(half2 (0, 0), false, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_DirectAO_1_Float, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_IndirectAO_2_Float);
        float _Minimum_3ac81a32823a439c8b51e89edf949c26_Out_2_Float;
        Unity_Minimum_float(_Property_26a27af7a1144ea0a77aad0b49f12697_Out_0_Float, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_IndirectAO_2_Float, _Minimum_3ac81a32823a439c8b51e89edf949c26_Out_2_Float);
        float3 _Multiply_423df8bc24d94add8f44f6db0bf46798_Out_2_Vector3;
        Unity_Multiply_float3_float3(_BakedGI_1e2ac029f59d497abdc2a7bcf1eb3346_Out_1_Vector3, (_Minimum_3ac81a32823a439c8b51e89edf949c26_Out_2_Float.xxx), _Multiply_423df8bc24d94add8f44f6db0bf46798_Out_2_Vector3);
        float3 _Multiply_4ee1636476de4393a1b2e9162c2328b9_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3, _Multiply_423df8bc24d94add8f44f6db0bf46798_Out_2_Vector3, _Multiply_4ee1636476de4393a1b2e9162c2328b9_Out_2_Vector3);
        float _Property_76583d84384b44cbb79843c917044c1b_Out_0_Float = _Reflectance;
        float _Property_f511aceb4377409690a68e8d8a16a032_Out_0_Float = _Smoothness;
        float _Lerp_496af92f7e424d5586ca49ffe949aff6_Out_3_Float;
        Unity_Lerp_float(float(8), float(0), _Property_f511aceb4377409690a68e8d8a16a032_Out_0_Float, _Lerp_496af92f7e424d5586ca49ffe949aff6_Out_3_Float);
        float3 _ReflectionProbe_d9eb69c9b54641068d948dd37fec342f_Out_3_Vector3;
        Unity_ReflectionProbe_float(IN.WorldSpaceViewDirection, _BranchOnInputConnection_dae192f178d24a5a9c9c540fd5488461_Out_3_Vector3, _Lerp_496af92f7e424d5586ca49ffe949aff6_Out_3_Float, _ReflectionProbe_d9eb69c9b54641068d948dd37fec342f_Out_3_Vector3);
        float3 _Multiply_25ab7f2171954fc4a710fe8c8c2cc402_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Property_76583d84384b44cbb79843c917044c1b_Out_0_Float.xxx), _ReflectionProbe_d9eb69c9b54641068d948dd37fec342f_Out_3_Vector3, _Multiply_25ab7f2171954fc4a710fe8c8c2cc402_Out_2_Vector3);
        float3 _Add_475a3a92ac834f51b869dedfbba66e44_Out_2_Vector3;
        Unity_Add_float3(_Multiply_4ee1636476de4393a1b2e9162c2328b9_Out_2_Vector3, _Multiply_25ab7f2171954fc4a710fe8c8c2cc402_Out_2_Vector3, _Add_475a3a92ac834f51b869dedfbba66e44_Out_2_Vector3);
        Ambient_1 = _Add_475a3a92ac834f51b869dedfbba66e44_Out_2_Vector3;
        Metallic_2 = _Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3;
        DdirectAO_3 = _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_DirectAO_1_Float;
        }
        
        void Unity_Saturate_float3(float3 In, out float3 Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Posterize_float(float In, float Steps, out float Out)
        {
            Out = floor(In / (1 / Steps)) * (1 / Steps);
        }
        
        struct Bindings_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        float3 WorldSpacePosition;
        float2 NDCPosition;
        float2 PixelPosition;
        half4 uv1;
        half4 uv2;
        };
        
        void SG_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float(float3 _Base_Color, bool _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected, float3 _NormalWS, bool _NormalWS_d2c1afa83f4f4d20ac34f445f069483d_IsConnected, float _Metallic, float _Smoothness, float _AO, float _Steps, float _Specular_Steps, float _Smoothness_Strength, Bindings_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float IN, out float3 Lit_1)
        {
        float3 _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3 = _NormalWS;
        bool _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3_IsConnected = _NormalWS_d2c1afa83f4f4d20ac34f445f069483d_IsConnected;
        float3 _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3 = _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3_IsConnected ? _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3 : IN.WorldSpaceNormal;
        Bindings_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91;
        _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91.WorldSpaceNormal = IN.WorldSpaceNormal;
        half _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float;
        SG_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float(_BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91, _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float);
        float _Property_af2c0fb6df7d4dc9abade557540f3ed7_Out_0_Float = _Smoothness;
        Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e;
        half _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float;
        SG_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float(_Property_af2c0fb6df7d4dc9abade557540f3ed7_Out_0_Float, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float);
        float3 _Property_b3987ddafd854a0a85863bc89dcde720_Out_0_Vector3 = _Base_Color;
        bool _Property_b3987ddafd854a0a85863bc89dcde720_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float _Property_3c53a42fedba46ba9fae794424fbe85e_Out_0_Float = _Metallic;
        Bindings_Reflectance_3dd134828a90d7446802ac63dbc0221d_float _Reflectance_0d5aeeef450744cf885ab04f87797c7b;
        _Reflectance_0d5aeeef450744cf885ab04f87797c7b.WorldSpaceNormal = IN.WorldSpaceNormal;
        _Reflectance_0d5aeeef450744cf885ab04f87797c7b.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3;
        SG_Reflectance_3dd134828a90d7446802ac63dbc0221d_float(_Property_b3987ddafd854a0a85863bc89dcde720_Out_0_Vector3, true, _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _Property_3c53a42fedba46ba9fae794424fbe85e_Out_0_Float, _Reflectance_0d5aeeef450744cf885ab04f87797c7b, _Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3);
        Bindings_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8;
        _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8.WorldSpaceNormal = IN.WorldSpaceNormal;
        _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8_Specular_1_Vector3;
        SG_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float(_BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float, _Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3, _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8, _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8_Specular_1_Vector3);
        float3 _Multiply_577b66af727b4e43be242c88de8def99_Out_2_Vector3;
        Unity_Multiply_float3_float3((_HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float.xxx), _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8_Specular_1_Vector3, _Multiply_577b66af727b4e43be242c88de8def99_Out_2_Vector3);
        Bindings_MainLight_0157caa4ea90d88499c9fdb016582871_float _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a;
        _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a.WorldSpacePosition = IN.WorldSpacePosition;
        float3 _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Direction_1_Vector3;
        float3 _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Color_2_Vector3;
        float _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_ShadowAtt_3_Float;
        SG_MainLight_0157caa4ea90d88499c9fdb016582871_float(_MainLight_1fffd9ee56744cde8cd3210b8fc0a55a, _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Direction_1_Vector3, _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Color_2_Vector3, _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_ShadowAtt_3_Float);
        float3 _Multiply_e3a50f2fbf214e64931956c0b1e7f194_Out_2_Vector3;
        Unity_Multiply_float3_float3(_MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Color_2_Vector3, (_MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_ShadowAtt_3_Float.xxx), _Multiply_e3a50f2fbf214e64931956c0b1e7f194_Out_2_Vector3);
        Bindings_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021;
        _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021.WorldSpacePosition = IN.WorldSpacePosition;
        float _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Diffuse_1_Float;
        float3 _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Specular_2_Vector3;
        float3 _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3;
        SG_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float(_HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float, _Multiply_577b66af727b4e43be242c88de8def99_Out_2_Vector3, _Multiply_e3a50f2fbf214e64931956c0b1e7f194_Out_2_Vector3, _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Diffuse_1_Float, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Specular_2_Vector3, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3);
        float _Property_7fabb829117947b8956799cbdf3be9e0_Out_0_Float = _Steps;
        float3 _Posterize_8f26285a65784a038d6d9d2087181b46_Out_2_Vector3;
        Unity_Posterize_float3(_AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, (_Property_7fabb829117947b8956799cbdf3be9e0_Out_0_Float.xxx), _Posterize_8f26285a65784a038d6d9d2087181b46_Out_2_Vector3);
        float3 _Property_3668f71779994b3a9fcc7095f880cb12_Out_0_Vector3 = _Base_Color;
        bool _Property_3668f71779994b3a9fcc7095f880cb12_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float3 _Saturation_e12bfe3d861c4f1fa4c1297d0dd08cc0_Out_2_Vector3;
        Unity_Saturation_float(_Property_3668f71779994b3a9fcc7095f880cb12_Out_0_Vector3, float(1.6), _Saturation_e12bfe3d861c4f1fa4c1297d0dd08cc0_Out_2_Vector3);
        float3 _Multiply_77c53f8596dd487fa3009d208e99c0fd_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Saturation_e12bfe3d861c4f1fa4c1297d0dd08cc0_Out_2_Vector3, float3(0.1, 0.1, 0.1), _Multiply_77c53f8596dd487fa3009d208e99c0fd_Out_2_Vector3);
        float3 _Property_494170212d604ba0a52a5b39f34e4c3f_Out_0_Vector3 = _Base_Color;
        bool _Property_494170212d604ba0a52a5b39f34e4c3f_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float3 _Multiply_e22a63bb0da045618a0202e45051407e_Out_2_Vector3;
        Unity_Multiply_float3_float3((_AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Diffuse_1_Float.xxx), _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, _Multiply_e22a63bb0da045618a0202e45051407e_Out_2_Vector3);
        float3 _Property_53d2f61f22684efca5bbd024184799a4_Out_0_Vector3 = _Base_Color;
        bool _Property_53d2f61f22684efca5bbd024184799a4_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float _Property_5204695d7d6d4d03b28eba481d372192_Out_0_Float = _AO;
        float _Property_aa169c45708b4343b76a3b52b483b8e2_Out_0_Float = _Smoothness;
        float _Property_1d116579c59d45ccaa1bdcb07a5ec5d5_Out_0_Float = _Metallic;
        Bindings_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.WorldSpaceNormal = IN.WorldSpaceNormal;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.WorldSpacePosition = IN.WorldSpacePosition;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.NDCPosition = IN.NDCPosition;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.PixelPosition = IN.PixelPosition;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.uv1 = IN.uv1;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.uv2 = IN.uv2;
        half3 _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Ambient_1_Vector3;
        half3 _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Metallic_2_Vector3;
        half _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_DdirectAO_3_Float;
        SG_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float(_Property_53d2f61f22684efca5bbd024184799a4_Out_0_Vector3, true, _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _Property_5204695d7d6d4d03b28eba481d372192_Out_0_Float, _Property_aa169c45708b4343b76a3b52b483b8e2_Out_0_Float, _Property_1d116579c59d45ccaa1bdcb07a5ec5d5_Out_0_Float, (_Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3).x, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Ambient_1_Vector3, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Metallic_2_Vector3, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_DdirectAO_3_Float);
        float3 _Add_96c33650335248caa7429346ca897bb7_Out_2_Vector3;
        Unity_Add_float3(_Multiply_e22a63bb0da045618a0202e45051407e_Out_2_Vector3, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Ambient_1_Vector3, _Add_96c33650335248caa7429346ca897bb7_Out_2_Vector3);
        float3 _Saturate_b993ba602dea4759baa643682158093c_Out_1_Vector3;
        Unity_Saturate_float3(_Add_96c33650335248caa7429346ca897bb7_Out_2_Vector3, _Saturate_b993ba602dea4759baa643682158093c_Out_1_Vector3);
        float3 _Saturation_559bfc31eca847d3acd46b2fd284dc13_Out_2_Vector3;
        Unity_Saturation_float(_Saturate_b993ba602dea4759baa643682158093c_Out_1_Vector3, float(1), _Saturation_559bfc31eca847d3acd46b2fd284dc13_Out_2_Vector3);
        float _Swizzle_3952ca1d9d274331b6a4a49630c606ae_Out_1_Float = _Saturation_559bfc31eca847d3acd46b2fd284dc13_Out_2_Vector3.x;
        float _Posterize_268e85bcc1d5424485e214740b6468e3_Out_2_Float;
        Unity_Posterize_float(_Swizzle_3952ca1d9d274331b6a4a49630c606ae_Out_1_Float, _Property_7fabb829117947b8956799cbdf3be9e0_Out_0_Float, _Posterize_268e85bcc1d5424485e214740b6468e3_Out_2_Float);
        float3 _Lerp_8c017a3a82434ee38c4432bf457e582b_Out_3_Vector3;
        Unity_Lerp_float3(_Multiply_77c53f8596dd487fa3009d208e99c0fd_Out_2_Vector3, _Property_494170212d604ba0a52a5b39f34e4c3f_Out_0_Vector3, (_Posterize_268e85bcc1d5424485e214740b6468e3_Out_2_Float.xxx), _Lerp_8c017a3a82434ee38c4432bf457e582b_Out_3_Vector3);
        float3 _Multiply_5c248bf4893d4acd9b10421fae8923d1_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Posterize_8f26285a65784a038d6d9d2087181b46_Out_2_Vector3, _Lerp_8c017a3a82434ee38c4432bf457e582b_Out_3_Vector3, _Multiply_5c248bf4893d4acd9b10421fae8923d1_Out_2_Vector3);
        float _Property_48b9a97103af41f3b6cafeceb8a6a0b7_Out_0_Float = _Smoothness_Strength;
        float3 _Add_57faecf3e31346a68db61c3e666b2975_Out_2_Vector3;
        Unity_Add_float3((_Property_48b9a97103af41f3b6cafeceb8a6a0b7_Out_0_Float.xxx), _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, _Add_57faecf3e31346a68db61c3e666b2975_Out_2_Vector3);
        float3 _Multiply_b90f73fc105c4da3b7586428b54d0c03_Out_2_Vector3;
        Unity_Multiply_float3_float3(_AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Specular_2_Vector3, _Add_57faecf3e31346a68db61c3e666b2975_Out_2_Vector3, _Multiply_b90f73fc105c4da3b7586428b54d0c03_Out_2_Vector3);
        float3 _Saturation_c81f01b7b0de4608a25fde9890c55880_Out_2_Vector3;
        Unity_Saturation_float(_Multiply_b90f73fc105c4da3b7586428b54d0c03_Out_2_Vector3, float(0), _Saturation_c81f01b7b0de4608a25fde9890c55880_Out_2_Vector3);
        float _Swizzle_fbfd922c5f7d42d399186d59a86d5660_Out_1_Float = _Saturation_c81f01b7b0de4608a25fde9890c55880_Out_2_Vector3.x;
        float _Property_62c311d0a3534ad1ad3a7b66c978a36c_Out_0_Float = _Specular_Steps;
        float _Posterize_1027f91210d94108ae1c593d0fb61d8b_Out_2_Float;
        Unity_Posterize_float(_Swizzle_fbfd922c5f7d42d399186d59a86d5660_Out_1_Float, _Property_62c311d0a3534ad1ad3a7b66c978a36c_Out_0_Float, _Posterize_1027f91210d94108ae1c593d0fb61d8b_Out_2_Float);
        float3 _Multiply_c92faf2f8cc04ff29ec2ba172ebefa59_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Posterize_1027f91210d94108ae1c593d0fb61d8b_Out_2_Float.xxx), _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, _Multiply_c92faf2f8cc04ff29ec2ba172ebefa59_Out_2_Vector3);
        float3 _Add_321e04b0f3bb47558352363c94828b10_Out_2_Vector3;
        Unity_Add_float3(_Multiply_5c248bf4893d4acd9b10421fae8923d1_Out_2_Vector3, _Multiply_c92faf2f8cc04ff29ec2ba172ebefa59_Out_2_Vector3, _Add_321e04b0f3bb47558352363c94828b10_Out_2_Vector3);
        Lit_1 = _Add_321e04b0f3bb47558352363c94828b10_Out_2_Vector3;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_BaseMap);
            float4 _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D.tex, _Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D.samplerstate, _Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_R_4_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.r;
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_G_5_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.g;
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_B_6_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.b;
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_A_7_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.a;
            float _Property_7ab914554b2b477db515a77874d1d4cd_Out_0_Float = _LightingSmoothness;
            float _Property_9e7bc9fe055c4e4db532299787c0e3ed_Out_0_Float = _Steps;
            float _Property_9d34386f48e54d9eaeda9d04da1ad72f_Out_0_Float = _SpecularSteps;
            Bindings_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.WorldSpaceNormal = IN.WorldSpaceNormal;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.WorldSpacePosition = IN.WorldSpacePosition;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.NDCPosition = IN.NDCPosition;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.PixelPosition = IN.PixelPosition;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.uv1 = IN.uv1;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.uv2 = IN.uv2;
            float3 _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3_Lit_1_Vector3;
            SG_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float((_SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.xyz), true, float3 (0, 0, 0), false, float(0), _Property_7ab914554b2b477db515a77874d1d4cd_Out_0_Float, float(0), _Property_9e7bc9fe055c4e4db532299787c0e3ed_Out_0_Float, _Property_9d34386f48e54d9eaeda9d04da1ad72f_Out_0_Float, float(0), _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3, _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3_Lit_1_Vector3);
            UnityTexture2D _Property_242f17afd4494f1eae45e57743744c8c_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_EmissiveMask);
            float4 _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_242f17afd4494f1eae45e57743744c8c_Out_0_Texture2D.tex, _Property_242f17afd4494f1eae45e57743744c8c_Out_0_Texture2D.samplerstate, _Property_242f17afd4494f1eae45e57743744c8c_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_R_4_Float = _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_RGBA_0_Vector4.r;
            float _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_G_5_Float = _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_RGBA_0_Vector4.g;
            float _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_B_6_Float = _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_RGBA_0_Vector4.b;
            float _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_A_7_Float = _SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_RGBA_0_Vector4.a;
            float4 _Multiply_ea903ee047b5477c855ed2b94b6227f2_Out_2_Vector4;
            Unity_Multiply_float4_float4(_SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4, (_SampleTexture2D_73ddc16769bb40fab81dc11b7b055b64_R_4_Float.xxxx), _Multiply_ea903ee047b5477c855ed2b94b6227f2_Out_2_Vector4);
            float _Property_21326e76ba4e4c4c99157b1ec259c331_Out_0_Float = _EmissiveIntensity;
            float4 _Multiply_3aa49a8e912d45d09c831299a6a9a550_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Multiply_ea903ee047b5477c855ed2b94b6227f2_Out_2_Vector4, (_Property_21326e76ba4e4c4c99157b1ec259c331_Out_0_Float.xxxx), _Multiply_3aa49a8e912d45d09c831299a6a9a550_Out_2_Vector4);
            UnityTexture2D _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_AlphaMap);
            float4 _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.tex, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.samplerstate, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_R_4_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.r;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_G_5_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.g;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_B_6_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.b;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.a;
            surface.BaseColor = _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3_Lit_1_Vector3;
            surface.Emission = (_Multiply_3aa49a8e912d45d09c831299a6a9a550_Out_2_Vector4.xyz);
            surface.Alpha = float(1);
            surface.AlphaClipThreshold = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
            // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            float3 unnormalizedNormalWS = input.normalWS;
            const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        
        
            output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
        
        
            output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
            output.WorldSpacePosition = input.positionWS;
        
            #if UNITY_UV_STARTS_AT_TOP
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #else
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #endif
        
            output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
            output.NDCPosition.y = 1.0f - output.NDCPosition.y;
        
            output.uv0 = input.texCoord0;
            output.uv1 = input.texCoord1;
            output.uv2 = input.texCoord2;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "SceneSelectionPass"
            Tags
            {
                "LightMode" = "SceneSelectionPass"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENESELECTIONPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        #define _ALPHATEST_ON 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _EmissiveIntensity;
        float4 _BaseMap_TexelSize;
        float4 _EmissiveMask_TexelSize;
        float4 _RoughnessMap_TexelSize;
        float4 _MetalicMAp_TexelSize;
        float4 _AlphaMap_TexelSize;
        float _LightingSmoothness;
        float _Steps;
        float _SpecularSteps;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissiveMask);
        SAMPLER(sampler_EmissiveMask);
        TEXTURE2D(_RoughnessMap);
        SAMPLER(sampler_RoughnessMap);
        TEXTURE2D(_MetalicMAp);
        SAMPLER(sampler_MetalicMAp);
        TEXTURE2D(_AlphaMap);
        SAMPLER(sampler_AlphaMap);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_AlphaMap);
            float4 _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.tex, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.samplerstate, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_R_4_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.r;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_G_5_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.g;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_B_6_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.b;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.a;
            surface.Alpha = float(1);
            surface.AlphaClipThreshold = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.uv0 = input.texCoord0;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ScenePickingPass"
            Tags
            {
                "LightMode" = "Picking"
            }
        
        // Render State
        Cull Back
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD1
        #define VARYINGS_NEED_TEXCOORD2
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENEPICKINGPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        #define _ALPHATEST_ON 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 texCoord0;
             float4 texCoord1;
             float4 texCoord2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpaceNormal;
             float3 WorldSpaceViewDirection;
             float3 WorldSpacePosition;
             float2 NDCPosition;
             float2 PixelPosition;
             float4 uv0;
             float4 uv1;
             float4 uv2;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0 : INTERP0;
             float4 texCoord1 : INTERP1;
             float4 texCoord2 : INTERP2;
             float3 positionWS : INTERP3;
             float3 normalWS : INTERP4;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            output.texCoord1.xyzw = input.texCoord1;
            output.texCoord2.xyzw = input.texCoord2;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            output.texCoord1 = input.texCoord1.xyzw;
            output.texCoord2 = input.texCoord2.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _EmissiveIntensity;
        float4 _BaseMap_TexelSize;
        float4 _EmissiveMask_TexelSize;
        float4 _RoughnessMap_TexelSize;
        float4 _MetalicMAp_TexelSize;
        float4 _AlphaMap_TexelSize;
        float _LightingSmoothness;
        float _Steps;
        float _SpecularSteps;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissiveMask);
        SAMPLER(sampler_EmissiveMask);
        TEXTURE2D(_RoughnessMap);
        SAMPLER(sampler_RoughnessMap);
        TEXTURE2D(_MetalicMAp);
        SAMPLER(sampler_MetalicMAp);
        TEXTURE2D(_AlphaMap);
        SAMPLER(sampler_AlphaMap);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void MainLightDirection_float(out float3 Direction)
        {
            #if SHADERGRAPH_PREVIEW
            Direction = half3(-0.5, -0.5, 0);
            #else
            Direction = SHADERGRAPH_MAIN_LIGHT_DIRECTION();
            #endif
        }
        
        void Unity_Negate_float3(float3 In, out float3 Out)
        {
            Out = -1 * In;
        }
        
        void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
        Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        struct Bindings_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float
        {
        float3 WorldSpaceNormal;
        };
        
        void SG_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float(float3 _NormalWS, bool _NormalWS_a616d67d9a9241cf9d4230dbfe62563f_IsConnected, Bindings_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float IN, out float Diffuse_1)
        {
        float3 _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3 = _NormalWS;
        bool _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3_IsConnected = _NormalWS_a616d67d9a9241cf9d4230dbfe62563f_IsConnected;
        float3 _BranchOnInputConnection_565230d8358148bfac6929645968ec6d_Out_3_Vector3 = _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3_IsConnected ? _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3 : IN.WorldSpaceNormal;
        float3 _MainLightDirection_46d8c4b675334560b12bb67c4e1ab4ca_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_46d8c4b675334560b12bb67c4e1ab4ca_Direction_0_Vector3);
        float3 _Negate_cefa58b1986e43aa934b4f687b6018fd_Out_1_Vector3;
        Unity_Negate_float3(_MainLightDirection_46d8c4b675334560b12bb67c4e1ab4ca_Direction_0_Vector3, _Negate_cefa58b1986e43aa934b4f687b6018fd_Out_1_Vector3);
        float _DotProduct_b0063238199f49058ba189bac4da970d_Out_2_Float;
        Unity_DotProduct_float3(_BranchOnInputConnection_565230d8358148bfac6929645968ec6d_Out_3_Vector3, _Negate_cefa58b1986e43aa934b4f687b6018fd_Out_1_Vector3, _DotProduct_b0063238199f49058ba189bac4da970d_Out_2_Float);
        float _Multiply_d11fc76f5fe1410e894c6ac565e2d937_Out_2_Float;
        Unity_Multiply_float_float(_DotProduct_b0063238199f49058ba189bac4da970d_Out_2_Float, 0.5, _Multiply_d11fc76f5fe1410e894c6ac565e2d937_Out_2_Float);
        float _Add_1a63a84f8dfc4d8ca825f5717c5a9d07_Out_2_Float;
        Unity_Add_float(_Multiply_d11fc76f5fe1410e894c6ac565e2d937_Out_2_Float, float(0.5), _Add_1a63a84f8dfc4d8ca825f5717c5a9d07_Out_2_Float);
        float _Saturate_95b5072011dc4a2c899250596471f236_Out_1_Float;
        Unity_Saturate_float(_Add_1a63a84f8dfc4d8ca825f5717c5a9d07_Out_2_Float, _Saturate_95b5072011dc4a2c899250596471f236_Out_1_Float);
        Diffuse_1 = _Saturate_95b5072011dc4a2c899250596471f236_Out_1_Float;
        }
        
        void Unity_Exponential2_float(float In, out float Out)
        {
            Out = exp2(In);
        }
        
        struct Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float
        {
        };
        
        void SG_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float(float _In, Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float IN, out float Out_1)
        {
        float _Property_ad239b68c11f4fcb920425661841c000_Out_0_Float = _In;
        float _Multiply_187758eae33846cb912e3d80a692d9b0_Out_2_Float;
        Unity_Multiply_float_float(_Property_ad239b68c11f4fcb920425661841c000_Out_0_Float, 10, _Multiply_187758eae33846cb912e3d80a692d9b0_Out_2_Float);
        float _Add_023529f132584452b7ef9ffb6f67eb4b_Out_2_Float;
        Unity_Add_float(_Multiply_187758eae33846cb912e3d80a692d9b0_Out_2_Float, float(1), _Add_023529f132584452b7ef9ffb6f67eb4b_Out_2_Float);
        float _Exponential_b2744bb952e44d0cb0d813283e357f8c_Out_1_Float;
        Unity_Exponential2_float(_Add_023529f132584452b7ef9ffb6f67eb4b_Out_2_Float, _Exponential_b2744bb952e44d0cb0d813283e357f8c_Out_1_Float);
        Out_1 = _Exponential_b2744bb952e44d0cb0d813283e357f8c_Out_1_Float;
        }
        
        void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
        {
            Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
        }
        
        void Unity_Lerp_float(float A, float B, float T, out float Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        struct Bindings_Reflectance_3dd134828a90d7446802ac63dbc0221d_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        };
        
        void SG_Reflectance_3dd134828a90d7446802ac63dbc0221d_float(float3 _Base_Color, bool _Base_Color_57260f26a3c14234b948dd74141df9c7_IsConnected, float3 _NormalWS, bool _NormalWS_ab3d33962fc64884948809ad8eaf7aa5_IsConnected, float _Metallic, Bindings_Reflectance_3dd134828a90d7446802ac63dbc0221d_float IN, out float3 Reflectance_1)
        {
        float3 _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3 = _NormalWS;
        bool _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3_IsConnected = _NormalWS_ab3d33962fc64884948809ad8eaf7aa5_IsConnected;
        float3 _BranchOnInputConnection_2b572462617842bf85ea171e93239787_Out_3_Vector3 = _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3_IsConnected ? _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3 : IN.WorldSpaceNormal;
        float _FresnelEffect_c130e1f911b140299ff5363626d5f22b_Out_3_Float;
        Unity_FresnelEffect_float(_BranchOnInputConnection_2b572462617842bf85ea171e93239787_Out_3_Vector3, IN.WorldSpaceViewDirection, float(3), _FresnelEffect_c130e1f911b140299ff5363626d5f22b_Out_3_Float);
        float _Lerp_3ed50cf0a0a94d98a7de54752c2f2581_Out_3_Float;
        Unity_Lerp_float(float(0.04), float(1), _FresnelEffect_c130e1f911b140299ff5363626d5f22b_Out_3_Float, _Lerp_3ed50cf0a0a94d98a7de54752c2f2581_Out_3_Float);
        float3 _Property_0d65a942fa4c458ebf1d5b61426a6680_Out_0_Vector3 = _Base_Color;
        bool _Property_0d65a942fa4c458ebf1d5b61426a6680_Out_0_Vector3_IsConnected = _Base_Color_57260f26a3c14234b948dd74141df9c7_IsConnected;
        float _Property_0899e6b3a7dd4ecb9b9de1f735daed5e_Out_0_Float = _Metallic;
        float3 _Lerp_e80ebb2ee2ec40c2a06b11a4bab428cc_Out_3_Vector3;
        Unity_Lerp_float3((_Lerp_3ed50cf0a0a94d98a7de54752c2f2581_Out_3_Float.xxx), _Property_0d65a942fa4c458ebf1d5b61426a6680_Out_0_Vector3, (_Property_0899e6b3a7dd4ecb9b9de1f735daed5e_Out_0_Float.xxx), _Lerp_e80ebb2ee2ec40c2a06b11a4bab428cc_Out_3_Vector3);
        Reflectance_1 = _Lerp_e80ebb2ee2ec40c2a06b11a4bab428cc_Out_3_Vector3;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Normalize_float3(float3 In, out float3 Out)
        {
            Out = normalize(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
        Out = A * B;
        }
        
        struct Bindings_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        };
        
        void SG_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float(float3 _NormalWS, bool _NormalWS_764643e0455a4ece932285a0241516e7_IsConnected, float _Smoothness, float3 _Reflectance, Bindings_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float IN, out float3 Specular_1)
        {
        float3 _MainLightDirection_436ef0cdefe04605b70adabe76102606_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_436ef0cdefe04605b70adabe76102606_Direction_0_Vector3);
        float3 _Negate_6a9ea0a4355445dcb9ea2b983008a197_Out_1_Vector3;
        Unity_Negate_float3(_MainLightDirection_436ef0cdefe04605b70adabe76102606_Direction_0_Vector3, _Negate_6a9ea0a4355445dcb9ea2b983008a197_Out_1_Vector3);
        float3 _Add_fec1db3bbef240b8b1b7d4f12ea2c97f_Out_2_Vector3;
        Unity_Add_float3(IN.WorldSpaceViewDirection, _Negate_6a9ea0a4355445dcb9ea2b983008a197_Out_1_Vector3, _Add_fec1db3bbef240b8b1b7d4f12ea2c97f_Out_2_Vector3);
        float3 _Normalize_5df6e3535e5842a988f33d859b57987a_Out_1_Vector3;
        Unity_Normalize_float3(_Add_fec1db3bbef240b8b1b7d4f12ea2c97f_Out_2_Vector3, _Normalize_5df6e3535e5842a988f33d859b57987a_Out_1_Vector3);
        float3 _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3 = _NormalWS;
        bool _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3_IsConnected = _NormalWS_764643e0455a4ece932285a0241516e7_IsConnected;
        float3 _BranchOnInputConnection_c0a0900e17284fa980ff63a7456c2199_Out_3_Vector3 = _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3_IsConnected ? _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3 : IN.WorldSpaceNormal;
        float _DotProduct_fdf0af8bd536429fa4b57dc806a67c04_Out_2_Float;
        Unity_DotProduct_float3(_Normalize_5df6e3535e5842a988f33d859b57987a_Out_1_Vector3, _BranchOnInputConnection_c0a0900e17284fa980ff63a7456c2199_Out_3_Vector3, _DotProduct_fdf0af8bd536429fa4b57dc806a67c04_Out_2_Float);
        float _Saturate_716ed2bdf36e4e60a35f74d186b278e9_Out_1_Float;
        Unity_Saturate_float(_DotProduct_fdf0af8bd536429fa4b57dc806a67c04_Out_2_Float, _Saturate_716ed2bdf36e4e60a35f74d186b278e9_Out_1_Float);
        float _Saturate_1d23f425974541b381f4fa80cf5a00b1_Out_1_Float;
        Unity_Saturate_float(_Saturate_716ed2bdf36e4e60a35f74d186b278e9_Out_1_Float, _Saturate_1d23f425974541b381f4fa80cf5a00b1_Out_1_Float);
        float _Property_1766606f1117460388cc77e7505598dd_Out_0_Float = _Smoothness;
        Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4;
        half _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4_Out_1_Float;
        SG_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float(_Property_1766606f1117460388cc77e7505598dd_Out_0_Float, _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4, _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4_Out_1_Float);
        float _Power_c507d979651749238e129118bb4c252f_Out_2_Float;
        Unity_Power_float(_Saturate_1d23f425974541b381f4fa80cf5a00b1_Out_1_Float, _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4_Out_1_Float, _Power_c507d979651749238e129118bb4c252f_Out_2_Float);
        float3 _Property_ab8c048e4ae44d56bf8dcb1bbc8ada89_Out_0_Vector3 = _Reflectance;
        float3 _Multiply_3c43a7f74b6c4c35a534990268ada20a_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Power_c507d979651749238e129118bb4c252f_Out_2_Float.xxx), _Property_ab8c048e4ae44d56bf8dcb1bbc8ada89_Out_0_Vector3, _Multiply_3c43a7f74b6c4c35a534990268ada20a_Out_2_Vector3);
        Specular_1 = _Multiply_3c43a7f74b6c4c35a534990268ada20a_Out_2_Vector3;
        }
        
        // unity-custom-func-begin
        void MainLightString_float(float3 worldPos, out float3 direction, out float3 color, out float shadowAtt){
        #ifdef SHADERGRAPH_PREVIEW
          direction = normalize(float3(-0.7,0.7,-0.7));
          color = float3(1,1,1);
          shadowAtt = 1;
        #else
          #if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)
            float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
            Light mainLight = GetMainLight(shadowCoord);
             direction = mainLight.direction;
        
             color = mainLight.color;
        
             shadowAtt = mainLight.shadowAttenuation;
          #else
            direction = normalize(float3(-0.7,0.7,-0.7));
            color = float3(1,1,1);
            shadowAtt = 1;
          #endif
        #endif
        }
        // unity-custom-func-end
        
        struct Bindings_MainLight_0157caa4ea90d88499c9fdb016582871_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_MainLight_0157caa4ea90d88499c9fdb016582871_float(Bindings_MainLight_0157caa4ea90d88499c9fdb016582871_float IN, out float3 Direction_1, out float3 Color_2, out float ShadowAtt_3)
        {
        float3 _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_direction_1_Vector3;
        float3 _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_color_2_Vector3;
        float _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_shadowAtt_3_Float;
        MainLightString_float(IN.WorldSpacePosition, _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_direction_1_Vector3, _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_color_2_Vector3, _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_shadowAtt_3_Float);
        Direction_1 = _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_direction_1_Vector3;
        Color_2 = _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_color_2_Vector3;
        ShadowAtt_3 = _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_shadowAtt_3_Float;
        }
        
        void Unity_ViewVectorWorld_float(out float3 Out, float3 WorldSpacePosition)
        {
            Out = _WorldSpaceCameraPos.xyz - GetAbsolutePositionWS(WorldSpacePosition);
            if(!IsPerspectiveProjection())
            {
                Out = GetViewForwardDir() * dot(Out, GetViewForwardDir());
            }
        }
        
        // unity-custom-func-begin
        void AddAdditionalLights_float(float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView, float MainDiffuse, float3 MainSpecular, float3 MainColor, out float Diffuse, out float3 Specular, out float3 Color){
        // Initialize outputs
        
        Diffuse = 0;
        
        Specular = 0;
        
        Color = 0;
        
        
        
        #ifndef SHADERGRAPH_PREVIEW
        
        
        
           // --- MAIN LIGHT ---
        
          Light mainLight = GetMainLight();
        
          mainLight.shadowAttenuation = MainLightRealtimeShadow(float4(WorldPosition, 1.0));
        
          float mainAtten = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
        
         
        
        
        
          float mainNdotL = saturate(dot(WorldNormal, mainLight.direction));
        
          float mainDiffuse = mainAtten * mainNdotL;
        
          float3 mainSpecular = LightingSpecular(mainDiffuse, mainLight.direction, WorldNormal, WorldView, 1, Smoothness);
        
        
        
          Diffuse += mainDiffuse;
        
          Specular += mainSpecular;
        
          Color += mainLight.color * (mainDiffuse + mainSpecular);
        
        
        
            // --- ADDITIONAL LIGHTS ---
        
            uint pixelLightCount = GetAdditionalLightsCount();
        
        
        
            LIGHT_LOOP_BEGIN(pixelLightCount)
        
                
        
                Light light = GetAdditionalPerObjectLight(lightIndex, WorldPosition);
        
        
        
                light.shadowAttenuation = AdditionalLightRealtimeShadow(lightIndex, WorldPosition, light.direction);
        
                float atten = light.distanceAttenuation * light.shadowAttenuation;
        
        
        
                float NdotL = saturate(dot(WorldNormal, light.direction) * 0.5 + 0.5);
        
                float thisDiffuse = atten * NdotL;
        
                float3 thisSpecular = LightingSpecular(thisDiffuse, light.direction, WorldNormal, WorldView, 1, Smoothness);
        
        
        
                Diffuse += thisDiffuse;
        
                Specular += thisSpecular;
        
                     #if defined(_LIGHT_COOKIES)
        
        	      float3 cookieColor = SampleAdditionalLightCookie(lightIndex, WorldPosition);
        
        	      light.color *= cookieColor;
        
                     #endif
        
                Color += light.color * (thisDiffuse + thisSpecular);
        
            LIGHT_LOOP_END
        
        
        
            // Normalize total lighting
        
            //Color = MainColor * Color;
        
        float totalWeight = Diffuse + dot(Specular, float3(0.333, 0.333, 0.333));
        
            Color = totalWeight > 0 ? MainColor * (Color / totalWeight) : MainColor;
        
        
        
        #endif
        }
        // unity-custom-func-end
        
        struct Bindings_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float(float _MainLightDiffuse, float3 _MainLightSpecular, float3 _MainLightColor, float3 _NormalWS, float _Smoothness, Bindings_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float IN, out float Diffuse_1, out float3 Specular_2, out float3 Color_3)
        {
        float _Property_f5ac72f32dc84ae988391b7d51172380_Out_0_Float = _Smoothness;
        float3 _Property_85d85c5164d5483787fecbcecfd6a519_Out_0_Vector3 = _NormalWS;
        float3 _ViewVector_0d331f2b495741bcb8795caa9549f016_Out_0_Vector3;
        Unity_ViewVectorWorld_float(_ViewVector_0d331f2b495741bcb8795caa9549f016_Out_0_Vector3, IN.WorldSpacePosition);
        float _Property_51c13cc19f414dacb86745cec42d9ab6_Out_0_Float = _MainLightDiffuse;
        float3 _Property_f1e96792c3cc4a1e90396147ef9f7160_Out_0_Vector3 = _MainLightColor;
        float _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Diffuse_7_Float;
        float3 _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Specular_8_Vector3;
        float3 _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Color_9_Vector3;
        AddAdditionalLights_float(_Property_f5ac72f32dc84ae988391b7d51172380_Out_0_Float, IN.WorldSpacePosition, _Property_85d85c5164d5483787fecbcecfd6a519_Out_0_Vector3, _ViewVector_0d331f2b495741bcb8795caa9549f016_Out_0_Vector3, _Property_51c13cc19f414dacb86745cec42d9ab6_Out_0_Float, _Property_f1e96792c3cc4a1e90396147ef9f7160_Out_0_Vector3, _Property_f1e96792c3cc4a1e90396147ef9f7160_Out_0_Vector3, _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Diffuse_7_Float, _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Specular_8_Vector3, _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Color_9_Vector3);
        Diffuse_1 = _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Diffuse_7_Float;
        Specular_2 = _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Specular_8_Vector3;
        Color_3 = _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Color_9_Vector3;
        }
        
        void Unity_Posterize_float3(float3 In, float3 Steps, out float3 Out)
        {
            Out = floor(In / (1 / Steps)) * (1 / Steps);
        }
        
        void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
        {
            float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
            Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
        }
        
        // unity-custom-func-begin
        void GetSSAO_float(float2 ScreenPos, out float DirectAO, out float IndirectAO){
         #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT) && !defined(SHADERGRAPH_PREVIEW)
        
         
          float ssao = saturate(SampleAmbientOcclusion(ScreenPos) + (1.0 - _AmbientOcclusionParam.x));
        
           IndirectAO = ssao;
        
           DirectAO = lerp(1.0, ssao, _AmbientOcclusionParam.w);
        
        #else
        
           DirectAO = 1.0;
        
           IndirectAO = 1.0;
        
        #endif
        }
        // unity-custom-func-end
        
        struct Bindings_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float
        {
        float2 NDCPosition;
        };
        
        void SG_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float(float2 _ScreenPos, bool _ScreenPos_72149ac21c784cc8b4614750ec683413_IsConnected, Bindings_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float IN, out float DirectAO_1, out float IndirectAO_2)
        {
        float2 _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2 = _ScreenPos;
        bool _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2_IsConnected = _ScreenPos_72149ac21c784cc8b4614750ec683413_IsConnected;
        float4 _ScreenPosition_a193bed040594170a08ae04fed7ea8a7_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
        float2 _BranchOnInputConnection_8e8119e37af64e9a957c820002cea48c_Out_3_Vector2 = _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2_IsConnected ? _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2 : (_ScreenPosition_a193bed040594170a08ae04fed7ea8a7_Out_0_Vector4.xy);
        float _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_DirectAO_1_Float;
        float _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_IndirectAO_2_Float;
        GetSSAO_float(_BranchOnInputConnection_8e8119e37af64e9a957c820002cea48c_Out_3_Vector2, _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_DirectAO_1_Float, _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_IndirectAO_2_Float);
        DirectAO_1 = _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_DirectAO_1_Float;
        IndirectAO_2 = _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_IndirectAO_2_Float;
        }
        
        void Unity_Minimum_float(float A, float B, out float Out)
        {
            Out = min(A, B);
        };
        
        void Unity_ReflectionProbe_float(float3 ViewDir, float3 Normal, float LOD, out float3 Out)
        {
            Out = SHADERGRAPH_REFLECTION_PROBE(ViewDir, Normal, LOD);
        }
        
        struct Bindings_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        float3 WorldSpacePosition;
        float2 NDCPosition;
        float2 PixelPosition;
        half4 uv1;
        half4 uv2;
        };
        
        void SG_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float(float3 _Base_Color, bool _Base_Color_87794c9dccf9430d8ee1585f52796223_IsConnected, float3 _NormalWS, bool _NormalWS_d8d26d45dd0b4d4a8e8426edcd206b0f_IsConnected, float _AO, float _Smoothness, float _Metallic, float _Reflectance, Bindings_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float IN, out float3 Ambient_1, out float3 Metallic_2, out float DdirectAO_3)
        {
        float3 _Property_50e51e65f64b40c29fa7b10305f0a7b4_Out_0_Vector3 = _Base_Color;
        bool _Property_50e51e65f64b40c29fa7b10305f0a7b4_Out_0_Vector3_IsConnected = _Base_Color_87794c9dccf9430d8ee1585f52796223_IsConnected;
        float _Property_b967758667b74ff5acb2a8af3ff58c39_Out_0_Float = _Metallic;
        float3 _Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3;
        Unity_Lerp_float3(_Property_50e51e65f64b40c29fa7b10305f0a7b4_Out_0_Vector3, float3(0, 0, 0), (_Property_b967758667b74ff5acb2a8af3ff58c39_Out_0_Float.xxx), _Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3);
        float3 _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3 = _NormalWS;
        bool _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3_IsConnected = _NormalWS_d8d26d45dd0b4d4a8e8426edcd206b0f_IsConnected;
        float3 _BranchOnInputConnection_dae192f178d24a5a9c9c540fd5488461_Out_3_Vector3 = _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3_IsConnected ? _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3 : IN.WorldSpaceNormal;
        float3 _BakedGI_1e2ac029f59d497abdc2a7bcf1eb3346_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, _BranchOnInputConnection_dae192f178d24a5a9c9c540fd5488461_Out_3_Vector3, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
        float _Property_26a27af7a1144ea0a77aad0b49f12697_Out_0_Float = _AO;
        Bindings_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float _SSAO_f6107fe543294e7f932b8c5cfc86bdd5;
        _SSAO_f6107fe543294e7f932b8c5cfc86bdd5.NDCPosition = IN.NDCPosition;
        half _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_DirectAO_1_Float;
        half _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_IndirectAO_2_Float;
        SG_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float(half2 (0, 0), false, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_DirectAO_1_Float, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_IndirectAO_2_Float);
        float _Minimum_3ac81a32823a439c8b51e89edf949c26_Out_2_Float;
        Unity_Minimum_float(_Property_26a27af7a1144ea0a77aad0b49f12697_Out_0_Float, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_IndirectAO_2_Float, _Minimum_3ac81a32823a439c8b51e89edf949c26_Out_2_Float);
        float3 _Multiply_423df8bc24d94add8f44f6db0bf46798_Out_2_Vector3;
        Unity_Multiply_float3_float3(_BakedGI_1e2ac029f59d497abdc2a7bcf1eb3346_Out_1_Vector3, (_Minimum_3ac81a32823a439c8b51e89edf949c26_Out_2_Float.xxx), _Multiply_423df8bc24d94add8f44f6db0bf46798_Out_2_Vector3);
        float3 _Multiply_4ee1636476de4393a1b2e9162c2328b9_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3, _Multiply_423df8bc24d94add8f44f6db0bf46798_Out_2_Vector3, _Multiply_4ee1636476de4393a1b2e9162c2328b9_Out_2_Vector3);
        float _Property_76583d84384b44cbb79843c917044c1b_Out_0_Float = _Reflectance;
        float _Property_f511aceb4377409690a68e8d8a16a032_Out_0_Float = _Smoothness;
        float _Lerp_496af92f7e424d5586ca49ffe949aff6_Out_3_Float;
        Unity_Lerp_float(float(8), float(0), _Property_f511aceb4377409690a68e8d8a16a032_Out_0_Float, _Lerp_496af92f7e424d5586ca49ffe949aff6_Out_3_Float);
        float3 _ReflectionProbe_d9eb69c9b54641068d948dd37fec342f_Out_3_Vector3;
        Unity_ReflectionProbe_float(IN.WorldSpaceViewDirection, _BranchOnInputConnection_dae192f178d24a5a9c9c540fd5488461_Out_3_Vector3, _Lerp_496af92f7e424d5586ca49ffe949aff6_Out_3_Float, _ReflectionProbe_d9eb69c9b54641068d948dd37fec342f_Out_3_Vector3);
        float3 _Multiply_25ab7f2171954fc4a710fe8c8c2cc402_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Property_76583d84384b44cbb79843c917044c1b_Out_0_Float.xxx), _ReflectionProbe_d9eb69c9b54641068d948dd37fec342f_Out_3_Vector3, _Multiply_25ab7f2171954fc4a710fe8c8c2cc402_Out_2_Vector3);
        float3 _Add_475a3a92ac834f51b869dedfbba66e44_Out_2_Vector3;
        Unity_Add_float3(_Multiply_4ee1636476de4393a1b2e9162c2328b9_Out_2_Vector3, _Multiply_25ab7f2171954fc4a710fe8c8c2cc402_Out_2_Vector3, _Add_475a3a92ac834f51b869dedfbba66e44_Out_2_Vector3);
        Ambient_1 = _Add_475a3a92ac834f51b869dedfbba66e44_Out_2_Vector3;
        Metallic_2 = _Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3;
        DdirectAO_3 = _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_DirectAO_1_Float;
        }
        
        void Unity_Saturate_float3(float3 In, out float3 Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Posterize_float(float In, float Steps, out float Out)
        {
            Out = floor(In / (1 / Steps)) * (1 / Steps);
        }
        
        struct Bindings_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        float3 WorldSpacePosition;
        float2 NDCPosition;
        float2 PixelPosition;
        half4 uv1;
        half4 uv2;
        };
        
        void SG_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float(float3 _Base_Color, bool _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected, float3 _NormalWS, bool _NormalWS_d2c1afa83f4f4d20ac34f445f069483d_IsConnected, float _Metallic, float _Smoothness, float _AO, float _Steps, float _Specular_Steps, float _Smoothness_Strength, Bindings_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float IN, out float3 Lit_1)
        {
        float3 _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3 = _NormalWS;
        bool _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3_IsConnected = _NormalWS_d2c1afa83f4f4d20ac34f445f069483d_IsConnected;
        float3 _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3 = _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3_IsConnected ? _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3 : IN.WorldSpaceNormal;
        Bindings_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91;
        _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91.WorldSpaceNormal = IN.WorldSpaceNormal;
        half _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float;
        SG_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float(_BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91, _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float);
        float _Property_af2c0fb6df7d4dc9abade557540f3ed7_Out_0_Float = _Smoothness;
        Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e;
        half _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float;
        SG_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float(_Property_af2c0fb6df7d4dc9abade557540f3ed7_Out_0_Float, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float);
        float3 _Property_b3987ddafd854a0a85863bc89dcde720_Out_0_Vector3 = _Base_Color;
        bool _Property_b3987ddafd854a0a85863bc89dcde720_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float _Property_3c53a42fedba46ba9fae794424fbe85e_Out_0_Float = _Metallic;
        Bindings_Reflectance_3dd134828a90d7446802ac63dbc0221d_float _Reflectance_0d5aeeef450744cf885ab04f87797c7b;
        _Reflectance_0d5aeeef450744cf885ab04f87797c7b.WorldSpaceNormal = IN.WorldSpaceNormal;
        _Reflectance_0d5aeeef450744cf885ab04f87797c7b.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3;
        SG_Reflectance_3dd134828a90d7446802ac63dbc0221d_float(_Property_b3987ddafd854a0a85863bc89dcde720_Out_0_Vector3, true, _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _Property_3c53a42fedba46ba9fae794424fbe85e_Out_0_Float, _Reflectance_0d5aeeef450744cf885ab04f87797c7b, _Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3);
        Bindings_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8;
        _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8.WorldSpaceNormal = IN.WorldSpaceNormal;
        _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8_Specular_1_Vector3;
        SG_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float(_BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float, _Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3, _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8, _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8_Specular_1_Vector3);
        float3 _Multiply_577b66af727b4e43be242c88de8def99_Out_2_Vector3;
        Unity_Multiply_float3_float3((_HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float.xxx), _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8_Specular_1_Vector3, _Multiply_577b66af727b4e43be242c88de8def99_Out_2_Vector3);
        Bindings_MainLight_0157caa4ea90d88499c9fdb016582871_float _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a;
        _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a.WorldSpacePosition = IN.WorldSpacePosition;
        float3 _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Direction_1_Vector3;
        float3 _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Color_2_Vector3;
        float _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_ShadowAtt_3_Float;
        SG_MainLight_0157caa4ea90d88499c9fdb016582871_float(_MainLight_1fffd9ee56744cde8cd3210b8fc0a55a, _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Direction_1_Vector3, _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Color_2_Vector3, _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_ShadowAtt_3_Float);
        float3 _Multiply_e3a50f2fbf214e64931956c0b1e7f194_Out_2_Vector3;
        Unity_Multiply_float3_float3(_MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Color_2_Vector3, (_MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_ShadowAtt_3_Float.xxx), _Multiply_e3a50f2fbf214e64931956c0b1e7f194_Out_2_Vector3);
        Bindings_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021;
        _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021.WorldSpacePosition = IN.WorldSpacePosition;
        float _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Diffuse_1_Float;
        float3 _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Specular_2_Vector3;
        float3 _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3;
        SG_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float(_HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float, _Multiply_577b66af727b4e43be242c88de8def99_Out_2_Vector3, _Multiply_e3a50f2fbf214e64931956c0b1e7f194_Out_2_Vector3, _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Diffuse_1_Float, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Specular_2_Vector3, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3);
        float _Property_7fabb829117947b8956799cbdf3be9e0_Out_0_Float = _Steps;
        float3 _Posterize_8f26285a65784a038d6d9d2087181b46_Out_2_Vector3;
        Unity_Posterize_float3(_AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, (_Property_7fabb829117947b8956799cbdf3be9e0_Out_0_Float.xxx), _Posterize_8f26285a65784a038d6d9d2087181b46_Out_2_Vector3);
        float3 _Property_3668f71779994b3a9fcc7095f880cb12_Out_0_Vector3 = _Base_Color;
        bool _Property_3668f71779994b3a9fcc7095f880cb12_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float3 _Saturation_e12bfe3d861c4f1fa4c1297d0dd08cc0_Out_2_Vector3;
        Unity_Saturation_float(_Property_3668f71779994b3a9fcc7095f880cb12_Out_0_Vector3, float(1.6), _Saturation_e12bfe3d861c4f1fa4c1297d0dd08cc0_Out_2_Vector3);
        float3 _Multiply_77c53f8596dd487fa3009d208e99c0fd_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Saturation_e12bfe3d861c4f1fa4c1297d0dd08cc0_Out_2_Vector3, float3(0.1, 0.1, 0.1), _Multiply_77c53f8596dd487fa3009d208e99c0fd_Out_2_Vector3);
        float3 _Property_494170212d604ba0a52a5b39f34e4c3f_Out_0_Vector3 = _Base_Color;
        bool _Property_494170212d604ba0a52a5b39f34e4c3f_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float3 _Multiply_e22a63bb0da045618a0202e45051407e_Out_2_Vector3;
        Unity_Multiply_float3_float3((_AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Diffuse_1_Float.xxx), _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, _Multiply_e22a63bb0da045618a0202e45051407e_Out_2_Vector3);
        float3 _Property_53d2f61f22684efca5bbd024184799a4_Out_0_Vector3 = _Base_Color;
        bool _Property_53d2f61f22684efca5bbd024184799a4_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float _Property_5204695d7d6d4d03b28eba481d372192_Out_0_Float = _AO;
        float _Property_aa169c45708b4343b76a3b52b483b8e2_Out_0_Float = _Smoothness;
        float _Property_1d116579c59d45ccaa1bdcb07a5ec5d5_Out_0_Float = _Metallic;
        Bindings_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.WorldSpaceNormal = IN.WorldSpaceNormal;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.WorldSpacePosition = IN.WorldSpacePosition;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.NDCPosition = IN.NDCPosition;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.PixelPosition = IN.PixelPosition;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.uv1 = IN.uv1;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.uv2 = IN.uv2;
        half3 _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Ambient_1_Vector3;
        half3 _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Metallic_2_Vector3;
        half _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_DdirectAO_3_Float;
        SG_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float(_Property_53d2f61f22684efca5bbd024184799a4_Out_0_Vector3, true, _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _Property_5204695d7d6d4d03b28eba481d372192_Out_0_Float, _Property_aa169c45708b4343b76a3b52b483b8e2_Out_0_Float, _Property_1d116579c59d45ccaa1bdcb07a5ec5d5_Out_0_Float, (_Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3).x, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Ambient_1_Vector3, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Metallic_2_Vector3, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_DdirectAO_3_Float);
        float3 _Add_96c33650335248caa7429346ca897bb7_Out_2_Vector3;
        Unity_Add_float3(_Multiply_e22a63bb0da045618a0202e45051407e_Out_2_Vector3, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Ambient_1_Vector3, _Add_96c33650335248caa7429346ca897bb7_Out_2_Vector3);
        float3 _Saturate_b993ba602dea4759baa643682158093c_Out_1_Vector3;
        Unity_Saturate_float3(_Add_96c33650335248caa7429346ca897bb7_Out_2_Vector3, _Saturate_b993ba602dea4759baa643682158093c_Out_1_Vector3);
        float3 _Saturation_559bfc31eca847d3acd46b2fd284dc13_Out_2_Vector3;
        Unity_Saturation_float(_Saturate_b993ba602dea4759baa643682158093c_Out_1_Vector3, float(1), _Saturation_559bfc31eca847d3acd46b2fd284dc13_Out_2_Vector3);
        float _Swizzle_3952ca1d9d274331b6a4a49630c606ae_Out_1_Float = _Saturation_559bfc31eca847d3acd46b2fd284dc13_Out_2_Vector3.x;
        float _Posterize_268e85bcc1d5424485e214740b6468e3_Out_2_Float;
        Unity_Posterize_float(_Swizzle_3952ca1d9d274331b6a4a49630c606ae_Out_1_Float, _Property_7fabb829117947b8956799cbdf3be9e0_Out_0_Float, _Posterize_268e85bcc1d5424485e214740b6468e3_Out_2_Float);
        float3 _Lerp_8c017a3a82434ee38c4432bf457e582b_Out_3_Vector3;
        Unity_Lerp_float3(_Multiply_77c53f8596dd487fa3009d208e99c0fd_Out_2_Vector3, _Property_494170212d604ba0a52a5b39f34e4c3f_Out_0_Vector3, (_Posterize_268e85bcc1d5424485e214740b6468e3_Out_2_Float.xxx), _Lerp_8c017a3a82434ee38c4432bf457e582b_Out_3_Vector3);
        float3 _Multiply_5c248bf4893d4acd9b10421fae8923d1_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Posterize_8f26285a65784a038d6d9d2087181b46_Out_2_Vector3, _Lerp_8c017a3a82434ee38c4432bf457e582b_Out_3_Vector3, _Multiply_5c248bf4893d4acd9b10421fae8923d1_Out_2_Vector3);
        float _Property_48b9a97103af41f3b6cafeceb8a6a0b7_Out_0_Float = _Smoothness_Strength;
        float3 _Add_57faecf3e31346a68db61c3e666b2975_Out_2_Vector3;
        Unity_Add_float3((_Property_48b9a97103af41f3b6cafeceb8a6a0b7_Out_0_Float.xxx), _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, _Add_57faecf3e31346a68db61c3e666b2975_Out_2_Vector3);
        float3 _Multiply_b90f73fc105c4da3b7586428b54d0c03_Out_2_Vector3;
        Unity_Multiply_float3_float3(_AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Specular_2_Vector3, _Add_57faecf3e31346a68db61c3e666b2975_Out_2_Vector3, _Multiply_b90f73fc105c4da3b7586428b54d0c03_Out_2_Vector3);
        float3 _Saturation_c81f01b7b0de4608a25fde9890c55880_Out_2_Vector3;
        Unity_Saturation_float(_Multiply_b90f73fc105c4da3b7586428b54d0c03_Out_2_Vector3, float(0), _Saturation_c81f01b7b0de4608a25fde9890c55880_Out_2_Vector3);
        float _Swizzle_fbfd922c5f7d42d399186d59a86d5660_Out_1_Float = _Saturation_c81f01b7b0de4608a25fde9890c55880_Out_2_Vector3.x;
        float _Property_62c311d0a3534ad1ad3a7b66c978a36c_Out_0_Float = _Specular_Steps;
        float _Posterize_1027f91210d94108ae1c593d0fb61d8b_Out_2_Float;
        Unity_Posterize_float(_Swizzle_fbfd922c5f7d42d399186d59a86d5660_Out_1_Float, _Property_62c311d0a3534ad1ad3a7b66c978a36c_Out_0_Float, _Posterize_1027f91210d94108ae1c593d0fb61d8b_Out_2_Float);
        float3 _Multiply_c92faf2f8cc04ff29ec2ba172ebefa59_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Posterize_1027f91210d94108ae1c593d0fb61d8b_Out_2_Float.xxx), _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, _Multiply_c92faf2f8cc04ff29ec2ba172ebefa59_Out_2_Vector3);
        float3 _Add_321e04b0f3bb47558352363c94828b10_Out_2_Vector3;
        Unity_Add_float3(_Multiply_5c248bf4893d4acd9b10421fae8923d1_Out_2_Vector3, _Multiply_c92faf2f8cc04ff29ec2ba172ebefa59_Out_2_Vector3, _Add_321e04b0f3bb47558352363c94828b10_Out_2_Vector3);
        Lit_1 = _Add_321e04b0f3bb47558352363c94828b10_Out_2_Vector3;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_BaseMap);
            float4 _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D.tex, _Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D.samplerstate, _Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_R_4_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.r;
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_G_5_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.g;
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_B_6_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.b;
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_A_7_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.a;
            float _Property_7ab914554b2b477db515a77874d1d4cd_Out_0_Float = _LightingSmoothness;
            float _Property_9e7bc9fe055c4e4db532299787c0e3ed_Out_0_Float = _Steps;
            float _Property_9d34386f48e54d9eaeda9d04da1ad72f_Out_0_Float = _SpecularSteps;
            Bindings_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.WorldSpaceNormal = IN.WorldSpaceNormal;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.WorldSpacePosition = IN.WorldSpacePosition;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.NDCPosition = IN.NDCPosition;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.PixelPosition = IN.PixelPosition;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.uv1 = IN.uv1;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.uv2 = IN.uv2;
            float3 _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3_Lit_1_Vector3;
            SG_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float((_SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.xyz), true, float3 (0, 0, 0), false, float(0), _Property_7ab914554b2b477db515a77874d1d4cd_Out_0_Float, float(0), _Property_9e7bc9fe055c4e4db532299787c0e3ed_Out_0_Float, _Property_9d34386f48e54d9eaeda9d04da1ad72f_Out_0_Float, float(0), _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3, _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3_Lit_1_Vector3);
            UnityTexture2D _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_AlphaMap);
            float4 _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.tex, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.samplerstate, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_R_4_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.r;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_G_5_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.g;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_B_6_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.b;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.a;
            surface.BaseColor = _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3_Lit_1_Vector3;
            surface.Alpha = float(1);
            surface.AlphaClipThreshold = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
            // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            float3 unnormalizedNormalWS = input.normalWS;
            const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        
        
            output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
        
        
            output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
            output.WorldSpacePosition = input.positionWS;
        
            #if UNITY_UV_STARTS_AT_TOP
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #else
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #endif
        
            output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
            output.NDCPosition.y = 1.0f - output.NDCPosition.y;
        
            output.uv0 = input.texCoord0;
            output.uv1 = input.texCoord1;
            output.uv2 = input.texCoord2;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "Universal 2D"
            Tags
            {
                "LightMode" = "Universal2D"
            }
        
        // Render State
        Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD1
        #define VARYINGS_NEED_TEXCOORD2
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_2D
        #define _ALPHATEST_ON 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 texCoord0;
             float4 texCoord1;
             float4 texCoord2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpaceNormal;
             float3 WorldSpaceViewDirection;
             float3 WorldSpacePosition;
             float2 NDCPosition;
             float2 PixelPosition;
             float4 uv0;
             float4 uv1;
             float4 uv2;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0 : INTERP0;
             float4 texCoord1 : INTERP1;
             float4 texCoord2 : INTERP2;
             float3 positionWS : INTERP3;
             float3 normalWS : INTERP4;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            output.texCoord1.xyzw = input.texCoord1;
            output.texCoord2.xyzw = input.texCoord2;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            output.texCoord1 = input.texCoord1.xyzw;
            output.texCoord2 = input.texCoord2.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _EmissiveIntensity;
        float4 _BaseMap_TexelSize;
        float4 _EmissiveMask_TexelSize;
        float4 _RoughnessMap_TexelSize;
        float4 _MetalicMAp_TexelSize;
        float4 _AlphaMap_TexelSize;
        float _LightingSmoothness;
        float _Steps;
        float _SpecularSteps;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissiveMask);
        SAMPLER(sampler_EmissiveMask);
        TEXTURE2D(_RoughnessMap);
        SAMPLER(sampler_RoughnessMap);
        TEXTURE2D(_MetalicMAp);
        SAMPLER(sampler_MetalicMAp);
        TEXTURE2D(_AlphaMap);
        SAMPLER(sampler_AlphaMap);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void MainLightDirection_float(out float3 Direction)
        {
            #if SHADERGRAPH_PREVIEW
            Direction = half3(-0.5, -0.5, 0);
            #else
            Direction = SHADERGRAPH_MAIN_LIGHT_DIRECTION();
            #endif
        }
        
        void Unity_Negate_float3(float3 In, out float3 Out)
        {
            Out = -1 * In;
        }
        
        void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
        Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        struct Bindings_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float
        {
        float3 WorldSpaceNormal;
        };
        
        void SG_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float(float3 _NormalWS, bool _NormalWS_a616d67d9a9241cf9d4230dbfe62563f_IsConnected, Bindings_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float IN, out float Diffuse_1)
        {
        float3 _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3 = _NormalWS;
        bool _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3_IsConnected = _NormalWS_a616d67d9a9241cf9d4230dbfe62563f_IsConnected;
        float3 _BranchOnInputConnection_565230d8358148bfac6929645968ec6d_Out_3_Vector3 = _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3_IsConnected ? _Property_4883f23117a04181b10efdad930e47d3_Out_0_Vector3 : IN.WorldSpaceNormal;
        float3 _MainLightDirection_46d8c4b675334560b12bb67c4e1ab4ca_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_46d8c4b675334560b12bb67c4e1ab4ca_Direction_0_Vector3);
        float3 _Negate_cefa58b1986e43aa934b4f687b6018fd_Out_1_Vector3;
        Unity_Negate_float3(_MainLightDirection_46d8c4b675334560b12bb67c4e1ab4ca_Direction_0_Vector3, _Negate_cefa58b1986e43aa934b4f687b6018fd_Out_1_Vector3);
        float _DotProduct_b0063238199f49058ba189bac4da970d_Out_2_Float;
        Unity_DotProduct_float3(_BranchOnInputConnection_565230d8358148bfac6929645968ec6d_Out_3_Vector3, _Negate_cefa58b1986e43aa934b4f687b6018fd_Out_1_Vector3, _DotProduct_b0063238199f49058ba189bac4da970d_Out_2_Float);
        float _Multiply_d11fc76f5fe1410e894c6ac565e2d937_Out_2_Float;
        Unity_Multiply_float_float(_DotProduct_b0063238199f49058ba189bac4da970d_Out_2_Float, 0.5, _Multiply_d11fc76f5fe1410e894c6ac565e2d937_Out_2_Float);
        float _Add_1a63a84f8dfc4d8ca825f5717c5a9d07_Out_2_Float;
        Unity_Add_float(_Multiply_d11fc76f5fe1410e894c6ac565e2d937_Out_2_Float, float(0.5), _Add_1a63a84f8dfc4d8ca825f5717c5a9d07_Out_2_Float);
        float _Saturate_95b5072011dc4a2c899250596471f236_Out_1_Float;
        Unity_Saturate_float(_Add_1a63a84f8dfc4d8ca825f5717c5a9d07_Out_2_Float, _Saturate_95b5072011dc4a2c899250596471f236_Out_1_Float);
        Diffuse_1 = _Saturate_95b5072011dc4a2c899250596471f236_Out_1_Float;
        }
        
        void Unity_Exponential2_float(float In, out float Out)
        {
            Out = exp2(In);
        }
        
        struct Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float
        {
        };
        
        void SG_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float(float _In, Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float IN, out float Out_1)
        {
        float _Property_ad239b68c11f4fcb920425661841c000_Out_0_Float = _In;
        float _Multiply_187758eae33846cb912e3d80a692d9b0_Out_2_Float;
        Unity_Multiply_float_float(_Property_ad239b68c11f4fcb920425661841c000_Out_0_Float, 10, _Multiply_187758eae33846cb912e3d80a692d9b0_Out_2_Float);
        float _Add_023529f132584452b7ef9ffb6f67eb4b_Out_2_Float;
        Unity_Add_float(_Multiply_187758eae33846cb912e3d80a692d9b0_Out_2_Float, float(1), _Add_023529f132584452b7ef9ffb6f67eb4b_Out_2_Float);
        float _Exponential_b2744bb952e44d0cb0d813283e357f8c_Out_1_Float;
        Unity_Exponential2_float(_Add_023529f132584452b7ef9ffb6f67eb4b_Out_2_Float, _Exponential_b2744bb952e44d0cb0d813283e357f8c_Out_1_Float);
        Out_1 = _Exponential_b2744bb952e44d0cb0d813283e357f8c_Out_1_Float;
        }
        
        void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
        {
            Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
        }
        
        void Unity_Lerp_float(float A, float B, float T, out float Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        struct Bindings_Reflectance_3dd134828a90d7446802ac63dbc0221d_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        };
        
        void SG_Reflectance_3dd134828a90d7446802ac63dbc0221d_float(float3 _Base_Color, bool _Base_Color_57260f26a3c14234b948dd74141df9c7_IsConnected, float3 _NormalWS, bool _NormalWS_ab3d33962fc64884948809ad8eaf7aa5_IsConnected, float _Metallic, Bindings_Reflectance_3dd134828a90d7446802ac63dbc0221d_float IN, out float3 Reflectance_1)
        {
        float3 _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3 = _NormalWS;
        bool _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3_IsConnected = _NormalWS_ab3d33962fc64884948809ad8eaf7aa5_IsConnected;
        float3 _BranchOnInputConnection_2b572462617842bf85ea171e93239787_Out_3_Vector3 = _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3_IsConnected ? _Property_210775ff505c40e68af30bb7f0607b19_Out_0_Vector3 : IN.WorldSpaceNormal;
        float _FresnelEffect_c130e1f911b140299ff5363626d5f22b_Out_3_Float;
        Unity_FresnelEffect_float(_BranchOnInputConnection_2b572462617842bf85ea171e93239787_Out_3_Vector3, IN.WorldSpaceViewDirection, float(3), _FresnelEffect_c130e1f911b140299ff5363626d5f22b_Out_3_Float);
        float _Lerp_3ed50cf0a0a94d98a7de54752c2f2581_Out_3_Float;
        Unity_Lerp_float(float(0.04), float(1), _FresnelEffect_c130e1f911b140299ff5363626d5f22b_Out_3_Float, _Lerp_3ed50cf0a0a94d98a7de54752c2f2581_Out_3_Float);
        float3 _Property_0d65a942fa4c458ebf1d5b61426a6680_Out_0_Vector3 = _Base_Color;
        bool _Property_0d65a942fa4c458ebf1d5b61426a6680_Out_0_Vector3_IsConnected = _Base_Color_57260f26a3c14234b948dd74141df9c7_IsConnected;
        float _Property_0899e6b3a7dd4ecb9b9de1f735daed5e_Out_0_Float = _Metallic;
        float3 _Lerp_e80ebb2ee2ec40c2a06b11a4bab428cc_Out_3_Vector3;
        Unity_Lerp_float3((_Lerp_3ed50cf0a0a94d98a7de54752c2f2581_Out_3_Float.xxx), _Property_0d65a942fa4c458ebf1d5b61426a6680_Out_0_Vector3, (_Property_0899e6b3a7dd4ecb9b9de1f735daed5e_Out_0_Float.xxx), _Lerp_e80ebb2ee2ec40c2a06b11a4bab428cc_Out_3_Vector3);
        Reflectance_1 = _Lerp_e80ebb2ee2ec40c2a06b11a4bab428cc_Out_3_Vector3;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Normalize_float3(float3 In, out float3 Out)
        {
            Out = normalize(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
        Out = A * B;
        }
        
        struct Bindings_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        };
        
        void SG_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float(float3 _NormalWS, bool _NormalWS_764643e0455a4ece932285a0241516e7_IsConnected, float _Smoothness, float3 _Reflectance, Bindings_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float IN, out float3 Specular_1)
        {
        float3 _MainLightDirection_436ef0cdefe04605b70adabe76102606_Direction_0_Vector3;
        MainLightDirection_float(_MainLightDirection_436ef0cdefe04605b70adabe76102606_Direction_0_Vector3);
        float3 _Negate_6a9ea0a4355445dcb9ea2b983008a197_Out_1_Vector3;
        Unity_Negate_float3(_MainLightDirection_436ef0cdefe04605b70adabe76102606_Direction_0_Vector3, _Negate_6a9ea0a4355445dcb9ea2b983008a197_Out_1_Vector3);
        float3 _Add_fec1db3bbef240b8b1b7d4f12ea2c97f_Out_2_Vector3;
        Unity_Add_float3(IN.WorldSpaceViewDirection, _Negate_6a9ea0a4355445dcb9ea2b983008a197_Out_1_Vector3, _Add_fec1db3bbef240b8b1b7d4f12ea2c97f_Out_2_Vector3);
        float3 _Normalize_5df6e3535e5842a988f33d859b57987a_Out_1_Vector3;
        Unity_Normalize_float3(_Add_fec1db3bbef240b8b1b7d4f12ea2c97f_Out_2_Vector3, _Normalize_5df6e3535e5842a988f33d859b57987a_Out_1_Vector3);
        float3 _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3 = _NormalWS;
        bool _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3_IsConnected = _NormalWS_764643e0455a4ece932285a0241516e7_IsConnected;
        float3 _BranchOnInputConnection_c0a0900e17284fa980ff63a7456c2199_Out_3_Vector3 = _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3_IsConnected ? _Property_15d9d70e9b8b4c6690c44c981a620f78_Out_0_Vector3 : IN.WorldSpaceNormal;
        float _DotProduct_fdf0af8bd536429fa4b57dc806a67c04_Out_2_Float;
        Unity_DotProduct_float3(_Normalize_5df6e3535e5842a988f33d859b57987a_Out_1_Vector3, _BranchOnInputConnection_c0a0900e17284fa980ff63a7456c2199_Out_3_Vector3, _DotProduct_fdf0af8bd536429fa4b57dc806a67c04_Out_2_Float);
        float _Saturate_716ed2bdf36e4e60a35f74d186b278e9_Out_1_Float;
        Unity_Saturate_float(_DotProduct_fdf0af8bd536429fa4b57dc806a67c04_Out_2_Float, _Saturate_716ed2bdf36e4e60a35f74d186b278e9_Out_1_Float);
        float _Saturate_1d23f425974541b381f4fa80cf5a00b1_Out_1_Float;
        Unity_Saturate_float(_Saturate_716ed2bdf36e4e60a35f74d186b278e9_Out_1_Float, _Saturate_1d23f425974541b381f4fa80cf5a00b1_Out_1_Float);
        float _Property_1766606f1117460388cc77e7505598dd_Out_0_Float = _Smoothness;
        Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4;
        half _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4_Out_1_Float;
        SG_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float(_Property_1766606f1117460388cc77e7505598dd_Out_0_Float, _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4, _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4_Out_1_Float);
        float _Power_c507d979651749238e129118bb4c252f_Out_2_Float;
        Unity_Power_float(_Saturate_1d23f425974541b381f4fa80cf5a00b1_Out_1_Float, _AdjustSmoothness_708600c60f9a427295fa3ca1e602b6a4_Out_1_Float, _Power_c507d979651749238e129118bb4c252f_Out_2_Float);
        float3 _Property_ab8c048e4ae44d56bf8dcb1bbc8ada89_Out_0_Vector3 = _Reflectance;
        float3 _Multiply_3c43a7f74b6c4c35a534990268ada20a_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Power_c507d979651749238e129118bb4c252f_Out_2_Float.xxx), _Property_ab8c048e4ae44d56bf8dcb1bbc8ada89_Out_0_Vector3, _Multiply_3c43a7f74b6c4c35a534990268ada20a_Out_2_Vector3);
        Specular_1 = _Multiply_3c43a7f74b6c4c35a534990268ada20a_Out_2_Vector3;
        }
        
        // unity-custom-func-begin
        void MainLightString_float(float3 worldPos, out float3 direction, out float3 color, out float shadowAtt){
        #ifdef SHADERGRAPH_PREVIEW
          direction = normalize(float3(-0.7,0.7,-0.7));
          color = float3(1,1,1);
          shadowAtt = 1;
        #else
          #if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)
            float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
            Light mainLight = GetMainLight(shadowCoord);
             direction = mainLight.direction;
        
             color = mainLight.color;
        
             shadowAtt = mainLight.shadowAttenuation;
          #else
            direction = normalize(float3(-0.7,0.7,-0.7));
            color = float3(1,1,1);
            shadowAtt = 1;
          #endif
        #endif
        }
        // unity-custom-func-end
        
        struct Bindings_MainLight_0157caa4ea90d88499c9fdb016582871_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_MainLight_0157caa4ea90d88499c9fdb016582871_float(Bindings_MainLight_0157caa4ea90d88499c9fdb016582871_float IN, out float3 Direction_1, out float3 Color_2, out float ShadowAtt_3)
        {
        float3 _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_direction_1_Vector3;
        float3 _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_color_2_Vector3;
        float _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_shadowAtt_3_Float;
        MainLightString_float(IN.WorldSpacePosition, _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_direction_1_Vector3, _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_color_2_Vector3, _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_shadowAtt_3_Float);
        Direction_1 = _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_direction_1_Vector3;
        Color_2 = _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_color_2_Vector3;
        ShadowAtt_3 = _MainLightStringCustomFunction_3609bd60b089453180aa67bd73dd9793_shadowAtt_3_Float;
        }
        
        void Unity_ViewVectorWorld_float(out float3 Out, float3 WorldSpacePosition)
        {
            Out = _WorldSpaceCameraPos.xyz - GetAbsolutePositionWS(WorldSpacePosition);
            if(!IsPerspectiveProjection())
            {
                Out = GetViewForwardDir() * dot(Out, GetViewForwardDir());
            }
        }
        
        // unity-custom-func-begin
        void AddAdditionalLights_float(float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView, float MainDiffuse, float3 MainSpecular, float3 MainColor, out float Diffuse, out float3 Specular, out float3 Color){
        // Initialize outputs
        
        Diffuse = 0;
        
        Specular = 0;
        
        Color = 0;
        
        
        
        #ifndef SHADERGRAPH_PREVIEW
        
        
        
           // --- MAIN LIGHT ---
        
          Light mainLight = GetMainLight();
        
          mainLight.shadowAttenuation = MainLightRealtimeShadow(float4(WorldPosition, 1.0));
        
          float mainAtten = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
        
         
        
        
        
          float mainNdotL = saturate(dot(WorldNormal, mainLight.direction));
        
          float mainDiffuse = mainAtten * mainNdotL;
        
          float3 mainSpecular = LightingSpecular(mainDiffuse, mainLight.direction, WorldNormal, WorldView, 1, Smoothness);
        
        
        
          Diffuse += mainDiffuse;
        
          Specular += mainSpecular;
        
          Color += mainLight.color * (mainDiffuse + mainSpecular);
        
        
        
            // --- ADDITIONAL LIGHTS ---
        
            uint pixelLightCount = GetAdditionalLightsCount();
        
        
        
            LIGHT_LOOP_BEGIN(pixelLightCount)
        
                
        
                Light light = GetAdditionalPerObjectLight(lightIndex, WorldPosition);
        
        
        
                light.shadowAttenuation = AdditionalLightRealtimeShadow(lightIndex, WorldPosition, light.direction);
        
                float atten = light.distanceAttenuation * light.shadowAttenuation;
        
        
        
                float NdotL = saturate(dot(WorldNormal, light.direction) * 0.5 + 0.5);
        
                float thisDiffuse = atten * NdotL;
        
                float3 thisSpecular = LightingSpecular(thisDiffuse, light.direction, WorldNormal, WorldView, 1, Smoothness);
        
        
        
                Diffuse += thisDiffuse;
        
                Specular += thisSpecular;
        
                     #if defined(_LIGHT_COOKIES)
        
        	      float3 cookieColor = SampleAdditionalLightCookie(lightIndex, WorldPosition);
        
        	      light.color *= cookieColor;
        
                     #endif
        
                Color += light.color * (thisDiffuse + thisSpecular);
        
            LIGHT_LOOP_END
        
        
        
            // Normalize total lighting
        
            //Color = MainColor * Color;
        
        float totalWeight = Diffuse + dot(Specular, float3(0.333, 0.333, 0.333));
        
            Color = totalWeight > 0 ? MainColor * (Color / totalWeight) : MainColor;
        
        
        
        #endif
        }
        // unity-custom-func-end
        
        struct Bindings_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float
        {
        float3 WorldSpacePosition;
        };
        
        void SG_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float(float _MainLightDiffuse, float3 _MainLightSpecular, float3 _MainLightColor, float3 _NormalWS, float _Smoothness, Bindings_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float IN, out float Diffuse_1, out float3 Specular_2, out float3 Color_3)
        {
        float _Property_f5ac72f32dc84ae988391b7d51172380_Out_0_Float = _Smoothness;
        float3 _Property_85d85c5164d5483787fecbcecfd6a519_Out_0_Vector3 = _NormalWS;
        float3 _ViewVector_0d331f2b495741bcb8795caa9549f016_Out_0_Vector3;
        Unity_ViewVectorWorld_float(_ViewVector_0d331f2b495741bcb8795caa9549f016_Out_0_Vector3, IN.WorldSpacePosition);
        float _Property_51c13cc19f414dacb86745cec42d9ab6_Out_0_Float = _MainLightDiffuse;
        float3 _Property_f1e96792c3cc4a1e90396147ef9f7160_Out_0_Vector3 = _MainLightColor;
        float _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Diffuse_7_Float;
        float3 _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Specular_8_Vector3;
        float3 _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Color_9_Vector3;
        AddAdditionalLights_float(_Property_f5ac72f32dc84ae988391b7d51172380_Out_0_Float, IN.WorldSpacePosition, _Property_85d85c5164d5483787fecbcecfd6a519_Out_0_Vector3, _ViewVector_0d331f2b495741bcb8795caa9549f016_Out_0_Vector3, _Property_51c13cc19f414dacb86745cec42d9ab6_Out_0_Float, _Property_f1e96792c3cc4a1e90396147ef9f7160_Out_0_Vector3, _Property_f1e96792c3cc4a1e90396147ef9f7160_Out_0_Vector3, _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Diffuse_7_Float, _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Specular_8_Vector3, _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Color_9_Vector3);
        Diffuse_1 = _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Diffuse_7_Float;
        Specular_2 = _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Specular_8_Vector3;
        Color_3 = _AddAdditionalLightsCustomFunction_68a2b746307e4363bdcaa408867c07c4_Color_9_Vector3;
        }
        
        void Unity_Posterize_float3(float3 In, float3 Steps, out float3 Out)
        {
            Out = floor(In / (1 / Steps)) * (1 / Steps);
        }
        
        void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
        {
            float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
            Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
        }
        
        // unity-custom-func-begin
        void GetSSAO_float(float2 ScreenPos, out float DirectAO, out float IndirectAO){
         #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT) && !defined(SHADERGRAPH_PREVIEW)
        
         
          float ssao = saturate(SampleAmbientOcclusion(ScreenPos) + (1.0 - _AmbientOcclusionParam.x));
        
           IndirectAO = ssao;
        
           DirectAO = lerp(1.0, ssao, _AmbientOcclusionParam.w);
        
        #else
        
           DirectAO = 1.0;
        
           IndirectAO = 1.0;
        
        #endif
        }
        // unity-custom-func-end
        
        struct Bindings_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float
        {
        float2 NDCPosition;
        };
        
        void SG_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float(float2 _ScreenPos, bool _ScreenPos_72149ac21c784cc8b4614750ec683413_IsConnected, Bindings_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float IN, out float DirectAO_1, out float IndirectAO_2)
        {
        float2 _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2 = _ScreenPos;
        bool _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2_IsConnected = _ScreenPos_72149ac21c784cc8b4614750ec683413_IsConnected;
        float4 _ScreenPosition_a193bed040594170a08ae04fed7ea8a7_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
        float2 _BranchOnInputConnection_8e8119e37af64e9a957c820002cea48c_Out_3_Vector2 = _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2_IsConnected ? _Property_0e17a591b92f46f0870fe11a1bcaa527_Out_0_Vector2 : (_ScreenPosition_a193bed040594170a08ae04fed7ea8a7_Out_0_Vector4.xy);
        float _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_DirectAO_1_Float;
        float _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_IndirectAO_2_Float;
        GetSSAO_float(_BranchOnInputConnection_8e8119e37af64e9a957c820002cea48c_Out_3_Vector2, _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_DirectAO_1_Float, _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_IndirectAO_2_Float);
        DirectAO_1 = _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_DirectAO_1_Float;
        IndirectAO_2 = _GetSSAOCustomFunction_f7b0f720c4974e319d6e2514d5fe8fe1_IndirectAO_2_Float;
        }
        
        void Unity_Minimum_float(float A, float B, out float Out)
        {
            Out = min(A, B);
        };
        
        void Unity_ReflectionProbe_float(float3 ViewDir, float3 Normal, float LOD, out float3 Out)
        {
            Out = SHADERGRAPH_REFLECTION_PROBE(ViewDir, Normal, LOD);
        }
        
        struct Bindings_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        float3 WorldSpacePosition;
        float2 NDCPosition;
        float2 PixelPosition;
        half4 uv1;
        half4 uv2;
        };
        
        void SG_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float(float3 _Base_Color, bool _Base_Color_87794c9dccf9430d8ee1585f52796223_IsConnected, float3 _NormalWS, bool _NormalWS_d8d26d45dd0b4d4a8e8426edcd206b0f_IsConnected, float _AO, float _Smoothness, float _Metallic, float _Reflectance, Bindings_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float IN, out float3 Ambient_1, out float3 Metallic_2, out float DdirectAO_3)
        {
        float3 _Property_50e51e65f64b40c29fa7b10305f0a7b4_Out_0_Vector3 = _Base_Color;
        bool _Property_50e51e65f64b40c29fa7b10305f0a7b4_Out_0_Vector3_IsConnected = _Base_Color_87794c9dccf9430d8ee1585f52796223_IsConnected;
        float _Property_b967758667b74ff5acb2a8af3ff58c39_Out_0_Float = _Metallic;
        float3 _Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3;
        Unity_Lerp_float3(_Property_50e51e65f64b40c29fa7b10305f0a7b4_Out_0_Vector3, float3(0, 0, 0), (_Property_b967758667b74ff5acb2a8af3ff58c39_Out_0_Float.xxx), _Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3);
        float3 _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3 = _NormalWS;
        bool _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3_IsConnected = _NormalWS_d8d26d45dd0b4d4a8e8426edcd206b0f_IsConnected;
        float3 _BranchOnInputConnection_dae192f178d24a5a9c9c540fd5488461_Out_3_Vector3 = _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3_IsConnected ? _Property_1dc31d87930641b7a587de8918dcdfa9_Out_0_Vector3 : IN.WorldSpaceNormal;
        float3 _BakedGI_1e2ac029f59d497abdc2a7bcf1eb3346_Out_1_Vector3 = SHADERGRAPH_BAKED_GI(IN.WorldSpacePosition, _BranchOnInputConnection_dae192f178d24a5a9c9c540fd5488461_Out_3_Vector3, IN.PixelPosition.xy, IN.uv1.xy, IN.uv2.xy, true);
        float _Property_26a27af7a1144ea0a77aad0b49f12697_Out_0_Float = _AO;
        Bindings_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float _SSAO_f6107fe543294e7f932b8c5cfc86bdd5;
        _SSAO_f6107fe543294e7f932b8c5cfc86bdd5.NDCPosition = IN.NDCPosition;
        half _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_DirectAO_1_Float;
        half _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_IndirectAO_2_Float;
        SG_SSAO_b5549489ce8bf6740b7f752382fcc1fb_float(half2 (0, 0), false, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_DirectAO_1_Float, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_IndirectAO_2_Float);
        float _Minimum_3ac81a32823a439c8b51e89edf949c26_Out_2_Float;
        Unity_Minimum_float(_Property_26a27af7a1144ea0a77aad0b49f12697_Out_0_Float, _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_IndirectAO_2_Float, _Minimum_3ac81a32823a439c8b51e89edf949c26_Out_2_Float);
        float3 _Multiply_423df8bc24d94add8f44f6db0bf46798_Out_2_Vector3;
        Unity_Multiply_float3_float3(_BakedGI_1e2ac029f59d497abdc2a7bcf1eb3346_Out_1_Vector3, (_Minimum_3ac81a32823a439c8b51e89edf949c26_Out_2_Float.xxx), _Multiply_423df8bc24d94add8f44f6db0bf46798_Out_2_Vector3);
        float3 _Multiply_4ee1636476de4393a1b2e9162c2328b9_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3, _Multiply_423df8bc24d94add8f44f6db0bf46798_Out_2_Vector3, _Multiply_4ee1636476de4393a1b2e9162c2328b9_Out_2_Vector3);
        float _Property_76583d84384b44cbb79843c917044c1b_Out_0_Float = _Reflectance;
        float _Property_f511aceb4377409690a68e8d8a16a032_Out_0_Float = _Smoothness;
        float _Lerp_496af92f7e424d5586ca49ffe949aff6_Out_3_Float;
        Unity_Lerp_float(float(8), float(0), _Property_f511aceb4377409690a68e8d8a16a032_Out_0_Float, _Lerp_496af92f7e424d5586ca49ffe949aff6_Out_3_Float);
        float3 _ReflectionProbe_d9eb69c9b54641068d948dd37fec342f_Out_3_Vector3;
        Unity_ReflectionProbe_float(IN.WorldSpaceViewDirection, _BranchOnInputConnection_dae192f178d24a5a9c9c540fd5488461_Out_3_Vector3, _Lerp_496af92f7e424d5586ca49ffe949aff6_Out_3_Float, _ReflectionProbe_d9eb69c9b54641068d948dd37fec342f_Out_3_Vector3);
        float3 _Multiply_25ab7f2171954fc4a710fe8c8c2cc402_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Property_76583d84384b44cbb79843c917044c1b_Out_0_Float.xxx), _ReflectionProbe_d9eb69c9b54641068d948dd37fec342f_Out_3_Vector3, _Multiply_25ab7f2171954fc4a710fe8c8c2cc402_Out_2_Vector3);
        float3 _Add_475a3a92ac834f51b869dedfbba66e44_Out_2_Vector3;
        Unity_Add_float3(_Multiply_4ee1636476de4393a1b2e9162c2328b9_Out_2_Vector3, _Multiply_25ab7f2171954fc4a710fe8c8c2cc402_Out_2_Vector3, _Add_475a3a92ac834f51b869dedfbba66e44_Out_2_Vector3);
        Ambient_1 = _Add_475a3a92ac834f51b869dedfbba66e44_Out_2_Vector3;
        Metallic_2 = _Lerp_6621b72d3a0d48cd85570fa80cb5e267_Out_3_Vector3;
        DdirectAO_3 = _SSAO_f6107fe543294e7f932b8c5cfc86bdd5_DirectAO_1_Float;
        }
        
        void Unity_Saturate_float3(float3 In, out float3 Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Posterize_float(float In, float Steps, out float Out)
        {
            Out = floor(In / (1 / Steps)) * (1 / Steps);
        }
        
        struct Bindings_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float
        {
        float3 WorldSpaceNormal;
        float3 WorldSpaceViewDirection;
        float3 WorldSpacePosition;
        float2 NDCPosition;
        float2 PixelPosition;
        half4 uv1;
        half4 uv2;
        };
        
        void SG_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float(float3 _Base_Color, bool _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected, float3 _NormalWS, bool _NormalWS_d2c1afa83f4f4d20ac34f445f069483d_IsConnected, float _Metallic, float _Smoothness, float _AO, float _Steps, float _Specular_Steps, float _Smoothness_Strength, Bindings_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float IN, out float3 Lit_1)
        {
        float3 _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3 = _NormalWS;
        bool _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3_IsConnected = _NormalWS_d2c1afa83f4f4d20ac34f445f069483d_IsConnected;
        float3 _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3 = _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3_IsConnected ? _Property_7b79357aace04012b6f221a6b12e5287_Out_0_Vector3 : IN.WorldSpaceNormal;
        Bindings_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91;
        _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91.WorldSpaceNormal = IN.WorldSpaceNormal;
        half _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float;
        SG_HalfLambertDiffuse_fb0b4afde5bb4134386364d51233fa2d_float(_BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91, _HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float);
        float _Property_af2c0fb6df7d4dc9abade557540f3ed7_Out_0_Float = _Smoothness;
        Bindings_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e;
        half _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float;
        SG_AdjustSmoothness_9f882e388fcb860498458b5ae7bfb8ee_float(_Property_af2c0fb6df7d4dc9abade557540f3ed7_Out_0_Float, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float);
        float3 _Property_b3987ddafd854a0a85863bc89dcde720_Out_0_Vector3 = _Base_Color;
        bool _Property_b3987ddafd854a0a85863bc89dcde720_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float _Property_3c53a42fedba46ba9fae794424fbe85e_Out_0_Float = _Metallic;
        Bindings_Reflectance_3dd134828a90d7446802ac63dbc0221d_float _Reflectance_0d5aeeef450744cf885ab04f87797c7b;
        _Reflectance_0d5aeeef450744cf885ab04f87797c7b.WorldSpaceNormal = IN.WorldSpaceNormal;
        _Reflectance_0d5aeeef450744cf885ab04f87797c7b.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3;
        SG_Reflectance_3dd134828a90d7446802ac63dbc0221d_float(_Property_b3987ddafd854a0a85863bc89dcde720_Out_0_Vector3, true, _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _Property_3c53a42fedba46ba9fae794424fbe85e_Out_0_Float, _Reflectance_0d5aeeef450744cf885ab04f87797c7b, _Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3);
        Bindings_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8;
        _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8.WorldSpaceNormal = IN.WorldSpaceNormal;
        _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        half3 _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8_Specular_1_Vector3;
        SG_SpecularBlinn_cf4b70d1df4a0de44b33d3283a02126d_float(_BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float, _Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3, _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8, _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8_Specular_1_Vector3);
        float3 _Multiply_577b66af727b4e43be242c88de8def99_Out_2_Vector3;
        Unity_Multiply_float3_float3((_HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float.xxx), _SpecularBlinn_23ffe2dd42fb4639b212b0bf4acb67e8_Specular_1_Vector3, _Multiply_577b66af727b4e43be242c88de8def99_Out_2_Vector3);
        Bindings_MainLight_0157caa4ea90d88499c9fdb016582871_float _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a;
        _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a.WorldSpacePosition = IN.WorldSpacePosition;
        float3 _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Direction_1_Vector3;
        float3 _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Color_2_Vector3;
        float _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_ShadowAtt_3_Float;
        SG_MainLight_0157caa4ea90d88499c9fdb016582871_float(_MainLight_1fffd9ee56744cde8cd3210b8fc0a55a, _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Direction_1_Vector3, _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Color_2_Vector3, _MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_ShadowAtt_3_Float);
        float3 _Multiply_e3a50f2fbf214e64931956c0b1e7f194_Out_2_Vector3;
        Unity_Multiply_float3_float3(_MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_Color_2_Vector3, (_MainLight_1fffd9ee56744cde8cd3210b8fc0a55a_ShadowAtt_3_Float.xxx), _Multiply_e3a50f2fbf214e64931956c0b1e7f194_Out_2_Vector3);
        Bindings_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021;
        _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021.WorldSpacePosition = IN.WorldSpacePosition;
        float _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Diffuse_1_Float;
        float3 _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Specular_2_Vector3;
        float3 _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3;
        SG_AdditionalLightsHlafLambert_8e4f8d42ce41b4f4a839ecad13527f75_float(_HalfLambertDiffuse_bea0a2eaac6d41dfac83e3c42cec7e91_Diffuse_1_Float, _Multiply_577b66af727b4e43be242c88de8def99_Out_2_Vector3, _Multiply_e3a50f2fbf214e64931956c0b1e7f194_Out_2_Vector3, _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, _AdjustSmoothness_7175e7b5aca64f0e9cf428069e36453e_Out_1_Float, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Diffuse_1_Float, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Specular_2_Vector3, _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3);
        float _Property_7fabb829117947b8956799cbdf3be9e0_Out_0_Float = _Steps;
        float3 _Posterize_8f26285a65784a038d6d9d2087181b46_Out_2_Vector3;
        Unity_Posterize_float3(_AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, (_Property_7fabb829117947b8956799cbdf3be9e0_Out_0_Float.xxx), _Posterize_8f26285a65784a038d6d9d2087181b46_Out_2_Vector3);
        float3 _Property_3668f71779994b3a9fcc7095f880cb12_Out_0_Vector3 = _Base_Color;
        bool _Property_3668f71779994b3a9fcc7095f880cb12_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float3 _Saturation_e12bfe3d861c4f1fa4c1297d0dd08cc0_Out_2_Vector3;
        Unity_Saturation_float(_Property_3668f71779994b3a9fcc7095f880cb12_Out_0_Vector3, float(1.6), _Saturation_e12bfe3d861c4f1fa4c1297d0dd08cc0_Out_2_Vector3);
        float3 _Multiply_77c53f8596dd487fa3009d208e99c0fd_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Saturation_e12bfe3d861c4f1fa4c1297d0dd08cc0_Out_2_Vector3, float3(0.1, 0.1, 0.1), _Multiply_77c53f8596dd487fa3009d208e99c0fd_Out_2_Vector3);
        float3 _Property_494170212d604ba0a52a5b39f34e4c3f_Out_0_Vector3 = _Base_Color;
        bool _Property_494170212d604ba0a52a5b39f34e4c3f_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float3 _Multiply_e22a63bb0da045618a0202e45051407e_Out_2_Vector3;
        Unity_Multiply_float3_float3((_AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Diffuse_1_Float.xxx), _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, _Multiply_e22a63bb0da045618a0202e45051407e_Out_2_Vector3);
        float3 _Property_53d2f61f22684efca5bbd024184799a4_Out_0_Vector3 = _Base_Color;
        bool _Property_53d2f61f22684efca5bbd024184799a4_Out_0_Vector3_IsConnected = _Base_Color_1f5ee893cff94e49a35e97868e0d8ebd_IsConnected;
        float _Property_5204695d7d6d4d03b28eba481d372192_Out_0_Float = _AO;
        float _Property_aa169c45708b4343b76a3b52b483b8e2_Out_0_Float = _Smoothness;
        float _Property_1d116579c59d45ccaa1bdcb07a5ec5d5_Out_0_Float = _Metallic;
        Bindings_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.WorldSpaceNormal = IN.WorldSpaceNormal;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.WorldSpacePosition = IN.WorldSpacePosition;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.NDCPosition = IN.NDCPosition;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.PixelPosition = IN.PixelPosition;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.uv1 = IN.uv1;
        _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997.uv2 = IN.uv2;
        half3 _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Ambient_1_Vector3;
        half3 _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Metallic_2_Vector3;
        half _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_DdirectAO_3_Float;
        SG_AmbientSimpleSSAO_e15b8d6996acfc8419c25f0aec7c1c9f_float(_Property_53d2f61f22684efca5bbd024184799a4_Out_0_Vector3, true, _BranchOnInputConnection_9d194c76947348ccba5affc007a3f973_Out_3_Vector3, true, _Property_5204695d7d6d4d03b28eba481d372192_Out_0_Float, _Property_aa169c45708b4343b76a3b52b483b8e2_Out_0_Float, _Property_1d116579c59d45ccaa1bdcb07a5ec5d5_Out_0_Float, (_Reflectance_0d5aeeef450744cf885ab04f87797c7b_Reflectance_1_Vector3).x, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Ambient_1_Vector3, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Metallic_2_Vector3, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_DdirectAO_3_Float);
        float3 _Add_96c33650335248caa7429346ca897bb7_Out_2_Vector3;
        Unity_Add_float3(_Multiply_e22a63bb0da045618a0202e45051407e_Out_2_Vector3, _AmbientSimpleSSAO_676d4ce4d6a3435f8f4000cf1e690997_Ambient_1_Vector3, _Add_96c33650335248caa7429346ca897bb7_Out_2_Vector3);
        float3 _Saturate_b993ba602dea4759baa643682158093c_Out_1_Vector3;
        Unity_Saturate_float3(_Add_96c33650335248caa7429346ca897bb7_Out_2_Vector3, _Saturate_b993ba602dea4759baa643682158093c_Out_1_Vector3);
        float3 _Saturation_559bfc31eca847d3acd46b2fd284dc13_Out_2_Vector3;
        Unity_Saturation_float(_Saturate_b993ba602dea4759baa643682158093c_Out_1_Vector3, float(1), _Saturation_559bfc31eca847d3acd46b2fd284dc13_Out_2_Vector3);
        float _Swizzle_3952ca1d9d274331b6a4a49630c606ae_Out_1_Float = _Saturation_559bfc31eca847d3acd46b2fd284dc13_Out_2_Vector3.x;
        float _Posterize_268e85bcc1d5424485e214740b6468e3_Out_2_Float;
        Unity_Posterize_float(_Swizzle_3952ca1d9d274331b6a4a49630c606ae_Out_1_Float, _Property_7fabb829117947b8956799cbdf3be9e0_Out_0_Float, _Posterize_268e85bcc1d5424485e214740b6468e3_Out_2_Float);
        float3 _Lerp_8c017a3a82434ee38c4432bf457e582b_Out_3_Vector3;
        Unity_Lerp_float3(_Multiply_77c53f8596dd487fa3009d208e99c0fd_Out_2_Vector3, _Property_494170212d604ba0a52a5b39f34e4c3f_Out_0_Vector3, (_Posterize_268e85bcc1d5424485e214740b6468e3_Out_2_Float.xxx), _Lerp_8c017a3a82434ee38c4432bf457e582b_Out_3_Vector3);
        float3 _Multiply_5c248bf4893d4acd9b10421fae8923d1_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Posterize_8f26285a65784a038d6d9d2087181b46_Out_2_Vector3, _Lerp_8c017a3a82434ee38c4432bf457e582b_Out_3_Vector3, _Multiply_5c248bf4893d4acd9b10421fae8923d1_Out_2_Vector3);
        float _Property_48b9a97103af41f3b6cafeceb8a6a0b7_Out_0_Float = _Smoothness_Strength;
        float3 _Add_57faecf3e31346a68db61c3e666b2975_Out_2_Vector3;
        Unity_Add_float3((_Property_48b9a97103af41f3b6cafeceb8a6a0b7_Out_0_Float.xxx), _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, _Add_57faecf3e31346a68db61c3e666b2975_Out_2_Vector3);
        float3 _Multiply_b90f73fc105c4da3b7586428b54d0c03_Out_2_Vector3;
        Unity_Multiply_float3_float3(_AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Specular_2_Vector3, _Add_57faecf3e31346a68db61c3e666b2975_Out_2_Vector3, _Multiply_b90f73fc105c4da3b7586428b54d0c03_Out_2_Vector3);
        float3 _Saturation_c81f01b7b0de4608a25fde9890c55880_Out_2_Vector3;
        Unity_Saturation_float(_Multiply_b90f73fc105c4da3b7586428b54d0c03_Out_2_Vector3, float(0), _Saturation_c81f01b7b0de4608a25fde9890c55880_Out_2_Vector3);
        float _Swizzle_fbfd922c5f7d42d399186d59a86d5660_Out_1_Float = _Saturation_c81f01b7b0de4608a25fde9890c55880_Out_2_Vector3.x;
        float _Property_62c311d0a3534ad1ad3a7b66c978a36c_Out_0_Float = _Specular_Steps;
        float _Posterize_1027f91210d94108ae1c593d0fb61d8b_Out_2_Float;
        Unity_Posterize_float(_Swizzle_fbfd922c5f7d42d399186d59a86d5660_Out_1_Float, _Property_62c311d0a3534ad1ad3a7b66c978a36c_Out_0_Float, _Posterize_1027f91210d94108ae1c593d0fb61d8b_Out_2_Float);
        float3 _Multiply_c92faf2f8cc04ff29ec2ba172ebefa59_Out_2_Vector3;
        Unity_Multiply_float3_float3((_Posterize_1027f91210d94108ae1c593d0fb61d8b_Out_2_Float.xxx), _AdditionalLightsHlafLambert_6150c22bf22345b0a4f5b41cc1344021_Color_3_Vector3, _Multiply_c92faf2f8cc04ff29ec2ba172ebefa59_Out_2_Vector3);
        float3 _Add_321e04b0f3bb47558352363c94828b10_Out_2_Vector3;
        Unity_Add_float3(_Multiply_5c248bf4893d4acd9b10421fae8923d1_Out_2_Vector3, _Multiply_c92faf2f8cc04ff29ec2ba172ebefa59_Out_2_Vector3, _Add_321e04b0f3bb47558352363c94828b10_Out_2_Vector3);
        Lit_1 = _Add_321e04b0f3bb47558352363c94828b10_Out_2_Vector3;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_BaseMap);
            float4 _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D.tex, _Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D.samplerstate, _Property_65d9b1c2fa404607b748169dbfb823b9_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_R_4_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.r;
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_G_5_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.g;
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_B_6_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.b;
            float _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_A_7_Float = _SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.a;
            float _Property_7ab914554b2b477db515a77874d1d4cd_Out_0_Float = _LightingSmoothness;
            float _Property_9e7bc9fe055c4e4db532299787c0e3ed_Out_0_Float = _Steps;
            float _Property_9d34386f48e54d9eaeda9d04da1ad72f_Out_0_Float = _SpecularSteps;
            Bindings_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.WorldSpaceNormal = IN.WorldSpaceNormal;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.WorldSpacePosition = IN.WorldSpacePosition;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.NDCPosition = IN.NDCPosition;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.PixelPosition = IN.PixelPosition;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.uv1 = IN.uv1;
            _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3.uv2 = IN.uv2;
            float3 _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3_Lit_1_Vector3;
            SG_ToonLightingSubGraph1_f2eef0b58fa207e4390601f615ce5bb2_float((_SampleTexture2D_c3801982ccd4459eb23f3b72f4b84af6_RGBA_0_Vector4.xyz), true, float3 (0, 0, 0), false, float(0), _Property_7ab914554b2b477db515a77874d1d4cd_Out_0_Float, float(0), _Property_9e7bc9fe055c4e4db532299787c0e3ed_Out_0_Float, _Property_9d34386f48e54d9eaeda9d04da1ad72f_Out_0_Float, float(0), _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3, _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3_Lit_1_Vector3);
            UnityTexture2D _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_AlphaMap);
            float4 _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.tex, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.samplerstate, _Property_89b3281d204e4a3d9030939074c6f64d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_R_4_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.r;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_G_5_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.g;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_B_6_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.b;
            float _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_RGBA_0_Vector4.a;
            surface.BaseColor = _ToonLightingSubGraph1_3b966c2e595442f6910f436690a6e1a3_Lit_1_Vector3;
            surface.Alpha = float(1);
            surface.AlphaClipThreshold = _SampleTexture2D_99de4c6c814142809cd31cfefbf0e556_A_7_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
            // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            float3 unnormalizedNormalWS = input.normalWS;
            const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        
        
            output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
        
        
            output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
            output.WorldSpacePosition = input.positionWS;
        
            #if UNITY_UV_STARTS_AT_TOP
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #else
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #endif
        
            output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
            output.NDCPosition.y = 1.0f - output.NDCPosition.y;
        
            output.uv0 = input.texCoord0;
            output.uv1 = input.texCoord1;
            output.uv2 = input.texCoord2;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
    }
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
    CustomEditorForRenderPipeline "UnityEditor.ShaderGraphLitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
    FallBack "Hidden/Shader Graph/FallbackError"
}