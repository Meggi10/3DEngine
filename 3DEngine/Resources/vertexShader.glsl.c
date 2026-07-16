#version 430 core

layout(location = 0) in vec3 coords;
layout(location = 1) in vec3 vertNormal;
layout(location = 2) in vec3 vertTangent;
layout(location = 3) in vec3 vertBitangent;
layout(location = 4) in vec4 vertBones;
layout(location = 5) in vec4 vertWeights;
layout(location = 6) in vec2 vertUV;
layout(location = 0) uniform mat4 worldTransform;

out vec3 fragCoords;
out vec2 fragUV;
out vec3 fragNormal;
out vec3 fragTangent;
out vec3 fragBitangent;

void main()
{
	vec4 pos = vec4(coords, 1);
	gl_Position = worldTransform * pos;
	mat3 normalMatrix = mat3(worldTransform);
	fragNormal = transpose(inverse(normalMatrix)) * vertNormal;
	fragTangent = normalMatrix * vertTangent;
	fragBitangent = normalMatrix * vertBitangent;
	fragUV = vertUV;
}