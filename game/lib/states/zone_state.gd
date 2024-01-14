class_name ZoneState extends State

var items_removed:PackedStringArray = PackedStringArray()
var items_added:ItemsCollection = ItemsCollection.new(false)

func _init(_name:String, _parent:Node=null):
	super(_name, _parent)
