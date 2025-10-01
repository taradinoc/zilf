<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (LDESC "Ldesc.")
    (FLAGS LIGHTBIT)>

<OBJECT BOX
    (IN STARTROOM)
    (DESC "box")
    (SYNONYM BOX)
    (FLAGS CONTBIT OPENABLEBIT TAKEBIT)
    (CAPACITY 15)>

<OBJECT ROCK
    (IN STARTROOM)
    (DESC "rock")
    (SYNONYM ROCK)
    (FLAGS TAKEBIT)
    (SIZE 10)>

<TEST-SETUP ()
    <MOVE ,WINNER ,STARTROOM>
    <MOVE ,BOX ,STARTROOM>
    <MOVE ,ROCK ,STARTROOM>
    <FSET ,BOX ,OPENBIT>>

<TEST-CASE ("Put object in container that has CAPACITY but default SIZE on container")
    <COMMAND [PUT ROCK IN BOX]>
    <EXPECT "[taking the rock]|You put the rock in the box.|">>

<TEST-GO ,STARTROOM>
