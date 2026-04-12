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

;"Prints a weapon type code as its display name.

Args:
  TYPE: Weapon type code (WEAPON-*).

Returns:
  T."

<ROUTINE PRINT-WEAPON-NAME (TYPE)
    <COND (<==? .TYPE ,WEAPON-DAGGER> <TELL "dagger">)
          (<==? .TYPE ,WEAPON-KATANA> <TELL "katana">)
          (<==? .TYPE ,WEAPON-WARAXE> <TELL "waraxe">)
          (<==? .TYPE ,WEAPON-SCYTHE> <TELL "scythe">)
          (<==? .TYPE ,WEAPON-CUDGEL> <TELL "cudgel">)
          (<==? .TYPE ,WEAPON-HAMMER> <TELL "hammer">)
          (ELSE <TELL "weapon">)>
    <RTRUE>>

;"Maps a weapon type code to its base damage.

Args:
  TYPE: Weapon type code (WEAPON-*).

Returns:
  Positive integer base damage."

<ROUTINE WEAPON-BASE-DMG (TYPE)
    <COND (<==? .TYPE ,WEAPON-DAGGER> ,WEAPON-BASE-DMG-DAGGER)
          (<==? .TYPE ,WEAPON-KATANA> ,WEAPON-BASE-DMG-KATANA)
          (<==? .TYPE ,WEAPON-WARAXE> ,WEAPON-BASE-DMG-WARAXE)
          (<==? .TYPE ,WEAPON-SCYTHE> ,WEAPON-BASE-DMG-SCYTHE)
          (<==? .TYPE ,WEAPON-CUDGEL> ,WEAPON-BASE-DMG-CUDGEL)
          (<==? .TYPE ,WEAPON-HAMMER> ,WEAPON-BASE-DMG-HAMMER)
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
    <COND (<==? .TYPE ,WEAPON-DAGGER> ,WEAPON-CRIT-PCT-DAGGER)
          (<==? .TYPE ,WEAPON-WARAXE> ,WEAPON-CRIT-PCT-WARAXE)
          (<==? .TYPE ,WEAPON-CUDGEL> ,WEAPON-CRIT-PCT-CUDGEL)
          (<==? .TYPE ,WEAPON-KATANA> ,WEAPON-CRIT-PCT-KATANA)
          (<==? .TYPE ,WEAPON-SCYTHE> ,WEAPON-CRIT-PCT-SCYTHE)
          (<==? .TYPE ,WEAPON-HAMMER> ,WEAPON-CRIT-PCT-HAMMER)
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
    <COND (<==? .TYPE ,WEAPON-DAGGER> ,WEAPON-VARIANCE-DIV-DAGGER)
          (<==? .TYPE ,WEAPON-KATANA> ,WEAPON-VARIANCE-DIV-KATANA)
          (<==? .TYPE ,WEAPON-WARAXE> ,WEAPON-VARIANCE-DIV-WARAXE)
          (<==? .TYPE ,WEAPON-SCYTHE> ,WEAPON-VARIANCE-DIV-SCYTHE)
          (<==? .TYPE ,WEAPON-CUDGEL> ,WEAPON-VARIANCE-DIV-CUDGEL)
          (<==? .TYPE ,WEAPON-HAMMER> ,WEAPON-VARIANCE-DIV-HAMMER)
          (ELSE 3)>>

;"Prints a food type code as its display name.

Args:
  TYPE: Food type code (FOOD-*).

Returns:
  T."

