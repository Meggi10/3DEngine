#version 430 core

layout(location = 0) in vec3 coords;
layout(location = 1) in vec3 vertNormal;
layout(location = 2) in vec3 vertTangent;
layout(location = 3) in vec3 vertBitangent;
layout(location = 4) in vec4 vertBones;
layout(location = 5) in vec4 vertWeights;
layout(location = 6) in vec2 vertUV;
layout(location = 0) uniform mat4 worldTransform;
const int MAX_BONES_TRANSFORMS = 100;
layout(std140, binding = 1) uniform Bones
{
	mat4 BonesTransforms[MAX_BONES_TRANSFORMS];
};

out vec3 fragCoords;
out vec2 fragUV;
out vec3 fragNormal;
out vec3 fragTangent;
out vec3 fragBitangent;

void main()
{
	vec4 pos = vec4(coords, 1);
    mat3 normalMatrix = mat3(worldTransform);
    if (vertWeights[0] > 0)
    {
        mat4 skinMatrix = mat4(0);
        for (int i = 0; i < 4; i++)
        {
            int boneIdx = int(vertBones[i]);
            float weight = vertWeights[i];
            if (weight > 0)
                skinMatrix += BonesTransforms[boneIdx] * weight;
        }
        pos = skinMatrix * pos;
        normalMatrix = mat3(skinMatrix);
    }
    else
        pos = worldTransform * pos;
	gl_Position = pos;
	fragNormal = transpose(inverse(normalMatrix)) * vertNormal;
	fragTangent = normalMatrix * vertTangent;
	fragBitangent = normalMatrix * vertBitangent;
	fragUV = vertUV;
}