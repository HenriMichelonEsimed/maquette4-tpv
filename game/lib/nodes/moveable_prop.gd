class_name MoveableProp extends RigidBody3D

@export var sound:AudioStream
var audio:AudioStreamPlayer3D

func _ready():
	set_collision_layer_value(Consts.LAYER_WORLD, false)
	set_collision_layer_value(Consts.LAYER_MOVEABLE_PROP, true)
	set_collision_mask_value(Consts.LAYER_WORLD, true)
	set_collision_mask_value(Consts.LAYER_USABLE, true)
	set_collision_mask_value(Consts.LAYER_MOVEABLE_PROP, true)
	set_collision_mask_value(Consts.LAYER_PLAYER, true)
	if (sound != null):
		connect("sleeping_state_changed", _on_sleeping_state_changed)
		audio = AudioStreamPlayer3D.new()
		audio.stream = sound
		#audio.max_distance = 8
		audio.bus = Consts.AUDIO_BUS_EFFECTS
		audio.volume_db = Consts.AUDIO_VOLUME_EFFECTS
		add_child(audio)

func _on_sleeping_state_changed():
	if (sleeping):
		audio.stop()
	else:
		audio.play()
