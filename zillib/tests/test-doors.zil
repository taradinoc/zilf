<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (LDESC "Ldesc.")
    (GLOBAL WOODEN-DOOR)
    (EAST TO HALLWAY IF WOODEN-DOOR IS OPEN ELSE "The wooden door stops you.")
    (FLAGS LIGHTBIT)>

<OBJECT HALLWAY
    (IN ROOMS)
    (DESC "Hallway")
    (LDESC "Ldesc.")
    (GLOBAL WOODEN-DOOR)
    (WEST TO STARTROOM IF WOODEN-DOOR IS OPEN)
    (FLAGS LIGHTBIT)>

<OBJECT WOODEN-DOOR
    (IN LOCAL-GLOBALS)
    (DESC "wooden door")
    (ADJECTIVE WOODEN)
    (SYNONYM DOOR)
    (FLAGS DOORBIT OPENABLEBIT)>

<TEST-SETUP ()
    <MOVE ,WINNER ,STARTROOM>
    <FCLEAR ,WOODEN-DOOR ,OPENBIT>
    <FCLEAR ,STARTROOM ,TOUCHBIT>
    <FCLEAR ,HALLWAY ,TOUCHBIT>>

<TEST-CASE ("Blocked when closed")
    <COMMAND [EAST]>
    <EXPECT "The wooden door stops you.|">
    <CHECK <IN? ,WINNER ,STARTROOM>>
    <MOVE ,WINNER ,HALLWAY>
    <COMMAND [WEST]>
    <EXPECT "You'll have to open the wooden door first.|">
    <CHECK <IN? ,WINNER ,HALLWAY>>>

<TEST-CASE ("Passable when open")
    <FSET ,WOODEN-DOOR ,OPENBIT>
    <COMMAND [EAST]>
    <EXPECT "Hallway|Ldesc.|">
    <CHECK <IN? ,WINNER ,HALLWAY>>
    <COMMAND [WEST]>
    <EXPECT "Start Room|Ldesc.|">
    <CHECK <IN? ,WINNER ,STARTROOM>>>

<TEST-CASE ("Enterable when open")
    <FSET ,WOODEN-DOOR ,OPENBIT>
    <COMMAND [ENTER WOODEN DOOR]>
    <EXPECT "Hallway|Ldesc.|">
    <CHECK <IN? ,WINNER ,HALLWAY>>
    <COMMAND [GO THROUGH WOODEN DOOR]>
    <EXPECT "Start Room|Ldesc.|">
    <CHECK <IN? ,WINNER ,STARTROOM>>>

<TEST-GO ,STARTROOM>
