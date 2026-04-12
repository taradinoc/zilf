"NPC implementations"

;"The infrastructure for transitioning between the dungeon and these NPC areas
  is in interior.zil."

;"TODO: object descriptions and NPC descriptions"

"---------------------------------------------------------------------------"

"Info Booth"

<ROOM INFO-BOOTH
    (IN ROOMS)
    (DESC "Info Booth")
    (INTERIOR-NAME "info booth")
    (LDESC "Here, tucked away in the dungeon, stands an information booth.
The grizzled old man behind the counter sports an excited look, like he's
been waiting a long time for someone to stop by. Various signs are placed
around the booth, and a stack of posters balances precariously on the edge
of the counter.")
    (ACTION INFO-BOOTH-R)
    (THINGS VARIOUS (SIGN SIGNS) ([READ EXAMINE] "The signs all say \"Info Booth\". Kind of redundant, I suppose.")
            <> (COUNTER EDGE) "It's covered with a cheap-looking, obviously-fake wood grain."
            (WOOD CHEAP FAKE) GRAIN "It's hardly the most interesting thing around here."
            (CHEAP PLYWOOD INFO INFORMATION) BOOTH "It appears to be made of cheap plywood."
            (DUNGEON DUNGEON\'S) (LOGO UNIFORM) "You'd recognize the League of Scryra's logo anywhere: five horizontal
colored stripes in a mirrored pattern, representing descent and ascent through the dungeon floors, each divided
into six parts, representing apophenia.")
    (FLAGS LIGHTBIT)>


<ROUTINE INFO-BOOTH-R (RARG)
    <COND (<==? .RARG ,M-ENTER> <THIS-IS-IT ,INFO-MAN>)
          (<==? .RARG ,M-BEG>
           <COND (<OR <VERB? EXIT> <AND <VERB? WALK> <PRSO? ,P?OUT>>>
                  <THROW <> ,INTERIOR-CATCH-TOKEN>)>)>>

<OBJECT INFO-MAN
    (IN INFO-BOOTH)
    (DESC "grizzled old man")
    (SYNONYM MAN)
    (ADJECTIVE GRIZZLED OLD INFO)
    (ACTION INFO-MAN-F)
    (FLAGS PERSONBIT NDESCBIT)>

<ROUTINE INFO-MAN-F (ARG "AUX" ID)
    <COND (<==? .ARG ,M-WINNER>
           <COND (<VERB? HELLO>
                  <TELL CT ,INFO-MAN
                        " smiles. \"I hope you're enjoying the dungeon so far.\""
                        CR>)
                 (<AND <VERB? TELL-ABOUT> <PRSO? ,CURRENT-PLAYER>>
                  <WITH-GLOBAL ((WINNER ,CURRENT-PLAYER))
                               <PERFORM ,V?ASK-ABOUT ,INFO-MAN ,PRSI>>)
                 (<VERB? SGIVE> <PERFORM ,V?GIVE ,PRSI ,PRSO>)
                 (<AND <VERB? GIVE> <PRSI? ,CURRENT-PLAYER>>
                  <WITH-GLOBAL ((WINNER ,CURRENT-PLAYER))
                               <PERFORM ,V?TAKE ,PRSO>>)
                 (ELSE <TELL "He doesn't respond." CR>)>)
          (<VERB? EXAMINE>
           <TELL "Easily one of the top three most grizzled old men you've ever seen.
He's wearing a uniform with the dungeon's logo embroidered on it." CR>)
          (<AND <VERB? ASK-ABOUT TELL-ABOUT> <PRSO? ,INFO-MAN>>
           <COND (<PRSI? ,INFO-MAN> <TELL "\"Some call me the info man.\"" CR>)
                 (<POSTER? ,PRSI>
                  <TELL "\"You can have one.\"" CR>)
                 (ELSE
                  <TELL "\"I'm not authorized to talk about that.\"" CR CR
                        "He leans in close and whispers, \"They might be listening.\""
                        CR>)>)
          (<AND <VERB? GIVE> <PRSI? ,INFO-MAN>>
             <COND (<TREASURE? ,PRSO>
                    <COND (<==? <SET ID <GETP ,PRSO ,P?R-ITID>> ,TREASURE-POSTER>
                           <TELL "\"Thanks for returning it. A lot of people would love to get their hands on one of these!\"" CR>
                           <REMOVE ,PRSO>
                           <FREE-RASCAL-ITEM ,PRSO>)
                          (<==? .ID ,TREASURE-TROPHY>
                           <TELL "\"That's what was down there? Huh. Well, I guess we'll have to update the posters.\"" CR>)
                          (ELSE
                           <TELL "\"Seems like you need that more than I do.\"" CR>)>)
                   (<FOOD? ,PRSO>
                    <TELL "\"No thanks, I had a big breakfast.\"" CR>)
                   (<OR <POTION? ,PRSO> <WEAPON? ,PRSO>>
                    <TELL "He smiles. \"My adventuring days are long past.\"" CR>)
                   (ELSE <TELL "The old man doesn't seem interested." CR>)>)>>

<OBJECT STACK-OF-POSTERS
    (DESC "stack of posters")
    (IN INFO-BOOTH)
    (SYNONYM STACK POSTER POSTERS)
    (ACTION STACK-OF-POSTERS-F)
    (GENERIC POSTER-GENERIC-FCN)
    (FLAGS TRYTAKEBIT NDESCBIT)>

<CONSTANT POSTER-DESCRIPTION
    "At the top of the poster, above a giant \"at\" sign
watermark, elegant lettering reads: \"So, You've Decided To Explore A Dungeon...\"↲↲
The rest of the poster is covered with text.">

<ROUTINE FIND-HELD-POSTER ("AUX" O N)
    <SET O <FIRST? ,WINNER>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN 0>)>
        <SET N <NEXT? .O>>
        <COND (<AND <TREASURE? .O> <==? <GETP .O ,P?R-ITID> ,TREASURE-POSTER>>
               <RETURN .O>)>
        <SET O .N>>>

<ROUTINE STACK-OF-POSTERS-F ("AUX" O)
    <COND (<VERB? TAKE>
           <COND (<G? <GET ,STATS-TREASURES-PICKED <- ,TREASURE-POSTER 1>> 0>
                  <TELL "The old man snaps at you. \"One per rascal!\"" CR>)
                 (<INTERIOR-PACK-FULL?> <TELL "Your pack is full." CR>)
                 (ELSE
                  <SET O <ALLOC-RASCAL-ITEM>>
                  <COND (<NOT .O>
                         <TELL "The old man frowns. \"Sorry. That's all I've got.\""
                               CR>
                         <RTRUE>)>
                  <PUTP .O ,P?R-ITKIND ,ITEMKIND-TREASURE>
                  <PUTP .O ,P?R-ITID ,TREASURE-POSTER>
                  <PUTP .O ,P?R-ITLVL 0>
                  <PUTP .O ,P?R-ITENCH 0>
                  <PUTP .O ,P?R-ITAMT 0>
                  <PUTP .O ,P?R-X 0>
                  <PUTP .O ,P?R-Y 0>
                  <MOVE .O ,WINNER>
                  <SET-ITEM-VOCAB .O>
                  <THIS-IS-IT .O>
                  <STATS-INC-WORD-TABLE ,STATS-TREASURES-PICKED
                                        <- ,TREASURE-POSTER 1>>
                  <TELL "The old man sees you eyeing the stack of posters and hands you one.
\"There aren't many left, but here you go.\""
                        CR>)>)
          (<VERB? EXAMINE>
           <TELL ,POSTER-DESCRIPTION CR CR "There aren't many left." CR>)
          (<VERB? READ>
           <COND (<ACCESSIBLE? <FIND-HELD-POSTER>>
                  <PERFORM ,V?READ <FIND-HELD-POSTER>>)
                 (<TELL "All you can read from here is the title: \"So, You've
Decided To Explore A Dungeon...\""
                        CR>)>)>>

<ROUTINE POSTER-F ()
    <COND (<VERB? EXAMINE> <TELL ,POSTER-DESCRIPTION CR>)
          (<VERB? READ> <TELL ,POSTER-TEXT CR>)>>

<CONSTANT POSTER-TEXT
    "\"So, You've Decided To Explore A Dungeon...↲
↲
Congratulations! It takes a brave soul to embark on such a quest. You'll need
to have strength and stealth, and a whole lot of luck. Here are some tips you
may find useful:↲
↲
As a rule, dungeons contain a trader on every fifth floor. The trader can sell
you provisions, weaponry, and potions to help you on your way.↲
↲
Traders will give you gold for your unneeded or valuable items. They drive a
hard bargain, though, and they can only carry so much.↲
↲
Potions can be tempting, but be careful: there are no standards when it comes to
coloring! Every dungeon has its own potion colors. It's a mess.↲
↲
These days, there are many types of weapons to choose from. Some do more
predictable damage, some make it easier to land a critical hit. Experiment to
learn which ones fit you! And remember, higher level isn't always better.↲
↲
If you want to get the most out of your journey, be sure to closely examine the
walls of your dungeon. Some doors are hard to make out from afar.↲
↲
And finally, they say something special awaits on the twenty-fifth floor of
every dungeon. None have ever made it that far, so it's little more than a
rumor... for now.↲
↲
Good luck!\"">

<ROUTINE POSTER-GENERIC-FCN (TBL "AUX" MAX IT)
    ;"Prefer the poster over the stack of posters."
    <SET MAX <GETB .TBL 0>>
    <DO (I 1 .MAX)
        <SET IT <GET/B .TBL .I>>
        <COND (<POSTER? .IT> <RETURN .IT>)>>
    ,STACK-OF-POSTERS>

"---------------------------------------------------------------------------"

"Bees"

<ROOM BEE-HIVE
    (IN ROOMS)
    (DESC "Bees")
    (INTERIOR-NAME "bees")
    (LDESC "The air in here buzzes. A writhing legion of bees hangs in a dark cloud.
One sting would be bad. A hundred will kill you.↲↲You should leave. Immediately.")
    (ACTION BEE-HIVE-R)
    (THINGS (WRITHING DARK) (LEGION BEES BEE CLOUD) "This is no time for curiosity!")
    (FLAGS LIGHTBIT)>

<ROUTINE BEE-HIVE-R (RARG)
    <COND (<==? .RARG ,M-BEG>
           <COND (<OR <VERB? EXIT> <AND <VERB? WALK> <PRSO? ,P?OUT>>>
                  <THROW <> ,INTERIOR-CATCH-TOKEN>)>)
          (<==? .RARG ,M-END>
           <TELL CR "The legion of bees engulfs you." CR CR "[Press any key to continue.]">
           <GETCHAR>
           <SETG PLAYER-HP 0>
           <THROW <> ,INTERIOR-CATCH-TOKEN>)>>

"---------------------------------------------------------------------------"

"Blacksmith"

<ROOM BLACKSMITH-SHOP
    (IN ROOMS)
    (DESC "Blacksmith")
    (INTERIOR-NAME "blacksmith")
    (ACTION BLACKSMITH-SHOP-R)
    (THINGS (HAND PAINTED HAND-PAINTED) SIGN BLACKSMITH-SIGN-R)
    (FLAGS LIGHTBIT)>

<ROUTINE BLACKSMITH-SIGN-R ()
    <COND (<VERB? READ EXAMINE>
           <TELL "\"Weapon enchantments, " N ,ENCHANT-COST " gold.\"" CR>)>>

<ROUTINE BLACKSMITH-SHOP-R (RARG)
    <COND (<==? .RARG ,M-ENTER> <THIS-IS-IT ,BLACKSMITH>)
          (<==? .RARG ,M-BEG>
           <COND (<OR <VERB? EXIT> <AND <VERB? WALK> <PRSO? ,P?OUT>>>
                  <THROW <> ,INTERIOR-CATCH-TOKEN>)>)
          (<==? .RARG ,M-LOOK>
           <TELL "A soot-darkened forge squats here, cold and quiet. The tools are laid out as if
someone stepped away mid-swing... days ago.↲↲A blacksmith leans against the anvil, watching you.↲↲
A hand-painted sign reads: \"Weapon enchantments, " N ,ENCHANT-COST " gold.\"" CR>)>>

<OBJECT BLACKSMITH
    (IN BLACKSMITH-SHOP)
    (DESC "blacksmith")
    (SYNONYM BLACKSMITH SMITH)
    (ADJECTIVE SILENT)
    (ACTION BLACKSMITH-F)
    (FLAGS PERSONBIT NDESCBIT)>

<ROUTINE BLACKSMITH-ENCHANT-WEAPON (OBJ "AUX" ID LVL ENCH NEWENCH)
    <COND (<NOT <WEAPON? .OBJ>>
           <TELL "The blacksmith says, \"That's not a weapon.\"" CR>
           <RFALSE>)>
    <COND (<L? ,PLAYER-GOLD ,ENCHANT-COST>
           <TELL "The blacksmith says, \"It'll cost you " N ,ENCHANT-COST
                 " gold.\"" CR>
           <RFALSE>)>

    <SETG PLAYER-GOLD <- ,PLAYER-GOLD ,ENCHANT-COST>>
    <SETG STATS-GOLD-SPENT-BLACKSMITH
          <+ ,STATS-GOLD-SPENT-BLACKSMITH ,ENCHANT-COST>>
    <SET ENCH <GETP .OBJ ,P?R-ITENCH>>
    <COND (<G? .ENCH 254> <SET NEWENCH 255>) (ELSE <SET NEWENCH <+ .ENCH 1>>)>
    <PUTP .OBJ ,P?R-ITENCH .NEWENCH>
    <COND (,EXPERT-MODE? <SETG ENCHANT-COST <PRICE-PLUS-HALF ,ENCHANT-COST>>)>

    <SET ID <GETP .OBJ ,P?R-ITID>>
    <SET LVL <GETP .OBJ ,P?R-ITLVL>>
    <TELL "The blacksmith takes your gold, mutters a few words, and taps the "
          WEAPON-NAME .ID " with a hammer." CR>
    <TELL "It hums faintly. It's now a level " N .LVL "+" N .NEWENCH " "
          WEAPON-NAME .ID "." CR>
    <RTRUE>>

<ROUTINE BLACKSMITH-F (ARG)
    <COND (<==? .ARG ,M-WINNER>
           <COND (<VERB? HELLO> <TELL "The blacksmith nods." CR>)
                 (<AND <VERB? TELL-ABOUT> <PRSO? ,CURRENT-PLAYER>>
                  <WITH-GLOBAL ((WINNER ,CURRENT-PLAYER))
                               <PERFORM ,V?ASK-ABOUT ,BLACKSMITH ,PRSI>>)
                 (ELSE <TELL "He doesn't respond." CR>)>)
          (<VERB? EXAMINE>
           <TELL "He's wearing an apron with a small logo, depicting a pair of tongs and a hammer."
                 CR>)
          (<AND <VERB? ASK-ABOUT TELL-ABOUT> <PRSO? ,BLACKSMITH>>
           <COND (<PRSI? ,BLACKSMITH> <TELL "\"I just work here.\"" CR>)
                 (<RASCAL-ITEM? ,PRSI>
                  <TELL "\"If you give me a weapon, I'll enchant it for "
                    N ,ENCHANT-COST " gold.\""
                        CR>)
                 (ELSE <TELL "\"That's none of my business.\"" CR>)>)
          (<AND <VERB? GIVE> <PRSI? ,BLACKSMITH>>
           <COND (<WEAPON? ,PRSO>
                  <BLACKSMITH-ENCHANT-WEAPON ,PRSO>)
                 (<FOOD? ,PRSO>
                  <TELL "\"I already ate.\"" CR>)
                 (<TREASURE? ,PRSO>
                  <TELL "\"Nah, you keep it. I'd just melt it down.\"" CR>)
                 (ELSE <TELL "\"I'm really more of a weapon guy.\"" CR>)>
           <RTRUE>)>>

"---------------------------------------------------------------------------"

"Carrot Farm"

<ROOM CARROT-FARM
    (IN ROOMS)
    (DESC "Carrot Farm")
    (INTERIOR-NAME "carrot farm")
    (ACTION CARROT-FARM-R)
    (THINGS (HAND PAINTED HAND-PAINTED) SIGN CARROT-SIGN-R)
    (FLAGS LIGHTBIT)>

<ROUTINE CARROT-SIGN-R ()
    <COND (<VERB? READ EXAMINE>
           <TELL "\"Carrots: " N ,CARROT-COST " gold. Potions identified: "
                 N ,IDENTIFY-COST " gold.\"" CR>)>>

<ROUTINE CARROT-FARM-R (RARG)
    <COND (<==? .RARG ,M-ENTER> <THIS-IS-IT ,CARROT-MAN>)
          (<==? .RARG ,M-BEG>
           <COND (<OR <VERB? EXIT> <AND <VERB? WALK> <PRSO? ,P?OUT>>>
                  <THROW <> ,INTERIOR-CATCH-TOKEN>)>)
          (<==? .RARG ,M-LOOK>
           <TELL "Neat rows of vegetables stretch across a small underground plot.
An earthy smell clings to the air. A farmer watches you closely, arms crossed.
In front of the carrot patch, a hand-painted sign reads: \"Carrots: "
                 N ,CARROT-COST " gold. Potions identified: " N ,IDENTIFY-COST " gold.\"" CR>)>>

<GLOBAL CARROTS-SOLD 0>

<ROUTINE CARROT-PRESENT? ("AUX" O N K ID)
    ;"True if a carrot exists either here or carried (real carrot object or inventory item)."
    <SET O <FIRST? ,WINNER>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN>)>
        <SET N <NEXT? .O>>
        <COND (<RASCAL-ITEM? .O>
               <SET K <GETP .O ,P?R-ITKIND>>
               <SET ID <GETP .O ,P?R-ITID>>
               <COND (<AND <==? .K ,ITEMKIND-FOOD> <==? .ID ,FOOD-CARROT>>
                      <RTRUE>)>)>
        <SET O .N>>

    <SET O <FIRST? ,HERE>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN>)>
        <SET N <NEXT? .O>>
        <COND (<RASCAL-ITEM? .O>
               <SET K <GETP .O ,P?R-ITKIND>>
               <SET ID <GETP .O ,P?R-ITID>>
               <COND (<AND <==? .K ,ITEMKIND-FOOD> <==? .ID ,FOOD-CARROT>>
                      <RTRUE>)>)>
        <SET O .N>>
    <RFALSE>>

<OBJECT CARROT-MAN
    (IN CARROT-FARM)
    (DESC "farmer")
    (SYNONYM FARMER MAN)
    (ACTION CARROT-MAN-F)
    (FLAGS PERSONBIT NDESCBIT)>

<ROUTINE CARROT-MAN-IDENTIFY-POTION (OBJ "AUX" COLOR TYPE)
    <COND (<NOT <POTION? .OBJ>>
           <TELL "The farmer says, \"I only deal in potions.\"" CR>
           <RFALSE>)>
    <SET COLOR <GETP .OBJ ,P?R-ITID>>
    <COND (<OR <L? .COLOR 1> <G? .COLOR ,POTION-COLOR-COUNT>>
           <TELL "The farmer squints at it. \"I can't make heads or tails of that one.\""
                 CR>
           <RFALSE>)>
    <COND (<G? <GETB ,POTION-DISCOVERED <- .COLOR 1>> 0>
           <TELL "The farmer scoffs, \"That's obviously a "
                 POTION-DISPLAY-NAME .COLOR ".\"" CR>
           <RTRUE>)>
    <COND (<L? ,PLAYER-GOLD ,IDENTIFY-COST>
           <TELL "The farmer says, \"I ain't doing this for fun. It'll cost you "
                 N ,IDENTIFY-COST " gold.\""
                 CR>
           <RFALSE>)>

    <SETG PLAYER-GOLD <- ,PLAYER-GOLD ,IDENTIFY-COST>>
    <SETG STATS-GOLD-SPENT-CARROT-FARM
          <+ ,STATS-GOLD-SPENT-CARROT-FARM ,IDENTIFY-COST>>
    <PUTB ,POTION-DISCOVERED <- .COLOR 1> 1>
    <SET TYPE <GETB ,POTION-TYPE-FOR-COLOR <- .COLOR 1>>>
    <COND (,EXPERT-MODE?
           <SETG IDENTIFY-COST <PRICE-PLUS-HALF ,IDENTIFY-COST>>)>
    <COND (<==? .TYPE ,POTION-HEALTH>
           <TELL "The farmer swirls it, sniffs, and nods. \"This'n's a potion of health.
It'll increase your max HP by two, plus it'll heal you a bunch.\""
                 CR>)
          (<==? .TYPE ,POTION-HIDING>
           <TELL "The farmer swirls it, peers closely at it, and nods. \"That's a potion of hiding.
Makes you invisible for thirty turns or so. Monsters won't attack you, but try not to
bump into them.\""
                 CR>)
          (<==? .TYPE ,POTION-MOTION>
           <TELL "The farmer swirls it, taps it a few times, and nods. \"Yep, that's a
potion of motion. It'll zap you to a random place on the dungeon floor, which could be
good or bad, depending.\""
                 CR>)
          (<==? .TYPE ,POTION-MUSCLE>
           <TELL "The farmer gives it a few vigorous swirls, watches the liquid slowly come
to a stop, and looks at it with a satisfied expression. \"You got yourself a potion of muscle.
That'll increase your strength by one. Good for carrying higher level weapons, or doing more
damage with the ones you got.\""
                 CR>)
          (<==? .TYPE ,POTION-POISON>
           <TELL "The farmer swirls it, sniffs, and gags. After several heavy coughs,
he frowns and says, \"That there's poison. Don't drink that.\""
                 CR>)
          (<==? .TYPE ,POTION-SHADOW>
           <TELL "The farmer swirls it, holds it up to the light, and nods. \"This'n's a
potion of shadow. Makes it hard to see more than an arm's length. Lasts a while, too.\""
                 CR>)
          (<==? .TYPE ,POTION-VISION>
           <TELL "The farmer swirls it, puts an ear up to it, and closes his eyes. Seconds later,
he opens them again and says, \"It's a potion of vision. Drink that and you'll be able to see
the beasts before they get anywhere near you.\""
                 CR>)
          (<==? .TYPE ,POTION-METTLE>
           <TELL "The farmer swirls it, dips a finger in, and rubs the liquid between his
finger and thumb. \"That's a potion of mettle. Every swig of that makes it harder for the
beasts to hit you.\""
                 CR>)
          (ELSE
           <TELL "\"Huh. I'm stumped. That shouldn't happen.\"" CR>)>
    <UPDATE-POTION-ITEMS-FOR-COLOR .COLOR>
    <RTRUE>>

<CONSTANT NOT-A-CARROT
    "\"If it ain't a carrot, I don't wanna hear it. I've always said that.\"">

<ROUTINE CARROT-MAN-F (ARG)
    <COND (<==? .ARG ,M-WINNER>
           <COND (<VERB? HELLO> <TELL CT ,CARROT-MAN " nods." CR>)
                 (<AND <VERB? TELL-ABOUT> <PRSO? ,CURRENT-PLAYER>>
                  <WITH-GLOBAL ((WINNER ,CURRENT-PLAYER))
                               <PERFORM ,V?ASK-ABOUT ,CARROT-MAN ,PRSI>>)
                 (<VERB? SGIVE> <PERFORM ,V?GIVE ,PRSI ,PRSO>)
                 (<AND <VERB? GIVE> <PRSI? ,CURRENT-PLAYER>>
                  <WITH-GLOBAL ((WINNER ,CURRENT-PLAYER))
                               <PERFORM ,V?TAKE ,PRSO>>)
                 (ELSE <TELL "He doesn't respond." CR>)>)
          (<VERB? EXAMINE>
           <TELL "He's wearing a pair of denim overalls and a straw hat." CR>)
          (<AND <VERB? ASK-ABOUT TELL-ABOUT> <PRSO? ,CARROT-MAN>>
           <COND (<CARROT? ,PRSI>
                  <TELL "\"They're a great source of vitamin A, and they're as nourishing as a muffin.\""
                        CR>)
                 (<FOOD? ,PRSI> <TELL ,NOT-A-CARROT CR>)
                 (<POTION? ,PRSI> <CARROT-MAN-IDENTIFY-POTION ,PRSI>)
                 (<OR <WEAPON? ,PRSI> <TREASURE? ,PRSI>>
                  <TELL "\"Sounds exciting, but that life ain't for me.\"" CR>)>)
          (<AND <VERB? GIVE> <PRSI? ,CARROT-MAN>>
           <COND (<POTION? ,PRSO> <CARROT-MAN-IDENTIFY-POTION ,PRSO>)
                 (<CARROT? ,PRSO> <TELL "\"All sales are final.\"" CR>)
                 (<FOOD? ,PRSO> <TELL ,NOT-A-CARROT CR>)
                 (<TREASURE? ,PRSO>
                  <TELL "\"No need to get all fancy, I only take gold.\"" CR>)
                 (<WEAPON? ,PRSO>
                  <TELL "\"Why would I need that? To protect myself from the carrots?\""
                        CR>)
                 (ELSE <TELL "The farmer doesn't seem interested." CR>)>)>>

<OBJECT CARROT-PATCH
    (IN CARROT-FARM)
    (DESC "carrot patch")
    (SYNONYM CARROTS PATCH ROW ROWS)
    (ADJECTIVE CARROT)
    (ACTION CARROT-PATCH-F)
    (GENERIC CARROT-GENERIC-FCN)
    (FLAGS TRYTAKEBIT NDESCBIT)>

<ROUTINE CARROT-PATCH-F ("AUX" O)
    <COND (<VERB? EXAMINE>
           <SETG P-CONT 0>
           <TELL "Bright orange carrots peek out from the soil." CR>
           <RTRUE>)
          (<VERB? TAKE>
           <SETG P-CONT 0>
           <COND (<G? ,CARROTS-SOLD 2>
                  <TELL "The farmer says, \"That's all I've got for sale.\"" CR>)
                 (<CARROT-PRESENT?>
                  <TELL "The farmer says, \"One carrot at a time.\"" CR>)
                 (<INTERIOR-PACK-FULL?> <TELL "Your pack is full." CR>)
                 (<L? ,PLAYER-GOLD ,CARROT-COST>
                  <TELL "The farmer says, \"" N ,CARROT-COST " gold.\"" CR>)
                 (ELSE
                  <SET O <ALLOC-RASCAL-ITEM>>
                  <COND (<NOT .O>
                         <TELL "The farmer frowns. \"That's all I've got.\"" CR>
                         <RTRUE>)>
                  <SETG PLAYER-GOLD <- ,PLAYER-GOLD ,CARROT-COST>>
                  <SETG STATS-GOLD-SPENT-CARROT-FARM
                      <+ ,STATS-GOLD-SPENT-CARROT-FARM ,CARROT-COST>>
                  <SETG CARROTS-SOLD <+ ,CARROTS-SOLD 1>>
                  <COND (,EXPERT-MODE?
                         <SETG CARROT-COST <* 2 ,CARROT-COST>>)> 
                  <PUTP .O ,P?R-ITKIND ,ITEMKIND-FOOD>
                  <PUTP .O ,P?R-ITID ,FOOD-CARROT>
                  <PUTP .O ,P?R-ITLVL 0>
                  <PUTP .O ,P?R-ITENCH 0>
                  <PUTP .O ,P?R-ITAMT 0>
                  <PUTP .O ,P?R-X 0>
                  <PUTP .O ,P?R-Y 0>
                  <MOVE .O ,WINNER>
                  <SET-ITEM-VOCAB .O>
                  <THIS-IS-IT .O>
                  <TELL "The farmer takes your gold and hands you a carrot." CR>)>)>>

<ROUTINE CARROT-GENERIC-FCN (TBL "AUX" MAX IT)
    ;"Prefer a carried carrot item over the carrot patch itself."
    <SET MAX <GETB .TBL 0>>
    <DO (I 1 .MAX)
        <COND (<AND <RASCAL-ITEM? <SET IT <GET/B .TBL .I>>>
                    <==? <GETP .IT ,P?R-ITKIND> ,ITEMKIND-FOOD>
                    <==? <GETP .IT ,P?R-ITID> ,FOOD-CARROT>>
               <RETURN .IT>)>>
    ,CARROT-PATCH>

"---------------------------------------------------------------------------"

"Grotto of the Oracle"

<ROOM ORACLE-GROTTO
    (IN ROOMS)
    (DESC "Grotto of the Oracle")
    (INTERIOR-NAME "grotto of the oracle")
    (LDESC "This small, cave-like structure is home to the dungeon's Oracle. A
sign near the entrance reads: \"Recall the Past! See the Future!\"↲
↲
The Oracle herself seems to be away at the moment, but she left behind her glassy sphere, resting atop a stone pedestal.")
    (ACTION ORACLE-GROTTO-R)
    (THINGS (WORN STONE) PEDESTAL "A worn stone pedestal supports the sphere."
            <> ORACLE "The Oracle isn't here."
            <> SIGN ([READ EXAMINE] "\"Recall the Past! See the Future!\""))
    (FLAGS LIGHTBIT)>

<ROUTINE ORACLE-GROTTO-R (RARG)
    <COND (<==? .RARG ,M-ENTER> <THIS-IS-IT ,GLASSY-SPHERE>)
          (<==? .RARG ,M-BEG>
           <COND (<OR <VERB? EXIT> <AND <VERB? WALK> <PRSO? ,P?OUT>>>
                  <THROW <> ,INTERIOR-CATCH-TOKEN>)>)>>

<OBJECT GLASSY-SPHERE
    (IN ORACLE-GROTTO)
    (DESC "glassy sphere")
    (SYNONYM SPHERE ORB BALL)
    (ADJECTIVE GLASSY CRYSTAL)
    (ACTION GLASSY-SPHERE-F)
    (FLAGS NDESCBIT)>

<GLOBAL ORACLE-VISION-FLOOR 0>
<GLOBAL ORACLE-VISION-KIND 0>
<GLOBAL ORACLE-VISION-LOCKTYPE 0>

<ROUTINE GLASSY-SPHERE-F (ARG)
    <COND (<VERB? EXAMINE LOOK>
           <ORACLE-SPHERE-VISION>
           <RTRUE>)
          (<VERB? TAKE>
           <SETG P-CONT 0>
           <TELL "The sphere is fused to its pedestal." CR>
           <RTRUE>)>
    <RFALSE>>

<ROUTINE ORACLE-MAX-VISITED-FLOOR ("AUX" MAX)
    <SET MAX 0>
    <DO (F 1 ,MAX-FLOORS)
        <COND (<FSET? <FLOOR-OBJ .F> ,TOUCHBIT> <SET MAX .F>)>>
    .MAX>

<ROUTINE ORACLE-COUNT-FLOOR-VISITED (F "AUX" CNT O K X Y)
    <SET CNT 0>
    <SET O <FIRST? <FLOOR-OBJ .F>>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN .CNT>)>
        <SET K <GETP .O ,P?R-ITKIND>>
        <COND (<==? .K ,ITEMKIND-KEY>
               <SET X <GETP .O ,P?R-X>>
               <SET Y <GETP .O ,P?R-Y>>
               <COND (<AND <G? .X 0> <G? .Y 0>> <SET CNT <+ .CNT 1>>)>)
              (<==? .K ,ITEMKIND-LOCKEDDOOR>
               <COND (<NOT <FSET? .O ,OPENBIT>> <SET CNT <+ .CNT 1>>)>)>
        <SET O <NEXT? .O>>>>

<ROUTINE ORACLE-COUNT-VISITED-CANDIDATES ("AUX" CNT)
    <SET CNT 0>
    <DO (F 1 ,MAX-FLOORS)
        <COND (<FSET? <FLOOR-OBJ .F> ,TOUCHBIT>
               <SET CNT <+ .CNT <ORACLE-COUNT-FLOOR-VISITED .F>>>)>>
    .CNT>

<ROUTINE ORACLE-COUNT-FLOOR-FUTURE (F "AUX" CNT O K)
    <SET CNT 0>
    <COND (<G? <GETB ,TREASURE-ROOM-LOCKTYPE <- .F 1>> 0> <SET CNT <+ .CNT 1>>)>
    <SET O <FIRST? <FLOOR-OBJ .F>>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN .CNT>)>
        <SET K <GETP .O ,P?R-ITKIND>>
        <COND (<==? .K ,ITEMKIND-KEY> <SET CNT <+ .CNT 1>>)>
        <SET O <NEXT? .O>>>>

<ROUTINE ORACLE-PICK-IN-FLOOR-VISITED (F TARGET "AUX" O K X Y)
    <SET O <FIRST? <FLOOR-OBJ .F>>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN .TARGET>)>
        <SET K <GETP .O ,P?R-ITKIND>>
        <COND (<==? .K ,ITEMKIND-KEY>
               <SET X <GETP .O ,P?R-X>>
               <SET Y <GETP .O ,P?R-Y>>
               <COND (<AND <G? .X 0> <G? .Y 0>>
                      <SET TARGET <- .TARGET 1>>
                      <COND (<L=? .TARGET 0>
                             <SETG ORACLE-VISION-FLOOR .F>
                             <SETG ORACLE-VISION-KIND ,ITEMKIND-KEY>
                             <SETG ORACLE-VISION-LOCKTYPE <GETP .O ,P?R-ITID>>
                             <RETURN 0>)>)>)
              (<==? .K ,ITEMKIND-LOCKEDDOOR>
               <COND (<NOT <FSET? .O ,OPENBIT>>
                      <SET TARGET <- .TARGET 1>>
                      <COND (<L=? .TARGET 0>
                             <SETG ORACLE-VISION-FLOOR .F>
                             <SETG ORACLE-VISION-KIND ,ITEMKIND-LOCKEDDOOR>
                             <SETG ORACLE-VISION-LOCKTYPE <GETP .O ,P?R-ITID>>
                             <RETURN 0>)>)>)>
        <SET O <NEXT? .O>>>>

