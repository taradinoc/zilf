// $ANTLR 3.2 Sep 23, 2009 12:02:23 C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g 2010-04-01 02:39:30

// The variable 'variable' is assigned but its value is never used.
#pragma warning disable 168, 219
// Unreachable code detected.
#pragma warning disable 162


using System;
using Antlr.Runtime;
using IList 		= System.Collections.IList;
using ArrayList 	= System.Collections.ArrayList;
using Stack 		= Antlr.Runtime.Collections.StackList;

using IDictionary	= System.Collections.IDictionary;
using Hashtable 	= System.Collections.Hashtable;
namespace  Zilf.Lexing 
{
public partial class ZilLexer : Lexer {
    public const int UVECTOR = 8;
    public const int APOS = 39;
    public const int LSQUARE = 34;
    public const int PERCENT = 29;
    public const int ATOM_TAIL = 23;
    public const int LANGLE = 30;
    public const int HASH = 12;
    public const int CHAR = 18;
    public const int GVAL = 13;
    public const int BANG = 36;
    public const int RSQUARE = 35;
    public const int ATOM = 24;
    public const int VMACRO = 11;
    public const int EOF = -1;
    public const int LIST = 6;
    public const int SPACE = 19;
    public const int SEMI = 27;
    public const int NUM = 25;
    public const int LPAREN = 32;
    public const int COLON = 28;
    public const int ATOM_OR_NUM = 26;
    public const int RPAREN = 33;
    public const int WS = 20;
    public const int VECTOR = 7;
    public const int COMMA = 38;
    public const int LVAL = 14;
    public const int ATOM_HEAD = 22;
    public const int DIGIT = 16;
    public const int ADECL = 15;
    public const int FORM = 5;
    public const int RANGLE = 31;
    public const int COMMENT = 4;
    public const int DOT = 37;
    public const int MACRO = 10;
    public const int NATOM = 21;
    public const int SEGMENT = 9;
    public const int STRING = 17;

    // delegates
    // delegators

    public ZilLexer() 
    {
		InitializeCyclicDFAs();
    }
    public ZilLexer(ICharStream input)
		: this(input, null) {
    }
    public ZilLexer(ICharStream input, RecognizerSharedState state)
		: base(input, state) {
		InitializeCyclicDFAs(); 

    }
    
    override public string GrammarFileName
    {
    	get { return "C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g";} 
    }

