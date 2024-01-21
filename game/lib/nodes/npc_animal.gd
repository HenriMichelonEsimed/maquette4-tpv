class_name NPCAnimal extends NPC

#region StateMachine
enum States {
	STARTING,
	DEAD,
	PLAYER_DEAD,
	IDLE,
}

const STATES_NAMES:Array[String] = [
	"STARTING",
	"DEAD",
	"PLAYER_DEAD",
	"IDLE",
]

var states:Dictionary = {
	States.STARTING: [ action_start ],
	States.DEAD: [],
	States.PLAYER_DEAD: [ action_idle ],
	States.IDLE: [
		condition_preconditions,
		action_idle
	],
}
#endregion

func _ready():
	super._ready()
	state = StateMachine.new(self, states, STATES_NAMES)

#region Conditions Block
func condition_preconditions(_delta) -> StateMachine.Result:
	if (condition_death(_delta) == StateMachine.Result.STOP): return StateMachine.Result.STOP
	if (condition_player_dead(_delta) == StateMachine.Result.STOP): return StateMachine.Result.STOP
	return super.condition_preconditions(_delta)

func condition_death(_delta) -> StateMachine.Result:
	if (hit_points <= 0):
		label_info.queue_free()
		progress_hp.queue_free()
		$CollisionShape3D.queue_free()
		label_info = null
		player_in_info_area = false
		return state.change_state(States.DEAD, "death")
	return StateMachine.Result.CONTINUE

func condition_player_dead(_delta) -> StateMachine.Result:
	if (GameState.player_state.hp <= 0):
		return state.change_state(States.PLAYER_DEAD, "player_dead")
	return StateMachine.Result.CONTINUE
#endregion

#region Actions Block
func action_start(_delta):
	super.action_start(_delta)
	state.change_state(States.IDLE, "start")
#endregion

#region Private Methods

#endregion
