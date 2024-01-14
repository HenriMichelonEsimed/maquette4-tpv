class_name Usable extends StaticBody3D

signal using(is_used:bool)
signal unlock(success:bool)

@export var label:String
@export var title:String
@export var sound:AudioStream

var save:bool
var is_used:bool = false
var unlocked:bool = false
var _animation:AnimationPlayer
var audio:AudioStreamPlayer3D

func _init(_save:bool = true):
	save = _save

func _ready():
	set_collision_layer_value(Consts.LAYER_USABLE, true)
	if (label == null): 
		label = get_path()
	var text = find_child("Text")
	if text != null:
		text.text = title
	_animation = find_child("AnimationPlayer")
	if (_animation != null):
		_animation.connect("animation_finished", _on_animation_finished)
	if (sound != null):
		audio = AudioStreamPlayer3D.new()
		audio.stream = sound
		#audio.max_distance = 5
		audio.bus = Consts.AUDIO_BUS_EFFECTS
		audio.volume_db = Consts.AUDIO_VOLUME_EFFECTS
		add_child(audio)

func _check_item_use(message_locked:String, message_unlocked:String, tools_to_use:Array) -> bool:
	if (unlocked): return true
	var check = false
	if (GameState.current_item != null):
		for tool in tools_to_use:
			if (tool[0] == GameState.current_item.type) and (tool[1] == GameState.current_item.key):
				unlocked = true
				NotificationManager.notif(message_unlocked)
				unlock.emit(true)
				return true
	NotificationManager.notif(message_locked)
	unlock.emit(false)
	return check
	
func _check_use():
	if (GameState.current_item != null) and not(GameState.current_item is ItemWeapon):
		unlock.emit(false)
		return false
	return true

func force_use():
	is_used = true
	if (_animation != null):
		_animation.play("use")
		_animation.seek(10)
	else:
		_use()
		using.emit(is_used)

func use(_byplayer:bool=false, startup:bool=false):
	if (not is_used):
		if (not startup and not _check_use()) :
			return
	is_used = !is_used
	if (audio != null) and (not startup):
		audio.play()
	if (is_used):
		if (_animation != null):
			_animation.play("use")
			if (startup): 
				_animation.seek(10)
		else:
			_use()
			using.emit(is_used)
	else:
		if (_animation != null): 
			_animation.play_backwards("use")
			if (startup): 
				_animation.seek(10)
		else:
			_unuse()
			using.emit(is_used)

func _on_animation_finished(anim_name:String):
	if (anim_name == "use"):
		_use()
		using.emit(is_used)
	else:
		_unuse()
		using.emit(is_used)

func _use():
	pass

func _unuse():
	pass

func _to_string():
	return label
