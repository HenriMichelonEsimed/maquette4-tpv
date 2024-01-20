class_name Tools extends Object

const DIALOG_SELECT_QUANTITY = "select_quantity"
const DIALOG_TRANSFERT_ITEMS = "items_transfert"
const DIALOG_LOAD_SAVEGAME = "load_savegame"
const DIALOG_SETTINGS = "settings"
const DIALOG_INPUT = "input"
const DIALOG_CONFIRM = "confirm"
const DIALOG_ALERT = "alert"
const DIALOG_PLAYER_SETUP = "player_setup"
const DIALOG_CONTROLLER = "controller"
const DIALOG_ENEMY_INFO = "enemy_info"
const DIALOG_WEAPON_INFO = "weapon_info"
const DIALOG_GAMEOVER = "gameover"

const SCREEN_INVENTORY = "inventory"
const SCREEN_TERMINAL = "terminal"
const SCREEN_NPC_TALK = "talk"
const SCREEN_NPC_TRADE = "trade"

const CONTROLLER_KEYBOARD = "keyboard"
const CONTROLLER_XBOX = "xbox"
const CONTROLLER_PS = "ps"

const SHORTCUT_DROP = "drop"
const SHORTCUT_HELP = "help"
const SHORTCUT_DELETE = "drop"
const SHORTCUT_CANCEL = "cancel"
const SHORTCUT_INVENTORY = "inventory"
const SHORTCUT_CRAFT = "craft"
const SHORTCUT_DECLINE = "decline"
const SHORTCUT_MENU = "menu"
const SHORTCUT_TERMINAL = "terminal"
const SHORTCUT_USE = "use"
const SHORTCUT_UNUSE = "unuse"
const SHORTCUT_ALL = "all"
const SHORTCUT_ACCEPT = "accept"
const SHORTCUT_INFO = "info"

const ITEMS_PATH = [ 'weapons', 'tools', 'consum', 'misc', 'quest']

#static func is_mobile():
#	return OS.get_name().to_lower() in ["android", "ios"]

static func load_shortcut_icon(name:String):
	var controller = CONTROLLER_KEYBOARD
	if GameState.use_joypad:
		controller = CONTROLLER_PS if GameState.use_joypad_ps else CONTROLLER_XBOX 
	return load("res://assets/textures/controllers/buttons/" + controller + "/" + name + ".png")

static func load_audio(type:String,name:String):
	return load("res://assets/audio/" + type + "/" + name + ".mp3")

static func load_item(type:int,name:String):
	var item = load("res://models/items/" + ITEMS_PATH[type] + "/" + name + ".tscn")
	if (item != null):
		return item.instantiate()
	return null

static func load_char(_char:String):
	var item = load("res://scenes/characters/" + _char + ".tscn")
	if (item != null):
		return item.instantiate()
	return null

static func load_enemy(_char:String):
	var item = load("res://scenes/characters/" + _char + ".tscn")
	if (item != null):
		return item
	return null

static func load_zone(zone_name:String):
	var zone_path = "res://zones/" + zone_name + ".tscn"
	return load(zone_path)
	#var _dummy = []
	#if (ResourceLoader.load_threaded_get_status(zone_path, _dummy) == ResourceLoader.THREAD_LOAD_INVALID_RESOURCE):
	#	ResourceLoader.load_threaded_request(zone_path, "", true)
	#return ResourceLoader.load_threaded_get(zone_path)

static func preload_zone(zone_name:String):
	var zone_path = "res://zones/" + zone_name + ".tscn"
	return ResourceLoader.load_threaded_request(zone_path, "", true)

static func preload_zone_status(zone_name:String) -> ResourceLoader.ThreadLoadStatus:
	var zone_path = "res://zones/" + zone_name + ".tscn"
	return ResourceLoader.load_threaded_get_status(zone_path)

static func load_dialog(parent:Node, dialog:String, on_close = null) -> Dialog:
	var scene = load("res://scenes/ui/" + dialog + "_dialog.tscn").instantiate()
	parent.add_child(scene)
	if (on_close != null): 
		scene._on_close = on_close
	return scene

static func load_controller_texture(controller:String) -> Texture2D:
	return load("res://assets/textures/controllers/" + controller + ".png")

static func show_item(item:Item, node_3d:Node3D):
	for c in node_3d.get_children():
		c.queue_free()
	var scale = item.preview_scale
	var clone = item.dup()
	node_3d.add_child(clone)
	clone.position = Vector3.ZERO
	clone.rotation = Vector3.ZERO
	clone.scale = clone.scale * scale

static func show_character(_char:InteractiveCharacter, node_3d:Node3D):
	for c in node_3d.get_children():
		c.queue_free()
	var clone = _char.duplicate(0)
	node_3d.add_child(clone)
	clone.position = Vector3.ZERO
	clone.rotation = Vector3.ZERO
	clone.scale = Vector3(1.0, 1.0, 1.0)

static func set_shortcut_icon(button:Control, name:String):
	if (button is TextureButton):
		button.texture_normal = load_shortcut_icon(name)
	elif (button is Button):
		button.icon = load_shortcut_icon(name)
	elif (button is TextureRect):
		button.texture = load_shortcut_icon(name)

static func reset_shortcut_icon(button:Control):
	if (button is TextureButton):
		button.texture_normal = null
	elif (button is Button):
		button.icon = null
	elif (button is TextureRect):
		button.texture = null

static func get_nearest_point(position:Vector3, array:Array[Vector3]) -> Vector3:
	array.sort_custom( func(a,b): return a.distance_to(position)<b.distance_to(position) )
	return array[0]

static func get_nearest_node(node:Node3D, array:Array[Node3D]) -> Node3D:
	array.sort_custom( func(a,b): return a.global_position.distance_to(node.global_position)<b.global_position.distance_to(node.global_position) )
	return array[0]

static func get_nearest_path(position:Vector3, array:Array[NearestPath]) -> NearestPath:
	array.sort_custom( func(a,b): return a.nearest.distance_to(position)<b.nearest.distance_to(position) )
	return array[0]

class NearestPath extends Object:
	var path:Path3D
	var nearest:Vector3
	func _init(p, n):
		path = p
		nearest = n
	
