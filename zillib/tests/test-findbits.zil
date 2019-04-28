<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
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

<OBJECT TROLL
    (IN STARTROOM)
    (DESC "troll")
    (SYNONYM TROLL)
    (FLAGS PERSONBIT)>

<TEST-SETUP ()
    ;"In case a test leaves it in the wrong place..."
    <MOVE ,APPLE ,STARTROOM>
    <MOVE ,NAIL ,STARTROOM>
    <MOVE ,TROLL ,STARTROOM>>

<TEST-CASE ("EAT without noun")
    <COMMAND [EAT]>
    <EXPECT "[the apple]|[taking the apple]|You devour the apple.|">
    <CHECK <IN? ,APPLE <>>>>

<TEST-CASE ("WAKE without noun")
    <COMMAND [WAKE]>
    <EXPECT "[the troll]|I don't think the troll would appreciate that.|">>

<TEST-CASE ("WAKE without noun when troll is gone")
    <REMOVE ,TROLL>
    <COMMAND [WAKE]>
    <EXPECT "Whom do you want to wake?|">>

<TEST-GO ,STARTROOM>
