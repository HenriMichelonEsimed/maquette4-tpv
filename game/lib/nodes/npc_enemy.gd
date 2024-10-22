class_name NPCEnemy extends NPC

#region StateMachine
enum States {
	STARTING,
	DEAD,
	PLAYER_DEAD,
	IDLE,
	MOVE_TO_POSITION,
	MOVE_TO_PLAYER,
	ATTACK,
	ESCAPE_TO_POSITION,
}

const STATES_NAMES:Array[String] = [
	"STARTING",
	"DEAD",
	"PLAYER_DEAD",
	"IDLE",
	"MOVE_TO_POSITION",
	"MOVE_TO_PLAYER",
	"ATTACK",
	"ESCAPE_TO_POSITION"
]

var states:Dictionary = {
	States.STARTING: [ action_start ],
	States.DEAD: [],
	States.PLAYER_DEAD: [ action_idle ],
	States.IDLE: [
		condition_preconditions,
		setvar_player_detected,
		condition_attack_player,
		condition_player_in_hearing_distance,
		condition_player_detected_and_not_hidden,
		condition_getaround_enemy,
		condition_heard_hit,
		condition_heard_help_call,
		action_idle
	],
	States.MOVE_TO_PLAYER : [
		condition_preconditions,
		setvar_player_detected,
		condition_player_still_detected,
		condition_attack_player,
		condition_is_blocked,
		action_move_to_player
	],
	States.MOVE_TO_POSITION: [
		condition_preconditions,
		setvar_player_detected,
		condition_attack_player,
		condition_player_detected_and_not_hidden,
		condition_getaround_enemy,
		condition_is_blocked,
		condition_heard_hit,
		condition_heard_help_call,
		condition_continue_to_position,
		action_move_to_detected_position
	],
	States.ATTACK: [
		condition_preconditions,
		setvar_player_detected,
		condition_player_still_detected,
		condition_player_not_in_sight,
		condition_player_in_attack_range,
		condition_can_attack,
		action_attack_player
	],
	States.ESCAPE_TO_POSITION: [
		condition_preconditions,
		condition_player_in_hearing_distance,
		condition_escape_stop,
		condition_escape_is_blocked,
		action_move_to_escape_position
	],	
}
#endregion

#region Exports
@export var hear_distance:float = 8
@export var attack_distance:float = 0.8
#endregion

#region Nodes
@onready var weapon:ItemWeapon = $RootNode/Skeleton3D/WeaponAttachement/Weapon
@onready var walking_speed_roll:DicesRoll = $WalkingSpeed
@onready var running_speed_roll:DicesRoll = $RunningSpeed
@onready var detection_distance_roll:DicesRoll = $DetectionDistance
@onready var help_distance_roll:DicesRoll = $HelpDistance
#endregion

#region Properties
# attack & weapon speed cooldown timer
var attack_allowed:bool = false
var attack_cooldown:bool = false
var timer_attack_cooldown:Timer = Timer.new()
# player detection raycast
var raycast_detection:RayCast3D = RayCast3D.new()
# XP gain
var xp:int
# player detection distance (*2.0 for help call)
var detection_distance:float = 10
# distance we hear hel calls
var help_distance:float = 15
# Gondor call for help !
var help_called:bool = false
# weapon speed animation scale
var attack_animation_scale:float
# rotation animation when idle
var idle_rotation_tween:Tween
# last player detection position
var detected_position:Vector3 = Vector3.ZERO
# blocked, trying to escape using a pre defined path
var escape_position:Tools.NearestPath
var escape_direction:int = 1
# blocked for too long
var is_blocked_count:int = 0
const is_blocked_count_trigger:int = 8
# trying to get around another enemy to attack
var getaround_count:int = 0
var getaround_count_trigger:int = 10
# player in detection area
var player_detected:bool = false
var player_hidden:bool = false
# raycast collision result
var is_colliding:bool = false
var last_collider:Node3D
# last position when moving to position
var previous_position:Vector3 = Vector3.ZERO
# we heard a fight
var heard_hit:bool = false
# we heard a call for help
var heard_help_call:bool = false
# default detection angle
var idle_detection_angle:float = 75
# detection angle after detection and move to player/position
var current_detection_angle:float = idle_detection_angle
# number of times the node have been blocked by another node
var blocked_count:int = 0
var slide_angle:float
#endregion

