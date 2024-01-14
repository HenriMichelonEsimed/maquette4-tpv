extends Node

var recipes = {}
var ingredients:Dictionary = {}

func add_recipe(recipe:String, _ingredients:Array):
	recipes[recipe] = _ingredients
	_refresh()

func _refresh():
	ingredients.clear()
	for target in recipes:
		var target_ingredients = recipes[target][1]
		for ingredient in target_ingredients:
			if ingredients.has(ingredient) and ingredients[ingredient].find(target) == -1:
				ingredients[ingredient].push_back(target)
			else:
				ingredients[ingredient] = [ target ]
	pass
