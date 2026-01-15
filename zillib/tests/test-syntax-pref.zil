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

<OBJECT BUCKET
    (DESC "bucket")
    (SYNONYM BUCKET)
    (FLAGS CONTBIT OPENBIT TAKEBIT)>

<OBJECT TABLE
    (DESC "table")
    (SYNONYM TABLE)
    (FLAGS CONTBIT SURFACEBIT OPENBIT)>

<SYNTAX SHOW OBJECT (TAKE HAVE HELD CARRIED) TO OBJECT (FIND PERSONBIT) = V-SHOW>
<SYNTAX SHOW OBJECT (FIND PERSONBIT) OBJECT (TAKE HAVE HELD CARRIED) = V-SSHOW>

<ROUTINE V-SSHOW ()
    <PERFORM ,V?SHOW ,PRSI ,PRSO>>

<ROUTINE V-SHOW ()
    <TELL CT ,PRSI " doesn't appear to be impressed by " T ,PRSO "." CR>>

<TEST-SETUP ()
    <MOVE ,WINNER ,STARTROOM>
    <MOVE ,GUN ,STARTROOM>
    <MOVE ,PAULINE ,STARTROOM>
    <REMOVE ,TABLE>
    <REMOVE ,BUCKET>>

<TEST-CASE ("SHOW GUN TO PAULINE")
    <COMMAND [SHOW GUN TO PAULINE]>
    <EXPECT "[taking the gun]|Pauline doesn't appear to be impressed by the gun.|">>

<TEST-CASE ("SHOW GUN")
    <COMMAND [SHOW GUN]>
    <EXPECT "[taking the gun]|[to Pauline]|Pauline doesn't appear to be impressed by the gun.|">>

<TEST-CASE ("SHOW PAULINE")
    <COMMAND [SHOW PAULINE]>
    <EXPECT "[the gun]|[taking the gun]|Pauline doesn't appear to be impressed by the gun.|">>

<TEST-CASE ("PUT GUN with container and surface available")
    <MOVE ,GUN ,WINNER>
    <MOVE ,TABLE ,STARTROOM>
    <MOVE ,BUCKET ,STARTROOM>
    <COMMAND [PUT GUN]>
    <EXPECT "[on the table]|You put the gun on the table.|">>

<TEST-CASE ("PUT GUN with only container available")
    <MOVE ,GUN ,WINNER>
    <MOVE ,BUCKET ,STARTROOM>
    <COMMAND [PUT GUN]>
    <EXPECT "[in the bucket]|You put the gun in the bucket.|">>

<TEST-CASE ("PUT GUN with only surface available")
    <MOVE ,GUN ,WINNER>
    <MOVE ,TABLE ,STARTROOM>
    <COMMAND [PUT GUN]>
    <EXPECT "[on the table]|You put the gun on the table.|">>

<TEST-CASE ("PUT GUN with neither available")
    <MOVE ,GUN ,WINNER>
    <COMMAND [PUT GUN]>
    <EXPECT "What do you want to put the gun on?|">>

<TEST-GO ,STARTROOM>
