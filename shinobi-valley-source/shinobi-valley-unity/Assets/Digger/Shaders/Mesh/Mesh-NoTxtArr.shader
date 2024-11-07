Shader "Nature/Terrain/Digger/Mesh-Standard-NoTxtArr" {
    Properties {
    
        // set by digger engine
        _Splat0 ("Layer 0", 2D) = "white" {}
        _Splat1 ("Layer 1", 2D) = "white" {}
        _Splat2 ("Layer 2", 2D) = "white" {}
        _Splat3 ("Layer 3", 2D) = "white" {}
        _Splat4 ("Layer 4", 2D) = "white" {}
        _Splat5 ("Layer 5", 2D) = "white" {}
        
        _Normal0 ("Normal 0", 2D) = "bump" {}
        _Normal1 ("Normal 1", 2D) = "bump" {}
        _Normal2 ("Normal 2", 2D) = "bump" {}
        _Normal3 ("Normal 3", 2D) = "bump" {}
        _Normal4 ("Normal 4", 2D) = "bump" {}
        _Normal5 ("Normal 5", 2D) = "bump" {}
        
        [Gamma] _Metallic0 ("Metallic 0", Range(0.0, 1.0)) = 0.0
        _Smoothness0 ("Smoothness 0", Range(0.0, 1.0)) = 0.0
        
        [Gamma] _Metallic1 ("Metallic 1", Range(0.0, 1.0)) = 0.0
        _Smoothness1 ("Smoothness 1", Range(0.0, 1.0)) = 0.0
        
        [Gamma] _Metallic2 ("Metallic 2", Range(0.0, 1.0)) = 0.0
        _Smoothness2 ("Smoothness 2", Range(0.0, 1.0)) = 0.0
        
        [Gamma] _Metallic3 ("Metallic 3", Range(0.0, 1.0)) = 0.0
        _Smoothness3 ("Smoothness 3", Range(0.0, 1.0)) = 0.0
        
        [Gamma] _Metallic4 ("Metallic 4", Range(0.0, 1.0)) = 0.0
        _Smoothness4 ("Smoothness 4", Range(0.0, 1.0)) = 0.0
        
        [Gamma] _Metallic5 ("Metallic 5", Range(0.0, 1.0)) = 0.0
        _Smoothness5 ("Smoothness 5", Range(0.0, 1.0)) = 0.0
        
        [Gamma] _Metallic6 ("Metallic 6", Range(0.0, 1.0)) = 0.0
        _Smoothness6 ("Smoothness 6", Range(0.0, 1.0)) = 0.0
        
        [Gamma] _Metallic7 ("Metallic 7", Range(0.0, 1.0)) = 0.0
        _Smoothness7 ("Smoothness 7", Range(0.0, 1.0)) = 0.0
      
        
        // used in fallback on old cards & base map
        _MainTex ("BaseMap (RGB)", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        
        _tiles0x ("tile0X", float) = 0.03
        _tiles0y ("tile0Y", float) = 0.03
        _tiles1x ("tile1X", float) = 0.03
        _tiles1y ("tile1Y", float) = 0.03
        _tiles2x ("tile2X", float) = 0.03
        _tiles2y ("tile2Y", float) = 0.03
        _tiles3x ("tile3X", float) = 0.03
        _tiles3y ("tile3Y", float) = 0.03
        _tiles4x ("tile4X", float) = 0.03
        _tiles4y ("tile4Y", float) = 0.03
        _tiles5x ("tile5X", float) = 0.03
        _tiles5y ("tile5Y", float) = 0.03
        [HideInInspector] _offset0x ("offset0X", float) = 0
        [HideInInspector] _offset0y ("offset0Y", float) = 0
        [HideInInspector] _offset1x ("offset1X", float) = 0
        [HideInInspector] _offset1y ("offset1Y", float) = 0
        [HideInInspector] _offset2x ("offset2X", float) = 0
        [HideInInspector] _offset2y ("offset2Y", float) = 0
        [HideInInspector] _offset3x ("offset3X", float) = 0
        [HideInInspector] _offset3y ("offset3Y", float) = 0
        [HideInInspector] _offset4x ("offset4X", float) = 0
        [HideInInspector] _offset4y ("offset4Y", float) = 0
        [HideInInspector] _offset5x ("offset5X", float) = 0
        [HideInInspector] _offset5y ("offset5Y", float) = 0

        [HideInInspector] _normalScale0 ("normalScale0", float) = 1
        [HideInInspector] _normalScale1 ("normalScale1", float) = 1
        [HideInInspector] _normalScale2 ("normalScale2", float) = 1
        [HideInInspector] _normalScale3 ("normalScale3", float) = 1
        [HideInInspector] _normalScale4 ("normalScale4", float) = 1
        [HideInInspector] _normalScale5 ("normalScale5", float) = 1
    }

    SubShader {
        Tags {
            "Queue" = "Geometry-101"
            "RenderType" = "Opaque"
        }

        CGPROGRAM
        #pragma surface surf Standard vertex:SplatmapVert finalcolor:SplatmapFinalColor finalgbuffer:SplatmapFinalGBuffer addshadow fullforwardshadows
        #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap forwardadd
        #pragma multi_compile_fog // needed because finalcolor oppresses fog code generation.
        #pragma target 3.0
        // needs more than 8 texcoords
        #pragma exclude_renderers gles
        #include "UnityPBSLighting.cginc"

        #pragma multi_compile __ _NORMALMAP

        //#define TERRAIN_STANDARD_SHADER
        //#define TERRAIN_INSTANCED_PERPIXEL_NORMAL
        #define TERRAIN_SURFACE_OUTPUT SurfaceOutputStandard
        #include "MeshTriplanarNoTxtArr.cginc"

        half _Metallic0;
        half _Metallic1;
        half _Metallic2;
        half _Metallic3;
        half _Metallic4;
        half _Metallic5;

        half _Smoothness0;
        half _Smoothness1;
        half _Smoothness2;
        half _Smoothness3;
        half _Smoothness4;
        half _Smoothness5;

        void surf (Input IN, inout SurfaceOutputStandard o) {
            half4 splat_control03;
            half4 splat_control47;
            half weight;
            fixed4 mixedDiffuse;
            half4 defaultSmoothness03 = half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);
            half4 defaultSmoothness47 = half4(_Smoothness4, _Smoothness5, 0, 0);
            SplatmapMix(IN, 
                        defaultSmoothness03, 
                        defaultSmoothness47, 
                        splat_control03, 
                        splat_control47, 
                        weight, mixedDiffuse, o.Normal);
            o.Albedo = mixedDiffuse.rgb;
            o.Alpha = weight;
            o.Smoothness = mixedDiffuse.a;
            o.Metallic = dot(splat_control03, half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3)) + 
                         dot(splat_control47, half4(_Metallic4, _Metallic5, 0, 0));
        }
        ENDCG
    }

    FallBack "Diffuse"
}
