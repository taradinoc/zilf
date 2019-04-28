<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (LDESC "Ldesc.")
    (FLAGS LIGHTBIT)>

<OBJECT APPLE
    (IN STARTROOM)
    (DESC "apple")
    (SYNONYM APPLE)
    (FLAGS VOWELBIT TAKEBIT EDIBLEBIT)>

<OBJECT BANANA
    (IN STARTROOM)
    (DESC "banana")
    (SYNONYM BANANA)
    (FLAGS TAKEBIT EDIBLEBIT)>

<OBJECT HAT
    (IN STARTROOM)
    (DESC "hat")
    (SYNONYM HAT)
    (FLAGS TAKEBIT WEARBIT)>

<OBJECT CAGE
    (IN STARTROOM)
    (DESC "cage")
    (SYNONYM CAGE)
    (FLAGS CONTBIT TRANSBIT OPENABLEBIT)>

<OBJECT DESK
    (IN STARTROOM)
    (DESC "desk")
    (SYNONYM DESK)
    (FLAGS SURFACEBIT)>

<OBJECT BUCKET
    (IN STARTROOM)
    (DESC "bucket")
    (SYNONYM BUCKET)
    (FLAGS CONTBIT OPENBIT)>

<OBJECT RED-CUBE
    (DESC "red cube")
    (SYNONYM CUBE CUBES)
    (ADJECTIVE RED)
    (FLAGS TAKEBIT)>

<OBJECT GREEN-CUBE
    (DESC "green cube")
    (SYNONYM CUBE CUBES)
    (ADJECTIVE GREEN)
    (FLAGS TAKEBIT)>

<OBJECT BLUE-CUBE
    (DESC "blue cube")
    (SYNONYM CUBE CUBES)
    (ADJECTIVE BLUE)
    (FLAGS TAKEBIT)>

<TEST-SETUP ()
    <MOVE ,WINNER ,STARTROOM>
    <MOVE ,APPLE ,STARTROOM>
    <MOVE ,BANANA ,STARTROOM>
    <MOVE ,HAT ,STARTROOM>
    <FCLEAR ,HAT ,WORNBIT>
    <MOVE ,CAGE ,STARTROOM>
    <MOVE ,DESK ,STARTROOM>
    <MOVE ,BUCKET ,STARTROOM>
    <REMOVE ,RED-CUBE>
    <REMOVE ,GREEN-CUBE>
    <REMOVE ,BLUE-CUBE>>

<TEST-CASE ("Multiple commands")
    <COMMAND [PICK THE APPLE UP \. PICK UP BANANA THEN GET HAT \.]>
    <EXPECT "You pick up the apple.|
|
You pick up the banana.|
|
You wear the hat.|">>

<TEST-CASE ("G THEN G")
    <COMMAND [TAKE INVENTORY THEN X ME]>
    <EXPECT "You are empty-handed.|
|
You look like you're up for an adventure.|">
    <COMMAND [G THEN G]>
    <EXPECT "You look like you're up for an adventure.|
|
You look like you're up for an adventure.|">>

;"TODO: test cases to make sure multi-cmds are interrupted when a command 'fails',
  including preaction conditions, HAVE check, pronouns no longer present"

<TEST-GO ,STARTROOM>
