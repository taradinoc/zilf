"Item definitions and lookup tables"

<CONSTANT ITEMKIND-FOOD 1>
<CONSTANT ITEMKIND-TREASURE 2>
<CONSTANT ITEMKIND-WEAPON 3>
<CONSTANT ITEMKIND-POTION 4>
<CONSTANT ITEMKIND-GOLD 5>
<CONSTANT ITEMKIND-KEY 6>
<CONSTANT ITEMKIND-LOCKEDDOOR 7>
<CONSTANT ITEMKIND-SHRINE 8>
<CONSTANT ITEMKIND-COUNT 8>

<CONSTANT LOCK-GOLDEN 1>
<CONSTANT LOCK-SILVER 2>
<CONSTANT LOCK-BRONZE 3>
<CONSTANT LOCK-COPPER 4>
<CONSTANT LOCK-NICKEL 5>
<CONSTANT LOCK-TYPE-COUNT 5>

<CONSTANT WEAPON-DAGGER 1>
<CONSTANT WEAPON-KATANA 2>
<CONSTANT WEAPON-WARAXE 3>
<CONSTANT WEAPON-SCYTHE 4>
<CONSTANT WEAPON-CUDGEL 5>
<CONSTANT WEAPON-HAMMER 6>
<CONSTANT WEAPON-COUNT 6>

<CONSTANT POTION-MUSCLE 1>
<CONSTANT POTION-HEALTH 2>
<CONSTANT POTION-HIDING 3>
<CONSTANT POTION-POISON 4>
<CONSTANT POTION-VISION 5>
<CONSTANT POTION-MOTION 6>
<CONSTANT POTION-SHADOW 7>
<CONSTANT POTION-METTLE 8>
<CONSTANT POTION-TYPE-COUNT 8>

<CONSTANT POTCOLOR-ARGENT 1>
<CONSTANT POTCOLOR-BLUISH 2>
<CONSTANT POTCOLOR-MAROON 3>
<CONSTANT POTCOLOR-VIOLET 4>
<CONSTANT POTCOLOR-ORANGE 5>
<CONSTANT POTCOLOR-YELLOW 6>
<CONSTANT POTCOLOR-INDIGO 7>
<CONSTANT POTCOLOR-SALMON 8>
<CONSTANT POTION-COLOR-COUNT 8>

<CONSTANT FOOD-BANANA 1>
<CONSTANT FOOD-CHEESE 2>
<CONSTANT FOOD-GRAPES 3>
<CONSTANT FOOD-MUFFIN 4>
<CONSTANT FOOD-TURKEY 5>
<CONSTANT FOOD-CARROT 6>
<CONSTANT FOOD-CAVIAR 7>
<CONSTANT FOOD-TYPE-COUNT 7>

<CONSTANT TREASURE-AMULET 1>
<CONSTANT TREASURE-SCARAB 2>
<CONSTANT TREASURE-GOBLET 3>
<CONSTANT TREASURE-IOLITE 4>
<CONSTANT TREASURE-GARNET 5>
<CONSTANT TREASURE-JASPER 6>
<CONSTANT TREASURE-ZIRCON 7>
<CONSTANT TREASURE-POSTER 8>
<CONSTANT TREASURE-TROPHY 9>
<CONSTANT TREASURE-COUNT 9>

;"Maps a weapon type code to its display name.

Args:
  TYPE: Weapon type code (WEAPON-*).

Returns:
  Lowercase name string."

<ROUTINE WEAPON-NAME (TYPE)
    <COND (<==? .TYPE ,WEAPON-DAGGER> "dagger")
          (<==? .TYPE ,WEAPON-KATANA> "katana")
          (<==? .TYPE ,WEAPON-WARAXE> "waraxe")
          (<==? .TYPE ,WEAPON-SCYTHE> "scythe")
          (<==? .TYPE ,WEAPON-CUDGEL> "cudgel")
          (<==? .TYPE ,WEAPON-HAMMER> "hammer")
          (ELSE "weapon")>>

;"Maps a weapon type code to its base damage.

Args:
  TYPE: Weapon type code (WEAPON-*).

Returns:
  Positive integer base damage."