<ROUTINE PRINT-FOOD-NAME (TYPE)
    <COND (<==? .TYPE ,FOOD-BANANA> <TELL "banana">)
          (<==? .TYPE ,FOOD-CHEESE> <TELL "cheese">)
          (<==? .TYPE ,FOOD-GRAPES> <TELL "grapes">)
          (<==? .TYPE ,FOOD-MUFFIN> <TELL "muffin">)
          (<==? .TYPE ,FOOD-TURKEY> <TELL "turkey">)
          (<==? .TYPE ,FOOD-CARROT> <TELL "carrot">)
          (<==? .TYPE ,FOOD-CAVIAR> <TELL "caviar">)
          (ELSE <TELL "food">)>
    <RTRUE>>

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
    <COND (<==? .TYPE ,FOOD-BANANA> ,FOOD-HEAL-AMT-BANANA)
          (<==? .TYPE ,FOOD-CHEESE> ,FOOD-HEAL-AMT-CHEESE)
          (<==? .TYPE ,FOOD-GRAPES> ,FOOD-HEAL-AMT-GRAPES)
          (<==? .TYPE ,FOOD-MUFFIN> ,FOOD-HEAL-AMT-MUFFIN)
          (<==? .TYPE ,FOOD-TURKEY> ,FOOD-HEAL-AMT-TURKEY)
          (<==? .TYPE ,FOOD-CARROT> ,FOOD-HEAL-AMT-CARROT)
          (<==? .TYPE ,FOOD-CAVIAR> ,FOOD-HEAL-AMT-CAVIAR)
          (ELSE 2)>>

;"Maps a food type code to the trader buy price.

Args:
  TYPE: Food type code (FOOD-*).

Returns:
  Gold value as a positive integer."

<ROUTINE FOOD-VALUE (TYPE)
    <COND (<==? .TYPE ,FOOD-CARROT> 100)
          (ELSE <+ 10 <* 10 <FOOD-HEAL-AMT .TYPE>>>)>>

;"Prints a potion effect/type code as its display name.

Args:
  TYPE: Potion type code (POTION-*).

Returns:
  T."

<ROUTINE PRINT-POTION-EFFECT-NAME (TYPE)
    <COND (<==? .TYPE ,POTION-MUSCLE> <TELL "muscle">)
          (<==? .TYPE ,POTION-HEALTH> <TELL "health">)
          (<==? .TYPE ,POTION-HIDING> <TELL "hiding">)
          (<==? .TYPE ,POTION-POISON> <TELL "poison">)
          (<==? .TYPE ,POTION-VISION> <TELL "vision">)
          (<==? .TYPE ,POTION-MOTION> <TELL "motion">)
          (<==? .TYPE ,POTION-SHADOW> <TELL "shadow">)
          (<==? .TYPE ,POTION-METTLE> <TELL "mettle">)
          (ELSE <TELL "potion">)>
    <RTRUE>>

<ROUTINE PRINT-POTION-TYPE-NAME (TYPE)
    <TELL "potion of ">
    <PRINT-POTION-EFFECT-NAME .TYPE>
    <RTRUE>>

;"Prints a potion effect/type code as its pluralized display name.

Args:
  TYPE: Potion type code (POTION-*).

Returns:
  T."

<ROUTINE PRINT-POTION-PLURAL-TYPE-NAME (TYPE)
    <TELL "potions of ">
    <PRINT-POTION-EFFECT-NAME .TYPE>
    <RTRUE>>

;"Prints a potion color code as its (concealed) display name.

Args:
  COLOR: Potion color code (POTCOLOR-*).

Returns:
  T."

<ROUTINE PRINT-POTION-COLOR-ADJ (COLOR)
    <COND (<==? .COLOR ,POTCOLOR-ARGENT> <TELL "argent">)
          (<==? .COLOR ,POTCOLOR-BLUISH> <TELL "bluish">)
          (<==? .COLOR ,POTCOLOR-MAROON> <TELL "maroon">)
          (<==? .COLOR ,POTCOLOR-VIOLET> <TELL "violet">)
          (<==? .COLOR ,POTCOLOR-ORANGE> <TELL "orange">)
          (<==? .COLOR ,POTCOLOR-YELLOW> <TELL "yellow">)
          (<==? .COLOR ,POTCOLOR-INDIGO> <TELL "indigo">)
          (<==? .COLOR ,POTCOLOR-SALMON> <TELL "salmon">)
          (ELSE <TELL "colored">)>
    <RTRUE>>

<ROUTINE PRINT-POTION-COLOR-NAME (COLOR)
    <PRINT-POTION-COLOR-ADJ .COLOR>
    <TELL " potion">
    <RTRUE>>

;"Prints a potion color code as the color adjective only.

