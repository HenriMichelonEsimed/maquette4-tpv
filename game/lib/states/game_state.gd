extends Node

signal saving_start()
signal saving_end()
signal loading_start()
signal loading_end()

var player_state:PlayerState
var inventory:ItemsCollection
var settings:SettingsState
var quests:QuestsManager
var camera:CameraState

var player:Player
var ui:MainUI
var current_item:Array[Item] = [ null, null ]
var current_zone:Zone
var savegame_name:String
var use_joypad:bool = false
var use_joypad_ps:bool = false
var use_joypad_xbox:bool = false
var game_started:bool = false
var oxygen:float = 100.0

func _ready():
	Input.set_mouse_mode(Input.MOUSE_MODE_CONFINED)
	_on_joypad_connection_changed(0, 0)
	Input.connect("joy_connection_changed", _on_joypad_connection_changed)
	GameState.load_game()

func new_game():
	player_state = PlayerState.new()
	inventory = ItemsCollection.new()
	settings = SettingsState.new()
	quests = QuestsManager.new()
	camera = CameraState.new()
	StateSaver.reset_path()
	var os_lang = OS.get_locale_language()
	for lang in Settings.langs:
		if (lang == os_lang):
			GameState.settings.lang = lang
	StateSaver.loadState(settings, true)

func prepare_game(continue_last_game:bool):
	new_game()
	TranslationServer.set_locale(GameState.settings.lang)
	if (continue_last_game):
		load_game(StateSaver.get_last())

func save_game(savegame = null):
	saving_start.emit()
	StateSaver.set_path(savegame)
	player_state.position = player.position
	player_state.rotation = player.rotation
	_save_item(CurrentItemManager.ItemSlot.SLOT_RIGHT_HAND)
	_save_item(CurrentItemManager.ItemSlot.SLOT_LEFT_HAND)
	StateSaver.saveState(player_state)
	StateSaver.saveState(camera)
	StateSaver.saveState(InventoryState.new(inventory))
	StateSaver.saveState(settings)
	StateSaver.saveState(QuestsState.new(quests))
	StateSaver.saveState(current_zone.state)
	saving_end.emit()
	
func load_game(savegame = null):
	GameState.prepare_game(false)
	loading_start.emit()
	StateSaver.set_path(savegame)
	StateSaver.loadState(player_state)
	StateSaver.loadState(camera)
	_load_item(Item.ItemSlot.SLOT_RIGHT_HAND)
	_load_item(Item.ItemSlot.SLOT_LEFT_HAND)
	StateSaver.loadState(InventoryState.new(inventory))
	StateSaver.loadState(settings)
	StateSaver.loadState(QuestsState.new(quests))
	loading_end.emit()

func _save_item(slot:Item.ItemSlot):
	if (current_item[slot] != null):
		player_state.current_item_type[slot] = current_item[slot].type
		player_state.current_item_key[slot] = current_item[slot].key
	else:
		player_state.current_item_typ[slot] = Item.ItemType.ITEM_UNKNOWN

func _load_item(slot:Item.ItemSlot):
	if (player_state.current_item_type[slot] != Item.ItemType.ITEM_UNKNOWN):
		current_item[slot] = Tools.load_item(player_state.current_item_type[slot], player_state.current_item_key[slot])

func pause_game():
	if (ui.menu.visible): return
	#player.mute()
	get_tree().paused = true
	if (not use_joypad): Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
	ui.pause_game()

func resume_game():
	if (ui.menu.visible): return
	call_deferred("_resume_game")

func _resume_game():
	get_tree().paused = false
	if (not use_joypad): Input.set_mouse_mode(Input.MOUSE_MODE_CONFINED)
	ui.resume_game()

func _on_joypad_connection_changed(_id, _connected):
	use_joypad = Input.get_connected_joypads().size() > 0
	if (use_joypad):
		var joyname = Input.get_joy_name(0)
		use_joypad_ps = joyname.contains("PS")
		use_joypad_xbox = joyname.contains("XBox")

class InventoryState extends State:
	var inventory:ItemsCollection
	func _init(_inventory):
		super("inventory")
		self.inventory = _inventory

class QuestsState extends State:
	var quests:QuestsManager
	func _init(_quests):
		super("quests")
		self.quests = _quests
