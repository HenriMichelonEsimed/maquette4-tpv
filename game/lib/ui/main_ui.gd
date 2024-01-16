class_name MainUI extends Control

@onready var label_fps:Label = $LabelFPS

func _process(delta):
	label_fps.text = str(Engine.get_frames_per_second())
