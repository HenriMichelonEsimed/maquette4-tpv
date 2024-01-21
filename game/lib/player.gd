class_name Player extends CharacterBody3D

@onready var character:Node3D = $Character
@onready var interactions:PlayerInteractions = $RayCastInteractions
@onready var timer_use:Timer = $TimerUse
@onready var camera_collision:Area3D = $Area3D

signal player_move()
signal player_change_anim(anim_name:String)
signal endurance_change()

const walking_speed:float = 4
const running_speed:float = 8
const jump_speed:float = 5
const mouse_sensitivity:float = 0.005
const joystick_sensitivity:float = 0.02
const max_camera_angle_up:float = deg_to_rad(50)
const max_camera_angle_down:float = -deg_to_rad(30)
const anim_blend:float = 0.2

var camera_pivot:CameraPivot
var anim:AnimationPlayer
var anim_group:String = Consts.ANIM_GROUP_PLAYER + "/"
var gravity = ProjectSettings.get_setting("physics/3d/default_gravity")
var mouse_captured:bool = false
var look_up_action:String = "look_up"
var look_down_action:String = "look_down"
var mouse_y_axis:int = -1
var previous_position:Vector3
var item_attachement:Array[Node3D] = [ null, null ]
# action animation playing
var attack_cooldown:bool = false
# weapon speed animation scale
var attack_speed_scale:float = 1.0
# one hit only allowed during attack cooldown
var hit_allowed:bool = false

func _ready():
	interactions.camera = camera_pivot.camera
	interactions.player = self
	anim = character.get_node("AnimationPlayer")
	item_attachement[Item.ItemSlot.SLOT_RIGHT_HAND] = character.get_node("Armature/Skeleton3D/RightHandAttachment/AttachmentPoint")
	item_attachement[Item.ItemSlot.SLOT_LEFT_HAND] = character.get_node("Armature/Skeleton3D/LeftHandAttachment/AttachmentPoint")
	if (GameState.player_state.position != Vector3.ZERO):
		set_pos()
	set_y_axis()
	capture_mouse()
	anim.play(anim_group + Consts.ANIM_IDLE)

func _input(event):
	if (event is InputEventScreenDrag) or (mouse_captured and (event is InputEventMouseMotion)):
		rotate_y(-event.relative.x * mouse_sensitivity)
		camera_pivot.rotate_y(-event.relative.x * mouse_sensitivity)
		camera_pivot.camera.rotate_x(event.relative.y * mouse_sensitivity * mouse_y_axis)
		camera_pivot.camera.rotation.x = clampf(camera_pivot.camera.rotation.x, max_camera_angle_down, max_camera_angle_up)
	if mouse_captured and Input.is_action_just_pressed("cancel"):
		release_mouse()

func _physics_process(delta):
	if (GameState.player_state.hp <= 0): return
	if (mouse_captured):
		var joypad_dir: Vector2 = Input.get_vector("look_left", "look_right", look_up_action, look_down_action)
		if joypad_dir.length() > 0:
			var look_dir = joypad_dir * delta
			rotate_y(-look_dir.x * 2.0)
			camera_pivot.rotate_y(-look_dir.x * 2.0)
			camera_pivot.rotate_x(-look_dir.y)
			camera_pivot.rotation.x = clamp(camera_pivot.rotation.x - look_dir.y,  max_camera_angle_down, max_camera_angle_up)
	if (Input.is_action_pressed("use") and mouse_captured):
		attack()
	if (attack_cooldown): 
		_regen_endurance()
		return
	var on_floor = is_on_floor_only() 
	if not on_floor:
		velocity.y += -gravity * delta
	var run = Input.is_action_pressed("run")
	var speed = running_speed if run else walking_speed
	var direction = Vector3.ZERO
	var input = Input.get_vector("move_left", "move_right", "move_forward", "move_backwards")
	direction = transform.basis * Vector3(input.x, 0, input.y)
	velocity.x = direction.x * speed
	velocity.z = direction.z * speed
	if  direction != Vector3.ZERO:
		#GameState.player_state.endurance -= 2
		endurance_change.emit()
		capture_mouse()
		if run and (GameState.player_state.endurance > 0):
			if (anim.current_animation != anim_group + Consts.ANIM_RUN):
				anim.play(anim_group + Consts.ANIM_RUN, anim_blend)
				player_change_anim.emit(Consts.ANIM_RUN)
		else:
			if (anim.current_animation != anim_group + Consts.ANIM_WALK):
				anim.play(anim_group + Consts.ANIM_WALK)
				player_change_anim.emit(Consts.ANIM_WALK)
		for index in range(get_slide_collision_count()):
			var collision = get_slide_collision(index)
			var collider = collision.get_collider()
			if collider == null: continue
			if collider.is_in_group("stairs"):
				velocity.y = 1.5
		player_move.emit()
	else:
		if (anim.current_animation != anim_group + Consts.ANIM_IDLE):
			anim.play(anim_group + Consts.ANIM_IDLE, anim_blend)
			player_change_anim.emit(Consts.ANIM_IDLE)
	previous_position = position
	move_and_slide()
	if (previous_position == position) and  (anim.current_animation != anim_group + Consts.ANIM_IDLE):
		anim.play(anim_group + Consts.ANIM_IDLE, anim_blend)
		player_change_anim.emit(Consts.ANIM_IDLE)
	if on_floor and Input.is_action_just_pressed("jump"):
		velocity.y = jump_speed

