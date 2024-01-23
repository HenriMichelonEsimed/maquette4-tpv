class_name NPC extends CharacterBody3D

#region Anims
const ANIM_IDLE = "idle"
const ANIM_WALK = "walk"
const ANIM_RUN = "run"
const ANIM_ATTACK = "attack"
const ANIM_DIE = "die"
const ANIM_HIT = "hit"
#endregion

#region Exports
@export var label:String = "Enemy"
@export var height:float = 0.0
#endregion

#region Nodes
@onready var hit_points_roll:DicesRoll = $HitPoints
@onready var collision_shape:CollisionShape3D = $CollisionShape3D
@onready var anim:AnimationPlayer = $AnimationPlayer
#endregion

#region Properties
var state:StateMachine
# enemy info label & HP display
var player_in_info_area:bool = false
var label_info:Label = Label.new()
var progress_hp:ProgressBar = ProgressBar.new()
# current HP
var hit_points:int = 100
# movements speed
var walking_speed:float = 0.5
var running_speed:float = 1.0
# distance info label & HP are displayed
var info_distance:float = 5
# player distance from node
var player_distance:float = 0.0
#endregion

func _ready():
	connect("input_event", _on_input_event)
	hit_points = hit_points_roll.roll()
	set_collision_layer_value(Consts.LAYER_NPC, true)
	if (height == 0) and (collision_shape.shape is CylinderShape3D):
		height = collision_shape.shape.height
	label_info.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label_info.visible = false
	get_tree().root.call_deferred("add_child", label_info)
	progress_hp.max_value = hit_points
	progress_hp.value = hit_points
	progress_hp.show_percentage = false
	progress_hp.size.x = 50
	progress_hp.modulate = Color.RED
	get_tree().root.call_deferred("add_child", progress_hp)
	_update_info()
	label_info.text = "%s" % label
	#GameState.player.connect("moving", _on_player_moving)

func _physics_process(delta):
	state.execute(delta)

#region Conditions Block
func condition_preconditions(_delta) -> StateMachine.Result:
	player_distance = global_position.distance_to(GameState.player.global_position)
	# update info label content 
	player_in_info_area = player_distance < info_distance
	_update_label_info_position()
	return StateMachine.Result.CONTINUE

#region Actions Block
func action_start(_delta):
	anim.play(ANIM_IDLE)
	anim.seek(randf()*10.0)

func action_idle(_delta):
	if (anim.current_animation != ANIM_IDLE):
		anim.play(ANIM_IDLE, 0.2)
		anim.seek(randf())

#region Private Methods
func _to_string():
	return label

func _update_info():
	if (label_info == null): return
	progress_hp.value = hit_points
	_update_label_info_position()

func _update_label_info_position():
	if (label_info == null): return
	#label_info.visible = player_in_info_area
	#progress_hp.visible = label_info.visible
	if (label_info.visible):
		var pos:Vector3 = global_position
		pos.y += height
		var pos2d:Vector2 = get_viewport().get_camera_3d().unproject_position(pos)
		progress_hp.position = pos2d
		progress_hp.position.x -= progress_hp.size.x / 2
		progress_hp.position.y -= progress_hp.size.y/2
		label_info.position = pos2d
		label_info.position.x -= label_info.size.x / 2
		label_info.position.y -= label_info.size.y + progress_hp.size.y
		#label_info.add_theme_font_size_override("font_size", 14 - GameState.camera.size / 10)

func hit(hit_by:ItemWeapon):
	var damage_points = min(hit_by.damages_roll.roll(), hit_points)
	hit_points -= damage_points
	_update_info()
	var pos = label_info.position
	pos.x += label_info.size.x / 2
	NotificationManager.hit(self, hit_by, damage_points)
	anim.play(ANIM_HIT if hit_points > 0 else ANIM_DIE)

func _on_input_event(_camera, event, _position, _normal, _shape_idx):
	if (event is InputEventMouseButton) and (event.button_index == MOUSE_BUTTON_MIDDLE) and not(event.pressed):
		Tools.load_dialog(self, Tools.DIALOG_ENEMY_INFO, GameState.resume_game).open(self)

func _on_animation_tree_animation_finished(anim_name):
	if (anim_name == "undead/react_death_backward_1"):
		$AnimationPlayer.queue_free()
		process_mode = Node.PROCESS_MODE_DISABLED

func _on_player_moving():
	pass

#endregion
