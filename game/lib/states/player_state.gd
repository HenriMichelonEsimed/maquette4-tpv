class_name PlayerState extends State

var zone_name:String = "level_1/level_1"
var position:Vector3 = Vector3.ZERO
var rotation:Vector3 = Vector3.ZERO
var current_item_type:Item.ItemType = Item.ItemType.ITEM_UNKNOWN
var current_item_key:String = ""
var nickname:String = "Player"
var sex:bool = true
#var char:String = "player_0"
var xp:int = 0
var xp_next_level = 1000
var hp:int = 100
var hp_max:int = 100
var endurance:int = 1000
var endurance_max:int = 1000

func _init():
	super("player")
