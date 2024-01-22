extends Node3D

var zones:ZonesManager = ZonesManager.new()

func _ready():
	GameState.player = $Player
	GameState.ui = $MainUI
	TranslationServer.set_locale(GameState.settings.lang)
	NotificationManager.connect("xp_gain", xp_gain)
	if (GameState.current_item[Item.ItemSlot.SLOT_RIGHT_HAND] != null):
		var item = GameState.current_item[Item.ItemSlot.SLOT_RIGHT_HAND]
		GameState.current_item[Item.ItemSlot.SLOT_RIGHT_HAND] = null
		CurrentItemManager.use(item, Item.ItemSlot.SLOT_RIGHT_HAND)
	else:
		pass
		#CurrentItemManager.use(Tools.load_item(Item.ItemType.ITEM_WEAPONS, "short_sword_1"), Item.ItemSlot.SLOT_RIGHT_HAND)
	if (GameState.current_item[Item.ItemSlot.SLOT_LEFT_HAND] != null):
		var item = GameState.current_item[Item.ItemSlot.SLOT_LEFT_HAND]
		GameState.current_item[Item.ItemSlot.SLOT_LEFT_HAND] = null
		CurrentItemManager.use(item, Item.ItemSlot.SLOT_LEFT_HAND)
	else:
		pass
		#CurrentItemManager.use(Tools.load_item(Item.ItemType.ITEM_WEAPONS, "shield_1"), Item.ItemSlot.SLOT_LEFT_HAND)
	GameState.quests.start("main")
	zones.change_zone(self, GameState.player_state.zone_name)
	if (GameState.player_state.position != Vector3.ZERO):
		GameState.player.move(GameState.player_state.position, GameState.player_state.rotation)
	GameState.game_started = true
	GameState.player_state.hp = GameState.player_state.hp_max
	GameState.player_state.endurance = GameState.player_state.endurance_max

func xp_gain(xp:int):
	GameState.player_state.xp += xp
	GameState.ui.display_xp()
