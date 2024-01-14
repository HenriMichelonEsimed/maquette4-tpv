class_name Storage extends Usable

signal open(node:Storage)

func _init(_save:bool=false):
	super(true)
	
func get_items() -> Array:
	return find_children("*", "Item", true, false)

func _on_child_entered_tree(node:Node):
	if (node is Item):
		node.disable()
	
func _ready():
	super._ready()
	connect("child_entered_tree", _on_child_entered_tree)
	for item:Node in find_children("*", "Item", true, true):
		item.disable()
	
func _use():
	if (is_used):
		open.emit(self)
