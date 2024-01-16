extends Node3D

var zones:ZonesManager = ZonesManager.new()

func _ready():
	if not(Tools.is_mobile()):
		DisplayServer.window_set_size(DisplayServer.window_get_size()*2)
		get_window().content_scale_mode = Window.CONTENT_SCALE_MODE_CANVAS_ITEMS
		get_window().content_scale_aspect = Window.CONTENT_SCALE_ASPECT_KEEP
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
	
	GameState.player = $Player
	GameState.ui = $MainUI
	TranslationServer.set_locale(GameState.settings.lang)
	NotificationManager.connect("xp_gain", xp_gain)
	GameState.quests.start("main")
	zones.change_zone(self, GameState.player_state.zone_name)
	if (GameState.player_state.position != Vector3.ZERO):
		GameState.player.move(GameState.player_state.position, GameState.player_state.rotation)
	GameState.game_started = true
	GameState.player_state.hp = GameState.player_state.hp_max
	GameState.player_state.endurance = GameState.player_state.endurance_max

func xp_gain(xp:int):
	GameState.player_state.xp += xp
	GameState.ui.display_xp()
