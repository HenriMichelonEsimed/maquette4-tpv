class_name Trigger extends StaticBody3D

signal triggered(trigger:Trigger)

var is_triggered:bool = false

func _ready():
	set_collision_layer_value(Consts.LAYER_TRIGGER, true)

func trigger():
	if (!is_triggered):
		is_triggered = true
		triggered.emit(self)
