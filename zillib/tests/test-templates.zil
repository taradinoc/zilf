<VERSION ZIP>

<INSERT-FILE "testing">

<USE "TEMPLATE">

<OBJECT-TEMPLATE
    LIGHT-ROOM = ROOM (FLAGS LIGHTBIT)
    PROP = OBJECT (FLAGS NDESCBIT) (ACTION PROP-F) (LOC LOCAL-GLOBALS)
    FURNITURE = OBJECT (FLAGS CONTBIT OPENBIT SURFACEBIT) (ACTION FURNITURE-F)
    NPC = OBJECT (FLAGS PERSONBIT) (ACTION NPC-F)>

<LIGHT-ROOM MEADOW
    (DESC "Meadow")
    (IN ROOMS)>

<PROP ROAD
    (DESC "road")
    (IN MEADOW)
    (ACTION ROAD-F)>

<FURNITURE BENCH
    (DESC "bench")
    (IN MEADOW)
    (SYNONYM SEAT CHAIR)>

<NPC ROBOT
    (DESC "robot")
    (IN MEADOW)
    (FLAGS ROBOTBIT '<NOT PERSONBIT>)>

<SETG CHAPTER-NUMBER 0>

<OBJECT-TEMPLATE
    CHAPTER = OBJECT (NUMBER <SETG CHAPTER-NUMBER <+ ,CHAPTER-NUMBER 1>>)>

<CHAPTER CHAPTER-ONE
    (DESC "Chapter One")
    (IN GLOBAL-OBJECTS)>

<CHAPTER CHAPTER-TWO
    (DESC "Chapter Two")
    (IN GLOBAL-OBJECTS)>

<CHAPTER CHAPTER-THREE
    (DESC "Chapter Three")
    (IN GLOBAL-OBJECTS)>

<ROUTINE ROAD-F ()
    <TELL "You walk along the avenue. You never thought you'd meet a girl like her." CR>>

<ROUTINE FURNITURE-F ()
    <TELL "You contemplate the similarities between writing desks and ravens." CR>>

<ROUTINE NPC-F ()
    <TELL "You know I love the players, and you love the game." CR>>

<TEST-CASE ("Templates should work as documented")
    ;LIGHT-ROOM
    <CHECK <FSET? ,MEADOW ,LIGHTBIT>>
    ;PROP
    <CHECK <FSET? ,ROAD ,NDESCBIT>>
    <CHECK <=? <GETP ,ROAD ,P?ACTION> ROAD-F>>
    <CHECK <IN? ,ROAD ,MEADOW>>
    ;FURNITURE
    <CHECK <FSET? ,BENCH ,CONTBIT>>
    <CHECK <FSET? ,BENCH ,OPENBIT>>
    <CHECK <FSET? ,BENCH ,SURFACEBIT>>
    <CHECK <=? <GETP ,BENCH ,P?ACTION> FURNITURE-F>>
    ;NPC
    <CHECK <FSET? ,ROBOT ,ROBOTBIT>>
    <CHECK <NOT <FSET? ,ROBOT ,PERSONBIT>>>
    <CHECK <=? <GETP ,ROBOT ,P?ACTION> NPC-F>>
    ;CHAPTER
    <CHECK <=? <GETP ,CHAPTER-ONE ,P?NUMBER> 1>>
    <CHECK <=? <GETP ,CHAPTER-TWO ,P?NUMBER> 2>>
    <CHECK <=? <GETP ,CHAPTER-THREE ,P?NUMBER> 3>>
    <CHECK <=? %,CHAPTER-NUMBER 3>>>

<TEST-GO ,MEADOW>