<ROUTINE ORACLE-PICK-VISITED ("AUX" TOTAL TARGET)
    <SET TOTAL <ORACLE-COUNT-VISITED-CANDIDATES>>
    <COND (<L? .TOTAL 1> <RETURN 0>)>
    <SET TARGET <RNG .TOTAL>>
    <DO (F 1 ,MAX-FLOORS)
        <COND (<FSET? <FLOOR-OBJ .F> ,TOUCHBIT>
               <SET TARGET <ORACLE-PICK-IN-FLOOR-VISITED .F .TARGET>>
               <COND (<L=? .TARGET 0> <RETURN T>)>)>>
    0>

<ROUTINE ORACLE-PICK-IN-FLOOR-FUTURE (F TARGET "AUX" LOCKTYPE O K)
    <SET LOCKTYPE <GETB ,TREASURE-ROOM-LOCKTYPE <- .F 1>>>
    <COND (<G? .LOCKTYPE 0>
           <SET TARGET <- .TARGET 1>>
           <COND (<L=? .TARGET 0>
                  <SETG ORACLE-VISION-FLOOR .F>
                  <SETG ORACLE-VISION-KIND ,ITEMKIND-LOCKEDDOOR>
                  <SETG ORACLE-VISION-LOCKTYPE .LOCKTYPE>
                  <RETURN 0>)>)>
    <SET O <FIRST? <FLOOR-OBJ .F>>>
    <REPEAT ()
        <COND (<NOT .O> <RETURN .TARGET>)>
        <SET K <GETP .O ,P?R-ITKIND>>
        <COND (<==? .K ,ITEMKIND-KEY>
               <SET TARGET <- .TARGET 1>>
               <COND (<L=? .TARGET 0>
                      <SETG ORACLE-VISION-FLOOR .F>
                      <SETG ORACLE-VISION-KIND ,ITEMKIND-KEY>
                      <SETG ORACLE-VISION-LOCKTYPE <GETP .O ,P?R-ITID>>
                      <RETURN 0>)>)>
        <SET O <NEXT? .O>>>>

