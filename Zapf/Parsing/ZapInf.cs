// $ANTLR 3.2 Sep 23, 2009 12:02:23 C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g 2009-11-21 23:54:17

// The variable 'variable' is assigned but its value is never used.
#pragma warning disable 168, 219
// Unreachable code detected.
#pragma warning disable 162


	using StringBuilder = System.Text.StringBuilder;


using System;
using Antlr.Runtime;
using IList 		= System.Collections.IList;
using ArrayList 	= System.Collections.ArrayList;
using Stack 		= Antlr.Runtime.Collections.StackList;

using IDictionary	= System.Collections.IDictionary;
using Hashtable 	= System.Collections.Hashtable;
namespace  Zapf.Lexing 
{
public partial class ZapInf : Lexer {
    public const int VOCBEG = 40;
    public const int PICFILE = 31;
    public const int ENDSEG = 15;
    public const int PCSET = 29;
    public const int CRLF = 73;
    public const int STRL = 36;
    public const int NEW = 25;
    public const int TABLE = 37;
    public const int LLABEL = 4;
    public const int VOCEND = 41;
    public const int EQUALS = 57;
    public const int DEFSEG = 12;
    public const int PROP = 32;
    public const int SPACE = 71;
    public const int EOF = -1;
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
    public const int PAGE = 28;
    public const int WS = 72;
    public const int FUNCT = 19;
    public const int SYMBOL_OR_NUM = 70;
    public const int GVAR = 21;
    public const int ENDT = 16;
    public const int PDEF = 30;
    public const int LEN = 24;
    public const int ARROW = 8;
    public const int END = 13;
    public const int FALSE = 17;
    public const int ENDI = 14;
    public const int ZWORD = 43;
    public const int DEBUG_GLOBAL = 50;
    public const int OPTIONS = 27;
    public const int LANG = 23;
    public const int DEBUG_PROP = 54;
    public const int STRING = 74;
    public const int BACKSLASH = 60;

    	internal IDictionary OpcodeDict;
    	
    	private bool IsOpcode(string text) {
    		return OpcodeDict.Contains(text);
    	}
    	
    	private static string UnquoteString(string text) {
    		StringBuilder sb = new StringBuilder(text, 1, text.Length - 2, text.Length - 2);
    		
    		for (int i = sb.Length - 1; i >= 0; i--)
    			if (sb[i] == '"')
    				sb.Remove(i--, 1);
    		
    		return sb.ToString();
    	}


    // delegates
    // delegators

    public ZapInf() 
    {
		InitializeCyclicDFAs();
    }
    public ZapInf(ICharStream input)
		: this(input, null) {
    }
    public ZapInf(ICharStream input, RecognizerSharedState state)
		: base(input, state) {
		InitializeCyclicDFAs(); 

    }
    
    override public string GrammarFileName
    {
    	get { return "C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g";} 
    }

