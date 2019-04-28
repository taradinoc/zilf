"EMPTY GAME main file"

<VERSION ZIP>
<CONSTANT RELEASEID 1>

"Main loop"

<CONSTANT GAME-BANNER
"EMPTY GAME|
An interactive fiction by AUTHOR NAME">

<ROUTINE GO ()
    <CRLF> <CRLF>
    <TELL "INTRODUCTORY TEXT!" CR CR>
    <V-VERSION> <CRLF>
    <SETG HERE ,STARTROOM>
    <MOVE ,PLAYER ,HERE>
    <V-LOOK>
    <MAIN-LOOP>>

<INSERT-FILE "parser">

"Objects"

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "START ROOM")
    (FLAGS LIGHTBIT)>