<ROUTINE ORACLE-PICK-FUTURE ("AUX" MAXVIS TOTAL TARGET)
    <SET MAXVIS <ORACLE-MAX-VISITED-FLOOR>>
    <SET TOTAL 0>
    <DO (F <+ .MAXVIS 1> ,MAX-FLOORS)
        <SET TOTAL <+ .TOTAL <ORACLE-COUNT-FLOOR-FUTURE .F>>>>
    <COND (<L? .TOTAL 1> <RETURN 0>)>
    <SET TARGET <RNG .TOTAL>>
    <DO (F <+ .MAXVIS 1> ,MAX-FLOORS)
        <SET TARGET <ORACLE-PICK-IN-FLOOR-FUTURE .F .TARGET>>
        <COND (<L=? .TARGET 0> <RETURN T>)>>
    0>

<ROUTINE ORACLE-DESCRIBE-VISION (KNOWN? "AUX" KIND LOCKTYPE FLOOR)
    <SET KIND ,ORACLE-VISION-KIND>
    <SET LOCKTYPE ,ORACLE-VISION-LOCKTYPE>
    <SET FLOOR ,ORACLE-VISION-FLOOR>
    <COND (<==? .KIND ,ITEMKIND-KEY>
           <TELL "Gazing into the sphere, you see a " KEY-NAME .LOCKTYPE>
           <COND (.KNOWN? <TELL " on floor " N .FLOOR>)
                 (ELSE <TELL " on a floor you don't recognize">)>
           <TELL "." CR>)
          (<==? .KIND ,ITEMKIND-LOCKEDDOOR>
           <TELL "Gazing into the sphere, you see a locked door that requires a "
                 KEY-NAME .LOCKTYPE>
           <COND (.KNOWN? <TELL " on floor " N .FLOOR>)
                 (ELSE <TELL " on a floor you don't recognize">)>
           <TELL "." CR>)
          (ELSE <TELL "The sphere shows only swirling fog." CR>)>>

