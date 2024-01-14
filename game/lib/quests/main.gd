class_name MainQuest extends Quest

func _init():
	super("main", QMain0.new(), "Main Quest",
	{
		"lvl0_make_first_purchase":
			[""]
	})

func _on_new_quest_event(type:Quest.QuestEventType, _event_key:String):
	if (not have_advpoint("lvl0_make_first_purchase")) and (type == Quest.QuestEventType.QUESTEVENT_BUY):
		add_advpoint("lvl0_make_first_purchase")
