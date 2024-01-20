class_name CameraPivot extends Node3D

@export var player:Player

@onready var camera:Camera3D = $Camera
@onready var raycast_to_walls:RayCast3D = $Camera/RayCastToWalls
@onready var raycast_to_floor:RayCast3D = $RayCastToFloor
@onready var camera_pivot:Array[Node3D] = [ $TopDown, $TPV, $TPVNear, $FPV ]

enum CameraView {
	CAMERA_TOPDOWN,
	CAMERA_TPV,
	CAMERA_TPV_NEAR,
	CAMERA_FPV,
}

const raycast_targets = [
	Vector3(-0.5, 0, 0),
	Vector3(-0.5, 0, -1),
	Vector3(0.5, 0, 0),
	Vector3(0.5, 0, -1),
	Vector3(0, 0, -2.5),
]
const camera_change_time:Array[float] = [ 0.3, 0.3, 0.2, 0 ]
const camera_fov:Array[int] = [ 75, 75, 70, 80 ]
const camera_fpv_distance:Array[float] = [ -0.25, -0.25, -0.6, -0.25 ]
var camera_collision:Array[CollisionShape3D]
var current_camera:CameraView = CameraView.CAMERA_FPV
var camera_tween:Tween

func _ready():
	camera_collision = [ player.get_node("CameraFPV"), player.get_node("CameraTPV"), player.get_node("CameraNear"), player.get_node("CameraFPV")]
	player.camera_pivot = self
	_on_player_move()
	player.connect("player_move", _on_player_move)
	player.connect("player_change_anim", _on_player_change_anim)
	_on_player_change_anim(Consts.ANIM_IDLE)
	_change_camera(current_camera)

func _on_player_move():
	position = player.position
	rotation = player.rotation
	if ((camera_tween == null) or (not camera_tween.is_valid())):
		if is_colliding() and current_camera != CameraView.CAMERA_FPV:
			if (camera.position != camera_pivot[CameraView.CAMERA_TPV_NEAR].position):
				_change_camera(CameraView.CAMERA_TPV_NEAR)
		elif (camera.position != camera_pivot[current_camera].position):
			_change_camera(current_camera)

func _input(event):
	if (event is InputEventKey) and Input.is_action_just_released("camera_view"):
		var prev_camera = current_camera
		current_camera += 1
		if (current_camera >= 4):
			current_camera  = 0
		camera_collision[prev_camera].disabled = true
		_change_camera(current_camera)

func is_colliding():
	for target:Vector3 in raycast_targets:
		raycast_to_walls.target_position = target
		raycast_to_walls.force_raycast_update()
		if (raycast_to_walls.is_colliding()):
			return true
	return raycast_to_floor.is_colliding()

func _on_player_change_anim(anim_name:String):
	if (current_camera == CameraView.CAMERA_FPV):
		if (player.anim == null or player.anim.current_animation == Consts.ANIM_IDLE):
			camera_pivot[current_camera].position.z = camera_fpv_distance[0]
		elif (player.anim.current_animation.ends_with(Consts.ANIM_WALK)):
			camera_pivot[current_camera].position.z = camera_fpv_distance[1]
		elif (player.anim.current_animation.ends_with(Consts.ANIM_RUN)):
			camera_pivot[current_camera].position.z = camera_fpv_distance[2]
		elif (player.anim.current_animation.ends_with(Consts.ANIM_ATTACK)):
			camera_pivot[current_camera].position.z = camera_fpv_distance[3]
		_change_camera(current_camera, false)

func _change_camera(view:CameraView, change_rotation:bool = true):
	camera_collision[view].disabled = false
	camera_tween = get_tree().create_tween()
	camera_tween.tween_property(
		camera,
		"position",
		camera_pivot[view].position, 
		camera_change_time[view]
		).from_current()
	if (change_rotation):
		camera_tween.tween_property(
			camera,
			"rotation",
			camera_pivot[view].rotation, 
			camera_change_time[view] / 2
			).from_current()
	camera_tween.tween_property(
		camera,
		"fov",
		camera_fov[view], 
		camera_change_time[view] / 2
		).from_current()
