// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "POS/Blur"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_BlurAmount("BlurAmount", Range(0, 0.01)) = 0
		_BlurIter("BlurIterations", Int) = 9
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
					half4 vertex : POSITION;
					half2 uv : TEXCOORD0;
				};

				struct v2f
				{
					half2 uv : TEXCOORD0;
					half4 vertex : SV_POSITION;
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
				half normpdf(half x)
				{
					return 0.39894*exp(-0.5*x*x);
				}


				//this is the blur function... pass in standard col derived from tex2d(_MainTex,i.uv)
				fixed3 blur(sampler2D tex, half2 uv,half blurAmount, int blurIter) {
					//get our base color...
					fixed3 col = tex2D(tex, uv);
					//this gives the number of times we'll iterate our blur on each side 
					//(up,down,left,right) of our uv coordinate;
					//NOTE that this needs to be a const or you'll get errors about unrolling for loops
					const int iter = (blurIter - 1) / 2;
					//run loops to do the equivalent of what's written out line by line above
					//(number of blur iterations can be easily sized up and down this way)
					for (int i = -iter; i <= iter; ++i) {
						for (int j = -iter; j <= iter; ++j) {
							col += tex2D(tex, half2(uv.x + i * blurAmount, uv.y + j * blurAmount)) * normpdf(half(i));
						}
					}
					//return blurred color
					return col/blurIter;
				}

				int _BlurIter;
				half _BlurAmount;
				sampler2D _MainTex;

				fixed4 frag(v2f i) : SV_Target {
					fixed3 col = blur(_MainTex, i.uv, _BlurAmount, _BlurIter);
					return fixed4(col.x, col.y, col.z, 1 - col.x);
				}			
				ENDCG
			}
		}
}
