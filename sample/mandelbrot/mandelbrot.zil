"Mandelbrot ASCII art sample for ZILF"

<VERSION EZIP>
<CONSTANT RELEASEID 1>

<CONSTANT MANDEL-SCALE 48>
<CONSTANT MANDEL-WIDTH 64>
<CONSTANT MANDEL-HEIGHT 32>
<CONSTANT MANDEL-MAXITER 32>
<CONSTANT MANDEL-CHARS <TABLE 32 !\. !\: !\* !\o !\O !\@ !\#>>
<CONSTANT MANDEL-CHARS-LEN 8>

<ROUTINE GO ()
    <CRLF>
    <HLIGHT 8>
    <DRAW-MANDELBROT>
    <CRLF>>

<ROUTINE DRAW-MANDELBROT ("AUX"
        (DENX <- ,MANDEL-WIDTH 1>)
        (DENY <- ,MANDEL-HEIGHT 1>)
        (SPANX <* 3 ,MANDEL-SCALE>)
        (SPANY <* 2 ,MANDEL-SCALE>)
        (XSTEP </ .SPANX .DENX>)
        (XREM <MOD .SPANX .DENX>)
        (YSTEP </ .SPANY .DENY>)
        (YREM <MOD .SPANY .DENY>)
        (IMAG ,MANDEL-SCALE)
        (IACC 0)
        (ROW 0)
        (ESC <* 4 <* ,MANDEL-SCALE ,MANDEL-SCALE>>))
    <REPEAT ()
        <COND (<G? .ROW .DENY> <RETURN>)>
        <DRAW-MANDELBROT-LINE .IMAG .XSTEP .XREM .DENX .ESC>
        <CRLF>
        <COND (<L? .ROW .DENY>
                <SET IMAG <- .IMAG .YSTEP>>
                <SET IACC <+ .IACC .YREM>>
                <COND (<G=? .IACC .DENY>
                       <SET IMAG <- .IMAG 1>>
                       <SET IACC <- .IACC .DENY>>)>)>
        <SET ROW <+ .ROW 1>>>>

<ROUTINE DRAW-MANDELBROT-LINE (IMAG XSTEP XREM DENX ESC "AUX"
        (REAL <- 0 <* 2 ,MANDEL-SCALE>>)
        (RACC 0)
        (COL 0)
        (ITER 0)
        (IDX 0))
    <REPEAT ()
        <COND (<G? .COL .DENX> <RETURN>)>
        <SET ITER <MANDEL-ITER .REAL .IMAG .ESC>>
        <SET IDX </ <* .ITER ,MANDEL-CHARS-LEN> ,MANDEL-MAXITER>>
        <COND (<G? .IDX <- ,MANDEL-CHARS-LEN 1>> <SET IDX <- ,MANDEL-CHARS-LEN 1>>)>
        <PRINTC <GET ,MANDEL-CHARS .IDX>>
        <PRINTC <GET ,MANDEL-CHARS .IDX>>
        <COND (<L? .COL .DENX>
                <SET REAL <+ .REAL .XSTEP>>
                <SET RACC <+ .RACC .XREM>>
                <COND (<G=? .RACC .DENX>
                       <SET REAL <+ .REAL 1>>
                       <SET RACC <- .RACC .DENX>>)>)>
        <SET COL <+ .COL 1>>>>

<ROUTINE MANDEL-ITER (CR CI ESC "AUX"
        (XR 0)
        (XI 0)
        (ITER 0)
        (XX 0)
        (YY 0)
        (TEMP 0))
    <REPEAT ()
        <COND (<G=? .ITER ,MANDEL-MAXITER> <RETURN .ITER>)>
        <SET XX <* .XR .XR>>
        <SET YY <* .XI .XI>>
        <COND (<G? <+ .XX .YY> .ESC> <RETURN .ITER>)>
        <SET TEMP </ <- .XX .YY> ,MANDEL-SCALE>>
        <SET XI <+ </ <* 2 <* .XR .XI>> ,MANDEL-SCALE> .CI>>
        <SET XR <+ .TEMP .CR>>
        <SET ITER <+ .ITER 1>>>>
