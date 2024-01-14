@tool
extends LineEdit

# allows to submit with escape and on focus exitd.
func _ready():
	focus_exited.connect(on_focus_exited)
	gui_input.connect(input)

func input(event):
	if(event is InputEventKey):
		if event.keycode == KEY_ESCAPE && event.pressed && has_focus():
			release_focus()
 
func on_focus_exited():
	emit_signal("text_submitted", text)
	
