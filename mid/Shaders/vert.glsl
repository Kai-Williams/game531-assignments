#version 330 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aUV;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProj;

out vec3 vPos;      // world-space position
out vec3 vNormal;   // world-space normal
out vec2 vUV;

void main()
{
    vec4 worldPos = uModel * vec4(aPos, 1.0);
    vPos    = worldPos.xyz;

    // transform normal with inverse-transpose of model
    vNormal = mat3(transpose(inverse(uModel))) * aNormal;

    vUV     = aUV;

    gl_Position = uProj * uView * worldPos;
}
