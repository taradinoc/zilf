"Orphaning is the mechanism where the parser asks a question to clarify an incomplete command
and the player can complete the command by typing a noun phrase at the next prompt.

There are a few cases where orphaning can happen:

- The command was missing a noun (like [GET]) and we couldn't fill it in using the FIND flag.

  In this case, the incomplete command had no noun phrase and thus no matched objects.

  We try to use the new noun phrase to match some objects and complete the command.
  If the noun phrase is ambiguous, we may orphan again.

- The direct or indirect object was ambiguous, i.e. it matched multiple objects when ALL/ANY
  weren't specified.

  In this case, the incomplete command had at least one noun phrase and matched a set of objects.

  We use the new noun phrase as a filter to narrow down the matched set. If the set isn't
  narrowed down to a single object, and if ALL/ANY still isn't specified, we either orphan
  again (if the new noun phrase eliminated at least one candidate) or give up (if it's still
  just as ambiguous).

The data we need to store for orphaning is:

- Whether we're orphaning at all
- Why we're orphaning (noun missing, or noun ambiguous)
- Which noun we're trying to improve (PRSO or PRSI)
- Everything about the current command (PRSA, P-V-WORD, P-PRSOS, etc.)

In the future, we may also need to store something about which part of the noun phrase
we're trying to improve (e.g. which YSPEC is ambiguous). So we use the top bits of a word
to recall whether we're orphaning and why, and reserve the rest for future use."

<CONSTANT P-OF-ORPHANING 32768>
<CONSTANT P-OF-MISSING 16384>
<CONSTANT P-OF-PRSI 8192>

<GLOBAL P-O-REASON <>>
<GLOBAL P-O-CONT <>>

<DEFMAC ORPHANING? () '<NOT <0? ,P-O-REASON>>>
<DEFMAC ORPHANING-PRSI? () '<BTST ,P-O-REASON ,P-OF-PRSI>>
<DEFMAC ORPHANING-BECAUSE-MISSING? () '<BTST ,P-O-REASON ,P-OF-MISSING>>

