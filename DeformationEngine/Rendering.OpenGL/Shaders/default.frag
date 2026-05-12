#version 330 core
out vec4 FragColor;

in vec3 FragPos;
in vec3 Normal;
in vec3 ViewPos;

uniform vec3 lightColor = vec3(1.0, 1.0, 1.0);
uniform vec3 objectColor = vec3(0.6, 0.6, 0.6);
uniform bool isWireframe;

void main() {
    if (isWireframe) {
        FragColor = vec4(objectColor, 1.0);
        return;
    }

    float ambientStrength = 0.4;
    vec3 ambient = ambientStrength * lightColor;
  	 
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(ViewPos - FragPos);
    
    float diff = max(dot(norm, lightDir), 0.0);
    float backDiff = max(dot(norm, -lightDir), 0.0) * 0.2;
    vec3 diffuse = (diff + backDiff) * lightColor;
            
    vec3 result = (ambient + diffuse) * objectColor;
    FragColor = vec4(result, 1.0);
}