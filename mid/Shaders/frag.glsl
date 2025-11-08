#version 330 core
out vec4 FragColor;

in vec3 vPos;
in vec3 vNormal;
in vec2 vUV;

uniform sampler2D uTex;
uniform vec3 uLightPos;
uniform vec3 uLightColor;
uniform float uLightIntensity;
uniform vec3 uViewPos;
uniform int uIsSky;

uniform int  uUnlit;          // 0 = lit planets, 1 = unlit (rings/flare)
uniform int  uAlphaFromTex;   // 0 = opaque, 1 = use texture alpha
uniform vec3 uTint;           // atmosphere/flare tint; (0,0,0) => no tint

void main(){
    vec4 texel = texture(uTex, vUV);
    float alpha = (uAlphaFromTex == 1) ? texel.a : 1.0;

    if (uIsSky == 1){
        FragColor = vec4(texel.rgb, 1.0);
        return;
    }

    if (uUnlit == 1){
        vec3 color = texel.rgb * (uTint == vec3(0.0) ? vec3(1.0) : uTint);
        FragColor = vec4(color, alpha);
        return;
    }

    vec3 albedo = texel.rgb;

    vec3 N = normalize(vNormal);
    vec3 L = normalize(uLightPos - vPos);
    vec3 V = normalize(uViewPos - vPos);

    float dist = length(uLightPos - vPos);
    float k0 = 1.0, k1 = 0.02, k2 = 0.001;
    float atten = 1.0 / (k0 + k1*dist + k2*dist*dist);

    float diff = max(dot(N, L), 0.0);
    vec3 H = normalize(L + V);
    float spec = pow(max(dot(N, H), 0.0), 64.0);

    vec3 ambient  = 0.06 * albedo;
    vec3 diffuse  = diff * albedo * uLightColor * uLightIntensity * atten;
    vec3 specular = 0.2 * spec * uLightColor * uLightIntensity * atten;

    float rim = pow(1.0 - max(dot(N, V), 0.0), 2.2);
    vec3 rimColor = 0.15 * rim * (uTint == vec3(0.0) ? albedo : uTint);

    vec3 color = ambient + diffuse + specular + rimColor;
    FragColor = vec4(color, alpha);
}
