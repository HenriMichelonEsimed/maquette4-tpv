class_name MainUI extends Control

@export var player:Player

@onready var label_fps:Label = $LabelFPS
@onready var label_info:Label = $HUD/LabelInfos
@onready var crosshair = $HUD/Crosshair


var displayed_node:Node

func _ready():
	label_info.visible = false
	player.interactions.connect("display_info", _on_display_info)
	player.interactions.connect("hide_info", hide_info)
	
func _process(_delta):
	label_fps.text = str(Engine.get_frames_per_second())

func _input(_event):
	if Input.is_action_just_pressed("quit"):
		_on_quit_pressed()

func _on_quit_pressed():
	GameState.save_game()
	get_tree().quit()

func _label_info_position():
	label_info.position = crosshair.position
	label_info.position.y += 30
	label_info.position.x += 40

func _on_display_info(node:Node3D):
	displayed_node = node
	var label:String = tr(str(displayed_node))
	if (label.is_empty()): 
		label = str(node)
	label_info.text = label
	_label_info_position()
	label_info.visible = true

func hide_info():
	label_info.visible = false
	label_info.text = ''
