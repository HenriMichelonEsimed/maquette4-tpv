class_name InteractiveCharacter extends CharacterBody3D

@export var label:String

signal trade(char:InteractiveCharacter)
signal talk(char:InteractiveCharacter,phrase:String, answers:Array)
signal end_talk()

var discussion:Array
var current:Array
var items_list:Array
var items:ItemsCollection
var generate_chance:int

func _init(disc = ["Hello !", [["Bye", _end, true]] ], it = [], gen_chance = 0):
	discussion = disc
	items_list = it
	if (not items_list.is_empty()):
		items = ItemsCollection.new()
		generate_chance = gen_chance
		var rng = RandomNumberGenerator.new()
		if (rng.randi_range(0, generate_chance) == 1) or (items.count() == 0):
			for item in items_list:
				var max_qty = 1
				if (item.size() == 3):
					max_qty = item[2]
					var have_item = items.getitem(item[0], item[1])
					if (have_item != null):
						max_qty -= have_item.quantity
				var qty = rng.randi_range(0, max_qty)
				if (qty > 0):
					items.new(item[0], item[1], qty)

func _ready():
	set_collision_layer_value(Consts.LAYER_INTERACTIVE_CHARACTER, true)

func interact(new_disc=null):
	GameState.player.look_at_char(self)
	var disc = discussion
	if (new_disc != null):
		disc = new_disc
	say(disc)

func _trade():
	trade.emit(self)

func _start_talking():
	pass

func say(disc):
	_start_talking()
	current = disc
	var phrase = current[0]
	var answr = current[1]
	if phrase is Array:
		var fun = phrase[1]
		if (phrase.size() == 3):
			var param = phrase[2]
			fun.call(param)
		else:
			fun.call()
		phrase = phrase[0]
	talk.emit(self, phrase, answr)

func _end():
	end_talk.emit()
	
func answer(index:int):
	var next = current[1][index]
	if (next is Callable):
		next = next.call()
	next = next[1]
	if (next is Callable):
		next = next.call()
	if next is Array:
		say(next)

func _to_string():
	return label