<ROUTINE WEAPON-BASE-DMG (TYPE)
    <COND (<==? .TYPE ,WEAPON-DAGGER> 2)
          (<==? .TYPE ,WEAPON-KATANA> 3)
          (<==? .TYPE ,WEAPON-WARAXE> 4)
          (<==? .TYPE ,WEAPON-SCYTHE> 4)
          (<==? .TYPE ,WEAPON-CUDGEL> 3)
          (<==? .TYPE ,WEAPON-HAMMER> 5)
          (ELSE 2)>>

;"Maps a weapon type code to its crit chance.

Design notes:
  - Dagger + waraxe have the highest crit chance.
  - Other weapons trade predictability/variance against crit odds.

Args:
  TYPE: Weapon type code (WEAPON-*).

Returns:
  Crit chance percentage (0..100)."

<ROUTINE WEAPON-CRIT-PCT (TYPE)
    <COND (<==? .TYPE ,WEAPON-DAGGER> 25)
          (<==? .TYPE ,WEAPON-WARAXE> 22)
          (<==? .TYPE ,WEAPON-CUDGEL> 12)
          (<==? .TYPE ,WEAPON-KATANA> 8)
          (<==? .TYPE ,WEAPON-SCYTHE> 6)
          (<==? .TYPE ,WEAPON-HAMMER> 5)
          (ELSE 10)>>

;"Maps a weapon type code to its damage variance behavior.

The caller computes POWER = base + level, then RANGE is derived from POWER:
  - DIV=1  => RANGE = POWER (very swingy)
  - DIV=2+ => RANGE = max(2, POWER/DIV) (more predictable)

Args:
  TYPE: Weapon type code (WEAPON-*).

Returns:
  Positive integer divisor (>= 1)."

<ROUTINE WEAPON-VARIANCE-DIV (TYPE)
    <COND (<==? .TYPE ,WEAPON-DAGGER> 2)
          (<==? .TYPE ,WEAPON-KATANA> 4)
          (<==? .TYPE ,WEAPON-WARAXE> 1)
          (<==? .TYPE ,WEAPON-SCYTHE> 1)
          (<==? .TYPE ,WEAPON-CUDGEL> 5)
          (<==? .TYPE ,WEAPON-HAMMER> 3)
          (ELSE 3)>>

;"Maps a food type code to its display name.

Args:
  TYPE: Food type code (FOOD-*).

Returns:
  Lowercase name string."

<ROUTINE FOOD-NAME (TYPE)
    <COND (<==? .TYPE ,FOOD-BANANA> "banana")
          (<==? .TYPE ,FOOD-CHEESE> "cheese")
          (<==? .TYPE ,FOOD-GRAPES> "grapes")
          (<==? .TYPE ,FOOD-MUFFIN> "muffin")
          (<==? .TYPE ,FOOD-TURKEY> "turkey")
          (<==? .TYPE ,FOOD-CARROT> "carrot")
          (<==? .TYPE ,FOOD-CAVIAR> "caviar")
          (ELSE "food")>>

;"Maps a food type code to its map sprite.

Args:
  TYPE: Food type code (FOOD-*).

Returns:
  ZSCII tile constant (TILE-*)."

<ROUTINE FOOD-TILE-FOR-TYPE (TYPE)
    <COND (<==? .TYPE ,FOOD-BANANA> ,TILE-BANANA)
          (<==? .TYPE ,FOOD-CHEESE> ,TILE-CHEESE)
          (<==? .TYPE ,FOOD-GRAPES> ,TILE-GRAPES)
          (<==? .TYPE ,FOOD-MUFFIN> ,TILE-MUFFIN)
          (<==? .TYPE ,FOOD-TURKEY> ,TILE-TURKEY)
          (<==? .TYPE ,FOOD-CARROT> ,TILE-CARROT)
          (<==? .TYPE ,FOOD-CAVIAR> ,TILE-CAVIAR)
          (ELSE ,TILE-MUFFIN)>>

;"Maps a food type code to its healing amount.

Args:
  TYPE: Food type code (FOOD-*).

Returns:
  Positive integer healing amount (HP)."

<ROUTINE FOOD-HEAL-AMT (TYPE)
    <COND (<==? .TYPE ,FOOD-BANANA> 2)
          (<==? .TYPE ,FOOD-CHEESE> 3)
          (<==? .TYPE ,FOOD-GRAPES> 1)
          (<==? .TYPE ,FOOD-MUFFIN> 4)
          (<==? .TYPE ,FOOD-TURKEY> 6)
          (<==? .TYPE ,FOOD-CARROT> 4)
          (<==? .TYPE ,FOOD-CAVIAR> 25)
          (ELSE 2)>>

