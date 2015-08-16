<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (LDESC "Ldesc.")
    ;(FLAGS LIGHTBIT)>

<OBJECT FIREFLY
    (IN STARTROOM)
    (DESC "firefly")
    (SYNONYM FIREFLY)
    (FLAGS TAKEBIT LIGHTBIT)>

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

<OBJECT SAFE
    (IN STARTROOM)
    (DESC "safe")
    (SYNONYM SAFE)
    (FLAGS CONTBIT OPENBIT OPENABLEBIT)>

<OBJECT FLASHLIGHT
    (IN PLAYER)
    (DESC "flashlight")
    (SYNONYM FLASHLIGHT)
    (ACTION FLASHLIGHT-R)
    (FLAGS TAKEBIT)>

<ROUTINE FLASHLIGHT-R ()
    <COND (<VERB? TURN-ON>
           <FSET ,FLASHLIGHT ,LIGHTBIT>
           <TELL "Turned on." CR>
           <NOW-LIT?>
           <RTRUE>)
          (<VERB? TURN-OFF>
           <FCLEAR ,FLASHLIGHT ,LIGHTBIT>
           <TELL "Turned off." CR>
           <NOW-DARK?>
           <RTRUE>)>>

<TEST-SETUP ()
    <MOVE ,WINNER ,STARTROOM>
    <MOVE ,FIREFLY ,STARTROOM>
    <MOVE ,CAGE ,STARTROOM>
    <MOVE ,DESK ,STARTROOM>
    <MOVE ,BUCKET ,STARTROOM>
    <MOVE ,SAFE ,STARTROOM>
    <FSET ,SAFE ,OPENBIT>
    <FCLEAR ,FLASHLIGHT ,LIGHTBIT>>

<TEST-CASE ("Initially lit")
    <CHECK <SEARCH-FOR-LIGHT>>>

<TEST-CASE ("Put firefly in safe")
    <MOVE ,FIREFLY ,WINNER>
    <COMMAND [PUT FIREFLY IN SAFE]>
    <CHECK <SEARCH-FOR-LIGHT>>
    <COMMAND [CLOSE SAFE]>
    <EXPECT "You close the safe.|You are plunged into darkness.|">
    <CHECK <NOT <SEARCH-FOR-LIGHT>>>>

<TEST-CASE ("Put firefly in bucket")
    <MOVE ,FIREFLY ,WINNER>
    <COMMAND [PUT FIREFLY IN BUCKET]>
    <CHECK <SEARCH-FOR-LIGHT>>>

<TEST-CASE ("Put firefly on desk")
    <MOVE ,FIREFLY ,WINNER>
    <COMMAND [PUT FIREFLY ON DESK]>
    <CHECK <SEARCH-FOR-LIGHT>>>

<TEST-CASE ("Remove firefly")
    <REMOVE ,FIREFLY>
    <CHECK <NOT <SEARCH-FOR-LIGHT>>>>

<TEST-CASE ("Turn on flashlight")
    <REMOVE ,FIREFLY>
    <COMMAND [TURN ON FLASHLIGHT]>
    <EXPECT "Turned on.|You can see your surroundings now.||
Start Room|Ldesc.||
There is a safe, a bucket, a desk, and a cage here.|">
    <CHECK <SEARCH-FOR-LIGHT>>
    <COMMAND [TURN OFF FLASHLIGHT]>
    <EXPECT "Turned off.|You are plunged into darkness.|">
    <CHECK <NOT <SEARCH-FOR-LIGHT>>>>

<TEST-GO ,STARTROOM>
