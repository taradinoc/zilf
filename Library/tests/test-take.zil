<VERSION ZIP>

<INSERT-FILE "parser">

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

<INSERT-FILE "testing">

<TEST-SETUP ()
    <MOVE ,WINNER ,STARTROOM>
    <MOVE ,APPLE ,STARTROOM>
    <MOVE ,BANANA ,STARTROOM>
    <MOVE ,HAT ,STARTROOM>
    <FCLEAR ,HAT ,WORNBIT>
    <MOVE ,CAGE ,STARTROOM>
    <MOVE ,DESK ,STARTROOM>
    <MOVE ,BUCKET ,STARTROOM>>

<TEST-CASE ("Take all")
    <COMMAND [TAKE ALL]>
    <EXPECT "bucket: That's not something you can pick up.|
desk: That's not something you can pick up.|
cage: That's not something you can pick up.|
hat: You wear the hat.|
banana: You pick up the banana.|
apple: You pick up the apple.|">
    <CHECK <IN? ,APPLE ,WINNER>>
    <CHECK <IN? ,BANANA ,WINNER>>
    <CHECK <AND <IN? ,HAT ,WINNER> <FSET? ,HAT ,WORNBIT>>>
    <CHECK <NOT <IN? ,CAGE ,WINNER>>>
    <CHECK <NOT <IN? ,DESK ,WINNER>>>
    <CHECK <NOT <IN? ,BUCKET ,WINNER>>>
    <CHECK <NOT <IN? ,WINNER ,WINNER>>>>

<TEST-CASE ("Take with no noun and one candidate")
    <REMOVE ,APPLE>
    <COMMAND [TAKE]>
    <EXPECT "[the apple]|You pick up the apple.|">
    <CHECK <IN? ,APPLE ,WINNER>>>

<TEST-CASE ("Take with no noun and multiple candidates")
    <COMMAND [TAKE]>
    <EXPECT "What do you want to take?|">>

<TEST-CASE ("Take with no noun and no candidates")
    <REMOVE ,APPLE>
    <REMOVE ,BANANA>
    <COMMAND [TAKE]>
    <EXPECT "What do you want to take?|">>

<TEST-CASE ("Take nearby object")
    <COMMAND [TAKE APPLE]>
    <EXPECT "You pick up the apple.|">
    <CHECK <IN? ,APPLE ,WINNER>>>

<TEST-CASE ("Take untakeable object")
    <COMMAND [TAKE DESK]>
    <EXPECT "That's not something you can pick up.|">
    <CHECK <IN? ,DESK ,STARTROOM>>>

<TEST-CASE ("Take carried object")
    <MOVE ,APPLE ,WINNER>
    <COMMAND [TAKE APPLE]>
    <EXPECT "You already have that.|">
    <CHECK <IN? ,APPLE ,WINNER>>>

<TEST-CASE ("Take object from surface")
    <MOVE ,APPLE ,DESK>
    <COMMAND [TAKE APPLE]>
    <EXPECT "You pick up the apple.|">
    <CHECK <IN? ,APPLE ,WINNER>>>

<TEST-CASE ("Take object from open container")
    <MOVE ,APPLE ,BUCKET>
    <COMMAND [TAKE APPLE]>
    <EXPECT "You reach in the bucket and take the apple.|">
    <CHECK <IN? ,APPLE ,WINNER>>>

<TEST-CASE ("Take object from closed container")
    <MOVE ,APPLE ,CAGE>
    <COMMAND [TAKE APPLE]>
    <EXPECT "The cage is in the way.|">
    <CHECK <IN? ,APPLE ,CAGE>>>

<TEST-CASE ("Take object from nested container with indirect block")
    <MOVE ,APPLE ,BUCKET>
    <MOVE ,BUCKET ,CAGE>
    <MOVE ,CAGE ,DESK>
    <COMMAND [TAKE APPLE]>
    <EXPECT "The cage is in the way.|">
    <CHECK <NOT <IN? ,APPLE ,WINNER>>>
    <MOVE ,HAT ,BUCKET>
    <COMMAND [TAKE HAT]>
    <EXPECT "The cage is in the way.|">
    <CHECK <NOT <IN? ,HAT ,WINNER>>>>

<TEST-GO ,STARTROOM>
