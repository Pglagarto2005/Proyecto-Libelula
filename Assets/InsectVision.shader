Shader "FullScreen/InsectVision"
{
    Properties
    {
        _HexScale ("Hex Scale (tamaño de celda)", Range(5, 200)) = 40
        _EdgeThickness ("Grosor de linea", Range(0.0, 0.3)) = 0.06
        _EdgeDarkness ("Oscurecer lineas", Range(0.0, 1.0)) = 0.6
        _Desaturation ("Desaturacion", Range(0.0, 1.0)) = 0.3
        _TintColor ("Tinte", Color) = (0.6, 0.9, 0.4, 1.0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "InsectVisionPass"
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Declaramos nosotros mismos lo que Blit.hlsl nos daria, para no depender
            // de una ruta de archivo que cambia entre versiones de URP.
            TEXTURE2D_X(_BlitTexture);
            float4 _BlitTexture_TexelSize;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Genera un triangulo que cubre toda la pantalla usando solo el vertexID,
            // sin necesidad de una mesh (tecnica estandar para shaders de post-proceso).
            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);

                float2 texcoord = uv;
                if (_ProjectionParams.x < 0.0)
                    texcoord.y = 1.0 - texcoord.y;
                output.texcoord = texcoord;

                return output;
            }

            float _HexScale;
            float _EdgeThickness;
            float _EdgeDarkness;
            float _Desaturation;
            float4 _TintColor;

            // Funcion de grilla hexagonal (basada en el algoritmo clasico de mattz, muy usado en shaders de hexagonos)
            static const float2 HEX_S = float2(1.0, 1.7320508); // 1, sqrt(3)

            float HexDistance(float2 p)
            {
                p = abs(p);
                return max(dot(p, HEX_S * 0.5), p.x);
            }

            // Devuelve: .xy = posicion local relativa al centro del hexagono, .zw = id de la celda
            float4 GetHex(float2 p)
            {
                float4 hC = floor(float4(p, p - float2(0.5, 1.0)) / HEX_S.xyxy) + 0.5;
                float4 h = float4(p - hC.xy * HEX_S, p - (hC.zw + 0.5) * HEX_S);

                return dot(h.xy, h.xy) < dot(h.zw, h.zw)
                    ? float4(h.xy, hC.xy)
                    : float4(h.zw, hC.zw + 0.5);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // Corrige el aspecto para que los hexagonos no salgan estirados
                float aspect = _BlitTexture_TexelSize.z / _BlitTexture_TexelSize.w;
                float2 uvAspect = float2(uv.x * aspect, uv.y);

                float2 p = uvAspect * _HexScale;
                float4 hexInfo = GetHex(p);
                float2 localOffset = hexInfo.xy;
                float2 cellId = hexInfo.zw;

                // Reconstruye el UV del centro de la celda para "pixelar" en hexagonos
                float2 hexCenterP = cellId * HEX_S;
                float2 hexCenterUV = hexCenterP / _HexScale;
                hexCenterUV.x /= aspect;
                hexCenterUV = saturate(hexCenterUV);

                float3 baseColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, hexCenterUV).rgb;

                // Lineas de borde entre hexagonos (como el patron de panal)
                float dist = HexDistance(localOffset);
                float edge = smoothstep(0.5 - _EdgeThickness, 0.5, dist);
                float3 col = baseColor * (1.0 - edge * _EdgeDarkness);

                // Desaturacion + tinte, para look de vision de insecto
                float luminance = dot(col, float3(0.299, 0.587, 0.114));
                float3 tinted = luminance * _TintColor.rgb;
                col = lerp(col, tinted, _Desaturation);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
