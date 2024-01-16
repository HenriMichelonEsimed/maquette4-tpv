class_name ItemWeapon extends ItemUnique

var damages_roll:DicesRoll
var speed_roll:DicesRoll

var speed:int = 1

func _init():
	type = ItemType.ITEM_WEAPONS

func _initialize():
	damages_roll = $Damages
	speed_roll = $Speed
	speed = speed_roll.roll()

func dup():
	var d = super.dup()
	d.damages_roll = damages_roll
	d.speed_roll = speed_roll
	return d