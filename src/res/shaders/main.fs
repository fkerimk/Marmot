#version 330

// DEBUG: 0 off
// 1, lightsCount visualization (normalized by /4)
// 2, fragNormal visualization
// 3, NdotL visualization for lights[0] (black = opposite the light / 0, white = directly toward the light)
// 4, attenuation visualization for lights[0] (black = very far / 0, white = 1.0 = very close)
#define DEBUG_MODE 0

#define MAX_LIGHTS 4
#define LIGHT_DIRECTIONAL 0
#define LIGHT_POINT       1
#define LIGHT_SPOT        2

in vec3 fragPosition;
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;
in vec4 fragTangent;

out vec4 finalColor;

// Material textures
uniform sampler2D texture0; // albedo
uniform sampler2D texture1; // metalness
uniform sampler2D texture2; // normal
uniform sampler2D texture3; // roughness
uniform sampler2D texture4; // occlusion
uniform sampler2D texture5; // emission

// Which textures are actually attached
uniform int useTexAlbedo;
uniform int useTexMetalness;
uniform int useTexNormal;
uniform int useTexRoughness;
uniform int useTexOcclusion;
uniform int useTexEmissive;

// Instance-level material modifiers
uniform vec4 colDiffuse;
uniform vec3 albedoBlend;
uniform vec3 emissiveBlend;
uniform float albedoMultiplier;
uniform float metalnessMultiplier;
uniform float normalMultiplier;
uniform float roughnessMultiplier;
uniform float occlusionMultiplier;
uniform float emissiveIntensity;
uniform float albedoOverride;
uniform float metalnessOverride;
uniform float normalOverride;
uniform float roughnessOverride;
uniform float occlusionOverride;
uniform float emissiveOverride;

// Light data
struct Light {

    int enabled;
    int type;
    vec3 position;
    vec3 direction;    // direction for spot/forward (RlForward)
    vec4 color;        // rgb = color (alpha is not used)
    float intensity;   // light intensity
    float range;       // point/spot: maximum effect distance
    float innerCutoff; // cos(inner angle) - spot
    float outerCutoff; // cos(outer angle) - spot
};

uniform Light lights[MAX_LIGHTS];
uniform int lightsCount;

uniform vec3 viewPos;
uniform vec3 ambientColor;
uniform float ambientIntensity;

const float PI = 3.14159265359;

vec3 GetAlbedo() {

    vec3 tex = albedoOverride >= 0.0 ? vec3(albedoOverride) : (useTexAlbedo == 1 ? texture(texture0, fragTexCoord).rgb * albedoMultiplier : vec3(1.0));
    return tex * albedoBlend * colDiffuse.rgb * fragColor.rgb;
}

float GetMetallic() {

    float value = useTexMetalness == 1 ? texture(texture1, fragTexCoord).r * metalnessMultiplier : 0.0;
    return clamp(metalnessOverride >= 0.0 ? metalnessOverride : value, 0.0, 1.0);
}

float GetRoughness() {

    float r = useTexRoughness == 1 ? texture(texture3, fragTexCoord).r * roughnessMultiplier : 1.0;
    r = roughnessOverride >= 0.0 ? roughnessOverride : r;
    return clamp(r, 0.04, 1.0);
}

float GetAO() {

    float value = useTexOcclusion == 1 ? texture(texture4, fragTexCoord).r * occlusionMultiplier : 1.0;
    return clamp(occlusionOverride >= 0.0 ? occlusionOverride : value, 0.0, 1.0);
}

vec3 GetEmissive() {

    vec3 tex = emissiveOverride >= 0.0 ? vec3(emissiveOverride) : (useTexEmissive == 1 ? texture(texture5, fragTexCoord).rgb : vec3(0.0));
    return tex * emissiveBlend * emissiveIntensity;
}

