class_name StateMachine extends Object

enum Result {
	STOP,
	CONTINUE,
}

var states:Dictionary
var states_names:Array[String]
var state:int = 0
var node:Node

func _init(_node:Node3D, _states:Dictionary, _states_names:Array[String]):
	states = _states
	states_names = _states_names
	node = _node

func change_state(new_state:int, _from:String) -> StateMachine.Result:
	#print("%s %s (%s) from %s " % [node.name, states_names[new_state], from, states_names[state]])
	state = new_state
	return StateMachine.Result.STOP

func execute(delta):
	for block:Callable in states[state]:
		var result = block.call(delta)
		if (result == StateMachine.Result.STOP):
			return
