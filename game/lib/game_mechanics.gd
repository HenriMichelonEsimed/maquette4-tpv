class_name GameMechanics extends Object

const ATTACK_COOLDOWN:Array[float] = [ 0.3, 0.6, 0.9, 1.3, 1.6, 1.9, 2.3, 2.6 ]
const ANIM_SCALE:Array[float] = [ 3.3, 1.68, 1.11, 0.77, 0.625, 0.525, 0.435, 0.385 ] # base : 1.0 secondes

static func attack_cooldown(speed:int) -> float:
	return ATTACK_COOLDOWN[speed-1] - 0.1
	
static func anim_scale(speed:int) -> float:
	return ANIM_SCALE[speed-1]
