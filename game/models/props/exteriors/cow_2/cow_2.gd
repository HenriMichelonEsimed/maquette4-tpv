extends StaticBodyAnimated

@onready var mesh:MeshInstance3D = $Sketchfab_model/ec391e5c533447c599096ccc677f3a8f_fbx/Object_2/RootNode/Object_4/Skeleton3D/Object_7

func _ready():
	super._ready()
	var mat:ShaderMaterial = mesh.get_active_material(0).duplicate()
	mat.set_shader_parameter("seed", Vector2(randf()*100, randf()*100))
	mesh.set_surface_override_material(0, mat)
	pass