<DEFMAC ORPHAN (WHETHER "OPT" WHY WHICH "AUX" V)
    <COND (<NOT .WHETHER> <SET V 0>)
          (<N==? .WHETHER T> <ERROR BAD-ARGUMENT WHETHER .WHETHER>)
          (ELSE
           <COND (<NOT <AND <ASSIGNED? WHY> <ASSIGNED? WHICH>>>
                  <ERROR MISSING-ARGUMENT '(WHY WHICH)>)>
           <SET V ,P-OF-ORPHANING>
           <COND (<==? .WHY MISSING>
                  <SET V <ORB .V ,P-OF-MISSING>>)
                 (<N==? .WHY AMBIGUOUS>
                  <ERROR BAD-ARGUMENT WHY .WHY>)>
           <COND (<==? .WHICH PRSI>
                  <SET V <ORB .V ,P-OF-PRSI>>)
                 (<N==? .WHICH PRSO>
                  <ERROR BAD-ARGUMENT WHICH .WHICH>)>)>
    <FORM PROG '()
          <FORM SETG P-O-REASON .V>
          '<SETG P-V-WORDN 0>
          '<SETG P-O-CONT ,P-CONT>>>

<CONSTANT O-RES-NOT-HANDLED 0>   ;"Not an orphaning response; parse as usual"
<CONSTANT O-RES-REORPHANED 1>    ;"We asked another question; abort parse"
<CONSTANT O-RES-FAILED 2>        ;"The response didn't please us; abort parse"
<CONSTANT O-RES-SET-NP 3>        ;"We set P-NP-[DI]OBJ; find objects"
<CONSTANT O-RES-SET-PRSTBL 4>    ;"We set P-PRS[OI]S and PRS[OI]; perform command"

"Checks the player's command to see if it answers our orphaning question, and
tries to complete the previous command if so.

Sets:
  PRSO
  PRSI
  P-PRSOS
  P-PRSIS

Returns:
  One of the O-RES-* codes above to indicate what action was taken, if any."
<CONSTANT TRY-REPHRASING-CMD " Try rephrasing the command.">
<ROUTINE HANDLE-ORPHAN-RESPONSE ("AUX" CNT MAX TBL O OUT NY)
    ;"Confirm that the command looks like a noun phrase, and parse it into P-NP-XOBJ."
    <COND (<OR <L? ,P-LEN 1>
               <NOT <OR <STARTS-NOUN-PHRASE? <GETWORD? 1>>
                        <PARSE-NUMBER? 1>>>
               <NOT <=? <PARSE-NOUN-PHRASE 1 ,P-NP-XOBJ T> <+ ,P-LEN 1>>>>
           <TRACE 1 "[HANDLE-ORPHAN-RESPONSE: doesn't look like a noun phrase]" CR>
           <RETURN ,O-RES-NOT-HANDLED>)>
    
    <TRACE 1 "[HANDLE-ORPHAN-RESPONSE: REASON=" N ,P-O-REASON
             <COND (<ORPHANING-BECAUSE-MISSING?> " MISSING") (ELSE " AMBIGUOUS")>
             <COND (<ORPHANING-PRSI?> " PRSI") (ELSE " PRSO")>
             "]" CR>
    <TRACE-IN>

    ;"If we were processing an order, restore P-CONT."
    <AND <VERB? TELL>
         <SETG P-CONT ,P-O-CONT>
         <ACTIVATE-BUFS "CONT">>

    ;"To fill in a missing noun phrase, just copy the new one in place."
    <COND (<ORPHANING-BECAUSE-MISSING?>
           <COPY-NOUN-PHRASE
               ,P-NP-XOBJ
               <COND (<ORPHANING-PRSI?> ,P-NP-IOBJ) (ELSE ,P-NP-DOBJ)>>
           <TRACE 2 "[setting NP: " NOUN-PHRASE ,P-NP-XOBJ "]" CR>
           <TRACE-OUT>
           <RETURN ,O-RES-SET-NP>)>
    ;"We're disambiguating. Loop over the previously matched objects and only keep the
      ones that match the new NP."
    <COND (<ORPHANING-PRSI?> <SET TBL ,P-PRSIS>) (ELSE <SET TBL ,P-PRSOS>)>
    <SET MAX <GETB .TBL 0>>
    <SET CNT 0>
    <SET OUT ,P-XOBJS>

    <TRACE 2 "[filtering " N .MAX " objects with NP: " NOUN-PHRASE ,P-NP-XOBJ "]" CR>

    <SET NY <NP-YCNT ,P-NP-XOBJ>>
    <TRACE-IN>
    <DO (I 1 .MAX)
        <SET O <GET/B .TBL .I>>
        <COND (<AND <OR <0? .NY> <NP-INCLUDES? ,P-NP-XOBJ .O>>
                    <NOT <NP-EXCLUDES? ,P-NP-XOBJ .O>>>
               <TRACE 3 "[accepting " T .O "]" CR>
               <SET CNT <+ .CNT 1>>
               <PUT/B .OUT .CNT .O>)
              (ELSE <TRACE 3 "[rejecting " T .O "]" CR>)>>
    <TRACE-OUT>
    <PUTB .OUT 0 .CNT>
    
    <TRACE 2 "[filter kept " N .CNT " object(s)]" CR>
    
    ;"Fill in PRSO/PRSI, and swap the newly created table with P-PRSOS or P-PRSIS."
    <COND (<0? .CNT>
           <SET O <>>)
          (<1? .CNT>
           <SET O <GET/B .OUT 1>>)
          (<=? <NP-MODE ,P-NP-XOBJ> ,MCM-ANY>
           ;"Pick a random object"
           <PUT/B .OUT 1 <SET O <GET/B .OUT <RANDOM .CNT>>>>
           <PUTB .OUT 0 1>
           <SET CNT 1>
           <TELL "[" T .O "]" CR>)
          (ELSE <SET O ,MANY-OBJECTS>)>
    <COND (<ORPHANING-PRSI?>
           <SETG P-PRSIS ,P-XOBJS>
           <SETG PRSI .O>)
          (ELSE
           <SETG P-PRSOS ,P-XOBJS>
           <SETG PRSO .O>)>
    <SETG P-XOBJS .TBL>
    ;"See if we solved the problem."
    <COND (<0? .CNT>
           <TELL "That wasn't an option." ,TRY-REPHRASING-CMD CR>
           <SETG P-CONT 0>
           <RETURN ,O-RES-FAILED>)
          (<OR <1? .CNT> <=? <NP-MODE ,P-NP-XOBJ> ,MCM-ALL>>
           <RETURN ,O-RES-SET-PRSTBL>)
          (<L? .CNT .MAX>
           <TELL "That narrowed it down a little. ">
           <WHICH-DO-YOU-MEAN .OUT>
           <RETURN ,O-RES-REORPHANED>)
          (ELSE
           <TELL "That didn't narrow it down at all." ,TRY-REPHRASING-CMD CR>
           <SETG P-CONT 0>
           <RETURN ,O-RES-FAILED>)>>
