extends StaticBodyAnimated

@onready var mesh:MeshInstance3D = $"Sketchfab_model/ec391e5c533447c599096ccc677f3a8f_fbx/Object_2/RootNode/Object_4/Skeleton3D/0"

const colors = [ Color(0,0,0,), Color(0.545, 0.4, 0.157), Color(0.337, 0.239, 0.078), Color(0,0,0,)  ]

func _ready():
	super._ready()
	var mat:ShaderMaterial = mesh.get_surface_override_material(1).duplicate()
	mat.set_shader_parameter("color_a", colors[randi_range(0, 3)])
	mat.set_shader_parameter("seed", Vector2(randf()*100, randf()*100))
	mesh.set_surface_override_material(1, mat)
	pass
