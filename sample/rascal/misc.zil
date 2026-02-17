"Utility functions"

<ROUTINE SUM-WORD-TABLE (TBL COUNT "AUX" SUM)
    <SET SUM 0>
    <DO (I 1 .COUNT) <SET SUM <+ .SUM <GET .TBL <- .I 1>>>>>
    .SUM>

;"Absolute value.

Args:
    N: Integer.

Returns:
    |N|."

<ROUTINE ABS (N)
    <COND (<L? .N 0> <- 0 .N>) (ELSE .N)>>

;"Clamps V into the inclusive range [LO, HI].

Args:
    V: Value.
    LO: Lower bound.
    HI: Upper bound.

Returns:
    Clamped value."

<ROUTINE CLAMP (V LO HI)
    <COND (<L? .V .LO> .LO) (<G? .V .HI> .HI) (ELSE .V)>>

;"Returns the greater of A and B.

Args:
    A: A value.
    B: Another value.

Returns:
    Higher value."
<ROUTINE MAX (A B)
    <COND (<G? .A .B> .A) (ELSE .B)>>

;"8-way direction delta helpers.

D is 1..8 and maps to:
  1 2 3
  4   5
  6 7 8"

<ROUTINE DIR8-DX (D)
    <COND (<==? .D 1 4 6> -1) (<==? .D 2 7> 0) (ELSE 1)>>

<ROUTINE DIR8-DY (D)
    <COND (<==? .D 1 2 3> -1) (<==? .D 4 5> 0) (ELSE 1)>>

;"9-way direction delta helpers (including standing still).

D is 1..9 and maps to:
  1 2 3
  4 5 6
  7 8 9"

<ROUTINE DIR9-DX (D)
    <COND (<==? .D 1 4 7> -1) (<==? .D 2 5 8> 0) (ELSE 1)>>

<ROUTINE DIR9-DY (D)
    <COND (<==? .D 1 2 3> -1) (<==? .D 4 5 6> 0) (ELSE 1)>>

;"Converts a numeric keypress to a 1-based inventory slot.

Args:
  C: ZSCII character code.

Returns:
  Slot number (1..10), or 0 if not a slot key."

<ROUTINE DIGIT-TO-SLOT (C)
    <COND (<==? .C !\1> 1)
          (<==? .C !\2> 2)
          (<==? .C !\3> 3)
          (<==? .C !\4> 4)
          (<==? .C !\5> 5)
          (<==? .C !\6> 6)
          (<==? .C !\7> 7)
          (<==? .C !\8> 8)
          (<==? .C !\9> 9)
          (<==? .C !\0> 10)
          (ELSE 0)>>
