"Balance Tuning Constants"

"Player Stats"

;"Initial player strength"
<CONSTANT INITIAL-PLAYER-STR 2>
;"Initial player defense"
<CONSTANT INITIAL-PLAYER-DEF 1>
;"Initial player [max] HP"
<CONSTANT INITIAL-PLAYER-MAX-HP 12>

;"The X in (X*DEF)/(Y+DEF)"
<CONSTANT DEFENSE-FORMULA-NUMERATOR 75>
;"The Y in (X*DEF)/(Y+DEF)"
<CONSTANT DEFENSE-FORMULA-DENOMINATOR 10>

"Dungeon Generation"

;"Percent chance of spawning an interior per floor"
<CONSTANT INTERIOR-ENTRANCE-SPAWN-PCT 33>
;"Percent chance of a nook containing a shrine"
<CONSTANT SHRINE-SPAWN-PCT 25>
;"Bonus added to weapon levels in treasure room"
<CONSTANT TREASURE-LOOT-WEAPON-LEVEL-BONUS 2>
;"Bonus added to weapon enchantments in treasure room"
<CONSTANT TREASURE-LOOT-WEAPON-ENCH-BONUS 2>
;"Minimum amount of gold in treasure room piles (minus 1)"
<CONSTANT TREASURE-LOOT-GOLD-BASE 200>
;"Maximum additional gold in treasure room piles"
<CONSTANT TREASURE-LOOT-GOLD-VARIANCE 55>
;"Minimum amount of gold in shrine piles (minus 1)"
<CONSTANT SHRINE-OFFER-GOLD-BASE 49>
;"Maximum additional gold in shrine piles"
<CONSTANT SHRINE-OFFER-GOLD-VARIANCE 101>

"Enemy Stats"

;"Maximum number of enemies per floor"
<CONSTANT MAX-ENEMIES 8>
;"Percent chance of spawning a monkey per floor"
<CONSTANT MONKEY-SPAWN-PCT 20>
;"How close an enemy has to be to detect and chase the player"
<CONSTANT CHASE-RADIUS 6>
;"How far the player has to be from the bee hive to escape"
<CONSTANT BEE-SWARM-LOSE-DIST 5>
;"Percent chance of an enemy dropping an item when killed"
<CONSTANT ITEM-ON-KILL-DROP-PCT 16>

;"Floor scaling divisor for goblin"
<CONSTANT ENEMY-SCALING-DIVISOR-EASY 4>
;"Floor scaling divisor for sphinx and wraith"
<CONSTANT ENEMY-SCALING-DIVISOR-MEDIUM 3>
;"Floor scaling divisor for kraken, dragon, and spirit"
<CONSTANT ENEMY-SCALING-DIVISOR-HARD 2>

;"Base damage and base HP for each enemy type"
<CONSTANT ENEMY-BASE-DMG-GOBLIN 1>
<CONSTANT ENEMY-BASE-HP-GOBLIN 3>

<CONSTANT ENEMY-BASE-DMG-SPHINX 2>
<CONSTANT ENEMY-BASE-HP-SPHINX 5>

<CONSTANT ENEMY-BASE-DMG-WRAITH 2>
<CONSTANT ENEMY-BASE-HP-WRAITH 4>

<CONSTANT ENEMY-BASE-DMG-KRAKEN 3>
<CONSTANT ENEMY-BASE-HP-KRAKEN 7>

<CONSTANT ENEMY-BASE-DMG-DRAGON 4>
<CONSTANT ENEMY-BASE-HP-DRAGON 9>

<CONSTANT ENEMY-BASE-DMG-SPIRIT 3>
<CONSTANT ENEMY-BASE-HP-SPIRIT 9>

<CONSTANT ENEMY-BASE-DMG-LEGION 12>

"Inventory"

;"Number of player inventory slots"
<CONSTANT INV-SIZE 10>
;"Number of trader inventory slots"
<CONSTANT TRINV-SIZE 10>

"Potion Effects"

;"Number of turns the potion of hiding lasts"
<CONSTANT HIDING-POTION-DURATION 60>
;"Number of turns the potion of vision lasts"
<CONSTANT VISION-POTION-DURATION 60>
;"Number of turns the potion of shadow lasts"
<CONSTANT SHADOW-POTION-DURATION 75>

"Healing"

<CONSTANT FOOD-HEAL-AMT-BANANA 2>
<CONSTANT FOOD-HEAL-AMT-CHEESE 3>
<CONSTANT FOOD-HEAL-AMT-GRAPES 1>
<CONSTANT FOOD-HEAL-AMT-MUFFIN 4>
<CONSTANT FOOD-HEAL-AMT-TURKEY 6>
<CONSTANT FOOD-HEAL-AMT-CARROT 4>
<CONSTANT FOOD-HEAL-AMT-CAVIAR 25>

"Weapon Stats"

<CONSTANT WEAPON-BASE-DMG-DAGGER 2>
<CONSTANT WEAPON-CRIT-PCT-DAGGER 25>
<CONSTANT WEAPON-VARIANCE-DIV-DAGGER 2>

<CONSTANT WEAPON-BASE-DMG-KATANA 3>
<CONSTANT WEAPON-CRIT-PCT-KATANA 8>
<CONSTANT WEAPON-VARIANCE-DIV-KATANA 4>

<CONSTANT WEAPON-BASE-DMG-WARAXE 4>
<CONSTANT WEAPON-CRIT-PCT-WARAXE 22>
<CONSTANT WEAPON-VARIANCE-DIV-WARAXE 1>

<CONSTANT WEAPON-BASE-DMG-SCYTHE 4>
<CONSTANT WEAPON-CRIT-PCT-SCYTHE 6>
<CONSTANT WEAPON-VARIANCE-DIV-SCYTHE 1>

<CONSTANT WEAPON-BASE-DMG-CUDGEL 3>
<CONSTANT WEAPON-CRIT-PCT-CUDGEL 12>
<CONSTANT WEAPON-VARIANCE-DIV-CUDGEL 5>

<CONSTANT WEAPON-BASE-DMG-HAMMER 5>
<CONSTANT WEAPON-CRIT-PCT-HAMMER 5>
<CONSTANT WEAPON-VARIANCE-DIV-HAMMER 3>

"Scoring"

;"Score bonus for each strength point above initial"
<CONSTANT SCORE-BONUS-PER-STR 100>
;"Score bonus for each defense point above initial"
<CONSTANT SCORE-BONUS-PER-DEF 100>
;"Score bonus for each max HP point above initial"
<CONSTANT SCORE-BONUS-PER-MAXHP 25>
;"Score bonus for winning"
<CONSTANT SCORE-BONUS-WIN 5000>
