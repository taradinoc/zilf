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
    (PLURAL APPLES)
    (FLAGS VOWELBIT TAKEBIT EDIBLEBIT)>

<OBJECT APPLE2
    (IN STARTROOM)
    (DESC "apple")
    (SYNONYM APPLE)
    (PLURAL APPLES)
    (FLAGS VOWELBIT TAKEBIT EDIBLEBIT)>

<OBJECT BANANA
    (IN STARTROOM)
    (DESC "banana")
    (SYNONYM BANANA)
    (PLURAL BANANAS)
    (FLAGS TAKEBIT EDIBLEBIT)>

<OBJECT BANANA2
    (IN STARTROOM)
    (DESC "banana")
    (SYNONYM FRUIT)
    (PLURAL BANANAS)
    (FLAGS TAKEBIT EDIBLEBIT)>

<OBJECT BANANA3
    (IN STARTROOM)
    (DESC "banana")
    (SYNONYM FRUIT)
    (PLURAL BANANAS)
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

<OBJECT BOW
    (IN STARTROOM)
    (DESC "bow")
    (SYNONYM BOW)
    (FLAGS TAKEBIT)>

<OBJECT ARROW1
    (IN STARTROOM)
    (DESC "arrow")
    (SYNONYM ARROW)
    (PLURAL ARROWS)
    (FLAGS TAKEBIT)>

<OBJECT ARROW2
    (IN STARTROOM)
    (DESC "arrow")
    (SYNONYM ARROW)
    (PLURAL ARROWS)
    (FLAGS TAKEBIT)>

<OBJECT ARROW3
    (IN STARTROOM)
    (DESC "arrow")
    (SYNONYM ARROW)
    (PLURAL ARROWS)
    (FLAGS TAKEBIT)>

<OBJECT ARROW4
    (IN STARTROOM)
    (DESC "arrow")
    (SYNONYM ARROW)
    (PLURAL ARROWS)
    (FLAGS TAKEBIT)>

<ROUTINE COUNT-HELD-ARROWS ("AUX" C)
    <COND (<IN? ,ARROW1 ,WINNER> <SET C <+ .C 1>>)> 
    <COND (<IN? ,ARROW2 ,WINNER> <SET C <+ .C 1>>)> 
    <COND (<IN? ,ARROW3 ,WINNER> <SET C <+ .C 1>>)> 
    <COND (<IN? ,ARROW4 ,WINNER> <SET C <+ .C 1>>)> 
    <RETURN .C>>

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
    <REMOVE ,APPLE2>
    <MOVE ,BANANA ,STARTROOM>
    <REMOVE ,BANANA2>
    <REMOVE ,BANANA3>
    <MOVE ,HAT ,STARTROOM>
    <FCLEAR ,HAT ,WORNBIT>
    <MOVE ,CAGE ,STARTROOM>
    <MOVE ,DESK ,STARTROOM>
    <MOVE ,BUCKET ,STARTROOM>
    <REMOVE ,BOW>
    <REMOVE ,ARROW1>
    <REMOVE ,ARROW2>
    <REMOVE ,ARROW3>
    <REMOVE ,ARROW4>
    <REMOVE ,RED-CUBE>
    <REMOVE ,GREEN-CUBE>
    <REMOVE ,BLUE-CUBE>>

<TEST-CASE ("Take all")
    <COMMAND [TAKE ALL]>
    <EXPECT "hat: Taken.|
banana: Taken.|
apple: Taken.|">
    <CHECK <IN? ,HAT ,WINNER>>
    <CHECK <IN? ,APPLE ,WINNER>>
    <CHECK <IN? ,BANANA ,WINNER>>
    <CHECK <AND <IN? ,HAT ,WINNER> <NOT <FSET? ,HAT ,WORNBIT>>>>
    <CHECK <NOT <IN? ,CAGE ,WINNER>>>
    <CHECK <NOT <IN? ,DESK ,WINNER>>>
    <CHECK <NOT <IN? ,BUCKET ,WINNER>>>
    <CHECK <NOT <IN? ,WINNER ,WINNER>>>>

