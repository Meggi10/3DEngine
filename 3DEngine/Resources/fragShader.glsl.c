#version 430 core

in vec3 fragNormal;
in vec3 fragTangent;
in vec3 fragBitangent;
in vec2 fragUV;

struct Material {
	sampler2D DiffuseMap;
	sampler2D SpecularMap;
	sampler2D NormalMap;
	float Shininess;
};
layout(location = 1) uniform Material material;

out vec4 color;

void main()
{
	vec4 matDiffuse = texture(material.DiffuseMap, fragUV);
	vec3 matSpecular = texture(material.SpecularMap, fragUV).rgb;
	color = matDiffuse;
	
	//color = vec4(fract(fragUV), 0.0, 1.0);
	//color = vec4(fragUV, 0.5, 1.0); // test koloru
}