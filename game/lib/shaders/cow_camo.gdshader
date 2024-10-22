/*
	迷彩柄シェーダー by あるる（きのもと 結衣）
	Camouflage Shader by Yui Kinomoto @arlez80

	MIT License
*/
shader_type spatial;

uniform vec4 color_a : source_color = vec4( 0.0, 0.0, 0.0, 1.0 );
uniform vec4 color_b : source_color = vec4( 0.44313725490196076, 0.32941176470588235, 0.21568627450980393, 1.0 );
uniform vec2 seed = vec2( 0.0, 0.0 );
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;
float random( vec2 pos )
{ 
	return fract(sin(dot(pos, vec2(12.9898,78.233))) * 43758.5453);
}

float noise( vec2 pos )
{
	return random( floor( pos ) );
}

float value_noise( vec2 pos )
{
	vec2 p = floor( pos );
	vec2 f = fract( pos );

	float v00 = noise( p + vec2( 0.0, 0.0 ) );
	float v10 = noise( p + vec2( 1.0, 0.0 ) );
	float v01 = noise( p + vec2( 0.0, 1.0 ) );
	float v11 = noise( p + vec2( 1.0, 1.0 ) );

	vec2 u = f * f * ( 3.0 - 2.0 * f );

	return mix( mix( v00, v10, u.x ), mix( v01, v11, u.x ), u.y );
}

float camouflage( vec2 uv )
{
	return (
			value_noise( uv * 8.0 ) * 0.2
		+	value_noise( uv * 8.0 ) * 0.2
		+	value_noise( uv * 8.0 ) * 0.2
		+	value_noise( uv * 8.0 ) * 0.2
		+	value_noise( uv * 64.0 ) * 0.2
	);
}

void fragment( )
{
	ALBEDO = mix(
		mix(
			color_a
		,	color_a
		,	float( camouflage( UV + seed ) < 0.5 )
		)
	,	mix(
			color_a
		,	color_b
		,	float( camouflage( UV + seed + vec2( 0.0, 100.0 ) ) < 0.5 )
		)
	,	float( camouflage( UV + seed + vec2( 100.0, 0.0 ) ) < 0.5 )
	).rgb;
}
