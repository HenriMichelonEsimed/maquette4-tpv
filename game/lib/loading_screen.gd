extends Control

func _ready():
	Tools.preload_zone(GameState.player_state.zone_name)

func _process(_delta):
	match Tools.preload_zone_status(GameState.player_state.zone_name):
		ResourceLoader.THREAD_LOAD_LOADED:
			get_tree().change_scene_to_file("res://scenes/main.tscn")
		ResourceLoader.THREAD_LOAD_FAILED:
			get_tree().change_scene_to_file("res://scenes/ui/main_menu.tscn")
