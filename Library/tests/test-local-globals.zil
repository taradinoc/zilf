;"https://bitbucket.org/jmcgrew/zilf/issue/25/local-globals-doesnt-work"
<VERSION ZIP>

<INSERT-FILE "testing">

<ROOM PILL-ROOM
    (DESC "Strange Room")
    (LOC ROOMS)
    (GLOBAL WOODEN-DOOR)
    (LDESC "The room looks like it's been unhabited for a long time.")
    (EAST TO CIRCULAR-HALL)
    (FLAGS ONBIT LIGHTBIT)>

<OBJECT WOODEN-DOOR
    (DESC "Wooden door")
    (SYNONYM DOOR)
    (ADJECTIVE WOODEN)
    (LOC LOCAL-GLOBALS)
    (ACTION WOODEN-DOOR-R)
    (FDESC "There is a big wooden door.")
    (SIZE 50)>

<ROUTINE WOODEN-DOOR-R ()
    <COND (<VERB? EXAMINE>
           <TELL "What a great door." CR>)>>

<ROOM CIRCULAR-HALL
    (DESC "Circular Hall")
    (LOC ROOMS)
    (WEST TO PILL-ROOM)
    (FLAGS ONBIT LIGHTBIT)>

<TEST-SETUP ()
    <MOVE ,WINNER ,PILL-ROOM>>

<TEST-CASE ("Refer to wooden door where it exists")
    <COMMAND [EXAMINE WOODEN DOOR]>
    <EXPECT "What a great door.|">>

<TEST-CASE ("Refer to wooden door where it doesn't exist")
    <COMMAND [EAST]>
    <COMMAND [EXAMINE WOODEN DOOR]>
    <EXPECT "You don't see that here.|">>

<TEST-GO ,PILL-ROOM>
