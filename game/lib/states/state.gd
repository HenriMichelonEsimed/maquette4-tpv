class_name State extends Object

var name:String
var parent:Node

func _init(_name:String, _parent:Node=null):
	self.parent = _parent
	self.name = _name
