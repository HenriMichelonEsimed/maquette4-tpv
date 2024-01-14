class_name Quest extends Node

enum QuestEventType {
	QUESTEVENT_READMESSAGE	= 0,
	QUESTEVENT_TALK			= 1,
	QUESTEVENT_BUY			= 2,
}

var key:String
var label:String
var starting_point:QuestGoal
var current:QuestGoal
var advancementPoints = []
var advancementPointsAll = {}

func _init(_k, _start, _label, _avdpoints):
	key = _k
	label = _label
	starting_point = _start
	advancementPointsAll = _avdpoints
	current = starting_point

func start():
	current.start()

func have_advpoint(adv_key:String) -> bool:
	for adv in advancementPoints:
		if (adv.key == adv_key):
			return true
	return false

func finished_advpoint(adv_key:String) -> bool:
	for adv in advancementPoints:
		if (adv.key == adv_key):
			return adv.finished
	return false
	
func get_advpoint(adv_key:String):
	for adv in advancementPoints:
		if (adv.key == adv_key):
			return adv
	return null

func add_advpoint(adv_key:String):
	if (not have_advpoint(adv_key)):
		var adv:Array = advancementPointsAll[adv_key]
		if (adv != null):
			if (adv.size() > 1):
				for i in range(1, adv.size()):
					var old_adv_key = adv[i]
					var old_adv = get_advpoint(old_adv_key)
					old_adv.finished = true
			advancementPoints.push_back(QuestAdvancementPoint.new(adv_key, adv[0]))

func get_advpoints():
	return advancementPoints.filter(func(adv): return not adv.finished)

func _on_new_quest_event(_type:Quest.QuestEventType, _event_key:String):
	pass

func on_new_quest_event(type:Quest.QuestEventType, event_key:String):
	_on_new_quest_event(type, event_key)
	current.on_new_quest_event(type, event_key)
	var next = current.success()
	if (next != null):
		var current_class = load_goal(next)
		## nil GAME ERROR
		current = current_class.new()
		current.start()

func load_goal(adv:String):
	return load("res://lib/quests/" + key + "/" + adv + ".gd")

func saveState(file:FileAccess):
	var classname = current.key
	file.store_pascal_string(classname)
	current.saveState(file)
	file.store_64(advancementPoints.size())
	for adv in advancementPoints:
		adv.saveState(file)

func loadState(file:FileAccess):
	var classname = file.get_pascal_string()
	var current_class = load_goal(classname)
	## nil GAME ERROR
	current = current_class.new()
	## nil else GAME ERROR
	current.loadState(file)
	advancementPoints.clear()
	var n = file.get_64()
	for i in range(0, n):
		var adv = QuestAdvancementPoint.new()
		adv.loadState(file)
		var adv_quest = advancementPointsAll[adv.key]
		if (adv_quest != null):
			adv.label = adv_quest[0]
			advancementPoints.push_back(adv)
