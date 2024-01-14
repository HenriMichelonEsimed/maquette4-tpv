shader_type spatial;
render_mode unshaded;

uniform float line_len = 10.0;
uniform vec4 grid_color : source_color;
uniform bool is_perspective = true;
varying vec3 distVec;
varying vec3 eyeVec;
uniform vec3 centerPos;

void vertex() {
	vec4 eyePos = MODELVIEW_MATRIX * vec4(VERTEX, 1.0); // not model_view_matrix bc we work in world vertex coords
	eyeVec = eyePos.xyz;
	distVec = VERTEX;
}

void fragment() {
	ALBEDO = grid_color.rgb;
	float relativeCenterDis = length(distVec)/line_len;
	float relCenterDisWithMin = min(max(relativeCenterDis - 0.2, 0.001), 1.0);
	
	float centerDist = max(1.0, length(distVec)-(line_len/2.5));
	
	float distWithMin = 20.0*relCenterDisWithMin;
	if(is_perspective) {
		float relEyeDist = 0.2*length(eyeVec)/log(line_len);
		distWithMin = centerDist * relEyeDist*relEyeDist;
	}
	ALPHA = clamp(1.0/(distWithMin)-0.2, 0, 1)*grid_color.a;
}