Args:
  COLOR: Potion color code (POTCOLOR-*).

Returns:
  T."
<ROUTINE PRINT-POTION-BARE-COLOR-NAME (COLOR)
    <PRINT-POTION-COLOR-ADJ .COLOR>
    <RTRUE>>

;"Prints the player-facing name for a potion of the given color.

Before a color is discovered, this returns the concealed color name. After it
is discovered, it returns the underlying potion type name.

Args:
  COLOR: Potion color code (POTCOLOR-*).

Returns:
  T."

<ADD-TELL-TOKENS
    POTION-TYPE-NAME * <PRINT-POTION-TYPE-NAME .X>
    POTION-PLURAL-TYPE-NAME * <PRINT-POTION-PLURAL-TYPE-NAME .X>
    POTION-COLOR-NAME * <PRINT-POTION-COLOR-NAME .X>
    POTION-BARE-COLOR-NAME * <PRINT-POTION-BARE-COLOR-NAME .X>
    POTION-DISPLAY-NAME * <PRINT-POTION-DISPLAY-NAME .X>>

<ROUTINE PRINT-POTION-DISPLAY-NAME (COLOR "AUX" TYPE)
    <COND (<OR <L? .COLOR 1> <G? .COLOR ,POTION-COLOR-COUNT>>
           <TELL "potion">
           <RTRUE>)>
    <COND (<G? <GETB ,POTION-DISCOVERED <- .COLOR 1>> 0>
           <SET TYPE <GETB ,POTION-TYPE-FOR-COLOR <- .COLOR 1>>>
           <PRINT-POTION-TYPE-NAME .TYPE>)
          (ELSE <PRINT-POTION-COLOR-NAME .COLOR>)>
    <RTRUE>>

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

;"Prints a treasure ID as its display name.

Args:
  ID: Treasure ID (TREASURE-*).

Returns:
  T."

<ROUTINE PRINT-TREASURE-NAME (ID)
    <COND (<==? .ID ,TREASURE-AMULET> <TELL "amulet">)
          (<==? .ID ,TREASURE-SCARAB> <TELL "scarab">)
          (<==? .ID ,TREASURE-GOBLET> <TELL "goblet">)
          (<==? .ID ,TREASURE-IOLITE> <TELL "iolite">)
          (<==? .ID ,TREASURE-GARNET> <TELL "garnet">)
          (<==? .ID ,TREASURE-JASPER> <TELL "jasper">)
          (<==? .ID ,TREASURE-ZIRCON> <TELL "zircon">)
          (<==? .ID ,TREASURE-POSTER> <TELL "poster">)
          (<==? .ID ,TREASURE-TROPHY> <TELL "Trophy of Scryra">)
          (ELSE <TELL "treasure">)>
    <RTRUE>>

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
          (<==? .ID ,TREASURE-POSTER> <COND (,EXPERT-MODE? 50) (ELSE 1500)>)
          (<==? .ID ,TREASURE-TROPHY> 5000)
          (ELSE 50)>>

;"Prints a key ID as its display name.

Args:
  LOCKTYPE: Key ID (LOCKTYPE-*).

Returns:
  T."
<ROUTINE PRINT-KEY-NAME (LOCKTYPE)
    <COND (<==? .LOCKTYPE ,LOCK-GOLDEN> <TELL "golden key">)
          (<==? .LOCKTYPE ,LOCK-SILVER> <TELL "silver key">)
          (<==? .LOCKTYPE ,LOCK-BRONZE> <TELL "bronze key">)
          (<==? .LOCKTYPE ,LOCK-COPPER> <TELL "copper key">)
          (<==? .LOCKTYPE ,LOCK-NICKEL> <TELL "nickel key">)
          (ELSE <TELL "key">)>
    <RTRUE>>

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

<ADD-TELL-TOKENS
    WEAPON-NAME *   <PRINT-WEAPON-NAME .X>
    FOOD-NAME *     <PRINT-FOOD-NAME .X>
    TREASURE-NAME * <PRINT-TREASURE-NAME .X>
    KEY-NAME *      <PRINT-KEY-NAME .X>>


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
