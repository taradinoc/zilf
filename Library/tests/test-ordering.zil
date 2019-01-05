<VERSION ZIP>

<COMPILATION-FLAG DEBUG T>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (NORTH TO HALLWAY)
    (FLAGS LIGHTBIT)>

<OBJECT APPLE
    (IN STARTROOM)
    (DESC "apple")
    (SYNONYM APPLE)
    (FLAGS VOWELBIT TAKEBIT)>

<OBJECT ROBOT
    (IN STARTROOM)
    (DESC "robot")
    (SYNONYM ROBOT)
    (ADJECTIVE EVIL)
    (ACTION ROBOT-F)
    (FLAGS PERSONBIT TRANSBIT)>

<OBJECT CAT
    (IN STARTROOM)
    (DESC "cat")
    (SYNONYM CAT)
    (ADJECTIVE EVIL)
    (ACTION CAT-F)
    (FLAGS PERSONBIT TRANSBIT)>

<OBJECT RED-CUBE
    (DESC "red cube")
    (SYNONYM CUBE CUBES)
    (ADJECTIVE RED RG RB)
    (FLAGS TAKEBIT LOCKEDBIT)>

<OBJECT GREEN-CUBE
    (DESC "green cube")
    (SYNONYM CUBE CUBES)
    (ADJECTIVE GREEN RG GB)
    (FLAGS TAKEBIT)>

<OBJECT BLUE-CUBE
    (DESC "blue cube")
    (SYNONYM CUBE CUBES)
    (ADJECTIVE BLUE RB GB NAVY)
    (FLAGS TAKEBIT)>

<OBJECT HALLWAY
    (IN ROOMS)
    (DESC "Hallway")
    (SOUTH TO STARTROOM)
    (FLAGS LIGHTBIT)>

<ROUTINE ROBOT-F (ARG "AUX" PT)
    <COND (<=? .ARG ,M-WINNER>
           <COND (<AND <VERB? TAKE> <FSET? ,PRSO ,TAKEBIT>>
                  <COND (<IN? ,PRSO ,ROBOT>
                         <TELL CT ,ROBOT " already has " T ,PRSO "." CR>)
                        (ELSE
                         <MOVE ,PRSO ,ROBOT>
                         <TELL CT ,ROBOT " picks up " T ,PRSO "." CR>)>)
                 (<AND <VERB? WALK>
                       ,PRSO-DIR
                       <SET PT <GETPT <LOC ,ROBOT> ,PRSO>>
                       <=? <PTSIZE .PT> ,UEXIT>>
                  <TELL CT ,ROBOT " leaves." CR>
                  <MOVE ,ROBOT <GET/B .PT ,EXIT-RM>>)
                 (<AND <VERB? PUSH>
                       <PRSO? ,PLAYER>>
                  <TELL CT ,ROBOT " pushes you into the hallway." CR>
                  <GOTO ,HALLWAY>)
                 (ELSE
                  <TELL "The robot doesn't respond." CR>)>)>>

<ROUTINE CAT-F (ARG)
    <COND (<=? .ARG ,M-WINNER>
           <TELL "The cat doesn't respond." CR>)>>

<TEST-SETUP ()
    <SETG TRACE-LEVEL 0>
    ;"In case a test leaves it in the wrong place..."
    <MOVE ,APPLE ,STARTROOM>
    <MOVE ,ROBOT ,STARTROOM>
    <MOVE ,PLAYER ,STARTROOM>
    <MOVE ,RED-CUBE ,STARTROOM>
    <MOVE ,GREEN-CUBE ,STARTROOM>
    <MOVE ,BLUE-CUBE ,STARTROOM>
    <SETG WINNER ,PLAYER>>

<TEST-CASE ("ROBOT, TAKE APPLE")
    <COMMAND [ROBOT \, TAKE APPLE]>
    <EXPECT "The robot picks up the apple.|">
    <CHECK <IN? ,APPLE ,ROBOT>>>

<TEST-CASE ("TELL ROBOT TO TAKE APPLE")
    <COMMAND [TELL ROBOT TO TAKE APPLE]>
    <EXPECT "The robot picks up the apple.|">
    <CHECK <IN? ,APPLE ,ROBOT>>>

<TEST-CASE ("ROBOT, [GO] DIRECTION")
    <COMMAND [ROBOT \, NORTH]>
    <EXPECT "The robot leaves.|">
    <CHECK <IN? ,ROBOT ,HALLWAY>>
    <CHECK <IN? ,PLAYER ,STARTROOM>>
    <COMMAND [NORTH]>
    <EXPECT "Hallway||A robot is here.|">
    <CHECK <=? ,WINNER ,PLAYER>>
    <CHECK <IN? ,WINNER ,HALLWAY>>
    <COMMAND [ROBOT \, GO SOUTH]>
    <EXPECT "The robot leaves.|">
    <CHECK <IN? ,ROBOT ,STARTROOM>>>

<TEST-CASE ("Ambiguous actor")
    <COMMAND [EVIL \, LOOK]>
    <EXPECT "Which do you mean, the robot or the cat?|">
    <COMMAND [CAT]>
    <EXPECT "The cat doesn't respond.|">>

<TEST-CASE ("Ambiguous actor with oops on actor")
    <COMMAND [EVIL \, LOOK]>
    <EXPECT "Which do you mean, the robot or the cat?|">
    <COMMAND ["XAT"]>
    <EXPECT "I don't know the word \"xat\".|">
    <COMMAND [OOPS CAT]>
    <EXPECT "The cat doesn't respond.|">>

<TEST-CASE ("Ambiguous actor with oops on noun")
    <COMMAND [EVIL \, GET "XUBE"]>
    <EXPECT "Which do you mean, the robot or the cat?|">
    <COMMAND [CAT]>
    <EXPECT "I don't know the word \"xube\".|">
    <COMMAND [OOPS CUBE]>
    <EXPECT "Which do you mean, the blue cube, the green cube, or the red cube?|">
    <COMMAND [GREEN]>
    <EXPECT "The cat doesn't respond.|">>

<TEST-CASE ("Too many actors")
    <COMMAND [RED CUBE AND GREEN CUBE \, LOOK]>
    <EXPECT "You can only address one person at a time.|">>

<TEST-CASE ("Actor not present")
    <REMOVE ,RED-CUBE>
    <COMMAND [RED CUBE \, LOOK]>
    <EXPECT "You don't see that here.|">>

<TEST-CASE ("Missing noun in order")
    <COMMAND [ROBOT \, TAKE]>
    <EXPECT "What do you want the robot to take?|">
    <COMMAND [APPLE]>
    <EXPECT "The robot picks up the apple.|">>

<TEST-CASE ("Ambiguous noun in order")
    <COMMAND [ROBOT \, TAKE CUBE]>
    <EXPECT "Which do you mean, the blue cube, the green cube, or the red cube?|">
    <COMMAND [RED]>
    <EXPECT "The robot picks up the red cube.|">>

<TEST-CASE ("AGAIN after order")
    <COMMAND [ROBOT \, TAKE RED CUBE]>
    <EXPECT "The robot picks up the red cube.|">
    <COMMAND [AGAIN]>
    <EXPECT "The robot already has the red cube.|">>

<TEST-CASE ("GOTO during order")
    <COMMAND [ROBOT \, PUSH ME]>
    <EXPECT "The robot pushes you into the hallway.|Hallway|">
    <CHECK <IN? ,PLAYER ,HALLWAY>>
    <CHECK <IN? ,ROBOT ,STARTROOM>>>

<TEST-GO ,STARTROOM>
