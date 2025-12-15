<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (LDESC "Ldesc.")
    (FLAGS LIGHTBIT)>

<OBJECT GUN
    (IN STARTROOM)
    (DESC "gun")
    (SYNONYM GUN)
    (FLAGS TAKEBIT)>

<OBJECT PAULINE
    (IN STARTROOM)
    (DESC "Pauline")
    (SYNONYM PAULINE)
    (FLAGS NARTICLEBIT PERSONBIT FEMALEBIT)>

<SYNTAX SHOW OBJECT (TAKE HAVE HELD CARRIED) TO OBJECT (FIND PERSONBIT) = V-SHOW>
<SYNTAX SHOW OBJECT (FIND PERSONBIT) OBJECT (TAKE HAVE HELD CARRIED) = V-SSHOW>

<ROUTINE V-SSHOW ()
    <PERFORM ,V?SHOW ,PRSI ,PRSO>>

<ROUTINE V-SHOW ()
    <TELL CT ,PRSI " doesn't appear to be impressed by " T ,PRSO "." CR>>

<TEST-SETUP ()
    <MOVE ,WINNER ,STARTROOM>
    <MOVE ,GUN ,STARTROOM>
    <MOVE ,PAULINE ,STARTROOM>>

<TEST-CASE ("SHOW GUN TO PAULINE")
    <COMMAND [SHOW GUN TO PAULINE]>
    <EXPECT "[taking the gun]|Pauline doesn't appear to be impressed by the gun.|">>

<TEST-CASE ("SHOW GUN")
    <COMMAND [SHOW GUN]>
    <EXPECT "[taking the gun]|[to Pauline]|Pauline doesn't appear to be impressed by the gun.|">>

<TEST-CASE ("SHOW PAULINE")
    <COMMAND [SHOW PAULINE]>
    <EXPECT "[the gun]|[taking the gun]|Pauline doesn't appear to be impressed by the gun.|">>

<TEST-GO ,STARTROOM>
