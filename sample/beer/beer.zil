"99 Bottles of Beer sample for ZILF"

<USE "QQ">

<ROUTINE GO () <SING 99>>

<ROUTINE SING (N)
    <REPEAT ()
        <BOTTLES .N>
        <PRINTI " of beer on the wall,|">
        <BOTTLES .N>
        <PRINTI " of beer,|Take one down, pass it around,|">
        <COND
            (<DLESS? N 1> <PRINTR "No more bottles of beer on the wall!">)
            (ELSE <BOTTLES .N> <PRINTI " of beer on the wall!||">)>>>

;"Macro version"
<DEFMAC BOTTLES ('N)
    `<PROG ()
        <PRINTN ~.N>
        <PRINTI " bottle">
        <COND (<N==? ~.N 1> <PRINTC !\s>)>>>

;"Routine version"
;<ROUTINE BOTTLES (N)
    <PRINTN .N>
    <PRINTI " bottle">
    <COND (<N==? .N 1> <PRINTC !\s>)>
    <RTRUE>>