func _regen_endurance():
	if (GameState.player_state.endurance < GameState.player_state.endurance_max):
		GameState.player_state.endurance += 1
		endurance_change.emit()

func attack():
	if (not attack_cooldown) and (GameState.current_item[Item.ItemSlot.SLOT_RIGHT_HAND] != null) and (GameState.current_item[Item.ItemSlot.SLOT_RIGHT_HAND] is ItemWeapon) and (interactions.target_node == null):
		anim.play(anim_group + Consts.ANIM_ATTACK, 0.2, attack_speed_scale)
		player_change_anim.emit(Consts.ANIM_ATTACK)
		hit_allowed = true
		timer_use.wait_time =  GameMechanics.attack_cooldown(GameState.current_item[Item.ItemSlot.SLOT_RIGHT_HAND].speed)
		attack_cooldown = true
		timer_use.start()

func move(pos:Vector3, rot:Vector3):
	position = pos
	rotation = rot
	player_move.emit()

func handle_item(slot:Item.ItemSlot):
	item_attachement[slot].add_child(GameState.current_item[slot])
	if (slot == Item.ItemSlot.SLOT_RIGHT_HAND and GameState.current_item[slot] is ItemWeapon):
		anim_group = GameState.current_item[slot].anim + "/"
		attack_speed_scale = GameMechanics.anim_scale(GameState.current_item[slot].speed)
		if (GameState.current_item[slot].use_area != null):
			GameState.current_item[slot].use_area.connect("body_entered", _on_item_hit)

func unhandle_item(slot:Item.ItemSlot):
	item_attachement[slot].remove_child(GameState.current_item[slot])
	if (slot == Item.ItemSlot.SLOT_RIGHT_HAND):
		anim_group = Consts.ANIM_GROUP_PLAYER + "/"
		if (GameState.current_item[slot].use_area != null):
			GameState.current_item[slot].use_area.disconnect("body_entered", _on_item_hit)
	
func look_at_node(node:Node3D):
	var pos:Vector3 = node.global_position
	pos.y = position.y
	look_at(pos)
	
func set_y_axis():
	if (GameState.settings.mouse_y_axis_inverted):
		mouse_y_axis = 1
	else:
		mouse_y_axis = -1
	if (GameState.settings.joypad_y_axis_inverted):
		look_up_action = "look_down"
		look_down_action = "look_up"
	else:
		look_up_action = "look_up"
		look_down_action = "look_down"

func capture_mouse() -> void:
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
	mouse_captured = true

func release_mouse() -> void:
	Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
	mouse_captured = false
	
func set_pos():
	position = GameState.player_state.position
	rotation = GameState.player_state.rotation

func _on_timer_use_timeout():
	attack_cooldown = false

func _on_item_hit(node:Node3D):
	if (hit_allowed):
		hit_allowed = false
		if (node is NPC):
			node.hit(GameState.current_item[Item.ItemSlot.SLOT_RIGHT_HAND])
