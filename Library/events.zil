"Events"

;"Queues an interrupt routine to run in some number of turns.

Uses:
  IQ-LENGTH

Sets:
  IQ-LENGTH
  IQUEUE (contents)

Args:
  IRTN: The interrupt routine to enqueue.
  TURNZ: The number of turns to count. 1 means the end of the current turn,
    2 means the end of the next turn, etc. If TURNZ is -1, the interrupt will
    run after every subsequent turn until dequeued."
<ROUTINE QUEUE (IRTN TURNZ)
    ;<PROG () <TELL "QUEING A ROUTINE. IQ-LENGTH IS CURRENTLY :"> <PRINTN ,IQ-LENGTH> <TELL CR>>
    <SETG IQ-LENGTH <+ ,IQ-LENGTH 2>>
    ;<PROG () <TELL "NOW IQ-LENGTH IS: "> <PRINTN ,IQ-LENGTH> <TELL CR>>
    <PUT ,IQUEUE <- ,IQ-LENGTH 1> .IRTN>
    <PUT ,IQUEUE ,IQ-LENGTH .TURNZ>>

;"Removes an interrupt routine from the event queue.

Interrupt routines should call this to dequeue themselves after they've fired.

Uses and sets:
  IQ-LENGTH
  IQUEUE (contents)

Args:
  IRTN: The interrupt routine."
<ROUTINE DEQUEUE (IRTN "AUX" S)
    ;<TELL "DEQUEUEING EVENT">
    <REPEAT ()
        <SET S <+ .S 2>>
        <COND (<G? .S ,IQ-LENGTH> <RETURN>)
              (<EQUAL? <GET ,IQUEUE <- .S 1>> .IRTN>
               <DEL-EVENT .S>
               <IQUEUE-CLEANUP>
               <RETURN>)>>>

;"Marks a slot in the interrupt queue as deleted. Internal use only.

Sets:
  IQUEUE (contents)

Args:
  IQPOS: An index into IQUEUE."
<ROUTINE DEL-EVENT (IQPOS)
    ;<TELL "DELETING EVENT" CR>
    ;<PUT ,IQUEUE .IQPOS "">
    <PUT ,IQUEUE .IQPOS -9>>

;"Compacts the interrupt queue, freeing up deleted slots. Internal use only.

Uses and sets:
  IQ-LENGTH
  IQUEUE"
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
                   ;<TELL "NOW Z IS " N .Z CR "IQ-LENGTH IS " N ,IQ-LENGTH CR>>)>>>

;"Determines whether an interrupt routine is queued to run at the end of the
current turn.

Uses:
  IQ-LENGTH
  IQUEUE

Args:
  E: The interrupt routine.

Returns:
  True if the interrupt will run at the end of the current turn, i.e. if it's
  queued with a remaining turn count of 1 or -1, otherwise false."
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
               <COND (<EQUAL? <GET ,IQUEUE .S> 1 -1>
                      ;<TELL "The turn indicator of S is 1 or -1 so returning true" CR>
                      <RTRUE>)>)>>>

"Clocker"

;"Advances the turn count, decrements interrupt turn counters, and runs
eligible interrupt routines.

Uses:
  TURNS
  IQ-LENGTH
  IQUEUE (contents)

Sets:
  TURNS
  IQUEUE (contents)

Returns:
  True if any interrupt routines were fired and returned true (indicating
  that something was printed), otherwise false."
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
               <RETURN>)>

        <COND (<EQUAL? <GET ,IQUEUE .S> -1>
               ;<PROG () <TELL "THE TURN COUNT IS " CR> <PRINTN <GET ,IQUEUE .S>> <TELL CR "SO WE ARE APPLYING AN EVERY-TURN FIRE" CR>>
               <SET FIRED <BOR .FIRED <APPLY <GET ,IQUEUE <- .S 1>>>>>)>

        <COND (<G? <GET ,IQUEUE .S> 0>
               ;<TELL "SUBTRACT 1 from event's TURN counter" CR>
               <PUT ,IQUEUE .S <- <GET ,IQUEUE .S> 1>>
               <COND (<EQUAL? <GET ,IQUEUE .S> 0>
                      ;<TELL "TURN COUNT IS 0, SO FIRE EVENT" CR>
                      <SET FIRED <BOR .FIRED <APPLY <GET ,IQUEUE <- .S 1>>>>>
                      <DEL-EVENT .S>
                      <SET C 1>)>)>>
    .FIRED>

;"Waits for a number of turns, exiting early if any interrupt routines printed
a message.

Args:
  TURNS: The number of turns to wait."
<ROUTINE WAIT-TURNS (TURNS "AUX" T INTERRUPT ENDACT BACKUP-WAIT)
    <SET BACKUP-WAIT ,STANDARD-WAIT>
    <SETG STANDARD-WAIT .TURNS>
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
                   <AND .INTERRUPT>>
               <SETG STANDARD-WAIT .BACKUP-WAIT>
               ;"To keep clocker from running again after the WAITED turns"
               <SETG AGAINCALL T>
               <RETURN>)>>>
