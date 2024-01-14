extends Node

var notifications:Array[String] = []

signal new_notification(message:String)
signal new_node_notification(sender:Node3D, message:String)
signal new_hit(target:Node3D, weapon:ItemWeapon, damage_points:int, positive:bool)
signal xp_gain(xp:int)
signal use_item(item:Item)
signal unuse_item()
signal node_call_for_help(sender:Node3D)

func notif(message:String):
	new_notification.emit(message)
	notifications.push_back(message)
	
func node_notif(sender:Node3D, message:String):
	new_node_notification.emit(sender, message)

func hit(target:Node3D, weapon:ItemWeapon, damage_points:int, positive:bool=true):
	new_hit.emit(target, weapon, damage_points, positive)

func xp(_xp:int):
	xp_gain.emit(_xp)

func use(item:Item):
	use_item.emit(item)

func unuse_current_item():
	unuse_item.emit()

func call_for_help(sender:Node3D):
	node_notif(sender, tr("%s call for help !") % str(sender))
	node_call_for_help.emit(sender)
