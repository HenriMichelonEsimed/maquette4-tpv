extends CharacterBody3D

var speed: float = 2.0
var accel: float = 5.0

@onready var nav:NavigationAgent3D = $NavigationAgent3D
@onready var target:Node3D = $"../Target"

func _physics_process(delta):
	var dir = Vector3()
	nav.target_position = target.global_position
	if (not nav.is_navigation_finished()):
		dir = nav.get_next_path_position() - global_position
		dir = dir.normalized()
		velocity = velocity.lerp(dir * speed, accel * delta)
		move_and_slide()
	