    // $ANTLR start "ALIGN"
    public void mALIGN() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = ALIGN;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:38:7: ( '.ALIGN' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:38:9: '.ALIGN'
            {
            	Match(".ALIGN"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "ALIGN"

    // $ANTLR start "BYTE"
    public void mBYTE() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = BYTE;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:39:6: ( '.BYTE' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:39:8: '.BYTE'
            {
            	Match(".BYTE"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "BYTE"

    // $ANTLR start "CHRSET"
    public void mCHRSET() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = CHRSET;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:40:8: ( '.CHRSET' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:40:10: '.CHRSET'
            {
            	Match(".CHRSET"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "CHRSET"

    // $ANTLR start "DEFSEG"
    public void mDEFSEG() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DEFSEG;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:41:8: ( '.DEFSEG' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:41:10: '.DEFSEG'
            {
            	Match(".DEFSEG"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DEFSEG"

    // $ANTLR start "END"
    public void mEND() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = END;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:42:5: ( '.END' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:42:7: '.END'
            {
            	Match(".END"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "END"

    // $ANTLR start "ENDI"
    public void mENDI() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = ENDI;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:43:6: ( '.ENDI' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:43:8: '.ENDI'
            {
            	Match(".ENDI"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "ENDI"

    // $ANTLR start "ENDSEG"
    public void mENDSEG() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = ENDSEG;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:44:8: ( '.ENDSEG' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:44:10: '.ENDSEG'
            {
            	Match(".ENDSEG"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "ENDSEG"

    // $ANTLR start "ENDT"
    public void mENDT() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = ENDT;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:45:6: ( '.ENDT' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:45:8: '.ENDT'
            {
            	Match(".ENDT"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "ENDT"

    // $ANTLR start "FALSE"
    public void mFALSE() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = FALSE;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:46:7: ( '.FALSE' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:46:9: '.FALSE'
            {
            	Match(".FALSE"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "FALSE"

    // $ANTLR start "FSTR"
    public void mFSTR() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = FSTR;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:47:6: ( '.FSTR' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:47:8: '.FSTR'
            {
            	Match(".FSTR"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "FSTR"

    // $ANTLR start "FUNCT"
    public void mFUNCT() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = FUNCT;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:48:7: ( '.FUNCT' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:48:9: '.FUNCT'
            {
            	Match(".FUNCT"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "FUNCT"

    // $ANTLR start "GSTR"
    public void mGSTR() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = GSTR;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:49:6: ( '.GSTR' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:49:8: '.GSTR'
            {
            	Match(".GSTR"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "GSTR"

    // $ANTLR start "GVAR"
    public void mGVAR() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = GVAR;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:50:6: ( '.GVAR' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:50:8: '.GVAR'
            {
            	Match(".GVAR"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "GVAR"

    // $ANTLR start "INSERT"
    public void mINSERT() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = INSERT;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:51:8: ( '.INSERT' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:51:10: '.INSERT'
            {
            	Match(".INSERT"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "INSERT"

    // $ANTLR start "LANG"
    public void mLANG() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = LANG;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:52:6: ( '.LANG' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:52:8: '.LANG'
            {
            	Match(".LANG"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "LANG"

    // $ANTLR start "LEN"
    public void mLEN() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = LEN;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:53:5: ( '.LEN' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:53:7: '.LEN'
            {
            	Match(".LEN"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "LEN"

    // $ANTLR start "NEW"
    public void mNEW() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = NEW;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:54:5: ( '.NEW' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:54:7: '.NEW'
            {
            	Match(".NEW"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "NEW"

    // $ANTLR start "OBJECT"
    public void mOBJECT() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = OBJECT;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:55:8: ( '.OBJECT' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:55:10: '.OBJECT'
            {
            	Match(".OBJECT"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "OBJECT"

    // $ANTLR start "OPTIONS"
    public void mOPTIONS() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = OPTIONS;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:56:9: ( '.OPTIONS' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:56:11: '.OPTIONS'
            {
            	Match(".OPTIONS"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "OPTIONS"

    // $ANTLR start "PAGE"
    public void mPAGE() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = PAGE;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:57:6: ( '.PAGE' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:57:8: '.PAGE'
            {
            	Match(".PAGE"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "PAGE"

    // $ANTLR start "PCSET"
    public void mPCSET() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = PCSET;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:58:7: ( '.PCSET' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:58:9: '.PCSET'
            {
            	Match(".PCSET"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "PCSET"

    // $ANTLR start "PDEF"
    public void mPDEF() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = PDEF;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:59:6: ( '.PDEF' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:59:8: '.PDEF'
            {
            	Match(".PDEF"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "PDEF"

    // $ANTLR start "PICFILE"
    public void mPICFILE() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = PICFILE;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:60:9: ( '.PICFILE' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:60:11: '.PICFILE'
            {
            	Match(".PICFILE"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "PICFILE"

    // $ANTLR start "PROP"
    public void mPROP() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = PROP;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:61:6: ( '.PROP' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:61:8: '.PROP'
            {
            	Match(".PROP"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "PROP"

    // $ANTLR start "SEGMENT"
    public void mSEGMENT() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = SEGMENT;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:62:9: ( '.SEGMENT' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:62:11: '.SEGMENT'
            {
            	Match(".SEGMENT"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "SEGMENT"

    // $ANTLR start "SEQ"
    public void mSEQ() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = SEQ;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:63:5: ( '.SEQ' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:63:7: '.SEQ'
            {
            	Match(".SEQ"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "SEQ"

    // $ANTLR start "STR"
    public void mSTR() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = STR;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:64:5: ( '.STR' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:64:7: '.STR'
            {
            	Match(".STR"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "STR"

    // $ANTLR start "STRL"
    public void mSTRL() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = STRL;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:65:6: ( '.STRL' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:65:8: '.STRL'
            {
            	Match(".STRL"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "STRL"

    // $ANTLR start "TABLE"
    public void mTABLE() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = TABLE;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:66:7: ( '.TABLE' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:66:9: '.TABLE'
            {
            	Match(".TABLE"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "TABLE"

    // $ANTLR start "TIME"
    public void mTIME() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = TIME;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:67:6: ( '.TIME' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:67:8: '.TIME'
            {
            	Match(".TIME"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "TIME"

    // $ANTLR start "TRUE"
    public void mTRUE() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = TRUE;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:68:6: ( '.TRUE' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:68:8: '.TRUE'
            {
            	Match(".TRUE"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "TRUE"

    // $ANTLR start "VOCBEG"
    public void mVOCBEG() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = VOCBEG;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:69:8: ( '.VOCBEG' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:69:10: '.VOCBEG'
            {
            	Match(".VOCBEG"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "VOCBEG"

    // $ANTLR start "VOCEND"
    public void mVOCEND() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = VOCEND;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:70:8: ( '.VOCEND' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:70:10: '.VOCEND'
            {
            	Match(".VOCEND"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "VOCEND"

    // $ANTLR start "WORD"
    public void mWORD() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = WORD;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:71:6: ( '.WORD' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:71:8: '.WORD'
            {
            	Match(".WORD"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "WORD"

    // $ANTLR start "ZWORD"
    public void mZWORD() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = ZWORD;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:72:7: ( '.ZWORD' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:72:9: '.ZWORD'
            {
            	Match(".ZWORD"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "ZWORD"

    // $ANTLR start "DEBUG_ACTION"
    public void mDEBUG_ACTION() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DEBUG_ACTION;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:75:2: ( '.DEBUG-ACTION' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:75:4: '.DEBUG-ACTION'
            {
            	Match(".DEBUG-ACTION"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DEBUG_ACTION"

    // $ANTLR start "DEBUG_ARRAY"
    public void mDEBUG_ARRAY() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DEBUG_ARRAY;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:77:2: ( '.DEBUG-ARRAY' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:77:4: '.DEBUG-ARRAY'
            {
            	Match(".DEBUG-ARRAY"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DEBUG_ARRAY"

    // $ANTLR start "DEBUG_ATTR"
    public void mDEBUG_ATTR() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DEBUG_ATTR;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:79:2: ( '.DEBUG-ATTR' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:79:4: '.DEBUG-ATTR'
            {
            	Match(".DEBUG-ATTR"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DEBUG_ATTR"

    // $ANTLR start "DEBUG_CLASS"
    public void mDEBUG_CLASS() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DEBUG_CLASS;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:81:2: ( '.DEBUG-CLASS' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:81:4: '.DEBUG-CLASS'
            {
            	Match(".DEBUG-CLASS"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DEBUG_CLASS"

    // $ANTLR start "DEBUG_FAKE_ACTION"
    public void mDEBUG_FAKE_ACTION() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DEBUG_FAKE_ACTION;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:83:2: ( '.DEBUG-FAKE-ACTION' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:83:4: '.DEBUG-FAKE-ACTION'
            {
            	Match(".DEBUG-FAKE-ACTION"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DEBUG_FAKE_ACTION"

    // $ANTLR start "DEBUG_FILE"
    public void mDEBUG_FILE() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DEBUG_FILE;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:85:2: ( '.DEBUG-FILE' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:85:4: '.DEBUG-FILE'
            {
            	Match(".DEBUG-FILE"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DEBUG_FILE"

    // $ANTLR start "DEBUG_GLOBAL"
    public void mDEBUG_GLOBAL() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DEBUG_GLOBAL;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:87:2: ( '.DEBUG-GLOBAL' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:87:4: '.DEBUG-GLOBAL'
            {
            	Match(".DEBUG-GLOBAL"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DEBUG_GLOBAL"

    // $ANTLR start "DEBUG_LINE"
    public void mDEBUG_LINE() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DEBUG_LINE;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:89:2: ( '.DEBUG-LINE' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:89:4: '.DEBUG-LINE'
            {
            	Match(".DEBUG-LINE"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DEBUG_LINE"

    // $ANTLR start "DEBUG_MAP"
    public void mDEBUG_MAP() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DEBUG_MAP;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:91:2: ( '.DEBUG-MAP' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:91:4: '.DEBUG-MAP'
            {
            	Match(".DEBUG-MAP"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DEBUG_MAP"

    // $ANTLR start "DEBUG_OBJECT"
    public void mDEBUG_OBJECT() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DEBUG_OBJECT;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:93:2: ( '.DEBUG-OBJECT' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:93:4: '.DEBUG-OBJECT'
            {
            	Match(".DEBUG-OBJECT"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DEBUG_OBJECT"

    // $ANTLR start "DEBUG_PROP"
    public void mDEBUG_PROP() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DEBUG_PROP;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:95:2: ( '.DEBUG-PROP' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:95:4: '.DEBUG-PROP'
            {
            	Match(".DEBUG-PROP"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DEBUG_PROP"

    // $ANTLR start "DEBUG_ROUTINE"
    public void mDEBUG_ROUTINE() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DEBUG_ROUTINE;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:97:2: ( '.DEBUG-ROUTINE' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:97:4: '.DEBUG-ROUTINE'
            {
            	Match(".DEBUG-ROUTINE"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DEBUG_ROUTINE"

    // $ANTLR start "DEBUG_ROUTINE_END"
    public void mDEBUG_ROUTINE_END() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DEBUG_ROUTINE_END;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:99:2: ( '.DEBUG-ROUTINE-END' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:99:4: '.DEBUG-ROUTINE-END'
            {
            	Match(".DEBUG-ROUTINE-END"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DEBUG_ROUTINE_END"

    // $ANTLR start "EQUALS"
    public void mEQUALS() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = EQUALS;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:101:8: ( '=' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:101:10: '='
            {
            	Match('='); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "EQUALS"

    // $ANTLR start "COMMA"
    public void mCOMMA() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = COMMA;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:102:7: ( ',' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:102:9: ','
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

    // $ANTLR start "PLUS"
    public void mPLUS() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = PLUS;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:103:6: ( '+' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:103:8: '+'
            {
            	Match('+'); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "PLUS"

    // $ANTLR start "APOSTROPHE"
    public void mAPOSTROPHE() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = APOSTROPHE;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:105:2: ( '\\'' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:105:4: '\\''
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
    // $ANTLR end "APOSTROPHE"

    // $ANTLR start "COLON"
    public void mCOLON() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = COLON;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:106:7: ( ':' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:106:9: ':'
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

    // $ANTLR start "DCOLON"
    public void mDCOLON() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = DCOLON;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:107:8: ( '::' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:107:10: '::'
            {
            	Match("::"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "DCOLON"

    // $ANTLR start "ARROW"
    public void mARROW() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = ARROW;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:109:7: ( '->' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:109:9: '->'
            {
            	Match("->"); if (state.failed) return ;


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "ARROW"

    // $ANTLR start "QUEST"
    public void mQUEST() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = QUEST;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:110:7: ( '?' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:110:9: '?'
            {
            	Match('?'); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "QUEST"

    // $ANTLR start "TILDE"
    public void mTILDE() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = TILDE;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:111:7: ( '~' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:111:9: '~'
            {
            	Match('~'); if (state.failed) return ;

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "TILDE"

    // $ANTLR start "NUM"
    public void mNUM() // throws RecognitionException [2]
    {
    		try
    		{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:114:2: ( ( '-' )? ( '0' .. '9' )+ )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:114:4: ( '-' )? ( '0' .. '9' )+
            {
            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:114:4: ( '-' )?
            	int alt1 = 2;
            	int LA1_0 = input.LA(1);

            	if ( (LA1_0 == '-') )
            	{
            	    alt1 = 1;
            	}
            	switch (alt1) 
            	{
            	    case 1 :
            	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:114:4: '-'
            	        {
            	        	Match('-'); if (state.failed) return ;

            	        }
            	        break;

            	}

            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:114:9: ( '0' .. '9' )+
            	int cnt2 = 0;
            	do 
            	{
            	    int alt2 = 2;
            	    int LA2_0 = input.LA(1);

            	    if ( ((LA2_0 >= '0' && LA2_0 <= '9')) )
            	    {
            	        alt2 = 1;
            	    }


            	    switch (alt2) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:114:9: '0' .. '9'
            			    {
            			    	MatchRange('0','9'); if (state.failed) return ;

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


            }

        }
        finally 
    	{
        }
    }
    // $ANTLR end "NUM"

    // $ANTLR start "NSYMBOL"
    public void mNSYMBOL() // throws RecognitionException [2]
    {
    		try
    		{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:118:2: (~ ( 'A' .. 'Z' | 'a' .. 'z' | '0' .. '9' | '-' | '_' | '$' | '#' | '&' | '.' ) )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:118:4: ~ ( 'A' .. 'Z' | 'a' .. 'z' | '0' .. '9' | '-' | '_' | '$' | '#' | '&' | '.' )
            {
            	if ( (input.LA(1) >= '\u0000' && input.LA(1) <= '\"') || input.LA(1) == '%' || (input.LA(1) >= '\'' && input.LA(1) <= ',') || input.LA(1) == '/' || (input.LA(1) >= ':' && input.LA(1) <= '@') || (input.LA(1) >= '[' && input.LA(1) <= '^') || input.LA(1) == '`' || (input.LA(1) >= '{' && input.LA(1) <= '\uFFFF') ) 
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
    // $ANTLR end "NSYMBOL"

    // $ANTLR start "SYMBOL"
    public void mSYMBOL() // throws RecognitionException [2]
    {
    		try
    		{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:122:2: ( ( 'A' .. 'Z' | 'a' .. 'z' | '0' .. '9' | '-' | '_' | '$' | '#' | '&' | '.' )+ )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:122:4: ( 'A' .. 'Z' | 'a' .. 'z' | '0' .. '9' | '-' | '_' | '$' | '#' | '&' | '.' )+
            {
            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:122:4: ( 'A' .. 'Z' | 'a' .. 'z' | '0' .. '9' | '-' | '_' | '$' | '#' | '&' | '.' )+
            	int cnt3 = 0;
            	do 
            	{
            	    int alt3 = 2;
            	    int LA3_0 = input.LA(1);

            	    if ( ((LA3_0 >= '#' && LA3_0 <= '$') || LA3_0 == '&' || (LA3_0 >= '-' && LA3_0 <= '.') || (LA3_0 >= '0' && LA3_0 <= '9') || (LA3_0 >= 'A' && LA3_0 <= 'Z') || LA3_0 == '_' || (LA3_0 >= 'a' && LA3_0 <= 'z')) )
            	    {
            	        alt3 = 1;
            	    }


            	    switch (alt3) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:
            			    {
            			    	if ( (input.LA(1) >= '#' && input.LA(1) <= '$') || input.LA(1) == '&' || (input.LA(1) >= '-' && input.LA(1) <= '.') || (input.LA(1) >= '0' && input.LA(1) <= '9') || (input.LA(1) >= 'A' && input.LA(1) <= 'Z') || input.LA(1) == '_' || (input.LA(1) >= 'a' && input.LA(1) <= 'z') ) 
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

            			default:
            			    if ( cnt3 >= 1 ) goto loop3;
            			    if ( state.backtracking > 0 ) {state.failed = true; return ;}
            		            EarlyExitException eee3 =
            		                new EarlyExitException(3, input);
            		            throw eee3;
            	    }
            	    cnt3++;
            	} while (true);

            	loop3:
            		;	// Stops C# compiler whining that label 'loop3' has no statements


            }

        }
        finally 
    	{
        }
    }
    // $ANTLR end "SYMBOL"

    // $ANTLR start "OPCODE"
    public void mOPCODE() // throws RecognitionException [2]
    {
    		try
    		{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:126:2: ( 'OPCODE' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:126:4: 'OPCODE'
            {
            	Match("OPCODE"); if (state.failed) return ;


            }

        }
        finally 
    	{
        }
    }
    // $ANTLR end "OPCODE"

    // $ANTLR start "SYMBOL_OR_NUM"
    public void mSYMBOL_OR_NUM() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = SYMBOL_OR_NUM;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:129:2: ( ( NUM NSYMBOL )=> NUM | SYMBOL )
            int alt4 = 2;
            switch ( input.LA(1) ) 
            {
            case '-':
            	{
                int LA4_1 = input.LA(2);

                if ( ((LA4_1 >= '0' && LA4_1 <= '9')) )
                {
                    int LA4_2 = input.LA(3);

                    if ( (synpred1_ZapInf()) )
                    {
                        alt4 = 1;
                    }
                    else if ( (true) )
                    {
                        alt4 = 2;
                    }
                    else 
                    {
                        if ( state.backtracking > 0 ) {state.failed = true; return ;}
                        NoViableAltException nvae_d4s2 =
                            new NoViableAltException("", 4, 2, input);

                        throw nvae_d4s2;
                    }
                }
                else 
                {
                    alt4 = 2;}
                }
                break;
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
                int LA4_2 = input.LA(2);

                if ( (synpred1_ZapInf()) )
                {
                    alt4 = 1;
                }
                else if ( (true) )
                {
                    alt4 = 2;
                }
                else 
                {
                    if ( state.backtracking > 0 ) {state.failed = true; return ;}
                    NoViableAltException nvae_d4s2 =
                        new NoViableAltException("", 4, 2, input);

                    throw nvae_d4s2;
                }
                }
                break;
            case '#':
            case '$':
            case '&':
            case '.':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case 'G':
            case 'H':
            case 'I':
            case 'J':
            case 'K':
            case 'L':
            case 'M':
            case 'N':
            case 'O':
            case 'P':
            case 'Q':
            case 'R':
            case 'S':
            case 'T':
            case 'U':
            case 'V':
            case 'W':
            case 'X':
            case 'Y':
            case 'Z':
            case '_':
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f':
            case 'g':
            case 'h':
            case 'i':
            case 'j':
            case 'k':
            case 'l':
            case 'm':
            case 'n':
            case 'o':
            case 'p':
            case 'q':
            case 'r':
            case 's':
            case 't':
            case 'u':
            case 'v':
            case 'w':
            case 'x':
            case 'y':
            case 'z':
            	{
                alt4 = 2;
                }
                break;
            	default:
            	    if ( state.backtracking > 0 ) {state.failed = true; return ;}
            	    NoViableAltException nvae_d4s0 =
            	        new NoViableAltException("", 4, 0, input);

            	    throw nvae_d4s0;
            }

            switch (alt4) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:129:4: ( NUM NSYMBOL )=> NUM
                    {
                    	mNUM(); if (state.failed) return ;
                    	if ( (state.backtracking==0) )
                    	{
                    	   _type = NUM; 
                    	}

                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:130:4: SYMBOL
                    {
                    	mSYMBOL(); if (state.failed) return ;
                    	if ( (state.backtracking==0) )
                    	{
                    	   _type = IsOpcode(Text) ? OPCODE : SYMBOL; 
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
    // $ANTLR end "SYMBOL_OR_NUM"

    // $ANTLR start "SPACE"
    public void mSPACE() // throws RecognitionException [2]
    {
    		try
    		{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:134:2: ( ' ' | '\\t' | '\\f' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:
            {
            	if ( input.LA(1) == '\t' || input.LA(1) == '\f' || input.LA(1) == ' ' ) 
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
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:137:4: ( ( SPACE )+ )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:137:6: ( SPACE )+
            {
            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:137:6: ( SPACE )+
            	int cnt5 = 0;
            	do 
            	{
            	    int alt5 = 2;
            	    int LA5_0 = input.LA(1);

            	    if ( (LA5_0 == '\t' || LA5_0 == '\f' || LA5_0 == ' ') )
            	    {
            	        alt5 = 1;
            	    }


            	    switch (alt5) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:137:6: SPACE
            			    {
            			    	mSPACE(); if (state.failed) return ;

            			    }
            			    break;

            			default:
            			    if ( cnt5 >= 1 ) goto loop5;
            			    if ( state.backtracking > 0 ) {state.failed = true; return ;}
            		            EarlyExitException eee5 =
            		                new EarlyExitException(5, input);
            		            throw eee5;
            	    }
            	    cnt5++;
            	} while (true);

            	loop5:
            		;	// Stops C# compiler whining that label 'loop5' has no statements

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

    // $ANTLR start "CRLF"
    public void mCRLF() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = CRLF;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:141:2: ( ( '\\r' | '\\n' )+ )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:141:4: ( '\\r' | '\\n' )+
            {
            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:141:4: ( '\\r' | '\\n' )+
            	int cnt6 = 0;
            	do 
            	{
            	    int alt6 = 2;
            	    int LA6_0 = input.LA(1);

            	    if ( (LA6_0 == '\n' || LA6_0 == '\r') )
            	    {
            	        alt6 = 1;
            	    }


            	    switch (alt6) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:
            			    {
            			    	if ( input.LA(1) == '\n' || input.LA(1) == '\r' ) 
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

            			default:
            			    if ( cnt6 >= 1 ) goto loop6;
            			    if ( state.backtracking > 0 ) {state.failed = true; return ;}
            		            EarlyExitException eee6 =
            		                new EarlyExitException(6, input);
            		            throw eee6;
            	    }
            	    cnt6++;
            	} while (true);

            	loop6:
            		;	// Stops C# compiler whining that label 'loop6' has no statements


            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "CRLF"

    // $ANTLR start "STRING"
    public void mSTRING() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = STRING;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:144:8: ( '\"' (~ '\"' | '\"\"' )* '\"' )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:144:10: '\"' (~ '\"' | '\"\"' )* '\"'
            {
            	Match('\"'); if (state.failed) return ;
            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:145:3: (~ '\"' | '\"\"' )*
            	do 
            	{
            	    int alt7 = 3;
            	    int LA7_0 = input.LA(1);

            	    if ( (LA7_0 == '\"') )
            	    {
            	        int LA7_1 = input.LA(2);

            	        if ( (LA7_1 == '\"') )
            	        {
            	            alt7 = 2;
            	        }


            	    }
            	    else if ( ((LA7_0 >= '\u0000' && LA7_0 <= '!') || (LA7_0 >= '#' && LA7_0 <= '\uFFFF')) )
            	    {
            	        alt7 = 1;
            	    }


            	    switch (alt7) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:145:4: ~ '\"'
            			    {
            			    	if ( (input.LA(1) >= '\u0000' && input.LA(1) <= '!') || (input.LA(1) >= '#' && input.LA(1) <= '\uFFFF') ) 
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
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:145:11: '\"\"'
            			    {
            			    	Match("\"\""); if (state.failed) return ;


            			    }
            			    break;

            			default:
            			    goto loop7;
            	    }
            	} while (true);

            	loop7:
            		;	// Stops C# compiler whining that label 'loop7' has no statements

            	Match('\"'); if (state.failed) return ;
            	if ( (state.backtracking==0) )
            	{
            	   Text = UnquoteString(Text); 
            	}

            }

            state.type = _type;
            state.channel = _channel;
        }
        finally 
    	{
        }
    }
    // $ANTLR end "STRING"

    // $ANTLR start "COMMENT"
    public void mCOMMENT() // throws RecognitionException [2]
    {
    		try
    		{
            int _type = COMMENT;
    	int _channel = DEFAULT_TOKEN_CHANNEL;
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:149:9: ( ';' (~ ( '\\r' | '\\n' ) )* )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:149:11: ';' (~ ( '\\r' | '\\n' ) )*
            {
            	Match(';'); if (state.failed) return ;
            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:149:15: (~ ( '\\r' | '\\n' ) )*
            	do 
            	{
            	    int alt8 = 2;
            	    int LA8_0 = input.LA(1);

            	    if ( ((LA8_0 >= '\u0000' && LA8_0 <= '\t') || (LA8_0 >= '\u000B' && LA8_0 <= '\f') || (LA8_0 >= '\u000E' && LA8_0 <= '\uFFFF')) )
            	    {
            	        alt8 = 1;
            	    }


            	    switch (alt8) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:149:15: ~ ( '\\r' | '\\n' )
            			    {
            			    	if ( (input.LA(1) >= '\u0000' && input.LA(1) <= '\t') || (input.LA(1) >= '\u000B' && input.LA(1) <= '\f') || (input.LA(1) >= '\u000E' && input.LA(1) <= '\uFFFF') ) 
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

            			default:
            			    goto loop8;
            	    }
            	} while (true);

            	loop8:
            		;	// Stops C# compiler whining that label 'loop8' has no statements

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
    // $ANTLR end "COMMENT"

    override public void mTokens() // throws RecognitionException 
    {
        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:8: ( ALIGN | BYTE | CHRSET | DEFSEG | END | ENDI | ENDSEG | ENDT | FALSE | FSTR | FUNCT | GSTR | GVAR | INSERT | LANG | LEN | NEW | OBJECT | OPTIONS | PAGE | PCSET | PDEF | PICFILE | PROP | SEGMENT | SEQ | STR | STRL | TABLE | TIME | TRUE | VOCBEG | VOCEND | WORD | ZWORD | DEBUG_ACTION | DEBUG_ARRAY | DEBUG_ATTR | DEBUG_CLASS | DEBUG_FAKE_ACTION | DEBUG_FILE | DEBUG_GLOBAL | DEBUG_LINE | DEBUG_MAP | DEBUG_OBJECT | DEBUG_PROP | DEBUG_ROUTINE | DEBUG_ROUTINE_END | EQUALS | COMMA | PLUS | APOSTROPHE | COLON | DCOLON | ARROW | QUEST | TILDE | SYMBOL_OR_NUM | WS | CRLF | STRING | COMMENT )
        int alt9 = 62;
        alt9 = dfa9.Predict(input);
        switch (alt9) 
        {
            case 1 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:10: ALIGN
                {
                	mALIGN(); if (state.failed) return ;

                }
                break;
            case 2 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:16: BYTE
                {
                	mBYTE(); if (state.failed) return ;

                }
                break;
            case 3 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:21: CHRSET
                {
                	mCHRSET(); if (state.failed) return ;

                }
                break;
            case 4 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:28: DEFSEG
                {
                	mDEFSEG(); if (state.failed) return ;

                }
                break;
            case 5 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:35: END
                {
                	mEND(); if (state.failed) return ;

                }
                break;
            case 6 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:39: ENDI
                {
                	mENDI(); if (state.failed) return ;

                }
                break;
            case 7 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:44: ENDSEG
                {
                	mENDSEG(); if (state.failed) return ;

                }
                break;
            case 8 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:51: ENDT
                {
                	mENDT(); if (state.failed) return ;

                }
                break;
            case 9 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:56: FALSE
                {
                	mFALSE(); if (state.failed) return ;

                }
                break;
            case 10 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:62: FSTR
                {
                	mFSTR(); if (state.failed) return ;

                }
                break;
            case 11 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:67: FUNCT
                {
                	mFUNCT(); if (state.failed) return ;

                }
                break;
            case 12 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:73: GSTR
                {
                	mGSTR(); if (state.failed) return ;

                }
                break;
            case 13 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:78: GVAR
                {
                	mGVAR(); if (state.failed) return ;

                }
                break;
            case 14 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:83: INSERT
                {
                	mINSERT(); if (state.failed) return ;

                }
                break;
            case 15 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:90: LANG
                {
                	mLANG(); if (state.failed) return ;

                }
                break;
            case 16 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:95: LEN
                {
                	mLEN(); if (state.failed) return ;

                }
                break;
            case 17 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:99: NEW
                {
                	mNEW(); if (state.failed) return ;

                }
                break;
            case 18 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:103: OBJECT
                {
                	mOBJECT(); if (state.failed) return ;

                }
                break;
            case 19 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:110: OPTIONS
                {
                	mOPTIONS(); if (state.failed) return ;

                }
                break;
            case 20 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:118: PAGE
                {
                	mPAGE(); if (state.failed) return ;

                }
                break;
            case 21 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:123: PCSET
                {
                	mPCSET(); if (state.failed) return ;

                }
                break;
            case 22 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:129: PDEF
                {
                	mPDEF(); if (state.failed) return ;

                }
                break;
            case 23 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:134: PICFILE
                {
                	mPICFILE(); if (state.failed) return ;

                }
                break;
            case 24 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:142: PROP
                {
                	mPROP(); if (state.failed) return ;

                }
                break;
            case 25 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:147: SEGMENT
                {
                	mSEGMENT(); if (state.failed) return ;

                }
                break;
            case 26 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:155: SEQ
                {
                	mSEQ(); if (state.failed) return ;

                }
                break;
            case 27 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:159: STR
                {
                	mSTR(); if (state.failed) return ;

                }
                break;
            case 28 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:163: STRL
                {
                	mSTRL(); if (state.failed) return ;

                }
                break;
            case 29 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:168: TABLE
                {
                	mTABLE(); if (state.failed) return ;

                }
                break;
            case 30 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:174: TIME
                {
                	mTIME(); if (state.failed) return ;

                }
                break;
            case 31 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:179: TRUE
                {
                	mTRUE(); if (state.failed) return ;

                }
                break;
            case 32 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:184: VOCBEG
                {
                	mVOCBEG(); if (state.failed) return ;

                }
                break;
            case 33 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:191: VOCEND
                {
                	mVOCEND(); if (state.failed) return ;

                }
                break;
            case 34 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:198: WORD
                {
                	mWORD(); if (state.failed) return ;

                }
                break;
            case 35 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:203: ZWORD
                {
                	mZWORD(); if (state.failed) return ;

                }
                break;
            case 36 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:209: DEBUG_ACTION
                {
                	mDEBUG_ACTION(); if (state.failed) return ;

                }
                break;
            case 37 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:222: DEBUG_ARRAY
                {
                	mDEBUG_ARRAY(); if (state.failed) return ;

                }
                break;
            case 38 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:234: DEBUG_ATTR
                {
                	mDEBUG_ATTR(); if (state.failed) return ;

                }
                break;
            case 39 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:245: DEBUG_CLASS
                {
                	mDEBUG_CLASS(); if (state.failed) return ;

                }
                break;
            case 40 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:257: DEBUG_FAKE_ACTION
                {
                	mDEBUG_FAKE_ACTION(); if (state.failed) return ;

                }
                break;
            case 41 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:275: DEBUG_FILE
                {
                	mDEBUG_FILE(); if (state.failed) return ;

                }
                break;
            case 42 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:286: DEBUG_GLOBAL
                {
                	mDEBUG_GLOBAL(); if (state.failed) return ;

                }
                break;
            case 43 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:299: DEBUG_LINE
                {
                	mDEBUG_LINE(); if (state.failed) return ;

                }
                break;
            case 44 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:310: DEBUG_MAP
                {
                	mDEBUG_MAP(); if (state.failed) return ;

                }
                break;
            case 45 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:320: DEBUG_OBJECT
                {
                	mDEBUG_OBJECT(); if (state.failed) return ;

                }
                break;
            case 46 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:333: DEBUG_PROP
                {
                	mDEBUG_PROP(); if (state.failed) return ;

                }
                break;
            case 47 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:344: DEBUG_ROUTINE
                {
                	mDEBUG_ROUTINE(); if (state.failed) return ;

                }
                break;
            case 48 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:358: DEBUG_ROUTINE_END
                {
                	mDEBUG_ROUTINE_END(); if (state.failed) return ;

                }
                break;
            case 49 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:376: EQUALS
                {
                	mEQUALS(); if (state.failed) return ;

                }
                break;
            case 50 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:383: COMMA
                {
                	mCOMMA(); if (state.failed) return ;

                }
                break;
            case 51 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:389: PLUS
                {
                	mPLUS(); if (state.failed) return ;

                }
                break;
            case 52 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:394: APOSTROPHE
                {
                	mAPOSTROPHE(); if (state.failed) return ;

                }
                break;
            case 53 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:405: COLON
                {
                	mCOLON(); if (state.failed) return ;

                }
                break;
            case 54 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:411: DCOLON
                {
                	mDCOLON(); if (state.failed) return ;

                }
                break;
            case 55 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:418: ARROW
                {
                	mARROW(); if (state.failed) return ;

                }
                break;
            case 56 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:424: QUEST
                {
                	mQUEST(); if (state.failed) return ;

                }
                break;
            case 57 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:430: TILDE
                {
                	mTILDE(); if (state.failed) return ;

                }
                break;
            case 58 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:436: SYMBOL_OR_NUM
                {
                	mSYMBOL_OR_NUM(); if (state.failed) return ;

                }
                break;
            case 59 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:450: WS
                {
                	mWS(); if (state.failed) return ;

                }
                break;
            case 60 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:453: CRLF
                {
                	mCRLF(); if (state.failed) return ;

                }
                break;
            case 61 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:458: STRING
                {
                	mSTRING(); if (state.failed) return ;

                }
                break;
            case 62 :
                // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:1:465: COMMENT
                {
                	mCOMMENT(); if (state.failed) return ;

                }
                break;

        }

    }

    // $ANTLR start "synpred1_ZapInf"
    public void synpred1_ZapInf_fragment() {
        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:129:4: ( NUM NSYMBOL )
        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zapf\\ZapInf.g:129:5: NUM NSYMBOL
        {
        	mNUM(); if (state.failed) return ;
        	mNSYMBOL(); if (state.failed) return ;

        }
    }
    // $ANTLR end "synpred1_ZapInf"

   	public bool synpred1_ZapInf() 
   	{
   	    state.backtracking++;
   	    int start = input.Mark();
   	    try 
   	    {
   	        synpred1_ZapInf_fragment(); // can never throw exception
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


    protected DFA9 dfa9;
	private void InitializeCyclicDFAs()
	{
	    this.dfa9 = new DFA9(this);
	}

    const string DFA9_eotS =
        "\x01\uffff\x01\x0a\x04\uffff\x01\x21\x01\x0a\x07\uffff\x11\x0a"+
        "\x03\uffff\x22\x0a\x01\x67\x07\x0a\x01\x6f\x01\x70\x08\x0a\x01\x79"+
        "\x01\x7b\x07\x0a\x01\u0084\x03\x0a\x01\u0088\x01\x0a\x01\u008a\x01"+
        "\uffff\x01\x0a\x01\u008c\x01\x0a\x01\u008e\x01\u008f\x01\x0a\x01"+
        "\u0091\x02\uffff\x02\x0a\x01\u0094\x01\x0a\x01\u0096\x01\x0a\x01"+
        "\u0098\x01\x0a\x01\uffff\x01\u009a\x01\uffff\x01\x0a\x01\u009c\x01"+
        "\u009d\x02\x0a\x01\u00a0\x01\x0a\x01\u00a2\x01\uffff\x03\x0a\x01"+
        "\uffff\x01\x0a\x01\uffff\x01\u00a7\x01\uffff\x01\u00a8\x02\uffff"+
        "\x01\x0a\x01\uffff\x02\x0a\x01\uffff\x01\u00ac\x01\uffff\x01\x0a"+
        "\x01\uffff\x01\x0a\x01\uffff\x01\u00af\x02\uffff\x02\x0a\x01\uffff"+
        "\x01\u00b2\x01\uffff\x01\u00b3\x01\u00b4\x01\x0a\x01\u00be\x02\uffff"+
        "\x01\u00bf\x01\u00c0\x01\x0a\x01\uffff\x02\x0a\x01\uffff\x01\u00c4"+
        "\x01\u00c5\x03\uffff\x09\x0a\x03\uffff\x01\u00d2\x01\u00d3\x01\u00d4"+
        "\x02\uffff\x0c\x0a\x03\uffff\x08\x0a\x01\u00e9\x05\x0a\x01\u00ef"+
        "\x02\x0a\x01\u00f2\x01\x0a\x01\u00f4\x01\uffff\x01\x0a\x01\u00f6"+
        "\x02\x0a\x01\u00f9\x01\uffff\x01\u00fa\x01\x0a\x01\uffff\x01\x0a"+
        "\x01\uffff\x01\x0a\x01\uffff\x01\x0a\x01\u00ff\x02\uffff\x01\x0a"+
        "\x01\u0101\x01\u0102\x01\x0a\x01\uffff\x01\x0a\x02\uffff\x01\u0106"+
        "\x02\x0a\x01\uffff\x04\x0a\x01\u010d\x01\u010e\x02\uffff";
    const string DFA9_eofS =
        "\u010f\uffff";
    const string DFA9_minS =
        "\x01\x09\x01\x41\x04\uffff\x01\x3a\x01\x3e\x07\uffff\x01\x4c\x01"+
        "\x59\x01\x48\x01\x45\x01\x4e\x01\x41\x01\x53\x01\x4e\x01\x41\x01"+
        "\x45\x01\x42\x01\x41\x01\x45\x01\x41\x02\x4f\x01\x57\x03\uffff\x01"+
        "\x49\x01\x54\x01\x52\x01\x42\x01\x44\x01\x4c\x01\x54\x01\x4e\x01"+
        "\x54\x01\x41\x01\x53\x02\x4e\x01\x57\x01\x4a\x01\x54\x01\x47\x01"+
        "\x53\x01\x45\x01\x43\x01\x4f\x01\x47\x01\x52\x01\x42\x01\x4d\x01"+
        "\x55\x01\x43\x01\x52\x01\x4f\x01\x47\x01\x45\x02\x53\x01\x55\x01"+
        "\x23\x01\x53\x01\x52\x01\x43\x02\x52\x01\x45\x01\x47\x02\x23\x01"+
        "\x45\x01\x49\x02\x45\x02\x46\x01\x50\x01\x4d\x02\x23\x01\x4c\x02"+
        "\x45\x01\x42\x01\x44\x01\x52\x01\x4e\x01\x23\x02\x45\x01\x47\x01"+
        "\x23\x01\x45\x01\x23\x01\uffff\x01\x45\x01\x23\x01\x54\x02\x23\x01"+
        "\x52\x01\x23\x02\uffff\x01\x43\x01\x4f\x01\x23\x01\x54\x01\x23\x01"+
        "\x49\x01\x23\x01\x45\x01\uffff\x01\x23\x01\uffff\x01\x45\x02\x23"+
        "\x01\x45\x01\x4e\x01\x23\x01\x44\x01\x23\x01\uffff\x01\x54\x01\x47"+
        "\x01\x2d\x01\uffff\x01\x47\x01\uffff\x01\x23\x01\uffff\x01\x23\x02"+
        "\uffff\x01\x54\x01\uffff\x01\x54\x01\x4e\x01\uffff\x01\x23\x01\uffff"+
        "\x01\x4c\x01\uffff\x01\x4e\x01\uffff\x01\x23\x02\uffff\x01\x47\x01"+
        "\x44\x01\uffff\x01\x23\x01\uffff\x02\x23\x01\x41\x01\x23\x02\uffff"+
        "\x02\x23\x01\x53\x01\uffff\x01\x45\x01\x54\x01\uffff\x02\x23\x03"+
        "\uffff\x01\x43\x01\x4c\x01\x41\x01\x4c\x01\x49\x01\x41\x01\x42\x01"+
        "\x52\x01\x4f\x03\uffff\x03\x23\x02\uffff\x01\x54\x01\x52\x01\x54"+
        "\x01\x41\x01\x4b\x01\x4c\x01\x4f\x01\x4e\x01\x50\x01\x4a\x01\x4f"+
        "\x01\x55\x03\uffff\x01\x49\x01\x41\x01\x52\x01\x53\x02\x45\x01\x42"+
        "\x01\x45\x01\x23\x01\x45\x01\x50\x01\x54\x01\x4f\x01\x59\x01\x23"+
        "\x01\x53\x01\x2d\x01\x23\x01\x41\x01\x23\x01\uffff\x01\x43\x01\x23"+
        "\x01\x49\x01\x4e\x01\x23\x01\uffff\x01\x23\x01\x41\x01\uffff\x01"+
        "\x4c\x01\uffff\x01\x54\x01\uffff\x01\x4e\x01\x23\x02\uffff\x01\x43"+
        "\x02\x23\x01\x45\x01\uffff\x01\x54\x02\uffff\x01\x23\x01\x49\x01"+
        "\x45\x01\uffff\x01\x4f\x02\x4e\x01\x44\x02\x23\x02\uffff";
    const string DFA9_maxS =
        "\x01\x7e\x01\x5a\x04\uffff\x01\x3a\x01\x3e\x07\uffff\x01\x4c\x01"+
        "\x59\x01\x48\x01\x45\x01\x4e\x01\x55\x01\x56\x01\x4e\x02\x45\x01"+
        "\x50\x01\x52\x01\x54\x01\x52\x02\x4f\x01\x57\x03\uffff\x01\x49\x01"+
        "\x54\x01\x52\x01\x46\x01\x44\x01\x4c\x01\x54\x01\x4e\x01\x54\x01"+
        "\x41\x01\x53\x02\x4e\x01\x57\x01\x4a\x01\x54\x01\x47\x01\x53\x01"+
        "\x45\x01\x43\x01\x4f\x01\x51\x01\x52\x01\x42\x01\x4d\x01\x55\x01"+
        "\x43\x01\x52\x01\x4f\x01\x47\x01\x45\x02\x53\x01\x55\x01\x7a\x01"+
        "\x53\x01\x52\x01\x43\x02\x52\x01\x45\x01\x47\x02\x7a\x01\x45\x01"+
        "\x49\x02\x45\x02\x46\x01\x50\x01\x4d\x02\x7a\x01\x4c\x03\x45\x01"+
        "\x44\x01\x52\x01\x4e\x01\x7a\x02\x45\x01\x47\x01\x7a\x01\x45\x01"+
        "\x7a\x01\uffff\x01\x45\x01\x7a\x01\x54\x02\x7a\x01\x52\x01\x7a\x02"+
        "\uffff\x01\x43\x01\x4f\x01\x7a\x01\x54\x01\x7a\x01\x49\x01\x7a\x01"+
        "\x45\x01\uffff\x01\x7a\x01\uffff\x01\x45\x02\x7a\x01\x45\x01\x4e"+
        "\x01\x7a\x01\x44\x01\x7a\x01\uffff\x01\x54\x01\x47\x01\x2d\x01\uffff"+
        "\x01\x47\x01\uffff\x01\x7a\x01\uffff\x01\x7a\x02\uffff\x01\x54\x01"+
        "\uffff\x01\x54\x01\x4e\x01\uffff\x01\x7a\x01\uffff\x01\x4c\x01\uffff"+
        "\x01\x4e\x01\uffff\x01\x7a\x02\uffff\x01\x47\x01\x44\x01\uffff\x01"+
        "\x7a\x01\uffff\x02\x7a\x01\x52\x01\x7a\x02\uffff\x02\x7a\x01\x53"+
        "\x01\uffff\x01\x45\x01\x54\x01\uffff\x02\x7a\x03\uffff\x01\x54\x01"+
        "\x4c\x01\x49\x01\x4c\x01\x49\x01\x41\x01\x42\x01\x52\x01\x4f\x03"+
        "\uffff\x03\x7a\x02\uffff\x01\x54\x01\x52\x01\x54\x01\x41\x01\x4b"+
        "\x01\x4c\x01\x4f\x01\x4e\x01\x50\x01\x4a\x01\x4f\x01\x55\x03\uffff"+
        "\x01\x49\x01\x41\x01\x52\x01\x53\x02\x45\x01\x42\x01\x45\x01\x7a"+
        "\x01\x45\x01\x50\x01\x54\x01\x4f\x01\x59\x01\x7a\x01\x53\x01\x2d"+
        "\x01\x7a\x01\x41\x01\x7a\x01\uffff\x01\x43\x01\x7a\x01\x49\x01\x4e"+
        "\x01\x7a\x01\uffff\x01\x7a\x01\x41\x01\uffff\x01\x4c\x01\uffff\x01"+
        "\x54\x01\uffff\x01\x4e\x01\x7a\x02\uffff\x01\x43\x02\x7a\x01\x45"+
        "\x01\uffff\x01\x54\x02\uffff\x01\x7a\x01\x49\x01\x45\x01\uffff\x01"+
        "\x4f\x02\x4e\x01\x44\x02\x7a\x02\uffff";
    const string DFA9_acceptS =
        "\x02\uffff\x01\x31\x01\x32\x01\x33\x01\x34\x02\uffff\x01\x38\x01"+
        "\x39\x01\x3a\x01\x3b\x01\x3c\x01\x3d\x01\x3e\x11\uffff\x01\x36\x01"+
        "\x35\x01\x37\x44\uffff\x01\x05\x07\uffff\x01\x10\x01\x11\x08\uffff"+
        "\x01\x1a\x01\uffff\x01\x1b\x08\uffff\x01\x02\x03\uffff\x01\x06\x01"+
        "\uffff\x01\x08\x01\uffff\x01\x0a\x01\uffff\x01\x0c\x01\x0d\x01\uffff"+
        "\x01\x0f\x02\uffff\x01\x14\x01\uffff\x01\x16\x01\uffff\x01\x18\x01"+
        "\uffff\x01\x1c\x01\uffff\x01\x1e\x01\x1f\x02\uffff\x01\x22\x01\uffff"+
        "\x01\x01\x04\uffff\x01\x09\x01\x0b\x03\uffff\x01\x15\x02\uffff\x01"+
        "\x1d\x02\uffff\x01\x23\x01\x03\x01\x04\x09\uffff\x01\x07\x01\x0e"+
        "\x01\x12\x03\uffff\x01\x20\x01\x21\x0c\uffff\x01\x13\x01\x17\x01"+
        "\x19\x14\uffff\x01\x2c\x05\uffff\x01\x26\x02\uffff\x01\x29\x01\uffff"+
        "\x01\x2b\x01\uffff\x01\x2e\x02\uffff\x01\x25\x01\x27\x04\uffff\x01"+
        "\x24\x01\uffff\x01\x2a\x01\x2d\x03\uffff\x01\x2f\x06\uffff\x01\x28"+
        "\x01\x30";
    const string DFA9_specialS =
        "\u010f\uffff}>";
    static readonly string[] DFA9_transitionS = {
            "\x01\x0b\x01\x0c\x01\uffff\x01\x0b\x01\x0c\x12\uffff\x01\x0b"+
            "\x01\uffff\x01\x0d\x02\x0a\x01\uffff\x01\x0a\x01\x05\x03\uffff"+
            "\x01\x04\x01\x03\x01\x07\x01\x01\x01\uffff\x0a\x0a\x01\x06\x01"+
            "\x0e\x01\uffff\x01\x02\x01\uffff\x01\x08\x01\uffff\x1a\x0a\x04"+
            "\uffff\x01\x0a\x01\uffff\x1a\x0a\x03\uffff\x01\x09",
            "\x01\x0f\x01\x10\x01\x11\x01\x12\x01\x13\x01\x14\x01\x15\x01"+
            "\uffff\x01\x16\x02\uffff\x01\x17\x01\uffff\x01\x18\x01\x19\x01"+
            "\x1a\x02\uffff\x01\x1b\x01\x1c\x01\uffff\x01\x1d\x01\x1e\x02"+
            "\uffff\x01\x1f",
            "",
            "",
            "",
            "",
            "\x01\x20",
            "\x01\x22",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "\x01\x23",
            "\x01\x24",
            "\x01\x25",
            "\x01\x26",
            "\x01\x27",
            "\x01\x28\x11\uffff\x01\x29\x01\uffff\x01\x2a",
            "\x01\x2b\x02\uffff\x01\x2c",
            "\x01\x2d",
            "\x01\x2e\x03\uffff\x01\x2f",
            "\x01\x30",
            "\x01\x31\x0d\uffff\x01\x32",
            "\x01\x33\x01\uffff\x01\x34\x01\x35\x04\uffff\x01\x36\x08\uffff"+
            "\x01\x37",
            "\x01\x38\x0e\uffff\x01\x39",
            "\x01\x3a\x07\uffff\x01\x3b\x08\uffff\x01\x3c",
            "\x01\x3d",
            "\x01\x3e",
            "\x01\x3f",
            "",
            "",
            "",
            "\x01\x40",
            "\x01\x41",
            "\x01\x42",
            "\x01\x44\x03\uffff\x01\x43",
            "\x01\x45",
            "\x01\x46",
            "\x01\x47",
            "\x01\x48",
            "\x01\x49",
            "\x01\x4a",
            "\x01\x4b",
            "\x01\x4c",
            "\x01\x4d",
            "\x01\x4e",
            "\x01\x4f",
            "\x01\x50",
            "\x01\x51",
            "\x01\x52",
            "\x01\x53",
            "\x01\x54",
            "\x01\x55",
            "\x01\x56\x09\uffff\x01\x57",
            "\x01\x58",
            "\x01\x59",
            "\x01\x5a",
            "\x01\x5b",
            "\x01\x5c",
            "\x01\x5d",
            "\x01\x5e",
            "\x01\x5f",
            "\x01\x60",
            "\x01\x61",
            "\x01\x62",
            "\x01\x63",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x08\x0a\x01\x64\x09\x0a\x01\x65\x01\x66\x06\x0a"+
            "\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\x68",
            "\x01\x69",
            "\x01\x6a",
            "\x01\x6b",
            "\x01\x6c",
            "\x01\x6d",
            "\x01\x6e",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\x71",
            "\x01\x72",
            "\x01\x73",
            "\x01\x74",
            "\x01\x75",
            "\x01\x76",
            "\x01\x77",
            "\x01\x78",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x0b\x0a\x01\x7a\x0e\x0a\x04\uffff\x01\x0a\x01"+
            "\uffff\x1a\x0a",
            "\x01\x7c",
            "\x01\x7d",
            "\x01\x7e",
            "\x01\x7f\x02\uffff\x01\u0080",
            "\x01\u0081",
            "\x01\u0082",
            "\x01\u0083",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u0085",
            "\x01\u0086",
            "\x01\u0087",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u0089",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            "\x01\u008b",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u008d",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u0090",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            "",
            "\x01\u0092",
            "\x01\u0093",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u0095",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u0097",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u0099",
            "",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            "\x01\u009b",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u009e",
            "\x01\u009f",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u00a1",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            "\x01\u00a3",
            "\x01\u00a4",
            "\x01\u00a5",
            "",
            "\x01\u00a6",
            "",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            "",
            "\x01\u00a9",
            "",
            "\x01\u00aa",
            "\x01\u00ab",
            "",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            "\x01\u00ad",
            "",
            "\x01\u00ae",
            "",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            "",
            "\x01\u00b0",
            "\x01\u00b1",
            "",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u00b5\x01\uffff\x01\u00b6\x02\uffff\x01\u00b7\x01\u00b8"+
            "\x04\uffff\x01\u00b9\x01\u00ba\x01\uffff\x01\u00bb\x01\u00bc"+
            "\x01\uffff\x01\u00bd",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            "",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u00c1",
            "",
            "\x01\u00c2",
            "\x01\u00c3",
            "",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            "",
            "",
            "\x01\u00c6\x0e\uffff\x01\u00c7\x01\uffff\x01\u00c8",
            "\x01\u00c9",
            "\x01\u00ca\x07\uffff\x01\u00cb",
            "\x01\u00cc",
            "\x01\u00cd",
            "\x01\u00ce",
            "\x01\u00cf",
            "\x01\u00d0",
            "\x01\u00d1",
            "",
            "",
            "",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            "",
            "\x01\u00d5",
            "\x01\u00d6",
            "\x01\u00d7",
            "\x01\u00d8",
            "\x01\u00d9",
            "\x01\u00da",
            "\x01\u00db",
            "\x01\u00dc",
            "\x01\u00dd",
            "\x01\u00de",
            "\x01\u00df",
            "\x01\u00e0",
            "",
            "",
            "",
            "\x01\u00e1",
            "\x01\u00e2",
            "\x01\u00e3",
            "\x01\u00e4",
            "\x01\u00e5",
            "\x01\u00e6",
            "\x01\u00e7",
            "\x01\u00e8",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u00ea",
            "\x01\u00eb",
            "\x01\u00ec",
            "\x01\u00ed",
            "\x01\u00ee",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u00f0",
            "\x01\u00f1",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u00f3",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            "\x01\u00f5",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u00f7",
            "\x01\u00f8",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u00fb",
            "",
            "\x01\u00fc",
            "",
            "\x01\u00fd",
            "",
            "\x01\u00fe",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            "",
            "\x01\u0100",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x01\u0103",
            "",
            "\x01\u0104",
            "",
            "",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x01\u0105\x01\x0a\x01"+
            "\uffff\x0a\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff"+
            "\x1a\x0a",
            "\x01\u0107",
            "\x01\u0108",
            "",
            "\x01\u0109",
            "\x01\u010a",
            "\x01\u010b",
            "\x01\u010c",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "\x02\x0a\x01\uffff\x01\x0a\x06\uffff\x02\x0a\x01\uffff\x0a"+
            "\x0a\x07\uffff\x1a\x0a\x04\uffff\x01\x0a\x01\uffff\x1a\x0a",
            "",
            ""
    };

    static readonly short[] DFA9_eot = DFA.UnpackEncodedString(DFA9_eotS);
    static readonly short[] DFA9_eof = DFA.UnpackEncodedString(DFA9_eofS);
    static readonly char[] DFA9_min = DFA.UnpackEncodedStringToUnsignedChars(DFA9_minS);
    static readonly char[] DFA9_max = DFA.UnpackEncodedStringToUnsignedChars(DFA9_maxS);
    static readonly short[] DFA9_accept = DFA.UnpackEncodedString(DFA9_acceptS);
    static readonly short[] DFA9_special = DFA.UnpackEncodedString(DFA9_specialS);
    static readonly short[][] DFA9_transition = DFA.UnpackEncodedStringArray(DFA9_transitionS);

    protected class DFA9 : DFA
    {
        public DFA9(BaseRecognizer recognizer)
        {
            this.recognizer = recognizer;
            this.decisionNumber = 9;
            this.eot = DFA9_eot;
            this.eof = DFA9_eof;
            this.min = DFA9_min;
            this.max = DFA9_max;
            this.accept = DFA9_accept;
            this.special = DFA9_special;
            this.transition = DFA9_transition;

        }

        override public string Description
        {
            get { return "1:1: Tokens : ( ALIGN | BYTE | CHRSET | DEFSEG | END | ENDI | ENDSEG | ENDT | FALSE | FSTR | FUNCT | GSTR | GVAR | INSERT | LANG | LEN | NEW | OBJECT | OPTIONS | PAGE | PCSET | PDEF | PICFILE | PROP | SEGMENT | SEQ | STR | STRL | TABLE | TIME | TRUE | VOCBEG | VOCEND | WORD | ZWORD | DEBUG_ACTION | DEBUG_ARRAY | DEBUG_ATTR | DEBUG_CLASS | DEBUG_FAKE_ACTION | DEBUG_FILE | DEBUG_GLOBAL | DEBUG_LINE | DEBUG_MAP | DEBUG_OBJECT | DEBUG_PROP | DEBUG_ROUTINE | DEBUG_ROUTINE_END | EQUALS | COMMA | PLUS | APOSTROPHE | COLON | DCOLON | ARROW | QUEST | TILDE | SYMBOL_OR_NUM | WS | CRLF | STRING | COMMENT );"; }
        }

    }

 
    
}
}