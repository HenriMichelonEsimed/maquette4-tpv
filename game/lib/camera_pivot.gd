class_name CameraPivot extends Node3D

@export var player:Player

@onready var camera:Camera3D = $Camera
@onready var raycast_to_walls:RayCast3D = $Camera/RayCastToWalls
@onready var raycast_to_floor:RayCast3D = $RayCastToFloor
@onready var near:Node3D = $Near
@onready var far:Node3D = $Far


const raycast_targets = [
	Vector3(-0.5, 0, 0),
	Vector3(-0.5, 0, -1),
	Vector3(0.5, 0, 0),
	Vector3(0.5, 0, -1),
	Vector3(0, 0, -2.5),
]

var camera_change:Tween
const camera_change_time:float = 0.5

func _ready():
	player.camera_pivot = self
	_on_player_move()
	player.connect("player_move", _on_player_move)
	camera.position = far.position

func _on_player_move():
	position = player.position
	rotation = player.rotation

func _physics_process(_delta):
	if ((camera_change == null) or (not camera_change.is_valid())):
		if is_colliding():
			if (camera.position != near.position):
				camera_change = get_tree().create_tween()
				camera_change.tween_property(
					camera,
					"position",
					near.position, 
					camera_change_time
					).from_current()
				camera.position = near.position
		elif (camera.position != far.position):
			camera_change = get_tree().create_tween()
			camera_change.tween_property(
				camera,
				"position",
				far.position, 
				camera_change_time
				).from_current()
			camera.position = far.position
	
func is_colliding():
	for target:Vector3 in raycast_targets:
		raycast_to_walls.target_position = target
		raycast_to_walls.force_raycast_update()
		if (raycast_to_walls.is_colliding()):
			return true
	return raycast_to_floor.is_colliding()

