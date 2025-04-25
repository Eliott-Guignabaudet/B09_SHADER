Shader "Custom/DistortionShader"
{
    Properties
    {
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _DistortionStrength("Distortion Strength", Range(0, 0.1)) = 0.03
        _NoiseScale("Noise Scale", Float) = 30.0
        _TimeSpeed("Time Speed", Float) = 1.0
        _Opacity("Opacity", Range(0,1)) = 0.5
    }

    SubShader
    {
        // Making the shader transparent and blend with the background
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            Name "Distortion"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;   // Position of the GameObject
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;   // Position of the screen
                float2 uv : TEXCOORD0;
                float4 screenUV : TEXCOORD1;
            };

            sampler2D _CameraOpaqueTexture;
            sampler2D _NoiseTex;
            float _DistortionStrength;
            float _NoiseScale;
            float _TimeSpeed;
            float _Opacity;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);    // Transform position from Object to Screen 
                OUT.uv = IN.uv;
                OUT.screenUV = ComputeScreenPos(OUT.positionHCS);   // UV used to read _CameraOpaqueTexture
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // Normalize UV for _CameraOpaqueTexture
                float2 screenUV = IN.screenUV.xy / IN.screenUV.w;

                // Calculate UV affected by the noise texture and other properties
                float2 noiseUV = IN.uv * _NoiseScale + float2(_Time.y * _TimeSpeed, _Time.y * _TimeSpeed);
                float2 noise = tex2D(_NoiseTex, noiseUV).rg * 2.0 - 1.0;

                // Apply distortion offset on screen UV
                float2 distortedUV = screenUV + noise * _DistortionStrength;

                // Return background texture with calculated distortion
                float4 distortedColor = tex2D(_CameraOpaqueTexture, distortedUV);
                distortedColor.a = _Opacity;

                return distortedColor;
            }
            ENDHLSL
        }
    }
}
