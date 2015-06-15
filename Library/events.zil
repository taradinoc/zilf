"Events"

<ROUTINE QUEUE (IRTN TURNZ)
    ;<PROG () <TELL "QUEING A ROUTINE. IQ-LENGTH IS CURRENTLY :"> <PRINTN ,IQ-LENGTH> <TELL CR>>
    <SETG IQ-LENGTH <+ ,IQ-LENGTH 2>>
    ;<PROG () <TELL "NOW IQ-LENGTH IS: "> <PRINTN ,IQ-LENGTH> <TELL CR>>
    <PUT ,IQUEUE <- ,IQ-LENGTH 1> .IRTN>
    <PUT ,IQUEUE ,IQ-LENGTH .TURNZ>
    >
    
<ROUTINE DEQUEUE (IRTN "AUX" S)
    ;<TELL "DEQUEUEING EVENT">
    <REPEAT ()
        <SET S <+ .S 2>>
        <COND (<G? .S ,IQ-LENGTH> <RETURN>)
              (<EQUAL? <GET ,IQUEUE <- .S 1>> .IRTN>
                    <DEL-EVENT .S>
                    <IQUEUE-CLEANUP>
                    <RETURN>)>
    > 
>

<ROUTINE DEL-EVENT (IQPOS)
    ;<TELL "DELETING EVENT" CR>
    ;<PUT ,IQUEUE .IQPOS "">
    <PUT ,IQUEUE .IQPOS -9>		
>

<ROUTINE IQUEUE-CLEANUP ("AUX" S Z)
    ;<TELL "CLEANING UP IQUEUE" CR>
    <REPEAT ()
        <SET S <+ .S 2>>
        ;<PROG () <TELL "CLEANUP S IS "> <PRINTN .S> <TELL CR>>
        <COND (<G? .S ,IQ-LENGTH> <RETURN>)
              (<EQUAL? <GET ,IQUEUE .S> -9>
                    ;<TELL "SHIFTING ELEMENTS OVER" CR>
                    <SET Z .S>
                        <REPEAT ()
                            <PUT ,IQUEUE <- .Z 1> <GET ,IQUEUE <+ .Z 1>>>
                            <PUT ,IQUEUE .Z  <GET ,IQUEUE <+ .Z 2>>>
                            ;<PROG () <TELL "DID SHIFT of the PAIR ending at "> <PRINTN .Z> <TELL CR>>
                            <COND (<=? .Z ,IQ-LENGTH> 
                                    <PUT ,IQUEUE <- .Z 1> 0>
                                    <PUT ,IQUEUE .Z 0>
                                    <SETG IQ-LENGTH <- ,IQ-LENGTH 2>>
                                    <SET S <- .S 2>>
                                    <RETURN>)>
                            <SET Z <+ .Z 2>>
                            ;<PROG () <TELL "NOW Z IS "> <PRINTN .Z> <TELL CR>
                            <TELL "IQ-LENGTH IS "> <PRINTN ,IQ-LENGTH> <TELL CR>>
                        >
               )>
    >
>


<ROUTINE RUNNING? (E "AUX" S)
    ;<TELL "In the RUNNING? routine" CR>
        <REPEAT ()
            <SET S <+ .S 2>>
            ;<PROG () <TELL "S IS :"> <PRINTN .S> <TELL CR>>
            <COND (<G? .S ,IQ-LENGTH> 
                        ;<TELL "And S is greater than IQ-LENGTH so returning false" CR>
                        <RFALSE>)
                  (<==? <GET ,IQUEUE <- .S 1>> .E>
                        ;<PROG () <TELL "And S is equal to E" CR>>
                        <COND
                            (<OR <EQUAL? <GET ,IQUEUE .S>  1> <EQUAL? <GET ,IQUEUE .S>  -1>
                             >
                                ;<TELL "The turn indicator of S is 1 or -1 so returning true" CR>
                                <RTRUE>
                            )
                        >
                  )
            >
       
        >
>

"Clocker"

<ROUTINE CLOCKER ("AUX" S C FIRED)
    <SETG TURNS <+ ,TURNS 1>>
    ;<PROG () <TELL "TURN :"> <PRINTN ,TURNS> <TELL CR>>
    ;<PROG () <TELL "IQ-LENGTH is :"> <PRINTN ,IQ-LENGTH> <TELL CR>>
    <REPEAT ()
        <SET S <+ .S 2>>
        ;<PROG () <TELL "S is :"> <PRINTN .S> <TELL CR>
        ;<TELL "IQ-LENGTH is :"> <PRINTN ,IQ-LENGTH> <TELL CR>>
        <COND (<G? .S ,IQ-LENGTH> 
                    ;<PROG () <TELL "GREATER THAN IQ-LENGTH" CR> <TELL "SEE IF NEED TO DO CLEANUP --" CR>>
                    <COND (<EQUAL? .C 1> <IQUEUE-CLEANUP>)>
                    <RETURN>
                )>
                    
        <COND (<EQUAL? <GET ,IQUEUE .S> -1>
                    ;<PROG () <TELL "THE TURN COUNT IS " CR> <PRINTN <GET ,IQUEUE .S>> <TELL CR "SO WE ARE APPLYING AN EVERY-TURN FIRE" CR>>
                    <SET FIRED <APPLY <GET ,IQUEUE <- .S 1>>>>
                )>
                    
        <COND (<G? <GET ,IQUEUE .S> 0>
                    ;<TELL "SUBTRACT 1 from event's TURN counter" CR>
                    <PUT ,IQUEUE .S <- <GET ,IQUEUE .S> 1>>
                        <COND (<EQUAL? <GET ,IQUEUE .S> 0>
                                ;<TELL "TURN COUNT IS 0, SO FIRE EVENT" CR>
                                <SET FIRED <APPLY <GET ,IQUEUE <- .S 1>>>>
                                <DEL-EVENT .S>
                                <SET C 1>
                               )>
              )
        >
                                
     >
     <COND 
           (<AND .FIRED> <RTRUE>)
           (ELSE <RFALSE>)
     >     
>

<ROUTINE WAIT-TURNS (TURNS "AUX" T INTERRUPT ENDACT BACKUP-WAIT)
    <SET BACKUP-WAIT ,STANDARD-WAIT>
    <SET STANDARD-WAIT .TURNS>
    <SET T 1>
    ;<TELL "Time passes." CR>
    <REPEAT ()
        ;<TELL "THE WAIT TURN IS " N .T CR>
        <SET ENDACT <APPLY <GETP ,HERE ,P?ACTION> ,M-END>>
        ;<TELL "ENDACT IS NOW " D .ENDACT CR>
        <SET INTERRUPT <CLOCKER>>
        ;<TELL "INTERRUPT IS NOW " D .INTERRUPT CR>
        <SET T <+ .T 1>>
        <COND (<OR <G? .T ,STANDARD-WAIT>
                   <AND .ENDACT>
                   <AND .INTERRUPT>
               >
                        <SET STANDARD-WAIT .BACKUP-WAIT>
                        ;"To keep clocker from running again after the WAITED turns"
                        <SET AGAINCALL 1>
                        <RETURN>
              )
        >
    >>