func _ready():
	super._ready()
	state = StateMachine.new(self, states, STATES_NAMES)
	weapon.disable()
	weapon.use_area.set_collision_mask_value(Consts.LAYER_PLAYER, true)
	weapon.use_area.set_collision_mask_value(Consts.LAYER_NPC, false)
	walking_speed = walking_speed_roll.roll()
	running_speed = running_speed_roll.roll()
	detection_distance = detection_distance_roll.roll()
	help_distance = help_distance_roll.roll()
	xp = hit_points
	set_collision_mask_value(Consts.LAYER_WALLS, true)
	set_collision_layer_value(Consts.LAYER_NPC, true)
	attack_animation_scale = GameMechanics.anim_scale(weapon.speed)
	raycast_detection.position.y = height
	raycast_detection.target_position = Vector3(0.0, 0.0, -detection_distance)
	raycast_detection.set_collision_mask_value(Consts.LAYER_WALLS, true)
	add_child(raycast_detection)
	timer_attack_cooldown.process_callback = Timer.TIMER_PROCESS_PHYSICS
	timer_attack_cooldown.one_shot = true
	timer_attack_cooldown.wait_time = GameMechanics.attack_cooldown(weapon.speed)
	timer_attack_cooldown.connect("timeout", _on_timer_attack_timeout)
	add_child(timer_attack_cooldown)
	if (weapon.use_area != null):
		weapon.use_area.connect("body_entered", _on_item_hit)
	NotificationManager.connect("new_hit", _on_new_hit)
	NotificationManager.connect("node_call_for_help", _on_call_for_help)
	if (randf() < 0.5): escape_direction = -1

#region Setvar Block
func setvar_player_detected(_delta):
	player_detected = (player_distance < hear_distance)
	if (not player_detected) and (player_distance < detection_distance):
		var forward_vector = -transform.basis.z
		var vector_to_player = (GameState.player.position - position).normalized()
		player_detected = acos(forward_vector.dot(vector_to_player)) <= deg_to_rad(current_detection_angle)
	var pos = GameState.player.position
	pos.y = GameState.player.height
	var local = raycast_detection.to_local(pos)
	raycast_detection.target_position = local
	last_collider = raycast_detection.get_collider()
	is_colliding = raycast_detection.is_colliding()
	#print("%s %s %s" % [ name, last_collider, is_colliding])
	player_hidden = not(is_colliding) or (is_colliding and not(last_collider is Player))
#endregion

#region Conditions Block
func condition_player_in_hearing_distance(_delta):
	if (not player_hidden) and (player_distance < hear_distance) and (is_blocked_count < is_blocked_count_trigger):
		return state.change_state(States.MOVE_TO_PLAYER, "player_in_hearing_distance")
	return StateMachine.Result.CONTINUE

func condition_getaround_enemy(_delta):
	if player_hidden and is_colliding and (last_collider is NPCEnemy) and (player_detected):
		if (getaround_count < getaround_count_trigger):
			detected_position = last_collider.position
			detected_position.x += escape_direction * randf() * getaround_count
			detected_position.z += -1.0 * randf() * getaround_count
			getaround_count += 1
			return state.change_state(States.MOVE_TO_POSITION, "getaround_enemy")
		else:
			look_at(GameState.player.position)
	return StateMachine.Result.CONTINUE

func condition_preconditions(_delta) -> StateMachine.Result:
	if (condition_death(_delta) == StateMachine.Result.STOP): return StateMachine.Result.STOP
	if (condition_player_dead(_delta) == StateMachine.Result.STOP): return StateMachine.Result.STOP
	return super.condition_preconditions(_delta)

func condition_death(_delta) -> StateMachine.Result:
	if (hit_points <= 0):
		NotificationManager.xp(xp)
		label_info.queue_free()
		progress_hp.queue_free()
		raycast_detection.queue_free()
		weapon.queue_free()
		$CollisionShape3D.queue_free()
		NotificationManager.disconnect("new_hit", _on_new_hit)
		label_info = null
		player_in_info_area = false
		return state.change_state(States.DEAD, "death")
	return StateMachine.Result.CONTINUE

func condition_player_dead(_delta) -> StateMachine.Result:
	if (GameState.player_state.hp <= 0):
		return state.change_state(States.PLAYER_DEAD, "player_dead")
	return StateMachine.Result.CONTINUE

func condition_attack_player(_delta) -> StateMachine.Result:
	if (player_distance <= attack_distance):
		if (not attack_cooldown):
			raycast_detection.target_position = Vector3(0.0, 0.0, -detection_distance)
			raycast_detection.force_raycast_update()
			if (raycast_detection.is_colliding() and raycast_detection.get_collider() is Player):
				_stop_idle_rotation()
				return state.change_state(States.ATTACK, "attack_player")
	return StateMachine.Result.CONTINUE

