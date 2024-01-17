class_name StaticBodyAnimated extends StaticBody3D

func _ready():
	var anim:AnimationPlayer = $AnimationPlayer
	anim.play("default", -1, randf() * 1.5)
	anim.seek(randf() * anim.current_animation_length)
