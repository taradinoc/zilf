<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (FLAGS LIGHTBIT)>

<OBJECT RED-CUBE
    (DESC "red cube")
    (SYNONYM CUBE CUBES)
    (ADJECTIVE RED RG RB)
    (FLAGS TAKEBIT LOCKEDBIT)>

<OBJECT GREEN-CUBE
    (DESC "green cube")
    (SYNONYM CUBE CUBES)
    (ADJECTIVE GREEN RG GB)
    (FLAGS TAKEBIT)>

<OBJECT BLUE-CUBE
    (DESC "blue cube")
    (SYNONYM CUBE CUBES)
    (ADJECTIVE BLUE RB GB NAVY)
    (FLAGS TAKEBIT)>

<OBJECT ROYAL-NAVY
    (DESC "royal navy")
    (SYNONYM NAVY)
    (ADJECTIVE ROYAL)
    (FLAGS TAKEBIT)>

<OBJECT DARK-GREEN-CUBE
    (DESC "dark green cube")
    (SYNONYM CUBE CUBES)
    (ADJECTIVE DARK GREEN)
    (FLAGS TAKEBIT)>

<OBJECT DARK-BLUE-CUBE
    (DESC "dark blue cube")
    (SYNONYM CUBE CUBES)
    (ADJECTIVE DARK BLUE)
    (FLAGS TAKEBIT)>

<OBJECT BUGLE
    (DESC "bugle")
    (SYNONYM BUGLE)
    (FLAGS TAKEBIT EDIBLEBIT)>

<TEST-SETUP ()
    ;"In case a test leaves it in the wrong place..."
    <MOVE ,RED-CUBE ,STARTROOM>
    <MOVE ,GREEN-CUBE ,STARTROOM>
    <MOVE ,BLUE-CUBE ,STARTROOM>
    <REMOVE ,ROYAL-NAVY>
    <REMOVE ,DARK-GREEN-CUBE>
    <REMOVE ,DARK-BLUE-CUBE>
    <REMOVE ,BUGLE>>

<TEST-CASE ("Disambiguate: single object")
    <COMMAND [GET CUBE]>
    <EXPECT "Which do you mean, the blue cube, the green cube, or the red cube?|">
    <COMMAND [BLUE]>
    <EXPECT "You pick up the blue cube.|">
    <CHECK <IN? ,BLUE-CUBE ,WINNER>>>

<TEST-CASE ("Disambiguate: ALL")
    <COMMAND [GET CUBE]>
    <EXPECT "Which do you mean, the blue cube, the green cube, or the red cube?|">
    <COMMAND [ALL]>
    <EXPECT "blue cube: Taken.|
green cube: Taken.|
red cube: Taken.|">
    <CHECK <IN? ,RED-CUBE ,WINNER>>
    <CHECK <IN? ,GREEN-CUBE ,WINNER>>
    <CHECK <IN? ,BLUE-CUBE ,WINNER>>>

<TEST-CASE ("Disambiguate: ALL BUT")
    <COMMAND [GET CUBE]>
    <EXPECT "Which do you mean, the blue cube, the green cube, or the red cube?|">
    <COMMAND [ALL BUT BLUE]>
    <EXPECT "green cube: Taken.|
red cube: Taken.|">
    <CHECK <IN? ,RED-CUBE ,WINNER>>
    <CHECK <IN? ,GREEN-CUBE ,WINNER>>
    <CHECK <NOT <IN? ,BLUE-CUBE ,WINNER>>>>

<TEST-CASE ("Disambiguate: ANY")
    <COMMAND [GET CUBE]>
    <EXPECT "Which do you mean, the blue cube, the green cube, or the red cube?|">
    <COMMAND [ANY]>
    <CHECK <FIRST? ,WINNER>>
    <CHECK <NOT <NEXT? <FIRST? ,WINNER>>>>>

