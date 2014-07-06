// $ANTLR 3.2 Sep 23, 2009 12:02:23 C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g 2009-11-21 23:53:09

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

using Antlr.Runtime.Tree;

namespace  Zapf.Parsing 
{
public partial class ZapParser : Parser
{
    public static readonly string[] tokenNames = new string[] 
	{
        "<invalid>", 
		"<EOR>", 
		"<DOWN>", 
		"<UP>", 
		"LLABEL", 
		"GLABEL", 
		"QUEST", 
		"TILDE", 
		"ARROW", 
		"ALIGN", 
		"BYTE", 
		"CHRSET", 
		"DEFSEG", 
		"END", 
		"ENDI", 
		"ENDSEG", 
		"ENDT", 
		"FALSE", 
		"FSTR", 
		"FUNCT", 
		"GSTR", 
		"GVAR", 
		"INSERT", 
		"LANG", 
		"LEN", 
		"NEW", 
		"OBJECT", 
		"OPTIONS", 
		"PAGE", 
		"PCSET", 
		"PDEF", 
		"PICFILE", 
		"PROP", 
		"SEGMENT", 
		"SEQ", 
		"STR", 
		"STRL", 
		"TABLE", 
		"TIME", 
		"TRUE", 
		"VOCBEG", 
		"VOCEND", 
		"WORD", 
		"ZWORD", 
		"DEBUG_ACTION", 
		"DEBUG_ARRAY", 
		"DEBUG_ATTR", 
		"DEBUG_CLASS", 
		"DEBUG_FAKE_ACTION", 
		"DEBUG_FILE", 
		"DEBUG_GLOBAL", 
		"DEBUG_LINE", 
		"DEBUG_MAP", 
		"DEBUG_OBJECT", 
		"DEBUG_PROP", 
		"DEBUG_ROUTINE", 
		"DEBUG_ROUTINE_END", 
		"EQUALS", 
		"COMMA", 
		"SLASH", 
		"BACKSLASH", 
		"RANGLE", 
		"PLUS", 
		"APOSTROPHE", 
		"COLON", 
		"DCOLON", 
		"NUM", 
		"NSYMBOL", 
		"SYMBOL", 
		"OPCODE", 
		"SYMBOL_OR_NUM", 
		"SPACE", 
		"WS", 
		"CRLF", 
		"STRING", 
		"COMMENT"
    };

    public const int VOCBEG = 40;
    public const int PICFILE = 31;
    public const int ENDSEG = 15;
    public const int PCSET = 29;
    public const int CRLF = 73;
    public const int STRL = 36;
    public const int NEW = 25;
    public const int LLABEL = 4;
    public const int TABLE = 37;
    public const int VOCEND = 41;
    public const int EQUALS = 57;
    public const int DEFSEG = 12;
    public const int SPACE = 71;
    public const int EOF = -1;
    public const int PROP = 32;
    public const int DEBUG_CLASS = 47;
    public const int DEBUG_OBJECT = 53;
    public const int DEBUG_ATTR = 46;
    public const int QUEST = 6;
    public const int WORD = 42;
    public const int STR = 35;
    public const int TIME = 38;
    public const int DCOLON = 65;
    public const int SLASH = 59;
    public const int INSERT = 22;
    public const int DEBUG_LINE = 51;
    public const int OBJECT = 26;
    public const int COMMA = 58;
    public const int SEQ = 34;
    public const int TILDE = 7;
    public const int PLUS = 62;
    public const int APOSTROPHE = 63;
    public const int ALIGN = 9;
    public const int RANGLE = 61;
    public const int DEBUG_ACTION = 44;
    public const int COMMENT = 75;
    public const int NSYMBOL = 67;
    public const int DEBUG_ROUTINE = 55;
    public const int SEGMENT = 33;
    public const int DEBUG_MAP = 52;
    public const int BYTE = 10;
    public const int GSTR = 20;
    public const int DEBUG_FAKE_ACTION = 48;
    public const int DEBUG_FILE = 49;
    public const int SYMBOL = 68;
    public const int CHRSET = 11;
    public const int DEBUG_ARRAY = 45;
    public const int GLABEL = 5;
    public const int DEBUG_ROUTINE_END = 56;
    public const int FSTR = 18;
    public const int OPCODE = 69;
    public const int TRUE = 39;
    public const int NUM = 66;
    public const int COLON = 64;
    public const int WS = 72;
    public const int PAGE = 28;
    public const int SYMBOL_OR_NUM = 70;
    public const int FUNCT = 19;
    public const int GVAR = 21;
    public const int ENDT = 16;
    public const int ARROW = 8;
    public const int LEN = 24;
    public const int PDEF = 30;
    public const int END = 13;
    public const int FALSE = 17;
    public const int ENDI = 14;
    public const int DEBUG_GLOBAL = 50;
    public const int ZWORD = 43;
    public const int OPTIONS = 27;
    public const int STRING = 74;
    public const int DEBUG_PROP = 54;
    public const int LANG = 23;
    public const int BACKSLASH = 60;

    // delegates
    // delegators



        public ZapParser(ITokenStream input)
    		: this(input, new RecognizerSharedState()) {
        }

        public ZapParser(ITokenStream input, RecognizerSharedState state)
    		: base(input, state) {
            InitializeCyclicDFAs();

             
        }
        
    protected ITreeAdaptor adaptor = new CommonTreeAdaptor();

    public ITreeAdaptor TreeAdaptor
    {
        get { return this.adaptor; }
        set {
    	this.adaptor = value;
    	}
    }

    override public string[] TokenNames {
		get { return ZapParser.tokenNames; }
    }

    override public string GrammarFileName {
		get { return "C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g"; }
    }


    	internal bool InformMode;


