extends CanvasLayer

@export  var player: Player
@onready var joystick_right = $JoystickRight/Analog
@onready var joystick_right_stick = $JoystickRight/Analog/Stick

var joystick_right_pressed := false
var joystick_right_offset : Vector2 = Vector2.ZERO
var joystick_right_size : Vector2

func _ready():
	joystick_right_size = joystick_right.texture_normal.get_size()

func _process(_delta):
	if joystick_right_pressed:
		var touch_position : Vector2 = (joystick_right.get_local_mouse_position() - joystick_right_offset).limit_length(joystick_right_size.x / 2.0)
		joystick_right_stick.position = touch_position + joystick_right_size / 2.0
		var strength : Vector2 = touch_position / (joystick_right_size / 2.0)
		print(strength)
		Input.action_press("look_left", 1.0 - strength.x)
		Input.action_press("look_right", strength.x)
		Input.action_press("look_up", 1.0 - strength.y)
		Input.action_press("look_down", strength.y)

func _on_analog_right_pressed():
	joystick_right_pressed = true
	joystick_right_offset = joystick_right.get_local_mouse_position()

func _on_analog_right_released():
	joystick_right_pressed = false
	Input.action_release("look_down")
	Input.action_release("look_up")
	Input.action_release("look_left")
	Input.action_release("look_right")
	joystick_right_stick.position = joystick_right_size / 2.0
