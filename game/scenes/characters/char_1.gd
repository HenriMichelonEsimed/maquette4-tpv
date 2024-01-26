extends CharacterBody3D

@onready var nav:NavigationAgent3D = $NavigationAgent3D
@export var patrol_path:Path3D

var speed: float = 10.0
var accel: float = 5.0

var anim:AnimationPlayer
var prev_pos = Vector3.ZERO

func _ready():
	anim = $Char1.get_node("AnimationPlayer")
	anim.play("idle")
	_find_patrol_target()

func _find_patrol_target():
	var curve = patrol_path.curve
	var curve_length = curve.get_baked_length()
	var offset = randf()*curve_length
	nav.set_target_position(to_global(patrol_path.curve.sample_baked(offset)))

func _physics_process(delta):
	if (position == prev_pos) or nav.is_navigation_finished():
		_find_patrol_target()
	velocity = global_position.direction_to(nav.get_next_path_position()) * speed
	if (velocity == Vector3.ZERO): print(velocity)
	prev_pos = position
	move_and_slide()
