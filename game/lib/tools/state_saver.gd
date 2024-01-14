extends Node

const default_ext:String = ".state"
const default_path:String = "user://savegames/"
const autosave_path:String = "_autosave"
const default_name:String = "my saved game"
const timestamp_name:String = "/timestamp"

enum {
 	STATE_VARIANT 		= 0,
	STATE_USABLE 		= 1,
	STATE_FUNCTIONAL	= 2,
	STATE_STRINGARRAY 	= 3,
	STATE_ITEMS			= 4,
	#STATE_EVENTS		= 5,
	STATE_MESSAGES		= 6,
	STATE_QUEST			= 7
}

var _last = null
var _path:String
const max_backup = 19

func get_savegames() -> Array:
	var dirs = Array(DirAccess.get_directories_at(default_path))
	dirs.sort_custom(_compare_times)
	return dirs

func get_last_savegame() -> String:
	if (_last == null) or (_last == autosave_path):
		return default_name
	return _last

func _compare_times(a,b):
	if not FileAccess.file_exists(default_path + a + timestamp_name):
		return b
	if not FileAccess.file_exists(default_path + b + timestamp_name):
		return a
	return FileAccess.get_modified_time(default_path + a + timestamp_name) > FileAccess.get_modified_time(default_path + b + timestamp_name)

func get_last():
	var dirs = get_savegames()
	if dirs.is_empty(): return null
	return dirs[0]

func savegame_exists(savegame:String):
	return DirAccess.dir_exists_absolute(default_path + _format_name(savegame))

func delete(savegame:String):
	OS.move_to_trash(ProjectSettings.globalize_path(default_path + _format_name(savegame)))

func reset_path():
	_path = ""

func set_path(savegame = null):
	_last = autosave_path if savegame == null else _format_name(savegame)
	_path = default_path + _last

func _format_name(text:String) -> String:
	return text.replace("/", "_").replace(":", "_").replace("%", "_").replace("'", "_").replace("\"", "_")
	
func saveState(res:State, global:bool=false):
	var filename:String
	if (global):
		DirAccess.make_dir_recursive_absolute(default_path)
		filename = default_path + _format_name(res.name) + default_ext
	else:
		filename = _path + "/" + _format_name(res.name) + default_ext
		DirAccess.make_dir_recursive_absolute(_path)
		var timestamp = FileAccess.open(_path + timestamp_name, FileAccess.WRITE)
		if (timestamp == null):
			return false
		timestamp.store_pascal_string(_last)
		timestamp.close()
	var file = FileAccess.open(filename, FileAccess.WRITE)
	if (file == null): 
		return false
	for prop in res.get_property_list():
		if (prop.name == "name"): continue
		if (prop.name == "parent"):
			var parent:Node = res.get("parent")
			if (parent != null):
				for node in parent.find_children("*", "Usable", true, false):
					if (node.save):
						file.store_8(STATE_USABLE)
						file.store_pascal_string(node.get_path())
						file.store_8(1 if node.is_used else 0)
						file.store_8(1 if node.unlocked else 0)
				for node in parent.find_children("*", "Functional", true, false):
					if (node.save):
						file.store_8(STATE_FUNCTIONAL)
						file.store_pascal_string(node.get_path())
						file.store_var(node.is_used)
			continue
		var value = res.get(prop.name)
		if (prop.type == TYPE_STRING 
			or prop.type == TYPE_BOOL
			or prop.type == TYPE_FLOAT
			or prop.type == TYPE_VECTOR3
			or prop.type == TYPE_INT):
			file.store_8(STATE_VARIANT)
			file.store_pascal_string(prop.name)
			file.store_var(value)
		elif (value is PackedStringArray):
			file.store_8(STATE_STRINGARRAY)
			file.store_pascal_string(prop.name)
			file.store_var(value)
		elif value is ItemsCollection:
			file.store_8(STATE_ITEMS)
			file.store_pascal_string(prop.name)
			value.saveState(file)
		#elif value is MessagesList:
		#	file.store_8(STATE_MESSAGES)
		#	file.store_pascal_string(prop.name)
		#	value.saveState(file)
		elif value is QuestsManager:
			file.store_8(STATE_QUEST)
			file.store_pascal_string(prop.name)
			value.saveState(file)
			#elif value is EventsQueue:
		#	file.store_8(STATE_EVENTS)
		#	file.store_pascal_string(prop.name)
		#	value.saveState(file)
	file.close()
	return true
	
func loadState(res:State, global:bool=false):
	var parent:Node = res.get("parent")
	var filename:String
	if (global):
		filename = default_path + _format_name(res.name) + default_ext
	elif _path.is_empty():
		return null
	else:
		filename = _path + "/" + _format_name(res.name) + default_ext
	var file = FileAccess.open(filename, FileAccess.READ)
	if (file == null):
		return null
	while (!file.eof_reached()):
		var entry_type = file.get_8()
		var entry_name = file.get_pascal_string()
		if (entry_type in [STATE_VARIANT, STATE_STRINGARRAY]):
			res.set(entry_name, file.get_var())
		elif (parent != null 
			and entry_type in [STATE_USABLE, STATE_FUNCTIONAL]):
			var is_used = file.get_8() == 1
			var unlocked = file.get_8() == 1
			var usable = parent.get_node_or_null(entry_name)
			if (usable != null):
				if is_used: 
					usable.use(false, true)
				usable.unlocked = unlocked
		elif (entry_type == STATE_ITEMS):
			var items:ItemsCollection = res.get(entry_name)
			items.loadState(file)
		#elif (entry_type == STATE_MESSAGES):
		#	var queue:MessagesList = res.get(entry_name)
		#	queue.loadState(file)
		elif (entry_type == STATE_QUEST):
			var queue:QuestsManager = res.get(entry_name)
			queue.loadState(file)
		#elif (entry_type == STATE_EVENTS):
		#	var queue:EventsQueue = res.get(entry_name)
		#	queue.loadState(file)
	file.close()

