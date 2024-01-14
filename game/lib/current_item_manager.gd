extends Node

var _previous_item = null

func _ready():
	NotificationManager.connect("use_item", use)
	NotificationManager.connect("unuse_item", unuse)

func _unhandled_input(event):
	if (GameState.current_item == null): return
	elif  Input.is_action_just_released("unuse"):
		unuse()
	elif Input.is_action_just_pressed("help"):
		Tools.load_dialog(get_tree().root, Tools.DIALOG_WEAPON_INFO).open(GameState.current_item)

func use(item:Item):
	unuse()
	GameState.current_item = item.dup()
	if (item is ItemMultiple):
		GameState.current_item.quantity = 1
	GameState.inventory.remove(GameState.current_item)
	GameState.player.handle_item()
	GameState.ui.panel_item.use()
	GameState.current_item.use()
	_previous_item = null

func unuse():
	if (GameState.current_item == null): return
	GameState.player.unhandle_item()
	GameState.ui.panel_item.unuse()
	GameState.current_item.unuse()
	_previous_item = GameState.current_item.dup()
	GameState.inventory.add(_previous_item)
	GameState.current_item = null
