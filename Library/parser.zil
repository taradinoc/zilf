"Library header"

<CONSTANT ZILLIB-VERSION "J1">

"Debugging"

<SETG DEBUG <>>

<DEFMAC IF-DEBUG ("ARGS" A)
    <FORM PROG '()
        !<COND (,DEBUG .A) (ELSE '(<>))>>>

"In V3, we need these globals for the status line. In V5, we could
call them something else, but we'll continue the tradition anyway."

<GLOBAL HERE <>>
<GLOBAL SCORE 0>
<GLOBAL TURNS 0>
"next two for the Interrupt Queue"
<CONSTANT IQUEUE <ITABLE 20>>
<GLOBAL TEMPTABLE <ITABLE 50 <> >>
<GLOBAL IQ-LENGTH 0>
<GLOBAL STANDARD-WAIT 4>
<GLOBAL AGAINCALL 0>
<GLOBAL USAVE 0>
<GLOBAL ONCAGAIN 0>
<GLOBAL NLITSET <>>
<GLOBAL DBCONT <>>
<GLOBAL DTURNS <>>

<DEFMAC TELL ("ARGS" A "AUX" O P)
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
                (<TYPE? .I ATOM>
                    <SET P <1 .A>>
                    <SET A <REST .A>>
                    <COND
                        (<==? .I N> <FORM PRINTN .P>)
                        (<==? .I D> <FORM PRINTD .P>)
                        (<==? .I B> <FORM PRINTB .P>)>)
                (ELSE
                    <FORM PRINT .I>)>>>>
    <COND
        (<LENGTH? .O 0> <>)
        (<LENGTH? .O 1> <1 .O>)
        (ELSE <FORM PROG '() !.O>)>>

"Version considerations: certain values are bytes on V3 but words on all
other versions. These macros let us write the same code for all versions."

<VERSION?
	(ZIP
		<DEFMAC GET/B ('T 'O) <FORM GETB .T .O>>
		<DEFMAC IN-PB/WTBL? ('O 'P 'V) <FORM IN-PBTBL? .O .P .V>>)
	(ELSE
		<DEFMAC GET/B ('T 'O) <FORM GET .T .O>>
		<DEFMAC IN-PB/WTBL? ('O 'P 'V) <FORM IN-PWTBL? .O .P .V>>)>

"Parser"

<CONSTANT READBUF-SIZE 100>
<CONSTANT READBUF <ITABLE NONE 100>>
<CONSTANT BACKREADBUF <ITABLE NONE 100>>
<CONSTANT LEXBUF <ITABLE 59 (LEXV) 0 #BYTE 0 #BYTE 0>>
<CONSTANT BACKLEXBUF <ITABLE 59 (LEXV) 0 #BYTE 0 #BYTE 0>>

<CONSTANT P1MASK 3>

<GLOBAL WINNER PLAYER>

<GLOBAL PRSA <>>
<GLOBAL PRSO <>>
<GLOBAL PRSI <>>
<GLOBAL PRSO-DIR <>>

<DEFMAC VERB? ("ARGS" A)
    <SET O <MAPF ,LIST
        <FUNCTION (I)
            <FORM GVAL <PARSE <STRING "V?" <SPNAME .I>>>>>
        .A>>
    <FORM EQUAL? ',PRSA !.O>>

<DEFMAC WORD? ('W 'T)
    <FORM CHKWORD? .W
        <FORM GVAL <PARSE <STRING "PS?" <SPNAME .T>>>>
        <FORM GVAL <PARSE <STRING "P1?" <SPNAME .T>>>>>>

<VERSION?
    (ZIP
        <CONSTANT VOCAB-FL 4>   ;"part of speech flags"
        <CONSTANT VOCAB-V1 5>   ;"value for 1st part of speech"
        <CONSTANT VOCAB-V2 6>   ;"value for 2nd part of speech")
    (T
        <CONSTANT VOCAB-FL 6>
        <CONSTANT VOCAB-V1 7>
        <CONSTANT VOCAB-V2 8>)>

<ROUTINE CHKWORD? (W PS "OPT" (P1 -1) "AUX" F)
    <COND (<0? .W> <RFALSE>)>
    <IF-DEBUG <TELL "[CHKWORD: " B .W " PS=" N .PS " P1=" N .P1>>
    <SET F <GETB .W ,VOCAB-FL>>
    <IF-DEBUG <TELL " F=" N .F>>
    <SET F <COND (<BTST .F .PS>
                    <COND (<L? .P1 0>
                            <RTRUE>)
                        (<==? <BAND .F ,P1MASK> .P1>
                            <GETB .W ,VOCAB-V1>)
                        (ELSE <GETB .W ,VOCAB-V2>)>)>>
    <IF-DEBUG <TELL " = " N .F "]" CR>>
    .F>

<ROUTINE GETWORD? (N "AUX" R)
    <SET R <GET ,LEXBUF <- <* .N 2> 1>>>
    <IF-DEBUG
        <TELL "[GETWORD " N .N " = ">
        <COND (.R <TELL B .R>) (ELSE <TELL "?">)>
        <TELL "]" CR>>
    .R>

<ROUTINE PRINT-WORD (N "AUX" I MAX)
    <SET I <GETB ,LEXBUF <+ <* .N 4> 1>>>
    <SET MAX <- <+ .I <GETB ,LEXBUF <* .N 4>>> 1>>
    <REPEAT ()
        <PRINTC <GETB ,READBUF .I>>
        <AND <IGRTR? I .MAX> <RETURN>>>>

<GLOBAL P-LEN 0>
<GLOBAL P-V <>>
<GLOBAL P-NOBJ 0>
<GLOBAL P-P1 <>>
<GLOBAL P-P2 <>>


<CONSTANT P-MAXOBJS 10>
"These each have one length byte, one mode byte, then P-MAXOBJS pairs of words."
<CONSTANT P-DOBJS <ITABLE 21 <>>>
<CONSTANT P-DOBJEX <ITABLE 21 <>>>
<CONSTANT P-IOBJS <ITABLE 21 <>>>
<CONSTANT P-IOBJEX <ITABLE 21 <>>>
"for recalling last PRSO with IT"
<CONSTANT P-DOBJS-BACK <ITABLE 21 <>>>
<CONSTANT P-DOBJEX-BACK <ITABLE 21 <>>>
<GLOBAL IT-USE 0>
<GLOBAL IT-ONCE 0>
"for recalling last PRSO with THEM"
<CONSTANT P-TOBJS-BACK <ITABLE 21<>>>
<CONSTANT P-TOBJEX-BACK <ITABLE 21 <>>>
<GLOBAL THEM-USE 0>
<GLOBAL THEM-ONCE 0>
"for recalling last male PRSO with HIM"
<CONSTANT P-MOBJS-BACK <ITABLE 21 <>>>
<CONSTANT P-MOBJEX-BACK <ITABLE 21 <>>>
<GLOBAL HIM-USE 0>
<GLOBAL HIM-ONCE 0>
"for recalling last PRSO with HER"
<CONSTANT P-FOBJS-BACK <ITABLE 21 <>>>
<CONSTANT P-FOBJEX-BACK <ITABLE 21 <>>>
<GLOBAL HER-USE 0>
<GLOBAL HER-ONCE 0>

<BUZZ A AN AND ANY ALL BUT EXCEPT OF ONE THE \. \, \">

<ROUTINE PARSER ("AUX" NOBJ VAL DIR)
    <SET NOBJ <>>
    <SET VAL <>>
    <SET DIR <>>
    <SETG HERE <LOC ,PLAYER>>
    <READLINE>
    <IF-DEBUG <DUMPLINE>>
    <SETG P-LEN <GETB ,LEXBUF 1>>
    <SETG P-V <>>
    <SETG P-NOBJ 0>
    <PUT ,P-DOBJS 0 0>
    <PUT ,P-IOBJS 0 0>
    <SETG P-P1 <>>
    <SETG P-P2 <>>
    <SETG HERE <LOC ,WINNER>>
    <REPEAT ((I 1) W)
    	;<TELL "W is currently " N .W CR>
         ;"setting of W separated out so verb recogniton could also be separated"
         <COND
		(<G? .I ,P-LEN>
				<RETURN>)
		(<NOT <SET W <GETWORD? .I>>>
			<TELL "I don't know the word \"">
			<PRINT-WORD .I>
			<TELL "\"." CR>
			<RFALSE>)>
	;"verb recognition separated from rest so a verb and object can have the same name"
         <COND
		(<AND <CHKWORD? .W ,PS?VERB>
			    <NOT ,P-V>>
			          <SETG P-V <WORD? .W VERB>>)
	         (ELSE
			<COND
				(<AND <EQUAL? ,P-V <> ,ACT?WALK>
						<SET VAL <WORD? .W DIRECTION>>>
					<SET DIR .VAL>)
				(<SET VAL <CHKWORD? .W ,PS?PREPOSITION 0>>
					<COND (<AND <==? .NOBJ 0> <NOT ,P-P1>>
							<SETG P-P1 .VAL>)
						(<AND <==? .NOBJ 1> <NOT ,P-P2>>
							<SETG P-P2 .VAL>)>)
				(<STARTS-CLAUSE? .W>
					<SET NOBJ <+ .NOBJ 1>>
					<COND (<==? .NOBJ 1>
							<SET VAL <MATCH-CLAUSE .I ,P-DOBJS ,P-DOBJEX>>)
						(<==? .NOBJ 2>
							<SET VAL <MATCH-CLAUSE .I ,P-IOBJS ,P-IOBJEX>>)
						(ELSE
							<TELL "That sentence has too many objects." CR>
							<RFALSE>)>
					<COND (.VAL
							<SET I .VAL>
							<AGAIN>)
						(ELSE
							<TELL "That noun clause didn't make sense." CR>
							<RFALSE>)>)
				(ELSE
					<TELL "I didn't expect the word \"">
					<PRINT-WORD .I>
					<TELL "\" there." CR>
					<RFALSE>)>
		)>
        <SET I <+ .I 1>>>
    <SETG P-NOBJ .NOBJ>
    <IF-DEBUG <TELL "[PARSER: V=" N ,P-V " NOBJ=" N ,P-NOBJ
        " P1=" N ,P-P1 " DOBJS=" N <GETB ,P-DOBJS 0>
        " P2=" N ,P-P2 " IOBJS=" N <GETB ,P-IOBJS 0> "]" CR>>
    <COND
        (.DIR
            <SETG PRSO-DIR T>
            <SETG PRSA ,V?WALK>
            <SETG PRSO .DIR>
            <SETG PRSI <>>
	    <VERSION?
		(ZIP <>)
		(EZIP <>)
		(ELSE
		 ;"save state for undo after moving from room to room"
		  <COND (<AND <NOT <VERB? UNDO >>
					   <NOT <EQUAL? ,AGAINCALL 1>>>
					    <SETG USAVE <ISAVE>>
					    ;<TELL "ISAVE returned " N ,USAVE CR>
					    <COND (<EQUAL? ,USAVE 2>
							<TELL "Previous turn undone." CR>
							;<SETG USAVE 0>  ;"prevent undoing twice in a row"
						<AGAIN>)>)>
		)>
            <RTRUE>)
        (<NOT ,P-V>
            <TELL "That sentence has no verb." CR>
            <RFALSE>)
        (ELSE
            <SETG PRSO-DIR <>>
            <COND 
            	(<AND
                	<MATCH-SYNTAX>
                	<FIND-OBJECTS>>
			<VERSION?
				(ZIP <>)
				(EZIP <>)
				(ELSE
				;"save state for UNDO, unless we're about to UNDO the previous command"
						<COND (<AND <NOT <VERB? UNDO >>
									<NOT <EQUAL? ,AGAINCALL 1>>>
					<SETG USAVE <ISAVE>>
					   ; <TELL "ISAVE returned " N ,USAVE CR>
					    <COND (<EQUAL? ,USAVE 2>
							<TELL "Previous turn undone." CR>
							;<SETG USAVE 0>  ;"prevent undoing twice in a row"
						<AGAIN>
						;<SETG NOUAGAIN 0>
						)>)>
				)>
                		;"if successful PRSO and not after an IT use, back up PRSO for IT"
                	    <COND (<AND <EQUAL? ,IT-USE 0>
                				    <AND ,PRSO>
                				    <NOT <FSET? ,PRSO ,PERSONBIT>>
                				    <NOT <FSET? ,PRSO ,PLURALBIT>>>
                				  		;<TELL "Copying P-DOBJS into P-DOBJS-BACK" CR>
                				  		<COPY-TABLE ,P-DOBJS ,P-DOBJS-BACK 21>
                				  		<COPY-TABLE ,P-DOBJEX ,P-DOBJEX-BACK 21>
                				  		<COND (<EQUAL? ,IT-ONCE 0> <SET ,IT-ONCE 1>)>)
                	    ;"if PRSO has PLURALBIT, back up to THEM instead"
                	         (<AND <EQUAL? ,THEM-USE 0>
                				    <AND ,PRSO>
                				    <NOT <FSET? ,PRSO ,PERSONBIT>>
                				    <FSET? ,PRSO ,PLURALBIT>>
                				  		;<TELL "Copying P-DOBJS into P-TOBJS-BACK" CR>
                				  		<COPY-TABLE ,P-DOBJS ,P-TOBJS-BACK 21>
                				  		<COPY-TABLE ,P-DOBJEX ,P-TOBJEX-BACK 21>
                				  		<COND (<EQUAL? ,THEM-ONCE 0> <SET ,THEM-ONCE 1>)>)
                	    ;"if successful PRSO who is male, back up PRSO for HIM"
                	    	   (<AND <EQUAL? ,HIM-USE 0>
                				    	<AND ,PRSO>
                				    	<FSET? ,PRSO ,PERSONBIT>
                				    	<NOT <FSET? ,PRSO ,FEMALEBIT>>>
                				  			;<TELL "Copying P-DOBJS into P-MOBJS-BACK" CR>
                				  			<COPY-TABLE ,P-DOBJS ,P-MOBJS-BACK 21>
                				  			<COPY-TABLE ,P-DOBJEX ,P-MOBJEX-BACK 21>
                				  			<COND (<0? ,HIM-ONCE> <SET ,HIM-ONCE 1>)>)
                	   ;"if successful PRSO who is female, back up PRSO for HER"
                	    	   (<AND <EQUAL? ,HER-USE 0>
                				    	<AND ,PRSO>
                				    	<FSET? ,PRSO ,PERSONBIT>
                				    	<FSET? ,PRSO ,FEMALEBIT>>
                				  			;<TELL "Copying P-DOBJS into P-FOBJS-BACK" CR>
                				  			<COPY-TABLE ,P-DOBJS ,P-FOBJS-BACK 21>
                				  			<COPY-TABLE ,P-DOBJEX ,P-FOBJEX-BACK 21>
                				  			<COND (<0? ,HER-ONCE> <SET ,HER-ONCE 1>)>
                				  			)>
                		<RTRUE>
                )>
         )>
>


<ROUTINE COPY-TABLE (T C S "AUX" N W)  
		<SET N 1>
		<REPEAT ()
	          <SET W <GETB .T .N>>
	          <PUTB .C .N .W> 
	          <SET N <+ .N 1>>
	          <COND (<G? .N .S> <RETURN>)>
	     >
>


<ROUTINE STARTS-CLAUSE? (W)
    <OR <EQUAL? .W ,W?A ,W?AN ,W?THE>
        <CHKWORD? .W ,PS?ADJECTIVE>
        <WORD? .W OBJECT>>>

<CONSTANT MCM-ALL 1>
<CONSTANT MCM-ANY 2>

<ROUTINE MATCH-CLAUSE (WN YTBL NTBL "AUX" (TI 1) W VAL (MODE 0) (ADJ <>) (NOUN <>) (BUT <>))
    <REPEAT ()
        <COND
            (<G? .WN ,P-LEN> <RETURN>)
            (<0? <SET W <GETWORD? .WN>>> <RFALSE>)
            (<EQUAL? .W ,W?ALL ,W?ANY ,W?ONE>
                <COND (<OR .MODE .ADJ .NOUN> <RFALSE>)>
                <SET MODE
                    <COND (<==? .W ,W?ALL> ,MCM-ALL)
                        (ELSE ,MCM-ANY)>>)
            (<VERSION?
                (ZIP <SET VAL <WORD? .W ADJECTIVE>>)
                (ELSE <AND <CHKWORD? .W ,PS?ADJECTIVE> <SET VAL .W>>)>
                    <COND
                        (<==? .TI ,P-MAXOBJS>
                            <TELL "That clause mentions too many objects." CR>
                            <RFALSE>)
                        (<NOT .ADJ> <SET ADJ .VAL>)>)
            (<WORD? .W OBJECT>
                <COND
                    (.NOUN <RETURN>)
                    (<==? .TI ,P-MAXOBJS>
                        <TELL "That clause mentions too many objects." CR>
                        <RFALSE>)
                    (ELSE <SET NOUN .W>)>)
            (<EQUAL? .W ,W?AND ,W?COMMA>
                <COND (<OR .ADJ .NOUN>
                        <PUT .YTBL .TI .ADJ>
                        <PUT .YTBL <+ .TI 1> .NOUN>
                        <SET TI <+ .TI 2>>)>)
            (<CHKWORD? .W ,PS?BUZZ-WORD>)       ;skip
            (ELSE <RETURN>)>
        <SET WN <+ .WN 1>>>
    <COND (<OR .ADJ .NOUN>
            <PUT .YTBL .TI .ADJ>
            <PUT .YTBL <+ .TI 1> .NOUN>
            <SET TI <+ .TI 2>>)>
    <PUTB .YTBL 0 </ <- .TI 1> 2>>
    <PUTB .YTBL 1 .MODE>
    .WN>

<CONSTANT SYN-REC-SIZE 8>
<CONSTANT SYN-NOBJ 0>
<CONSTANT SYN-PREP1 1>
<CONSTANT SYN-PREP2 2>
<CONSTANT SYN-FIND1 3>
<CONSTANT SYN-FIND2 4>
<CONSTANT SYN-OPTS1 5>
<CONSTANT SYN-OPTS2 6>
<CONSTANT SYN-ACTION 7>

<ROUTINE MATCH-SYNTAX ("AUX" PTR CNT TEST-PTR TEST-P-NOBJ)
    <SET PTR <GET ,VERBS <- 255 ,P-V>>>
    <SET CNT <GETB .PTR 0>>
    <SET PTR <+ .PTR 1>>
    <IF-DEBUG <TELL "[MATCH-SYNTAX: " N .CNT " syntaxes at " N .PTR "]" CR>>
    <REPEAT ()
        ;<TELL "CNT is currently " N .CNT CR>
        <PROG () <SET TEST-PTR <GET .PTR ,SYN-NOBJ>>
        		 <SET TEST-P-NOBJ ,P-NOBJ>
        		 ;<TELL "<GETB .PTR ,SYN-NOBJ> is " N .TEST-PTR "and P-NOBJ is " N .TEST-P-NOBJ CR>
        		 >
        <COND (<DLESS? CNT 0>
            		<TELL "I don't understand that sentence." CR>
            			   		<RFALSE>)
            (<AND <==? <GETB .PTR ,SYN-NOBJ> ,P-NOBJ>
                  <OR <L? ,P-NOBJ 1> <==? <GETB .PTR ,SYN-PREP1> ,P-P1>>
                  <OR <L? ,P-NOBJ 2> <==? <GETB .PTR ,SYN-PREP2> ,P-P2>>>
                <SETG PRSA <GETB .PTR ,SYN-ACTION>>
                <RTRUE>)>
        <SET PTR <+ .PTR ,SYN-REC-SIZE>>>>

<ROUTINE FIND-OBJECTS ("AUX" X)
	<COND (<G=? ,P-NOBJ 1>
    		<SET X <GET ,P-DOBJS 2>>
    		;<TELL "Find objects PRSO test - X is " N .X CR>
    		<COND (<EQUAL? .X W?IT>
    					<COND (<L? ,IT-ONCE 1> <TELL "I'm unsure what you're referring to." CR> <RFALSE>)>
    					;<TELL "IT found (in D.O.).  Replacing with backed-up P-DOBJS" CR>
							<COPY-TABLE ,P-DOBJS-BACK ,P-DOBJS  21>
                		    <COPY-TABLE ,P-DOBJEX-BACK ,P-DOBJEX  21>
    					<SETG IT-USE 1>
    					)
    			  (ELSE <SETG IT-USE 0>)>
    		<COND (<EQUAL? .X W?THEM>
    					<COND (<L? ,THEM-ONCE 1> <TELL "I'm unsure what you're referring to." CR> <RFALSE>)>
    					;<TELL "IT found (in D.O.).  Replacing with backed-up P-TOBJS" CR>
							<COPY-TABLE ,P-TOBJS-BACK ,P-DOBJS  21>
                		    <COPY-TABLE ,P-TOBJEX-BACK ,P-DOBJEX  21>
    					<SETG THEM-USE 1>
    					)
    			  (ELSE <SETG THEM-USE 0>)>
    		<COND (<EQUAL? .X W?HIM>
    					<COND (<0? ,HIM-ONCE> <TELL "I'm unsure to whom you are referring." CR> <RFALSE>)>
    					;<TELL "HIM found (in D.O.).  Replacing with backed-up P-DOBJS" CR>
							<COPY-TABLE ,P-MOBJS-BACK ,P-DOBJS  21>
                		    <COPY-TABLE ,P-MOBJEX-BACK ,P-DOBJEX  21>
    					<SETG HIM-USE 1>
    					)
    			  (ELSE <SETG HIM-USE 0>)>
    		<COND (<EQUAL? .X W?HER>
    					<COND (<0? ,HER-ONCE> <TELL "I'm unsure to whom you are referring." CR> <RFALSE>)>
    					;<TELL "HER found (in D.O.).  Replacing with backed-up P-DOBJS" CR>
							<COPY-TABLE ,P-FOBJS-BACK ,P-DOBJS  21>
                		    <COPY-TABLE ,P-FOBJEX-BACK ,P-DOBJEX  21>
    					<SETG HER-USE 1>
    					)
    			  (ELSE <SETG HER-USE 0>)>
    		<COND (<NOT <SETG PRSO <FIND-ONE-OBJ ,P-DOBJS ,P-DOBJEX>>>
                    	<RFALSE>)
            >)
         (ELSE <SETG PRSO <>>)>
    <COND (<G=? ,P-NOBJ 2>
    		<SET X <GET ,P-IOBJS 2>>
    		;<TELL "Find objects PRSI test - X is " N .X CR>
			<COND (<EQUAL? .X W?IT>
    					;<TELL "IT found (in .I.O). Replacing with backed-up P-DOBJS" CR>
						<COPY-TABLE ,P-DOBJS-BACK ,P-IOBJS 21>
                		<COPY-TABLE ,P-DOBJEX-BACK ,P-IOBJEX 21>
    					<SETG IT-USE 1>
    					)
    			  (ELSE <SETG IT-USE 0>)>
    	    <COND (<EQUAL? .X W?THEM>
    					;<TELL "THEM found (in .I.O). Replacing with backed-up P-DOBJS" CR>
						<COPY-TABLE ,P-TOBJS-BACK ,P-IOBJS 21>
                		<COPY-TABLE ,P-TOBJEX-BACK ,P-IOBJEX 21>
    					<SETG THEM-USE 1>
    					)
    			  (ELSE <SETG THEM-USE 0>)>
    		<COND (<EQUAL? .X W?HIM>
    					;<TELL "HIM found (in .I.O). Replacing with backed-up P-DOBJS" CR>
						<COPY-TABLE ,P-MOBJS-BACK ,P-IOBJS 21>
                		<COPY-TABLE ,P-MOBJEX-BACK ,P-IOBJEX 21>
    					<SETG HIM-USE 1>
    					)
    			  (ELSE <SETG HIM-USE 0>)>
    		<COND (<EQUAL? .X W?HER>
    					;<TELL "HER found (in .I.O). Replacing with backed-up P-DOBJS" CR>
							<COPY-TABLE ,P-FOBJS-BACK ,P-IOBJS 21>
                		    <COPY-TABLE ,P-FOBJEX-BACK ,P-IOBJEX 21>
    					<SETG HER-USE 1>
    					)
    			  (ELSE <SETG HER-USE 0>)>
    		<COND (<NOT <SETG PRSI <FIND-ONE-OBJ ,P-IOBJS ,P-IOBJEX>>>
                    	<RFALSE>)
            >)
          (ELSE <SETG PRSI <>>)>
    <RTRUE>>

;"This seems really inefficient - mostly repeating FIND-ONE-OBJ & CONTAINER-SEARCH - can't think of better right now"
<ROUTINE SEARCH-FOR-LIGHT ("AUX" F G H GMAX X)
	   ;<TELL "search inventory for light source">
	    <OBJECTLOOP I ,WINNER
	    ;<TELL "I is currently " D . I  CR>
	    <COND (<FSET? .I ,LIGHTBIT>
                <SET F .I>
                <RTRUE>)
              ;"if any items are surfaces or open containers, search their contents"
              (<COND (<OR <FSET? .I ,SURFACEBIT>
				    	  <AND  <FSET? .I ,OPENABLE>
				    	  		<FSET? .I ,OPENBIT>
				    	  >
				    	  ;"the always-open case"
				    	  <AND  <FSET? .I ,CONTBIT> 
				    	  		<NOT <FSET? .I ,OPENABLE>>
				    	  >
				    	  ;"transparent container"
                          <AND  <FSET? .I ,CONTBIT> 
                                <FSET? .I ,TRANSBIT>>
				      >
              	   				;<TELL D .I " is a container." CR>
              	   				<SET F <CONTAINER-LIGHT-SEARCH .I>>
              	   				;<TELL "Back in main SEARCH-FOR-LIGHT loop and F is " D .F CR>
              	   				<AND .F <RETURN>>
              	   				;<RETURN>
              	      )>
               )       
        >>
        <AND .F <RTRUE>>
        ;"check location"
    	<OBJECTLOOP I ,HERE
    	;<TELL "Room Loop object is currently " D .I CR>
        	<COND (<FSET? .I ,LIGHTBIT>
                <SET F .I>
                ;<TELL "F is now set to " D .F CR>
                <RETURN>)
        	  ;"if any items are surfaces or open containers, search their contents"
        	  (<COND (<OR <FSET? .I ,SURFACEBIT>
				    	  <FSET? .I ,OPENBIT>
				    	  ;"transparent container"
                          <AND  <FSET? .I ,CONTBIT> 
                                <FSET? .I ,TRANSBIT>>
				    	  <AND  <FSET? .I ,CONTBIT> 
                                <NOT <FSET? .I ,OPENABLE>>
                          >
                	  >
              	   				;<TELL D .I " is a container." CR>
              	   				<SET F <CONTAINER-LIGHT-SEARCH .I>>
              	   				;<TELL "Back in main SEARCH-FOR-LIGHT loop and F is " D .F CR>
              	   				<AND .F <RETURN>>
              	   				;<RETURN>
              	      )>
               )
         	>
    	>
    	<AND .F <RTRUE>>
    	;"check global objects"
     	<OBJECTLOOP I ,GLOBAL-OBJECTS
        	<COND (<FSET? .I ,LIGHTBIT>
                <SET F .I>
                <RETURN>)>>
    	<AND .F <RETURN .F>>
    	;"check local-globals"
    	<OBJECTLOOP I ,LOCAL-GLOBALS
    		<COND (<FSET? .I ,LIGHTBIT>
    				;<TELL "FOUND OBJ with light in LOCAL-GLOBALS - now checking room" CR>
    				;<PROG () <TELL "I IS SET TO "> <PRINTN .I> <TELL CR>>
    				<OR <SET H <GETPT ,HERE ,P?GLOBAL>> <RETURN>>
   				        <SET GMAX <- </ <PTSIZE .H> 2> 1>>
    					<REPEAT ((X 0))
       							 <COND
            						(<==? <GET .H .X> .I>
            								;<TELL "ROOM has a object that matches this GLOBAL with LIGHTBIT" CR>
            								<SET F .I>
                							<RETURN>)
            						(<IGRTR? X .GMAX> <RETURN>)
            					  >
            			>
        	  )>>
    	<AND .F <RTRUE>> 
    	;<TELL "no light source found" CR>
    	<RFALSE>>      



<ROUTINE CONTAINER-LIGHT-SEARCH (I "AUX" J F)
    		<OBJECTLOOP J .I
                    ;<TELL "Searching contents of " D .I " for light source" CR>
                    ;<TELL "Current object is " D .J CR>
                    <COND (<FSET? .J ,LIGHTBIT>
                          	<SET F .J>
                            ;<TELL "Light source match found in container search, F is now " D .F CR>
                            <RETURN>)
                          (<OR 	<FSET? .J ,SURFACEBIT>
				    	  		<AND  <FSET? .J ,OPENABLE>
				    	  			  <FSET? .J ,OPENBIT>
				    	  		>
				    	  	    ;"the always-open case"
				    	  		<AND  <FSET? .J ,CONTBIT> 
				    	  			  <NOT <FSET? .J ,OPENABLE>>
				    	  		>
				      	   >                      				
				      ;<TELL "Found another container - about to search through " D .J CR>
                      				<SET F <CONTAINER-LIGHT-SEARCH .J>>
                     				<AND .F <RETURN>>)    	
                     >
             >
       		<AND .F <RETURN .F>>
 >





<ROUTINE FIND-ONE-OBJ (YTBL NTBL "AUX" A N F G H GMAX X P)
    <SET A <GET .YTBL 1>>
    <SET N <GET .YTBL 2>>
    <IF-DEBUG <TELL "[FIND-ONE-OBJ: adj=" N .A " noun=" N .N "]" CR>>
    ;"check abstract/generic objects"
    <OBJECTLOOP I ,GENERIC-OBJECTS
        <COND (<REFERS? .A .N .I>
                <SET F .I>
                <RETURN>)>>
    <AND .F <RETURN .F>>
    ;"check for light"
    <COND (<NOT <FSET? ,HERE ,LIGHTBIT>>
    			<COND (<NOT <SEARCH-FOR-LIGHT>>
                			<TELL "It's too dark to see anything here." CR>
                			<RFALSE>)>)>
    ;"check location"
    <OBJECTLOOP I ,HERE
    	;<TELL "Room Loop object is currently " D .I CR>
        <COND (<REFERS? .A .N .I>
                <SET F .I>
                ;<TELL "F is now set to " D .F CR>
                <RETURN>)
        	  ;"if any items are surfaces, open containers, or transparent containers, search their contents"
        	  (<COND (<OR <FSET? .I ,SURFACEBIT>
				    	  ;"open container"
				    	  <AND  <FSET? .I ,OPENABLE>
				    	  		<FSET? .I ,OPENBIT>
				    	  >
				    	  ;"always open container"
				    	  <AND  <FSET? .I ,CONTBIT> 
                                <NOT <FSET? .I ,OPENABLE>>
                          >
                          ;"transparent container"
                          <AND  <FSET? .I ,CONTBIT> 
                                <FSET? .I ,TRANSBIT>>
                          >
              	   				;<TELL D .I " is a container." CR>
              	   				<SET F <CONTAINER-SEARCH .I .A .N>>
              	   				;<TELL "Back in main FIND-ONE-OBJ loop and F is " D .F CR>
              	   				<AND .F <RETURN>>
              	   				;<RETURN>
              	      )>
               )
         >
    >
    <AND .F <RETURN .F>>       
    ;"check inventory"
    <OBJECTLOOP I ,WINNER
        <COND (<REFERS? .A .N .I>
                <SET F .I>
                <RETURN>)
              ;"if any items are surfaces or open containers, search their contents"
              (<COND (<OR <FSET? .I ,SURFACEBIT>
				    	  ;"open container"
				    	  <AND  <FSET? .I ,OPENABLE>
				    	  		<FSET? .I ,OPENBIT>
				    	  >
				    	  ;"always open container"
				    	  <AND  <FSET? .I ,CONTBIT> 
                                <NOT <FSET? .I ,OPENABLE>>
                          >
                          ;"transparent container"
                          <AND  <FSET? .I ,CONTBIT> 
                                <FSET? .I ,TRANSBIT>>
                          >
              	   				;<TELL D .I " is a container." CR>
              	   				<SET F <CONTAINER-SEARCH .I .A .N>>
              	   				;<TELL "Back in main FIND-ONE-OBJ loop and F is " D .F CR>
              	   				<AND .F <RETURN>>
              	   				;<RETURN>
              	      )>
               )       
        >>
    <AND .F <RETURN .F>>
    ;"check global objects"
     <OBJECTLOOP I ,GLOBAL-OBJECTS
        <COND (<REFERS? .A .N .I>
                <SET F .I>
                <RETURN>)>>
    <AND .F <RETURN .F>>
    ;"check local-globals"
    <OBJECTLOOP I ,LOCAL-GLOBALS
    	<COND (<REFERS? .A .N .I>
    				;<TELL "FOUND OBJ in LOCAL-GLOBALS - now checking room" CR>
    				;<PROG () <TELL "I IS SET TO "> <PRINTN .I> <TELL CR>>
    				<OR <SET H <GETPT ,HERE ,P?GLOBAL>> <RETURN>>
   				        <SET GMAX <- </ <PTSIZE .H> 2> 1>>
    					<REPEAT ((X 0))
       							 <COND
            						(<==? <GET .H .X> .I>
            								;<TELL "ROOM has a matching GLOBAL" CR>
            								<SET F .I>
                							<RETURN>)
            						(<IGRTR? X .GMAX> <RETURN>)
            					  >
            			>
        	  )>>
    <AND .F <RETURN .F>> 
    ;"no match"
    ;"TO DO - Search through containers in rooms to see if I-matching NPC is there for 'does not seem to be here' message"
    <OBJECTLOOP I ROOMS
    		<OBJECTLOOP J .I
    				<COND (<REFERS? .A .N .J>
    						<SET .P .J>
    						;<TELL "No match - P is " D .P CR>
    						<COND (<FSET? .P ,PERSONBIT>
    				  		  		<TELL D .P " does not seem to be here." CR > <RFALSE>)>
    				  		)>>>

    <TELL "You don't see that here." CR>
    <RFALSE>>
    
<ROUTINE CONTAINER-SEARCH (I A N "AUX" J F)
    		<OBJECTLOOP J .I
                    ;<TELL "Searching contents of " D .I CR>
                    ;<TELL "Current object is " D .J CR>
                    <COND (<REFERS? .A .N .J>
                          	<SET F .J>
                            ;<TELL "Match found in container search, F is now " D .F CR>
                            <RETURN>)
                          (<OR 	<FSET? .I ,SURFACEBIT>
				    	  		<AND  <FSET? .I ,OPENABLE>
				    	  			  <FSET? .I ,OPENBIT>
				    	  		>
				    	  	    ;"the always-open case"
				    	  		<AND  <FSET? .I ,CONTBIT> 
				    	  			  <NOT <FSET? .I ,OPENABLE>>
				    	  		>
				    	  	 	 ;"transparent container"
                          		<AND  <FSET? .I ,CONTBIT> 
                                <FSET? .I ,TRANSBIT>>
                          >                      				
				      ;<TELL "Found another container - about to search through " D .J CR>
                      				<SET F <CONTAINER-SEARCH .J .A .N>>
                     				<AND .F <RETURN>>)    	
                     >
             >
       		<AND .F <RETURN .F>>
 >
 
 
     	
                	
    
<ROUTINE GLOBAL-IN? (O R "AUX" H GMAX X)
	<OR <SET H <GETPT .R ,P?GLOBAL>> <RFALSE>>
   		<SET GMAX <- </ <PTSIZE .H> 2> 1>>
    	<REPEAT ((X 0))
       			<COND
            		(<==? <GET .H .X> .O>
            				;<TELL "ROOM has a matching GLOBAL" CR>
            				<RTRUE>)
            		(<IGRTR? X .GMAX> <RFALSE>)>
    	>
>

<ROUTINE REFERS? (A N O)
    <AND
        <OR <0? .A> <IN-PB/WTBL? .O ,P?ADJECTIVE .A>>
            <IN-PWTBL? .O ,P?SYNONYM .N>>>

<ROUTINE IN-PWTBL? (O P V "AUX" PT MAX)
    <OR <SET PT <GETPT .O .P>> <RFALSE>>
    <SET MAX <- </ <PTSIZE .PT> 2> 1>>
    <REPEAT ((I 0))
        <COND
            (<==? <GET .PT .I> .V> <RTRUE>)
            (<IGRTR? I .MAX> <RFALSE>)>>>

<ROUTINE IN-PBTBL? (O P V "AUX" PT MAX)
    <OR <SET PT <GETPT .O .P>> <RFALSE>>
    <SET MAX <- <PTSIZE .PT> 1>>
    <REPEAT ((I 0))
        <COND
            (<==? <GETB .PT .I> .V> <RTRUE>)
            (<IGRTR? I .MAX> <RFALSE>)>>>

<ROUTINE DUMPLINE ("AUX" (P <+ ,LEXBUF 2>) (WDS <GETB ,LEXBUF 1>))
    <TELL N .WDS " words:">
    <REPEAT ()
        <COND (<DLESS? WDS 0> <CRLF> <RTRUE>)
            (ELSE <TELL " "> <DUMPWORD <GET .P 0>>)>
        <SET P <+ .P 4>>>>
        
<ROUTINE DUMPLEX ("AUX" C (P <+ ,LEXBUF 2>) (WDS <GETB ,LEXBUF 1>))
    ;<TELL N .WDS " words:">
    <SET C 1>
        <REPEAT ()
        <TELL N .C " of LEXBUF is " N <GET ,LEXBUF .C> CR >
        <SET C <+ .C 1>>
        <COND (<G? .C .WDS> <RETURN>)>
    >>


<ROUTINE DUMPWORD (W "AUX" FL)
    <COND (.W
            <PRINTB .W>
            <TELL "(">
            <SET FL <GETB .W ,VOCAB-FL>>
            <COND (<BTST .FL ,PS?BUZZ-WORD> <TELL "B">)>
            <COND (<BTST .FL ,PS?PREPOSITION> <TELL "P">)>
            <COND (<BTST .FL ,PS?DIRECTION> <TELL "D">)>
            <COND (<BTST .FL ,PS?ADJECTIVE> <TELL "A">)>
            <COND (<BTST .FL ,PS?VERB> <TELL "V">)>
            <COND (<BTST .FL ,PS?OBJECT> <TELL "O">)>
            <TELL ")">)
        (ELSE <TELL "---">)>>
        

<ROUTINE COPY-LEXBUF ("AUX" C W (WDS <GETB ,LEXBUF 1>))  
		<SET C 1>
		<PUTB ,BACKLEXBUF 1 .WDS>
		<REPEAT ()
	          <SET W <GET ,LEXBUF .C>>
	          ;<TELL N .C "COPY LEX W is " N .W CR>
	          <PUT ,BACKLEXBUF  .C .W> 
	          <SET C <+ .C 1>>
	          <COND (<G? .C .WDS> <RETURN>)>
	     >
>

<ROUTINE RESTORE-LEX ("AUX" C W (WDS <GETB ,BACKLEXBUF 1>))  
		<SET C 1>
		<PUTB ,LEXBUF 1 .WDS>
		<REPEAT ()
	          <SET W <GET ,BACKLEXBUF .C>>
	          ;<TELL N .C "RESTORE LEX W is " N .W CR>
	          <PUT ,LEXBUF .C .W> 
	          <SET C <+ .C 1>>
	          <COND (<G? .C .WDS> <RETURN>)>
	     >
>

<ROUTINE COPY-READBUF ("AUX" C W)  
		<SET C 1>
		<REPEAT ()
	          <SET W <GETB ,READBUF .C>>
	          <PUTB ,BACKREADBUF .C .W> 
	          <SET C <+ .C 1>>
	          <COND (<G? .C 100> <RETURN>)>
	     >
>


<ROUTINE RESTORE-READBUF ("AUX" C W)  
		<SET C 1>
		<REPEAT ()
	          <SET W <GETB ,BACKREADBUF .C>>
	          <PUTB ,READBUF .C .W> 
	          <SET C <+ .C 1>>
	          <COND (<G? .C 100> <RETURN>)>
	     >
>

<ROUTINE DUMPBUF ("AUX" C (WDS <GETB ,READBUF 1>))
    ;<TELL N .WDS " words:">
    <SET C 1>
        <REPEAT ()
        <TELL N .C " of READBUF is " N <GET ,READBUF .C> CR >
        <SET C <+ .C 1>>
        <COND (<G? .C 100> <RETURN>)>
    >>
	           

;"The read buffer has a slightly different format on V3."
<ROUTINE READLINE ()
	;"skip input if doing an AGAIN"
	<COND (<AND ,AGAINCALL>
					<RETURN>)>
    <TELL CR "> ">
    <PUTB ,READBUF 0 <- ,READBUF-SIZE 2>>
    <VERSION? (ZIP <>) (ELSE <PUTB ,READBUF 1 0>)>
    <READ ,READBUF ,LEXBUF>>
    

"Action framework"

<ROUTINE PERFORM (ACT "OPT" DOBJ IOBJ "AUX" PRTN RTN OA OD ODD OI WON)
    <IF-DEBUG <TELL "[PERFORM: ACT=" N .ACT " DOBJ=" N .DOBJ " IOBJ=" N .IOBJ "]" CR>>
    <SET PRTN <GET ,PREACTIONS .ACT>>
    <SET RTN <GET ,ACTIONS .ACT>>
    <SET OA ,PRSA>
    <SET OD ,PRSO>
    <SET ODD ,PRSO-DIR>
    <SET OI ,PRSI>
    <SETG PRSA .ACT>
    <SETG PRSO .DOBJ>
    <SETG PRSO-DIR <==? .ACT ,V?WALK>>
    <SETG PRSI .IOBJ>
    <SET WON <PERFORM-CALL-HANDLERS .PRTN .RTN>>
    <SETG PRSA .OA>
    <SETG PRSO .OD>
    <SETG PRSO-DIR .ODD>
    <SETG PRSI .OI>
    .WON>

;"Handler order: player's ACTION, location's ACTION (M-BEG),
verb preaction, PRSI's ACTION, PRSO's ACTION, verb action."

<ROUTINE PERFORM-CALL-HANDLERS (PRTN RTN "AUX" AC RM)
    <COND (<AND <SET AC <GETP ,WINNER ,P?ACTION>>
            <APPLY .AC>>
                <RTRUE>)
        (<AND <SET RM <LOC ,WINNER>>
            <SET AC <GETP .RM ,P?ACTION>>
            <APPLY .AC ,M-BEG>>
                <RTRUE>)
        (<AND .PRTN <APPLY .PRTN>>
            <RTRUE>)
        (<AND ,PRSI
            <SET AC <GETP ,PRSI ,P?ACTION>>
            <APPLY .AC>>
                <RTRUE>)
        (<AND <NOT ,PRSO-DIR> ,PRSO
            <SET AC <GETP ,PRSO ,P?ACTION>>
            <APPLY .AC>>
                <RTRUE>)
        (ELSE <APPLY .RTN>)>>

<DEFMAC OBJECTLOOP ('VAR 'LOC "ARGS" BODY)
    <FORM REPEAT <LIST <LIST .VAR <FORM FIRST? .LOC>>>
        <FORM COND
            <LIST <FORM LVAL .VAR>
                !.BODY
                <FORM SET .VAR <FORM NEXT? <FORM LVAL .VAR>>>>
            '(ELSE <RETURN>)>>>

<ROUTINE GOTO (RM)
    <SETG HERE .RM>
    <MOVE ,WINNER ,HERE>
    <FSET ,HERE ,TOUCHBIT>
    <APPLY <GETP .RM ,P?ACTION> ,M-ENTER>
    ;"moved V-LOOK into GOTO so descriptors will be called when you call GOTO from a PER routine, etc"
    <V-LOOK>>

"Misc Routines"


<ROUTINE PICK-ONE (TABL "AUX" LENGTH CNT RND S MSG)
       <SET LENGTH <GET .TABL 0>>
       <SET CNT <GET .TABL 1>>
       <REPEAT ()
               <PUT ,TEMPTABLE .S <GET .TABL <+ .CNT .S>>  >
               <SET S <+ .S 1>>
               ;<PROG () <TELL "IN LOOP: S IS NOW: "> <PRINTN .S> <TELL CR>>
               <COND (<G? <+ .S .CNT> .LENGTH> <RETURN>)>
       >
       ;<PROG () <TELL "S IS CURRENTLY: "> <PRINTN .S> <TELL CR>>
       <SET RND <- <RANDOM .S> 1>>
       ;<PROG () <TELL "RND IS CURRENTLY: "> <PRINTN .RND> <TELL CR>>
       <SET MSG <GET ,TEMPTABLE .RND>>
       <PUT .TABL <+ .CNT .RND> <GET .TABL .CNT> >
       <PUT .TABL .CNT .MSG >
       <SET CNT <+ 1 .CNT>>
       <COND (<G? .CNT .LENGTH> <SET CNT 2>)>
       <PUT .TABL 1 .CNT>
       <RETURN .MSG>
>


<ROUTINE PICK-ONE-R (TABL "AUX" MSG RND)
      <SET RND <RANDOM <GET .TABL 0>>>
      <SET MSG <GET .TABL .RND>>
      <RETURN .MSG>>
      

<ROUTINE WAIT-TURNS (TURNS "AUX" T INTERRUPT ENDACT BACKUP-WAIT)
	<SET .BACKUP-WAIT ,STANDARD-WAIT>
	<SET ,STANDARD-WAIT .TURNS>
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
    					<SET ,STANDARD-WAIT .BACKUP-WAIT>
    					;"To keep clocker from running again after the WAITED turns"
    					<SET ,AGAINCALL 1>
    					<RETURN>
    		  )
        >
    >>
    
<ROUTINE JIGS-UP (TEXT "AUX" RESP (Y 0) (X <>) R)
	<TELL .TEXT CR>
	 <TELL "***** The game is over *****" CR CR>
	 <VERSION?
			(ZIP <>)
			(EZIP <>)
			(ELSE <SET X T>)>
     <COND (<AND .X>
     			<PRINTI "Would you like to RESTART, UNDO, RESTORE, or QUIT? > ">)
     		(T
     			<PRINTI "Would you like to RESTART, RESTORE or QUIT? > ">)>
	 <REPEAT ()
		<PUTB ,READBUF 0 <- ,READBUF-SIZE 2>>
		<VERSION? (ZIP <>) (ELSE <PUTB ,READBUF 1 0>)>
		<READ ,READBUF ,LEXBUF>
			<COND (<EQUAL? <GET ,LEXBUF 1> ,W?RESTART>
		 			<SET Y 1>
		 			<RETURN>)
		 	   (<EQUAL? <GET ,LEXBUF 1>  ,W?RESTORE>
		 			<SET Y 2>
		 			<RETURN>)
		 	  (<EQUAL? <GET ,LEXBUF 1> ,W?QUIT>
		 			<SET Y 3>
		 			<RETURN>)
			  (<EQUAL? <GET ,LEXBUF 1>  ,W?UNDO>
		 			<SET Y 4>
		 			<RETURN>)
		 	   (T
		 	   		<COND (<AND .X>
						<TELL CR "(Please type RESTART, UNDO, RESTORE or QUIT)  >" >)
						     (ELSE
							<TELL CR "(Please type RESTART, RESTORE or QUIT) > " > )>
					 )>>
	<COND (<=? .Y 1>
			  <RESTART>)
		  (<=? .Y 2>
		  	  <COND (<AND .X>
		  	  			<SET R <RESTORE>>
		  	  			;"Workaround for restore failing duirng JIGS-UP, otherwise game will continue, even though player is 'dead'"
		  	  			<COND (<NOT .R> <TELL "Restore failed - restarting instead." CR> 
		  	  							<TELL "Press any key >">
		  	  							<GETONECHAR>
		  	  							<RESTART>)>)
		  	  		(T
		  	  			<JIGS-UP "">)>)
		  (<=? .Y 3>
		  	  <TELL "Thanks for playing." CR>
			  <QUIT>)
		 (<=? .Y 4>
		  	   <COND (<AND .X>
		  	  			<SET R <V-UNDO>>
		  	  			;"Workaround for undo failing duirng JIGS-UP, otherwise game will continue, even though player is 'dead'"
		  	  			<COND (<NOT .R> <TELL "Undo failed - restarting instead." CR> 
		  	  							<TELL "Press any key >">
		  	  							<GETONECHAR>
		  	  							<RESTART>)>)
		  	  		(T
		  	  			<JIGS-UP "">)>)
			  
			  
			  
			  >
	
>      
	
<ROUTINE ROB (VICTIM "OPT" DEST "AUX" N I)
	 <SET I <FIRST? .VICTIM>>
	 <REPEAT ()
		 <COND (<NOT .I>
			<RETURN>)>
		 <SET N <NEXT? .I>>
		 <COND (<AND <FSET? .I ,WORNBIT>
		 			 <NOT <FSET? .DEST ,PERSONBIT>>>
						<FCLEAR .I ,WORNBIT>)>
		 <COND (<NOT .DEST>
		 			<REMOVE .I>)
		 	   (ELSE
		 	   		<MOVE .I .DEST>)>
		 <SET I .N>>>

<ROUTINE YES? ("AUX" RESP)
	 <PRINTI " (y/n) >">
	 <REPEAT ()
		 <SET RESP <GETONECHAR>>
		 <CRLF>
		 <COND (<EQUAL? .RESP !\Y !\y>
		 			<RTRUE>)
		 	   (<EQUAL? .RESP !\N !\n>
		 			<RFALSE>)
		 	   (T
		 	   		<TELL "(Please type y or n) >" >)>>>

<VERSION?
	(ZIP
		<ROUTINE GETONECHAR ()
			<PUTB ,READBUF 0 <- ,READBUF-SIZE 2>>
			<READ ,READBUF ,LEXBUF>
			<GETB ,READBUF 1>>)
	(ELSE
		<ROUTINE GETONECHAR ()
				 <BUFOUT <>>
				 <BUFOUT T>
				 <INPUT 1>>)>			
		 
<ROUTINE VISIBLE? (OBJ "AUX" P M (T 0))
	<SET P <LOC .OBJ>>
	<SET M <META-LOC .OBJ>>
	<COND (<NOT <=? .M ,HERE>>
				<COND (<OR
					<AND <=? .P ,LOCAL-GLOBALS>
					<GLOBAL-IN? .OBJ ,HERE>>
					<=? .P ,GLOBAL-OBJECTS>>
									<RTRUE>)
					      					(ELSE <RFALSE>)>)
		     (ELSE
				;<TELL "The meta-loc = HERE and the LOC is " D .P CR>
				<REPEAT ()
					<COND 
					 	(<AND 
							<FSET? .P ,CONTBIT>
							<NOT <FSET? .P ,SURFACEBIT>>
							<NOT <FSET? .P ,TRANSBIT>>
							<NOT <FSET? .P ,OPENBIT>>>
								;<TELL D .P " is a non-transparent container that is closed." CR>
								<SET T 0>
								<RETURN>)
						(<OR
							<=? .P ,HERE>
							<=? .P ,WINNER>>
								;<TELL D .P " is either = HERE or the player." CR>
								<SET T 1>
								<RETURN>)
						(ELSE
							<SET P <LOC .P>>)>>
				<COND (<=? .T 1>
						<RTRUE>)
					  (ELSE
						<RFALSE>)>							
		)>		
>

<ROUTINE ACCESSIBLE? (OBJ "AUX" L M (T 0))
       ;"currently GLOBALs and LOCAL-GLOBALS return false since they are non-interactive scenery."
       <SET L <LOC .OBJ>>
       <SET M <META-LOC .OBJ>>
       <COND (<NOT <=? .M ,HERE>>
				;<TELL "Object not in room" CR>
				<RFALSE>)>
       <REPEAT ()
                                      ;<TELL "In accessible repeat loop, L is " D .L CR>
				<COND
                                               (<AND
                                                       <FSET? .L ,CONTBIT>
                                                       <NOT <FSET? .L ,OPENBIT>>
					        <NOT <FSET? .L ,SURFACEBIT>>>
                                                                       ;<TELL D .L " is a closed container." CR>
                                                                       <SET T 0>
                                                                       <RETURN>)
					(<OR
                                                       <=? .L ,HERE>
                                                       <=? .L ,WINNER>>
                                                               ;<TELL D .L " is either = HERE or the player." CR>
                                                               <SET T 1>
                                                               <RETURN>)
                                               (ELSE
                                                       <SET L <LOC .L>>)>>
                               <COND (<=? .T 1>
                                               <RTRUE>)
                                         (ELSE
                                               <RFALSE>)>
>

<ROUTINE HELD? (OBJ "OPT" (HLDR <>) "AUX" TH (T 0))
	   <OR .HLDR <SET HLDR ,WINNER>>
	   <REPEAT ()
                                      <COND 
					(<=? <LOC .OBJ> .HLDR>
						<SET T 1>
						<RETURN>)
					(<NOT <AND .OBJ>>
						<SET T 0>
						<RETURN>)
					(ELSE
						<SET OBJ <LOC .OBJ>>)>>
                               <COND (<=? .T 1>
                                               <RTRUE>)
                                         (ELSE
                                               <RFALSE>)>
>

<ROUTINE META-LOC (OBJ "AUX" P (T 0))
	<SET P <LOC .OBJ>>
	<COND (<IN? .P ,ROOMS>
				<RETURN .P>)>
	<REPEAT ()
		;<TELL "In META-LOC repeat -- P is " D .P CR>
		<COND (<OR
					<FSET? .P ,PERSONBIT>
					 <FSET? .P ,CONTBIT>
					 <=? .P ,LOCAL-GLOBALS>
					 <=? .P ,GLOBAL-OBJECTS >
					 <=? .P ,GENERIC-OBJECTS>>
							<SET P <LOC .P>>)>
		<COND  (<IN? .P ,ROOMS>
					<SET T 1>
					<RETURN>)
			       (<NOT .P>
					<SET T 0>
					<RETURN>)>>
		<COND (<=? T 1>
					<RETURN .P>)
			      (ELSE
					<RFALSE>)>					
>

<ROUTINE NOW-DARK ()
	    <COND (<AND <NOT <FSET? ,HERE ,LIGHTBIT>>
				     <NOT <SEARCH-FOR-LIGHT>>>
						<TELL "You are plunged into darkness." CR>
						)>>
	
	
<ROUTINE NOW-LIT ()
	    <COND (<AND <NOT <FSET? ,HERE ,LIGHTBIT>>
				     <NOT <SEARCH-FOR-LIGHT>>>
						<TELL "You can see your surroundings now." CR CR>
						<SETG NLITSET 1>
						<V-LOOK>)>>
      
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
    		  						<SETG ,IQ-LENGTH <- ,IQ-LENGTH 2>>
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
    							<SET .C 1>
    						   )>
    	      )
    	>
    							
     >
     <COND 
    	   (<AND .FIRED> <RTRUE>)
    	   (ELSE <RFALSE>)
     >     
>
    		

"Verbs"

<DIRECTIONS NORTH SOUTH EAST WEST NORTHEAST NORTHWEST SOUTHEAST SOUTHWEST IN OUT UP DOWN>

<SYNONYM NORTH N>
<SYNONYM SOUTH S>
<SYNONYM EAST E>
<SYNONYM WEST W>
<SYNONYM NORTHEAST NE>
<SYNONYM NORTHWEST NW>
<SYNONYM SOUTHEAST SE>
<SYNONYM SOUTHWEST SW>
<SYNONYM IN ENTER>
<SYNONYM OUT EXIT>
<SYNONYM UP U>
<SYNONYM DOWN D>

<SYNTAX LOOK = V-LOOK>
<VERB-SYNONYM LOOK L>

<SYNTAX WALK OBJECT = V-WALK>
<VERB-SYNONYM WALK GO>

<SYNTAX QUIT = V-QUIT>

<SYNTAX TAKE OBJECT (FIND TAKEBIT) (ON-GROUND IN-ROOM) = V-TAKE>
<VERB-SYNONYM TAKE GRAB>
<SYNTAX PICK UP OBJECT (FIND TAKEBIT) (ON-GROUND IN-ROOM) = V-TAKE>
<SYNTAX GET OBJECT (FIND TAKEBIT) (ON-GROUND IN-ROOM) = V-TAKE>

<SYNTAX DROP OBJECT (HAVE HELD CARRIED) = V-DROP>
;<SYNTAX PUT DOWN OBJECT (HAVE HELD CARRIED) = V-DROP>	;"too many parts of speech for DOWN"

<SYNTAX EXAMINE OBJECT = V-EXAMINE>
<SYNTAX LOOK AT OBJECT = V-EXAMINE>
<VERB-SYNONYM EXAMINE X>

<SYNTAX WEAR OBJECT (FIND WEARBIT) (HAVE TAKE) = V-WEAR>
<VERB-SYNONYM WEAR DON>
<SYNTAX PUT ON OBJECT (FIND WEARBIT) (HAVE TAKE) = V-WEAR>

<SYNTAX UNWEAR OBJECT (FIND WORNBIT) (HAVE HELD CARRIED) = V-UNWEAR>
<VERB-SYNONYM UNWEAR DOFF>
<SYNTAX TAKE OFF OBJECT (FIND WORNBIT) (HAVE HELD CARRIED) = V-UNWEAR>

<SYNTAX PUT OBJECT (HAVE HELD CARRIED) ON OBJECT (FIND SURFACEBIT) = V-PUT-ON>
<SYNTAX PUT UP OBJECT (HAVE HELD CARRIED) ON OBJECT (FIND SURFACEBIT) = V-PUT-ON>
<VERB-SYNONYM PUT HANG PLACE>

<SYNTAX PUT OBJECT (HAVE HELD CARRIED) IN OBJECT (FIND CONTBIT) = V-PUT-IN>
<VERB-SYNONYM PUT PLACE INSERT>

<SYNTAX INVENTORY = V-INVENTORY>
<VERB-SYNONYM INVENTORY I>

<SYNTAX CONTEMPLATE OBJECT = V-THINK-ABOUT>
<VERB-SYNONYM CONTEMPLATE CONSIDER>
<SYNTAX THINK ABOUT OBJECT = V-THINK-ABOUT>

<SYNTAX OPEN OBJECT = V-OPEN>

<SYNTAX CLOSE OBJECT = V-CLOSE>
<VERB-SYNONYM SHUT>

<SYNTAX TURN ON OBJECT = V-TURN-ON>
<VERB-SYNONYM FLIP SWITCH>

<SYNTAX TURN OFF OBJECT = V-TURN-OFF>
<VERB-SYNONYM FLIP SWITCH>

<SYNTAX FLIP OBJECT = V-FLIP>
<VERB-SYNONYM TOGGLE>

<SYNTAX WAIT = V-WAIT>
<VERB-SYNONYM WAIT Z>

<SYNTAX AGAIN = V-AGAIN>
<VERB-SYNONYM AGAIN G>

<SYNTAX READ OBJECT = V-READ>
<VERB-SYNONYM READ PERUSE>

<SYNTAX EAT OBJECT = V-EAT>
<VERB-SYNONYM EAT SCARF DEVOUR GULP CHEW>

<SYNTAX PUSH OBJECT = V-PUSH>
<VERB-SYNONYM PUSH SHOVE>

<SYNTAX VERSION = V-VERSION>

<SYNTAX UNDO = V-UNDO>
<SYNTAX SAVE = V-SAVE>
<SYNTAX RESTORE = V-RESTORE>
<SYNTAX RESTART = V-RESTART>

;"debugging verbs - remove"
<IF-DEBUG
	<SYNTAX DROB OBJECT = V-DROB>
	<SYNTAX DSEND OBJECT TO OBJECT = V-DSEND>
	<SYNTAX DOBJL OBJECT = V-DOBJL>
	;<SYNTAX DVIS = V-DVIS>
	;<SYNTAX DMETALOC OBJECT = V-DMETALOC>
	;<SYNTAX DACCESS OBJECT = V-DACCESS>
	;<SYNTAX DHELD OBJECT IN OBJECT = V-DHELD>
	;<SYNTAX DHELDP OBJECT = V-DHELDP>
	;<SYNTAX DLIGHT = V-DLIGHT>
	<SYNTAX DCONT = V-DCONT>
	<SYNTAX DTURN = V-DTURN>
>

<CONSTANT M-BEG 1>
<CONSTANT M-END 2>
<CONSTANT M-ENTER 3>
<CONSTANT M-LOOK 4>

<ROUTINE V-LOOK ("AUX" P F N S)
    <COND
        ;"check for light, unless running LOOK from NOW-LIT (which sets NLITSET to 1)"
        (<OR <FSET? ,HERE ,LIGHTBIT>
		<AND <SEARCH-FOR-LIGHT>>
	          <EQUAL? ,NLITSET 1>>

        	 ;"print the room's real name"
        	 <TELL D ,HERE CR>
            ;"either print the room's LDESC or call its ACTION with M-LOOK"
            <COND
                (<SET P <GETP ,HERE ,P?LDESC>>
                    <TELL .P CR>)
                (ELSE
                    <APPLY <GETP ,HERE ,P?ACTION> ,M-LOOK>)>
            ;"describe contents"
            ;"do any FDESC objects"
		    <OBJECTLOOP I ,HERE
		    	<COND (<AND <NOT <FSET? .I ,TOUCHBIT>> <SET P <GETP .I ,P?FDESC>>>
		                                    <TELL .P CR>)>>
		    ;"use N add up all non fdesc, ndescbit, personbit objects in room"
		    <OBJECTLOOP I ,HERE
		    		 <COND
		                    (<NOT <OR <FSET? .I ,NDESCBIT> 
		                    			<==? .I ,WINNER> 
		                    			<FSET? .I ,PERSONBIT> 
		                    			<AND <NOT <FSET? .I ,TOUCHBIT>> <SET P <GETP .I ,P?FDESC>> >>>
		    									
		    									<SET N <+ .N 1>>
		    									<COND (<==? .N 1>
		    												<SET F .I>)
		    										  (<==? .N 2>
		    										  		<SET S .I>)>		
		    				)>>
		    ;"go through the N objects"
		    <COND (<G? .N 0> 
		    	<COND (<FSET? .F ,PLURALBIT>
		    				<TELL "There are">)
		    		  (ELSE <TELL "There is">)>
		    	<COND
		        	(<==? .N 1>
			    		<ARTICLE .F>
		            	<TELL D .F>)
		        	(<==? .N 2>
			    		<ARTICLE .F> 
		            	<TELL D .F " and">
			    		<ARTICLE .S>
		            	<TELL D .S>)
		        	(ELSE
		            	<OBJECTLOOP I ,HERE
		                		 <COND
		                    		(<NOT <OR <FSET? .I ,NDESCBIT> 
		                    				<==? .I ,WINNER> 
		                    				<FSET? .I ,PERSONBIT> 
		                    				<AND <NOT <FSET? .I ,TOUCHBIT>> <SET P <GETP .I ,P?FDESC>> >>>
		                    					<ARTICLE .I> 
		                						<TELL D .I>
		                						<SET N <- .N 1>>
		                						<COND
		                    						(<0? .N>)
		                    						(<==? .N 1> <TELL ", and">)
		                    						(ELSE <TELL ",">)>)>            
		           		>)>
		          <TELL " here." CR>)>
			;"describe visible contents of containers and surfaces"
		 	<OBJECTLOOP I ,HERE
		      			<COND 
		      					(<AND <FSET? .I ,CONTBIT> 
		                              <AND <FIRST? .I>>
		                        	  <OR <FSET? .I ,SURFACEBIT>
						    			  <FSET? .I ,OPENBIT>>
						         >
		                                	<DESCRIBE-CONTENTS .I>)
		                >
		   	>
		   	;"Re-use N to add up NPCs"
		    <SET N 0>
		    <OBJECTLOOP I ,HERE
		    		 <COND
		                    (<AND <FSET? .I ,PERSONBIT>
		                    	  <NOT <==? .I ,WINNER>>> 
		    							<SET N <+ .N 1>>
		    							<COND (<==? .N 1>
		    										<SET F .I>)
		    							    	(<==? .N 2>
		    										<SET S .I>)>		
		    				)>>
		   ;"go through the N NPCs"
		    <COND (<G? .N 0>
		    	;<TELL CR>
		    	<COND
		        	(<==? .N 1>
		            	<TELL D .F>
		            	<TELL " is">)
		        	(<==? .N 2>
		            	<TELL D .F " and ">
		            	<TELL D .S>
		           		<TELL " are">)
		        	(ELSE
		            	<OBJECTLOOP I ,HERE
		                		 <COND
		                    		(<AND <FSET? .I ,PERSONBIT>
		                    	  		  <NOT <==? .I ,WINNER>>> 
		                						<TELL D .I>
		                						<SET N <- .N 1>>
		                						<COND
		                    						(<0? .N>)
		                    						(<==? .N 1> <TELL ", and ">)
		                    						(ELSE <TELL ",">)>)>            
		           		>
		           		<TELL " are">)>
		          <TELL " here." CR>)>
			<SETG NLITSET 0>
              )
          (ELSE 
                    <TELL "Darkness" CR "It is pitch black. You are likely to be eaten by a grue." CR>)>>
                    
<ROUTINE ARTICLE (OBJ)
	<COND (<FSET? .OBJ ,NARTICLEBIT> <TELL " ">)
              (<FSET? .OBJ ,VOWELBIT> <TELL " an ">)
              (ELSE
		<TELL " a ">)>>

<ROUTINE DESCRIBE-CONTENTS (OBJ)
    <COND (<FSET? .OBJ ,SURFACEBIT> <TELL "On">)
        (ELSE <TELL "In">)>
    <TELL " the " D .OBJ " ">
    <ISARE-LIST .OBJ>
    <TELL "." CR>>
    
<ROUTINE INV-DESCRIBE-CONTENTS (OBJ "AUX" N F)
    <COND (<FSET? .OBJ ,SURFACEBIT> <TELL " (holding">)
        (ELSE <TELL " (containing">)>
    <SET F <FIRST? .OBJ>>
    <COND (<NOT .F>
            <TELL " nothing)"> <RETURN>)>
    <OBJECTLOOP I .OBJ <SET N <+ .N 1>>>
    <COND
        (<==? .N 1>
	    	<ARTICLE .F>
            <TELL D .F>)
        (<==? .N 2>
	    	<ARTICLE .F> 
            <TELL D .F " and">
	   	    <ARTICLE <NEXT? .F>>
            <TELL D <NEXT? .F>>)
    	(ELSE
            <OBJECTLOOP I .OBJ
                <ARTICLE .I> 
                <TELL D .I>
                <SET N <- .N 1>>
                <COND
                    (<0? .N>)
                    (<==? .N 1> <TELL ", and">)
                    (ELSE <TELL ",">)>
             >)
       >
 	<TELL ")">
 >
 

<ROUTINE ISARE-LIST (O "AUX" N F)
    <SET F <FIRST? .O>>
    <COND (<NOT .F>
            <TELL "is nothing"> <RETURN>)>
    <OBJECTLOOP I .O <SET N <+ .N 1>>>
    <COND
        (<==? .N 1>
            <COND (<FSET? .F ,PLURALBIT>
            		<TELL "are">)
            	  (ELSE <TELL "is">)>
	    	<ARTICLE .F>
            <TELL D .F>)
        (<==? .N 2>
            <TELL "are">
	    <ARTICLE .F> 
            <TELL D .F " and">
	    <ARTICLE <NEXT? .F>>
            <TELL D <NEXT? .F>>)
        (ELSE
            <TELL "are">
            <OBJECTLOOP I .O
                <ARTICLE .I> 
                <TELL D .I>
                <SET N <- .N 1>>
                <COND
                    (<0? .N>)
                    (<==? .N 1> <TELL ", and">)
                    (ELSE <TELL ",">)>>)>>

;"Direction properties have a different format on V4+, where object numbers are words."
<VERSION?
    (ZIP
        <CONSTANT UEXIT 1>          ;"size of unconditional exit"
        <CONSTANT NEXIT 2>          ;"size of non-exit"
        <CONSTANT FEXIT 3>          ;"size of function exit"
        <CONSTANT CEXIT 4>          ;"size of conditional exit"
        <CONSTANT DEXIT 5>          ;"size of door exit"

        <CONSTANT EXIT-RM 0>        ;GET/B
        <CONSTANT NEXIT-MSG 0>      ;GET
        <CONSTANT FEXIT-RTN 0>      ;GET
        <CONSTANT CEXIT-VAR 1>      ;GETB
        <CONSTANT CEXIT-MSG 1>      ;GET
        <CONSTANT DEXIT-OBJ 1>      ;GET/B
        <CONSTANT DEXIT-MSG 1>      ;GET)
    (T
        <CONSTANT UEXIT 2>
        <CONSTANT NEXIT 3>
        <CONSTANT FEXIT 4>
        <CONSTANT CEXIT 5>
        <CONSTANT DEXIT 6>

        <CONSTANT EXIT-RM 0>
        <CONSTANT NEXIT-MSG 0>
        <CONSTANT FEXIT-RTN 0>
        <CONSTANT CEXIT-VAR 4>
        <CONSTANT CEXIT-MSG 1>
        <CONSTANT DEXIT-OBJ 1>
        <CONSTANT DEXIT-MSG 2>)>

<DEFMAC META-VERB? ()
    '<OR <VERB? QUIT VERSION WAIT> <VERB? SAVE RESTORE INVENTORY> <VERB? ;DLIGHT UNDO ;DSEND>>>

<ROUTINE V-WALK ("AUX" PT PTS RM)
    <COND (<NOT ,PRSO-DIR>
            <PRINTR "You must give a direction to walk in.">)
        (<0? <SET PT <GETPT ,HERE ,PRSO>>>
            <PRINTR "You can't go that way.">)
        (<==? <SET PTS <PTSIZE .PT>> ,UEXIT>
            <SET RM <GET/B .PT ,EXIT-RM>>)
        (<==? .PTS ,NEXIT>
            <PRINT <GET .PT ,NEXIT-MSG>>
            <CRLF>
            <RTRUE>)
        (<==? .PTS ,FEXIT>
            <OR
                <SET RM <APPLY <GET .PT ,FEXIT-RTN>>>
                <RTRUE>>)
        (<==? .PTS ,CEXIT>
            <COND (<VALUE <GETB .PT ,CEXIT-VAR>>
                    <SET RM <GET/B .PT ,EXIT-RM>>)
                (<SET RM <GET .PT ,CEXIT-MSG>>
                    <PRINT .RM>
                    <CRLF>
                    <RTRUE>)
                (ELSE
                    <PRINTR "You can't go that way.">)>)
        (<==? .PTS ,DEXIT>
            <PRINTR "Not implemented.">)
        (ELSE
            <TELL "Broken exit (" N .PTS ")." CR>
            <RTRUE>)>
    <GOTO .RM>
    ;<V-LOOK>>

<ROUTINE V-QUIT ()
    <TELL "Are you sure you want to quit?" CR>
    <COND (<YES?>
    			<TELL "Thanks for playing." CR>
    			<QUIT>)
    	  (ELSE
    	  		<TELL "OK - not quitting." CR>)>>

<ROUTINE V-EXAMINE ("AUX" P N)
    <SET N 0>
    <COND (<SET P <GETP ,PRSO P?LDESC>>
    			<TELL .P CR>
    			<SET N 1>)>
    <COND (<SET P <GETP ,PRSO P?TEXT>>
    			<TELL .P CR>
    			<SET N 1>)>
    <COND
        (<AND <FSET? ,PRSO ,OPENABLE> 
        	  <NOT <FSET? ,PRSO ,OPENBIT>>
         >
			  	<TELL "The " D ,PRSO " is closed." CR>
			  	<COND (<AND <FSET? ,PRSO ,TRANSBIT> <FIRST? ,PRSO>> <DESCRIBE-CONTENTS ,PRSO>)>
			  	<SET N 1>)
	    (<AND <FSET? ,PRSO ,OPENABLE> 
        	  <FSET? ,PRSO ,OPENBIT>
         >
			  	<TELL "The " D ,PRSO " is open. ">
			  	<DESCRIBE-CONTENTS ,PRSO>
			  	<SET N 1>)
		(<AND 	<AND <FSET? ,PRSO ,CONTBIT> 
                   	 <AND <FIRST? ,PRSO>>
              	>
              	<OR <FSET? ,PRSO ,TRANSBIT>
              	    <FSET? ,PRSO ,SURFACEBIT>
          		  	<AND <FSET? ,PRSO ,OPENBIT>
               			 <FSET? ,PRSO ,OPENABLE>
          		  	>
          		  	<AND <FSET? ,PRSO ,CONTBIT>
               		 	 <NOT <FSET? ,PRSO ,OPENABLE>>
          			>
        	 	>
     >
                <DESCRIBE-CONTENTS ,PRSO>
                <SET N 1>)
     >
     <COND (<0? .N>
              <TELL "You see nothing special about the " D ,PRSO "." CR>)
    >
>


<ROUTINE V-INVENTORY ()
  ;"check for light first"
  <COND
     (<OR <FSET? ,HERE ,LIGHTBIT>
           <AND <SEARCH-FOR-LIGHT>>>
				<COND
		        (<FIRST? ,WINNER>
		            <TELL "You are carrying:" CR>
		            <OBJECTLOOP I ,WINNER
		                <TELL "   " D .I>
		                <AND <FSET? .I ,WORNBIT> <TELL " (worn)">>
		                <AND <FSET? .I ,LIGHTBIT> <TELL " (providing light)">>
						<COND (<FSET? .I ,CONTBIT>
		                	   		<COND (<AND <FSET? .I ,OPENABLE>
		        	  			  				<FSET? .I ,OPENBIT>
		        	  			  	 	    >
					  								<TELL " (open) ">
					  								<INV-DESCRIBE-CONTENTS .I>
					  				  	  )
					  				  	  (<AND    
		                						<FSET? .I ,OPENABLE>
		        	  			  				<NOT <FSET? .I ,OPENBIT>>
		        	  			           >
					  								<TELL " (closed)">
					  		  			  )
					  		  			  (<AND 
					  		    	        	;<FIRST? .I>
		                          	   			<OR <FSET? .I ,SURFACEBIT>
						              	   			<FSET? .I ,OPENBIT>
						              	   			<AND <FSET? .I ,CONTBIT>
						              	   				 <NOT <FSET? .I ,OPENABLE>>
						              	   			>
					              	   			>
				             		     	>
		                            				<INV-DESCRIBE-CONTENTS .I>
		                            	  )
		                    		>
		                )	
		             	>
		                <CRLF>>)
		        (ELSE
		            <TELL "You are empty-handed." CR>)>)
	(ELSE <TELL "It is too dark to see what you're carrying." CR>)>
>

<ROUTINE V-TAKE ("AUX" HOLDER S X)
    <COND
        (<FSET? ,PRSO ,PERSONBIT>
            <TELL "I don't think " D ,PRSO " would appreciate that." CR>)
        (<NOT <FSET? ,PRSO ,TAKEBIT>>
            <PRINTR "That's not something you can pick up.">)
        (<IN? ,PRSO ,WINNER>
            <PRINTR "You already have that.">)
        (ELSE
            <COND
                (<FSET? ,PRSO ,WEARBIT>
                    <TELL "You wear the " D ,PRSO "." CR>
                    <FSET ,PRSO ,WORNBIT>
                    <MOVE ,PRSO ,WINNER>
                    <FSET ,PRSO ,TOUCHBIT>)
                (ELSE
                	;"See if picked up object is being taken from a container"
                	<SET HOLDER <TAKE-CONT-SEARCH ,HERE>>
                	;<TELL "HOLDER is currently " D .HOLDER CR>
               		<COND (<AND .HOLDER>
    							<COND (<FSET? .HOLDER ,SURFACEBIT>
    										<TELL "You pick up the " D ,PRSO "." CR>
    										<FSET ,PRSO ,TOUCHBIT>
    										<MOVE ,PRSO ,WINNER>)
    								   (<FSET? .HOLDER ,OPENBIT>
    										<TELL "You reach in the " D .HOLDER " and take the " D ,PRSO "." CR>
    										<FSET ,PRSO ,TOUCHBIT>
    										<MOVE ,PRSO ,WINNER>)
    								  (ELSE
    										<TELL "The enclosing " D .HOLDER " prevents you from taking the " D ,PRSO "." CR>)
    							>)
                    	  (ELSE <TELL "You pick up the " D ,PRSO "." CR>
                    	  <FSET ,PRSO ,TOUCHBIT>
                    	  <MOVE ,PRSO ,WINNER>)
                    >
                 )>)>>
                 
                 
 <ROUTINE TAKE-CONT-SEARCH (A "AUX" H)  	
 			<OBJECTLOOP I .A
                		;<TELL "Looping to check containers.  I is currently " D .I CR>
                		<COND (<OR <FSET? .I ,CONTBIT>
                				   <==? .I ,WINNER>
                			   >
                						;<TELL "Found container " D .I CR>
                						<COND (<IN? ,PRSO .I>
                								;<TELL "PRSO is in I, setting HOLDER" CR>
                								<SET H .I>
                								<RETURN>)
                						  	  (ELSE
                						  		<SET H <TAKE-CONT-SEARCH .I>>
                						  		<AND .H <RETURN>>
                						  	  )
                						>
                			  )
                		>
   			>
   			<AND .H <RETURN .H>>
> 

           

<ROUTINE V-DROP ()
    <COND
        (<NOT <IN? ,PRSO ,WINNER>>
            <PRINTR "You don't have that.">)
        (ELSE
            <MOVE ,PRSO ,HERE>
            <FSET ,PRSO ,TOUCHBIT>
            <FCLEAR ,PRSO ,WORNBIT>
            <TELL "You drop the " D ,PRSO "." CR>)>>

<ROUTINE INDIRECTLY-IN? (OBJ CONT)
	<REPEAT ()
		<COND (<0? .OBJ> <RFALSE>)
			(<EQUAL? <SET OBJ <LOC .OBJ>> .CONT> <RTRUE>)>>>

<ROUTINE V-PUT-ON ("AUX" S CCAP CSIZE X W B)
    <COND
        (<FSET? ,PRSI ,PERSONBIT>
            <TELL "I don't think " D ,PRSI " would appreciate that." CR>)
        (<NOT <AND <FSET? ,PRSI ,CONTBIT> <FSET? ,PRSI ,SURFACEBIT>>>
            <TELL "The " D ,PRSI> <COND (<FSET? ,PRSI ,PLURALBIT> <TELL " aren't">) (ELSE <TELL " isn't">)> <TELL " something you can put things on." CR>)
        (<NOT <IN? ,PRSO ,WINNER>>
            <PRINTR "You don't have that.">)
        (<OR <EQUAL? ,PRSO ,PRSI> <INDIRECTLY-IN? ,PRSI ,PRSO>>
            <PRINTR "You can't put something on itself.">)
        (ELSE
            ;"Need to check if size property exists for DO - using GETPT to see if property has an actual address"
            <COND (<SET X <GETPT ,PRSO ,P?SIZE>>
            			<SET S <GETP ,PRSO ,P?SIZE>>
    					;<TELL D ,PRSO " has a size prop of " N .S CR>)
    			  (ELSE 
    			  		;<TELL D ,PRSO " has no size prop - will be assigned default size of 5" CR>
    			  		<SET S 5>)>
            <COND (<SET X <GETPT ,PRSI ,P?CAPACITY>>
    					<SET CCAP <GETP ,PRSI ,P?CAPACITY>>
    				    ;<TELL D ,PRSI " has a capacity prop of " N .CCAP CR>)
    			  (ELSE 
    			  		;<TELL D ,PRSI " has no capacity prop.  Will take endless amount of objects as long as each object is size 5 or under" CR>
    			  		<SET CCAP 5>
    			  		;"set bottomless flag"
    			  		<SET B 1>
    			  )>
    	   <COND (<SET X <GETPT ,PRSI ,P?SIZE>>
    					<SET CSIZE <GETP ,PRSI ,P?SIZE>>
    				    ;<TELL D ,PRSI " has a size prop of " N .CSIZE CR>)
    			  (ELSE <SET CSIZE 5>)>
    		;<TELL D ,PRSO "size is " N .S ", " D ,PRSI " size is " N .CSIZE ", capacity " >
    		    		;<COND (<0? .B>
    				 		;<TELL N .CCAP CR>)
    			  		;(ELSE <TELL "infinite" CR>)>
    		<COND (<OR <G? .S .CCAP>
    				   <G? .S .CSIZE>>
    						<TELL "That won't fit on the " D ,PRSI "." CR>
    						<RETURN>)>
    		<COND (<0? .B>
    					;"Determine weight of contents of IO"
    					<SET W <CONTENTS-WEIGHT ,PRSI>>
    					;<TELL "Back from Contents-weight loop" CR>
    					<SET X <+ .W .S>>
    					<COND (<G? .X .CCAP> 
    						<TELL "There's not enough room on the " D ,PRSI "." CR>
    						;<TELL D ,PRSO " of size " N .S " can't fit, since current weight of " D ,PRSI "'s contents is " N .W " and " D ,PRSI "'s capacity is " N .CCAP CR>
    						<RETURN>)>
    				   ; <TELL D ,PRSO " of size " N .S " can fit, since current weight of of " D ,PRSI "'s contents is " N .W " and " D ,PRSI "'s capacity is " N .CCAP CR>
    			  )>
    		<MOVE ,PRSO ,PRSI>
            <FSET ,PRSO ,TOUCHBIT>
            <FCLEAR ,PRSO ,WORNBIT>
            <TELL "You put the " D ,PRSO " on the " D ,PRSI "." CR>
 	)>>
            
<ROUTINE V-PUT-IN ("AUX" S CCAP CSIZE X W B)
    ;<TELL "In the PUT-IN routine" CR>
    <COND 
    	(<FSET? ,PRSI ,PERSONBIT>
            	<TELL "I don't think " D ,PRSI " would appreciate that." CR>)
        (<OR <NOT <FSET? ,PRSI ,CONTBIT>>
    		 <FSET? ,PRSI ,SURFACEBIT>
         >
            	<TELL "The " D ,PRSI> <COND (<FSET? ,PRSI ,PLURALBIT> <TELL " aren't">) (ELSE <TELL " isn't">)> <TELL " something you can put things in." CR>)
	(<AND <NOT <FSET? ,PRSI ,OPENBIT>>
			      <FSET? ,PRSI ,OPENABLE>
         >
            	<TELL "The " D ,PRSI " is closed." CR>)
	;"always closed case"
	(<AND <NOT <FSET? ,PRSI ,OPENBIT>>
		   <FSET? ,PRSI ,CONTBIT>
         >
            	<TELL "You see no way to put things into the " D ,PRSI  "." CR>)
        (<NOT <IN? ,PRSO ,WINNER>>
            <PRINTR "You aren't holding that.">)
        (<OR <EQUAL? ,PRSO ,PRSI> <INDIRECTLY-IN? ,PRSI ,PRSO>>
            <PRINTR "You can't put something in itself.">)
        (ELSE
            ;"Need to check if size property exists for DO - using GETPT to see if property has an actual address"
            <COND (<SET X <GETPT ,PRSO ,P?SIZE>>
            			<SET S <GETP ,PRSO ,P?SIZE>>
    					;<TELL D ,PRSO " has a size prop of " N .S CR>)
    			  (ELSE 
    			  		;<TELL D ,PRSO " has no size prop - will be assigned default size of 5" CR>
    			  		<SET S 5>)>
            <COND (<SET X <GETPT ,PRSI ,P?CAPACITY>>
    					<SET CCAP <GETP ,PRSI ,P?CAPACITY>>
    				    <COND (<AND ,DBCONT> <TELL D ,PRSI " has a capacity prop of " N .CCAP CR>)>)
    			  (ELSE 
    			  		<COND (<AND ,DBCONT> <TELL D ,PRSI " has no capacity prop.  Will take endless amount of objects as long as each object is size 5 or under" CR>)>
    			  		<SET CCAP 5>
    			  		;"set bottomless flag"
    			  		<SET B 1>
    			  )>
    	   <COND (<SET X <GETPT ,PRSI ,P?SIZE>>
    					<SET CSIZE <GETP ,PRSI ,P?SIZE>>)
    			  (ELSE <SET CSIZE 5>)>
    		;<COND (<AND ,DBCONT> <TELL D ,PRSI " has a size of " N .CSIZE CR>)>
		<COND (<AND ,DBCONT> <TELL D ,PRSO "size is " N .S ", " D ,PRSI " size is " N .CSIZE CR >)>
    		    		;<COND (<0? .B>
    				 		;<TELL N .CCAP CR>)
    			  		;(ELSE <TELL "infinite" CR>)>
    		<COND (<OR <G? .S .CCAP>
    				   <G? .S .CSIZE>>
    						<TELL "That won't fit in the " D ,PRSI "." CR>
    						<RETURN>)>
    		<COND (<0? .B>
    					;"Determine weight of contents of IO"
    					<SET W <CONTENTS-WEIGHT ,PRSI>>
    					;<TELL "Back from Contents-weight loop" CR>
    					<SET X <+ .W .S>>
    					<COND (<G? .X .CCAP> 
    						<TELL "There's not enough room in the " D ,PRSI "." CR>
    						<COND (<AND ,DBCONT> <TELL D ,PRSO " can't fit, since current bulk of " D ,PRSI "'s contents is " N .W " and " D ,PRSI "'s capacity is " N .CCAP CR>)>
    						<RETURN>)>
    				    <COND (<AND ,DBCONT> <TELL D ,PRSO " can fit, since current bulk of " D ,PRSI "'s contents is " N .W " and " D ,PRSI "'s capacity is " N .CCAP CR>)>
    			  )>
    		<MOVE ,PRSO ,PRSI>
            <FSET ,PRSO ,TOUCHBIT>
            <FCLEAR ,PRSO ,WORNBIT>
            <TELL "You put the " D ,PRSO " in the " D ,PRSI "." CR>
 	)>>
            
            
  <ROUTINE CONTENTS-WEIGHT (O "AUX" X W)
    		;"add size of objects inside container - does not recurse through containers withink this container"
    		<OBJECTLOOP I .O
                		;<TELL "Content-weight loop for " D .O ", which contains " D .I CR>
                		<COND (<SET X <GETPT .I ,P?SIZE>>
    								<SET X <GETP .I ,P?SIZE>>
    								<SET W <+ .W .X>>)
    			  		(ELSE <SET W <+ .W 5>>)>
                		;<TELL "Content weight of " D .O " is now " N .W CR>
   			>
   			;<TELL "Total weight of contents of " D .O " is " N .W CR>
   			<AND .W <RETURN .W>>
>
 
 
 <ROUTINE WEIGHT (O "AUX" X W)  	
 			;"Unlike CONTENTS-WEIGHT - drills down through all contents, adding sizes of all objects + contents"
 			;"start with size of container itself"
 			<COND (<SET X <GETPT .O ,P?SIZE>>
    								<SET W <GETP .O ,P?SIZE>>)
    			  		(ELSE <SET W <+ .W 5>>)>
    		;"add size of objects inside container"
    		<OBJECTLOOP I .O
                		<TELL "Looping to set weight.  I is currently " D .I CR>
                		<COND (<SET X <GETPT .I ,P?SIZE>>
    								<SET X <GETP .I ,P?SIZE>>
    								<SET W <+ .W .X>>)
    			  		(ELSE <SET W <+ .W 5>>)>
                		;<TELL "Weight of " D .O " is now " N .W CR>
                		<COND (<OR <FSET? .I ,CONTBIT>
                				   <==? .I ,WINNER>
                			   >
                					;<TELL "Weightloop: found container " D .I CR>
                					<SET X <WEIGHT .I>>
                				    <SET W <+ .W .X>>
                				    ;<TELL "Weightloop-containerloop: Weight of " D .O " is now " N .W CR>
                			  )
                		>
   			>
   			;<TELL "Total weight (its size + contents' size) of " D .O " is " N .W CR>
   			<AND .W <RETURN .W>>
> 



<ROUTINE V-WEAR ()
    <COND (<FSET? ,PRSO ,WEARBIT>
            <PERFORM ,V?TAKE ,PRSO>)
        (ELSE <TELL "You can't wear that." CR>)>>

<ROUTINE V-UNWEAR ()
    <COND (<AND <FSET? ,PRSO ,WORNBIT>
                <IN? ,PRSO ,WINNER>>
                    <PERFORM ,V?DROP ,PRSO>)
        (ELSE <TELL "You aren't wearing that." CR>)>>
        
<ROUTINE V-EAT ()
    <COND (<FSET? ,PRSO ,EDIBLEBIT>
          		;"TO DO: improve this check will a real, drilling-down HELD? routine"
          		<COND (<IN? ,PRSO ,WINNER>
           				<TELL "You devour the " D ,PRSO CR>
          				<REMOVE ,PRSO>
          				)
          			  (ELSE <TELL "You're not holding that." CR>)>)	
          (ELSE <TELL "That's hardly edible." CR>)>>

<ROUTINE V-VERSION ("AUX" (CNT 17))
	 <TELL ,GAME-BANNER "|Release ">
	 <PRINTN <BAND <GET 0 1> *3777*>>
	 <TELL " / Serial number ">
	 <REPEAT ()
		 <COND (<IGRTR? CNT 23>
			<RETURN>)
		       (T
			<PRINTC <GETB 0 .CNT>>)>>
	 <TELL " / " %,ZIL-VERSION " lib " ,ZILLIB-VERSION>
	 <CRLF>>
	 
<ROUTINE V-THINK-ABOUT ()
	<TELL "You contemplate">
	<COND (<OR <FSET? ,PRSO ,NARTICLEBIT> 
			   <FSET? ,PRSO ,PERSONBIT>>
			   		<TELL " ">)
	 	  (ELSE <TELL " the ">)>
	<TELL D ,PRSO " for a bit, but nothing fruitful comes to mind." CR>>
	
<ROUTINE V-OPEN ()
    <COND
        (<FSET? ,PRSO ,PERSONBIT>
            <TELL "I don't think " D ,PRSO " would appreciate that." CR>)
        (<NOT <FSET? ,PRSO ,OPENABLE>>
            <PRINTR "That's not something you can open.">) 			
        (<FSET? ,PRSO ,OPENBIT>
            <PRINTR "It's already open.">)
        (ELSE
            <FSET ,PRSO ,TOUCHBIT>
            <FSET ,PRSO ,OPENBIT>
            <TELL "You open the " D ,PRSO "." CR>
            <DESCRIBE-CONTENTS ,PRSO>
        )>>
        
 <ROUTINE V-CLOSE ()
    <COND
        (<FSET? ,PRSO ,PERSONBIT>
            <TELL "I don't think " D ,PRSO " would appreciate that." CR>)
        (<NOT <FSET? ,PRSO ,OPENABLE>>
            		<PRINTR "That's not something you can close.">)
        ;(<FSET? ,PRSO ,SURFACEBIT>
            <PRINTR "That's not something you can close.">)
        (<NOT <FSET? ,PRSO ,OPENBIT>>
            <PRINTR "It's already closed.">)
        (ELSE
            <FSET ,PRSO ,TOUCHBIT>
            <FCLEAR ,PRSO ,OPENBIT>
            <TELL "You close the " D ,PRSO "." CR>
        )>>

<ROUTINE V-WAIT ("AUX" T INTERRUPT ENDACT)
	<SET T 1>
	<TELL "Time passes." CR>
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
    					<RETURN>
    		  )
        >
    >>
    
<ROUTINE V-AGAIN ()
     <SETG ,AGAINCALL 1>
     <RESTORE-READBUF>
     <RESTORE-LEX>
     ;<TELL "In V-AGAIN - Previous readbuf and lexbuf restored." CR>
     ;<DUMPBUF>
     ;<DUMPLEX>
     <COND (<NOT <EQUAL? <GET ,READBUF 1> 0>>
			<COND (<PARSER>
					;<TELL "Doing PERFORM within V-AGAIN" CR>
					<PERFORM ,PRSA ,PRSO ,PRSI>
					<OR <META-VERB?>  <APPLY <GETP ,HERE ,P?ACTION> ,M-END>>
							<OR <META-VERB?> <CLOCKER>>)>
	  <SETG HERE <LOC ,WINNER>>
	  )
	  (ELSE <TELL "Nothing to repeat." CR>)>
     >

<ROUTINE V-READ ("AUX" T)
     <COND (<NOT <FSET? ,PRSO ,READBIT>>
		<TELL "That's not something you can read." CR>)
     	   (<SET T <GETP ,PRSO ,P?TEXT>>
		<TELL .T CR>)
     	   (<SET T <GETP ,PRSO ,P?TEXT-HELD>>
		<COND (<IN? ,PRSO ,WINNER>
			   <TELL .T CR>)
		      (ELSE
			   <TELL "You must be holding that to be able to read it." CR>)
		>)
	   (ELSE
		<PERFORM ,V?EXAMINE ,PRSO>)
     >
>

<ROUTINE V-TURN-ON ()
    ;<TELL "CURRENTLY IN TURN-ON" CR>
     <COND (<NOT <FSET? ,PRSO ,DEVICEBIT>>
     			<TELL "That's not something you can switch on and off." CR>)
     	   (<FSET? ,PRSO ,ONBIT>
				<TELL "It's already on." CR>)
     	   (ELSE
     	   		 <FSET ,PRSO ,ONBIT>
				 <TELL "You switch on the " D ,PRSO "." CR>)
     >
>



<ROUTINE V-TURN-OFF ()
	;<TELL "CURRENTLY IN TURN-OFF" CR>
     <COND (<NOT <FSET? ,PRSO ,DEVICEBIT>>
     			<TELL "That's not something you can switch on and off." CR>)
     	   (<NOT <FSET? ,PRSO ,ONBIT>>
				<TELL "It's already off." CR>)
     	   (ELSE
     	   		 <FCLEAR ,PRSO ,ONBIT>
				 <TELL "You switch off the " D ,PRSO "." CR>)
     >
>



<ROUTINE V-FLIP ()
     ;<TELL "CURRENTLY IN FLIP" CR>
     <COND (<NOT <FSET? ,PRSO ,DEVICEBIT>>
     			<TELL "That's not something you can switch on and off." CR>)
     	   (<FSET? ,PRSO ,ONBIT>
				<FCLEAR ,PRSO ,ONBIT>
				<TELL "You switch off the " D ,PRSO "." CR>)
     	   (<NOT <FSET? ,PRSO ,ONBIT>>
     	   		 <FSET ,PRSO ,ONBIT>
				 <TELL "You switch on the " D ,PRSO "." CR>)
     >
>

<ROUTINE V-PUSH ()
     <COND
        (<FSET? ,PRSO ,PERSONBIT>
            <TELL "I don't think " D ,PRSO " would appreciate that." CR>)
        (ELSE
        	 <TELL "Pushing the " D ,PRSO " doesn't seem to accomplish much." CR>)>
>


<ROUTINE V-UNDO ("AUX" R)
     <VERSION?
     			(ZIP <TELL "Undo is not available in this version." CR>)
			(EZIP <TELL "Undo is not available in this version." CR>)
			(ELSE
				<COND (<0? ,USAVE>
					<TELL "Cannot undo any further." CR>
					<RETURN>)>
     				<SET R <IRESTORE>>
     				;<TELL "IRESTORE returned " N .R CR>
     				<COND (<EQUAL? .R 0>
     					<TELL "Undo failed." CR>)>
     			)
	>
>

<ROUTINE V-SAVE ("AUX" S)
     ;<TELL "Now in save routine" CR>
	<TELL "Saving..." CR>
	<COND (<SAVE> <V-LOOK>)
		(ELSE <TELL "Save failed." CR>)>
>


<ROUTINE V-RESTORE ("AUX" R)
    ; <TELL "Now in restore routine" CR>
	<COND (<NOT <RESTORE>>
		<TELL "Restore failed." CR>)>
>

<ROUTINE V-RESTART ()
    <TELL "Are you sure you want to restart?" CR>
    <COND (<YES?>
    			<RESTART>)
    	  (ELSE
    	  		<TELL "Restart aborted." CR>)>>


<ROUTINE V-DROB ()
	<TELL "REMOVING CONTENTS OF " D ,PRSO " FROM THE GAME." CR>
	<ROB ,PRSO>
	>
	
<ROUTINE V-DSEND ()
	<TELL "SENDING CONTENTS OF " D ,PRSO " TO " D ,PRSI "." CR>
	<ROB ,PRSO ,PRSI>
	>
	
<ROUTINE V-DOBJL ()
	<OBJECTLOOP I ,PRSO
		<TELL "The objects in " D ,PRSO " include " D .I CR>>
	>

;<ROUTINE V-DVIS ("AUX" P)
	<SET P <VISIBLE? ,BILL>>
	<COND (<NOT .P>
				<TELL "The bill is not visible." CR>)
		      		(ELSE
				<TELL "The bill is visible." CR>)>
	<SET P <VISIBLE? ,GRIME>>
	<COND (<NOT .P>
				<TELL "The grime is not visible." CR>)
		      		(ELSE
				<TELL "The grime is visible." CR>)>
	>
    
;<ROUTINE V-DMETALOC ("AUX" P)
	<SET P <META-LOC ,PRSO>>
	<COND (<NOT .P>
			<TELL "META-LOC returned false." CR>)
		      (ELSE
			<TELL "Meta-Loc of " D ,PRSO " is " D .P CR>)>
	<SET P <META-LOC ,GRIME>>
	<COND (<NOT .P>
			<TELL "META-LOC of grime returned false." CR>)
		      (ELSE
			<TELL "Meta-Loc of grime is " D .P CR>)
			>
	> 
	
;<ROUTINE V-DACCESS ("AUX" P)
	<SET P <ACCESSIBLE? ,PRSO>>
	<COND (<NOT .P>
			<TELL D ,PRSO " is not accessible" CR>)
		      (ELSE
			<TELL D ,PRSO " is accessible" CR>)>
	<SET P <ACCESSIBLE? ,BILL>>
	<COND (<NOT .P>
			<TELL "Bill is not accessible" CR>)
		      (ELSE
			<TELL "Bill is accessible" CR>)>
	> 
	
;<ROUTINE V-DHELD ("AUX" P)
	<SET P <HELD? ,PRSO ,PRSI>>
	<COND (<NOT .P>
			<TELL D ,PRSO " is not held by " D ,PRSI CR>)
		      (ELSE
			<TELL D ,PRSO " is held by " D ,PRSI CR>)>
	<SET P <HELD? ,PRSO ,FOYER>>
	<COND (<NOT .P>
			<TELL D ,PRSO " is not held by the Foyer" CR>)
		      (ELSE
			<TELL D ,PRSO " is held by the Foyer" CR>)>
	> 
	
;<ROUTINE V-DHELDP ("AUX" P)
	<SET P <HELD? ,PRSO>>
	<COND (<NOT .P>
			<TELL D ,PRSO " is not held by the player" CR>)
		      (ELSE
			<TELL D ,PRSO " is held by the player" CR>)>
	<SET P <HELD? ,BILL>>
	<COND (<NOT .P>
			<TELL "Bill is not held by the player" CR>)
		      (ELSE
			<TELL "Bill is held by the player" CR>)>
	>
	
;<ROUTINE V-DLIGHT ()
	<COND (<FSET? ,FLASHLIGHT ,LIGHTBIT>
				<FCLEAR ,FLASHLIGHT ,LIGHTBIT>
				<FCLEAR ,FLASHLIGHT ,ONBIT>
				<TELL "Flashlight is turned off." CR>
				<NOW-DARK>)
		      (ELSE
				<TELL "Flashlight is turned on." CR>
				<NOW-LIT>
				;"always set LIGHTBIT after calling NOW-LIT"
				<FSET ,FLASHLIGHT ,LIGHTBIT>
				<FSET ,FLASHLIGHT ,ONBIT>
				)>
	>
	
<ROUTINE V-DCONT ()
	<COND (<AND ,DBCONT>
				<SET ,DBCONT <>>
				<TELL "Reporting of PUT-IN process with containers turned off." CR>)
		      (ELSE
				<SET ,DBCONT 1>
				<TELL "Reporting of PUT-IN process with containers turned on." CR>)>
>  

<ROUTINE V-DTURN ()
	<COND (<AND ,DTURNS>
				<SET ,DTURNS <>>
				<TELL "Reporting of TURN # turned off." CR>)
		      (ELSE
				<SET ,DTURNS 1>
				<TELL "Reporting of TURN # turned on." CR>)>
>  

	

     

"Objects"

;"This has all the flags, just in case other objects don't define them."
<OBJECT ROOMS
    (FLAGS PERSONBIT TOUCHBIT TAKEBIT WEARBIT WORNBIT LIGHTBIT
           SURFACEBIT CONTBIT NDESCBIT VOWELBIT NARTICLEBIT OPENBIT OPENABLE READBIT DEVICEBIT ONBIT EDIBLEBIT TRANSBIT FEMALEBIT PLURALBIT)>
         
;"This has any special properties, just in case other objects don't define them."
;"I guess all properties should go on this dummy object, just to be safe?"
<OBJECT NULLTHANG
	(SIZE 2)
	(ADJECTIVE NULLTHANG)
	(LDESC <>)
	(FDESC <>)
	(GLOBAL NULLTHANG)
	(TEXT <>)
	(TEXT-HELD <>)
	(CAPACITY 10)>
	
<OBJECT GLOBAL-OBJECTS>

<OBJECT GENERIC-OBJECTS>

<OBJECT LOCAL-GLOBALS>

<OBJECT IT
	(SYNONYM IT)>
	
<OBJECT HIM
	(SYNONYM HIM)>
	
<OBJECT HER
	(SYNONYM HER)>
	
<OBJECT THEM
	(SYNONYM THEM)>

<OBJECT PLAYER
    (DESC "you")
    (SYNONYM ME MYSELF)
    (FLAGS PERSONBIT TOUCHBIT)
    (ACTION PLAYER-R)>

<ROUTINE PLAYER-R ()
    <COND (<N==? ,PLAYER ,PRSO> <RFALSE>)>
    <COND
        (<VERB? EXAMINE> <PRINTR "You look like you're up for an adventure.">)
        (<VERB? TAKE> <PRINTR "That couldn't possibly work.">)>>
