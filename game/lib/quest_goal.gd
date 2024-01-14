class_name QuestGoal extends Node

var key:String
var label:String
var parent:String
var started = false

func _init(_k, _parent, _label):
	key = _k
	label = _label
	parent = _parent

func start():
	if (not started):
		_start()
		started = true
	_restart()
	NotificationManager.notif(label)

func _restart():
	pass

func _start():
	pass

func on_new_quest_event(_type:Quest.QuestEventType, _key:String):
	pass

func success():
	return null

func saveState(file:FileAccess):
	file.store_8(1 if started else 0)

func loadState(file:FileAccess):
	started = file.get_8() == 1
