<DEFINE DISTNAME ()
    <MAPF ,STRING
          <FUNCTION (C "AUX" (A <ASCII .C>))
	      <COND (<==? .C !\ > !\-)
		    (<AND <G=? .A %<ASCII !\A>>
		          <L=? .A %<ASCII !\Z>>>
		     <ASCII <+ .A %<- <ASCII !\a> <ASCII !\A>>>>)
		    (ELSE .C)>>
	  ,ZIL-VERSION>>

<PRINC <DISTNAME>>
<CRLF>