<TEST-CASE ("Disambiguate: ANY BUT")
    <REMOVE ,RED-CUBE>
    <COMMAND [GET CUBE]>
    <EXPECT "Which do you mean, the blue cube or the green cube?|">
    <COMMAND [ANY BUT BLUE]>
    <EXPECT "You pick up the green cube.|">
    <CHECK <IN? ,GREEN-CUBE ,WINNER>>
    <CHECK <NOT <IN? ,BLUE-CUBE ,WINNER>>>>

<TEST-CASE ("Disambiguate: still ambiguous")
    <COMMAND [GET CUBE]>
    <EXPECT "Which do you mean, the blue cube, the green cube, or the red cube?|">
    <COMMAND [CUBE]>
    <EXPECT "That didn't narrow it down at all. Try rephrasing the command.|">
    <COMMAND [RED]>
    <EXPECT "That sentence has no verb.|">>

<TEST-CASE ("Disambiguation: slightly less ambiguous")
    <COMMAND [GET CUBE]>
    <EXPECT "Which do you mean, the blue cube, the green cube, or the red cube?|">
    <COMMAND [RG]>
    <EXPECT "That narrowed it down a little. Which do you mean, the green cube or the red cube?|">
    <COMMAND [RED]>
    <EXPECT "You pick up the red cube.|">
    <CHECK <IN? ,RED-CUBE ,WINNER>>>

<TEST-CASE ("Disambiguate: irrelevant noun phrase")
    <COMMAND [GET CUBE]>
    <EXPECT "Which do you mean, the blue cube, the green cube, or the red cube?|">
    <COMMAND [ME]>
    <EXPECT "That wasn't an option. Try rephrasing the command.|">
    <COMMAND [RED]>
    <EXPECT "That sentence has no verb.|">>

<TEST-CASE ("Disambiguate: retry command")
    <COMMAND [GET CUBE]>
    <EXPECT "Which do you mean, the blue cube, the green cube, or the red cube?|">
    <COMMAND [X RED CUBE]>
    <EXPECT "You see nothing special about the red cube.|">>

<TEST-CASE ("Disambiguate: noun as adjective")
    <COMMAND [GET CUBE]>
    <EXPECT "Which do you mean, the blue cube, the green cube, or the red cube?|">
    <COMMAND [NAVY]>
    <EXPECT "You pick up the blue cube.|">>

<TEST-CASE ("Noun as adjective: quality distinction")
    <MOVE ,ROYAL-NAVY ,STARTROOM>
    <COMMAND [GET NAVY]>
    <EXPECT "You pick up the royal navy.|">
    <CHECK <IN? ,ROYAL-NAVY ,WINNER>>>

<TEST-CASE ("Missing noun: single object")
    <COMMAND [GET]>
    <EXPECT "What do you want to get?|">
    <COMMAND [RED CUBE]>
    <EXPECT "You pick up the red cube.|">
    <CHECK <IN? ,RED-CUBE ,WINNER>>>

<TEST-CASE ("Missing noun: ALL")
    <COMMAND [GET]>
    <EXPECT "What do you want to get?|">
    <COMMAND [ALL]>
    <EXPECT "blue cube: Taken.|
green cube: Taken.|
red cube: Taken.|">
    <CHECK <IN? ,RED-CUBE ,WINNER>>
    <CHECK <IN? ,GREEN-CUBE ,WINNER>>
    <CHECK <IN? ,BLUE-CUBE ,WINNER>>>

<TEST-CASE ("Missing noun: ALL BUT")
    <COMMAND [GET]>
    <EXPECT "What do you want to get?|">
    <COMMAND [ALL BUT BLUE]>
    <EXPECT "green cube: Taken.|
red cube: Taken.|">
    <CHECK <IN? ,RED-CUBE ,WINNER>>
    <CHECK <IN? ,GREEN-CUBE ,WINNER>>
    <CHECK <NOT <IN? ,BLUE-CUBE ,WINNER>>>>

<TEST-CASE ("Missing noun: ANY")
    <COMMAND [GET]>
    <EXPECT "What do you want to get?|">
    <COMMAND [ANY]>
    <CHECK <FIRST? ,WINNER>>
    <CHECK <NOT <NEXT? <FIRST? ,WINNER>>>>>

