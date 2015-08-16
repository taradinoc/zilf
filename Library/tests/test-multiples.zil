<VERSION ZIP>

<COMPILATION-FLAG DEBUG T>

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

<TEST-CASE ("Take all")
    <COMMAND [TAKE ALL]>
    <EXPECT "hat: Taken (and worn).|
banana: Taken.|
apple: Taken.|">
    <CHECK <IN? ,HAT ,WINNER>>
    <CHECK <IN? ,APPLE ,WINNER>>
    <CHECK <IN? ,BANANA ,WINNER>>
    <CHECK <AND <IN? ,HAT ,WINNER> <FSET? ,HAT ,WORNBIT>>>
    <CHECK <NOT <IN? ,CAGE ,WINNER>>>
    <CHECK <NOT <IN? ,DESK ,WINNER>>>
    <CHECK <NOT <IN? ,BUCKET ,WINNER>>>
    <CHECK <NOT <IN? ,WINNER ,WINNER>>>>

<TEST-CASE ("Exclude one object with BUT")
    <COMMAND [TAKE ALL BUT BANANA]>
    <EXPECT "hat: Taken (and worn).|
apple: Taken.|">
    <CHECK <IN? ,HAT ,WINNER>>
    <CHECK <IN? ,APPLE ,WINNER>>
    <CHECK <NOT <IN? ,BANANA ,WINNER>>>
    <CHECK <AND <IN? ,HAT ,WINNER> <FSET? ,HAT ,WORNBIT>>>
    <CHECK <NOT <IN? ,CAGE ,WINNER>>>
    <CHECK <NOT <IN? ,DESK ,WINNER>>>
    <CHECK <NOT <IN? ,BUCKET ,WINNER>>>
    <CHECK <NOT <IN? ,WINNER ,WINNER>>>>

<TEST-CASE ("Exclude two objects with BUT")
    <COMMAND [TAKE ALL BUT BANANA AND APPLE]>
    <EXPECT "You wear the hat.|">
    <CHECK <IN? ,HAT ,WINNER>>
    <CHECK <NOT <IN? ,APPLE ,WINNER>>>
    <CHECK <NOT <IN? ,BANANA ,WINNER>>>
    <CHECK <AND <IN? ,HAT ,WINNER> <FSET? ,HAT ,WORNBIT>>>
    <CHECK <NOT <IN? ,CAGE ,WINNER>>>
    <CHECK <NOT <IN? ,DESK ,WINNER>>>
    <CHECK <NOT <IN? ,BUCKET ,WINNER>>>
    <CHECK <NOT <IN? ,WINNER ,WINNER>>>>

<TEST-CASE ("Take individual objects with AND")
    <COMMAND [TAKE HAT AND BANANA]>
    <EXPECT "hat: Taken (and worn).|
banana: Taken.|">
    <CHECK <AND <IN? ,HAT ,WINNER> <FSET? ,HAT ,WORNBIT>>>
    <CHECK <IN? ,BANANA ,WINNER>>
    <CHECK <NOT <IN? APPLE ,WINNER>>>>

<TEST-CASE ("Drop all")
    <MOVE ,HAT ,WINNER>
    <MOVE ,BANANA ,WINNER>
    <MOVE ,APPLE ,WINNER>
    <COMMAND [DROP ALL]>
    <EXPECT "apple: Dropped.|
banana: Dropped.|
hat: Dropped.|">
    <CHECK <NOT <IN? ,HAT ,WINNER>>>
    <CHECK <NOT <IN? ,BANANA ,WINNER>>>
    <CHECK <NOT <IN? ,APPLE ,WINNER>>>>

<TEST-CASE ("Examine all")
    <COMMAND [EXAMINE ALL]>
    <EXPECT "bucket: You see nothing special about the bucket.|
desk: You see nothing special about the desk.|
cage: The cage is closed.|
hat: You see nothing special about the hat.|
banana: You see nothing special about the banana.|
apple: You see nothing special about the apple.|">>

<TEST-CASE ("Eat all")
    <COMMAND [EAT ALL]>
    <EXPECT "You can't use multiple direct objects with \"eat\".|">>

<TEST-CASE ("Put all in all")
    <MOVE ,APPLE ,WINNER>
    <MOVE ,BANANA ,WINNER>
    <MOVE ,HAT ,WINNER>
    <COMMAND [PUT ALL IN ALL]>
    <EXPECT "You can't use multiple indirect objects with \"put\".|">>

<TEST-CASE ("Present objects AND non-present objects")
    <REMOVE ,APPLE>
    <COMMAND [GET APPLE AND HAT]>
    <EXPECT "You don't see that here.|">>

<TEST-CASE ("GET CUBE with one matching object in location")
    <MOVE ,RED-CUBE ,STARTROOM>
    <MOVE ,GREEN-CUBE ,WINNER>
    <MOVE ,BLUE-CUBE ,WINNER>
    <COMMAND [GET CUBE]>
    <EXPECT "You pick up the red cube.|">>

<TEST-CASE ("GET CUBE with two matching objects in location")
    <MOVE ,RED-CUBE ,STARTROOM>
    <MOVE ,GREEN-CUBE ,STARTROOM>
    <MOVE ,BLUE-CUBE ,WINNER>
    <COMMAND [GET CUBE]>
    <EXPECT "Which do you mean, the green cube or the red cube?|">>

<TEST-CASE ("GET CUBE with two matching objects in inventory")
    <MOVE ,RED-CUBE ,WINNER>
    <MOVE ,GREEN-CUBE ,WINNER>
    <COMMAND [GET CUBE]>
    <EXPECT "Which do you mean, the green cube or the red cube?|">>

<TEST-CASE ("GET ALL CUBES with two matching objects in inventory")
    <MOVE ,RED-CUBE ,WINNER>
    <MOVE ,GREEN-CUBE ,WINNER>
    <COMMAND [GET ALL CUBES]>
    <EXPECT "green cube: You already have that.|
red cube: You already have that.|">>

<TEST-CASE ("GET ALL CUBES with one matching object in location")
    <MOVE ,RED-CUBE ,STARTROOM>
    <MOVE ,GREEN-CUBE ,WINNER>
    <MOVE ,BLUE-CUBE ,WINNER>
    <COMMAND [GET ALL CUBES]>
    <EXPECT "You pick up the red cube.|">>

<TEST-CASE ("GET ALL CUBES EXCEPT GREEN")
    <MOVE ,RED-CUBE ,STARTROOM>
    <MOVE ,GREEN-CUBE ,STARTROOM>
    <MOVE ,BLUE-CUBE ,STARTROOM>
    <COMMAND [GET ALL CUBES EXCEPT GREEN]>
    <EXPECT "blue cube: Taken.|
red cube: Taken.|">>

<TEST-GO ,STARTROOM>
