<VERSION ZIP>

<INSERT-FILE "parser">

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

<INSERT-FILE "testing">

<TEST-SETUP ()
    ;"In case a test leaves it in the wrong place..."
    <MOVE ,APPLE ,STARTROOM>
    <MOVE ,NAIL ,STARTROOM>>

<TEST-CASE ("EAT without noun")
    <COMMAND [EAT]>
    <EXPECT "[the apple]|[taking the apple]|You devour the apple.|">
    <CHECK <IN? ,APPLE <>>>>

<TEST-GO ,STARTROOM>
