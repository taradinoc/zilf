<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (FLAGS LIGHTBIT)>

<OBJECT HIDEOUT
    (IN ROOMS)
    (DESC "Hideout")
    (FLAGS LIGHTBIT)>

<OBJECT SUSPECT
    (IN HIDEOUT)
    (DESC "suspect")
    (SYNONYM SUSPECT)
    (FLAGS PERSONBIT)>

<SYNTAX FOLLOW OBJECT (EVERYWHERE) = V-FOLLOW>

<ROUTINE V-FOLLOW ()
    <COND (<VISIBLE? ,PRSO>
           <TELL CT ,PRSO " is right here!" CR>)
          (ELSE
           <TELL CT ,PRSO " is nowhere in sight." CR>)>>

;<TEST-SETUP () >

<TEST-CASE ("FOLLOW SUSPECT")
    <COMMAND [FOLLOW SUSPECT]>
    <EXPECT "The suspect is nowhere in sight.|">>

<TEST-GO ,STARTROOM>