<ROUTINE ORACLE-SPHERE-VISION ("AUX" FOUND?)
    <SETG P-CONT 0>
    <COND (<SET FOUND? <ORACLE-PICK-VISITED>>
           <ORACLE-DESCRIBE-VISION T>
           <RTRUE>)
          (<SET FOUND? <ORACLE-PICK-FUTURE>>
           <ORACLE-DESCRIBE-VISION <>>
           <RTRUE>)
          (ELSE
           <TELL "The sphere shows only swirling fog." CR>
           <RTRUE>)>>

"---------------------------------------------------------------------------"

"Busker"

<GLOBAL BUSKER-MONKEY-SALES 0>

<ROUTINE BUSKER-OFFER ()
    <COND (<==? ,BUSKER-MONKEY-SALES 0> 600)
          (<==? ,BUSKER-MONKEY-SALES 1> 500)
          (<==? ,BUSKER-MONKEY-SALES 2> 400)
          (ELSE 0)>>

<ROUTINE INTERIOR-MONKEY? (BIT)
        ;"Return a tamed monkey object if one is present in the interior world
            (either carried or in the room)."
        <MAP-CONTENTS (O ,WINNER)
            <COND (<AND <==? <GETP .O ,P?R-ETYPE> ,ETYPE-MONKEY>
                        <FSET? .O .BIT>>
                   <RETURN .O>)>>
        <MAP-CONTENTS (O ,HERE)
            <COND (<AND <==? <GETP .O ,P?R-ETYPE> ,ETYPE-MONKEY>
                        <FSET? .O .BIT>>
                   <RETURN .O>)>>
        0>

