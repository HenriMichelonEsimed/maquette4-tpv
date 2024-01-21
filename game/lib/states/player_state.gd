class_name PlayerState extends State

var zone_name:String = "level_test/level_test"
var position:Vector3 = Vector3.ZERO
var rotation:Vector3 = Vector3.ZERO
var camera_view:CameraPivot.CameraView = CameraPivot.CameraView.CAMERA_TPV
var current_item_type:Array[Item.ItemType] = [ Item.ItemType.ITEM_UNKNOWN, Item.ItemType.ITEM_UNKNOWN ]
var current_item_key:Array[String] = [ "", "" ]
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