    public class file_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "file"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:156:1: file : ( CRLF )* ( label | ( label )? line ) ( ( CRLF )+ ( label | ( label )? line ) )* ( CRLF )* ;
    public ZapParser.file_return file() // throws RecognitionException [1]
    {   
        ZapParser.file_return retval = new ZapParser.file_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken CRLF1 = null;
        IToken CRLF5 = null;
        IToken CRLF9 = null;
        ZapParser.label_return label2 = default(ZapParser.label_return);

        ZapParser.label_return label3 = default(ZapParser.label_return);

        ZapParser.line_return line4 = default(ZapParser.line_return);

        ZapParser.label_return label6 = default(ZapParser.label_return);

        ZapParser.label_return label7 = default(ZapParser.label_return);

        ZapParser.line_return line8 = default(ZapParser.line_return);


        object CRLF1_tree=null;
        object CRLF5_tree=null;
        object CRLF9_tree=null;

        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:2: ( ( CRLF )* ( label | ( label )? line ) ( ( CRLF )+ ( label | ( label )? line ) )* ( CRLF )* )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:4: ( CRLF )* ( label | ( label )? line ) ( ( CRLF )+ ( label | ( label )? line ) )* ( CRLF )*
            {
            	root_0 = (object)adaptor.GetNilNode();

            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:8: ( CRLF )*
            	do 
            	{
            	    int alt1 = 2;
            	    int LA1_0 = input.LA(1);

            	    if ( (LA1_0 == CRLF) )
            	    {
            	        alt1 = 1;
            	    }


            	    switch (alt1) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:8: CRLF
            			    {
            			    	CRLF1=(IToken)Match(input,CRLF,FOLLOW_CRLF_in_file843); if (state.failed) return retval;

            			    }
            			    break;

            			default:
            			    goto loop1;
            	    }
            	} while (true);

            	loop1:
            		;	// Stops C# compiler whining that label 'loop1' has no statements

            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:11: ( label | ( label )? line )
            	int alt3 = 2;
            	int LA3_0 = input.LA(1);

            	if ( (LA3_0 == SYMBOL) )
            	{
            	    switch ( input.LA(2) ) 
            	    {
            	    case DCOLON:
            	    	{
            	        int LA3_3 = input.LA(3);

            	        if ( (LA3_3 == EOF || LA3_3 == CRLF) )
            	        {
            	            alt3 = 1;
            	        }
            	        else if ( (LA3_3 == BYTE || (LA3_3 >= END && LA3_3 <= ENDI) || (LA3_3 >= ENDT && LA3_3 <= INSERT) || (LA3_3 >= LEN && LA3_3 <= OBJECT) || LA3_3 == PROP || (LA3_3 >= STR && LA3_3 <= DEBUG_ROUTINE_END) || LA3_3 == APOSTROPHE || LA3_3 == NUM || (LA3_3 >= SYMBOL && LA3_3 <= OPCODE)) )
            	        {
            	            alt3 = 2;
            	        }
            	        else 
            	        {
            	            if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            	            NoViableAltException nvae_d3s3 =
            	                new NoViableAltException("", 3, 3, input);

            	            throw nvae_d3s3;
            	        }
            	        }
            	        break;
            	    case COLON:
            	    	{
            	        int LA3_4 = input.LA(3);

            	        if ( (LA3_4 == EOF || LA3_4 == CRLF) )
            	        {
            	            alt3 = 1;
            	        }
            	        else if ( (LA3_4 == BYTE || (LA3_4 >= END && LA3_4 <= ENDI) || (LA3_4 >= ENDT && LA3_4 <= INSERT) || (LA3_4 >= LEN && LA3_4 <= OBJECT) || LA3_4 == PROP || (LA3_4 >= STR && LA3_4 <= DEBUG_ROUTINE_END) || LA3_4 == APOSTROPHE || LA3_4 == NUM || (LA3_4 >= SYMBOL && LA3_4 <= OPCODE)) )
            	        {
            	            alt3 = 2;
            	        }
            	        else 
            	        {
            	            if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            	            NoViableAltException nvae_d3s4 =
            	                new NoViableAltException("", 3, 4, input);

            	            throw nvae_d3s4;
            	        }
            	        }
            	        break;
            	    case EOF:
            	    case QUEST:
            	    case ARROW:
            	    case EQUALS:
            	    case COMMA:
            	    case SLASH:
            	    case BACKSLASH:
            	    case RANGLE:
            	    case PLUS:
            	    case APOSTROPHE:
            	    case NUM:
            	    case SYMBOL:
            	    case OPCODE:
            	    case CRLF:
            	    case STRING:
            	    	{
            	        alt3 = 2;
            	        }
            	        break;
            	    	default:
            	    	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            	    	    NoViableAltException nvae_d3s1 =
            	    	        new NoViableAltException("", 3, 1, input);

            	    	    throw nvae_d3s1;
            	    }

            	}
            	else if ( (LA3_0 == BYTE || (LA3_0 >= END && LA3_0 <= ENDI) || (LA3_0 >= ENDT && LA3_0 <= INSERT) || (LA3_0 >= LEN && LA3_0 <= OBJECT) || LA3_0 == PROP || (LA3_0 >= STR && LA3_0 <= DEBUG_ROUTINE_END) || LA3_0 == APOSTROPHE || LA3_0 == NUM || LA3_0 == OPCODE) )
            	{
            	    alt3 = 2;
            	}
            	else 
            	{
            	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            	    NoViableAltException nvae_d3s0 =
            	        new NoViableAltException("", 3, 0, input);

            	    throw nvae_d3s0;
            	}
            	switch (alt3) 
            	{
            	    case 1 :
            	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:12: label
            	        {
            	        	PushFollow(FOLLOW_label_in_file848);
            	        	label2 = label();
            	        	state.followingStackPointer--;
            	        	if (state.failed) return retval;
            	        	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, label2.Tree);

            	        }
            	        break;
            	    case 2 :
            	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:20: ( label )? line
            	        {
            	        	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:20: ( label )?
            	        	int alt2 = 2;
            	        	int LA2_0 = input.LA(1);

            	        	if ( (LA2_0 == SYMBOL) )
            	        	{
            	        	    int LA2_1 = input.LA(2);

            	        	    if ( ((LA2_1 >= COLON && LA2_1 <= DCOLON)) )
            	        	    {
            	        	        alt2 = 1;
            	        	    }
            	        	}
            	        	switch (alt2) 
            	        	{
            	        	    case 1 :
            	        	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:20: label
            	        	        {
            	        	        	PushFollow(FOLLOW_label_in_file852);
            	        	        	label3 = label();
            	        	        	state.followingStackPointer--;
            	        	        	if (state.failed) return retval;
            	        	        	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, label3.Tree);

            	        	        }
            	        	        break;

            	        	}

            	        	PushFollow(FOLLOW_line_in_file855);
            	        	line4 = line();
            	        	state.followingStackPointer--;
            	        	if (state.failed) return retval;
            	        	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, line4.Tree);

            	        }
            	        break;

            	}

            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:33: ( ( CRLF )+ ( label | ( label )? line ) )*
            	do 
            	{
            	    int alt7 = 2;
            	    alt7 = dfa7.Predict(input);
            	    switch (alt7) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:34: ( CRLF )+ ( label | ( label )? line )
            			    {
            			    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:38: ( CRLF )+
            			    	int cnt4 = 0;
            			    	do 
            			    	{
            			    	    int alt4 = 2;
            			    	    int LA4_0 = input.LA(1);

            			    	    if ( (LA4_0 == CRLF) )
            			    	    {
            			    	        alt4 = 1;
            			    	    }


            			    	    switch (alt4) 
            			    		{
            			    			case 1 :
            			    			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:38: CRLF
            			    			    {
            			    			    	CRLF5=(IToken)Match(input,CRLF,FOLLOW_CRLF_in_file859); if (state.failed) return retval;

            			    			    }
            			    			    break;

            			    			default:
            			    			    if ( cnt4 >= 1 ) goto loop4;
            			    			    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            			    		            EarlyExitException eee4 =
            			    		                new EarlyExitException(4, input);
            			    		            throw eee4;
            			    	    }
            			    	    cnt4++;
            			    	} while (true);

            			    	loop4:
            			    		;	// Stops C# compiler whining that label 'loop4' has no statements

            			    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:41: ( label | ( label )? line )
            			    	int alt6 = 2;
            			    	int LA6_0 = input.LA(1);

            			    	if ( (LA6_0 == SYMBOL) )
            			    	{
            			    	    switch ( input.LA(2) ) 
            			    	    {
            			    	    case EOF:
            			    	    case QUEST:
            			    	    case ARROW:
            			    	    case EQUALS:
            			    	    case COMMA:
            			    	    case SLASH:
            			    	    case BACKSLASH:
            			    	    case RANGLE:
            			    	    case PLUS:
            			    	    case APOSTROPHE:
            			    	    case NUM:
            			    	    case SYMBOL:
            			    	    case OPCODE:
            			    	    case CRLF:
            			    	    case STRING:
            			    	    	{
            			    	        alt6 = 2;
            			    	        }
            			    	        break;
            			    	    case DCOLON:
            			    	    	{
            			    	        int LA6_3 = input.LA(3);

            			    	        if ( (LA6_3 == EOF || LA6_3 == CRLF) )
            			    	        {
            			    	            alt6 = 1;
            			    	        }
            			    	        else if ( (LA6_3 == BYTE || (LA6_3 >= END && LA6_3 <= ENDI) || (LA6_3 >= ENDT && LA6_3 <= INSERT) || (LA6_3 >= LEN && LA6_3 <= OBJECT) || LA6_3 == PROP || (LA6_3 >= STR && LA6_3 <= DEBUG_ROUTINE_END) || LA6_3 == APOSTROPHE || LA6_3 == NUM || (LA6_3 >= SYMBOL && LA6_3 <= OPCODE)) )
            			    	        {
            			    	            alt6 = 2;
            			    	        }
            			    	        else 
            			    	        {
            			    	            if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            			    	            NoViableAltException nvae_d6s3 =
            			    	                new NoViableAltException("", 6, 3, input);

            			    	            throw nvae_d6s3;
            			    	        }
            			    	        }
            			    	        break;
            			    	    case COLON:
            			    	    	{
            			    	        int LA6_4 = input.LA(3);

            			    	        if ( (LA6_4 == EOF || LA6_4 == CRLF) )
            			    	        {
            			    	            alt6 = 1;
            			    	        }
            			    	        else if ( (LA6_4 == BYTE || (LA6_4 >= END && LA6_4 <= ENDI) || (LA6_4 >= ENDT && LA6_4 <= INSERT) || (LA6_4 >= LEN && LA6_4 <= OBJECT) || LA6_4 == PROP || (LA6_4 >= STR && LA6_4 <= DEBUG_ROUTINE_END) || LA6_4 == APOSTROPHE || LA6_4 == NUM || (LA6_4 >= SYMBOL && LA6_4 <= OPCODE)) )
            			    	        {
            			    	            alt6 = 2;
            			    	        }
            			    	        else 
            			    	        {
            			    	            if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            			    	            NoViableAltException nvae_d6s4 =
            			    	                new NoViableAltException("", 6, 4, input);

            			    	            throw nvae_d6s4;
            			    	        }
            			    	        }
            			    	        break;
            			    	    	default:
            			    	    	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            			    	    	    NoViableAltException nvae_d6s1 =
            			    	    	        new NoViableAltException("", 6, 1, input);

            			    	    	    throw nvae_d6s1;
            			    	    }

            			    	}
            			    	else if ( (LA6_0 == BYTE || (LA6_0 >= END && LA6_0 <= ENDI) || (LA6_0 >= ENDT && LA6_0 <= INSERT) || (LA6_0 >= LEN && LA6_0 <= OBJECT) || LA6_0 == PROP || (LA6_0 >= STR && LA6_0 <= DEBUG_ROUTINE_END) || LA6_0 == APOSTROPHE || LA6_0 == NUM || LA6_0 == OPCODE) )
            			    	{
            			    	    alt6 = 2;
            			    	}
            			    	else 
            			    	{
            			    	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            			    	    NoViableAltException nvae_d6s0 =
            			    	        new NoViableAltException("", 6, 0, input);

            			    	    throw nvae_d6s0;
            			    	}
            			    	switch (alt6) 
            			    	{
            			    	    case 1 :
            			    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:42: label
            			    	        {
            			    	        	PushFollow(FOLLOW_label_in_file864);
            			    	        	label6 = label();
            			    	        	state.followingStackPointer--;
            			    	        	if (state.failed) return retval;
            			    	        	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, label6.Tree);

            			    	        }
            			    	        break;
            			    	    case 2 :
            			    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:50: ( label )? line
            			    	        {
            			    	        	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:50: ( label )?
            			    	        	int alt5 = 2;
            			    	        	int LA5_0 = input.LA(1);

            			    	        	if ( (LA5_0 == SYMBOL) )
            			    	        	{
            			    	        	    int LA5_1 = input.LA(2);

            			    	        	    if ( ((LA5_1 >= COLON && LA5_1 <= DCOLON)) )
            			    	        	    {
            			    	        	        alt5 = 1;
            			    	        	    }
            			    	        	}
            			    	        	switch (alt5) 
            			    	        	{
            			    	        	    case 1 :
            			    	        	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:50: label
            			    	        	        {
            			    	        	        	PushFollow(FOLLOW_label_in_file868);
            			    	        	        	label7 = label();
            			    	        	        	state.followingStackPointer--;
            			    	        	        	if (state.failed) return retval;
            			    	        	        	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, label7.Tree);

            			    	        	        }
            			    	        	        break;

            			    	        	}

            			    	        	PushFollow(FOLLOW_line_in_file871);
            			    	        	line8 = line();
            			    	        	state.followingStackPointer--;
            			    	        	if (state.failed) return retval;
            			    	        	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, line8.Tree);

            			    	        }
            			    	        break;

            			    	}


            			    }
            			    break;

            			default:
            			    goto loop7;
            	    }
            	} while (true);

            	loop7:
            		;	// Stops C# compiler whining that label 'loop7' has no statements

            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:69: ( CRLF )*
            	do 
            	{
            	    int alt8 = 2;
            	    int LA8_0 = input.LA(1);

            	    if ( (LA8_0 == CRLF) )
            	    {
            	        alt8 = 1;
            	    }


            	    switch (alt8) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:157:69: CRLF
            			    {
            			    	CRLF9=(IToken)Match(input,CRLF,FOLLOW_CRLF_in_file876); if (state.failed) return retval;

            			    }
            			    break;

            			default:
            			    goto loop8;
            	    }
            	} while (true);

            	loop8:
            		;	// Stops C# compiler whining that label 'loop8' has no statements


            }

            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "file"

    public class label_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "label"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:159:1: label : s= SYMBOL ( DCOLON -> ^( GLABEL[$s] ) | COLON -> ^( LLABEL[$s] ) ) ;
    public ZapParser.label_return label() // throws RecognitionException [1]
    {   
        ZapParser.label_return retval = new ZapParser.label_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken s = null;
        IToken DCOLON10 = null;
        IToken COLON11 = null;

        object s_tree=null;
        object DCOLON10_tree=null;
        object COLON11_tree=null;
        RewriteRuleTokenStream stream_COLON = new RewriteRuleTokenStream(adaptor,"token COLON");
        RewriteRuleTokenStream stream_DCOLON = new RewriteRuleTokenStream(adaptor,"token DCOLON");
        RewriteRuleTokenStream stream_SYMBOL = new RewriteRuleTokenStream(adaptor,"token SYMBOL");

        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:159:7: (s= SYMBOL ( DCOLON -> ^( GLABEL[$s] ) | COLON -> ^( LLABEL[$s] ) ) )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:159:9: s= SYMBOL ( DCOLON -> ^( GLABEL[$s] ) | COLON -> ^( LLABEL[$s] ) )
            {
            	s=(IToken)Match(input,SYMBOL,FOLLOW_SYMBOL_in_label888); if (state.failed) return retval; 
            	if ( (state.backtracking==0) ) stream_SYMBOL.Add(s);

            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:160:3: ( DCOLON -> ^( GLABEL[$s] ) | COLON -> ^( LLABEL[$s] ) )
            	int alt9 = 2;
            	int LA9_0 = input.LA(1);

            	if ( (LA9_0 == DCOLON) )
            	{
            	    alt9 = 1;
            	}
            	else if ( (LA9_0 == COLON) )
            	{
            	    alt9 = 2;
            	}
            	else 
            	{
            	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            	    NoViableAltException nvae_d9s0 =
            	        new NoViableAltException("", 9, 0, input);

            	    throw nvae_d9s0;
            	}
            	switch (alt9) 
            	{
            	    case 1 :
            	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:160:5: DCOLON
            	        {
            	        	DCOLON10=(IToken)Match(input,DCOLON,FOLLOW_DCOLON_in_label894); if (state.failed) return retval; 
            	        	if ( (state.backtracking==0) ) stream_DCOLON.Add(DCOLON10);



            	        	// AST REWRITE
            	        	// elements:          
            	        	// token labels:      
            	        	// rule labels:       retval
            	        	// token list labels: 
            	        	// rule list labels:  
            	        	// wildcard labels: 
            	        	if ( (state.backtracking==0) ) {
            	        	retval.Tree = root_0;
            	        	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

            	        	root_0 = (object)adaptor.GetNilNode();
            	        	// 160:12: -> ^( GLABEL[$s] )
            	        	{
            	        	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:160:15: ^( GLABEL[$s] )
            	        	    {
            	        	    object root_1 = (object)adaptor.GetNilNode();
            	        	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(GLABEL, s), root_1);

            	        	    adaptor.AddChild(root_0, root_1);
            	        	    }

            	        	}

            	        	retval.Tree = root_0;retval.Tree = root_0;}
            	        }
            	        break;
            	    case 2 :
            	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:161:5: COLON
            	        {
            	        	COLON11=(IToken)Match(input,COLON,FOLLOW_COLON_in_label907); if (state.failed) return retval; 
            	        	if ( (state.backtracking==0) ) stream_COLON.Add(COLON11);



            	        	// AST REWRITE
            	        	// elements:          
            	        	// token labels:      
            	        	// rule labels:       retval
            	        	// token list labels: 
            	        	// rule list labels:  
            	        	// wildcard labels: 
            	        	if ( (state.backtracking==0) ) {
            	        	retval.Tree = root_0;
            	        	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

            	        	root_0 = (object)adaptor.GetNilNode();
            	        	// 161:12: -> ^( LLABEL[$s] )
            	        	{
            	        	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:161:15: ^( LLABEL[$s] )
            	        	    {
            	        	    object root_1 = (object)adaptor.GetNilNode();
            	        	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(LLABEL, s), root_1);

            	        	    adaptor.AddChild(root_0, root_1);
            	        	    }

            	        	}

            	        	retval.Tree = root_0;retval.Tree = root_0;}
            	        }
            	        break;

            	}


            }

            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "label"

    public class symbol_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "symbol"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:164:1: symbol : ( SYMBOL | OPCODE -> SYMBOL[$OPCODE] );
    public ZapParser.symbol_return symbol() // throws RecognitionException [1]
    {   
        ZapParser.symbol_return retval = new ZapParser.symbol_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken SYMBOL12 = null;
        IToken OPCODE13 = null;

        object SYMBOL12_tree=null;
        object OPCODE13_tree=null;
        RewriteRuleTokenStream stream_OPCODE = new RewriteRuleTokenStream(adaptor,"token OPCODE");

        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:164:8: ( SYMBOL | OPCODE -> SYMBOL[$OPCODE] )
            int alt10 = 2;
            int LA10_0 = input.LA(1);

            if ( (LA10_0 == SYMBOL) )
            {
                alt10 = 1;
            }
            else if ( (LA10_0 == OPCODE) )
            {
                alt10 = 2;
            }
            else 
            {
                if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                NoViableAltException nvae_d10s0 =
                    new NoViableAltException("", 10, 0, input);

                throw nvae_d10s0;
            }
            switch (alt10) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:164:10: SYMBOL
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	SYMBOL12=(IToken)Match(input,SYMBOL,FOLLOW_SYMBOL_in_symbol927); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{SYMBOL12_tree = (object)adaptor.Create(SYMBOL12);
                    		adaptor.AddChild(root_0, SYMBOL12_tree);
                    	}

                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:165:4: OPCODE
                    {
                    	OPCODE13=(IToken)Match(input,OPCODE,FOLLOW_OPCODE_in_symbol932); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_OPCODE.Add(OPCODE13);



                    	// AST REWRITE
                    	// elements:          
                    	// token labels:      
                    	// rule labels:       retval
                    	// token list labels: 
                    	// rule list labels:  
                    	// wildcard labels: 
                    	if ( (state.backtracking==0) ) {
                    	retval.Tree = root_0;
                    	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	root_0 = (object)adaptor.GetNilNode();
                    	// 165:12: -> SYMBOL[$OPCODE]
                    	{
                    	    adaptor.AddChild(root_0, (object)adaptor.Create(SYMBOL, OPCODE13));

                    	}

                    	retval.Tree = root_0;retval.Tree = root_0;}
                    }
                    break;

            }
            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "symbol"

    public class line_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "line"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:168:1: line : ( OPCODE operands | OPCODE STRING | SYMBOL ( -> ^( WORD SYMBOL ) | ( expr | STRING | SLASH | BACKSLASH | RANGLE | QUEST | ARROW )=> ( operands -> ^( SYMBOL operands ) | STRING -> ^( SYMBOL STRING ) ) | EQUALS expr -> ^( EQUALS SYMBOL expr ) ) | meta_directive | data_directive | funct_directive | table_directive | debug_directive );
    public ZapParser.line_return line() // throws RecognitionException [1]
    {   
        ZapParser.line_return retval = new ZapParser.line_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken OPCODE14 = null;
        IToken OPCODE16 = null;
        IToken STRING17 = null;
        IToken SYMBOL18 = null;
        IToken STRING20 = null;
        IToken EQUALS21 = null;
        ZapParser.operands_return operands15 = default(ZapParser.operands_return);

        ZapParser.operands_return operands19 = default(ZapParser.operands_return);

        ZapParser.expr_return expr22 = default(ZapParser.expr_return);

        ZapParser.meta_directive_return meta_directive23 = default(ZapParser.meta_directive_return);

        ZapParser.data_directive_return data_directive24 = default(ZapParser.data_directive_return);

        ZapParser.funct_directive_return funct_directive25 = default(ZapParser.funct_directive_return);

        ZapParser.table_directive_return table_directive26 = default(ZapParser.table_directive_return);

        ZapParser.debug_directive_return debug_directive27 = default(ZapParser.debug_directive_return);


        object OPCODE14_tree=null;
        object OPCODE16_tree=null;
        object STRING17_tree=null;
        object SYMBOL18_tree=null;
        object STRING20_tree=null;
        object EQUALS21_tree=null;
        RewriteRuleTokenStream stream_EQUALS = new RewriteRuleTokenStream(adaptor,"token EQUALS");
        RewriteRuleTokenStream stream_SYMBOL = new RewriteRuleTokenStream(adaptor,"token SYMBOL");
        RewriteRuleTokenStream stream_STRING = new RewriteRuleTokenStream(adaptor,"token STRING");
        RewriteRuleSubtreeStream stream_operands = new RewriteRuleSubtreeStream(adaptor,"rule operands");
        RewriteRuleSubtreeStream stream_expr = new RewriteRuleSubtreeStream(adaptor,"rule expr");
        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:169:2: ( OPCODE operands | OPCODE STRING | SYMBOL ( -> ^( WORD SYMBOL ) | ( expr | STRING | SLASH | BACKSLASH | RANGLE | QUEST | ARROW )=> ( operands -> ^( SYMBOL operands ) | STRING -> ^( SYMBOL STRING ) ) | EQUALS expr -> ^( EQUALS SYMBOL expr ) ) | meta_directive | data_directive | funct_directive | table_directive | debug_directive )
            int alt13 = 8;
            alt13 = dfa13.Predict(input);
            switch (alt13) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:169:4: OPCODE operands
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	OPCODE14=(IToken)Match(input,OPCODE,FOLLOW_OPCODE_in_line949); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{OPCODE14_tree = (object)adaptor.Create(OPCODE14);
                    		root_0 = (object)adaptor.BecomeRoot(OPCODE14_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_operands_in_line952);
                    	operands15 = operands();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, operands15.Tree);

                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:170:4: OPCODE STRING
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	OPCODE16=(IToken)Match(input,OPCODE,FOLLOW_OPCODE_in_line957); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{OPCODE16_tree = (object)adaptor.Create(OPCODE16);
                    		root_0 = (object)adaptor.BecomeRoot(OPCODE16_tree, root_0);
                    	}
                    	STRING17=(IToken)Match(input,STRING,FOLLOW_STRING_in_line960); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING17_tree = (object)adaptor.Create(STRING17);
                    		adaptor.AddChild(root_0, STRING17_tree);
                    	}

                    }
                    break;
                case 3 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:171:4: SYMBOL ( -> ^( WORD SYMBOL ) | ( expr | STRING | SLASH | BACKSLASH | RANGLE | QUEST | ARROW )=> ( operands -> ^( SYMBOL operands ) | STRING -> ^( SYMBOL STRING ) ) | EQUALS expr -> ^( EQUALS SYMBOL expr ) )
                    {
                    	SYMBOL18=(IToken)Match(input,SYMBOL,FOLLOW_SYMBOL_in_line965); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_SYMBOL.Add(SYMBOL18);

                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:172:3: ( -> ^( WORD SYMBOL ) | ( expr | STRING | SLASH | BACKSLASH | RANGLE | QUEST | ARROW )=> ( operands -> ^( SYMBOL operands ) | STRING -> ^( SYMBOL STRING ) ) | EQUALS expr -> ^( EQUALS SYMBOL expr ) )
                    	int alt12 = 3;
                    	alt12 = dfa12.Predict(input);
                    	switch (alt12) 
                    	{
                    	    case 1 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:172:9: 
                    	        {

                    	        	// AST REWRITE
                    	        	// elements:          SYMBOL
                    	        	// token labels:      
                    	        	// rule labels:       retval
                    	        	// token list labels: 
                    	        	// rule list labels:  
                    	        	// wildcard labels: 
                    	        	if ( (state.backtracking==0) ) {
                    	        	retval.Tree = root_0;
                    	        	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	        	root_0 = (object)adaptor.GetNilNode();
                    	        	// 172:9: -> ^( WORD SYMBOL )
                    	        	{
                    	        	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:172:12: ^( WORD SYMBOL )
                    	        	    {
                    	        	    object root_1 = (object)adaptor.GetNilNode();
                    	        	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(WORD, "WORD"), root_1);

                    	        	    adaptor.AddChild(root_1, stream_SYMBOL.NextNode());

                    	        	    adaptor.AddChild(root_0, root_1);
                    	        	    }

                    	        	}

                    	        	retval.Tree = root_0;retval.Tree = root_0;}
                    	        }
                    	        break;
                    	    case 2 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:173:5: ( expr | STRING | SLASH | BACKSLASH | RANGLE | QUEST | ARROW )=> ( operands -> ^( SYMBOL operands ) | STRING -> ^( SYMBOL STRING ) )
                    	        {
                    	        	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:174:4: ( operands -> ^( SYMBOL operands ) | STRING -> ^( SYMBOL STRING ) )
                    	        	int alt11 = 2;
                    	        	int LA11_0 = input.LA(1);

                    	        	if ( (LA11_0 == EOF || LA11_0 == QUEST || LA11_0 == ARROW || (LA11_0 >= SLASH && LA11_0 <= RANGLE) || LA11_0 == APOSTROPHE || LA11_0 == NUM || (LA11_0 >= SYMBOL && LA11_0 <= OPCODE) || LA11_0 == CRLF) )
                    	        	{
                    	        	    alt11 = 1;
                    	        	}
                    	        	else if ( (LA11_0 == STRING) )
                    	        	{
                    	        	    alt11 = 2;
                    	        	}
                    	        	else 
                    	        	{
                    	        	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        	    NoViableAltException nvae_d11s0 =
                    	        	        new NoViableAltException("", 11, 0, input);

                    	        	    throw nvae_d11s0;
                    	        	}
                    	        	switch (alt11) 
                    	        	{
                    	        	    case 1 :
                    	        	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:174:6: operands
                    	        	        {
                    	        	        	PushFollow(FOLLOW_operands_in_line1021);
                    	        	        	operands19 = operands();
                    	        	        	state.followingStackPointer--;
                    	        	        	if (state.failed) return retval;
                    	        	        	if ( (state.backtracking==0) ) stream_operands.Add(operands19.Tree);


                    	        	        	// AST REWRITE
                    	        	        	// elements:          operands, SYMBOL
                    	        	        	// token labels:      
                    	        	        	// rule labels:       retval
                    	        	        	// token list labels: 
                    	        	        	// rule list labels:  
                    	        	        	// wildcard labels: 
                    	        	        	if ( (state.backtracking==0) ) {
                    	        	        	retval.Tree = root_0;
                    	        	        	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	        	        	root_0 = (object)adaptor.GetNilNode();
                    	        	        	// 174:17: -> ^( SYMBOL operands )
                    	        	        	{
                    	        	        	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:174:20: ^( SYMBOL operands )
                    	        	        	    {
                    	        	        	    object root_1 = (object)adaptor.GetNilNode();
                    	        	        	    root_1 = (object)adaptor.BecomeRoot(stream_SYMBOL.NextNode(), root_1);

                    	        	        	    adaptor.AddChild(root_1, stream_operands.NextTree());

                    	        	        	    adaptor.AddChild(root_0, root_1);
                    	        	        	    }

                    	        	        	}

                    	        	        	retval.Tree = root_0;retval.Tree = root_0;}
                    	        	        }
                    	        	        break;
                    	        	    case 2 :
                    	        	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:175:6: STRING
                    	        	        {
                    	        	        	STRING20=(IToken)Match(input,STRING,FOLLOW_STRING_in_line1038); if (state.failed) return retval; 
                    	        	        	if ( (state.backtracking==0) ) stream_STRING.Add(STRING20);



                    	        	        	// AST REWRITE
                    	        	        	// elements:          SYMBOL, STRING
                    	        	        	// token labels:      
                    	        	        	// rule labels:       retval
                    	        	        	// token list labels: 
                    	        	        	// rule list labels:  
                    	        	        	// wildcard labels: 
                    	        	        	if ( (state.backtracking==0) ) {
                    	        	        	retval.Tree = root_0;
                    	        	        	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	        	        	root_0 = (object)adaptor.GetNilNode();
                    	        	        	// 175:15: -> ^( SYMBOL STRING )
                    	        	        	{
                    	        	        	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:175:18: ^( SYMBOL STRING )
                    	        	        	    {
                    	        	        	    object root_1 = (object)adaptor.GetNilNode();
                    	        	        	    root_1 = (object)adaptor.BecomeRoot(stream_SYMBOL.NextNode(), root_1);

                    	        	        	    adaptor.AddChild(root_1, stream_STRING.NextNode());

                    	        	        	    adaptor.AddChild(root_0, root_1);
                    	        	        	    }

                    	        	        	}

                    	        	        	retval.Tree = root_0;retval.Tree = root_0;}
                    	        	        }
                    	        	        break;

                    	        	}


                    	        }
                    	        break;
                    	    case 3 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:177:5: EQUALS expr
                    	        {
                    	        	EQUALS21=(IToken)Match(input,EQUALS,FOLLOW_EQUALS_in_line1059); if (state.failed) return retval; 
                    	        	if ( (state.backtracking==0) ) stream_EQUALS.Add(EQUALS21);

                    	        	PushFollow(FOLLOW_expr_in_line1061);
                    	        	expr22 = expr();
                    	        	state.followingStackPointer--;
                    	        	if (state.failed) return retval;
                    	        	if ( (state.backtracking==0) ) stream_expr.Add(expr22.Tree);


                    	        	// AST REWRITE
                    	        	// elements:          EQUALS, expr, SYMBOL
                    	        	// token labels:      
                    	        	// rule labels:       retval
                    	        	// token list labels: 
                    	        	// rule list labels:  
                    	        	// wildcard labels: 
                    	        	if ( (state.backtracking==0) ) {
                    	        	retval.Tree = root_0;
                    	        	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	        	root_0 = (object)adaptor.GetNilNode();
                    	        	// 177:20: -> ^( EQUALS SYMBOL expr )
                    	        	{
                    	        	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:177:23: ^( EQUALS SYMBOL expr )
                    	        	    {
                    	        	    object root_1 = (object)adaptor.GetNilNode();
                    	        	    root_1 = (object)adaptor.BecomeRoot(stream_EQUALS.NextNode(), root_1);

                    	        	    adaptor.AddChild(root_1, stream_SYMBOL.NextNode());
                    	        	    adaptor.AddChild(root_1, stream_expr.NextTree());

                    	        	    adaptor.AddChild(root_0, root_1);
                    	        	    }

                    	        	}

                    	        	retval.Tree = root_0;retval.Tree = root_0;}
                    	        }
                    	        break;

                    	}


                    }
                    break;
                case 4 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:179:4: meta_directive
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	PushFollow(FOLLOW_meta_directive_in_line1083);
                    	meta_directive23 = meta_directive();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, meta_directive23.Tree);

                    }
                    break;
                case 5 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:180:4: data_directive
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	PushFollow(FOLLOW_data_directive_in_line1088);
                    	data_directive24 = data_directive();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, data_directive24.Tree);

                    }
                    break;
                case 6 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:181:4: funct_directive
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	PushFollow(FOLLOW_funct_directive_in_line1093);
                    	funct_directive25 = funct_directive();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, funct_directive25.Tree);

                    }
                    break;
                case 7 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:182:4: table_directive
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	PushFollow(FOLLOW_table_directive_in_line1098);
                    	table_directive26 = table_directive();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, table_directive26.Tree);

                    }
                    break;
                case 8 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:183:4: debug_directive
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	PushFollow(FOLLOW_debug_directive_in_line1103);
                    	debug_directive27 = debug_directive();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, debug_directive27.Tree);

                    }
                    break;

            }
            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "line"

    public class operands_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "operands"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:186:1: operands : ({...}? inf_operands | {...}? zap_operands ) ;
    public ZapParser.operands_return operands() // throws RecognitionException [1]
    {   
        ZapParser.operands_return retval = new ZapParser.operands_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        ZapParser.inf_operands_return inf_operands28 = default(ZapParser.inf_operands_return);

        ZapParser.zap_operands_return zap_operands29 = default(ZapParser.zap_operands_return);



        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:186:9: ( ({...}? inf_operands | {...}? zap_operands ) )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:186:11: ({...}? inf_operands | {...}? zap_operands )
            {
            	root_0 = (object)adaptor.GetNilNode();

            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:186:11: ({...}? inf_operands | {...}? zap_operands )
            	int alt14 = 2;
            	alt14 = dfa14.Predict(input);
            	switch (alt14) 
            	{
            	    case 1 :
            	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:186:13: {...}? inf_operands
            	        {
            	        	if ( !((InformMode)) ) 
            	        	{
            	        	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            	        	    throw new FailedPredicateException(input, "operands", "InformMode");
            	        	}
            	        	PushFollow(FOLLOW_inf_operands_in_operands1116);
            	        	inf_operands28 = inf_operands();
            	        	state.followingStackPointer--;
            	        	if (state.failed) return retval;
            	        	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, inf_operands28.Tree);

            	        }
            	        break;
            	    case 2 :
            	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:187:5: {...}? zap_operands
            	        {
            	        	if ( !((!InformMode)) ) 
            	        	{
            	        	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            	        	    throw new FailedPredicateException(input, "operands", "!InformMode");
            	        	}
            	        	PushFollow(FOLLOW_zap_operands_in_operands1124);
            	        	zap_operands29 = zap_operands();
            	        	state.followingStackPointer--;
            	        	if (state.failed) return retval;
            	        	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, zap_operands29.Tree);

            	        }
            	        break;

            	}


            }

            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "operands"

    public class inf_operands_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "inf_operands"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:190:1: inf_operands : ( expr )* ( inf_spec_operand )* ;
    public ZapParser.inf_operands_return inf_operands() // throws RecognitionException [1]
    {   
        ZapParser.inf_operands_return retval = new ZapParser.inf_operands_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        ZapParser.expr_return expr30 = default(ZapParser.expr_return);

        ZapParser.inf_spec_operand_return inf_spec_operand31 = default(ZapParser.inf_spec_operand_return);



        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:191:2: ( ( expr )* ( inf_spec_operand )* )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:191:4: ( expr )* ( inf_spec_operand )*
            {
            	root_0 = (object)adaptor.GetNilNode();

            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:191:4: ( expr )*
            	do 
            	{
            	    int alt15 = 2;
            	    int LA15_0 = input.LA(1);

            	    if ( (LA15_0 == APOSTROPHE || LA15_0 == NUM || (LA15_0 >= SYMBOL && LA15_0 <= OPCODE)) )
            	    {
            	        alt15 = 1;
            	    }


            	    switch (alt15) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:191:4: expr
            			    {
            			    	PushFollow(FOLLOW_expr_in_inf_operands1136);
            			    	expr30 = expr();
            			    	state.followingStackPointer--;
            			    	if (state.failed) return retval;
            			    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr30.Tree);

            			    }
            			    break;

            			default:
            			    goto loop15;
            	    }
            	} while (true);

            	loop15:
            		;	// Stops C# compiler whining that label 'loop15' has no statements

            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:191:10: ( inf_spec_operand )*
            	do 
            	{
            	    int alt16 = 2;
            	    int LA16_0 = input.LA(1);

            	    if ( (LA16_0 == QUEST || LA16_0 == ARROW) )
            	    {
            	        alt16 = 1;
            	    }


            	    switch (alt16) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:191:10: inf_spec_operand
            			    {
            			    	PushFollow(FOLLOW_inf_spec_operand_in_inf_operands1139);
            			    	inf_spec_operand31 = inf_spec_operand();
            			    	state.followingStackPointer--;
            			    	if (state.failed) return retval;
            			    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, inf_spec_operand31.Tree);

            			    }
            			    break;

            			default:
            			    goto loop16;
            	    }
            	} while (true);

            	loop16:
            		;	// Stops C# compiler whining that label 'loop16' has no statements


            }

            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "inf_operands"

    protected class inf_spec_operand_scope 
    {
        protected internal bool neg;
    }
    protected Stack inf_spec_operand_stack = new Stack();

    public class inf_spec_operand_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "inf_spec_operand"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:194:1: inf_spec_operand : ( QUEST ( TILDE )? ( SYMBOL -> SYMBOL | OPCODE ({...}? -> TRUE[$OPCODE] | {...}? -> FALSE[$OPCODE] ) ) ({...}? -> ^( BACKSLASH[$QUEST] $inf_spec_operand) | {...}? -> ^( SLASH[$QUEST] $inf_spec_operand) ) | ARROW symbol -> ^( RANGLE[$ARROW] symbol ) );
    public ZapParser.inf_spec_operand_return inf_spec_operand() // throws RecognitionException [1]
    {   
        inf_spec_operand_stack.Push(new inf_spec_operand_scope());
        ZapParser.inf_spec_operand_return retval = new ZapParser.inf_spec_operand_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken QUEST32 = null;
        IToken TILDE33 = null;
        IToken SYMBOL34 = null;
        IToken OPCODE35 = null;
        IToken ARROW36 = null;
        ZapParser.symbol_return symbol37 = default(ZapParser.symbol_return);


        object QUEST32_tree=null;
        object TILDE33_tree=null;
        object SYMBOL34_tree=null;
        object OPCODE35_tree=null;
        object ARROW36_tree=null;
        RewriteRuleTokenStream stream_ARROW = new RewriteRuleTokenStream(adaptor,"token ARROW");
        RewriteRuleTokenStream stream_SYMBOL = new RewriteRuleTokenStream(adaptor,"token SYMBOL");
        RewriteRuleTokenStream stream_OPCODE = new RewriteRuleTokenStream(adaptor,"token OPCODE");
        RewriteRuleTokenStream stream_QUEST = new RewriteRuleTokenStream(adaptor,"token QUEST");
        RewriteRuleTokenStream stream_TILDE = new RewriteRuleTokenStream(adaptor,"token TILDE");
        RewriteRuleSubtreeStream stream_symbol = new RewriteRuleSubtreeStream(adaptor,"rule symbol");
        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:196:2: ( QUEST ( TILDE )? ( SYMBOL -> SYMBOL | OPCODE ({...}? -> TRUE[$OPCODE] | {...}? -> FALSE[$OPCODE] ) ) ({...}? -> ^( BACKSLASH[$QUEST] $inf_spec_operand) | {...}? -> ^( SLASH[$QUEST] $inf_spec_operand) ) | ARROW symbol -> ^( RANGLE[$ARROW] symbol ) )
            int alt21 = 2;
            int LA21_0 = input.LA(1);

            if ( (LA21_0 == QUEST) )
            {
                alt21 = 1;
            }
            else if ( (LA21_0 == ARROW) )
            {
                alt21 = 2;
            }
            else 
            {
                if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                NoViableAltException nvae_d21s0 =
                    new NoViableAltException("", 21, 0, input);

                throw nvae_d21s0;
            }
            switch (alt21) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:196:4: QUEST ( TILDE )? ( SYMBOL -> SYMBOL | OPCODE ({...}? -> TRUE[$OPCODE] | {...}? -> FALSE[$OPCODE] ) ) ({...}? -> ^( BACKSLASH[$QUEST] $inf_spec_operand) | {...}? -> ^( SLASH[$QUEST] $inf_spec_operand) )
                    {
                    	QUEST32=(IToken)Match(input,QUEST,FOLLOW_QUEST_in_inf_spec_operand1155); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_QUEST.Add(QUEST32);

                    	if ( (state.backtracking==0) )
                    	{
                    	  ((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg =  false;
                    	}
                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:197:3: ( TILDE )?
                    	int alt17 = 2;
                    	int LA17_0 = input.LA(1);

                    	if ( (LA17_0 == TILDE) )
                    	{
                    	    alt17 = 1;
                    	}
                    	switch (alt17) 
                    	{
                    	    case 1 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:197:4: TILDE
                    	        {
                    	        	TILDE33=(IToken)Match(input,TILDE,FOLLOW_TILDE_in_inf_spec_operand1162); if (state.failed) return retval; 
                    	        	if ( (state.backtracking==0) ) stream_TILDE.Add(TILDE33);

                    	        	if ( (state.backtracking==0) )
                    	        	{
                    	        	  ((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg =  true;
                    	        	}

                    	        }
                    	        break;

                    	}

                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:198:3: ( SYMBOL -> SYMBOL | OPCODE ({...}? -> TRUE[$OPCODE] | {...}? -> FALSE[$OPCODE] ) )
                    	int alt19 = 2;
                    	int LA19_0 = input.LA(1);

                    	if ( (LA19_0 == SYMBOL) )
                    	{
                    	    alt19 = 1;
                    	}
                    	else if ( (LA19_0 == OPCODE) )
                    	{
                    	    alt19 = 2;
                    	}
                    	else 
                    	{
                    	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	    NoViableAltException nvae_d19s0 =
                    	        new NoViableAltException("", 19, 0, input);

                    	    throw nvae_d19s0;
                    	}
                    	switch (alt19) 
                    	{
                    	    case 1 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:198:5: SYMBOL
                    	        {
                    	        	SYMBOL34=(IToken)Match(input,SYMBOL,FOLLOW_SYMBOL_in_inf_spec_operand1172); if (state.failed) return retval; 
                    	        	if ( (state.backtracking==0) ) stream_SYMBOL.Add(SYMBOL34);



                    	        	// AST REWRITE
                    	        	// elements:          SYMBOL
                    	        	// token labels:      
                    	        	// rule labels:       retval
                    	        	// token list labels: 
                    	        	// rule list labels:  
                    	        	// wildcard labels: 
                    	        	if ( (state.backtracking==0) ) {
                    	        	retval.Tree = root_0;
                    	        	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	        	root_0 = (object)adaptor.GetNilNode();
                    	        	// 198:16: -> SYMBOL
                    	        	{
                    	        	    adaptor.AddChild(root_0, stream_SYMBOL.NextNode());

                    	        	}

                    	        	retval.Tree = root_0;retval.Tree = root_0;}
                    	        }
                    	        break;
                    	    case 2 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:199:5: OPCODE ({...}? -> TRUE[$OPCODE] | {...}? -> FALSE[$OPCODE] )
                    	        {
                    	        	OPCODE35=(IToken)Match(input,OPCODE,FOLLOW_OPCODE_in_inf_spec_operand1186); if (state.failed) return retval; 
                    	        	if ( (state.backtracking==0) ) stream_OPCODE.Add(OPCODE35);

                    	        	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:200:4: ({...}? -> TRUE[$OPCODE] | {...}? -> FALSE[$OPCODE] )
                    	        	int alt18 = 2;
                    	        	switch ( input.LA(1) ) 
                    	        	{
                    	        	case CRLF:
                    	        		{
                    	        	    int LA18_1 = input.LA(2);

                    	        	    if ( ((((((OPCODE35 != null) ? OPCODE35.Text : null) == "rtrue") && (((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg))|| ((((OPCODE35 != null) ? OPCODE35.Text : null) == "rtrue") && (!((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)))) )
                    	        	    {
                    	        	        alt18 = 1;
                    	        	    }
                    	        	    else if ( ((((((OPCODE35 != null) ? OPCODE35.Text : null) == "rfalse") && (((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg))|| ((((OPCODE35 != null) ? OPCODE35.Text : null) == "rfalse") && (!((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)))) )
                    	        	    {
                    	        	        alt18 = 2;
                    	        	    }
                    	        	    else 
                    	        	    {
                    	        	        if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        	        NoViableAltException nvae_d18s1 =
                    	        	            new NoViableAltException("", 18, 1, input);

                    	        	        throw nvae_d18s1;
                    	        	    }
                    	        	    }
                    	        	    break;
                    	        	case EOF:
                    	        		{
                    	        	    int LA18_2 = input.LA(2);

                    	        	    if ( ((((((OPCODE35 != null) ? OPCODE35.Text : null) == "rtrue") && (((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg))|| ((((OPCODE35 != null) ? OPCODE35.Text : null) == "rtrue") && (!((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)))) )
                    	        	    {
                    	        	        alt18 = 1;
                    	        	    }
                    	        	    else if ( ((((((OPCODE35 != null) ? OPCODE35.Text : null) == "rfalse") && (((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg))|| ((((OPCODE35 != null) ? OPCODE35.Text : null) == "rfalse") && (!((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)))) )
                    	        	    {
                    	        	        alt18 = 2;
                    	        	    }
                    	        	    else 
                    	        	    {
                    	        	        if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        	        NoViableAltException nvae_d18s2 =
                    	        	            new NoViableAltException("", 18, 2, input);

                    	        	        throw nvae_d18s2;
                    	        	    }
                    	        	    }
                    	        	    break;
                    	        	case QUEST:
                    	        		{
                    	        	    int LA18_3 = input.LA(2);

                    	        	    if ( ((((((OPCODE35 != null) ? OPCODE35.Text : null) == "rtrue") && (((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg))|| ((((OPCODE35 != null) ? OPCODE35.Text : null) == "rtrue") && (!((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)))) )
                    	        	    {
                    	        	        alt18 = 1;
                    	        	    }
                    	        	    else if ( ((((((OPCODE35 != null) ? OPCODE35.Text : null) == "rfalse") && (((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg))|| ((((OPCODE35 != null) ? OPCODE35.Text : null) == "rfalse") && (!((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)))) )
                    	        	    {
                    	        	        alt18 = 2;
                    	        	    }
                    	        	    else 
                    	        	    {
                    	        	        if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        	        NoViableAltException nvae_d18s3 =
                    	        	            new NoViableAltException("", 18, 3, input);

                    	        	        throw nvae_d18s3;
                    	        	    }
                    	        	    }
                    	        	    break;
                    	        	case ARROW:
                    	        		{
                    	        	    int LA18_4 = input.LA(2);

                    	        	    if ( ((((((OPCODE35 != null) ? OPCODE35.Text : null) == "rtrue") && (((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg))|| ((((OPCODE35 != null) ? OPCODE35.Text : null) == "rtrue") && (!((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)))) )
                    	        	    {
                    	        	        alt18 = 1;
                    	        	    }
                    	        	    else if ( ((((((OPCODE35 != null) ? OPCODE35.Text : null) == "rfalse") && (((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg))|| ((((OPCODE35 != null) ? OPCODE35.Text : null) == "rfalse") && (!((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)))) )
                    	        	    {
                    	        	        alt18 = 2;
                    	        	    }
                    	        	    else 
                    	        	    {
                    	        	        if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        	        NoViableAltException nvae_d18s4 =
                    	        	            new NoViableAltException("", 18, 4, input);

                    	        	        throw nvae_d18s4;
                    	        	    }
                    	        	    }
                    	        	    break;
                    	        		default:
                    	        		    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        		    NoViableAltException nvae_d18s0 =
                    	        		        new NoViableAltException("", 18, 0, input);

                    	        		    throw nvae_d18s0;
                    	        	}

                    	        	switch (alt18) 
                    	        	{
                    	        	    case 1 :
                    	        	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:200:6: {...}?
                    	        	        {
                    	        	        	if ( !((((OPCODE35 != null) ? OPCODE35.Text : null) == "rtrue")) ) 
                    	        	        	{
                    	        	        	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        	        	    throw new FailedPredicateException(input, "inf_spec_operand", "$OPCODE.text == \"rtrue\"");
                    	        	        	}


                    	        	        	// AST REWRITE
                    	        	        	// elements:          
                    	        	        	// token labels:      
                    	        	        	// rule labels:       retval
                    	        	        	// token list labels: 
                    	        	        	// rule list labels:  
                    	        	        	// wildcard labels: 
                    	        	        	if ( (state.backtracking==0) ) {
                    	        	        	retval.Tree = root_0;
                    	        	        	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	        	        	root_0 = (object)adaptor.GetNilNode();
                    	        	        	// 200:33: -> TRUE[$OPCODE]
                    	        	        	{
                    	        	        	    adaptor.AddChild(root_0, (object)adaptor.Create(TRUE, OPCODE35));

                    	        	        	}

                    	        	        	retval.Tree = root_0;retval.Tree = root_0;}
                    	        	        }
                    	        	        break;
                    	        	    case 2 :
                    	        	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:201:6: {...}?
                    	        	        {
                    	        	        	if ( !((((OPCODE35 != null) ? OPCODE35.Text : null) == "rfalse")) ) 
                    	        	        	{
                    	        	        	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        	        	    throw new FailedPredicateException(input, "inf_spec_operand", "$OPCODE.text == \"rfalse\"");
                    	        	        	}


                    	        	        	// AST REWRITE
                    	        	        	// elements:          
                    	        	        	// token labels:      
                    	        	        	// rule labels:       retval
                    	        	        	// token list labels: 
                    	        	        	// rule list labels:  
                    	        	        	// wildcard labels: 
                    	        	        	if ( (state.backtracking==0) ) {
                    	        	        	retval.Tree = root_0;
                    	        	        	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	        	        	root_0 = (object)adaptor.GetNilNode();
                    	        	        	// 201:34: -> FALSE[$OPCODE]
                    	        	        	{
                    	        	        	    adaptor.AddChild(root_0, (object)adaptor.Create(FALSE, OPCODE35));

                    	        	        	}

                    	        	        	retval.Tree = root_0;retval.Tree = root_0;}
                    	        	        }
                    	        	        break;

                    	        	}


                    	        }
                    	        break;

                    	}

                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:202:3: ({...}? -> ^( BACKSLASH[$QUEST] $inf_spec_operand) | {...}? -> ^( SLASH[$QUEST] $inf_spec_operand) )
                    	int alt20 = 2;
                    	switch ( input.LA(1) ) 
                    	{
                    	case CRLF:
                    		{
                    	    int LA20_1 = input.LA(2);

                    	    if ( ((((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)) )
                    	    {
                    	        alt20 = 1;
                    	    }
                    	    else if ( ((!((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)) )
                    	    {
                    	        alt20 = 2;
                    	    }
                    	    else 
                    	    {
                    	        if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        NoViableAltException nvae_d20s1 =
                    	            new NoViableAltException("", 20, 1, input);

                    	        throw nvae_d20s1;
                    	    }
                    	    }
                    	    break;
                    	case EOF:
                    		{
                    	    int LA20_2 = input.LA(2);

                    	    if ( ((((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)) )
                    	    {
                    	        alt20 = 1;
                    	    }
                    	    else if ( ((!((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)) )
                    	    {
                    	        alt20 = 2;
                    	    }
                    	    else 
                    	    {
                    	        if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        NoViableAltException nvae_d20s2 =
                    	            new NoViableAltException("", 20, 2, input);

                    	        throw nvae_d20s2;
                    	    }
                    	    }
                    	    break;
                    	case QUEST:
                    		{
                    	    int LA20_3 = input.LA(2);

                    	    if ( ((((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)) )
                    	    {
                    	        alt20 = 1;
                    	    }
                    	    else if ( ((!((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)) )
                    	    {
                    	        alt20 = 2;
                    	    }
                    	    else 
                    	    {
                    	        if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        NoViableAltException nvae_d20s3 =
                    	            new NoViableAltException("", 20, 3, input);

                    	        throw nvae_d20s3;
                    	    }
                    	    }
                    	    break;
                    	case ARROW:
                    		{
                    	    int LA20_4 = input.LA(2);

                    	    if ( ((((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)) )
                    	    {
                    	        alt20 = 1;
                    	    }
                    	    else if ( ((!((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)) )
                    	    {
                    	        alt20 = 2;
                    	    }
                    	    else 
                    	    {
                    	        if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        NoViableAltException nvae_d20s4 =
                    	            new NoViableAltException("", 20, 4, input);

                    	        throw nvae_d20s4;
                    	    }
                    	    }
                    	    break;
                    		default:
                    		    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    		    NoViableAltException nvae_d20s0 =
                    		        new NoViableAltException("", 20, 0, input);

                    		    throw nvae_d20s0;
                    	}

                    	switch (alt20) 
                    	{
                    	    case 1 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:202:5: {...}?
                    	        {
                    	        	if ( !((((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)) ) 
                    	        	{
                    	        	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        	    throw new FailedPredicateException(input, "inf_spec_operand", "$inf_spec_operand::neg");
                    	        	}


                    	        	// AST REWRITE
                    	        	// elements:          inf_spec_operand
                    	        	// token labels:      
                    	        	// rule labels:       retval
                    	        	// token list labels: 
                    	        	// rule list labels:  
                    	        	// wildcard labels: 
                    	        	if ( (state.backtracking==0) ) {
                    	        	retval.Tree = root_0;
                    	        	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	        	root_0 = (object)adaptor.GetNilNode();
                    	        	// 202:32: -> ^( BACKSLASH[$QUEST] $inf_spec_operand)
                    	        	{
                    	        	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:202:35: ^( BACKSLASH[$QUEST] $inf_spec_operand)
                    	        	    {
                    	        	    object root_1 = (object)adaptor.GetNilNode();
                    	        	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(BACKSLASH, QUEST32), root_1);

                    	        	    adaptor.AddChild(root_1, stream_retval.NextTree());

                    	        	    adaptor.AddChild(root_0, root_1);
                    	        	    }

                    	        	}

                    	        	retval.Tree = root_0;retval.Tree = root_0;}
                    	        }
                    	        break;
                    	    case 2 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:203:5: {...}?
                    	        {
                    	        	if ( !((!((inf_spec_operand_scope)inf_spec_operand_stack.Peek()).neg)) ) 
                    	        	{
                    	        	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        	    throw new FailedPredicateException(input, "inf_spec_operand", "!$inf_spec_operand::neg");
                    	        	}


                    	        	// AST REWRITE
                    	        	// elements:          inf_spec_operand
                    	        	// token labels:      
                    	        	// rule labels:       retval
                    	        	// token list labels: 
                    	        	// rule list labels:  
                    	        	// wildcard labels: 
                    	        	if ( (state.backtracking==0) ) {
                    	        	retval.Tree = root_0;
                    	        	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	        	root_0 = (object)adaptor.GetNilNode();
                    	        	// 203:33: -> ^( SLASH[$QUEST] $inf_spec_operand)
                    	        	{
                    	        	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:203:36: ^( SLASH[$QUEST] $inf_spec_operand)
                    	        	    {
                    	        	    object root_1 = (object)adaptor.GetNilNode();
                    	        	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(SLASH, QUEST32), root_1);

                    	        	    adaptor.AddChild(root_1, stream_retval.NextTree());

                    	        	    adaptor.AddChild(root_0, root_1);
                    	        	    }

                    	        	}

                    	        	retval.Tree = root_0;retval.Tree = root_0;}
                    	        }
                    	        break;

                    	}


                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:204:4: ARROW symbol
                    {
                    	ARROW36=(IToken)Match(input,ARROW,FOLLOW_ARROW_in_inf_spec_operand1252); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_ARROW.Add(ARROW36);

                    	PushFollow(FOLLOW_symbol_in_inf_spec_operand1254);
                    	symbol37 = symbol();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( (state.backtracking==0) ) stream_symbol.Add(symbol37.Tree);


                    	// AST REWRITE
                    	// elements:          symbol
                    	// token labels:      
                    	// rule labels:       retval
                    	// token list labels: 
                    	// rule list labels:  
                    	// wildcard labels: 
                    	if ( (state.backtracking==0) ) {
                    	retval.Tree = root_0;
                    	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	root_0 = (object)adaptor.GetNilNode();
                    	// 204:21: -> ^( RANGLE[$ARROW] symbol )
                    	{
                    	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:204:24: ^( RANGLE[$ARROW] symbol )
                    	    {
                    	    object root_1 = (object)adaptor.GetNilNode();
                    	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(RANGLE, ARROW36), root_1);

                    	    adaptor.AddChild(root_1, stream_symbol.NextTree());

                    	    adaptor.AddChild(root_0, root_1);
                    	    }

                    	}

                    	retval.Tree = root_0;retval.Tree = root_0;}
                    }
                    break;

            }
            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
            inf_spec_operand_stack.Pop();
        }
        return retval;
    }
    // $ANTLR end "inf_spec_operand"

    public class zap_operands_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "zap_operands"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:207:1: zap_operands : ( expr ( COMMA expr )* )? ( zap_spec_operand )* ;
    public ZapParser.zap_operands_return zap_operands() // throws RecognitionException [1]
    {   
        ZapParser.zap_operands_return retval = new ZapParser.zap_operands_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken COMMA39 = null;
        ZapParser.expr_return expr38 = default(ZapParser.expr_return);

        ZapParser.expr_return expr40 = default(ZapParser.expr_return);

        ZapParser.zap_spec_operand_return zap_spec_operand41 = default(ZapParser.zap_spec_operand_return);


        object COMMA39_tree=null;

        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:207:13: ( ( expr ( COMMA expr )* )? ( zap_spec_operand )* )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:207:15: ( expr ( COMMA expr )* )? ( zap_spec_operand )*
            {
            	root_0 = (object)adaptor.GetNilNode();

            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:207:15: ( expr ( COMMA expr )* )?
            	int alt23 = 2;
            	int LA23_0 = input.LA(1);

            	if ( (LA23_0 == APOSTROPHE || LA23_0 == NUM || (LA23_0 >= SYMBOL && LA23_0 <= OPCODE)) )
            	{
            	    alt23 = 1;
            	}
            	switch (alt23) 
            	{
            	    case 1 :
            	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:207:16: expr ( COMMA expr )*
            	        {
            	        	PushFollow(FOLLOW_expr_in_zap_operands1277);
            	        	expr38 = expr();
            	        	state.followingStackPointer--;
            	        	if (state.failed) return retval;
            	        	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr38.Tree);
            	        	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:207:21: ( COMMA expr )*
            	        	do 
            	        	{
            	        	    int alt22 = 2;
            	        	    int LA22_0 = input.LA(1);

            	        	    if ( (LA22_0 == COMMA) )
            	        	    {
            	        	        alt22 = 1;
            	        	    }


            	        	    switch (alt22) 
            	        		{
            	        			case 1 :
            	        			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:207:22: COMMA expr
            	        			    {
            	        			    	COMMA39=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_zap_operands1280); if (state.failed) return retval;
            	        			    	PushFollow(FOLLOW_expr_in_zap_operands1283);
            	        			    	expr40 = expr();
            	        			    	state.followingStackPointer--;
            	        			    	if (state.failed) return retval;
            	        			    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr40.Tree);

            	        			    }
            	        			    break;

            	        			default:
            	        			    goto loop22;
            	        	    }
            	        	} while (true);

            	        	loop22:
            	        		;	// Stops C# compiler whining that label 'loop22' has no statements


            	        }
            	        break;

            	}

            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:207:38: ( zap_spec_operand )*
            	do 
            	{
            	    int alt24 = 2;
            	    int LA24_0 = input.LA(1);

            	    if ( ((LA24_0 >= SLASH && LA24_0 <= RANGLE)) )
            	    {
            	        alt24 = 1;
            	    }


            	    switch (alt24) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:207:38: zap_spec_operand
            			    {
            			    	PushFollow(FOLLOW_zap_spec_operand_in_zap_operands1289);
            			    	zap_spec_operand41 = zap_spec_operand();
            			    	state.followingStackPointer--;
            			    	if (state.failed) return retval;
            			    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, zap_spec_operand41.Tree);

            			    }
            			    break;

            			default:
            			    goto loop24;
            	    }
            	} while (true);

            	loop24:
            		;	// Stops C# compiler whining that label 'loop24' has no statements


            }

            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "zap_operands"

    public class zap_spec_operand_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "zap_spec_operand"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:210:1: zap_spec_operand : ( (s= SLASH | s= BACKSLASH ) SYMBOL ({...}? -> ^( $s TRUE ) | {...}? -> ^( $s FALSE ) | -> ^( $s SYMBOL ) ) | RANGLE symbol );
    public ZapParser.zap_spec_operand_return zap_spec_operand() // throws RecognitionException [1]
    {   
        ZapParser.zap_spec_operand_return retval = new ZapParser.zap_spec_operand_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken s = null;
        IToken SYMBOL42 = null;
        IToken RANGLE43 = null;
        ZapParser.symbol_return symbol44 = default(ZapParser.symbol_return);


        object s_tree=null;
        object SYMBOL42_tree=null;
        object RANGLE43_tree=null;
        RewriteRuleTokenStream stream_SLASH = new RewriteRuleTokenStream(adaptor,"token SLASH");
        RewriteRuleTokenStream stream_SYMBOL = new RewriteRuleTokenStream(adaptor,"token SYMBOL");
        RewriteRuleTokenStream stream_BACKSLASH = new RewriteRuleTokenStream(adaptor,"token BACKSLASH");

        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:211:2: ( (s= SLASH | s= BACKSLASH ) SYMBOL ({...}? -> ^( $s TRUE ) | {...}? -> ^( $s FALSE ) | -> ^( $s SYMBOL ) ) | RANGLE symbol )
            int alt27 = 2;
            int LA27_0 = input.LA(1);

            if ( ((LA27_0 >= SLASH && LA27_0 <= BACKSLASH)) )
            {
                alt27 = 1;
            }
            else if ( (LA27_0 == RANGLE) )
            {
                alt27 = 2;
            }
            else 
            {
                if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                NoViableAltException nvae_d27s0 =
                    new NoViableAltException("", 27, 0, input);

                throw nvae_d27s0;
            }
            switch (alt27) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:211:4: (s= SLASH | s= BACKSLASH ) SYMBOL ({...}? -> ^( $s TRUE ) | {...}? -> ^( $s FALSE ) | -> ^( $s SYMBOL ) )
                    {
                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:211:4: (s= SLASH | s= BACKSLASH )
                    	int alt25 = 2;
                    	int LA25_0 = input.LA(1);

                    	if ( (LA25_0 == SLASH) )
                    	{
                    	    alt25 = 1;
                    	}
                    	else if ( (LA25_0 == BACKSLASH) )
                    	{
                    	    alt25 = 2;
                    	}
                    	else 
                    	{
                    	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	    NoViableAltException nvae_d25s0 =
                    	        new NoViableAltException("", 25, 0, input);

                    	    throw nvae_d25s0;
                    	}
                    	switch (alt25) 
                    	{
                    	    case 1 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:211:5: s= SLASH
                    	        {
                    	        	s=(IToken)Match(input,SLASH,FOLLOW_SLASH_in_zap_spec_operand1304); if (state.failed) return retval; 
                    	        	if ( (state.backtracking==0) ) stream_SLASH.Add(s);


                    	        }
                    	        break;
                    	    case 2 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:211:15: s= BACKSLASH
                    	        {
                    	        	s=(IToken)Match(input,BACKSLASH,FOLLOW_BACKSLASH_in_zap_spec_operand1310); if (state.failed) return retval; 
                    	        	if ( (state.backtracking==0) ) stream_BACKSLASH.Add(s);


                    	        }
                    	        break;

                    	}

                    	SYMBOL42=(IToken)Match(input,SYMBOL,FOLLOW_SYMBOL_in_zap_spec_operand1313); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_SYMBOL.Add(SYMBOL42);

                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:212:3: ({...}? -> ^( $s TRUE ) | {...}? -> ^( $s FALSE ) | -> ^( $s SYMBOL ) )
                    	int alt26 = 3;
                    	switch ( input.LA(1) ) 
                    	{
                    	case CRLF:
                    		{
                    	    int LA26_1 = input.LA(2);

                    	    if ( ((((SYMBOL42 != null) ? SYMBOL42.Text : null) == "TRUE")) )
                    	    {
                    	        alt26 = 1;
                    	    }
                    	    else if ( ((((SYMBOL42 != null) ? SYMBOL42.Text : null) == "FALSE")) )
                    	    {
                    	        alt26 = 2;
                    	    }
                    	    else if ( (true) )
                    	    {
                    	        alt26 = 3;
                    	    }
                    	    else 
                    	    {
                    	        if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        NoViableAltException nvae_d26s1 =
                    	            new NoViableAltException("", 26, 1, input);

                    	        throw nvae_d26s1;
                    	    }
                    	    }
                    	    break;
                    	case EOF:
                    		{
                    	    int LA26_2 = input.LA(2);

                    	    if ( ((((SYMBOL42 != null) ? SYMBOL42.Text : null) == "TRUE")) )
                    	    {
                    	        alt26 = 1;
                    	    }
                    	    else if ( ((((SYMBOL42 != null) ? SYMBOL42.Text : null) == "FALSE")) )
                    	    {
                    	        alt26 = 2;
                    	    }
                    	    else if ( (true) )
                    	    {
                    	        alt26 = 3;
                    	    }
                    	    else 
                    	    {
                    	        if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        NoViableAltException nvae_d26s2 =
                    	            new NoViableAltException("", 26, 2, input);

                    	        throw nvae_d26s2;
                    	    }
                    	    }
                    	    break;
                    	case SLASH:
                    		{
                    	    int LA26_3 = input.LA(2);

                    	    if ( ((((SYMBOL42 != null) ? SYMBOL42.Text : null) == "TRUE")) )
                    	    {
                    	        alt26 = 1;
                    	    }
                    	    else if ( ((((SYMBOL42 != null) ? SYMBOL42.Text : null) == "FALSE")) )
                    	    {
                    	        alt26 = 2;
                    	    }
                    	    else if ( (true) )
                    	    {
                    	        alt26 = 3;
                    	    }
                    	    else 
                    	    {
                    	        if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        NoViableAltException nvae_d26s3 =
                    	            new NoViableAltException("", 26, 3, input);

                    	        throw nvae_d26s3;
                    	    }
                    	    }
                    	    break;
                    	case BACKSLASH:
                    		{
                    	    int LA26_4 = input.LA(2);

                    	    if ( ((((SYMBOL42 != null) ? SYMBOL42.Text : null) == "TRUE")) )
                    	    {
                    	        alt26 = 1;
                    	    }
                    	    else if ( ((((SYMBOL42 != null) ? SYMBOL42.Text : null) == "FALSE")) )
                    	    {
                    	        alt26 = 2;
                    	    }
                    	    else if ( (true) )
                    	    {
                    	        alt26 = 3;
                    	    }
                    	    else 
                    	    {
                    	        if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        NoViableAltException nvae_d26s4 =
                    	            new NoViableAltException("", 26, 4, input);

                    	        throw nvae_d26s4;
                    	    }
                    	    }
                    	    break;
                    	case RANGLE:
                    		{
                    	    int LA26_5 = input.LA(2);

                    	    if ( ((((SYMBOL42 != null) ? SYMBOL42.Text : null) == "TRUE")) )
                    	    {
                    	        alt26 = 1;
                    	    }
                    	    else if ( ((((SYMBOL42 != null) ? SYMBOL42.Text : null) == "FALSE")) )
                    	    {
                    	        alt26 = 2;
                    	    }
                    	    else if ( (true) )
                    	    {
                    	        alt26 = 3;
                    	    }
                    	    else 
                    	    {
                    	        if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        NoViableAltException nvae_d26s5 =
                    	            new NoViableAltException("", 26, 5, input);

                    	        throw nvae_d26s5;
                    	    }
                    	    }
                    	    break;
                    		default:
                    		    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    		    NoViableAltException nvae_d26s0 =
                    		        new NoViableAltException("", 26, 0, input);

                    		    throw nvae_d26s0;
                    	}

                    	switch (alt26) 
                    	{
                    	    case 1 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:212:5: {...}?
                    	        {
                    	        	if ( !((((SYMBOL42 != null) ? SYMBOL42.Text : null) == "TRUE")) ) 
                    	        	{
                    	        	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        	    throw new FailedPredicateException(input, "zap_spec_operand", "$SYMBOL.text == \"TRUE\"");
                    	        	}


                    	        	// AST REWRITE
                    	        	// elements:          s
                    	        	// token labels:      s
                    	        	// rule labels:       retval
                    	        	// token list labels: 
                    	        	// rule list labels:  
                    	        	// wildcard labels: 
                    	        	if ( (state.backtracking==0) ) {
                    	        	retval.Tree = root_0;
                    	        	RewriteRuleTokenStream stream_s = new RewriteRuleTokenStream(adaptor, "token s", s);
                    	        	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	        	root_0 = (object)adaptor.GetNilNode();
                    	        	// 212:31: -> ^( $s TRUE )
                    	        	{
                    	        	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:212:34: ^( $s TRUE )
                    	        	    {
                    	        	    object root_1 = (object)adaptor.GetNilNode();
                    	        	    root_1 = (object)adaptor.BecomeRoot(stream_s.NextNode(), root_1);

                    	        	    adaptor.AddChild(root_1, (object)adaptor.Create(TRUE, "TRUE"));

                    	        	    adaptor.AddChild(root_0, root_1);
                    	        	    }

                    	        	}

                    	        	retval.Tree = root_0;retval.Tree = root_0;}
                    	        }
                    	        break;
                    	    case 2 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:213:5: {...}?
                    	        {
                    	        	if ( !((((SYMBOL42 != null) ? SYMBOL42.Text : null) == "FALSE")) ) 
                    	        	{
                    	        	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	        	    throw new FailedPredicateException(input, "zap_spec_operand", "$SYMBOL.text == \"FALSE\"");
                    	        	}


                    	        	// AST REWRITE
                    	        	// elements:          s
                    	        	// token labels:      s
                    	        	// rule labels:       retval
                    	        	// token list labels: 
                    	        	// rule list labels:  
                    	        	// wildcard labels: 
                    	        	if ( (state.backtracking==0) ) {
                    	        	retval.Tree = root_0;
                    	        	RewriteRuleTokenStream stream_s = new RewriteRuleTokenStream(adaptor, "token s", s);
                    	        	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	        	root_0 = (object)adaptor.GetNilNode();
                    	        	// 213:32: -> ^( $s FALSE )
                    	        	{
                    	        	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:213:35: ^( $s FALSE )
                    	        	    {
                    	        	    object root_1 = (object)adaptor.GetNilNode();
                    	        	    root_1 = (object)adaptor.BecomeRoot(stream_s.NextNode(), root_1);

                    	        	    adaptor.AddChild(root_1, (object)adaptor.Create(FALSE, "FALSE"));

                    	        	    adaptor.AddChild(root_0, root_1);
                    	        	    }

                    	        	}

                    	        	retval.Tree = root_0;retval.Tree = root_0;}
                    	        }
                    	        break;
                    	    case 3 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:214:9: 
                    	        {

                    	        	// AST REWRITE
                    	        	// elements:          s, SYMBOL
                    	        	// token labels:      s
                    	        	// rule labels:       retval
                    	        	// token list labels: 
                    	        	// rule list labels:  
                    	        	// wildcard labels: 
                    	        	if ( (state.backtracking==0) ) {
                    	        	retval.Tree = root_0;
                    	        	RewriteRuleTokenStream stream_s = new RewriteRuleTokenStream(adaptor, "token s", s);
                    	        	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	        	root_0 = (object)adaptor.GetNilNode();
                    	        	// 214:9: -> ^( $s SYMBOL )
                    	        	{
                    	        	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:214:12: ^( $s SYMBOL )
                    	        	    {
                    	        	    object root_1 = (object)adaptor.GetNilNode();
                    	        	    root_1 = (object)adaptor.BecomeRoot(stream_s.NextNode(), root_1);

                    	        	    adaptor.AddChild(root_1, stream_SYMBOL.NextNode());

                    	        	    adaptor.AddChild(root_0, root_1);
                    	        	    }

                    	        	}

                    	        	retval.Tree = root_0;retval.Tree = root_0;}
                    	        }
                    	        break;

                    	}


                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:215:4: RANGLE symbol
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	RANGLE43=(IToken)Match(input,RANGLE,FOLLOW_RANGLE_in_zap_spec_operand1366); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{RANGLE43_tree = (object)adaptor.Create(RANGLE43);
                    		root_0 = (object)adaptor.BecomeRoot(RANGLE43_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_symbol_in_zap_spec_operand1369);
                    	symbol44 = symbol();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, symbol44.Tree);

                    }
                    break;

            }
            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "zap_spec_operand"

    public class expr_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "expr"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:218:1: expr : term ( PLUS term )* ;
    public ZapParser.expr_return expr() // throws RecognitionException [1]
    {   
        ZapParser.expr_return retval = new ZapParser.expr_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken PLUS46 = null;
        ZapParser.term_return term45 = default(ZapParser.term_return);

        ZapParser.term_return term47 = default(ZapParser.term_return);


        object PLUS46_tree=null;

        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:218:6: ( term ( PLUS term )* )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:218:8: term ( PLUS term )*
            {
            	root_0 = (object)adaptor.GetNilNode();

            	PushFollow(FOLLOW_term_in_expr1379);
            	term45 = term();
            	state.followingStackPointer--;
            	if (state.failed) return retval;
            	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, term45.Tree);
            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:218:13: ( PLUS term )*
            	do 
            	{
            	    int alt28 = 2;
            	    int LA28_0 = input.LA(1);

            	    if ( (LA28_0 == PLUS) )
            	    {
            	        alt28 = 1;
            	    }


            	    switch (alt28) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:218:14: PLUS term
            			    {
            			    	PLUS46=(IToken)Match(input,PLUS,FOLLOW_PLUS_in_expr1382); if (state.failed) return retval;
            			    	if ( state.backtracking == 0 )
            			    	{PLUS46_tree = (object)adaptor.Create(PLUS46);
            			    		root_0 = (object)adaptor.BecomeRoot(PLUS46_tree, root_0);
            			    	}
            			    	PushFollow(FOLLOW_term_in_expr1385);
            			    	term47 = term();
            			    	state.followingStackPointer--;
            			    	if (state.failed) return retval;
            			    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, term47.Tree);

            			    }
            			    break;

            			default:
            			    goto loop28;
            	    }
            	} while (true);

            	loop28:
            		;	// Stops C# compiler whining that label 'loop28' has no statements


            }

            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "expr"

    public class term_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "term"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:221:1: term : ( NUM | symbol | APOSTROPHE symbol );
    public ZapParser.term_return term() // throws RecognitionException [1]
    {   
        ZapParser.term_return retval = new ZapParser.term_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken NUM48 = null;
        IToken APOSTROPHE50 = null;
        ZapParser.symbol_return symbol49 = default(ZapParser.symbol_return);

        ZapParser.symbol_return symbol51 = default(ZapParser.symbol_return);


        object NUM48_tree=null;
        object APOSTROPHE50_tree=null;

        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:221:6: ( NUM | symbol | APOSTROPHE symbol )
            int alt29 = 3;
            switch ( input.LA(1) ) 
            {
            case NUM:
            	{
                alt29 = 1;
                }
                break;
            case SYMBOL:
            case OPCODE:
            	{
                alt29 = 2;
                }
                break;
            case APOSTROPHE:
            	{
                alt29 = 3;
                }
                break;
            	default:
            	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            	    NoViableAltException nvae_d29s0 =
            	        new NoViableAltException("", 29, 0, input);

            	    throw nvae_d29s0;
            }

            switch (alt29) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:221:8: NUM
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	NUM48=(IToken)Match(input,NUM,FOLLOW_NUM_in_term1397); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{NUM48_tree = (object)adaptor.Create(NUM48);
                    		adaptor.AddChild(root_0, NUM48_tree);
                    	}

                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:222:4: symbol
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	PushFollow(FOLLOW_symbol_in_term1402);
                    	symbol49 = symbol();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, symbol49.Tree);

                    }
                    break;
                case 3 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:223:4: APOSTROPHE symbol
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	APOSTROPHE50=(IToken)Match(input,APOSTROPHE,FOLLOW_APOSTROPHE_in_term1407); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{APOSTROPHE50_tree = (object)adaptor.Create(APOSTROPHE50);
                    		root_0 = (object)adaptor.BecomeRoot(APOSTROPHE50_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_symbol_in_term1410);
                    	symbol51 = symbol();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, symbol51.Tree);

                    }
                    break;

            }
            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "term"

    public class meta_directive_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "meta_directive"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:226:1: meta_directive : ( NEW ( expr )? | TIME | INSERT STRING | END | ENDI );
    public ZapParser.meta_directive_return meta_directive() // throws RecognitionException [1]
    {   
        ZapParser.meta_directive_return retval = new ZapParser.meta_directive_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken NEW52 = null;
        IToken TIME54 = null;
        IToken INSERT55 = null;
        IToken STRING56 = null;
        IToken END57 = null;
        IToken ENDI58 = null;
        ZapParser.expr_return expr53 = default(ZapParser.expr_return);


        object NEW52_tree=null;
        object TIME54_tree=null;
        object INSERT55_tree=null;
        object STRING56_tree=null;
        object END57_tree=null;
        object ENDI58_tree=null;

        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:227:2: ( NEW ( expr )? | TIME | INSERT STRING | END | ENDI )
            int alt31 = 5;
            switch ( input.LA(1) ) 
            {
            case NEW:
            	{
                alt31 = 1;
                }
                break;
            case TIME:
            	{
                alt31 = 2;
                }
                break;
            case INSERT:
            	{
                alt31 = 3;
                }
                break;
            case END:
            	{
                alt31 = 4;
                }
                break;
            case ENDI:
            	{
                alt31 = 5;
                }
                break;
            	default:
            	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            	    NoViableAltException nvae_d31s0 =
            	        new NoViableAltException("", 31, 0, input);

            	    throw nvae_d31s0;
            }

            switch (alt31) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:227:4: NEW ( expr )?
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	NEW52=(IToken)Match(input,NEW,FOLLOW_NEW_in_meta_directive1421); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{NEW52_tree = (object)adaptor.Create(NEW52);
                    		root_0 = (object)adaptor.BecomeRoot(NEW52_tree, root_0);
                    	}
                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:227:9: ( expr )?
                    	int alt30 = 2;
                    	int LA30_0 = input.LA(1);

                    	if ( (LA30_0 == APOSTROPHE || LA30_0 == NUM || (LA30_0 >= SYMBOL && LA30_0 <= OPCODE)) )
                    	{
                    	    alt30 = 1;
                    	}
                    	switch (alt30) 
                    	{
                    	    case 1 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:227:9: expr
                    	        {
                    	        	PushFollow(FOLLOW_expr_in_meta_directive1424);
                    	        	expr53 = expr();
                    	        	state.followingStackPointer--;
                    	        	if (state.failed) return retval;
                    	        	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr53.Tree);

                    	        }
                    	        break;

                    	}


                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:228:4: TIME
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	TIME54=(IToken)Match(input,TIME,FOLLOW_TIME_in_meta_directive1430); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{TIME54_tree = (object)adaptor.Create(TIME54);
                    		adaptor.AddChild(root_0, TIME54_tree);
                    	}

                    }
                    break;
                case 3 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:229:4: INSERT STRING
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	INSERT55=(IToken)Match(input,INSERT,FOLLOW_INSERT_in_meta_directive1435); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{INSERT55_tree = (object)adaptor.Create(INSERT55);
                    		root_0 = (object)adaptor.BecomeRoot(INSERT55_tree, root_0);
                    	}
                    	STRING56=(IToken)Match(input,STRING,FOLLOW_STRING_in_meta_directive1438); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING56_tree = (object)adaptor.Create(STRING56);
                    		adaptor.AddChild(root_0, STRING56_tree);
                    	}

                    }
                    break;
                case 4 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:230:4: END
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	END57=(IToken)Match(input,END,FOLLOW_END_in_meta_directive1443); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{END57_tree = (object)adaptor.Create(END57);
                    		adaptor.AddChild(root_0, END57_tree);
                    	}

                    }
                    break;
                case 5 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:231:4: ENDI
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	ENDI58=(IToken)Match(input,ENDI,FOLLOW_ENDI_in_meta_directive1448); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{ENDI58_tree = (object)adaptor.Create(ENDI58);
                    		adaptor.AddChild(root_0, ENDI58_tree);
                    	}

                    }
                    break;

            }
            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "meta_directive"

    public class data_directive_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "data_directive"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:234:1: data_directive : ( ( BYTE | WORD ) expr ( COMMA expr )* | expr ( COMMA expr )* -> ^( WORD ( expr )+ ) | ( TRUE | FALSE ) | PROP expr COMMA expr | ( LEN | STR | STRL | ZWORD ) STRING | ( FSTR | GSTR ) SYMBOL COMMA STRING | GVAR SYMBOL ( EQUALS expr ( COMMA SYMBOL )? )? | OBJECT expr COMMA expr COMMA expr ( COMMA expr )? COMMA expr COMMA expr COMMA expr COMMA expr );
    public ZapParser.data_directive_return data_directive() // throws RecognitionException [1]
    {   
        ZapParser.data_directive_return retval = new ZapParser.data_directive_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken set59 = null;
        IToken COMMA61 = null;
        IToken COMMA64 = null;
        IToken set66 = null;
        IToken PROP67 = null;
        IToken COMMA69 = null;
        IToken set71 = null;
        IToken STRING72 = null;
        IToken set73 = null;
        IToken SYMBOL74 = null;
        IToken COMMA75 = null;
        IToken STRING76 = null;
        IToken GVAR77 = null;
        IToken SYMBOL78 = null;
        IToken EQUALS79 = null;
        IToken COMMA81 = null;
        IToken SYMBOL82 = null;
        IToken OBJECT83 = null;
        IToken COMMA85 = null;
        IToken COMMA87 = null;
        IToken COMMA89 = null;
        IToken COMMA91 = null;
        IToken COMMA93 = null;
        IToken COMMA95 = null;
        IToken COMMA97 = null;
        ZapParser.expr_return expr60 = default(ZapParser.expr_return);

        ZapParser.expr_return expr62 = default(ZapParser.expr_return);

        ZapParser.expr_return expr63 = default(ZapParser.expr_return);

        ZapParser.expr_return expr65 = default(ZapParser.expr_return);

        ZapParser.expr_return expr68 = default(ZapParser.expr_return);

        ZapParser.expr_return expr70 = default(ZapParser.expr_return);

        ZapParser.expr_return expr80 = default(ZapParser.expr_return);

        ZapParser.expr_return expr84 = default(ZapParser.expr_return);

        ZapParser.expr_return expr86 = default(ZapParser.expr_return);

        ZapParser.expr_return expr88 = default(ZapParser.expr_return);

        ZapParser.expr_return expr90 = default(ZapParser.expr_return);

        ZapParser.expr_return expr92 = default(ZapParser.expr_return);

        ZapParser.expr_return expr94 = default(ZapParser.expr_return);

        ZapParser.expr_return expr96 = default(ZapParser.expr_return);

        ZapParser.expr_return expr98 = default(ZapParser.expr_return);


        object set59_tree=null;
        object COMMA61_tree=null;
        object COMMA64_tree=null;
        object set66_tree=null;
        object PROP67_tree=null;
        object COMMA69_tree=null;
        object set71_tree=null;
        object STRING72_tree=null;
        object set73_tree=null;
        object SYMBOL74_tree=null;
        object COMMA75_tree=null;
        object STRING76_tree=null;
        object GVAR77_tree=null;
        object SYMBOL78_tree=null;
        object EQUALS79_tree=null;
        object COMMA81_tree=null;
        object SYMBOL82_tree=null;
        object OBJECT83_tree=null;
        object COMMA85_tree=null;
        object COMMA87_tree=null;
        object COMMA89_tree=null;
        object COMMA91_tree=null;
        object COMMA93_tree=null;
        object COMMA95_tree=null;
        object COMMA97_tree=null;
        RewriteRuleTokenStream stream_COMMA = new RewriteRuleTokenStream(adaptor,"token COMMA");
        RewriteRuleSubtreeStream stream_expr = new RewriteRuleSubtreeStream(adaptor,"rule expr");
        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:235:2: ( ( BYTE | WORD ) expr ( COMMA expr )* | expr ( COMMA expr )* -> ^( WORD ( expr )+ ) | ( TRUE | FALSE ) | PROP expr COMMA expr | ( LEN | STR | STRL | ZWORD ) STRING | ( FSTR | GSTR ) SYMBOL COMMA STRING | GVAR SYMBOL ( EQUALS expr ( COMMA SYMBOL )? )? | OBJECT expr COMMA expr COMMA expr ( COMMA expr )? COMMA expr COMMA expr COMMA expr COMMA expr )
            int alt37 = 8;
            switch ( input.LA(1) ) 
            {
            case BYTE:
            case WORD:
            	{
                alt37 = 1;
                }
                break;
            case APOSTROPHE:
            case NUM:
            case SYMBOL:
            case OPCODE:
            	{
                alt37 = 2;
                }
                break;
            case FALSE:
            case TRUE:
            	{
                alt37 = 3;
                }
                break;
            case PROP:
            	{
                alt37 = 4;
                }
                break;
            case LEN:
            case STR:
            case STRL:
            case ZWORD:
            	{
                alt37 = 5;
                }
                break;
            case FSTR:
            case GSTR:
            	{
                alt37 = 6;
                }
                break;
            case GVAR:
            	{
                alt37 = 7;
                }
                break;
            case OBJECT:
            	{
                alt37 = 8;
                }
                break;
            	default:
            	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            	    NoViableAltException nvae_d37s0 =
            	        new NoViableAltException("", 37, 0, input);

            	    throw nvae_d37s0;
            }

            switch (alt37) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:235:4: ( BYTE | WORD ) expr ( COMMA expr )*
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	set59=(IToken)input.LT(1);
                    	set59 = (IToken)input.LT(1);
                    	if ( input.LA(1) == BYTE || input.LA(1) == WORD ) 
                    	{
                    	    input.Consume();
                    	    if ( state.backtracking == 0 ) root_0 = (object)adaptor.BecomeRoot((object)adaptor.Create(set59), root_0);
                    	    state.errorRecovery = false;state.failed = false;
                    	}
                    	else 
                    	{
                    	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	    MismatchedSetException mse = new MismatchedSetException(null,input);
                    	    throw mse;
                    	}

                    	PushFollow(FOLLOW_expr_in_data_directive1468);
                    	expr60 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr60.Tree);
                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:235:24: ( COMMA expr )*
                    	do 
                    	{
                    	    int alt32 = 2;
                    	    int LA32_0 = input.LA(1);

                    	    if ( (LA32_0 == COMMA) )
                    	    {
                    	        alt32 = 1;
                    	    }


                    	    switch (alt32) 
                    		{
                    			case 1 :
                    			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:235:25: COMMA expr
                    			    {
                    			    	COMMA61=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_data_directive1471); if (state.failed) return retval;
                    			    	PushFollow(FOLLOW_expr_in_data_directive1474);
                    			    	expr62 = expr();
                    			    	state.followingStackPointer--;
                    			    	if (state.failed) return retval;
                    			    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr62.Tree);

                    			    }
                    			    break;

                    			default:
                    			    goto loop32;
                    	    }
                    	} while (true);

                    	loop32:
                    		;	// Stops C# compiler whining that label 'loop32' has no statements


                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:236:4: expr ( COMMA expr )*
                    {
                    	PushFollow(FOLLOW_expr_in_data_directive1481);
                    	expr63 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( (state.backtracking==0) ) stream_expr.Add(expr63.Tree);
                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:236:9: ( COMMA expr )*
                    	do 
                    	{
                    	    int alt33 = 2;
                    	    int LA33_0 = input.LA(1);

                    	    if ( (LA33_0 == COMMA) )
                    	    {
                    	        alt33 = 1;
                    	    }


                    	    switch (alt33) 
                    		{
                    			case 1 :
                    			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:236:10: COMMA expr
                    			    {
                    			    	COMMA64=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_data_directive1484); if (state.failed) return retval; 
                    			    	if ( (state.backtracking==0) ) stream_COMMA.Add(COMMA64);

                    			    	PushFollow(FOLLOW_expr_in_data_directive1486);
                    			    	expr65 = expr();
                    			    	state.followingStackPointer--;
                    			    	if (state.failed) return retval;
                    			    	if ( (state.backtracking==0) ) stream_expr.Add(expr65.Tree);

                    			    }
                    			    break;

                    			default:
                    			    goto loop33;
                    	    }
                    	} while (true);

                    	loop33:
                    		;	// Stops C# compiler whining that label 'loop33' has no statements



                    	// AST REWRITE
                    	// elements:          expr
                    	// token labels:      
                    	// rule labels:       retval
                    	// token list labels: 
                    	// rule list labels:  
                    	// wildcard labels: 
                    	if ( (state.backtracking==0) ) {
                    	retval.Tree = root_0;
                    	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	root_0 = (object)adaptor.GetNilNode();
                    	// 236:24: -> ^( WORD ( expr )+ )
                    	{
                    	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:236:27: ^( WORD ( expr )+ )
                    	    {
                    	    object root_1 = (object)adaptor.GetNilNode();
                    	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(WORD, "WORD"), root_1);

                    	    if ( !(stream_expr.HasNext()) ) {
                    	        throw new RewriteEarlyExitException();
                    	    }
                    	    while ( stream_expr.HasNext() )
                    	    {
                    	        adaptor.AddChild(root_1, stream_expr.NextTree());

                    	    }
                    	    stream_expr.Reset();

                    	    adaptor.AddChild(root_0, root_1);
                    	    }

                    	}

                    	retval.Tree = root_0;retval.Tree = root_0;}
                    }
                    break;
                case 3 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:237:4: ( TRUE | FALSE )
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	set66 = (IToken)input.LT(1);
                    	if ( input.LA(1) == FALSE || input.LA(1) == TRUE ) 
                    	{
                    	    input.Consume();
                    	    if ( state.backtracking == 0 ) adaptor.AddChild(root_0, (object)adaptor.Create(set66));
                    	    state.errorRecovery = false;state.failed = false;
                    	}
                    	else 
                    	{
                    	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	    MismatchedSetException mse = new MismatchedSetException(null,input);
                    	    throw mse;
                    	}


                    }
                    break;
                case 4 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:238:4: PROP expr COMMA expr
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	PROP67=(IToken)Match(input,PROP,FOLLOW_PROP_in_data_directive1514); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{PROP67_tree = (object)adaptor.Create(PROP67);
                    		root_0 = (object)adaptor.BecomeRoot(PROP67_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_expr_in_data_directive1517);
                    	expr68 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr68.Tree);
                    	COMMA69=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_data_directive1519); if (state.failed) return retval;
                    	PushFollow(FOLLOW_expr_in_data_directive1522);
                    	expr70 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr70.Tree);

                    }
                    break;
                case 5 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:239:4: ( LEN | STR | STRL | ZWORD ) STRING
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	set71=(IToken)input.LT(1);
                    	set71 = (IToken)input.LT(1);
                    	if ( input.LA(1) == LEN || (input.LA(1) >= STR && input.LA(1) <= STRL) || input.LA(1) == ZWORD ) 
                    	{
                    	    input.Consume();
                    	    if ( state.backtracking == 0 ) root_0 = (object)adaptor.BecomeRoot((object)adaptor.Create(set71), root_0);
                    	    state.errorRecovery = false;state.failed = false;
                    	}
                    	else 
                    	{
                    	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	    MismatchedSetException mse = new MismatchedSetException(null,input);
                    	    throw mse;
                    	}

                    	STRING72=(IToken)Match(input,STRING,FOLLOW_STRING_in_data_directive1544); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING72_tree = (object)adaptor.Create(STRING72);
                    		adaptor.AddChild(root_0, STRING72_tree);
                    	}

                    }
                    break;
                case 6 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:240:4: ( FSTR | GSTR ) SYMBOL COMMA STRING
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	set73=(IToken)input.LT(1);
                    	set73 = (IToken)input.LT(1);
                    	if ( input.LA(1) == FSTR || input.LA(1) == GSTR ) 
                    	{
                    	    input.Consume();
                    	    if ( state.backtracking == 0 ) root_0 = (object)adaptor.BecomeRoot((object)adaptor.Create(set73), root_0);
                    	    state.errorRecovery = false;state.failed = false;
                    	}
                    	else 
                    	{
                    	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                    	    MismatchedSetException mse = new MismatchedSetException(null,input);
                    	    throw mse;
                    	}

                    	SYMBOL74=(IToken)Match(input,SYMBOL,FOLLOW_SYMBOL_in_data_directive1558); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{SYMBOL74_tree = (object)adaptor.Create(SYMBOL74);
                    		adaptor.AddChild(root_0, SYMBOL74_tree);
                    	}
                    	COMMA75=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_data_directive1560); if (state.failed) return retval;
                    	STRING76=(IToken)Match(input,STRING,FOLLOW_STRING_in_data_directive1563); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING76_tree = (object)adaptor.Create(STRING76);
                    		adaptor.AddChild(root_0, STRING76_tree);
                    	}

                    }
                    break;
                case 7 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:241:4: GVAR SYMBOL ( EQUALS expr ( COMMA SYMBOL )? )?
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	GVAR77=(IToken)Match(input,GVAR,FOLLOW_GVAR_in_data_directive1568); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{GVAR77_tree = (object)adaptor.Create(GVAR77);
                    		root_0 = (object)adaptor.BecomeRoot(GVAR77_tree, root_0);
                    	}
                    	SYMBOL78=(IToken)Match(input,SYMBOL,FOLLOW_SYMBOL_in_data_directive1571); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{SYMBOL78_tree = (object)adaptor.Create(SYMBOL78);
                    		adaptor.AddChild(root_0, SYMBOL78_tree);
                    	}
                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:241:17: ( EQUALS expr ( COMMA SYMBOL )? )?
                    	int alt35 = 2;
                    	int LA35_0 = input.LA(1);

                    	if ( (LA35_0 == EQUALS) )
                    	{
                    	    alt35 = 1;
                    	}
                    	switch (alt35) 
                    	{
                    	    case 1 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:241:18: EQUALS expr ( COMMA SYMBOL )?
                    	        {
                    	        	EQUALS79=(IToken)Match(input,EQUALS,FOLLOW_EQUALS_in_data_directive1574); if (state.failed) return retval;
                    	        	PushFollow(FOLLOW_expr_in_data_directive1577);
                    	        	expr80 = expr();
                    	        	state.followingStackPointer--;
                    	        	if (state.failed) return retval;
                    	        	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr80.Tree);
                    	        	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:241:31: ( COMMA SYMBOL )?
                    	        	int alt34 = 2;
                    	        	int LA34_0 = input.LA(1);

                    	        	if ( (LA34_0 == COMMA) )
                    	        	{
                    	        	    alt34 = 1;
                    	        	}
                    	        	switch (alt34) 
                    	        	{
                    	        	    case 1 :
                    	        	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:241:32: COMMA SYMBOL
                    	        	        {
                    	        	        	COMMA81=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_data_directive1580); if (state.failed) return retval;
                    	        	        	SYMBOL82=(IToken)Match(input,SYMBOL,FOLLOW_SYMBOL_in_data_directive1583); if (state.failed) return retval;
                    	        	        	if ( state.backtracking == 0 )
                    	        	        	{SYMBOL82_tree = (object)adaptor.Create(SYMBOL82);
                    	        	        		adaptor.AddChild(root_0, SYMBOL82_tree);
                    	        	        	}

                    	        	        }
                    	        	        break;

                    	        	}


                    	        }
                    	        break;

                    	}


                    }
                    break;
                case 8 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:242:4: OBJECT expr COMMA expr COMMA expr ( COMMA expr )? COMMA expr COMMA expr COMMA expr COMMA expr
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	OBJECT83=(IToken)Match(input,OBJECT,FOLLOW_OBJECT_in_data_directive1592); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{OBJECT83_tree = (object)adaptor.Create(OBJECT83);
                    		root_0 = (object)adaptor.BecomeRoot(OBJECT83_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_expr_in_data_directive1595);
                    	expr84 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr84.Tree);
                    	COMMA85=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_data_directive1599); if (state.failed) return retval;
                    	PushFollow(FOLLOW_expr_in_data_directive1602);
                    	expr86 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr86.Tree);
                    	COMMA87=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_data_directive1604); if (state.failed) return retval;
                    	PushFollow(FOLLOW_expr_in_data_directive1607);
                    	expr88 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr88.Tree);
                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:243:27: ( COMMA expr )?
                    	int alt36 = 2;
                    	alt36 = dfa36.Predict(input);
                    	switch (alt36) 
                    	{
                    	    case 1 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:243:28: COMMA expr
                    	        {
                    	        	COMMA89=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_data_directive1610); if (state.failed) return retval;
                    	        	PushFollow(FOLLOW_expr_in_data_directive1613);
                    	        	expr90 = expr();
                    	        	state.followingStackPointer--;
                    	        	if (state.failed) return retval;
                    	        	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr90.Tree);

                    	        }
                    	        break;

                    	}

                    	COMMA91=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_data_directive1619); if (state.failed) return retval;
                    	PushFollow(FOLLOW_expr_in_data_directive1622);
                    	expr92 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr92.Tree);
                    	COMMA93=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_data_directive1624); if (state.failed) return retval;
                    	PushFollow(FOLLOW_expr_in_data_directive1627);
                    	expr94 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr94.Tree);
                    	COMMA95=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_data_directive1629); if (state.failed) return retval;
                    	PushFollow(FOLLOW_expr_in_data_directive1632);
                    	expr96 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr96.Tree);
                    	COMMA97=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_data_directive1636); if (state.failed) return retval;
                    	PushFollow(FOLLOW_expr_in_data_directive1639);
                    	expr98 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr98.Tree);

                    }
                    break;

            }
            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "data_directive"

    public class funct_directive_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "funct_directive"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:248:1: funct_directive : FUNCT SYMBOL ( COMMA funct_param )* ;
    public ZapParser.funct_directive_return funct_directive() // throws RecognitionException [1]
    {   
        ZapParser.funct_directive_return retval = new ZapParser.funct_directive_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken FUNCT99 = null;
        IToken SYMBOL100 = null;
        IToken COMMA101 = null;
        ZapParser.funct_param_return funct_param102 = default(ZapParser.funct_param_return);


        object FUNCT99_tree=null;
        object SYMBOL100_tree=null;
        object COMMA101_tree=null;

        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:249:2: ( FUNCT SYMBOL ( COMMA funct_param )* )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:249:4: FUNCT SYMBOL ( COMMA funct_param )*
            {
            	root_0 = (object)adaptor.GetNilNode();

            	FUNCT99=(IToken)Match(input,FUNCT,FOLLOW_FUNCT_in_funct_directive1650); if (state.failed) return retval;
            	if ( state.backtracking == 0 )
            	{FUNCT99_tree = (object)adaptor.Create(FUNCT99);
            		root_0 = (object)adaptor.BecomeRoot(FUNCT99_tree, root_0);
            	}
            	SYMBOL100=(IToken)Match(input,SYMBOL,FOLLOW_SYMBOL_in_funct_directive1653); if (state.failed) return retval;
            	if ( state.backtracking == 0 )
            	{SYMBOL100_tree = (object)adaptor.Create(SYMBOL100);
            		adaptor.AddChild(root_0, SYMBOL100_tree);
            	}
            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:249:18: ( COMMA funct_param )*
            	do 
            	{
            	    int alt38 = 2;
            	    int LA38_0 = input.LA(1);

            	    if ( (LA38_0 == COMMA) )
            	    {
            	        alt38 = 1;
            	    }


            	    switch (alt38) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:249:19: COMMA funct_param
            			    {
            			    	COMMA101=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_funct_directive1656); if (state.failed) return retval;
            			    	PushFollow(FOLLOW_funct_param_in_funct_directive1659);
            			    	funct_param102 = funct_param();
            			    	state.followingStackPointer--;
            			    	if (state.failed) return retval;
            			    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, funct_param102.Tree);

            			    }
            			    break;

            			default:
            			    goto loop38;
            	    }
            	} while (true);

            	loop38:
            		;	// Stops C# compiler whining that label 'loop38' has no statements


            }

            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "funct_directive"

    public class funct_param_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "funct_param"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:252:1: funct_param : symbol ( EQUALS expr )? ;
    public ZapParser.funct_param_return funct_param() // throws RecognitionException [1]
    {   
        ZapParser.funct_param_return retval = new ZapParser.funct_param_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken EQUALS104 = null;
        ZapParser.symbol_return symbol103 = default(ZapParser.symbol_return);

        ZapParser.expr_return expr105 = default(ZapParser.expr_return);


        object EQUALS104_tree=null;

        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:253:2: ( symbol ( EQUALS expr )? )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:253:4: symbol ( EQUALS expr )?
            {
            	root_0 = (object)adaptor.GetNilNode();

            	PushFollow(FOLLOW_symbol_in_funct_param1672);
            	symbol103 = symbol();
            	state.followingStackPointer--;
            	if (state.failed) return retval;
            	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, symbol103.Tree);
            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:253:11: ( EQUALS expr )?
            	int alt39 = 2;
            	int LA39_0 = input.LA(1);

            	if ( (LA39_0 == EQUALS) )
            	{
            	    alt39 = 1;
            	}
            	switch (alt39) 
            	{
            	    case 1 :
            	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:253:12: EQUALS expr
            	        {
            	        	EQUALS104=(IToken)Match(input,EQUALS,FOLLOW_EQUALS_in_funct_param1675); if (state.failed) return retval;
            	        	if ( state.backtracking == 0 )
            	        	{EQUALS104_tree = (object)adaptor.Create(EQUALS104);
            	        		root_0 = (object)adaptor.BecomeRoot(EQUALS104_tree, root_0);
            	        	}
            	        	PushFollow(FOLLOW_expr_in_funct_param1678);
            	        	expr105 = expr();
            	        	state.followingStackPointer--;
            	        	if (state.failed) return retval;
            	        	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr105.Tree);

            	        }
            	        break;

            	}


            }

            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "funct_param"

    public class table_directive_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "table_directive"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:256:1: table_directive : ( TABLE ( NUM )? | ENDT | VOCBEG NUM COMMA NUM | VOCEND );
    public ZapParser.table_directive_return table_directive() // throws RecognitionException [1]
    {   
        ZapParser.table_directive_return retval = new ZapParser.table_directive_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken TABLE106 = null;
        IToken NUM107 = null;
        IToken ENDT108 = null;
        IToken VOCBEG109 = null;
        IToken NUM110 = null;
        IToken COMMA111 = null;
        IToken NUM112 = null;
        IToken VOCEND113 = null;

        object TABLE106_tree=null;
        object NUM107_tree=null;
        object ENDT108_tree=null;
        object VOCBEG109_tree=null;
        object NUM110_tree=null;
        object COMMA111_tree=null;
        object NUM112_tree=null;
        object VOCEND113_tree=null;

        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:257:2: ( TABLE ( NUM )? | ENDT | VOCBEG NUM COMMA NUM | VOCEND )
            int alt41 = 4;
            switch ( input.LA(1) ) 
            {
            case TABLE:
            	{
                alt41 = 1;
                }
                break;
            case ENDT:
            	{
                alt41 = 2;
                }
                break;
            case VOCBEG:
            	{
                alt41 = 3;
                }
                break;
            case VOCEND:
            	{
                alt41 = 4;
                }
                break;
            	default:
            	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            	    NoViableAltException nvae_d41s0 =
            	        new NoViableAltException("", 41, 0, input);

            	    throw nvae_d41s0;
            }

            switch (alt41) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:257:4: TABLE ( NUM )?
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	TABLE106=(IToken)Match(input,TABLE,FOLLOW_TABLE_in_table_directive1691); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{TABLE106_tree = (object)adaptor.Create(TABLE106);
                    		root_0 = (object)adaptor.BecomeRoot(TABLE106_tree, root_0);
                    	}
                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:257:11: ( NUM )?
                    	int alt40 = 2;
                    	int LA40_0 = input.LA(1);

                    	if ( (LA40_0 == NUM) )
                    	{
                    	    alt40 = 1;
                    	}
                    	switch (alt40) 
                    	{
                    	    case 1 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:257:11: NUM
                    	        {
                    	        	NUM107=(IToken)Match(input,NUM,FOLLOW_NUM_in_table_directive1694); if (state.failed) return retval;
                    	        	if ( state.backtracking == 0 )
                    	        	{NUM107_tree = (object)adaptor.Create(NUM107);
                    	        		adaptor.AddChild(root_0, NUM107_tree);
                    	        	}

                    	        }
                    	        break;

                    	}


                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:258:4: ENDT
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	ENDT108=(IToken)Match(input,ENDT,FOLLOW_ENDT_in_table_directive1700); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{ENDT108_tree = (object)adaptor.Create(ENDT108);
                    		adaptor.AddChild(root_0, ENDT108_tree);
                    	}

                    }
                    break;
                case 3 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:259:4: VOCBEG NUM COMMA NUM
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	VOCBEG109=(IToken)Match(input,VOCBEG,FOLLOW_VOCBEG_in_table_directive1705); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{VOCBEG109_tree = (object)adaptor.Create(VOCBEG109);
                    		root_0 = (object)adaptor.BecomeRoot(VOCBEG109_tree, root_0);
                    	}
                    	NUM110=(IToken)Match(input,NUM,FOLLOW_NUM_in_table_directive1708); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{NUM110_tree = (object)adaptor.Create(NUM110);
                    		adaptor.AddChild(root_0, NUM110_tree);
                    	}
                    	COMMA111=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_table_directive1710); if (state.failed) return retval;
                    	NUM112=(IToken)Match(input,NUM,FOLLOW_NUM_in_table_directive1713); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{NUM112_tree = (object)adaptor.Create(NUM112);
                    		adaptor.AddChild(root_0, NUM112_tree);
                    	}

                    }
                    break;
                case 4 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:260:4: VOCEND
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	VOCEND113=(IToken)Match(input,VOCEND,FOLLOW_VOCEND_in_table_directive1718); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{VOCEND113_tree = (object)adaptor.Create(VOCEND113);
                    		adaptor.AddChild(root_0, VOCEND113_tree);
                    	}

                    }
                    break;

            }
            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "table_directive"

    public class debug_directive_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "debug_directive"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:263:1: debug_directive : ( DEBUG_ACTION expr COMMA STRING | DEBUG_ARRAY expr COMMA STRING | DEBUG_ATTR expr COMMA STRING | DEBUG_CLASS STRING COMMA debug_lineref COMMA debug_lineref | DEBUG_FAKE_ACTION expr COMMA STRING | DEBUG_FILE expr COMMA STRING COMMA STRING | DEBUG_GLOBAL expr COMMA STRING | DEBUG_LINE debug_lineref | DEBUG_MAP STRING EQUALS expr | DEBUG_OBJECT expr COMMA STRING COMMA debug_lineref COMMA debug_lineref | DEBUG_PROP expr COMMA STRING | DEBUG_ROUTINE debug_lineref COMMA STRING ( COMMA STRING )* | DEBUG_ROUTINE_END debug_lineref );
    public ZapParser.debug_directive_return debug_directive() // throws RecognitionException [1]
    {   
        ZapParser.debug_directive_return retval = new ZapParser.debug_directive_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken DEBUG_ACTION114 = null;
        IToken COMMA116 = null;
        IToken STRING117 = null;
        IToken DEBUG_ARRAY118 = null;
        IToken COMMA120 = null;
        IToken STRING121 = null;
        IToken DEBUG_ATTR122 = null;
        IToken COMMA124 = null;
        IToken STRING125 = null;
        IToken DEBUG_CLASS126 = null;
        IToken STRING127 = null;
        IToken COMMA128 = null;
        IToken COMMA130 = null;
        IToken DEBUG_FAKE_ACTION132 = null;
        IToken COMMA134 = null;
        IToken STRING135 = null;
        IToken DEBUG_FILE136 = null;
        IToken COMMA138 = null;
        IToken STRING139 = null;
        IToken COMMA140 = null;
        IToken STRING141 = null;
        IToken DEBUG_GLOBAL142 = null;
        IToken COMMA144 = null;
        IToken STRING145 = null;
        IToken DEBUG_LINE146 = null;
        IToken DEBUG_MAP148 = null;
        IToken STRING149 = null;
        IToken EQUALS150 = null;
        IToken DEBUG_OBJECT152 = null;
        IToken COMMA154 = null;
        IToken STRING155 = null;
        IToken COMMA156 = null;
        IToken COMMA158 = null;
        IToken DEBUG_PROP160 = null;
        IToken COMMA162 = null;
        IToken STRING163 = null;
        IToken DEBUG_ROUTINE164 = null;
        IToken COMMA166 = null;
        IToken STRING167 = null;
        IToken COMMA168 = null;
        IToken STRING169 = null;
        IToken DEBUG_ROUTINE_END170 = null;
        ZapParser.expr_return expr115 = default(ZapParser.expr_return);

        ZapParser.expr_return expr119 = default(ZapParser.expr_return);

        ZapParser.expr_return expr123 = default(ZapParser.expr_return);

        ZapParser.debug_lineref_return debug_lineref129 = default(ZapParser.debug_lineref_return);

        ZapParser.debug_lineref_return debug_lineref131 = default(ZapParser.debug_lineref_return);

        ZapParser.expr_return expr133 = default(ZapParser.expr_return);

        ZapParser.expr_return expr137 = default(ZapParser.expr_return);

        ZapParser.expr_return expr143 = default(ZapParser.expr_return);

        ZapParser.debug_lineref_return debug_lineref147 = default(ZapParser.debug_lineref_return);

        ZapParser.expr_return expr151 = default(ZapParser.expr_return);

        ZapParser.expr_return expr153 = default(ZapParser.expr_return);

        ZapParser.debug_lineref_return debug_lineref157 = default(ZapParser.debug_lineref_return);

        ZapParser.debug_lineref_return debug_lineref159 = default(ZapParser.debug_lineref_return);

        ZapParser.expr_return expr161 = default(ZapParser.expr_return);

        ZapParser.debug_lineref_return debug_lineref165 = default(ZapParser.debug_lineref_return);

        ZapParser.debug_lineref_return debug_lineref171 = default(ZapParser.debug_lineref_return);


        object DEBUG_ACTION114_tree=null;
        object COMMA116_tree=null;
        object STRING117_tree=null;
        object DEBUG_ARRAY118_tree=null;
        object COMMA120_tree=null;
        object STRING121_tree=null;
        object DEBUG_ATTR122_tree=null;
        object COMMA124_tree=null;
        object STRING125_tree=null;
        object DEBUG_CLASS126_tree=null;
        object STRING127_tree=null;
        object COMMA128_tree=null;
        object COMMA130_tree=null;
        object DEBUG_FAKE_ACTION132_tree=null;
        object COMMA134_tree=null;
        object STRING135_tree=null;
        object DEBUG_FILE136_tree=null;
        object COMMA138_tree=null;
        object STRING139_tree=null;
        object COMMA140_tree=null;
        object STRING141_tree=null;
        object DEBUG_GLOBAL142_tree=null;
        object COMMA144_tree=null;
        object STRING145_tree=null;
        object DEBUG_LINE146_tree=null;
        object DEBUG_MAP148_tree=null;
        object STRING149_tree=null;
        object EQUALS150_tree=null;
        object DEBUG_OBJECT152_tree=null;
        object COMMA154_tree=null;
        object STRING155_tree=null;
        object COMMA156_tree=null;
        object COMMA158_tree=null;
        object DEBUG_PROP160_tree=null;
        object COMMA162_tree=null;
        object STRING163_tree=null;
        object DEBUG_ROUTINE164_tree=null;
        object COMMA166_tree=null;
        object STRING167_tree=null;
        object COMMA168_tree=null;
        object STRING169_tree=null;
        object DEBUG_ROUTINE_END170_tree=null;

        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:264:2: ( DEBUG_ACTION expr COMMA STRING | DEBUG_ARRAY expr COMMA STRING | DEBUG_ATTR expr COMMA STRING | DEBUG_CLASS STRING COMMA debug_lineref COMMA debug_lineref | DEBUG_FAKE_ACTION expr COMMA STRING | DEBUG_FILE expr COMMA STRING COMMA STRING | DEBUG_GLOBAL expr COMMA STRING | DEBUG_LINE debug_lineref | DEBUG_MAP STRING EQUALS expr | DEBUG_OBJECT expr COMMA STRING COMMA debug_lineref COMMA debug_lineref | DEBUG_PROP expr COMMA STRING | DEBUG_ROUTINE debug_lineref COMMA STRING ( COMMA STRING )* | DEBUG_ROUTINE_END debug_lineref )
            int alt43 = 13;
            switch ( input.LA(1) ) 
            {
            case DEBUG_ACTION:
            	{
                alt43 = 1;
                }
                break;
            case DEBUG_ARRAY:
            	{
                alt43 = 2;
                }
                break;
            case DEBUG_ATTR:
            	{
                alt43 = 3;
                }
                break;
            case DEBUG_CLASS:
            	{
                alt43 = 4;
                }
                break;
            case DEBUG_FAKE_ACTION:
            	{
                alt43 = 5;
                }
                break;
            case DEBUG_FILE:
            	{
                alt43 = 6;
                }
                break;
            case DEBUG_GLOBAL:
            	{
                alt43 = 7;
                }
                break;
            case DEBUG_LINE:
            	{
                alt43 = 8;
                }
                break;
            case DEBUG_MAP:
            	{
                alt43 = 9;
                }
                break;
            case DEBUG_OBJECT:
            	{
                alt43 = 10;
                }
                break;
            case DEBUG_PROP:
            	{
                alt43 = 11;
                }
                break;
            case DEBUG_ROUTINE:
            	{
                alt43 = 12;
                }
                break;
            case DEBUG_ROUTINE_END:
            	{
                alt43 = 13;
                }
                break;
            	default:
            	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            	    NoViableAltException nvae_d43s0 =
            	        new NoViableAltException("", 43, 0, input);

            	    throw nvae_d43s0;
            }

            switch (alt43) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:264:4: DEBUG_ACTION expr COMMA STRING
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	DEBUG_ACTION114=(IToken)Match(input,DEBUG_ACTION,FOLLOW_DEBUG_ACTION_in_debug_directive1729); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{DEBUG_ACTION114_tree = (object)adaptor.Create(DEBUG_ACTION114);
                    		root_0 = (object)adaptor.BecomeRoot(DEBUG_ACTION114_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_expr_in_debug_directive1732);
                    	expr115 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr115.Tree);
                    	COMMA116=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_directive1734); if (state.failed) return retval;
                    	STRING117=(IToken)Match(input,STRING,FOLLOW_STRING_in_debug_directive1737); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING117_tree = (object)adaptor.Create(STRING117);
                    		adaptor.AddChild(root_0, STRING117_tree);
                    	}

                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:265:4: DEBUG_ARRAY expr COMMA STRING
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	DEBUG_ARRAY118=(IToken)Match(input,DEBUG_ARRAY,FOLLOW_DEBUG_ARRAY_in_debug_directive1742); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{DEBUG_ARRAY118_tree = (object)adaptor.Create(DEBUG_ARRAY118);
                    		root_0 = (object)adaptor.BecomeRoot(DEBUG_ARRAY118_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_expr_in_debug_directive1745);
                    	expr119 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr119.Tree);
                    	COMMA120=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_directive1747); if (state.failed) return retval;
                    	STRING121=(IToken)Match(input,STRING,FOLLOW_STRING_in_debug_directive1750); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING121_tree = (object)adaptor.Create(STRING121);
                    		adaptor.AddChild(root_0, STRING121_tree);
                    	}

                    }
                    break;
                case 3 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:266:4: DEBUG_ATTR expr COMMA STRING
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	DEBUG_ATTR122=(IToken)Match(input,DEBUG_ATTR,FOLLOW_DEBUG_ATTR_in_debug_directive1755); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{DEBUG_ATTR122_tree = (object)adaptor.Create(DEBUG_ATTR122);
                    		root_0 = (object)adaptor.BecomeRoot(DEBUG_ATTR122_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_expr_in_debug_directive1758);
                    	expr123 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr123.Tree);
                    	COMMA124=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_directive1760); if (state.failed) return retval;
                    	STRING125=(IToken)Match(input,STRING,FOLLOW_STRING_in_debug_directive1763); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING125_tree = (object)adaptor.Create(STRING125);
                    		adaptor.AddChild(root_0, STRING125_tree);
                    	}

                    }
                    break;
                case 4 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:267:4: DEBUG_CLASS STRING COMMA debug_lineref COMMA debug_lineref
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	DEBUG_CLASS126=(IToken)Match(input,DEBUG_CLASS,FOLLOW_DEBUG_CLASS_in_debug_directive1768); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{DEBUG_CLASS126_tree = (object)adaptor.Create(DEBUG_CLASS126);
                    		root_0 = (object)adaptor.BecomeRoot(DEBUG_CLASS126_tree, root_0);
                    	}
                    	STRING127=(IToken)Match(input,STRING,FOLLOW_STRING_in_debug_directive1771); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING127_tree = (object)adaptor.Create(STRING127);
                    		adaptor.AddChild(root_0, STRING127_tree);
                    	}
                    	COMMA128=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_directive1773); if (state.failed) return retval;
                    	PushFollow(FOLLOW_debug_lineref_in_debug_directive1776);
                    	debug_lineref129 = debug_lineref();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, debug_lineref129.Tree);
                    	COMMA130=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_directive1778); if (state.failed) return retval;
                    	PushFollow(FOLLOW_debug_lineref_in_debug_directive1781);
                    	debug_lineref131 = debug_lineref();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, debug_lineref131.Tree);

                    }
                    break;
                case 5 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:268:4: DEBUG_FAKE_ACTION expr COMMA STRING
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	DEBUG_FAKE_ACTION132=(IToken)Match(input,DEBUG_FAKE_ACTION,FOLLOW_DEBUG_FAKE_ACTION_in_debug_directive1786); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{DEBUG_FAKE_ACTION132_tree = (object)adaptor.Create(DEBUG_FAKE_ACTION132);
                    		root_0 = (object)adaptor.BecomeRoot(DEBUG_FAKE_ACTION132_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_expr_in_debug_directive1789);
                    	expr133 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr133.Tree);
                    	COMMA134=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_directive1791); if (state.failed) return retval;
                    	STRING135=(IToken)Match(input,STRING,FOLLOW_STRING_in_debug_directive1794); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING135_tree = (object)adaptor.Create(STRING135);
                    		adaptor.AddChild(root_0, STRING135_tree);
                    	}

                    }
                    break;
                case 6 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:269:4: DEBUG_FILE expr COMMA STRING COMMA STRING
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	DEBUG_FILE136=(IToken)Match(input,DEBUG_FILE,FOLLOW_DEBUG_FILE_in_debug_directive1799); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{DEBUG_FILE136_tree = (object)adaptor.Create(DEBUG_FILE136);
                    		root_0 = (object)adaptor.BecomeRoot(DEBUG_FILE136_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_expr_in_debug_directive1802);
                    	expr137 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr137.Tree);
                    	COMMA138=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_directive1804); if (state.failed) return retval;
                    	STRING139=(IToken)Match(input,STRING,FOLLOW_STRING_in_debug_directive1807); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING139_tree = (object)adaptor.Create(STRING139);
                    		adaptor.AddChild(root_0, STRING139_tree);
                    	}
                    	COMMA140=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_directive1809); if (state.failed) return retval;
                    	STRING141=(IToken)Match(input,STRING,FOLLOW_STRING_in_debug_directive1812); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING141_tree = (object)adaptor.Create(STRING141);
                    		adaptor.AddChild(root_0, STRING141_tree);
                    	}

                    }
                    break;
                case 7 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:270:4: DEBUG_GLOBAL expr COMMA STRING
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	DEBUG_GLOBAL142=(IToken)Match(input,DEBUG_GLOBAL,FOLLOW_DEBUG_GLOBAL_in_debug_directive1817); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{DEBUG_GLOBAL142_tree = (object)adaptor.Create(DEBUG_GLOBAL142);
                    		root_0 = (object)adaptor.BecomeRoot(DEBUG_GLOBAL142_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_expr_in_debug_directive1820);
                    	expr143 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr143.Tree);
                    	COMMA144=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_directive1822); if (state.failed) return retval;
                    	STRING145=(IToken)Match(input,STRING,FOLLOW_STRING_in_debug_directive1825); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING145_tree = (object)adaptor.Create(STRING145);
                    		adaptor.AddChild(root_0, STRING145_tree);
                    	}

                    }
                    break;
                case 8 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:271:4: DEBUG_LINE debug_lineref
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	DEBUG_LINE146=(IToken)Match(input,DEBUG_LINE,FOLLOW_DEBUG_LINE_in_debug_directive1830); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{DEBUG_LINE146_tree = (object)adaptor.Create(DEBUG_LINE146);
                    		root_0 = (object)adaptor.BecomeRoot(DEBUG_LINE146_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_debug_lineref_in_debug_directive1833);
                    	debug_lineref147 = debug_lineref();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, debug_lineref147.Tree);

                    }
                    break;
                case 9 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:272:4: DEBUG_MAP STRING EQUALS expr
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	DEBUG_MAP148=(IToken)Match(input,DEBUG_MAP,FOLLOW_DEBUG_MAP_in_debug_directive1838); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{DEBUG_MAP148_tree = (object)adaptor.Create(DEBUG_MAP148);
                    		root_0 = (object)adaptor.BecomeRoot(DEBUG_MAP148_tree, root_0);
                    	}
                    	STRING149=(IToken)Match(input,STRING,FOLLOW_STRING_in_debug_directive1841); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING149_tree = (object)adaptor.Create(STRING149);
                    		adaptor.AddChild(root_0, STRING149_tree);
                    	}
                    	EQUALS150=(IToken)Match(input,EQUALS,FOLLOW_EQUALS_in_debug_directive1843); if (state.failed) return retval;
                    	PushFollow(FOLLOW_expr_in_debug_directive1846);
                    	expr151 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr151.Tree);

                    }
                    break;
                case 10 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:273:4: DEBUG_OBJECT expr COMMA STRING COMMA debug_lineref COMMA debug_lineref
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	DEBUG_OBJECT152=(IToken)Match(input,DEBUG_OBJECT,FOLLOW_DEBUG_OBJECT_in_debug_directive1851); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{DEBUG_OBJECT152_tree = (object)adaptor.Create(DEBUG_OBJECT152);
                    		root_0 = (object)adaptor.BecomeRoot(DEBUG_OBJECT152_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_expr_in_debug_directive1854);
                    	expr153 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr153.Tree);
                    	COMMA154=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_directive1856); if (state.failed) return retval;
                    	STRING155=(IToken)Match(input,STRING,FOLLOW_STRING_in_debug_directive1859); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING155_tree = (object)adaptor.Create(STRING155);
                    		adaptor.AddChild(root_0, STRING155_tree);
                    	}
                    	COMMA156=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_directive1861); if (state.failed) return retval;
                    	PushFollow(FOLLOW_debug_lineref_in_debug_directive1864);
                    	debug_lineref157 = debug_lineref();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, debug_lineref157.Tree);
                    	COMMA158=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_directive1866); if (state.failed) return retval;
                    	PushFollow(FOLLOW_debug_lineref_in_debug_directive1869);
                    	debug_lineref159 = debug_lineref();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, debug_lineref159.Tree);

                    }
                    break;
                case 11 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:274:4: DEBUG_PROP expr COMMA STRING
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	DEBUG_PROP160=(IToken)Match(input,DEBUG_PROP,FOLLOW_DEBUG_PROP_in_debug_directive1874); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{DEBUG_PROP160_tree = (object)adaptor.Create(DEBUG_PROP160);
                    		root_0 = (object)adaptor.BecomeRoot(DEBUG_PROP160_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_expr_in_debug_directive1877);
                    	expr161 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr161.Tree);
                    	COMMA162=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_directive1879); if (state.failed) return retval;
                    	STRING163=(IToken)Match(input,STRING,FOLLOW_STRING_in_debug_directive1882); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING163_tree = (object)adaptor.Create(STRING163);
                    		adaptor.AddChild(root_0, STRING163_tree);
                    	}

                    }
                    break;
                case 12 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:275:4: DEBUG_ROUTINE debug_lineref COMMA STRING ( COMMA STRING )*
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	DEBUG_ROUTINE164=(IToken)Match(input,DEBUG_ROUTINE,FOLLOW_DEBUG_ROUTINE_in_debug_directive1887); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{DEBUG_ROUTINE164_tree = (object)adaptor.Create(DEBUG_ROUTINE164);
                    		root_0 = (object)adaptor.BecomeRoot(DEBUG_ROUTINE164_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_debug_lineref_in_debug_directive1890);
                    	debug_lineref165 = debug_lineref();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, debug_lineref165.Tree);
                    	COMMA166=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_directive1892); if (state.failed) return retval;
                    	STRING167=(IToken)Match(input,STRING,FOLLOW_STRING_in_debug_directive1895); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING167_tree = (object)adaptor.Create(STRING167);
                    		adaptor.AddChild(root_0, STRING167_tree);
                    	}
                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:275:47: ( COMMA STRING )*
                    	do 
                    	{
                    	    int alt42 = 2;
                    	    int LA42_0 = input.LA(1);

                    	    if ( (LA42_0 == COMMA) )
                    	    {
                    	        alt42 = 1;
                    	    }


                    	    switch (alt42) 
                    		{
                    			case 1 :
                    			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:275:48: COMMA STRING
                    			    {
                    			    	COMMA168=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_directive1898); if (state.failed) return retval;
                    			    	STRING169=(IToken)Match(input,STRING,FOLLOW_STRING_in_debug_directive1901); if (state.failed) return retval;
                    			    	if ( state.backtracking == 0 )
                    			    	{STRING169_tree = (object)adaptor.Create(STRING169);
                    			    		adaptor.AddChild(root_0, STRING169_tree);
                    			    	}

                    			    }
                    			    break;

                    			default:
                    			    goto loop42;
                    	    }
                    	} while (true);

                    	loop42:
                    		;	// Stops C# compiler whining that label 'loop42' has no statements


                    }
                    break;
                case 13 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:276:5: DEBUG_ROUTINE_END debug_lineref
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	DEBUG_ROUTINE_END170=(IToken)Match(input,DEBUG_ROUTINE_END,FOLLOW_DEBUG_ROUTINE_END_in_debug_directive1909); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{DEBUG_ROUTINE_END170_tree = (object)adaptor.Create(DEBUG_ROUTINE_END170);
                    		root_0 = (object)adaptor.BecomeRoot(DEBUG_ROUTINE_END170_tree, root_0);
                    	}
                    	PushFollow(FOLLOW_debug_lineref_in_debug_directive1912);
                    	debug_lineref171 = debug_lineref();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, debug_lineref171.Tree);

                    }
                    break;

            }
            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "debug_directive"

    public class debug_lineref_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "debug_lineref"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:279:1: debug_lineref : expr COMMA expr COMMA expr ;
    public ZapParser.debug_lineref_return debug_lineref() // throws RecognitionException [1]
    {   
        ZapParser.debug_lineref_return retval = new ZapParser.debug_lineref_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken COMMA173 = null;
        IToken COMMA175 = null;
        ZapParser.expr_return expr172 = default(ZapParser.expr_return);

        ZapParser.expr_return expr174 = default(ZapParser.expr_return);

        ZapParser.expr_return expr176 = default(ZapParser.expr_return);


        object COMMA173_tree=null;
        object COMMA175_tree=null;

        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:280:2: ( expr COMMA expr COMMA expr )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:280:4: expr COMMA expr COMMA expr
            {
            	root_0 = (object)adaptor.GetNilNode();

            	PushFollow(FOLLOW_expr_in_debug_lineref1923);
            	expr172 = expr();
            	state.followingStackPointer--;
            	if (state.failed) return retval;
            	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr172.Tree);
            	COMMA173=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_lineref1925); if (state.failed) return retval;
            	PushFollow(FOLLOW_expr_in_debug_lineref1928);
            	expr174 = expr();
            	state.followingStackPointer--;
            	if (state.failed) return retval;
            	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr174.Tree);
            	COMMA175=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_debug_lineref1930); if (state.failed) return retval;
            	PushFollow(FOLLOW_expr_in_debug_lineref1933);
            	expr176 = expr();
            	state.followingStackPointer--;
            	if (state.failed) return retval;
            	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, expr176.Tree);

            }

            retval.Stop = input.LT(-1);

            if ( (state.backtracking==0) )
            {	retval.Tree = (object)adaptor.RulePostProcessing(root_0);
            	adaptor.SetTokenBoundaries(retval.Tree, (IToken) retval.Start, (IToken) retval.Stop);}
        }
        catch (RecognitionException re) 
    	{
            ReportError(re);
            Recover(input,re);
    	// Conversion of the second argument necessary, but harmless
    	retval.Tree = (object)adaptor.ErrorNode(input, (IToken) retval.Start, input.LT(-1), re);

        }
        finally 
    	{
        }
        return retval;
    }
    // $ANTLR end "debug_lineref"

    // $ANTLR start "synpred1_Zap"
    public void synpred1_Zap_fragment() {
        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:173:5: ( expr | STRING | SLASH | BACKSLASH | RANGLE | QUEST | ARROW )
        int alt44 = 7;
        switch ( input.LA(1) ) 
        {
        case APOSTROPHE:
        case NUM:
        case SYMBOL:
        case OPCODE:
        	{
            alt44 = 1;
            }
            break;
        case STRING:
        	{
            alt44 = 2;
            }
            break;
        case SLASH:
        	{
            alt44 = 3;
            }
            break;
        case BACKSLASH:
        	{
            alt44 = 4;
            }
            break;
        case RANGLE:
        	{
            alt44 = 5;
            }
            break;
        case QUEST:
        	{
            alt44 = 6;
            }
            break;
        case ARROW:
        	{
            alt44 = 7;
            }
            break;
        	default:
        	    if ( state.backtracking > 0 ) {state.failed = true; return ;}
        	    NoViableAltException nvae_d44s0 =
        	        new NoViableAltException("", 44, 0, input);

        	    throw nvae_d44s0;
        }

        switch (alt44) 
        {
            case 1 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:173:6: expr
                {
                	PushFollow(FOLLOW_expr_in_synpred1_Zap988);
                	expr();
                	state.followingStackPointer--;
                	if (state.failed) return ;

                }
                break;
            case 2 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:173:13: STRING
                {
                	Match(input,STRING,FOLLOW_STRING_in_synpred1_Zap992); if (state.failed) return ;

                }
                break;
            case 3 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:173:22: SLASH
                {
                	Match(input,SLASH,FOLLOW_SLASH_in_synpred1_Zap996); if (state.failed) return ;

                }
                break;
            case 4 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:173:30: BACKSLASH
                {
                	Match(input,BACKSLASH,FOLLOW_BACKSLASH_in_synpred1_Zap1000); if (state.failed) return ;

                }
                break;
            case 5 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:173:42: RANGLE
                {
                	Match(input,RANGLE,FOLLOW_RANGLE_in_synpred1_Zap1004); if (state.failed) return ;

                }
                break;
            case 6 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:173:51: QUEST
                {
                	Match(input,QUEST,FOLLOW_QUEST_in_synpred1_Zap1008); if (state.failed) return ;

                }
                break;
            case 7 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\Zap.g:173:59: ARROW
                {
                	Match(input,ARROW,FOLLOW_ARROW_in_synpred1_Zap1012); if (state.failed) return ;

                }
                break;

        }}
    // $ANTLR end "synpred1_Zap"

    // Delegated rules

   	public bool synpred1_Zap() 
   	{
   	    state.backtracking++;
   	    int start = input.Mark();
   	    try 
   	    {
   	        synpred1_Zap_fragment(); // can never throw exception
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


   	protected DFA7 dfa7;
   	protected DFA13 dfa13;
   	protected DFA12 dfa12;
   	protected DFA14 dfa14;
   	protected DFA36 dfa36;
	private void InitializeCyclicDFAs()
	{
    	this.dfa7 = new DFA7(this);
    	this.dfa13 = new DFA13(this);
    	this.dfa12 = new DFA12(this);
    	this.dfa14 = new DFA14(this);
    	this.dfa36 = new DFA36(this);
	    this.dfa12.specialStateTransitionHandler = new DFA.SpecialStateTransitionHandler(DFA12_SpecialStateTransition);
	    this.dfa14.specialStateTransitionHandler = new DFA.SpecialStateTransitionHandler(DFA14_SpecialStateTransition);
	}

    const string DFA7_eotS =
        "\x04\uffff";
    const string DFA7_eofS =
        "\x02\x02\x02\uffff";
    const string DFA7_minS =
        "\x01\x49\x01\x0a\x02\uffff";
    const string DFA7_maxS =
        "\x02\x49\x02\uffff";
    const string DFA7_acceptS =
        "\x02\uffff\x01\x02\x01\x01";
    const string DFA7_specialS =
        "\x04\uffff}>";
    static readonly string[] DFA7_transitionS = {
            "\x01\x01",
            "\x01\x03\x02\uffff\x02\x03\x01\uffff\x07\x03\x01\uffff\x03"+
            "\x03\x05\uffff\x01\x03\x02\uffff\x16\x03\x06\uffff\x01\x03\x02"+
            "\uffff\x01\x03\x01\uffff\x02\x03\x03\uffff\x01\x01",
            "",
            ""
    };

    static readonly short[] DFA7_eot = DFA.UnpackEncodedString(DFA7_eotS);
    static readonly short[] DFA7_eof = DFA.UnpackEncodedString(DFA7_eofS);
    static readonly char[] DFA7_min = DFA.UnpackEncodedStringToUnsignedChars(DFA7_minS);
    static readonly char[] DFA7_max = DFA.UnpackEncodedStringToUnsignedChars(DFA7_maxS);
    static readonly short[] DFA7_accept = DFA.UnpackEncodedString(DFA7_acceptS);
    static readonly short[] DFA7_special = DFA.UnpackEncodedString(DFA7_specialS);
    static readonly short[][] DFA7_transition = DFA.UnpackEncodedStringArray(DFA7_transitionS);

    protected class DFA7 : DFA
    {
        public DFA7(BaseRecognizer recognizer)
        {
            this.recognizer = recognizer;
            this.decisionNumber = 7;
            this.eot = DFA7_eot;
            this.eof = DFA7_eof;
            this.min = DFA7_min;
            this.max = DFA7_max;
            this.accept = DFA7_accept;
            this.special = DFA7_special;
            this.transition = DFA7_transition;

        }

        override public string Description
        {
            get { return "()* loopback of 157:33: ( ( CRLF )+ ( label | ( label )? line ) )*"; }
        }

    }

    const string DFA13_eotS =
        "\x0a\uffff";
    const string DFA13_eofS =
        "\x01\uffff\x01\x09\x08\uffff";
    const string DFA13_minS =
        "\x01\x0a\x01\x06\x08\uffff";
    const string DFA13_maxS =
        "\x01\x45\x01\x4a\x08\uffff";
    const string DFA13_acceptS =
        "\x02\uffff\x01\x03\x01\x04\x01\x05\x01\x06\x01\x07\x01\x08\x01"+
        "\x02\x01\x01";
    const string DFA13_specialS =
        "\x0a\uffff}>";
    static readonly string[] DFA13_transitionS = {
            "\x01\x04\x02\uffff\x02\x03\x01\uffff\x01\x06\x02\x04\x01\x05"+
            "\x02\x04\x01\x03\x01\uffff\x01\x04\x01\x03\x01\x04\x05\uffff"+
            "\x01\x04\x02\uffff\x02\x04\x01\x06\x01\x03\x01\x04\x02\x06\x02"+
            "\x04\x0d\x07\x06\uffff\x01\x04\x02\uffff\x01\x04\x01\uffff\x01"+
            "\x02\x01\x01",
            "\x01\x09\x01\uffff\x01\x09\x32\uffff\x03\x09\x01\uffff\x01"+
            "\x09\x02\uffff\x01\x09\x01\uffff\x02\x09\x03\uffff\x01\x09\x01"+
            "\x08",
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
            get { return "168:1: line : ( OPCODE operands | OPCODE STRING | SYMBOL ( -> ^( WORD SYMBOL ) | ( expr | STRING | SLASH | BACKSLASH | RANGLE | QUEST | ARROW )=> ( operands -> ^( SYMBOL operands ) | STRING -> ^( SYMBOL STRING ) ) | EQUALS expr -> ^( EQUALS SYMBOL expr ) ) | meta_directive | data_directive | funct_directive | table_directive | debug_directive );"; }
        }

    }

    const string DFA12_eotS =
        "\x0f\uffff";
    const string DFA12_eofS =
        "\x01\x02\x0e\uffff";
    const string DFA12_minS =
        "\x01\x06\x02\x00\x0c\uffff";
    const string DFA12_maxS =
        "\x01\x4a\x02\x00\x0c\uffff";
    const string DFA12_acceptS =
        "\x03\uffff\x0a\x02\x01\x03\x01\x01";
    const string DFA12_specialS =
        "\x01\x01\x01\x00\x01\x02\x0c\uffff}>";
    static readonly string[] DFA12_transitionS = {
            "\x01\x07\x01\uffff\x01\x08\x30\uffff\x01\x0d\x01\uffff\x01"+
            "\x09\x01\x0a\x01\x0b\x01\uffff\x01\x06\x02\uffff\x01\x03\x01"+
            "\uffff\x01\x04\x01\x05\x03\uffff\x01\x01\x01\x0c",
            "\x01\uffff",
            "\x01\uffff",
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
            get { return "172:3: ( -> ^( WORD SYMBOL ) | ( expr | STRING | SLASH | BACKSLASH | RANGLE | QUEST | ARROW )=> ( operands -> ^( SYMBOL operands ) | STRING -> ^( SYMBOL STRING ) ) | EQUALS expr -> ^( EQUALS SYMBOL expr ) )"; }
        }

    }


    protected internal int DFA12_SpecialStateTransition(DFA dfa, int s, IIntStream _input) //throws NoViableAltException
    {
            ITokenStream input = (ITokenStream)_input;
    	int _s = s;
        switch ( s )
        {
               	case 0 : 
                   	int LA12_1 = input.LA(1);

                   	 
                   	int index12_1 = input.Index();
                   	input.Rewind();
                   	s = -1;
                   	if ( (true) ) { s = 14; }

                   	else if ( (((synpred1_Zap() && (!InformMode))|| (synpred1_Zap() && (InformMode)))) ) { s = 12; }

                   	 
                   	input.Seek(index12_1);
                   	if ( s >= 0 ) return s;
                   	break;
               	case 1 : 
                   	int LA12_0 = input.LA(1);

                   	 
                   	int index12_0 = input.Index();
                   	input.Rewind();
                   	s = -1;
                   	if ( (LA12_0 == CRLF) ) { s = 1; }

                   	else if ( (LA12_0 == EOF) ) { s = 2; }

                   	else if ( (LA12_0 == NUM) && (synpred1_Zap()) ) { s = 3; }

                   	else if ( (LA12_0 == SYMBOL) && (synpred1_Zap()) ) { s = 4; }

                   	else if ( (LA12_0 == OPCODE) && (synpred1_Zap()) ) { s = 5; }

                   	else if ( (LA12_0 == APOSTROPHE) && (synpred1_Zap()) ) { s = 6; }

                   	else if ( (LA12_0 == QUEST) && (synpred1_Zap()) ) { s = 7; }

                   	else if ( (LA12_0 == ARROW) && (synpred1_Zap()) ) { s = 8; }

                   	else if ( (LA12_0 == SLASH) && (synpred1_Zap()) ) { s = 9; }

                   	else if ( (LA12_0 == BACKSLASH) && (synpred1_Zap()) ) { s = 10; }

                   	else if ( (LA12_0 == RANGLE) && (synpred1_Zap()) ) { s = 11; }

                   	else if ( (LA12_0 == STRING) && (synpred1_Zap()) ) { s = 12; }

                   	else if ( (LA12_0 == EQUALS) ) { s = 13; }

                   	 
                   	input.Seek(index12_0);
                   	if ( s >= 0 ) return s;
                   	break;
               	case 2 : 
                   	int LA12_2 = input.LA(1);

                   	 
                   	int index12_2 = input.Index();
                   	input.Rewind();
                   	s = -1;
                   	if ( (true) ) { s = 14; }

                   	else if ( (((synpred1_Zap() && (!InformMode))|| (synpred1_Zap() && (InformMode)))) ) { s = 12; }

                   	 
                   	input.Seek(index12_2);
                   	if ( s >= 0 ) return s;
                   	break;
        }
        if (state.backtracking > 0) {state.failed = true; return -1;}
        NoViableAltException nvae12 =
            new NoViableAltException(dfa.Description, 12, _s, input);
        dfa.Error(nvae12);
        throw nvae12;
    }
    const string DFA14_eotS =
        "\x0b\uffff";
    const string DFA14_eofS =
        "\x01\x07\x0a\uffff";
    const string DFA14_minS =
        "\x01\x06\x03\x00\x01\x44\x01\uffff\x02\x00\x01\uffff\x02\x00";
    const string DFA14_maxS =
        "\x01\x49\x03\x00\x01\x45\x01\uffff\x02\x00\x01\uffff\x02\x00";
    const string DFA14_acceptS =
        "\x05\uffff\x01\x01\x02\uffff\x01\x02\x02\uffff";
    const string DFA14_specialS =
        "\x01\uffff\x01\x05\x01\x04\x01\x01\x02\uffff\x01\x03\x01\x02\x01"+
        "\uffff\x01\x00\x01\x06}>";
    static readonly string[] DFA14_transitionS = {
            "\x01\x05\x01\uffff\x01\x05\x32\uffff\x03\x08\x01\uffff\x01"+
            "\x04\x02\uffff\x01\x01\x01\uffff\x01\x02\x01\x03\x03\uffff\x01"+
            "\x06",
            "\x01\uffff",
            "\x01\uffff",
            "\x01\uffff",
            "\x01\x09\x01\x0a",
            "",
            "\x01\uffff",
            "\x01\uffff",
            "",
            "\x01\uffff",
            "\x01\uffff"
    };

    static readonly short[] DFA14_eot = DFA.UnpackEncodedString(DFA14_eotS);
    static readonly short[] DFA14_eof = DFA.UnpackEncodedString(DFA14_eofS);
    static readonly char[] DFA14_min = DFA.UnpackEncodedStringToUnsignedChars(DFA14_minS);
    static readonly char[] DFA14_max = DFA.UnpackEncodedStringToUnsignedChars(DFA14_maxS);
    static readonly short[] DFA14_accept = DFA.UnpackEncodedString(DFA14_acceptS);
    static readonly short[] DFA14_special = DFA.UnpackEncodedString(DFA14_specialS);
    static readonly short[][] DFA14_transition = DFA.UnpackEncodedStringArray(DFA14_transitionS);

    protected class DFA14 : DFA
    {
        public DFA14(BaseRecognizer recognizer)
        {
            this.recognizer = recognizer;
            this.decisionNumber = 14;
            this.eot = DFA14_eot;
            this.eof = DFA14_eof;
            this.min = DFA14_min;
            this.max = DFA14_max;
            this.accept = DFA14_accept;
            this.special = DFA14_special;
            this.transition = DFA14_transition;

        }

        override public string Description
        {
            get { return "186:11: ({...}? inf_operands | {...}? zap_operands )"; }
        }

    }


    protected internal int DFA14_SpecialStateTransition(DFA dfa, int s, IIntStream _input) //throws NoViableAltException
    {
            ITokenStream input = (ITokenStream)_input;
    	int _s = s;
        switch ( s )
        {
               	case 0 : 
                   	int LA14_9 = input.LA(1);

                   	 
                   	int index14_9 = input.Index();
                   	input.Rewind();
                   	s = -1;
                   	if ( ((InformMode)) ) { s = 5; }

                   	else if ( ((!InformMode)) ) { s = 8; }

                   	 
                   	input.Seek(index14_9);
                   	if ( s >= 0 ) return s;
                   	break;
               	case 1 : 
                   	int LA14_3 = input.LA(1);

                   	 
                   	int index14_3 = input.Index();
                   	input.Rewind();
                   	s = -1;
                   	if ( ((InformMode)) ) { s = 5; }

                   	else if ( ((!InformMode)) ) { s = 8; }

                   	 
                   	input.Seek(index14_3);
                   	if ( s >= 0 ) return s;
                   	break;
               	case 2 : 
                   	int LA14_7 = input.LA(1);

                   	 
                   	int index14_7 = input.Index();
                   	input.Rewind();
                   	s = -1;
                   	if ( ((InformMode)) ) { s = 5; }

                   	else if ( ((!InformMode)) ) { s = 8; }

                   	 
                   	input.Seek(index14_7);
                   	if ( s >= 0 ) return s;
                   	break;
               	case 3 : 
                   	int LA14_6 = input.LA(1);

                   	 
                   	int index14_6 = input.Index();
                   	input.Rewind();
                   	s = -1;
                   	if ( ((InformMode)) ) { s = 5; }

                   	else if ( ((!InformMode)) ) { s = 8; }

                   	 
                   	input.Seek(index14_6);
                   	if ( s >= 0 ) return s;
                   	break;
               	case 4 : 
                   	int LA14_2 = input.LA(1);

                   	 
                   	int index14_2 = input.Index();
                   	input.Rewind();
                   	s = -1;
                   	if ( ((InformMode)) ) { s = 5; }

                   	else if ( ((!InformMode)) ) { s = 8; }

                   	 
                   	input.Seek(index14_2);
                   	if ( s >= 0 ) return s;
                   	break;
               	case 5 : 
                   	int LA14_1 = input.LA(1);

                   	 
                   	int index14_1 = input.Index();
                   	input.Rewind();
                   	s = -1;
                   	if ( ((InformMode)) ) { s = 5; }

                   	else if ( ((!InformMode)) ) { s = 8; }

                   	 
                   	input.Seek(index14_1);
                   	if ( s >= 0 ) return s;
                   	break;
               	case 6 : 
                   	int LA14_10 = input.LA(1);

                   	 
                   	int index14_10 = input.Index();
                   	input.Rewind();
                   	s = -1;
                   	if ( ((InformMode)) ) { s = 5; }

                   	else if ( ((!InformMode)) ) { s = 8; }

                   	 
                   	input.Seek(index14_10);
                   	if ( s >= 0 ) return s;
                   	break;
        }
        if (state.backtracking > 0) {state.failed = true; return -1;}
        NoViableAltException nvae14 =
            new NoViableAltException(dfa.Description, 14, _s, input);
        dfa.Error(nvae14);
        throw nvae14;
    }
    const string DFA36_eotS =
        "\x3b\uffff";
    const string DFA36_eofS =
        "\x2a\uffff\x03\x32\x06\uffff\x05\x32\x01\uffff\x02\x32";
    const string DFA36_minS =
        "\x01\x3a\x01\x3f\x03\x3a\x01\x44\x02\x3f\x05\x3a\x01\x44\x03\x3a"+
        "\x01\x44\x02\x3a\x02\x3f\x05\x3a\x01\x44\x03\x3a\x01\x44\x02\x3a"+
        "\x02\x3f\x05\x3a\x01\x44\x03\x3a\x01\x44\x02\x3a\x01\x3f\x02\uffff"+
        "\x05\x3a\x01\x44\x02\x3a";
    const string DFA36_maxS =
        "\x01\x3a\x01\x45\x03\x3e\x03\x45\x05\x3e\x01\x45\x03\x3e\x01\x45"+
        "\x02\x3e\x02\x45\x05\x3e\x01\x45\x03\x3e\x01\x45\x02\x3e\x02\x45"+
        "\x05\x3e\x01\x45\x03\x49\x01\x45\x02\x3e\x01\x45\x02\uffff\x05\x49"+
        "\x01\x45\x02\x49";
    const string DFA36_acceptS =
        "\x31\uffff\x01\x01\x01\x02\x08\uffff";
    const string DFA36_specialS =
        "\x3b\uffff}>";
    static readonly string[] DFA36_transitionS = {
            "\x01\x01",
            "\x01\x05\x02\uffff\x01\x02\x01\uffff\x01\x03\x01\x04",
            "\x01\x07\x03\uffff\x01\x06",
            "\x01\x07\x03\uffff\x01\x06",
            "\x01\x07\x03\uffff\x01\x06",
            "\x01\x08\x01\x09",
            "\x01\x0d\x02\uffff\x01\x0a\x01\uffff\x01\x0b\x01\x0c",
            "\x01\x11\x02\uffff\x01\x0e\x01\uffff\x01\x0f\x01\x10",
            "\x01\x07\x03\uffff\x01\x06",
            "\x01\x07\x03\uffff\x01\x06",
            "\x01\x07\x03\uffff\x01\x06",
            "\x01\x07\x03\uffff\x01\x06",
            "\x01\x07\x03\uffff\x01\x06",
            "\x01\x12\x01\x13",
            "\x01\x15\x03\uffff\x01\x14",
            "\x01\x15\x03\uffff\x01\x14",
            "\x01\x15\x03\uffff\x01\x14",
            "\x01\x16\x01\x17",
            "\x01\x07\x03\uffff\x01\x06",
            "\x01\x07\x03\uffff\x01\x06",
            "\x01\x1b\x02\uffff\x01\x18\x01\uffff\x01\x19\x01\x1a",
            "\x01\x1f\x02\uffff\x01\x1c\x01\uffff\x01\x1d\x01\x1e",
            "\x01\x15\x03\uffff\x01\x14",
            "\x01\x15\x03\uffff\x01\x14",
            "\x01\x15\x03\uffff\x01\x14",
            "\x01\x15\x03\uffff\x01\x14",
            "\x01\x15\x03\uffff\x01\x14",
            "\x01\x20\x01\x21",
            "\x01\x23\x03\uffff\x01\x22",
            "\x01\x23\x03\uffff\x01\x22",
            "\x01\x23\x03\uffff\x01\x22",
            "\x01\x24\x01\x25",
            "\x01\x15\x03\uffff\x01\x14",
            "\x01\x15\x03\uffff\x01\x14",
            "\x01\x29\x02\uffff\x01\x26\x01\uffff\x01\x27\x01\x28",
            "\x01\x2d\x02\uffff\x01\x2a\x01\uffff\x01\x2b\x01\x2c",
            "\x01\x23\x03\uffff\x01\x22",
            "\x01\x23\x03\uffff\x01\x22",
            "\x01\x23\x03\uffff\x01\x22",
            "\x01\x23\x03\uffff\x01\x22",
            "\x01\x23\x03\uffff\x01\x22",
            "\x01\x2e\x01\x2f",
            "\x01\x31\x03\uffff\x01\x30\x0a\uffff\x01\x32",
            "\x01\x31\x03\uffff\x01\x30\x0a\uffff\x01\x32",
            "\x01\x31\x03\uffff\x01\x30\x0a\uffff\x01\x32",
            "\x01\x33\x01\x34",
            "\x01\x23\x03\uffff\x01\x22",
            "\x01\x23\x03\uffff\x01\x22",
            "\x01\x38\x02\uffff\x01\x35\x01\uffff\x01\x36\x01\x37",
            "",
            "",
            "\x01\x31\x03\uffff\x01\x30\x0a\uffff\x01\x32",
            "\x01\x31\x03\uffff\x01\x30\x0a\uffff\x01\x32",
            "\x01\x31\x03\uffff\x01\x30\x0a\uffff\x01\x32",
            "\x01\x31\x03\uffff\x01\x30\x0a\uffff\x01\x32",
            "\x01\x31\x03\uffff\x01\x30\x0a\uffff\x01\x32",
            "\x01\x39\x01\x3a",
            "\x01\x31\x03\uffff\x01\x30\x0a\uffff\x01\x32",
            "\x01\x31\x03\uffff\x01\x30\x0a\uffff\x01\x32"
    };

    static readonly short[] DFA36_eot = DFA.UnpackEncodedString(DFA36_eotS);
    static readonly short[] DFA36_eof = DFA.UnpackEncodedString(DFA36_eofS);
    static readonly char[] DFA36_min = DFA.UnpackEncodedStringToUnsignedChars(DFA36_minS);
    static readonly char[] DFA36_max = DFA.UnpackEncodedStringToUnsignedChars(DFA36_maxS);
    static readonly short[] DFA36_accept = DFA.UnpackEncodedString(DFA36_acceptS);
    static readonly short[] DFA36_special = DFA.UnpackEncodedString(DFA36_specialS);
    static readonly short[][] DFA36_transition = DFA.UnpackEncodedStringArray(DFA36_transitionS);

    protected class DFA36 : DFA
    {
        public DFA36(BaseRecognizer recognizer)
        {
            this.recognizer = recognizer;
            this.decisionNumber = 36;
            this.eot = DFA36_eot;
            this.eof = DFA36_eof;
            this.min = DFA36_min;
            this.max = DFA36_max;
            this.accept = DFA36_accept;
            this.special = DFA36_special;
            this.transition = DFA36_transition;

        }

        override public string Description
        {
            get { return "243:27: ( COMMA expr )?"; }
        }

    }

 

    public static readonly BitSet FOLLOW_CRLF_in_file843 = new BitSet(new ulong[]{0x81FFFFF9077F6400UL,0x0000000000000234UL});
    public static readonly BitSet FOLLOW_label_in_file848 = new BitSet(new ulong[]{0x0000000000000002UL,0x0000000000000200UL});
    public static readonly BitSet FOLLOW_label_in_file852 = new BitSet(new ulong[]{0x81FFFFF9077F6400UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_line_in_file855 = new BitSet(new ulong[]{0x0000000000000002UL,0x0000000000000200UL});
    public static readonly BitSet FOLLOW_CRLF_in_file859 = new BitSet(new ulong[]{0x81FFFFF9077F6400UL,0x0000000000000234UL});
    public static readonly BitSet FOLLOW_label_in_file864 = new BitSet(new ulong[]{0x0000000000000002UL,0x0000000000000200UL});
    public static readonly BitSet FOLLOW_label_in_file868 = new BitSet(new ulong[]{0x81FFFFF9077F6400UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_line_in_file871 = new BitSet(new ulong[]{0x0000000000000002UL,0x0000000000000200UL});
    public static readonly BitSet FOLLOW_CRLF_in_file876 = new BitSet(new ulong[]{0x0000000000000002UL,0x0000000000000200UL});
    public static readonly BitSet FOLLOW_SYMBOL_in_label888 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000003UL});
    public static readonly BitSet FOLLOW_DCOLON_in_label894 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_COLON_in_label907 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_SYMBOL_in_symbol927 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_OPCODE_in_symbol932 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_OPCODE_in_line949 = new BitSet(new ulong[]{0xB800000000000140UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_operands_in_line952 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_OPCODE_in_line957 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_line960 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_SYMBOL_in_line965 = new BitSet(new ulong[]{0xBA00000000000142UL,0x0000000000000434UL});
    public static readonly BitSet FOLLOW_operands_in_line1021 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_STRING_in_line1038 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_EQUALS_in_line1059 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_line1061 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_meta_directive_in_line1083 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_data_directive_in_line1088 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_funct_directive_in_line1093 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_table_directive_in_line1098 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_debug_directive_in_line1103 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_inf_operands_in_operands1116 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_zap_operands_in_operands1124 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_expr_in_inf_operands1136 = new BitSet(new ulong[]{0x8000000000000142UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_inf_spec_operand_in_inf_operands1139 = new BitSet(new ulong[]{0x0000000000000142UL});
    public static readonly BitSet FOLLOW_QUEST_in_inf_spec_operand1155 = new BitSet(new ulong[]{0x0000000000000080UL,0x0000000000000030UL});
    public static readonly BitSet FOLLOW_TILDE_in_inf_spec_operand1162 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000030UL});
    public static readonly BitSet FOLLOW_SYMBOL_in_inf_spec_operand1172 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_OPCODE_in_inf_spec_operand1186 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_ARROW_in_inf_spec_operand1252 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000030UL});
    public static readonly BitSet FOLLOW_symbol_in_inf_spec_operand1254 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_expr_in_zap_operands1277 = new BitSet(new ulong[]{0x3C00000000000002UL});
    public static readonly BitSet FOLLOW_COMMA_in_zap_operands1280 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_zap_operands1283 = new BitSet(new ulong[]{0x3C00000000000002UL});
    public static readonly BitSet FOLLOW_zap_spec_operand_in_zap_operands1289 = new BitSet(new ulong[]{0x3800000000000002UL});
    public static readonly BitSet FOLLOW_SLASH_in_zap_spec_operand1304 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000010UL});
    public static readonly BitSet FOLLOW_BACKSLASH_in_zap_spec_operand1310 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000010UL});
    public static readonly BitSet FOLLOW_SYMBOL_in_zap_spec_operand1313 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_RANGLE_in_zap_spec_operand1366 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000030UL});
    public static readonly BitSet FOLLOW_symbol_in_zap_spec_operand1369 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_term_in_expr1379 = new BitSet(new ulong[]{0x4000000000000002UL});
    public static readonly BitSet FOLLOW_PLUS_in_expr1382 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_term_in_expr1385 = new BitSet(new ulong[]{0x4000000000000002UL});
    public static readonly BitSet FOLLOW_NUM_in_term1397 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_symbol_in_term1402 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_APOSTROPHE_in_term1407 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000030UL});
    public static readonly BitSet FOLLOW_symbol_in_term1410 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_NEW_in_meta_directive1421 = new BitSet(new ulong[]{0x8000000000000002UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_meta_directive1424 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_TIME_in_meta_directive1430 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_INSERT_in_meta_directive1435 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_meta_directive1438 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_END_in_meta_directive1443 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_ENDI_in_meta_directive1448 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_set_in_data_directive1459 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_data_directive1468 = new BitSet(new ulong[]{0x0400000000000002UL});
    public static readonly BitSet FOLLOW_COMMA_in_data_directive1471 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_data_directive1474 = new BitSet(new ulong[]{0x0400000000000002UL});
    public static readonly BitSet FOLLOW_expr_in_data_directive1481 = new BitSet(new ulong[]{0x0400000000000002UL});
    public static readonly BitSet FOLLOW_COMMA_in_data_directive1484 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_data_directive1486 = new BitSet(new ulong[]{0x0400000000000002UL});
    public static readonly BitSet FOLLOW_set_in_data_directive1503 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_PROP_in_data_directive1514 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_data_directive1517 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_data_directive1519 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_data_directive1522 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_set_in_data_directive1527 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_data_directive1544 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_set_in_data_directive1549 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000010UL});
    public static readonly BitSet FOLLOW_SYMBOL_in_data_directive1558 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_data_directive1560 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_data_directive1563 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_GVAR_in_data_directive1568 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000010UL});
    public static readonly BitSet FOLLOW_SYMBOL_in_data_directive1571 = new BitSet(new ulong[]{0x0200000000000002UL});
    public static readonly BitSet FOLLOW_EQUALS_in_data_directive1574 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_data_directive1577 = new BitSet(new ulong[]{0x0400000000000002UL});
    public static readonly BitSet FOLLOW_COMMA_in_data_directive1580 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000010UL});
    public static readonly BitSet FOLLOW_SYMBOL_in_data_directive1583 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_OBJECT_in_data_directive1592 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_data_directive1595 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_data_directive1599 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_data_directive1602 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_data_directive1604 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_data_directive1607 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_data_directive1610 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_data_directive1613 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_data_directive1619 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_data_directive1622 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_data_directive1624 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_data_directive1627 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_data_directive1629 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_data_directive1632 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_data_directive1636 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_data_directive1639 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_FUNCT_in_funct_directive1650 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000010UL});
    public static readonly BitSet FOLLOW_SYMBOL_in_funct_directive1653 = new BitSet(new ulong[]{0x0400000000000002UL});
    public static readonly BitSet FOLLOW_COMMA_in_funct_directive1656 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000030UL});
    public static readonly BitSet FOLLOW_funct_param_in_funct_directive1659 = new BitSet(new ulong[]{0x0400000000000002UL});
    public static readonly BitSet FOLLOW_symbol_in_funct_param1672 = new BitSet(new ulong[]{0x0200000000000002UL});
    public static readonly BitSet FOLLOW_EQUALS_in_funct_param1675 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_funct_param1678 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_TABLE_in_table_directive1691 = new BitSet(new ulong[]{0x0000000000000002UL,0x0000000000000004UL});
    public static readonly BitSet FOLLOW_NUM_in_table_directive1694 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_ENDT_in_table_directive1700 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_VOCBEG_in_table_directive1705 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000004UL});
    public static readonly BitSet FOLLOW_NUM_in_table_directive1708 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_table_directive1710 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000004UL});
    public static readonly BitSet FOLLOW_NUM_in_table_directive1713 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_VOCEND_in_table_directive1718 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_DEBUG_ACTION_in_debug_directive1729 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_debug_directive1732 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_directive1734 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_debug_directive1737 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_DEBUG_ARRAY_in_debug_directive1742 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_debug_directive1745 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_directive1747 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_debug_directive1750 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_DEBUG_ATTR_in_debug_directive1755 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_debug_directive1758 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_directive1760 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_debug_directive1763 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_DEBUG_CLASS_in_debug_directive1768 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_debug_directive1771 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_directive1773 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_debug_lineref_in_debug_directive1776 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_directive1778 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_debug_lineref_in_debug_directive1781 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_DEBUG_FAKE_ACTION_in_debug_directive1786 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_debug_directive1789 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_directive1791 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_debug_directive1794 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_DEBUG_FILE_in_debug_directive1799 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_debug_directive1802 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_directive1804 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_debug_directive1807 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_directive1809 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_debug_directive1812 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_DEBUG_GLOBAL_in_debug_directive1817 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_debug_directive1820 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_directive1822 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_debug_directive1825 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_DEBUG_LINE_in_debug_directive1830 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_debug_lineref_in_debug_directive1833 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_DEBUG_MAP_in_debug_directive1838 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_debug_directive1841 = new BitSet(new ulong[]{0x0200000000000000UL});
    public static readonly BitSet FOLLOW_EQUALS_in_debug_directive1843 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_debug_directive1846 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_DEBUG_OBJECT_in_debug_directive1851 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_debug_directive1854 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_directive1856 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_debug_directive1859 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_directive1861 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_debug_lineref_in_debug_directive1864 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_directive1866 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_debug_lineref_in_debug_directive1869 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_DEBUG_PROP_in_debug_directive1874 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_debug_directive1877 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_directive1879 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_debug_directive1882 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_DEBUG_ROUTINE_in_debug_directive1887 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_debug_lineref_in_debug_directive1890 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_directive1892 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_debug_directive1895 = new BitSet(new ulong[]{0x0400000000000002UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_directive1898 = new BitSet(new ulong[]{0x0000000000000000UL,0x0000000000000400UL});
    public static readonly BitSet FOLLOW_STRING_in_debug_directive1901 = new BitSet(new ulong[]{0x0400000000000002UL});
    public static readonly BitSet FOLLOW_DEBUG_ROUTINE_END_in_debug_directive1909 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_debug_lineref_in_debug_directive1912 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_expr_in_debug_lineref1923 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_lineref1925 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_debug_lineref1928 = new BitSet(new ulong[]{0x0400000000000000UL});
    public static readonly BitSet FOLLOW_COMMA_in_debug_lineref1930 = new BitSet(new ulong[]{0x8000000000000000UL,0x0000000000000034UL});
    public static readonly BitSet FOLLOW_expr_in_debug_lineref1933 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_expr_in_synpred1_Zap988 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_STRING_in_synpred1_Zap992 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_SLASH_in_synpred1_Zap996 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_BACKSLASH_in_synpred1_Zap1000 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_RANGLE_in_synpred1_Zap1004 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_QUEST_in_synpred1_Zap1008 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_ARROW_in_synpred1_Zap1012 = new BitSet(new ulong[]{0x0000000000000002UL});

}
}