class_name Dialog extends Control

var _on_close = null

static var dialogs_stack:Array[Dialog] = []
static var _ignore_input:bool = false

func _ready():
	if (GameState.game_started):
		process_mode = PROCESS_MODE_WHEN_PAUSED
	z_index = 2

func set_shortcuts():
	pass
	
static func refresh_shortcuts():
	dialogs_stack.all(func(dlg): dlg.set_shortcuts())

func _open():
	set_shortcuts()
	if (not GameState.game_started): return
	if (dialogs_stack.is_empty()):
		GameState.pause_game()
	else:
		dialogs_stack.back().process_mode = PROCESS_MODE_DISABLED
	dialogs_stack.push_back(self)

func close():
	if (GameState.game_started): 
		var back = dialogs_stack.pop_back()
		if (self == back):
			if (dialogs_stack.is_empty()):
				if (not GameState.ui.menu.visible):
					GameState.resume_game()
			else:
				dialogs_stack.back().process_mode = PROCESS_MODE_WHEN_PAUSED
		_ignore_input = true
	queue_free()
	if (_on_close != null):
		_on_close.call()

static func ignore_input() -> bool:
	var ignore = _ignore_input
	_ignore_input = false
	return ignore
