<VERSION ZIP>

<INSERT-FILE "parser">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (WEST TO HALLWAY)
    (FLAGS LIGHTBIT)>

<OBJECT HALLWAY
    (IN ROOMS)
    (DESC "Hallway")
    (EAST TO STARTROOM)
    (FLAGS LIGHTBIT)>

<OBJECT APPLE
    (IN STARTROOM)
    (DESC "apple")
    (SYNONYM APPLE)
    (FLAGS VOWELBIT TAKEBIT EDIBLEBIT)>

<OBJECT NAIL
    (IN STARTROOM)
    (DESC "rusty nail")
    (SYNONYM NAIL)
    (ADJECTIVE RUSTY)
    (FLAGS TAKEBIT)>

<OBJECT WALLET
    (IN PLAYER)
    (DESC "wallet")
    (SYNONYM WALLET)
    (FLAGS TAKEBIT)>

<INSERT-FILE "testing">

<TEST-SETUP ()
    ;"In case a test leaves it in the wrong place..."
    <MOVE ,WINNER ,STARTROOM>
    <MOVE ,APPLE ,STARTROOM>
    <MOVE ,NAIL ,STARTROOM>
    <MOVE ,WALLET ,WINNER>>

<TEST-CASE ("TAKE ALL then DROP THEM")
    <COMMAND [TAKE ALL]>
    <EXPECT "rusty nail: You pick up the rusty nail.|
apple: You pick up the apple.|">
    <COMMAND [DROP THEM]>
    <EXPECT "rusty nail: You drop the rusty nail.|
apple: You drop the apple.|">
    <CHECK <NOT <IN? ,APPLE ,WINNER>>>
    <CHECK <NOT <IN? ,NAIL ,WINNER>>>
    <CHECK <IN? ,WALLET ,WINNER>>>

<TEST-CASE ("DROP WALLET then EXAMINE IT in another room")
    <COMMAND [DROP WALLET]>
    <COMMAND [WEST]>
    <COMMAND [EXAMINE IT]>
    <EXPECT "The wallet is no longer here.|">>

<TEST-GO ,STARTROOM>
