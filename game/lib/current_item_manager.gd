extends Node


var previous_items:Array = [ null, null ]

func _ready():
	NotificationManager.connect("use_item", use)
	NotificationManager.connect("unuse_item", unuse)

func use(item:Item, slot:Item.ItemSlot):
	unuse(slot)
	GameState.current_item[slot] = item.dup()
	if (item is ItemMultiple):
		GameState.current_item[slot].quantity = 1
	GameState.inventory.remove(GameState.current_item[slot])
	GameState.player.handle_item(slot)
	GameState.current_item[slot].use()
	previous_items[slot] = null

func unuse(slot:Item.ItemSlot):
	if (GameState.current_item[slot] == null): return
	GameState.player.unhandle_item(slot)
	GameState.current_item[slot].unuse()
	previous_items[slot] = GameState.current_item[slot].dup()
	GameState.inventory.add(previous_items[slot])
	GameState.current_item[slot] = null
