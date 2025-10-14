<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (LDESC "Home sweet home.")
    (FLAGS LIGHTBIT)
    (EAST TO SECONDROOM)
    (NORTH TO THIRDROOM)>

<OBJECT COUCH
    (IN STARTROOM)
    (DESC "couch")
    ;"Learning ZIL: 'All objects with the VEHBIT should usually have the
      CONTBIT and the OPENBIT.'"
    (FLAGS SURFACEBIT VEHBIT TAKEBIT CONTBIT OPENBIT)
    (SYNONYM COUCH)
    (ACTION VEHICLE-F)>

<OBJECT WAGON
    (IN STARTROOM)
    (DESC "wagon")
    (FLAGS VEHBIT TAKEBIT TRANSBIT CONTBIT OPENBIT)
    (SYNONYM WAGON)
    (ACTION VEHICLE-F)>

<OBJECT BOX
    (DESC "box")
    (FLAGS CONTBIT VEHBIT)
    (SYNONYM BOX LOCKEDBIT)>

<ROUTINE VEHICLE-F (ARG)
    ;"block movement between STARTROOM and THIRDROOM"
    <COND (<==? .ARG ,M-BEG>
           <COND (<AND <VERB? WALK>
                       ,PRSO-DIR
                       <OR <AND <==? ,HERE STARTROOM> <==? ,PRSO ,P?NORTH>>
                           <AND <==? ,HERE THIRDROOM> <==? ,PRSO ,P?SOUTH>>>>
                  <TELL "You can't go that way while in the vehicle." CR>)>)>>

<ROOM SECONDROOM
    (IN ROOMS)
    (DESC "Second Room")
    (FLAGS LIGHTBIT)
    (WEST TO STARTROOM)>

<ROOM THIRDROOM
    (IN ROOMS)
    (DESC "Third Room")
    (FLAGS LIGHTBIT)
    (SOUTH TO STARTROOM)>

<OBJECT GOLD-COIN
    (IN STARTROOM)
    (DESC "gold coin")
    (FLAGS TAKEBIT)
    (SYNONYM COIN)>

<OBJECT LAMP
    (DESC "lamp")
    (FLAGS LIGHTBIT TAKEBIT)
    (SYNONYM LAMP)>

<TEST-SETUP ()
    <REMOVE ,BOX>
    <REMOVE ,LAMP>
    <FSET ,BOX ,LOCKEDBIT>
    <FCLEAR ,BOX ,OPENABLEBIT>
    <FCLEAR ,BOX ,OPENBIT>
    <MOVE ,GOLD-COIN ,STARTROOM>
    <MOVE ,COUCH ,STARTROOM>
    <MOVE ,WAGON ,STARTROOM>
    <MOVE ,WINNER ,STARTROOM>>

<TEST-CASE ("Room description when in vehicle")
    <MOVE ,WINNER ,COUCH>
    <COMMAND [LOOK]>
    <EXPECT "Start Room, on the couch|Home sweet home.||There is a wagon and a gold coin here.|">
    <MOVE ,WINNER ,WAGON>
    <COMMAND [LOOK]>
    <EXPECT "Start Room, in the wagon|Home sweet home.||There is a couch and a gold coin here.|">>

<TEST-CASE ("Room description when not in vehicle")
    <COMMAND [LOOK]>
    <EXPECT "Start Room|Home sweet home.||There is a wagon, a couch, and a gold coin here.|">>

<TEST-CASE ("Get in and out of vehicle")
    <COMMAND [GET ON COUCH]>
    <EXPECT "You get onto the couch.|">
    <COMMAND [GET OUT]>
    <EXPECT "You get off of the couch.|">
    <COMMAND [ENTER WAGON]>
    <EXPECT "You get into the wagon.|">
    <COMMAND [EXIT]>
    <EXPECT "You get out of the wagon.|">>

<TEST-CASE ("Move while in vehicle")
    <MOVE ,WINNER ,COUCH>
    <COMMAND [EAST]>
    <EXPECT "Second Room, on the couch|">
    <COMMAND [WEST]>
    <EXPECT "Start Room, on the couch||There is a wagon and a gold coin here.|">
    <COMMAND [NORTH]>
    <EXPECT "You can't go that way while in the vehicle.|">>

<TEST-CASE ("Dropping items: SURFACEBIT => drops to room; otherwise goes into vehicle")
    ;"place an item in player's inventory"
    <MOVE ,GOLD-COIN ,WINNER>
    ;"wagon without SURFACEBIT: items should go into the wagon"
    <MOVE ,WINNER ,WAGON>
    <COMMAND [DROP COIN]>
    <EXPECT "You drop the gold coin.|">
    <COMMAND [LOOK]>
    <EXPECT "Start Room, in the wagon|Home sweet home.||There is a couch here.|In the wagon is a gold coin.|">
    ;"now try again with the couch"
    ;"couch with SURFACEBIT: dropping should leave coin in the room"
    <MOVE ,GOLD-COIN ,WINNER>
    <MOVE ,WINNER ,COUCH>
    <COMMAND [DROP COIN]>
    <EXPECT "You drop the gold coin.|">
    <COMMAND [LOOK]>
    <EXPECT "Start Room, on the couch|Home sweet home.||There is a gold coin and a wagon here.|">>

<TEST-CASE ("Can't pick up a vehicle while you're in it")
    <MOVE ,WINNER ,COUCH>
    <COMMAND [TAKE COUCH]>
    <EXPECT "You can't pick up the couch while you're on it.|">
    <MOVE ,WINNER ,WAGON>
    <COMMAND [TAKE WAGON]>
    <EXPECT "You can't pick up the wagon while you're in it.|">>

