#version 330 core
#define CHECKER_SIZE (32)
#define COLOR_CORRECT_MIN (76./255.)
#define COLOR_CORRECT_MING (84./255.)
#define COLOR_CORRECT_MAX (1)
out vec4 FragColor;

in vec2 texCoord;

uniform vec2 texSize;
uniform sampler2D mainTex;
uniform bvec4 showChannels;
uniform int showChecker;
uniform int doColorCorrection;
uniform int convertToLinear;
uniform float correctionAmount;

vec3 colorCorrect(vec3 color)
{
    return vec3(mix(COLOR_CORRECT_MIN * correctionAmount, COLOR_CORRECT_MAX, color.r), 
                mix(COLOR_CORRECT_MING * correctionAmount, COLOR_CORRECT_MAX, color.g), 
                mix(COLOR_CORRECT_MIN * correctionAmount, COLOR_CORRECT_MAX, color.b));
}

vec4 toSrgb(vec4 c)
{
	c.r = max(1.055 * pow(c.r, 0.416666667) - 0.055, 0);
	c.g = max(1.055 * pow(c.g, 0.416666667) - 0.055, 0);
	c.b = max(1.055 * pow(c.b, 0.416666667) - 0.055, 0);
	return c;
}

vec4 toLinear(vec4 c)
{
	c.rgb = c.rgb * (c.rgb * (c.rgb * 0.305306011 + 0.682171111) + 0.012522878);
	return c;
}

void main()
{
    vec4 color = texture(mainTex, texCoord);
    
    // Show/Hide channels
    color.r = mix(0, color.r, showChannels.r);
    color.g = mix(0, color.g, showChannels.g);
    color.b = mix(0, color.b, showChannels.b);
    color.a = mix(1, color.a, showChannels.a);

    color.rgb = mix(color.rgb, colorCorrect(color.rgb), doColorCorrection);
    color = mix(color, toLinear(color), convertToLinear * doColorCorrection);
    color = clamp(color, 0, 1);

    // Checker pattern
    vec2 checkerUV = trunc(texCoord * texSize / CHECKER_SIZE);
    float checkerCol = mix(0.05, 0.55, mod(checkerUV.x + checkerUV.y, 2));
    checkerCol = mix(0, checkerCol, showChecker);

    // Final mix
    FragColor = vec4(color.rgb * color.a + vec3(checkerCol) * (1 - color.a), 1.0);
} 