;"Maps a food type code to the trader buy price.

Args:
  TYPE: Food type code (FOOD-*).

Returns:
  Gold value as a positive integer."

<ROUTINE FOOD-VALUE (TYPE)
    <COND (<==? .TYPE ,FOOD-CARROT> 100)
          (ELSE <+ 10 <* 10 <FOOD-HEAL-AMT .TYPE>>>)>>

;"Maps a potion effect/type code to its display name.

Args:
  TYPE: Potion type code (POTION-*).

Returns:
  Lowercase name string (e.g. \"potion of health\")."

<ROUTINE POTION-TYPE-NAME (TYPE)
    <COND (<==? .TYPE ,POTION-MUSCLE> "potion of muscle")
          (<==? .TYPE ,POTION-HEALTH> "potion of health")
          (<==? .TYPE ,POTION-HIDING> "potion of hiding")
          (<==? .TYPE ,POTION-POISON> "potion of poison")
          (<==? .TYPE ,POTION-VISION> "potion of vision")
          (<==? .TYPE ,POTION-MOTION> "potion of motion")
          (<==? .TYPE ,POTION-SHADOW> "potion of shadow")
          (<==? .TYPE ,POTION-METTLE> "potion of mettle")
          (ELSE "potion")>>

;"Maps a potion color code to its (concealed) display name.

Args:
  COLOR: Potion color code (POTCOLOR-*).

Returns:
  Name string like \"violet potion\"."

<ROUTINE POTION-COLOR-NAME (COLOR)
    <COND (<==? .COLOR ,POTCOLOR-ARGENT> "argent potion")
          (<==? .COLOR ,POTCOLOR-BLUISH> "bluish potion")
          (<==? .COLOR ,POTCOLOR-MAROON> "maroon potion")
          (<==? .COLOR ,POTCOLOR-VIOLET> "violet potion")
          (<==? .COLOR ,POTCOLOR-ORANGE> "orange potion")
          (<==? .COLOR ,POTCOLOR-YELLOW> "yellow potion")
          (<==? .COLOR ,POTCOLOR-INDIGO> "indigo potion")
          (<==? .COLOR ,POTCOLOR-SALMON> "salmon potion")
          (ELSE "potion")>>

;"Maps a potion color code to the name of the color.

Args:
  COLOR: Potion color code (POTCOLOR-*).

Returns:
  Color name string like \"violet\"."
<ROUTINE POTION-BARE-COLOR-NAME (COLOR)
    <COND (<==? .COLOR ,POTCOLOR-ARGENT> "argent")
          (<==? .COLOR ,POTCOLOR-BLUISH> "bluish")
          (<==? .COLOR ,POTCOLOR-MAROON> "maroon")
          (<==? .COLOR ,POTCOLOR-VIOLET> "violet")
          (<==? .COLOR ,POTCOLOR-ORANGE> "orange")
          (<==? .COLOR ,POTCOLOR-YELLOW> "yellow")
          (<==? .COLOR ,POTCOLOR-INDIGO> "indigo")
          (<==? .COLOR ,POTCOLOR-SALMON> "salmon")
          (ELSE "colored")>>

;"Returns the player-facing name for a potion of the given color.

Before a color is discovered, this returns the concealed color name. After it
is discovered, it returns the underlying potion type name.

Args:
  COLOR: Potion color code (POTCOLOR-*).

Returns:
  Name string for UI/logging."

<ROUTINE POTION-DISPLAY-NAME (COLOR "AUX" TYPE)
    <COND (<OR <L? .COLOR 1> <G? .COLOR ,POTION-COLOR-COUNT>>
           <RETURN "potion">)>
    <COND (<G? <GETB ,POTION-DISCOVERED <- .COLOR 1>> 0>
           <SET TYPE <GETB ,POTION-TYPE-FOR-COLOR <- .COLOR 1>>>
           <POTION-TYPE-NAME .TYPE>)
          (ELSE <POTION-COLOR-NAME .COLOR>)>>

;"Returns the correct indefinite article (a vs an) for the player-facing
  potion name of the given color.

We only need special handling for undiscovered color names like argent potion;
once discovered, the name becomes potion of ...

Args:
  COLOR: Potion color code (POTCOLOR-*).

Returns:
  The string a or an."

<ROUTINE POTION-ARTICLE (COLOR)
    <COND (<OR <L? .COLOR 1> <G? .COLOR ,POTION-COLOR-COUNT>> <RETURN "a">)>
    <COND (<G? <GETB ,POTION-DISCOVERED <- .COLOR 1>> 0> <RETURN "a">)>
    <COND (<OR <==? .COLOR ,POTCOLOR-ARGENT>
               <==? .COLOR ,POTCOLOR-ORANGE>
               <==? .COLOR ,POTCOLOR-INDIGO>>
           "an")
          (ELSE "a")>>

;"Maps a treasure ID to its display name.

Args:
  ID: Treasure ID (TREASURE-*).

Returns:
  Lowercase name string."

<ROUTINE TREASURE-NAME (ID)
    <COND (<==? .ID ,TREASURE-AMULET> "amulet")
          (<==? .ID ,TREASURE-SCARAB> "scarab")
          (<==? .ID ,TREASURE-GOBLET> "goblet")
          (<==? .ID ,TREASURE-IOLITE> "iolite")
          (<==? .ID ,TREASURE-GARNET> "garnet")
          (<==? .ID ,TREASURE-JASPER> "jasper")
          (<==? .ID ,TREASURE-ZIRCON> "zircon")
          (<==? .ID ,TREASURE-POSTER> "poster")
          (<==? .ID ,TREASURE-TROPHY> "Trophy of Scryra")
          (ELSE "treasure")>>

;"Maps a treasure ID to the trader buy price.

Args:
  ID: Treasure ID (TREASURE-*).

Returns:
  Gold value as a positive integer."

<ROUTINE TREASURE-VALUE (ID)
    <COND (<==? .ID ,TREASURE-AMULET> 180)
          (<==? .ID ,TREASURE-SCARAB> 200)
          (<==? .ID ,TREASURE-GOBLET> 250)
          (<==? .ID ,TREASURE-IOLITE> 300)
          (<==? .ID ,TREASURE-GARNET> 400)
          (<==? .ID ,TREASURE-JASPER> 600)
          (<==? .ID ,TREASURE-ZIRCON> 1000)
          (<==? .ID ,TREASURE-POSTER> 1500)
          (<==? .ID ,TREASURE-TROPHY> 5000)
          (ELSE 50)>>

;"Maps a key ID to its display name.

Args:
  LOCKTYPE: Key ID (LOCKTYPE-*).

Returns:
  Name string like \"golden key\"."
<ROUTINE KEY-NAME (LOCKTYPE)
    <COND (<==? .LOCKTYPE ,LOCK-GOLDEN> "golden key")
          (<==? .LOCKTYPE ,LOCK-SILVER> "silver key")
          (<==? .LOCKTYPE ,LOCK-BRONZE> "bronze key")
          (<==? .LOCKTYPE ,LOCK-COPPER> "copper key")
          (<==? .LOCKTYPE ,LOCK-NICKEL> "nickel key")
          (ELSE "key")>>

;"Maps a key ID to its metal adjective.

Args:
  LOCKTYPE: Key ID (LOCKTYPE-*).

Returns:
  Adjective string like \"golden\"."
<ROUTINE KEY-BARE-METAL-DESC (LOCKTYPE)
    <COND (<==? .LOCKTYPE ,LOCK-GOLDEN> "golden")
          (<==? .LOCKTYPE ,LOCK-SILVER> "silver")
          (<==? .LOCKTYPE ,LOCK-BRONZE> "bronze")
          (<==? .LOCKTYPE ,LOCK-COPPER> "copper")
          (<==? .LOCKTYPE ,LOCK-NICKEL> "nickel")
          (ELSE "metal")>>


;"Trader buy price for any potion (same regardless of type/color)."

<CONSTANT POTION-VALUE 150>

;"Maps a weapon type+level+enchantment to the trader buy price.

Weapon values scale with their level.

Args:
    TYPE: Weapon type code (WEAPON-*).
    LVL: Weapon level.
    ENCH: Enchantment level.

Returns:
  Gold value as a positive integer."

<ROUTINE WEAPON-VALUE (TYPE LVL ENCH)
    <+ 1 <+ <* 2 <WEAPON-BASE-DMG .TYPE>> <* 4 .LVL> <* 2 .ENCH>>>>