<TEST-CASE ("Can't get in a vehicle while you're holding it")
    <MOVE ,COUCH ,WINNER>
    <COMMAND [GET ON COUCH]>
    <EXPECT "You can't get on the couch while you're holding it.|">
    <MOVE ,WAGON ,WINNER>
    <COMMAND [GET ON WAGON]>
    <EXPECT "You can't get in the wagon while you're holding it.|">>

<TEST-CASE ("Can't see inside a closed opaque vehicle")
    <MOVE ,BOX ,STARTROOM>
    <MOVE ,WINNER ,BOX>
    <FCLEAR ,BOX ,OPENBIT>
    <SETG HERE ,STARTROOM>
    <SETG HERE-LIT <SEARCH-FOR-LIGHT>>
    <CHECK <NOT ,HERE-LIT>>
    <COMMAND [LOOK]>
    <EXPECT "It is pitch black. You can't see a thing.|">>

<TEST-CASE ("Closing an opaque vehicle around you leads to darkness")
    <MOVE ,BOX ,STARTROOM>
    <MOVE ,WINNER ,BOX>
    <FCLEAR ,BOX ,LOCKEDBIT>
    <FSET ,BOX ,OPENABLEBIT>
    <FSET ,BOX ,OPENBIT>
    <MOVE ,LAMP ,STARTROOM>
    <SETG HERE-LIT <SEARCH-FOR-LIGHT>>
    <COMMAND [CLOSE BOX]>
    <EXPECT "You close the box.|You are plunged into darkness.|">
    <CHECK <NOT <SEARCH-FOR-LIGHT>>>
    <COMMAND [LOOK]>
    <EXPECT "It is pitch black. You can't see a thing.|">>

<TEST-CASE ("An opaque vehicle can be opened from inside")
    <MOVE ,BOX ,STARTROOM>
    <MOVE ,WINNER ,BOX>
    <FCLEAR ,BOX ,LOCKEDBIT>
    <FSET ,BOX ,OPENABLEBIT>
    <FCLEAR ,BOX ,OPENBIT>
    <COMMAND [EXIT BOX]>
    <EXPECT "[opening the box]|You get out of the box.|You can see your surroundings now.||Start Room|Home sweet home.||There is a box, a wagon, a couch, and a gold coin here.|">
    <CHECK <SEARCH-FOR-LIGHT>>>

<TEST-CASE ("Can't enter a locked vehicle")
    <FSET ,BOX ,OPENABLEBIT>
    <FCLEAR ,BOX ,OPENBIT>
    <FSET ,BOX ,LOCKEDBIT>
    <MOVE ,BOX ,STARTROOM>
    <COMMAND [ENTER BOX]>
    <EXPECT "You'll have to open the box first.|">>

<TEST-CASE ("Can't enter a closed, unopenable vehicle")
    <FCLEAR ,BOX ,OPENABLEBIT>
    <FCLEAR ,BOX ,OPENBIT>
    <FCLEAR ,BOX ,LOCKEDBIT>
    <MOVE ,BOX ,STARTROOM>
    <COMMAND [ENTER BOX]>
    <EXPECT "You'll have to open the box first.|">>

<TEST-CASE ("Can't exit a locked vehicle")
    <FSET ,BOX ,OPENABLEBIT>
    <FCLEAR ,BOX ,OPENBIT>
    <FSET ,BOX ,LOCKEDBIT>
    <MOVE ,BOX ,STARTROOM>
    <MOVE ,WINNER ,BOX>
    <COMMAND [EXIT]>
    <EXPECT "You'll have to open the box first.|">>

<TEST-CASE ("Can't exit a closed, unopenable vehicle")
    <FCLEAR ,BOX ,OPENABLEBIT>
    <FCLEAR ,BOX ,OPENBIT>
    <FCLEAR ,BOX ,LOCKEDBIT>
    <MOVE ,BOX ,STARTROOM>
    <MOVE ,WINNER ,BOX>
    <COMMAND [EXIT]>
    <EXPECT "You'll have to open the box first.|">>

<TEST-CASE ("Implicitly open a closed vehicle when entering")
    <MOVE ,BOX ,STARTROOM>
    <FSET ,BOX ,OPENABLEBIT>
    <FCLEAR ,BOX ,LOCKEDBIT>
    <COMMAND [ENTER BOX]>
    <EXPECT "[opening the box]|You get into the box.|">
    <CHECK <FSET? ,BOX ,OPENBIT>>>

<TEST-CASE ("Implicitly open a closed vehicle when exiting")
    <MOVE ,BOX ,STARTROOM>
    <FSET ,BOX ,OPENABLEBIT>
    <FCLEAR ,BOX ,LOCKEDBIT>
    <MOVE ,WINNER ,BOX>
    <COMMAND [EXIT]>
    <EXPECT "[opening the box]|You get out of the box.|You can see your surroundings now.||Start Room|Home sweet home.||There is a box, a wagon, a couch, and a gold coin here.|">
    <CHECK <FSET? ,BOX ,OPENBIT>>>

<TEST-CASE ("Use a pronoun to refer to a closed vehicle while inside")
    <MOVE ,BOX ,STARTROOM>
    <FSET ,BOX ,OPENABLEBIT>
    <FCLEAR ,BOX ,LOCKEDBIT>
    <FSET ,BOX ,OPENBIT>
    <COMMAND [ENTER BOX]>
    <EXPECT "You get into the box.|">
    <COMMAND [CLOSE IT]>
    <EXPECT "You close the box.|You are plunged into darkness.|">
    <COMMAND [OPEN IT]>
    <EXPECT "You open the box.|You can see your surroundings now.||Start Room, in the box|Home sweet home.||There is a wagon, a couch, and a gold coin here.|">>

<TEST-GO ,STARTROOM>