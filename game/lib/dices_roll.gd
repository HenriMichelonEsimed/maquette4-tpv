class_name DicesRoll extends Node

@export var dice_count:int = 1
@export var dice_faces:int = 3
@export var modifier:int = 0
@export var multiplier:int = 1
@export var divider:int = 1
@export var minimum:int = 1

func _to_string():
	var _str = "%dd%d" % [ dice_count, dice_faces]
	if (modifier > 0):
		_str += "+%d" % modifier
	elif (modifier < 0):
		_str += "-%d" % (-modifier)
	if (multiplier > 1):
		_str += "*%d" % multiplier
	elif (divider > 1):
		_str += "/%d" % divider
	return _str

func roll():
	var points = modifier
	for i in range(0, dice_count):
		points += (randi() % dice_faces)+1
	if (multiplier > 1): points *= multiplier
	if (divider > 1): points /= divider
	if (points < minimum): points = minimum
	if (points == 0) and (divider > 1): points = 1
	#print("Dice roll %s : %d" % [ self, points ])
	return points
