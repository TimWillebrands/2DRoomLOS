// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Blur"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_BlurAmount("BlurAmount", Range(0, 0.01)) = 0
	}

		SubShader
		{
			// No culling or depth
			Cull Off ZWrite Off ZTest Always

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
				};

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;

					return o;
				}

				 //normpdf function gives us a Guassian distribution for each blur iteration; 
				//this is equivalent of multiplying by hard #s 0.16,0.15,0.12,0.09, etc. in code above
				float normpdf(float x)
				{
					return 0.39894*exp(-0.5*x*x);
				}

				//this is the blur function... pass in standard col derived from tex2d(_MainTex,i.uv)
				half4 blur(sampler2D tex, float2 uv,float blurAmount) {
					//get our base color...
					half4 col = tex2D(tex, uv);
					//total width/height of our blur "grid":
					const int mSize = 9;
					//this gives the number of times we'll iterate our blur on each side 
					//(up,down,left,right) of our uv coordinate;
					//NOTE that this needs to be a const or you'll get errors about unrolling for loops
					const int iter = (mSize - 1) / 2;
					//run loops to do the equivalent of what's written out line by line above
					//(number of blur iterations can be easily sized up and down this way)
					for (int i = -iter; i <= iter; ++i) {
						for (int j = -iter; j <= iter; ++j) {
							col += tex2D(tex, float2(uv.x + i * blurAmount, uv.y + j * blurAmount)) * normpdf(float(i));
						}
					}
					//return blurred color
					return col/mSize;
				}

				float _BlurAmount;
				sampler2D _MainTex;

				fixed4 frag(v2f i) : SV_Target
				{
					return blur(_MainTex, i.uv, _BlurAmount);
				}			
				ENDCG
			}
		}
}
