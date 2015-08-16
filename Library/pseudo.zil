"Pseudo-objects"

"This provides a way to populate games with scenery without creating separate objects for each noun
 mentioned in a room description.
 
 When the parser can't find a match for an OBJSPEC, it checks the location's THINGS property, which
 (if present) defines a set of pseudo-objects, each with a list of adjectives, a list of nouns, and
 an action routine. If one of them matches, the singleton PSEUDO-OBJECT is returned, after setting
 its ACTION property to the routine and setting the global PSEUDO-LOC to the location.
 
 The format of the THINGS property is:
 
     .PROP 2,P?THINGS
     .WORD T?THINGS-TABLE
     
   T?THINGS-TABLE::
     .WORD 1                          ; Number of pseudo-objects
     .BYTE 1                          ; Number of adjectives
     .BYTE 2                          ; Number of nouns
     .WORD A?GINGERBREAD              ; Adjective (stored directly since # adjectives = 1,
                                      ; otherwise this would point to a byte/word table)
     .WORD T?GINGERBREAD-HOUSE-NOUNS  ; Noun table (since # adjectives > 1)
     .WORD GINGERBREAD-HOUSE-F        ; Action routine
   
   T?GINGERBREAD-HOUSE-NOUNS::
     .WORD W?HOUSE
     .WORD W?MANSION
   
 The property definition syntax is implemented by THINGS-PROPSPEC below."

"Constants and macros to access pseudo entries"
<CONSTANT PDO-SIZE 8>

<DEFMAC PDO-NADJ ('PDO)
    <FORM GETB .PDO 0>>

<DEFMAC PDO-NNOUN ('PDO)
    <FORM GETB .PDO 1>>

<DEFMAC PDO-ADJ/TBL ('PDO)
    <FORM GET .PDO 1>>

<DEFMAC PDO-NOUN/TBL ('PDO)
    <FORM GET .PDO 2>>

<DEFMAC PDO-ACTION ('PDO)
    <FORM GET .PDO 3>>

;"Like REFERS? but for pseudo entries.

Args:
  SPEC: An OBJSPEC.
  PDO: A pseudo entry.

Returns:
  A quality score from 0 to 3 indicating how well the OBJSPEC matches the pseudo."
<ROUTINE REFERS-PSEUDO? (SPEC PDO "AUX" NA NN AT NT
                         (A <OBJSPEC-ADJ .SPEC>) (N <OBJSPEC-NOUN .SPEC>))
    <SET NA <PDO-NADJ .PDO>>
    <SET NN <PDO-NNOUN .PDO>>
    <SET AT <PDO-ADJ/TBL .PDO>>
    <SET NT <PDO-NOUN/TBL .PDO>>
    <COND (<AND .A .N>
           <COND (<AND <PDO-ADJ-REFERS?> <PDO-NOUN-REFERS?>>
                  <RETURN 3>)>)
          (.N
           <COND (<PDO-NOUN-REFERS?> <RETURN 2>)
                 (<VERSION?
                      (ZIP <SET A <CHKWORD? .N ,PS?ADJECTIVE ,P1?ADJECTIVE>>)
                      (ELSE <AND <CHKWORD? .N ,PS?ADJECTIVE> <SET A .N>>)>
                  <COND (<PDO-ADJ-REFERS?> <RETURN 1>)>)>)
          (.A
           <COND (<PDO-ADJ-REFERS?> <RETURN 1>)>)>
    <RETURN 0>>

<DEFMAC PDO-ADJ-REFERS? ()
    '<OR <AND <1? .NA> <=? .A .AT>>
         <AND <NOT <1? .NA>> <IN-B/WTBL? .AT .NA .A>>>>

<DEFMAC PDO-NOUN-REFERS? ()
    '<OR <AND <1? .NN> <=? .N .NT>>
         <AND <NOT <1? .NN>> <IN-WTBL? .NT .NN .N>>>>

;"Tries to match an OBJSPEC against any of the pseudo entries in a THINGS property.

Args:
  SPEC: The OBJSPEC.
  PT: The property table for the THINGS property.

Returns:
  The address of the pseudo entry, or false if none matched."
<ROUTINE MATCH-PSEUDO MP (SPEC PT "AUX" CNT)
    <SET CNT <GET .PT 0>>
    <SET PT <REST .PT 2>>
    <REPEAT ()
        <COND (<DLESS? CNT 0> <RFALSE>)
              (<REFERS-PSEUDO? .SPEC .PT> <RETURN .PT>)>
        <SET PT <REST .PT ,PDO-SIZE>>>>

;"Initializes PSEUDO-OBJECT from a pseudo entry and returns it."
<ROUTINE MAKE-PSEUDO (PDO)
    <PUTP ,PSEUDO-OBJECT ,P?ACTION <PDO-ACTION .PDO>>
    <SETG PSEUDO-LOC ,HERE>
    ,PSEUDO-OBJECT>

<IF-DEBUGGING-VERBS
    <ROUTINE PRINT-PSEUDOS (PDO "AUX" MAX NA NN AT NT)
        <SET MAX <GET .PDO 0>>
        <SET PDO <REST .PDO 2>>
        <REPEAT ()
            <COND (<DLESS? MAX 0> <RETURN>)>
            <SET NA <PDO-NADJ .PDO>>
            <SET NN <PDO-NNOUN .PDO>>
            <SET AT <PDO-ADJ/TBL .PDO>>
            <SET NT <PDO-NOUN/TBL .PDO>>
            <TELL CR "  (">
            <COND (<1? .NA>
                   <PRINT-ADJ .AT>)
                  (.NA
                   <SET NA <- .NA 1>>
                   <DO (I 0 .NA)
                       <COND (.I <TELL ", ">)>
                       <PRINT-ADJ <GET/B .AT .I>>>)>
            <TELL ") (">
            <COND (<1? .NN>
                   <PRINTB .NT>)
                  (.NN
                   <SET NN <- .NN 1>>
                   <DO (I 0 .NN)
                       <COND (.I <TELL ", ">)>
                       <PRINTB <GET .NT .I>>>)>
            <TELL !\)>
            <SET PDO <REST .PDO ,PDO-SIZE>>>>>

<GLOBAL PSEUDO-LOC <>>

<OBJECT PSEUDO-OBJECT
    (DESC "that")
    (ACTION <>)
    (FLAGS NARTICLEBIT)>

<SETG NEXT-PSEUDO-AUTO-ACTION 1>

<DEFINE THINGS-PROPSPEC (L "AUX" CNT R)
    <SET L <REST .L>>
    <COND (<NOT <0? <MOD <LENGTH .L> 3>>>
           <ERROR LENGTH-NOT-DIVISIBLE-BY-3 .L>)>
    <SET CNT </ <LENGTH .L> 3>>
    <SET R <MAPF ,LIST
                 <FUNCTION ("AUX" A N F NA NN)
                     <COND (<EMPTY? .L> <MAPSTOP>)>
                     <SET A <1 .L>>
                     <SET N <2 .L>>
                     <SET F <3 .L>>
                     <SET L <REST .L 3>>
                     <COND (<AND <TYPE? .A LIST> <LENGTH? .A 1>>
                            <COND (<LENGTH? .A 0> <ERROR EMPTY-ADJ-LIST>)
                                  (ELSE <SET A <1 .A>>)>)>
                     <COND (<AND <TYPE? .N LIST> <LENGTH? .N 1>>
                            <COND (<LENGTH? .N 0> <ERROR EMPTY-NOUN-LIST>)
                                  (ELSE <SET N <1 .N>>)>)>
                     <SET NA <COND (<TYPE? .A FALSE> 0)
                                   (<TYPE? .A ATOM> 1)
                                   (ELSE <LENGTH .A>)>>
                     <SET NN <COND (<TYPE? .N FALSE> 0)
                                   (<TYPE? .N ATOM> 1)
                                   (ELSE <LENGTH .N>)>>
                     <MAPRET
                         <BYTE .NA>
                         <BYTE .NN>
                         ;"Adjectives"
                         <COND (<TYPE? .A LIST>
                                <BIND ((L <MAPF ,LIST
                                             <FUNCTION (ATM "AUX" A?ATM)
                                                 <VERSION?
                                                     (ZIP
                                                      <VOC <SPNAME .ATM> ADJ>
                                                      <SET A?ATM <PARSE <STRING "A?" <SPNAME .ATM>>>>
                                                      <FORM GVAL .A?ATM>)
                                                     (ELSE
                                                      <VOC <SPNAME .ATM> ADJ>)>>
                                             .A>))
                                    <VERSION? (ZIP <PTABLE (BYTE) !.L>)
                                              (ELSE <PTABLE !.L>)>>)
                               (<TYPE? .A ATOM>
                                <VERSION? (ZIP
                                           <BIND ((A?A <PARSE <STRING "A?" <SPNAME .A>>>))
                                               <VOC <SPNAME .A> ADJ>
                                               <FORM GVAL .A?A>>)
                                          (ELSE
                                           <VOC <SPNAME .A> ADJ>)>)
                               (ELSE .A)>
                         ;"Nouns"
                         <COND (<TYPE? .N LIST>
                                <MAPF ,PTABLE
                                      <FUNCTION (ATM) <VOC <SPNAME .ATM> OBJECT>>
                                      .N>)
                               (<TYPE? .N ATOM>
                                <VOC <SPNAME .N> OBJECT>)
                               (ELSE .N)>
                         ;"Action routine or text"
                         <COND (<OR <TYPE? .F STRING>
                                    <AND <TYPE? .F LIST>
                                         <==? <LENGTH? .F 2> 2>
                                         <TYPE? <1 .F> VECTOR>
                                         <TYPE? <2 .F> STRING>>>
                                ;"Create a simple action routine that responds to the verbs"
                                <BIND (NAME NUM (VERBS '(EXAMINE)))
                                    <COND (<TYPE? .F LIST>
                                           <SET VERBS <1 .F>>
                                           <SET F <2 .F>>)>
                                    <SET NUM ,NEXT-PSEUDO-AUTO-ACTION>
                                    <SETG NEXT-PSEUDO-AUTO-ACTION <+ .NUM 1>>
                                    <SET NAME <PARSE <STRING "PSEUDO-AUTO-ACTION-"
                                                             <UNPARSE .NUM>>>>
                                    <EVAL <FORM ROUTINE .NAME '()
                                                <FORM COND <LIST <FORM VERB? !.VERBS>
                                                                 <FORM PRINTR .F>>>>>
                                    .NAME>)
                               (<TYPE? .F ATOM FALSE> .F)
                               (ELSE <ERROR BAD-PSEUDO-ACTION .F>)>>>>>
    (<> <PTABLE .CNT !.R>)>

<PUTPROP THINGS PROPSPEC THINGS-PROPSPEC>