func condition_player_in_attack_range(_delta) -> StateMachine.Result:
	if (not player_hidden) and (player_distance <= attack_distance):
		return StateMachine.Result.CONTINUE
	return state.change_state(States.MOVE_TO_PLAYER, "player_in_attack_range")

func condition_can_attack(_delta) -> StateMachine.Result:
	if (attack_cooldown):
		return StateMachine.Result.STOP
	return StateMachine.Result.CONTINUE

func condition_player_not_in_sight(_delta):
	raycast_detection.target_position = Vector3(0.0, 0.0, -detection_distance)
	raycast_detection.force_raycast_update()
	if (raycast_detection.is_colliding() and not(raycast_detection.get_collider() is Player)):
		current_detection_angle = 90
		previous_position = Vector3.ZERO
		return state.change_state(States.MOVE_TO_POSITION, "player_not_in_sight")
	return StateMachine.Result.CONTINUE

func condition_player_still_detected(_delta) -> StateMachine.Result:
	if (player_detected):
		return StateMachine.Result.CONTINUE
	return state.change_state(States.IDLE, "player_detected")

func condition_player_detected_and_not_hidden(_delta) -> StateMachine.Result:
	if (player_detected):
		if (is_blocked_count > is_blocked_count_trigger):
			return StateMachine.Result.CONTINUE
		if not player_hidden:
			current_detection_angle = idle_detection_angle
			return state.change_state(States.MOVE_TO_PLAYER, "player_detected_and_not_hidden")
	return StateMachine.Result.CONTINUE

func condition_player_detected(_delta) -> StateMachine.Result:
	if (player_detected):
		return state.change_state(States.MOVE_TO_PLAYER, "condition_player_detected")
	return StateMachine.Result.CONTINUE

func condition_heard_hit(_delta) -> StateMachine.Result:
	if (heard_hit):
		heard_hit = false
		previous_position = Vector3.ZERO
		return state.change_state(States.MOVE_TO_POSITION, "heard_hit")
	return StateMachine.Result.CONTINUE

func condition_heard_help_call(_delta) -> StateMachine.Result:
	if (heard_help_call):
		heard_help_call = false
		previous_position = Vector3.ZERO
		return state.change_state(States.MOVE_TO_POSITION, "heard_help_call")
	return StateMachine.Result.CONTINUE

func condition_have_detected_position(_delta) -> StateMachine.Result:
	if (detected_position == Vector3.ZERO):
		return state.change_state(States.IDLE, "have_detected_position")
	return StateMachine.Result.CONTINUE

func condition_continue_to_position(_delta) -> StateMachine.Result:
	if (position.distance_to(detected_position) < 0.1):
		return state.change_state(States.IDLE, "continue_to_position")
	return StateMachine.Result.CONTINUE

func condition_escape_is_blocked(_delta) -> StateMachine.Result:
	if (position.distance_to(previous_position) < 0.02):
		is_blocked_count = 0
		current_detection_angle = 90
		return state.change_state(States.IDLE, "escape_is_blocked")
	return StateMachine.Result.CONTINUE

func condition_escape_stop(_delta) -> StateMachine.Result:
	if (randf() < 0.005):
		return state.change_state(States.MOVE_TO_POSITION, "escape_stop")
	return StateMachine.Result.CONTINUE

func condition_is_blocked(_delta) -> StateMachine.Result:
	var distance = position.distance_to(previous_position)
	if (distance < 0.02):
		is_blocked_count += 1
		var nearest_points:Array[Tools.NearestPath] = []
		for path:Path3D in get_parent().find_children("", "EnemyEscapePath"):
			nearest_points.push_back(Tools.NearestPath.new(path, path.curve.get_closest_point(position)))
		escape_position = Tools.get_nearest_path(position, nearest_points)
		previous_position = Vector3.ZERO
		if (position.distance_to(escape_position.nearest) > escape_position.path.escape_distance):
			return state.change_state(States.IDLE, "is_blocked (escape_distance)")
		var pos = escape_position.nearest
		pos.y = position.y
		look_at(pos)
		return state.change_state(States.ESCAPE_TO_POSITION, "is_blocked")
	elif (is_blocked_count > (is_blocked_count_trigger * 1.5)):
		is_blocked_count = 0
	return StateMachine.Result.CONTINUE
#endregion

#region Actions Block
func action_start(_delta):
	super.action_start(_delta)
	state.change_state(States.IDLE, "start")

