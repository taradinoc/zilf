<VERSION ZIP>

<INSERT-FILE "parser">

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

<INSERT-FILE "testing">

<TEST-SETUP ()
    ;"In case a test leaves it in the wrong place..."
    <MOVE ,RED-CUBE ,STARTROOM>
    <MOVE ,GREEN-CUBE ,STARTROOM>
    <MOVE ,BLUE-CUBE ,STARTROOM>
    <REMOVE ,ROYAL-NAVY>>

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
    <EXPECT "blue cube: You pick up the blue cube.|
green cube: You pick up the green cube.|
red cube: You pick up the red cube.|">
    <CHECK <IN? ,RED-CUBE ,WINNER>>
    <CHECK <IN? ,GREEN-CUBE ,WINNER>>
    <CHECK <IN? ,BLUE-CUBE ,WINNER>>>

<TEST-CASE ("Disambiguate: ALL BUT")
    <COMMAND [GET CUBE]>
    <EXPECT "Which do you mean, the blue cube, the green cube, or the red cube?|">
    <COMMAND [ALL BUT BLUE]>
    <EXPECT "green cube: You pick up the green cube.|
red cube: You pick up the red cube.|">
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
    <EXPECT "blue cube: You pick up the blue cube.|
green cube: You pick up the green cube.|
red cube: You pick up the red cube.|">
    <CHECK <IN? ,RED-CUBE ,WINNER>>
    <CHECK <IN? ,GREEN-CUBE ,WINNER>>
    <CHECK <IN? ,BLUE-CUBE ,WINNER>>>

<TEST-CASE ("Missing noun: ALL BUT")
    <COMMAND [GET]>
    <EXPECT "What do you want to get?|">
    <COMMAND [ALL BUT BLUE]>
    <EXPECT "green cube: You pick up the green cube.|
red cube: You pick up the red cube.|">
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

<TEST-CASE ("Missing second noun")
    <COMMAND [THROW RED CUBE]>
    <EXPECT "[taking the red cube]|What do you want to throw the red cube at?|">
    <COMMAND [BLUE CUBE]>
    <EXPECT "Taking your frustration out on the blue cube doesn't seem like it will help.|">>

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

<TEST-GO ,STARTROOM>