<ROUTINE MONKEY? (OBJ)
    <==? <GETP .OBJ ,P?R-ETYPE> ,ETYPE-MONKEY>>

<ROOM BUSKERS-CORNER
    (IN ROOMS)
    (DESC "Busker's Corner")
    (INTERIOR-NAME "busker")
    (ACTION BUSKERS-CORNER-R)
    (THINGS <> HAT BUSKER-HAT-F
            (WOODEN BARREL) (ORGAN BOX) "It's a wooden box, supported by a strap around the busker's shoulders, with a giant crank on the side."
            GIANT (CRANK STRAP) "The workings of a barrel organ sure are fascinating, but that's not why you're here."
            <> STRING "This is no time to worry about string.")
    (FLAGS LIGHTBIT)>

<ROUTINE BUSKER-HAT-F ()
    <COND (<VERB? EXAMINE>
           <COND (<==? ,BUSKER-MONKEY-SALES 0>
                  <TELL "It's a ratty old hat, set out to collect tips, but containing barely anything at all." CR>)
                 (ELSE
                  <TELL "It's a ratty old hat. The monkey has a tight grip on it and is waving it around like a checkered flag." CR>)>)>>

<GLOBAL BUSKER-GREETS? <>>

<ROUTINE BUSKERS-CORNER-R (RARG "AUX" OFFER)
    <COND (<==? .RARG ,M-ENTER> <SETG BUSKER-GREETS? T>)
          (<==? .RARG ,M-LOOK>
           <COND (<==? ,BUSKER-MONKEY-SALES 0>
                  <TELL "A tired-looking busker holds a barrel organ in a quiet nook of the dungeon.
His hat sits on the ground, but it doesn't look very full." CR>)
                 (<==? ,BUSKER-MONKEY-SALES 1>
                  <TELL "A smiling busker holds a barrel organ in a quiet nook of the dungeon. A monkey, tied to the organ by a string,
skitters around on the ground looking cute and waving a hat." CR>)
                 (<==? ,BUSKER-MONKEY-SALES 2>
                  <TELL "A bored-looking busker holds a barrel organ in a quiet nook of the dungeon. Tied to the organ with string are
two monkeys, skittering around on the ground in that way only monkeys do. One of them is waving a hat in its little paw as the other
looks on with envy." CR>)
                 (ELSE
                  <TELL "An exasperated-looking busker holds a barrel organ in a quiet nook of the dungeon. Three monkeys are tied to
the organ with string. One of them is waving a hat in its little paw, keeping it away from the other two trying to grab it." CR>)>)
          (<AND <==? .RARG ,M-FLASH> ,BUSKER-GREETS?>
           <CRLF>
           <SETG BUSKER-GREETS? <>>
           <THIS-IS-IT ,BUSKER>
           <COND (<INTERIOR-MONKEY? ,TAMEBIT>
                  <COND (<G? <SET OFFER <BUSKER-OFFER>> 0>
                         <COND (<==? ,BUSKER-MONKEY-SALES 0>
                                <TELL "The busker's eyes light up as he sees your monkey. \"Say, a monkey like that's just what I need!
It's hard to get good tips without a monkey, you know? 'Course you know. Look, I'll give you " N .OFFER " gold for it.\"" CR>)
                               (<==? ,BUSKER-MONKEY-SALES 1>
                                <TELL "The busker smiles as he sees your monkey. \"Another monkey, huh? Come to think of it, I've never
seen anyone else with two monkeys. That'd help me stand out for sure! All right, I'll give you " N .OFFER " gold for it.\"" CR>)
                               (ELSE <TELL "The busker stares in disbelief as he sees your monkey. \"Where do you keep getting these?
I really don't know if I can handle another monkey. I'm not running a zoo here! I can give you " N .OFFER " gold for it, but this is
the last one, OK?\"" CR>)>)
                        (ELSE
                         <TELL "The busker glances at your monkey and shakes his head. \"No more.\""
                               CR>)>)
                 (<INTERIOR-MONKEY? ,SOLDBIT>
                  ;"No comment from the busker"
                  <RFALSE>)
                 (ELSE
                  <TELL "The busker sighs. \"Hard to get good tips without a monkey, you know.\""
                        CR>)>)
          (<==? .RARG ,M-BEG>
           <COND (<OR <VERB? EXIT> <AND <VERB? WALK> <PRSO? ,P?OUT>>>
                  <THROW <> ,INTERIOR-CATCH-TOKEN>)>)>>

<OBJECT BUSKER
    (IN BUSKERS-CORNER)
    (DESC "busker")
    (SYNONYM BUSKER MUSICIAN MAN GRINDER)
    (ADJECTIVE ORGAN)
    (ACTION BUSKER-F)
    (FLAGS PERSONBIT NDESCBIT)>

<ROUTINE BUSKER-F (ARG "AUX" M OFFER)
    <COND (<==? .ARG ,M-WINNER>
           <COND (<VERB? HELLO>
                  <COND (<SET M <INTERIOR-MONKEY? ,TAMEBIT>>
                         <COND (<G? <SET OFFER <BUSKER-OFFER>> 0>
                                <TELL "The busker nods toward your monkey. \"I bet that monkey would drum up some business. I'll give you "
                                      N .OFFER " gold for it.\"" CR>)
                               (ELSE
                                <TELL "The busker says, \"I can't use another one.\""
                                      CR>)>)
                        (,BUSKER-MONKEY-SALES
                         <TELL "The busker says, \"It was a pleasure doing monkey business with you.\"" CR>)
                        (ELSE <TELL "The busker says, \"Hello! Let me know if you find a monkey. I could use one around here.\"" CR>)>
                  <RTRUE>)
                 (<AND <VERB? TELL-ABOUT> <PRSO? ,CURRENT-PLAYER>>
                  <WITH-GLOBAL ((WINNER ,CURRENT-PLAYER))
                      <PERFORM ,V?ASK-ABOUT ,BUSKER ,PRSI>>
                  <RTRUE>)
                 (<VERB? SGIVE>
                  <PERFORM ,V?GIVE ,PRSI ,PRSO>
                  <RTRUE>)
                 (<AND <VERB? GIVE> <PRSI? ,CURRENT-PLAYER>>
                  <WITH-GLOBAL ((WINNER ,CURRENT-PLAYER))
                      <PERFORM ,V?TAKE ,PRSO>>
                  <RTRUE>)
                 (ELSE
                  <TELL "He doesn't respond." CR>
                  <RTRUE>)>)
          (<VERB? EXAMINE>
           <TELL "The organ grinder has a barrel organ hanging from a strap around his shoulders." CR>)
          (<AND <VERB? ASK-ABOUT TELL-ABOUT> <PRSO? ,BUSKER>>
           <COND (<PRSI? ,BUSKER>
                  <TELL "\"Just trying to make a living,\" the busker says." CR>)
                 (<MONKEY? ,PRSI>
                  <TELL "\"Every self-respecting organ grinder needs a monkey.\"" CR>)
                 (ELSE
                  <TELL "\"If it's not about monkeys, I don't know,\" he says."
                        CR>)>
           <RTRUE>)
          (<AND <VERB? GIVE> <PRSI? ,BUSKER>>
           <COND (<NOT <MONKEY? ,PRSO>> <TELL "The busker scoffs, \"You call that a monkey?\"" CR>)
                 (<FSET? ,PRSO ,SOLDBIT> <TELL "The busker cocks his head and raises one eyebrow. \"You gave me that one already, remember?\"" CR>)
                 (<NOT <FSET? ,PRSO ,TAMEBIT>>
                  <TELL "The busker says, \"How'd you even get that one in here? Take it away before someone gets bitten!\""
                        CR>
                  <RTRUE>)
                 (<L? <SET OFFER <BUSKER-OFFER>> 1>
                  <TELL "The busker says, \"No more.\"" CR>
                  <RTRUE>)
                 (ELSE
                  <SETG PLAYER-GOLD <+ ,PLAYER-GOLD .OFFER>>
                  <SETG BUSKER-MONKEY-SALES <+ ,BUSKER-MONKEY-SALES 1>>
                  <SETG STATS-GOLD-EARNED-BUSKER <+ ,STATS-GOLD-EARNED-BUSKER .OFFER>>
                  <FCLEAR ,PRSO ,TAMEBIT>
                  <FSET ,PRSO ,SOLDBIT>
                  <FSET ,PRSO ,NDESCBIT>
                  <MOVE ,PRSO ,HERE> ;"just in case it isn't already"
                  <TELL "The busker counts out " N .OFFER
                        " gold and takes the monkey." CR>
                  <RTRUE>)>)
          (ELSE <RFALSE>)>>

<ROUTINE MONKEY-DESCFCN (ARG)
    <COND (<==? .ARG ,M-OBJDESC?> T)
          (<==? .ARG ,M-OBJDESC>
           <TELL "A monkey seems to have followed you here." CR>)>>

<CONSTANT MONKEY-IGNORES "The monkey pays no attention.">

<ROUTINE ENEMY-ACTION-MONKEY (ARG)
    <COND (<VERB? TELL>
           <SETG P-CONT 0>
           <TELL ,MONKEY-IGNORES CR>)
          (<VERB? TAKE>
           <TELL "The monkey scurries away, staying just out of your reach." CR>)
          (<VERB? EXAMINE>
           <COND (<FSET? ,PRSO ,SOLDBIT>
                  <TELL "The monkey is tied to the organ by a string, but seems happy." CR>)
                 (ELSE <TELL "The monkey is scurrying around your feet. It seems friendly." CR>)>)
          (<OR <AND <MONKEY? ,PRSO> <VERB? SGIVE ASK-ABOUT TELL-ABOUT>>
               <AND <MONKEY? ,PRSI> <VERB? GIVE>>>
           <TELL ,MONKEY-IGNORES CR>)>>

<ROUTINE MONKEY-GENERIC-FCN (TBL "AUX" MAX O)
    ;"Prefer tame monkeys."
    <SET MAX <GET .TBL 0>>
    <DO (I 1 .MAX)
        <SET O <GET .TBL .I>>
        <COND (<AND <MONKEY? .O> <FSET? .O ,TAMEBIT>> <RETURN .O>)>>
    <>>
