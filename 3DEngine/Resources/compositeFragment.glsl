#version 430 core
//#define FXAA_REDUCE_MIN (1.0 / 128.0)
//#define FXAA_REDUCE_MUL (1.0 / 8.0)
//#define FXAA_SPAN_MAX   8.0

in vec2 vUv;
layout(binding = 0) uniform sampler2D OpaqueTex;
layout(binding = 1) uniform sampler2D AccumTex;
layout(binding = 2) uniform sampler2D RevealTex;

out vec4 outColor;

// Weighted-blended OIT resolve: avg = accum.rgb / accum.a, final = opaque * reveal + avg * (1 - reveal).vec3 resolve(vec2 uv)
void main()
{
    vec3 opaque = texture(OpaqueTex, vUv).rgb;
    vec4 accum = texture(AccumTex, vUv);
    float reveal = texture(RevealTex, vUv).r;
    vec3 avg = accum.rgb / max(accum.a, 1e-5);
    outColor = vec4(opaque * reveal + avg * (1.0 - reveal), 1);
    //outColor = vec4(opaque, 1);
}