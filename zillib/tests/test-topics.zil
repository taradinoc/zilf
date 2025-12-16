<VERSION ZIP>

<INSERT-FILE "testing">

<OBJECT STARTROOM
    (IN ROOMS)
    (DESC "Start Room")
    (FLAGS LIGHTBIT)>

<SYNTAX PONDER TOPIC = V-PONDER>
<SYNTAX RANT ABOUT TOPIC = V-RANT>

<ROUTINE V-PONDER ()
    <TELL "You soon tire of thinking about">
    <DO (I ,P-TOPIC-START ,P-TOPIC-END) <TELL " " WORD .I>>
    <TELL "." CR>>

<ROUTINE V-RANT ()
    <TELL "You've had it up to here with">
    <DO (I ,P-TOPIC-START ,P-TOPIC-END) <TELL " " WORD .I>>
    <TELL "!" CR>>

;<TEST-SETUP () >

<TEST-CASE ("Topic with preposition")
    <COMMAND [RANT ABOUT ELEPHANTS]>
    <EXPECT "You've had it up to here with elephants!|">>

<TEST-CASE ("Topic without preposition")
    <COMMAND [PONDER THE MEANING OF LIFE]>
    <EXPECT "You soon tire of thinking about the meaning of life.|">>

<TEST-GO ,STARTROOM>