    // $ANTLR start "DIGIT"
    public void mDIGIT() // throws RecognitionException [2]
    {
    		try
    		{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:14:2: ( '0' .. '9' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:14:4: '0' .. '9'
            {
            	MatchRange('0','9'); if (state.failed) return ;

            }

        }
        finally 
    	{
        }
    }
    // $ANTLR end "DIGIT"

    // $ANTLR start "STRING"
    public void mSTRING() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = STRING;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:17:8: ( '\"' (~ ( '\\\\' | '\"' ) | '\\\\' . )* '\"' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:17:10: '\"' (~ ( '\\\\' | '\"' ) | '\\\\' . )* '\"'
            {
            	Match('\"'); if (state.failed) return ;
            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:18:3: (~ ( '\\\\' | '\"' ) | '\\\\' . )*
            	do 
            	{
            	    int alt1 = 3;
            	    int LA1_0 = input.LA(1);

            	    if ( ((LA1_0 >= '\u0000' && LA1_0 <= '!') || (LA1_0 >= '#' && LA1_0 <= '[') || (LA1_0 >= ']' && LA1_0 <= '\uFFFF')) )
            	    {
            	        alt1 = 1;
            	    }
            	    else if ( (LA1_0 == '\\') )
            	    {
            	        alt1 = 2;
            	    }


            	    switch (alt1) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:18:4: ~ ( '\\\\' | '\"' )
            			    {
            			    	if ( (input.LA(1) >= '\u0000' && input.LA(1) <= '!') || (input.LA(1) >= '#' && input.LA(1) <= '[') || (input.LA(1) >= ']' && input.LA(1) <= '\uFFFF') ) 
            			    	{
            			    	    input.Consume();
            			    	state.failed = false;
            			    	}
            			    	else 
            			    	{
            			    	    if ( state.backtracking > 0 ) {state.failed = true; return ;}
            			    	    MismatchedSetException mse = new MismatchedSetException(null,input);
            			    	    Recover(mse);
            			    	    throw mse;}


            			    }
            			    break;
            			case 2 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:18:20: '\\\\' .
            			    {
            			    	Match('\\'); if (state.failed) return ;
            			    	MatchAny(); if (state.failed) return ;

            			    }
            			    break;

            			default:
            			    goto loop1;
            	    }
            	} while (true);

            	loop1:
            		;	// Stops C# compiler whining that label 'loop1' has no statements

            	Match('\"'); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "STRING"

    // $ANTLR start "CHAR"
    public void mCHAR() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = CHAR;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:22:6: ( '!' '\\\\' . )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:22:8: '!' '\\\\' .
            {
            	Match('!'); if (state.failed) return ;
            	Match('\\'); if (state.failed) return ;
            	MatchAny(); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "CHAR"

    // $ANTLR start "SPACE"
    public void mSPACE() // throws RecognitionException [2]
    {
    		try
    		{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:26:2: ( ' ' | '\\t' | '\\r' | '\\n' | '\\f' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:
            {
            	if ( (input.LA(1) >= '\t' && input.LA(1) <= '\n') || (input.LA(1) >= '\f' && input.LA(1) <= '\r') || input.LA(1) == ' ' ) 
            	{
            	    input.Consume();
            	state.failed = false;
            	}
            	else 
            	{
            	    if ( state.backtracking > 0 ) {state.failed = true; return ;}
            	    MismatchedSetException mse = new MismatchedSetException(null,input);
            	    Recover(mse);
            	    throw mse;}


            }

        }
        finally 
    	{
        }
    }
    // $ANTLR end "SPACE"

    // $ANTLR start "WS"
    public void mWS() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = WS;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:29:4: ( ( SPACE )+ )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:29:6: ( SPACE )+
            {
            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:29:6: ( SPACE )+
            	int cnt2 = 0;
            	do 
            	{
            	    int alt2 = 2;
            	    int LA2_0 = input.LA(1);

            	    if ( ((LA2_0 >= '\t' && LA2_0 <= '\n') || (LA2_0 >= '\f' && LA2_0 <= '\r') || LA2_0 == ' ') )
            	    {
            	        alt2 = 1;
            	    }


            	    switch (alt2) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:29:6: SPACE
            			    {
            			    	mSPACE(); if (state.failed) return ;

            			    }
            			    break;

            			default:
            			    if ( cnt2 >= 1 ) goto loop2;
            			    if ( state.backtracking > 0 ) {state.failed = true; return ;}
            		            EarlyExitException eee2 =
            		                new EarlyExitException(2, input);
            		            throw eee2;
            	    }
            	    cnt2++;
            	} while (true);

            	loop2:
            		;	// Stops C# compiler whining that label 'loop2' has no statements

            	if ( (state.backtracking==0) )
            	{
            	   _channel = HIDDEN; 
            	}

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "WS"

    // $ANTLR start "ATOM_HEAD"
    public void mATOM_HEAD() // throws RecognitionException [2]
    {
    		try
    		{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:33:2: (~ ( NATOM | '.' | '!' ) | '\\\\' . )
            int alt3 = 2;
            int LA3_0 = input.LA(1);

            if ( ((LA3_0 >= '\u0000' && LA3_0 <= '\b') || LA3_0 == '\u000B' || (LA3_0 >= '\u000E' && LA3_0 <= '\u001F') || LA3_0 == '$' || LA3_0 == '&' || (LA3_0 >= '*' && LA3_0 <= '+') || LA3_0 == '-' || (LA3_0 >= '/' && LA3_0 <= '9') || LA3_0 == '=' || (LA3_0 >= '?' && LA3_0 <= 'Z') || (LA3_0 >= '^' && LA3_0 <= 'z') || LA3_0 == '|' || (LA3_0 >= '~' && LA3_0 <= '\uFFFF')) )
            {
                alt3 = 1;
            }
            else if ( (LA3_0 == '\\') )
            {
                alt3 = 2;
            }
            else 
            {
                if ( state.backtracking > 0 ) {state.failed = true; return ;}
                NoViableAltException nvae_d3s0 =
                    new NoViableAltException("", 3, 0, input);

                throw nvae_d3s0;
            }
            switch (alt3) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:33:4: ~ ( NATOM | '.' | '!' )
                    {
                    	if ( (input.LA(1) >= '\u0000' && input.LA(1) <= '\b') || input.LA(1) == '\u000B' || (input.LA(1) >= '\u000E' && input.LA(1) <= '\u001F') || input.LA(1) == '$' || input.LA(1) == '&' || (input.LA(1) >= '*' && input.LA(1) <= '+') || input.LA(1) == '-' || (input.LA(1) >= '/' && input.LA(1) <= '9') || input.LA(1) == '=' || (input.LA(1) >= '?' && input.LA(1) <= 'Z') || (input.LA(1) >= '^' && input.LA(1) <= 'z') || input.LA(1) == '|' || (input.LA(1) >= '~' && input.LA(1) <= '\uFFFF') ) 
                    	{
                    	    input.Consume();
                    	state.failed = false;
                    	}
                    	else 
                    	{
                    	    if ( state.backtracking > 0 ) {state.failed = true; return ;}
                    	    MismatchedSetException mse = new MismatchedSetException(null,input);
                    	    Recover(mse);
                    	    throw mse;}


                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:34:4: '\\\\' .
                    {
                    	Match('\\'); if (state.failed) return ;
                    	MatchAny(); if (state.failed) return ;

                    }
                    break;

            }
        }
        finally 
    	{
        }
    }
    // $ANTLR end "ATOM_HEAD"

    // $ANTLR start "ATOM_TAIL"
    public void mATOM_TAIL() // throws RecognitionException [2]
    {
    		try
    		{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:38:2: (~ ( NATOM ) | '\\\\' . )
            int alt4 = 2;
            int LA4_0 = input.LA(1);

            if ( ((LA4_0 >= '\u0000' && LA4_0 <= '\b') || LA4_0 == '\u000B' || (LA4_0 >= '\u000E' && LA4_0 <= '\u001F') || LA4_0 == '!' || LA4_0 == '$' || LA4_0 == '&' || (LA4_0 >= '*' && LA4_0 <= '+') || (LA4_0 >= '-' && LA4_0 <= '9') || LA4_0 == '=' || (LA4_0 >= '?' && LA4_0 <= 'Z') || (LA4_0 >= '^' && LA4_0 <= 'z') || LA4_0 == '|' || (LA4_0 >= '~' && LA4_0 <= '\uFFFF')) )
            {
                alt4 = 1;
            }
            else if ( (LA4_0 == '\\') )
            {
                alt4 = 2;
            }
            else 
            {
                if ( state.backtracking > 0 ) {state.failed = true; return ;}
                NoViableAltException nvae_d4s0 =
                    new NoViableAltException("", 4, 0, input);

                throw nvae_d4s0;
            }
            switch (alt4) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:38:4: ~ ( NATOM )
                    {
                    	if ( (input.LA(1) >= '\u0000' && input.LA(1) <= '\b') || input.LA(1) == '\u000B' || (input.LA(1) >= '\u000E' && input.LA(1) <= '\u001F') || input.LA(1) == '!' || input.LA(1) == '$' || input.LA(1) == '&' || (input.LA(1) >= '*' && input.LA(1) <= '+') || (input.LA(1) >= '-' && input.LA(1) <= '9') || input.LA(1) == '=' || (input.LA(1) >= '?' && input.LA(1) <= 'Z') || (input.LA(1) >= '^' && input.LA(1) <= 'z') || input.LA(1) == '|' || (input.LA(1) >= '~' && input.LA(1) <= '\uFFFF') ) 
                    	{
                    	    input.Consume();
                    	state.failed = false;
                    	}
                    	else 
                    	{
                    	    if ( state.backtracking > 0 ) {state.failed = true; return ;}
                    	    MismatchedSetException mse = new MismatchedSetException(null,input);
                    	    Recover(mse);
                    	    throw mse;}


                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:39:4: '\\\\' .
                    {
                    	Match('\\'); if (state.failed) return ;
                    	MatchAny(); if (state.failed) return ;

                    }
                    break;

            }
        }
        finally 
    	{
        }
    }
    // $ANTLR end "ATOM_TAIL"

    // $ANTLR start "NATOM"
    public void mNATOM() // throws RecognitionException [2]
    {
    		try
    		{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:43:2: ( SPACE | '<' | '>' | '(' | ')' | '{' | '}' | '[' | ']' | ':' | ';' | '\"' | '\\'' | '\\\\' | ',' | '%' | '#' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:
            {
            	if ( (input.LA(1) >= '\t' && input.LA(1) <= '\n') || (input.LA(1) >= '\f' && input.LA(1) <= '\r') || input.LA(1) == ' ' || (input.LA(1) >= '\"' && input.LA(1) <= '#') || input.LA(1) == '%' || (input.LA(1) >= '\'' && input.LA(1) <= ')') || input.LA(1) == ',' || (input.LA(1) >= ':' && input.LA(1) <= '<') || input.LA(1) == '>' || (input.LA(1) >= '[' && input.LA(1) <= ']') || input.LA(1) == '{' || input.LA(1) == '}' ) 
            	{
            	    input.Consume();
            	state.failed = false;
            	}
            	else 
            	{
            	    if ( state.backtracking > 0 ) {state.failed = true; return ;}
            	    MismatchedSetException mse = new MismatchedSetException(null,input);
            	    Recover(mse);
            	    throw mse;}


            }

        }
        finally 
    	{
        }
    }
    // $ANTLR end "NATOM"

    // $ANTLR start "ATOM"
    public void mATOM() // throws RecognitionException [2]
    {
    		try
    		{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:47:2: ( ATOM_HEAD ( ATOM_TAIL )* )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:47:4: ATOM_HEAD ( ATOM_TAIL )*
            {
            	mATOM_HEAD(); if (state.failed) return ;
            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:47:14: ( ATOM_TAIL )*
            	do 
            	{
            	    int alt5 = 2;
            	    int LA5_0 = input.LA(1);

            	    if ( ((LA5_0 >= '\u0000' && LA5_0 <= '\b') || LA5_0 == '\u000B' || (LA5_0 >= '\u000E' && LA5_0 <= '\u001F') || LA5_0 == '!' || LA5_0 == '$' || LA5_0 == '&' || (LA5_0 >= '*' && LA5_0 <= '+') || (LA5_0 >= '-' && LA5_0 <= '9') || LA5_0 == '=' || (LA5_0 >= '?' && LA5_0 <= 'Z') || LA5_0 == '\\' || (LA5_0 >= '^' && LA5_0 <= 'z') || LA5_0 == '|' || (LA5_0 >= '~' && LA5_0 <= '\uFFFF')) )
            	    {
            	        alt5 = 1;
            	    }


            	    switch (alt5) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:47:14: ATOM_TAIL
            			    {
            			    	mATOM_TAIL(); if (state.failed) return ;

            			    }
            			    break;

            			default:
            			    goto loop5;
            	    }
            	} while (true);

            	loop5:
            		;	// Stops C# compiler whining that label 'loop5' has no statements


            }

        }
        finally 
    	{
        }
    }
    // $ANTLR end "ATOM"

    // $ANTLR start "NUM"
    public void mNUM() // throws RecognitionException [2]
    {
    		try
    		{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:51:2: ( ( '-' | '+' )? ( DIGIT )+ | '*' ( '0' .. '7' )+ '*' | '#2' ( SPACE )+ ( '0' .. '1' )+ )
            int alt11 = 3;
            switch ( input.LA(1) ) 
            {
            case '+':
            case '-':
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
            	{
                alt11 = 1;
                }
                break;
            case '*':
            	{
                alt11 = 2;
                }
                break;
            case '#':
            	{
                alt11 = 3;
                }
                break;
            	default:
            	    if ( state.backtracking > 0 ) {state.failed = true; return ;}
            	    NoViableAltException nvae_d11s0 =
            	        new NoViableAltException("", 11, 0, input);

            	    throw nvae_d11s0;
            }

            switch (alt11) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:51:4: ( '-' | '+' )? ( DIGIT )+
                    {
                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:51:4: ( '-' | '+' )?
                    	int alt6 = 2;
                    	int LA6_0 = input.LA(1);

                    	if ( (LA6_0 == '+' || LA6_0 == '-') )
                    	{
                    	    alt6 = 1;
                    	}
                    	switch (alt6) 
                    	{
                    	    case 1 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:
                    	        {
                    	        	if ( input.LA(1) == '+' || input.LA(1) == '-' ) 
                    	        	{
                    	        	    input.Consume();
                    	        	state.failed = false;
                    	        	}
                    	        	else 
                    	        	{
                    	        	    if ( state.backtracking > 0 ) {state.failed = true; return ;}
                    	        	    MismatchedSetException mse = new MismatchedSetException(null,input);
                    	        	    Recover(mse);
                    	        	    throw mse;}


                    	        }
                    	        break;

                    	}

                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:51:17: ( DIGIT )+
                    	int cnt7 = 0;
                    	do 
                    	{
                    	    int alt7 = 2;
                    	    int LA7_0 = input.LA(1);

                    	    if ( ((LA7_0 >= '0' && LA7_0 <= '9')) )
                    	    {
                    	        alt7 = 1;
                    	    }


                    	    switch (alt7) 
                    		{
                    			case 1 :
                    			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:51:17: DIGIT
                    			    {
                    			    	mDIGIT(); if (state.failed) return ;

                    			    }
                    			    break;

                    			default:
                    			    if ( cnt7 >= 1 ) goto loop7;
                    			    if ( state.backtracking > 0 ) {state.failed = true; return ;}
                    		            EarlyExitException eee7 =
                    		                new EarlyExitException(7, input);
                    		            throw eee7;
                    	    }
                    	    cnt7++;
                    	} while (true);

                    	loop7:
                    		;	// Stops C# compiler whining that label 'loop7' has no statements


                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:52:4: '*' ( '0' .. '7' )+ '*'
                    {
                    	Match('*'); if (state.failed) return ;
                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:52:8: ( '0' .. '7' )+
                    	int cnt8 = 0;
                    	do 
                    	{
                    	    int alt8 = 2;
                    	    int LA8_0 = input.LA(1);

                    	    if ( ((LA8_0 >= '0' && LA8_0 <= '7')) )
                    	    {
                    	        alt8 = 1;
                    	    }


                    	    switch (alt8) 
                    		{
                    			case 1 :
                    			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:52:8: '0' .. '7'
                    			    {
                    			    	MatchRange('0','7'); if (state.failed) return ;

                    			    }
                    			    break;

                    			default:
                    			    if ( cnt8 >= 1 ) goto loop8;
                    			    if ( state.backtracking > 0 ) {state.failed = true; return ;}
                    		            EarlyExitException eee8 =
                    		                new EarlyExitException(8, input);
                    		            throw eee8;
                    	    }
                    	    cnt8++;
                    	} while (true);

                    	loop8:
                    		;	// Stops C# compiler whining that label 'loop8' has no statements

                    	Match('*'); if (state.failed) return ;

                    }
                    break;
                case 3 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:53:4: '#2' ( SPACE )+ ( '0' .. '1' )+
                    {
                    	Match("#2"); if (state.failed) return ;

                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:53:9: ( SPACE )+
                    	int cnt9 = 0;
                    	do 
                    	{
                    	    int alt9 = 2;
                    	    int LA9_0 = input.LA(1);

                    	    if ( ((LA9_0 >= '\t' && LA9_0 <= '\n') || (LA9_0 >= '\f' && LA9_0 <= '\r') || LA9_0 == ' ') )
                    	    {
                    	        alt9 = 1;
                    	    }


                    	    switch (alt9) 
                    		{
                    			case 1 :
                    			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:53:9: SPACE
                    			    {
                    			    	mSPACE(); if (state.failed) return ;

                    			    }
                    			    break;

                    			default:
                    			    if ( cnt9 >= 1 ) goto loop9;
                    			    if ( state.backtracking > 0 ) {state.failed = true; return ;}
                    		            EarlyExitException eee9 =
                    		                new EarlyExitException(9, input);
                    		            throw eee9;
                    	    }
                    	    cnt9++;
                    	} while (true);

                    	loop9:
                    		;	// Stops C# compiler whining that label 'loop9' has no statements

                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:53:16: ( '0' .. '1' )+
                    	int cnt10 = 0;
                    	do 
                    	{
                    	    int alt10 = 2;
                    	    int LA10_0 = input.LA(1);

                    	    if ( ((LA10_0 >= '0' && LA10_0 <= '1')) )
                    	    {
                    	        alt10 = 1;
                    	    }


                    	    switch (alt10) 
                    		{
                    			case 1 :
                    			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:53:16: '0' .. '1'
                    			    {
                    			    	MatchRange('0','1'); if (state.failed) return ;

                    			    }
                    			    break;

                    			default:
                    			    if ( cnt10 >= 1 ) goto loop10;
                    			    if ( state.backtracking > 0 ) {state.failed = true; return ;}
                    		            EarlyExitException eee10 =
                    		                new EarlyExitException(10, input);
                    		            throw eee10;
                    	    }
                    	    cnt10++;
                    	} while (true);

                    	loop10:
                    		;	// Stops C# compiler whining that label 'loop10' has no statements


                    }
                    break;

            }
        }
        finally 
    	{
        }
    }
    // $ANTLR end "NUM"

    // $ANTLR start "ATOM_OR_NUM"
    public void mATOM_OR_NUM() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = ATOM_OR_NUM;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:57:2: ( ( NUM ( EOF | NATOM ) )=> NUM | ATOM )
            int alt12 = 2;
            alt12 = dfa12.Predict(input);
            switch (alt12) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:57:4: ( NUM ( EOF | NATOM ) )=> NUM
                    {
                    	mNUM(); if (state.failed) return ;
                    	if ( (state.backtracking==0) )
                    	{
                    	   _type = NUM; 
                    	}

                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:58:4: ATOM
                    {
                    	mATOM(); if (state.failed) return ;
                    	if ( (state.backtracking==0) )
                    	{
                    	   _type = ATOM; 
                    	}

                    }
                    break;

            }
            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "ATOM_OR_NUM"

    // $ANTLR start "SEMI"
    public void mSEMI() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = SEMI;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:61:6: ( ';' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:61:8: ';'
            {
            	Match(';'); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "SEMI"

    // $ANTLR start "COLON"
    public void mCOLON() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = COLON;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:62:7: ( ':' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:62:9: ':'
            {
            	Match(':'); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "COLON"

    // $ANTLR start "HASH"
    public void mHASH() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = HASH;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:63:6: ( '#' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:63:8: '#'
            {
            	Match('#'); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "HASH"

    // $ANTLR start "PERCENT"
    public void mPERCENT() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = PERCENT;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:64:9: ( '%' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:64:11: '%'
            {
            	Match('%'); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "PERCENT"

    // $ANTLR start "LANGLE"
    public void mLANGLE() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = LANGLE;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:65:8: ( '<' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:65:10: '<'
            {
            	Match('<'); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "LANGLE"

    // $ANTLR start "RANGLE"
    public void mRANGLE() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = RANGLE;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:66:8: ( '>' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:66:10: '>'
            {
            	Match('>'); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "RANGLE"

    // $ANTLR start "LPAREN"
    public void mLPAREN() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = LPAREN;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:67:8: ( '(' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:67:10: '('
            {
            	Match('('); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "LPAREN"

    // $ANTLR start "RPAREN"
    public void mRPAREN() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = RPAREN;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:68:8: ( ')' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:68:10: ')'
            {
            	Match(')'); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "RPAREN"

    // $ANTLR start "LSQUARE"
    public void mLSQUARE() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = LSQUARE;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:69:9: ( '[' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:69:11: '['
            {
            	Match('['); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "LSQUARE"

    // $ANTLR start "RSQUARE"
    public void mRSQUARE() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = RSQUARE;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:70:9: ( ']' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:70:11: ']'
            {
            	Match(']'); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "RSQUARE"

    // $ANTLR start "BANG"
    public void mBANG() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = BANG;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:71:6: ( '!' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:71:8: '!'
            {
            	Match('!'); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "BANG"

    // $ANTLR start "DOT"
    public void mDOT() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DOT;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:72:5: ( '.' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:72:7: '.'
            {
            	Match('.'); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DOT"

    // $ANTLR start "COMMA"
    public void mCOMMA() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = COMMA;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:73:7: ( ',' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:73:9: ','
            {
            	Match(','); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "COMMA"

    // $ANTLR start "APOS"
    public void mAPOS() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = APOS;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:74:6: ( '\\'' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:74:8: '\\''
            {
            	Match('\''); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "APOS"

    override public void mTokens() // throws RecognitionException 
    {
        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:8: ( STRING | CHAR | WS | ATOM_OR_NUM | SEMI | COLON | HASH | PERCENT | LANGLE | RANGLE | LPAREN | RPAREN | LSQUARE | RSQUARE | BANG | DOT | COMMA | APOS )
        int alt13 = 18;
        alt13 = dfa13.Predict(input);
        switch (alt13) 
        {
            case 1 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:10: STRING
                {
                	mSTRING(); if (state.failed) return ;

                }
                break;
            case 2 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:17: CHAR
                {
                	mCHAR(); if (state.failed) return ;

                }
                break;
            case 3 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:22: WS
                {
                	mWS(); if (state.failed) return ;

                }
                break;
            case 4 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:25: ATOM_OR_NUM
                {
                	mATOM_OR_NUM(); if (state.failed) return ;

                }
                break;
            case 5 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:37: SEMI
                {
                	mSEMI(); if (state.failed) return ;

                }
                break;
            case 6 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:42: COLON
                {
                	mCOLON(); if (state.failed) return ;

                }
                break;
            case 7 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:48: HASH
                {
                	mHASH(); if (state.failed) return ;

                }
                break;
            case 8 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:53: PERCENT
                {
                	mPERCENT(); if (state.failed) return ;

                }
                break;
            case 9 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:61: LANGLE
                {
                	mLANGLE(); if (state.failed) return ;

                }
                break;
            case 10 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:68: RANGLE
                {
                	mRANGLE(); if (state.failed) return ;

                }
                break;
            case 11 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:75: LPAREN
                {
                	mLPAREN(); if (state.failed) return ;

                }
                break;
            case 12 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:82: RPAREN
                {
                	mRPAREN(); if (state.failed) return ;

                }
                break;
            case 13 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:89: LSQUARE
                {
                	mLSQUARE(); if (state.failed) return ;

                }
                break;
            case 14 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:97: RSQUARE
                {
                	mRSQUARE(); if (state.failed) return ;

                }
                break;
            case 15 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:105: BANG
                {
                	mBANG(); if (state.failed) return ;

                }
                break;
            case 16 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:110: DOT
                {
                	mDOT(); if (state.failed) return ;

                }
                break;
            case 17 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:114: COMMA
                {
                	mCOMMA(); if (state.failed) return ;

                }
                break;
            case 18 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:1:120: APOS
                {
                	mAPOS(); if (state.failed) return ;

                }
                break;

        }

    }

    // $ANTLR start "synpred1_Zil"
    public void synpred1_Zil_fragment() {
        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:57:4: ( NUM ( EOF | NATOM ) )
        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:57:5: NUM ( EOF | NATOM )
        {
        	mNUM(); if (state.failed) return ;
        	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:57:9: ( EOF | NATOM )
        	int alt14 = 2;
        	int LA14_0 = input.LA(1);

        	if ( ((LA14_0 >= '\t' && LA14_0 <= '\n') || (LA14_0 >= '\f' && LA14_0 <= '\r') || LA14_0 == ' ' || (LA14_0 >= '\"' && LA14_0 <= '#') || LA14_0 == '%' || (LA14_0 >= '\'' && LA14_0 <= ')') || LA14_0 == ',' || (LA14_0 >= ':' && LA14_0 <= '<') || LA14_0 == '>' || (LA14_0 >= '[' && LA14_0 <= ']') || LA14_0 == '{' || LA14_0 == '}') )
        	{
        	    alt14 = 2;
        	}
        	else 
        	{
        	    alt14 = 1;}
        	switch (alt14) 
        	{
        	    case 1 :
        	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:57:10: EOF
        	        {
        	        	Match(EOF); if (state.failed) return ;

        	        }
        	        break;
        	    case 2 :
        	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:57:16: NATOM
        	        {
        	        	mNATOM(); if (state.failed) return ;

        	        }
        	        break;

        	}


        }
    }
    // $ANTLR end "synpred1_Zil"

   	public bool synpred1_Zil() 
   	{
   	    state.backtracking++;
   	    int start = input.Mark();
   	    try 
   	    {
   	        synpred1_Zil_fragment(); // can never throw exception
   	    }
   	    catch (RecognitionException re) 
   	    {
   	        Console.Error.WriteLine("impossible: "+re);
   	    }
   	    bool success = !state.failed;
   	    input.Rewind(start);
   	    state.backtracking--;
   	    state.failed = false;
   	    return success;
   	}


    protected DFA12 dfa12;
    protected DFA13 dfa13;
	private void InitializeCyclicDFAs()
	{
	    this.dfa12 = new DFA12(this);
	    this.dfa13 = new DFA13(this);
	    this.dfa12.specialStateTransitionHandler = new DFA.SpecialStateTransitionHandler(DFA12_SpecialStateTransition);
	    this.dfa13.specialStateTransitionHandler = new DFA.SpecialStateTransitionHandler(DFA13_SpecialStateTransition);
	}

    const string DFA12_eotS =
        "\x01\uffff\x01\x05\x01\uffff\x01\x05\x03\uffff\x01\x05\x01\uffff";
    const string DFA12_eofS =
        "\x09\uffff";
    const string DFA12_minS =
        "\x01\x00\x01\x30\x01\x00\x01\x30\x02\uffff\x01\x00\x01\x2a\x01"+
        "\x00";
    const string DFA12_maxS =
        "\x01\uffff\x01\x39\x01\x00\x01\x37\x02\uffff\x01\x00\x01\x37\x01"+
        "\x00";
    const string DFA12_acceptS =
        "\x04\uffff\x01\x01\x01\x02\x03\uffff";
    const string DFA12_specialS =
        "\x01\x03\x01\uffff\x01\x01\x03\uffff\x01\x00\x01\uffff\x01\x02}>";
    static readonly string[] DFA12_transitionS = {
            "\x09\x05\x02\uffff\x01\x05\x02\uffff\x12\x05\x03\uffff\x01"+
            "\x04\x01\x05\x01\uffff\x01\x05\x03\uffff\x01\x03\x01\x01\x01"+
            "\uffff\x01\x01\x01\uffff\x01\x05\x0a\x02\x03\uffff\x01\x05\x01"+
            "\uffff\x1c\x05\x01\uffff\x01\x05\x01\uffff\x1d\x05\x01\uffff"+
            "\x01\x05\x01\uffff\uff82\x05",
            "\x0a\x06",
            "\x01\uffff",
            "\x08\x07",
            "",
            "",
            "\x01\uffff",
            "\x01\x08\x05\uffff\x08\x07",
            "\x01\uffff"
    };

    static readonly short[] DFA12_eot = DFA.UnpackEncodedString(DFA12_eotS);
    static readonly short[] DFA12_eof = DFA.UnpackEncodedString(DFA12_eofS);
    static readonly char[] DFA12_min = DFA.UnpackEncodedStringToUnsignedChars(DFA12_minS);
    static readonly char[] DFA12_max = DFA.UnpackEncodedStringToUnsignedChars(DFA12_maxS);
    static readonly short[] DFA12_accept = DFA.UnpackEncodedString(DFA12_acceptS);
    static readonly short[] DFA12_special = DFA.UnpackEncodedString(DFA12_specialS);
    static readonly short[][] DFA12_transition = DFA.UnpackEncodedStringArray(DFA12_transitionS);

    protected class DFA12 : DFA
    {
        public DFA12(BaseRecognizer recognizer)
        {
            this.recognizer = recognizer;
            this.decisionNumber = 12;
            this.eot = DFA12_eot;
            this.eof = DFA12_eof;
            this.min = DFA12_min;
            this.max = DFA12_max;
            this.accept = DFA12_accept;
            this.special = DFA12_special;
            this.transition = DFA12_transition;

        }

        override public string Description
        {
            get { return "56:1: ATOM_OR_NUM : ( ( NUM ( EOF | NATOM ) )=> NUM | ATOM );"; }
        }

    }


    protected internal int DFA12_SpecialStateTransition(DFA dfa, int s, IIntStream _input) //throws NoViableAltException
    {
            IIntStream input = _input;
    	int _s = s;
        switch ( s )
        {
               	case 0 : 
                   	int LA12_6 = input.LA(1);

                   	 
                   	int index12_6 = input.Index();
                   	input.Rewind();
                   	s = -1;
                   	if ( (synpred1_Zil()) ) { s = 4; }

                   	else if ( (true) ) { s = 5; }

                   	 
                   	input.Seek(index12_6);
                   	if ( s >= 0 ) return s;
                   	break;
               	case 1 : 
                   	int LA12_2 = input.LA(1);

                   	 
                   	int index12_2 = input.Index();
                   	input.Rewind();
                   	s = -1;
                   	if ( (synpred1_Zil()) ) { s = 4; }

                   	else if ( (true) ) { s = 5; }

                   	 
                   	input.Seek(index12_2);
                   	if ( s >= 0 ) return s;
                   	break;
               	case 2 : 
                   	int LA12_8 = input.LA(1);

                   	 
                   	int index12_8 = input.Index();
                   	input.Rewind();
                   	s = -1;
                   	if ( (synpred1_Zil()) ) { s = 4; }

                   	else if ( (true) ) { s = 5; }

                   	 
                   	input.Seek(index12_8);
                   	if ( s >= 0 ) return s;
                   	break;
               	case 3 : 
                   	int LA12_0 = input.LA(1);

                   	 
                   	int index12_0 = input.Index();
                   	input.Rewind();
                   	s = -1;
                   	if ( (LA12_0 == '+' || LA12_0 == '-') ) { s = 1; }

                   	else if ( ((LA12_0 >= '0' && LA12_0 <= '9')) ) { s = 2; }

                   	else if ( (LA12_0 == '*') ) { s = 3; }

                   	else if ( (LA12_0 == '#') && (synpred1_Zil()) ) { s = 4; }

                   	else if ( ((LA12_0 >= '\u0000' && LA12_0 <= '\b') || LA12_0 == '\u000B' || (LA12_0 >= '\u000E' && LA12_0 <= '\u001F') || LA12_0 == '$' || LA12_0 == '&' || LA12_0 == '/' || LA12_0 == '=' || (LA12_0 >= '?' && LA12_0 <= 'Z') || LA12_0 == '\\' || (LA12_0 >= '^' && LA12_0 <= 'z') || LA12_0 == '|' || (LA12_0 >= '~' && LA12_0 <= '\uFFFF')) ) { s = 5; }

                   	 
                   	input.Seek(index12_0);
                   	if ( s >= 0 ) return s;
                   	break;
        }
        if (state.backtracking > 0) {state.failed = true; return -1;}
        NoViableAltException nvae12 =
            new NoViableAltException(dfa.Description, 12, _s, input);
        dfa.Error(nvae12);
        throw nvae12;
    }
    const string DFA13_eotS =
        "\x02\uffff\x01\x13\x02\uffff\x01\x14\x0f\uffff";
    const string DFA13_eofS =
        "\x15\uffff";
    const string DFA13_minS =
        "\x01\x00\x01\uffff\x01\x5c\x02\uffff\x01\x32\x0f\uffff";
    const string DFA13_maxS =
        "\x01\uffff\x01\uffff\x01\x5c\x02\uffff\x01\x32\x0f\uffff";
    const string DFA13_acceptS =
        "\x01\uffff\x01\x01\x01\uffff\x01\x03\x01\x04\x01\uffff\x01\x05"+
        "\x01\x06\x01\x08\x01\x09\x01\x0a\x01\x0b\x01\x0c\x01\x0d\x01\x0e"+
        "\x01\x10\x01\x11\x01\x12\x01\x02\x01\x0f\x01\x07";
    const string DFA13_specialS =
        "\x01\x00\x14\uffff}>";
    static readonly string[] DFA13_transitionS = {
            "\x09\x04\x02\x03\x01\x04\x02\x03\x12\x04\x01\x03\x01\x02\x01"+
            "\x01\x01\x05\x01\x04\x01\x08\x01\x04\x01\x11\x01\x0b\x01\x0c"+
            "\x02\x04\x01\x10\x01\x04\x01\x0f\x0b\x04\x01\x07\x01\x06\x01"+
            "\x09\x01\x04\x01\x0a\x1c\x04\x01\x0d\x01\x04\x01\x0e\x1d\x04"+
            "\x01\uffff\x01\x04\x01\uffff\uff82\x04",
            "",
            "\x01\x12",
            "",
            "",
            "\x01\x04",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            ""
    };

    static readonly short[] DFA13_eot = DFA.UnpackEncodedString(DFA13_eotS);
    static readonly short[] DFA13_eof = DFA.UnpackEncodedString(DFA13_eofS);
    static readonly char[] DFA13_min = DFA.UnpackEncodedStringToUnsignedChars(DFA13_minS);
    static readonly char[] DFA13_max = DFA.UnpackEncodedStringToUnsignedChars(DFA13_maxS);
    static readonly short[] DFA13_accept = DFA.UnpackEncodedString(DFA13_acceptS);
    static readonly short[] DFA13_special = DFA.UnpackEncodedString(DFA13_specialS);
    static readonly short[][] DFA13_transition = DFA.UnpackEncodedStringArray(DFA13_transitionS);

    protected class DFA13 : DFA
    {
        public DFA13(BaseRecognizer recognizer)
        {
            this.recognizer = recognizer;
            this.decisionNumber = 13;
            this.eot = DFA13_eot;
            this.eof = DFA13_eof;
            this.min = DFA13_min;
            this.max = DFA13_max;
            this.accept = DFA13_accept;
            this.special = DFA13_special;
            this.transition = DFA13_transition;

        }

        override public string Description
        {
            get { return "1:1: Tokens : ( STRING | CHAR | WS | ATOM_OR_NUM | SEMI | COLON | HASH | PERCENT | LANGLE | RANGLE | LPAREN | RPAREN | LSQUARE | RSQUARE | BANG | DOT | COMMA | APOS );"; }
        }

    }


    protected internal int DFA13_SpecialStateTransition(DFA dfa, int s, IIntStream _input) //throws NoViableAltException
    {
            IIntStream input = _input;
    	int _s = s;
        switch ( s )
        {
               	case 0 : 
                   	int LA13_0 = input.LA(1);

                   	s = -1;
                   	if ( (LA13_0 == '\"') ) { s = 1; }

                   	else if ( (LA13_0 == '!') ) { s = 2; }

                   	else if ( ((LA13_0 >= '\t' && LA13_0 <= '\n') || (LA13_0 >= '\f' && LA13_0 <= '\r') || LA13_0 == ' ') ) { s = 3; }

                   	else if ( ((LA13_0 >= '\u0000' && LA13_0 <= '\b') || LA13_0 == '\u000B' || (LA13_0 >= '\u000E' && LA13_0 <= '\u001F') || LA13_0 == '$' || LA13_0 == '&' || (LA13_0 >= '*' && LA13_0 <= '+') || LA13_0 == '-' || (LA13_0 >= '/' && LA13_0 <= '9') || LA13_0 == '=' || (LA13_0 >= '?' && LA13_0 <= 'Z') || LA13_0 == '\\' || (LA13_0 >= '^' && LA13_0 <= 'z') || LA13_0 == '|' || (LA13_0 >= '~' && LA13_0 <= '\uFFFF')) ) { s = 4; }

                   	else if ( (LA13_0 == '#') ) { s = 5; }

                   	else if ( (LA13_0 == ';') ) { s = 6; }

                   	else if ( (LA13_0 == ':') ) { s = 7; }

                   	else if ( (LA13_0 == '%') ) { s = 8; }

                   	else if ( (LA13_0 == '<') ) { s = 9; }

                   	else if ( (LA13_0 == '>') ) { s = 10; }

                   	else if ( (LA13_0 == '(') ) { s = 11; }

                   	else if ( (LA13_0 == ')') ) { s = 12; }

                   	else if ( (LA13_0 == '[') ) { s = 13; }

                   	else if ( (LA13_0 == ']') ) { s = 14; }

                   	else if ( (LA13_0 == '.') ) { s = 15; }

                   	else if ( (LA13_0 == ',') ) { s = 16; }

                   	else if ( (LA13_0 == '\'') ) { s = 17; }

                   	if ( s >= 0 ) return s;
                   	break;
        }
        if (state.backtracking > 0) {state.failed = true; return -1;}
        NoViableAltException nvae13 =
            new NoViableAltException(dfa.Description, 13, _s, input);
        dfa.Error(nvae13);
        throw nvae13;
    }
 
    
}
}