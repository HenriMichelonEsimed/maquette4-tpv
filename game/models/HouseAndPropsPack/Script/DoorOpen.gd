extends Node3D

@onready var animation = $AnimationPlayer
@onready var area = $Area3D
var door_state = false




func _on_area_3d_body_entered(body):
	if body is CharacterBody3D and !door_state :
		animation.play("open")
	

func _on_area_3d_body_exited(body):
	if body is CharacterBody3D:
		animation.play_backwards("open")
