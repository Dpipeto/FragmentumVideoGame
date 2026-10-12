// Un sprite que se funde en transparente por abajo: la cola de agua de la Doncella del Lamento,
// que su lienzo corta en recto. Las últimas filas pierden opacidad poco a poco hasta el borde;
// el resto del dibujo no cambia. El PNG no se toca.
//
// _Fundido es la fracción del alto del lienzo que se funde (0 = nada). La pone, por fotograma,
// Bosses.ColaFundida: los fotogramas que no acaban en la cola cortada no se funden.
//
// Respeta el color del SpriteRenderer (el aviso y el destello del jefe) y, como la silueta, no
// lleva luces: el pase no tiene LightMode y el 2D Renderer de URP lo dibuja como SRPDefaultUnlit.
Shader "Fragmentum/SpriteFundidoAbajo"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Fundido ("Fracción del alto que se funde por abajo", Range(0, 0.5)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "False"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half _Fundido;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                // uv.y va de 0 (la fila de abajo del lienzo) a 1: el sprite ocupa la textura entera.
                half visible = _Fundido > 0.0001 ? saturate(input.uv.y / _Fundido) : 1.0;
                color.a *= visible;
                return color;
            }
            ENDHLSL
        }
    }
}
