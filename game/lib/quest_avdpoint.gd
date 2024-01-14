class_name QuestAdvancementPoint extends Node

var key:String
var label:String
var finished:bool = false

func _init(_k = "", _label = ""):
	key = _k
	label = "" if _label == null else _label

func saveState(file:FileAccess):
	file.store_pascal_string(key)
	file.store_8(1 if finished else 0)

func loadState(file:FileAccess):
	key = file.get_pascal_string()
	finished = file.get_8() == 1
