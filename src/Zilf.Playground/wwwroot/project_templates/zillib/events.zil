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
    <SETG IQ-LENGTH <+ ,IQ-LENGTH 2>>
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
    <PUT ,IQUEUE .IQPOS -9>>

;"Compacts the interrupt queue, freeing up deleted slots. Internal use only.

Uses and sets:
  IQ-LENGTH
  IQUEUE"
<ROUTINE IQUEUE-CLEANUP ("AUX" S Z)
    <REPEAT ()
        <SET S <+ .S 2>>
        <COND (<G? .S ,IQ-LENGTH> <RETURN>)
              (<EQUAL? <GET ,IQUEUE .S> -9>
               <SET Z .S>
               <REPEAT ()
                   <PUT ,IQUEUE <- .Z 1> <GET ,IQUEUE <+ .Z 1>>>
                   <PUT ,IQUEUE .Z  <GET ,IQUEUE <+ .Z 2>>>
                   <COND (<=? .Z ,IQ-LENGTH>
                          <PUT ,IQUEUE <- .Z 1> 0>
                          <PUT ,IQUEUE .Z 0>
                          <SETG IQ-LENGTH <- ,IQ-LENGTH 2>>
                          <SET S <- .S 2>>
                          <RETURN>)>
                   <SET Z <+ .Z 2>>>)>>>

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
    <REPEAT ()
        <SET S <+ .S 2>>
        <COND (<G? .S ,IQ-LENGTH>
               <RFALSE>)
              (<==? <GET ,IQUEUE <- .S 1>> .E>
               <COND (<EQUAL? <GET ,IQUEUE .S> 1 -1>
                      <RTRUE>)>)>>>

"Clocker"

;"Advances the turn count, decrements interrupt turn counters, and runs
eligible interrupt routines.

Uses:
  MOVES
  IQ-LENGTH
  IQUEUE (contents)

Sets:
  MOVES
  IQUEUE (contents)

Returns:
  True if any interrupt routines were fired and returned true (indicating
  that something was printed), otherwise false."
<ROUTINE CLOCKER ("AUX" S C FIRED)
    <SETG MOVES <+ ,MOVES 1>>
    <REPEAT ()
        <SET S <+ .S 2>>
        <COND (<G? .S ,IQ-LENGTH>
               <COND (<EQUAL? .C 1> <IQUEUE-CLEANUP>)>
               <RETURN>)>

        <COND (<EQUAL? <GET ,IQUEUE .S> -1>
               <SET FIRED <BOR .FIRED <APPLY <GET ,IQUEUE <- .S 1>>>>>)>

        <COND (<G? <GET ,IQUEUE .S> 0>
               <PUT ,IQUEUE .S <- <GET ,IQUEUE .S> 1>>
               <COND (<EQUAL? <GET ,IQUEUE .S> 0>
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
    <REPEAT ()
        <SET ENDACT <APPLY <GETP ,HERE ,P?ACTION> ,M-END>>
        <SET INTERRUPT <CLOCKER>>
        <SET T <+ .T 1>>
        <COND (<OR <G? .T ,STANDARD-WAIT>
                   .ENDACT
                   .INTERRUPT>
               <SETG STANDARD-WAIT .BACKUP-WAIT>
               <RETURN>)>>>
