"Name and age sample for ZILF"

<VERSION ZIP>
<CONSTANT RELEASEID 1>

"We need an object for the V3 status line."

<ROOM NOWHERE
    (DESC "Nowhere")>

<GLOBAL HERE NOWHERE>
<GLOBAL SCORE 500>
<GLOBAL TURNS 1>

<CONSTANT READBUF-SIZE 100>
<GLOBAL READBUF <ITABLE NONE 100>>
<GLOBAL LEXBUF <ITABLE 59 (LEXV) 0 #BYTE 0 #BYTE 0>>

<CONSTANT NAMEBUF-SIZE 20>
<GLOBAL NAMEBUF <ITABLE NONE 20>>

<GLOBAL BIRTHYEAR 0>
<GLOBAL CURYEAR 0>

<DEFMAC TELL ("ARGS" A "AUX" O)
    <SET O <MAPF ,LIST
        <FUNCTION ("AUX" I)
            <COND (<EMPTY? .A> <MAPSTOP>)>
            <SET I <NTH .A 1>>
            <SET A <REST .A>>
            <COND
                (<TYPE? .I STRING>
                    <COND
                        (<LENGTH? .I 0>
                            <MAPRET>)
                        (<LENGTH? .I 1>
                            <FORM PRINTC <NTH .I 1>>)
                        (ELSE
                            <FORM PRINTI .I>)>)
                (<==? .I CR>
                    <FORM CRLF>)
                (<==? .I N>
                    <SET I <NTH .A 1>>
                    <SET A <REST .A>>
                    <FORM PRINTN .I>)
                (<==? .I BUF>
                    <SET I <NTH .A 1>>
                    <SET A <REST .A>>
                    <FORM PRINTBUF .I>)
                (ELSE
                    <FORM PRINT .I>)>>>>
    <COND
        (<LENGTH? .O 0>
            <>)
        (<LENGTH? .O 1>
            <NTH .O 1>)
        (ELSE
            <FORM PROG '() !.O>)>>

<ROUTINE GO ()
    <TELL "Hi! What is your name?" CR>
    <READLINE>
    <COPYBUF ,READBUF ,NAMEBUF ,NAMEBUF-SIZE>
    <TELL "What year were you born in?" CR>
    <SETG BIRTHYEAR <READNUM>>
    <TELL "And what year is it now?" CR>
    <SETG CURYEAR <READNUM>>
    <TELL "Nice to meet you, " BUF ,NAMEBUF "! You must be about "
        N <- ,CURYEAR ,BIRTHYEAR> " by now!" CR>>

<ROUTINE READLINE ()
    <PRINTI "> ">
    <PUTB ,READBUF 0 ,READBUF-SIZE>
    <READ ,READBUF ,LEXBUF>
    <COND (<==? <GETB ,READBUF 0> 0>
            <TELL "I beg your pardon?" CR>
            <AGAIN>)>
    <REPEAT ((I 1))
        <COND (<OR <==? .I ,READBUF-SIZE> <ZERO? <GETB ,READBUF .I>>>
                <PUTB ,READBUF 0 <- .I 1>>
                <RETURN>)>
        <SET I <+ .I 1>>>
    <RTRUE>>

<ROUTINE COPYBUF (SRC DEST MAX)
    <REPEAT ((I 0) (LEN <+ <GETB .SRC 0> 1>))
        <PUTB .DEST .I <GETB .SRC .I>>
        <SET I <+ .I 1>>
        <COND (<EQUAL? .I .MAX .LEN> <RETURN>)>>>

<ROUTINE PRINTBUF (BUF "AUX" (I 1) (LEN <GETB .BUF 0>))
    <REPEAT ()
        <PRINTC <GETB .BUF .I>>
        <COND (<L? .I .LEN> <SET I <+ .I 1>>)
            (ELSE <RETURN>)>>>

<ROUTINE READNUM ("AUX" LEN (OK <>) VAL)
    <READLINE>
    <SET LEN <GETB ,READBUF 0>>
    <SET VAL 0>
    <REPEAT ((I 0) CH)
        <COND (<IGRTR? I .LEN>
                <RETURN>)>
        <SET CH <GETB ,READBUF .I>>
        <COND (<OR <L? .CH !\0> <G? .CH !\9>>
                <SET OK <>>
                <RETURN>)>
        <SET OK T>
        <SET VAL <+ <* .VAL 10> <- .CH !\0>>>>
    <COND (<NOT .OK>
            <TELL "That isn't a number!" CR>
            <AGAIN>)>
    .VAL>