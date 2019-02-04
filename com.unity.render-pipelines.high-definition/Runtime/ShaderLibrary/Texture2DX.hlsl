#ifndef UNITY_TEXTURE2DX_INCLUDED
#define UNITY_TEXTURE2DX_INCLUDED

// XRTODO: refactor this with UnityInstancing.hlsl and sync with LWRP
// XRTODO: update supported platforms based on Unity version (for required C++ fixes)

// Must be in sync with C# with property useTexArray in Texture2DX.cs
#if defined(SHADER_API_D3D11)
    // XRTODO: disabled until all SPI code is merged
    //#define UNITY_TEXTURE2DX_AS_ARRAY_SUPPORTED
#endif

// Validate supported platforms
#if defined(STEREO_INSTANCING_ON) && !defined(UNITY_TEXTURE2DX_AS_ARRAY_SUPPORTED)
    #error Single-pass stereo instancing is not supported on this platform (see UNITY_TEXTURE2DX_AS_ARRAY_SUPPORTED).
#endif

// Control if TEXTURE2DX* macros will expand to texture arrays
#if defined(UNITY_TEXTURE2DX_AS_ARRAY_SUPPORTED) && !defined(FORCE_NO_TEXTURE2DX_ARRAY)
    #define USE_TEXTURE2DX_AS_ARRAY
#endif

// Early defines for single-pass stereo instancing
#if defined(STEREO_INSTANCING_ON) && defined(UNITY_TEXTURE2DX_AS_ARRAY_SUPPORTED)
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

// Workaround for lack of multi compile in compute shaders
#if defined(SHADER_STAGE_COMPUTE) && defined(UNITY_TEXTURE2DX_AS_ARRAY_SUPPORTED)
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

// Define to override default rendering matrices (used mostly in ShaderVariables.hlsl)
#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED)
    #define USING_STEREO_MATRICES
#endif

// Helper macros to handle XR instancing with Texture2DArray
// Render textures allocated with the flag 'xrInstancing' used Texture2DArray where each slice is associated to an eye.
// With single-pass stereo instancing, unity_StereoEyeIndex is used to select the eye in the current context.
// Otherwise, the index is statically set to 0
#if defined(USE_TEXTURE2DX_AS_ARRAY)

    // Only single-pass stereo instancing used array indexing
    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
        #define SLICE_ARRAY_INDEX   unity_StereoEyeIndex
    #else
        #define SLICE_ARRAY_INDEX  0
    #endif

    #define TEXTURE2DX                                                      TEXTURE2D_ARRAY
    #define TEXTURE2DX_PARAM                                                TEXTURE2D_ARRAY_PARAM
    #define TEXTURE2DX_ARGS                                                 TEXTURE2D_ARRAY_ARGS
    #define TEXTURE2DX_HALF                                                 TEXTURE2D_ARRAY_HALF
    #define TEXTURE2DX_FLOAT                                                TEXTURE2D_ARRAY_FLOAT
    #define TEXTURE2DX_UINT(textureName)                                    Texture2DArray<uint> textureName
    #define TEXTURE2DX_MSAA(type, textureName)                              Texture2DMSArray<type> textureName

    #define RW_TEXTURE2DX(type, textureName)                                RW_TEXTURE2D_ARRAY(type, textureName)
    #define COORD_TEXTURE2DX(pixelCoord)                                    uint3(pixelCoord, SLICE_ARRAY_INDEX)
    #define LOAD_TEXTURE2DX(textureName, unCoord2)                          LOAD_TEXTURE2D_ARRAY(textureName, unCoord2, SLICE_ARRAY_INDEX)
    #define LOAD_TEXTURE2DX_MSAA(textureName, unCoord2, sampleIndex)        LOAD_TEXTURE2D_ARRAY_MSAA(textureName, unCoord2, SLICE_ARRAY_INDEX, sampleIndex)
    #define LOAD_TEXTURE2DX_LOD(textureName, unCoord2, lod)                 LOAD_TEXTURE2D_ARRAY_LOD(textureName, unCoord2, SLICE_ARRAY_INDEX, lod)
    #define SAMPLE_TEXTURE2DX(textureName, samplerName, coord2)             SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, SLICE_ARRAY_INDEX)
    #define SAMPLE_TEXTURE2DX_LOD(textureName, samplerName, coord2, lod)    SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, SLICE_ARRAY_INDEX, lod)
    #define GATHER_TEXTURE2DX(textureName, samplerName, coord2)             GATHER_TEXTURE2D_ARRAY(textureName, samplerName, coord2, SLICE_ARRAY_INDEX)
    #define GATHER_RED_TEXTURE2DX(textureName, samplerName, coord2)         GATHER_RED_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
    #define GATHER_GREEN_TEXTURE2DX(textureName, samplerName, coord2)       GATHER_GREEN_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
#else
    #define TEXTURE2DX                                                      TEXTURE2D
    #define TEXTURE2DX_PARAM                                                TEXTURE2D_PARAM
    #define TEXTURE2DX_ARGS                                                 TEXTURE2D_ARGS
    #define TEXTURE2DX_HALF                                                 TEXTURE2D_HALF
    #define TEXTURE2DX_FLOAT                                                TEXTURE2D_FLOAT
    #define TEXTURE2DX_UINT(textureName)                                    Texture2D<uint> textureName
    #define TEXTURE2DX_MSAA(type, textureName)                              Texture2DMS<type> textureName

    #define RW_TEXTURE2DX                                                   RW_TEXTURE2D
    #define COORD_TEXTURE2DX(pixelCoord)                                    pixelCoord
    #define LOAD_TEXTURE2DX                                                 LOAD_TEXTURE2D
    #define LOAD_TEXTURE2DX_MSAA                                            LOAD_TEXTURE2D_MSAA
    #define LOAD_TEXTURE2DX_LOD                                             LOAD_TEXTURE2D_LOD
    #define SAMPLE_TEXTURE2DX                                               SAMPLE_TEXTURE2D
    #define SAMPLE_TEXTURE2DX_LOD                                           SAMPLE_TEXTURE2D_LOD
    #define GATHER_TEXTURE2DX                                               GATHER_TEXTURE2D
    #define GATHER_RED_TEXTURE2DX                                           GATHER_RED_TEXTURE2D
    #define GATHER_GREEN_TEXTURE2DX                                         GATHER_GREEN_TEXTURE2D
#endif

// Notes on current stereo support status
// single-pass doule-wide is the only working at the moment
// single-pass instancing is in progress
// multi-view and multi-pass are not supported
// see Unity\Shaders\Includes\UnityShaderVariables.cginc for impl used by the C++ renderer
#if defined(USING_STEREO_MATRICES)

    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
        static uint unity_StereoEyeIndex;
    #elif defined(UNITY_SINGLE_PASS_STEREO) // XRTODO: remove once SinglePassInstanced is working
        #if SHADER_STAGE_COMPUTE
        #else
            CBUFFER_START(UnityStereoEyeIndex)
                int unity_StereoEyeIndex;
            CBUFFER_END
        #endif
    #endif

#else
    #define unity_StereoEyeIndex 0
#endif

// Helper macro to assign eye index during compute pass (usually from SV_DispatchThreadID)
#if defined(SHADER_STAGE_COMPUTE)
    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
        #define UNITY_STEREO_ASSIGN_COMPUTE_EYE_INDEX(eyeIndex) unity_StereoEyeIndex = eyeIndex;
    #else
        #define UNITY_STEREO_ASSIGN_COMPUTE_EYE_INDEX(eyeIndex)
    #endif
#endif

#endif // UNITY_TEXTURE2DX_INCLUDED
