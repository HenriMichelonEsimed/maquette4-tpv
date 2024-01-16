class_name MainUI extends Control

@onready var label_fps:Label = $LabelFPS

func _process(_delta):
	label_fps.text = str(Engine.get_frames_per_second())

func _input(event):
	if Input.is_action_just_pressed("quit"):
		_on_quit_pressed()

func _on_quit_pressed():
	get_tree().quit()
