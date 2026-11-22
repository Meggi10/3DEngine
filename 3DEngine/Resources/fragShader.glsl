#version 430 core

in vec3 fragCoords;
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

struct Light {
	vec4 Position;
	vec3 AmbientLight;
	vec3 DiffuseLight;
	vec3 SpecularLight;
	vec3 AttCoeff;
};

layout(location = 1) uniform Material material;
layout(location = 5) uniform int uOIT;

layout(std140, binding = 0) uniform Camera
{
    mat4 ViewProjectionMatrix;
    vec3 CameraPos;
};

const int MAX_LIGHTS = 10; 
layout(std140, binding = 2) uniform Lights
{
    Light lights [MAX_LIGHTS];
    float LightsCount;
};
//layout(location = 4) uniform vec3 viewPos; // Pozycja kamery w przestrzeni œwiata

// Dwa wyjœcia dla WOIT (Multiple Render Targets)
layout(location = 0) out vec4 accum;
layout(location = 1) out float reveal;

void main()
{
	vec4 matDiffuse = texture(material.DiffuseMap, fragUV);

	// Odrzucenie ca³kowicie przezroczystych pikseli (opcjonalne w WOIT, ale przyspiesza renderowanie)
	if (matDiffuse.a < 0.01)
		discard;

	vec3 matSpecular = texture(material.SpecularMap, fragUV).rgb;

    // --- 1. NORMAL MAPPING ---
    // Odczyt normalnej z mapy i przeskalowanie z [0, 1] do [-1, 1]
    vec3 normal = texture(material.NormalMap, fragUV).rgb;
    normal = normalize(normal * 2.0 - 1.0);

    // Macierz TBN przekszta³caj¹ca z przestrzeni stycznej (Tangent) do przestrzeni œwiata
    mat3 TBN = mat3(normalize(fragTangent), normalize(fragBitangent), normalize(fragNormal));
    vec3 N = normalize(TBN * normal); //To check out!
    //vec3 N = normal; //test

    vec3 finalColor = vec3(0);
	// --- 2. MODEL OŒWIETLENIA BUI TUONG PHONGA ---
	for (int i = 0; i < LightsCount; i++)
	{
		Light light = lights[i];
        vec3 L = light.Position.xyz;
        float attenuation = 1;
        if (light.Position.w > 0)
        {
            L -= fragCoords;
            float d = length(L);
            L /= d;
            attenuation = light.AttCoeff.x + light.AttCoeff.y * d + light.AttCoeff.z * d * d;
        }
        //vec3 L = normalize(lightDir - fragCoords); // Kierunek do œwiat³a
        vec3 V = normalize(CameraPos - fragCoords);        // Kierunek do kamery
        vec3 R = reflect(-L, N);                      // Odbicie œwiat³a (klasyczny Phong)

        // Sk³adowa otoczenia (Ambient)
        vec3 ambient = light.AmbientLight * matDiffuse.rgb;

        // Sk³adowa rozproszona (Diffuse)
        float diff = max(dot(N, L), 0.0);
        //float diff = abs(dot(N, L)); //test
        vec3 diffuse = light.DiffuseLight * diff * matDiffuse.rgb;

        // Sk³adowa lustrzana (Specular)
        float spec = pow(max(dot(V, R), 0.0), max(material.Shininess, 1.0));
        vec3 specular = light.SpecularLight * spec * matSpecular;
        finalColor += ambient + (diffuse + specular) / attenuation;
        //finalColor = matDiffuse.rgb;
    }
	float alpha = matDiffuse.a;
    // Tymczasowo zamiast oœwietlenia:
    //finalColor = matDiffuse.rgb;                        // wynik koñcowy z map¹

    // --- 3. OBLICZENIA WOIT (McGuire & Bavoil) ---
    // Funkcja wagi zale¿na od g³êbi (gl_FragCoord.z) i przezroczystoœci (alpha)
    float weight = clamp(pow(min(1.0, alpha * 10.0) + 0.01, 3.0) * 1e8 * pow(1.0 - gl_FragCoord.z * 0.9, 3.0), 1e-2, 3e3);

	if (uOIT == 0)
	{
		accum = vec4(finalColor, 1);
	}
    else
    {
        // Zapis do buforów WOIT (kolor musi byæ pomno¿ony przez alpha - premultiplied alpha)
        accum = vec4(finalColor * alpha, alpha) * weight;
        reveal = alpha;
    }
}