func action_attack_player(_delta) -> StateMachine.Result:
	#print("%s attack player" % name)
	anim.play(ANIM_ATTACK, 0.2, attack_animation_scale)
	timer_attack_cooldown.start()
	attack_cooldown = true
	attack_allowed = true
	getaround_count = 0
	return StateMachine.Result.CONTINUE

func action_move_to_player(_delta) -> StateMachine.Result:
	if (anim.current_animation != ANIM_RUN):
		_stop_idle_rotation()
		blocked_count = 0
		getaround_count = 0
		#print("%s move to player from %s" % [name, anim.current_animation])
		anim.play(ANIM_RUN, 0.2)
		anim.seek(randf())
	detected_position = GameState.player.position
	look_at(detected_position)
	velocity = -transform.basis.z * running_speed
	_check_stairs()
	previous_position = position
	move_and_slide()
	return StateMachine.Result.CONTINUE

func action_idle(_delta):
	if (anim.current_animation != ANIM_IDLE):
		#print("%s idle from %s" % [name, anim.current_animation])
		anim.play(ANIM_IDLE, 0.2)
		anim.seek(randf())
	else:
		_idle_rotation(randf_range(-45, 45), 10)

func action_move_to_detected_position(_delta):
	if (anim.current_animation != ANIM_RUN):
		_stop_idle_rotation()
		blocked_count = 0
		#print("%s move to position from %s" % [name, anim.current_animation])
		anim.play(ANIM_RUN, 0.2)
		anim.seek(randf())
	look_at(detected_position)
	velocity = -transform.basis.z * running_speed
	_check_stairs()
	previous_position = position
	move_and_slide()

func action_move_to_escape_position(_delta):
	if (anim.current_animation != ANIM_RUN):
		_stop_idle_rotation()
		blocked_count = 0
		getaround_count = 0
		#print("%s move to escape position from %s" % [name, anim.current_animation])
		anim.play(ANIM_RUN, 0.2)
		anim.seek(randf())
	var pos = escape_position.nearest
	pos.y = position.y
	if (position.distance_to(pos) < 0.5):
		var nearest_offset = escape_position.path.curve.get_closest_offset(position) + escape_direction
		var _max = escape_position.path.curve.get_baked_length()
		if (nearest_offset >= _max):
			nearest_offset = 1.0
		elif (nearest_offset <= 0):
			nearest_offset = _max - 1.0
		escape_position.nearest = escape_position.path.curve.sample_baked(nearest_offset)
	else:
		look_at(pos)
		velocity = -transform.basis.z * running_speed
		previous_position = position
		move_and_slide()
#endregion

#region Private Methods

func _idle_rotation(angle, time):
	if ((idle_rotation_tween == null) or (not idle_rotation_tween.is_valid())) and (randf() < 0.5):
		idle_rotation_tween = get_tree().create_tween()
		idle_rotation_tween.tween_property(
			self, # target
			"rotation_degrees:y", # target property
			rotation_degrees.y+angle, # end value
			time # animation time length
		)

func _stop_idle_rotation():
	if (idle_rotation_tween != null) and (idle_rotation_tween.is_valid()):
		idle_rotation_tween.kill()

func _check_stairs():
	if (detected_position.y > position.y):
		for index in range(get_slide_collision_count()):
			var collision = get_slide_collision(index)
			if (collision == null): return
			var collider = collision.get_collider()
			if collider.is_in_group("stairs"):
				velocity.y = 5

func hit(hit_by:ItemWeapon):
	super.hit(hit_by)
	look_at(GameState.player.position)
	velocity = Vector3.ZERO
	if (hit_points < (progress_hp.max_value * 0.25)) and (not help_called) and (randf() < 0.25):
		help_called = true
		NotificationManager.call_for_help(self)
	is_blocked_count = 0

func _on_new_hit(target:Node3D, _weapon:ItemWeapon, _damage_points:int, positive:bool):
	if positive and (target != self) and (position.distance_to(target.position) < detection_distance):
		if (randf() < 0.2):
			getaround_count = 0
			detected_position = target.position
			heard_hit = true

func _on_call_for_help(sender:Node3D):
	if (sender is NPCEnemy) and (sender != self) and (position.distance_to(sender.position) < (detection_distance * 2.0)):
		if (randf() < 0.2):
			getaround_count = 0
			detected_position = sender.position
			heard_help_call = true

func _on_timer_attack_timeout():
	attack_cooldown = false

func _on_item_hit(node:Node3D):
	if (attack_allowed):
		attack_allowed = false
		if (node is Player):
			node.hit(weapon)

func _on_player_moving():
	if (player_detected) :
		getaround_count = 0
		is_blocked_count = 0

#endregion