<TEST-CASE ("Take all when everything is held")
    <MOVE ,HAT ,WINNER>
    <MOVE ,BANANA ,WINNER>
    <MOVE ,APPLE ,WINNER>
    <COMMAND [TAKE ALL]>
    <EXPECT "There are none at all available!|">>

<TEST-CASE ("Exclude one object with BUT")
    <COMMAND [TAKE ALL BUT BANANA]>
    <EXPECT "hat: Taken.|
apple: Taken.|">
    <CHECK <IN? ,HAT ,WINNER>>
    <CHECK <IN? ,APPLE ,WINNER>>
    <CHECK <NOT <IN? ,BANANA ,WINNER>>>
    <CHECK <AND <IN? ,HAT ,WINNER> <NOT <FSET? ,HAT ,WORNBIT>>>>
    <CHECK <NOT <IN? ,CAGE ,WINNER>>>
    <CHECK <NOT <IN? ,DESK ,WINNER>>>
    <CHECK <NOT <IN? ,BUCKET ,WINNER>>>
    <CHECK <NOT <IN? ,WINNER ,WINNER>>>>

<TEST-CASE ("Exclude two objects with BUT")
    <COMMAND [TAKE ALL BUT BANANA AND APPLE]>
    <EXPECT "You pick up the hat.|">
    <CHECK <IN? ,HAT ,WINNER>>
    <CHECK <NOT <IN? ,APPLE ,WINNER>>>
    <CHECK <NOT <IN? ,BANANA ,WINNER>>>
    <CHECK <AND <IN? ,HAT ,WINNER> <NOT <FSET? ,HAT ,WORNBIT>>>>
    <CHECK <NOT <IN? ,CAGE ,WINNER>>>
    <CHECK <NOT <IN? ,DESK ,WINNER>>>
    <CHECK <NOT <IN? ,BUCKET ,WINNER>>>
    <CHECK <NOT <IN? ,WINNER ,WINNER>>>>

<TEST-CASE ("Take individual objects with AND")
    <COMMAND [TAKE HAT AND BANANA]>
    <EXPECT "hat: Taken.|
banana: Taken.|">
    <CHECK <AND <IN? ,HAT ,WINNER> <NOT <FSET? ,HAT ,WORNBIT>>>>
    <CHECK <IN? ,BANANA ,WINNER>>
    <CHECK <NOT <IN? ,APPLE ,WINNER>>>>

<TEST-CASE ("Vocab collision: TAKE BANANA is singular")
    <MOVE ,BANANA2 ,STARTROOM>
    <MOVE ,BANANA3 ,STARTROOM>
    <COMMAND [TAKE BANANA]>
    <CHECK <IN? ,BANANA ,WINNER>>
    <CHECK <NOT <IN? ,BANANA2 ,WINNER>>>
    <CHECK <NOT <IN? ,BANANA3 ,WINNER>>>>

<TEST-CASE ("Distinct plural: TAKE APPLES is all")
    <MOVE ,APPLE2 ,STARTROOM>
    <COMMAND [TAKE APPLES]>
    <CHECK <IN? ,APPLE ,WINNER>>
    <CHECK <IN? ,APPLE2 ,WINNER>>>

<TEST-CASE ("Distinct singular: TAKE APPLE is single")
    <MOVE ,APPLE2 ,STARTROOM>
    <COMMAND [TAKE APPLE]>
    <EXPECT "Which do you mean, the apple or the apple?|">
    <CHECK <NOT <IN? ,APPLE ,WINNER>>>
    <CHECK <NOT <IN? ,APPLE2 ,WINNER>>>>

<TEST-CASE ("Plural noun implies all")
    <MOVE ,APPLE2 ,STARTROOM>
    <COMMAND [TAKE APPLES]>
    <CHECK <IN? ,APPLE ,WINNER>>
    <CHECK <IN? ,APPLE2 ,WINNER>>>

