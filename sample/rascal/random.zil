"Pseudorandom number generator"

;"In order for seeds to work the same across different interpreters, we can't
  rely on the RANDOM opcode for anything except randomizing the seed itself."

<GLOBAL RNG-STATE-HI 0>
<GLOBAL RNG-STATE-LO 1>
<GLOBAL RNG-SEEDED? <>>

<GLOBAL RNG-SAVED-HI 0>
<GLOBAL RNG-SAVED-LO 1>
<GLOBAL RNG-SAVED-SEEDED? <>>

;"Bitwise XOR for a 16-bit word."
<ROUTINE RNG-XOR16 (A B "AUX" AB ANDAB)
    <SET AB <BOR .A .B>>
    <SET ANDAB <BAND .A .B>>
    <BAND .AB <BCOM .ANDAB>>>

;"Seeds the PRNG from a 32-bit value split into two 16-bit words.

This is a convenience for callers that can provide two 16-bit parts.

Args:
  HI: High 16 bits.
  LO: Low 16 bits.

Returns:
  T."
<ROUTINE SEED-RNG-32 (HI LO)
    <COND (<AND <==? .HI 0> <==? .LO 0>> <SET LO 1>)>
    <SETG RNG-STATE-HI .HI>
    <SETG RNG-STATE-LO .LO>
    <SETG RNG-SEEDED? T>
    <RTRUE>>

<ROUTINE RNG-SAVE-STATE ()
    <SETG RNG-SAVED-HI ,RNG-STATE-HI>
    <SETG RNG-SAVED-LO ,RNG-STATE-LO>
    <SETG RNG-SAVED-SEEDED? ,RNG-SEEDED?>
    <RTRUE>>

<ROUTINE RNG-RESTORE-STATE ()
    <SETG RNG-STATE-HI ,RNG-SAVED-HI>
    <SETG RNG-STATE-LO ,RNG-SAVED-LO>
    <SETG RNG-SEEDED? ,RNG-SAVED-SEEDED?>
    <RTRUE>>

;"Randomizes the initial seed using the built-in RANDOM, then seeds the PRNG.

Returns:
  T."
<ROUTINE INIT-RNG-RANDOM ("AUX" HI LO)
    ;"Use built-in RANDOM only for the initial seed when not explicitly seeded."
    <SET HI <RANDOM 32767>>
    <SET LO <RANDOM 32767>>
    <COND (<==? <RANDOM 2> 2> <SET HI <BOR .HI -32768>>)>
    <COND (<==? <RANDOM 2> 2> <SET LO <BOR .LO -32768>>)>
    <SEED-RNG-32 .HI .LO>>

;"Advances the internal 32-bit state using xorshift32.

Returns:
  T."
<ROUTINE RNG-STEP ("AUX" HI LO SHI SLO)
    <SET HI ,RNG-STATE-HI>
    <SET LO ,RNG-STATE-LO>

    ;"x ^= x << 13"
    <SET SLO <LSH .LO 13>>
    <SET SHI <BOR <LSH .HI 13> <LSH .LO <- 13 16>>>>
    <SET HI <RNG-XOR16 .HI .SHI>>
    <SET LO <RNG-XOR16 .LO .SLO>>

    ;"x ^= x >> 17"
    <SET SHI 0>
    <SET SLO <LSH .HI <- 16 17>>>
    <SET HI <RNG-XOR16 .HI .SHI>>
    <SET LO <RNG-XOR16 .LO .SLO>>

    ;"x ^= x << 5"
    <SET SLO <LSH .LO 5>>
    <SET SHI <BOR <LSH .HI 5> <LSH .LO <- 5 16>>>>
    <SET HI <RNG-XOR16 .HI .SHI>>
    <SET LO <RNG-XOR16 .LO .SLO>>

    <COND (<AND <==? .HI 0> <==? .LO 0>> <SET LO 1>)>
    <SETG RNG-STATE-HI .HI>
    <SETG RNG-STATE-LO .LO>
    <RTRUE>>

;"Returns a raw 16-bit word derived from the PRNG state.

Returns:
  16-bit word value (0..65535)."
<ROUTINE RNG-WORD16 ()
    <COND (<NOT ,RNG-SEEDED?>
           <INIT-RNG-RANDOM>)>
    <RNG-STEP>
    ,RNG-STATE-LO>

;"Returns a random integer from 1..N (like RANDOM).

If the PRNG has not been explicitly seeded with SEED-RNG, it will be
initialized once using the built-in RANDOM.

Args:
  N: Upper bound (positive integer).

Returns:
  Integer in 1..N, or 0 if N<=0."
<ROUTINE RNG (N "AUX" R)
    <COND (<NOT ,RNG-SEEDED?> <INIT-RNG-RANDOM>)>
    <COND (<L=? .N 0> 0)
          (ELSE
           <RNG-STEP>
           ;"Project the 32-bit state to a non-negative value for MOD."
           <SET R <LSH <RNG-XOR16 ,RNG-STATE-HI ,RNG-STATE-LO> -1>>
           <+ 1 <MOD .R .N>>)>>
