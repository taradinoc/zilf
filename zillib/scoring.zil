"Scoring system"

<USE "HOOKS">

;"The scoring system provides a basic SCORE command, score notifications that
  the player can toggle with NOTIFY ON/OFF, and more.

  To use it, enable USE-SCORING? and define MAX-SCORE:

     <SETG USE-SCORING? T>
     <CONSTANT MAX-SCORE 120>

  To award the player points, use <AWARD-POINTS num>, where num can be positive
  or negative. Or just change SCORE with SETG.

  You can classify the points, first by defining a set of achievements:

     <SCORING-ACHIEVEMENTS
         (PIE-EATING \"winning a pie-eating contest\")
         (SHEEP \"finding lost sheep\" 'REPEATABLE)>

  And then by passing the corresponding ACH constant when awarding points:

     <AWARD-POINTS 10 ,ACH?PIE-EATING>

  If the achievement isn't defined with 'REPEATABLE, points will only be awarded
  the first time. The player can see a breakdown with FULLSCORE (or FULL).

  You can also give the player a rating based on their score:

     <REPLACE-DEFINITION PRINT-RANK
         <ROUTINE PRINT-RANK (DEAD?)
             <COND (.DEAD? <TELL \"Historians \">)
                   (ELSE <TELL \"At this rate, historians will surely \">)>
             <COND (<G=? ,SCORE 100> <TELL \"venerate you as a Legend.\" CR>)
                   (<G=? ,SCORE 50> <TELL \"remember you as a Has-Been.\" CR>)
                   (ELSE <TELL \"dismiss you as a Never-Was.\" CR>)>>
  "

<GLOBAL PREV-SCORE 0>
<GLOBAL NOTIFY-SCORE? T>

<SETG EXTRA-GAME-VERBS (SCORE FULLSCORE NOTIFY-ON NOTIFY-OFF !,EXTRA-GAME-VERBS)>

<DEFAULT-DEFINITION PRINT-RANK
    <DEFMAC PRINT-RANK ('DEAD?) <>>>

<SYNTAX SCORE = V-SCORE>

<ROUTINE V-SCORE ("OPT" DEAD?)
    <TELL <LIBRARY-MESSAGE SCORE DEFAULT ((MOVES ,MOVES) (POINTS ,SCORE) (DEAD? .DEAD?) (MAX ,MAX-SCORE))> CR>
    <PRINT-RANK .DEAD?>>

<SYNTAX FULL SCORE OBJECT (FIND KLUDGEBIT) = V-FULLSCORE>
<SYNTAX FULL = V-FULLSCORE>
<SYNTAX FULLSCORE = V-FULLSCORE>

<ROUTINE V-FULLSCORE ("OPT" DEAD? "AUX" MAX ANY?)
    <V-SCORE .DEAD?>
    <SET MAX <* ,ACHIEVEMENT-COUNT ,ACHTBL-ENTRY-LENGTH>>
    <REPEAT ((I 2))
        <COND (<G=? .I .MAX> <RETURN>)
              (<GET ,ACHIEVEMENTS .I> <SET ANY? T> <RETURN>)
              (ELSE <SET I <+ .I ,ACHTBL-ENTRY-LENGTH>>)>>
    <COND (.ANY?
           <TELL <LIBRARY-MESSAGE FULLSCORE HEADER> CR>
           <REPEAT ((I 0) P)
               <COND (<G=? .I .MAX> <RETURN>)>
               <COND (<SET P <GET ,ACHIEVEMENTS <+ .I 2>>>
                      <PRINTN-RIGHT-ALIGNED .P>
                      <TELL " " <GET ,ACHIEVEMENTS .I> CR>)>
               <SET I <+ .I ,ACHTBL-ENTRY-LENGTH>>>)>>

;"Prints a number right-aligned in a five-character field.

Returns:
  T."
<ROUTINE PRINTN-RIGHT-ALIGNED (N)
    <COND (<G=? .N 1000> <TELL " ">)
          (<G=? .N 100> <TELL "  ">)
          (<G=? .N 10> <TELL "   ">)
          (<G=? .N 0> <TELL "    ">)
          (<L=? .N -1000>)
          (<L=? .N -100> <TELL " ">)
          (<L=? .N -10> <TELL "  ">)
          (ELSE        <TELL "   ">)>
    <TELL N .N>
    <RTRUE>>

<SYNTAX NOTIFY ON OBJECT (FIND KLUDGEBIT) = V-NOTIFY-ON>
<SYNTAX NOTIFY = V-NOTIFY-ON>

<ROUTINE V-NOTIFY-ON ()
    <SETG NOTIFY-SCORE? T>
    <TELL <LIBRARY-MESSAGE NOTIFY-ON SUCCESS> CR>>

<SYNTAX NOTIFY OFF OBJECT (FIND KLUDGEBIT) = V-NOTIFY-OFF>

<ROUTINE V-NOTIFY-OFF ()
    <SETG NOTIFY-SCORE? <>>
    <TELL <LIBRARY-MESSAGE NOTIFY-OFF SUCCESS> CR>>

;"Prints a score-change notification when notifications are enabled.

Returns:
  True if the score changed, otherwise false."
<ROUTINE NOTIFY-SCORE ("AUX" D DOWN?)
    <COND (<NOT ,NOTIFY-SCORE?> <RFALSE>)>
    <SET D <- ,SCORE ,PREV-SCORE>>
    <COND (.D
           <COND (<L? .D 0> <SET D <- .D>> <SET DOWN? T>)>
           <TELL CR <LIBRARY-MESSAGE SCORE NOTIFICATION ((POINTS .D) (DOWN? .DOWN?))> CR>)>
    <SETG PREV-SCORE ,SCORE>
    <T? .D>>

<DEFSTRUCT ACHIEVEMENT VECTOR
    (ACH-NAME ATOM)
    (ACH-INDEX FIX)
    (ACH-DESC STRING)
    (ACH-REPEATABLE? <OR ATOM FALSE>)>

<SETG ACHIEVEMENT-DEFINITIONS ()>

<DEFINE SCORING-ACHIEVEMENTS ("ARGS" ITEMS "AUX" ACH)
    #DECL ((ITEMS) <LIST <LIST ATOM STRING [REST ''REPEATABLE]>>)
    <MAPF <>
          <FUNCTION (I "AUX" A IDX)
              <SET IDX <LENGTH ,ACHIEVEMENT-DEFINITIONS>>
              <SET A <MAKE-ACHIEVEMENT 'ACH-NAME <1 .I>
                                       'ACH-INDEX .IDX
                                       'ACH-DESC <2 .I>>>
              <SETG ACHIEVEMENT-DEFINITIONS (.A !,ACHIEVEMENT-DEFINITIONS)>
              <EVAL <FORM CONSTANT <PARSE <STRING "ACH?" <SPNAME <1 .I>>>> .IDX>>
              <SET I <REST .I 2>>
              <REPEAT (F)
                  <COND (<EMPTY? .I> <RETURN>)
                        (<=? <SET F <1 .I>> ''REPEATABLE> <ACH-REPEATABLE? .A T>)
                        (ELSE <ERROR UNRECOGNIZED-ACHIEVEMENT-FLAG .F>)>
                  <SET I <REST .I>>>>
          .ITEMS>>

<CONSTANT ACHTBL-F-REPEATABLE 1>
<CONSTANT ACHTBL-F-GRANTED 2>

<CONSTANT ACHTBL-ENTRY-LENGTH 3>

<DEFINE DEFINE-ACHIEVEMENTS-TABLE ("AUX" DATA DEFS)
    <SET DEFS <VECTOR !,ACHIEVEMENT-DEFINITIONS>>
    <SORT <FUNCTION (A B) <G? <ACH-INDEX .A> <ACH-INDEX .B>>> .DEFS>
    <SET DATA <MAPF ,LIST
                    <FUNCTION (A "AUX" F)
                        <SET F <COND (<ACH-REPEATABLE? .A> ,ACHTBL-F-REPEATABLE) (ELSE 0)>>
                        <MAPRET <ACH-DESC .A>       ;"achievement description"
                                .F                  ;"achievement flags"
                                0                   ;"total points granted">>
                    .DEFS>>
    <EVAL <FORM CONSTANT ACHIEVEMENTS <FORM TABLE !.DATA>>>
    <EVAL <FORM CONSTANT ACHIEVEMENT-COUNT <LENGTH ,ACHIEVEMENT-DEFINITIONS>>>>

<ADD-FINISHER
    <FUNCTION ()
        <DEFINE-ACHIEVEMENTS-TABLE>
        <COND (<NOT <GETPROP MAX-SCORE ZVAL>>
               <ERROR MAX-SCORE-NOT-DEFINED>)>>>

;"Adds points to the player's score, optionally recording an achievement.

Args:
  PTS: The number of points to add. May be negative.
  ACH: The achievement index, or -1 to award unclassified points.

Returns:
  True if the points were awarded. False if a non-repeatable achievement had
  already been awarded."
<ROUTINE AWARD-POINTS (PTS "OPT" (ACH -1) "AUX" P F)
    <COND (<G=? .ACH 0>
           <SET P <+ ,ACHIEVEMENTS <* <* ,ACHTBL-ENTRY-LENGTH ,WORD-SIZE> .ACH>>>
           <COND (<AND <NOT <BTST <SET F <GET .P 1>> ,ACHTBL-F-REPEATABLE>>
                       <BTST .F ,ACHTBL-F-GRANTED>>
                  <RFALSE>)>
           <PUT .P 1 <BOR .F ,ACHTBL-F-GRANTED>>
           <PUT .P 2 <+ <GET .P 2> .PTS>>)>
    <SETG SCORE <+ ,SCORE .PTS>>
    <RTRUE>>
