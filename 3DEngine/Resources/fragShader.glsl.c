#version 430 core

in vec3 fragPos; // DODANE: pozycja wierzcho³ka w przestrzeni œwiata (z Vertex Shadera)
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
	vec3 position;
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
};

layout(location = 1) uniform Material material;
uniform Light light;
uniform vec3 viewPos; // Pozycja kamery w przestrzeni œwiata

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
	vec3 N = normalize(TBN * normal);

	// --- 2. MODEL OŒWIETLENIA BUI TUONG PHONGA ---
	vec3 L = normalize(light.position - fragPos); // Kierunek do œwiat³a
	vec3 V = normalize(viewPos - fragPos);        // Kierunek do kamery
	vec3 R = reflect(-L, N);                      // Odbicie œwiat³a (klasyczny Phong)

	// Sk³adowa otoczenia (Ambient)
	vec3 ambient = light.ambient * matDiffuse.rgb;

	// Sk³adowa rozproszona (Diffuse)
	float diff = max(dot(N, L), 0.0);
	vec3 diffuse = light.diffuse * diff * matDiffuse.rgb;

	// Sk³adowa lustrzana (Specular)
	float spec = pow(max(dot(V, R), 0.0), material.Shininess);
	vec3 specular = light.specular * spec * matSpecular;

	vec3 finalColor = ambient + diffuse + specular;
	float alpha = matDiffuse.a;

	// --- 3. OBLICZENIA WOIT (McGuire & Bavoil) ---
	// Funkcja wagi zale¿na od g³êbi (gl_FragCoord.z) i przezroczystoœci (alpha)
	float weight = clamp(pow(min(1.0, alpha * 10.0) + 0.01, 3.0) * 1e8 * pow(1.0 - gl_FragCoord.z * 0.9, 3.0), 1e-2, 3e3);

	// Zapis do buforów WOIT (kolor musi byæ pomno¿ony przez alpha - premultiplied alpha)
	accum = vec4(finalColor * alpha, alpha) * weight;
	reveal = alpha;
}