vec3 GetNormal() {

    vec3 N = normalize(fragNormal);

    if (useTexNormal == 1) {

        // Normalize tangent
        vec3 T = normalize(fragTangent.xyz);
        
        // Gram-Schmidt orthogonalization to ensure that T and N are orthogonal
        T = normalize(T - dot(T, N) * N);
        
        // Calculate the bitangent using the cross product to correct its direction using the w component (bitangent sign)
        vec3 B = cross(N, T) * fragTangent.w;
        
        // Create the TBN matrix
        mat3 TBN = mat3(T, B, N);
        
        // Read from normal map and scale to [-1, 1] range
        vec3 tangentNormal = texture(texture2, fragTexCoord).xyz * 2.0 - 1.0;
        tangentNormal.xy *= normalOverride >= 0.0 ? normalOverride : normalMultiplier;
        tangentNormal = normalize(tangentNormal);
        
        // Transform to world space
        return normalize(TBN * tangentNormal);
    }
    
    return N;
}

// Cook-Torrance BRDF
float DistributionGGX(vec3 N, vec3 H, float roughness) {

    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;

    float denom = (NdotH2 * (a2 - 1.0) + 1.0);

    denom = PI * denom * denom;

    return a2 / max(denom, 0.0000001);
}

float GeometrySchlickGGX(float NdotV, float roughness) {

    float r = roughness + 1.0;
    float k = (r * r) / 8.0;

    return NdotV / (NdotV * (1.0 - k) + k);
}

float GeometrySmith(float NdotV, float NdotL, float roughness) {

    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

vec3 FresnelSchlick(float cosTheta, vec3 F0) {

    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

void main() {

    vec3 albedo = GetAlbedo();
    float metallic = GetMetallic();
    float roughness = GetRoughness();
    float ao = GetAO();

    vec3 N = GetNormal();
    vec3 V = normalize(viewPos - fragPosition);
    float NdotV = max(dot(N, V), 0.0000001);

    vec3 F0 = mix(vec3(0.04), albedo, metallic);

    vec3 Lo = vec3(0.0);

    for (int i = 0; i < MAX_LIGHTS; i++) {

        if (i >= lightsCount || lights[i].enabled == 0) continue;

        vec3 L;
        float attenuation = 1.0;

        if (lights[i].type == LIGHT_DIRECTIONAL) {

            L = normalize(-lights[i].direction);
            // Directional: no range or cone, attenuation = 1

        } else {

            vec3 toLight = lights[i].position - fragPosition;
            float dist   = length(toLight);
            L = toLight / max(dist, 0.0001);

            // Range-based window function (similar to KHR_lights_punctual):
            //   f_win = saturate(1 - (dist/range)^4)^2
            //   f_att = f_win / (dist^2 + 1)
            // It smoothly decays to zero outside the range and does not exceed 1.0 when dist=0.

            float r  = max(lights[i].range, 0.0001);
            float dr = dist / r;
            float dr2 = dr * dr;
            float win = clamp(1.0 - dr2 * dr2, 0.0, 1.0);
            
            win *= win;
            attenuation = win / (dist * dist + 1.0);

            if (lights[i].type == LIGHT_SPOT) {

                float theta   = dot(L, normalize(-lights[i].direction));
                float epsilon = max(lights[i].innerCutoff - lights[i].outerCutoff, 0.0001);
                float spotFactor = clamp((theta - lights[i].outerCutoff) / epsilon, 0.0, 1.0);

                attenuation *= spotFactor;
            }
        }

        vec3 H = normalize(V + L);
        float NdotL = max(dot(N, L), 0.0);

        if (NdotL == 0.0) continue;

        vec3 radiance = lights[i].color.rgb * lights[i].intensity * attenuation;

        float NDF = DistributionGGX(N, H, roughness);
        float G = GeometrySmith(NdotV, NdotL, roughness);
        vec3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);

        vec3 numerator = NDF * G * F;
        float denominator = 4.0 * NdotV * NdotL;
        vec3 specular = numerator / max(denominator, 0.0000001);

        vec3 kS = F;
        vec3 kD = (vec3(1.0) - kS) * (1.0 - metallic);

        Lo += (kD * albedo / PI + specular) * radiance * NdotL;
    }

    vec3 ambient = ambientColor * ambientIntensity * albedo * ao;
    vec3 emissive = GetEmissive();

    vec3 color = ambient + Lo + emissive;

    // Tone mapping (Reinhard) + gamma correction
    color = color / (color + vec3(1.0));
    color = pow(color, vec3(1.0 / 2.2));

    finalColor = vec4(color, colDiffuse.a);
}
