Shader "Avena/Detect"
{
	Properties { }

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"RenderType"="Transparent"
			"Mask"="tag_center"
		}

		LOD 0

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
					float4 vertex : POSITION;
			};

			struct v2f
			{
					float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
					return fixed4(0.0, 0.0, 0.0, 1.0);
			}
			ENDCG
		}
	}
	
	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"RenderType"="Transparent" 
			"Mask"="tag_target"
		}
		
		LOD 0

		GrabPass
		{
			"_Back"
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
					float4 vertex : POSITION;
			};

			struct v2f
			{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
			};

			fixed4 _MaskColor;
			sampler2D _Back;

			v2f vert (appdata v)
			{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = ComputeGrabScreenPos(o.vertex);
					return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
					fixed4 back = tex2D(_Back, i.uv);
					return fixed4(_MaskColor.r, _MaskColor.g, _MaskColor.b, back.a);
			}
			ENDCG
		}
	}
}