<TEST-CASE ("Quantifier with plural noun picks any count")
    <MOVE ,ARROW1 ,STARTROOM>
    <MOVE ,ARROW2 ,STARTROOM>
    <MOVE ,ARROW3 ,STARTROOM>
    <MOVE ,ARROW4 ,STARTROOM>
    <COMMAND [TAKE TWO ARROWS]>
    <CHECK <=? <COUNT-HELD-ARROWS> 2>>>

<TEST-CASE ("TAKE TWO plural objects excludes held candidates")
    <MOVE ,ARROW1 ,WINNER>
    <MOVE ,ARROW2 ,STARTROOM>
    <REMOVE ,ARROW3>
    <REMOVE ,ARROW4>
    <COMMAND [TAKE TWO ARROWS]>
    <EXPECT "There is only 1 available.|">
    <CHECK <IN? ,ARROW1 ,WINNER>>
    <CHECK <IN? ,ARROW2 ,STARTROOM>>>

<TEST-CASE ("TAKE ONE plural object behaves like ANY")
    <MOVE ,ARROW1 ,STARTROOM>
    <MOVE ,ARROW2 ,STARTROOM>
    <MOVE ,ARROW3 ,WINNER>
    <REMOVE ,ARROW4>
    <COMMAND [TAKE ONE ARROWS]>
    <EXPECT "[the arrow]|You pick up the arrow.|">
    <CHECK <=? <COUNT-HELD-ARROWS> 2>>>

<TEST-CASE ("TAKE ONE singular object behaves like ANY")
    <MOVE ,ARROW1 ,STARTROOM>
    <MOVE ,ARROW2 ,STARTROOM>
    <MOVE ,ARROW3 ,WINNER>
    <REMOVE ,ARROW4>
    <COMMAND [TAKE ONE ARROW]>
    <EXPECT "[the arrow]|You pick up the arrow.|">
    <CHECK <=? <COUNT-HELD-ARROWS> 2>>>

<TEST-CASE ("Quantifier applies per OBJSPEC")
    <MOVE ,BOW ,STARTROOM>
    <MOVE ,ARROW1 ,STARTROOM>
    <MOVE ,ARROW2 ,STARTROOM>
    <MOVE ,ARROW3 ,STARTROOM>
    <MOVE ,ARROW4 ,STARTROOM>
    <COMMAND [TAKE BOW AND THREE ARROWS]>
    <CHECK <IN? ,BOW ,WINNER>>
    <CHECK <=? <COUNT-HELD-ARROWS> 3>>>

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

<TEST-CASE ("DROP plural noun behaves like DROP ALL")
    <MOVE ,ARROW1 ,WINNER>
    <MOVE ,ARROW2 ,WINNER>
    <MOVE ,ARROW3 ,STARTROOM>
    <REMOVE ,ARROW4>
    <COMMAND [DROP ARROWS]>
    <CHECK <NOT <IN? ,ARROW1 ,WINNER>>>
    <CHECK <NOT <IN? ,ARROW2 ,WINNER>>>
    <CHECK <IN? ,ARROW3 ,STARTROOM>>>

<TEST-CASE ("Drop all while empty-handed")
    <COMMAND [DROP ALL]>
    <EXPECT "There are none at all available!|">>

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

<TEST-CASE ("Orphan response ANY TWO")
    <MOVE ,RED-CUBE ,STARTROOM>
    <MOVE ,GREEN-CUBE ,STARTROOM>
    <REMOVE ,BLUE-CUBE>
    <COMMAND [GET CUBE]>
    <EXPECT "Which do you mean, the green cube or the red cube?|">
    <COMMAND [ANY TWO]>
    <EXPECT "green cube: Taken.|
red cube: Taken.|">
    <CHECK <IN? ,GREEN-CUBE ,WINNER>>
    <CHECK <IN? ,RED-CUBE ,WINNER>>>

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
