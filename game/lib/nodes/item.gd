class_name Item extends StaticBody3D

enum ItemType {
	ITEM_UNKNOWN		= -1,
	ITEM_WEAPONS		= 0,
	ITEM_TOOLS			= 1,
	ITEM_CONSUMABLES	= 2,
	ITEM_MISCELLANEOUS	= 3,
	ITEM_QUEST			= 4,
}

enum ItemSlot {
	SLOT_RIGHT_HAND	 = 0,
	SLOT_LEFT_HAND	 = 1
}

@export var key:String
@export var label:String
@export var price:float = 0.0
@export var type:ItemType
@export var preview_scale:float = 1.0

var original_rotation:Vector3
var use_area:Area3D
var initialized:bool = false

func _ready():
	label = tr(label)
	if (not initialized):
		_initialize()
		initialized = true
	set_collision_layer_value(Consts.LAYER_WORLD, false)
	set_collision_mask_value(Consts.LAYER_WORLD, true)
	original_rotation = rotation
	enable()
	use_area = get_node_or_null("Area3D")
	if (use_area != null):
		use_area.set_collision_layer_value(Consts.LAYER_WORLD, false)
		use_area.set_collision_mask_value(Consts.LAYER_NPC, true)
		use_area.set_collision_mask_value(Consts.LAYER_WORLD, false)

func use():
	disable()
	position = Vector3.ZERO
	scale = Vector3(100.0,100.0,100.0) # compensate mixamo/CC chars scale

func unuse():
	position = Vector3.ZERO
	rotation = original_rotation
	scale = Vector3(1.0,1.0,1.0)
	enable()

func _initialize():
	pass

func collect():
	return true

func dup():
	var d = duplicate(DUPLICATE_SCRIPTS)
	d.original_rotation = original_rotation
	return d

func disable():
	set_collision_layer_value(Consts.LAYER_ITEM, false)

func enable():
	set_collision_layer_value(Consts.LAYER_ITEM, true)

func is_enabled():
	return get_collision_layer_value(Consts.LAYER_ITEM)
	
func _to_string():
	return label

