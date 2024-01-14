class_name ItemMultiple extends Item

@export var quantity:int = 1

func _to_string():
	if (quantity == 1):
		return label
	return str(quantity) + " x " + tr(label)