<TEST-CASE ("Missing noun: ANY BUT")
    <REMOVE ,RED-CUBE>
    <COMMAND [GET]>
    <EXPECT "What do you want to get?|">
    <COMMAND [ANY BUT BLUE]>
    <EXPECT "You pick up the green cube.|">
    <CHECK <IN? ,GREEN-CUBE ,WINNER>>
    <CHECK <NOT <IN? ,BLUE-CUBE ,WINNER>>>>

<TEST-CASE ("Missing noun: ambiguous")
    <COMMAND [GET]>
    <EXPECT "What do you want to get?|">
    <COMMAND [CUBE]>
    <EXPECT "Which do you mean, the blue cube, the green cube, or the red cube?|">
    <COMMAND [BLUE CUBE]>
    <EXPECT "You pick up the blue cube.|">
    <CHECK <IN? ,BLUE-CUBE ,WINNER>>>

<TEST-CASE ("Missing noun: number")
    <COMMAND [GET]>
    <EXPECT "What do you want to get?|">
    <COMMAND ["244"]>
    <EXPECT "That's not something you can pick up.|">>

<TEST-CASE ("Missing second noun: single object")
    <COMMAND [THROW RED CUBE]>
    <EXPECT "[taking the red cube]|What do you want to throw the red cube at?|">
    <COMMAND [BLUE CUBE]>
    <EXPECT "Taking your frustration out on the blue cube doesn't seem like it will help.|">>

<TEST-CASE ("Missing second noun: number")
    <COMMAND [THROW RED CUBE]>
    <EXPECT "[taking the red cube]|What do you want to throw the red cube at?|">
    <COMMAND ["244"]>
    <EXPECT "Taking your frustration out on the number doesn't seem like it will help.|">>

<TEST-CASE ("GWIMmed PRSO, disambiguated PRSI")
    <MOVE ,BLUE-CUBE ,WINNER>
    <COMMAND [UNLOCK]>
    <EXPECT "[the red cube]|What do you want to unlock the red cube with?|">
    <COMMAND [BLUE CUBE]>
    <EXPECT "That's not something you can unlock.|">>

<TEST-CASE ("Missing PRSO, ambiguous orphan response, disambiguate")
    <MOVE ,RED-CUBE ,WINNER>
    <MOVE ,GREEN-CUBE ,WINNER>
    <COMMAND [TOSS RED CUBE AT BLUE CUBE]>
    <COMMAND [TOSS]>
    <EXPECT "What do you want to toss?|">
    <COMMAND [CUBE]>
    <EXPECT "Which do you mean, the green cube or the red cube?|">
    <COMMAND [RED]>
    <EXPECT "What do you want to toss the red cube at?|">>

<TEST-CASE ("AGAIN with object that must be selected with orphaning")
    <MOVE ,DARK-GREEN-CUBE ,STARTROOM>
    <MOVE ,DARK-BLUE-CUBE ,STARTROOM>
    <COMMAND [TAKE DARK GREEN CUBE]>
    <EXPECT "Which do you mean, the dark blue cube or the dark green cube?|"> ;[sic]
    <COMMAND [GREEN]>
    <EXPECT "You pick up the dark green cube.|">
    <COMMAND [AGAIN]>
    <EXPECT "You already have that.|">>

<TEST-CASE ("AGAIN with object that becomes inaccessible")
    <MOVE ,BUGLE ,STARTROOM>
    <COMMAND [EAT BUGLE]>
    <EXPECT "[taking the bugle]|You devour the bugle.|">
    <CHECK <IN? ,BUGLE <>>>
    <COMMAND [AGAIN]>
    <EXPECT "The bugle is no longer here.|">>

<TEST-CASE ("Implicit take of ambiguous noun")
    <COMMAND [EAT CUBE]>
    <EXPECT "Which do you mean, the blue cube, the green cube, or the red cube?|">
    <COMMAND [BLUE]>
    <EXPECT "[taking the blue cube]|That's hardly edible.|">>

<TEST-GO ,STARTROOM>
