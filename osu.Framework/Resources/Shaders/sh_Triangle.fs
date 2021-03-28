#include "sh_Utils.h"
#include "sh_TextureWrapping.h"

varying lowp vec4 v_Colour;
varying mediump vec2 v_TexCoord;
varying mediump vec4 v_TexRect;

uniform lowp sampler2D m_Sampler;

void main(void)
{
	highp vec2 v1 = vec2(v_TexRect.x, v_TexRect.w);
	highp vec2 v2 = vec2((v_TexRect.x + v_TexRect.z) / 2.0, v_TexRect.y);
	highp vec2 v3 = vec2(v_TexRect.z, v_TexRect.w);
	
	float w1 = (v1.x * (v3.y - v1.y) + (v_TexCoord.y - v1.y) * (v3.x - v1.x) - (v_TexCoord.x) * (v3.y - v1.y)) / ((v2.y - v1.y) * (v3.x - v1.x) - (v2.x - v1.x) * (v3.y - v1.y));
	highp float w2 = (v_TexCoord.y - v1.y - w1 * (v2.y - v1.y)) / (v3.y - v1.y);
    
	if (w1 >= 0.0 && w2 >= 0.0 && (w1 + w2) <= 1.0)
	    gl_FragColor = toSRGB(v_Colour * wrappedSampler(wrap(v_TexCoord, v_TexRect), v_TexRect, m_Sampler, -0.9));
    else
        gl_FragColor = vec4(0);
}
