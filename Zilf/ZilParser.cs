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

using Antlr.Runtime.Tree;

namespace  Zilf.Parsing 
{
public partial class ZilParser : Parser
{
    public static readonly string[] tokenNames = new string[] 
	{
        "<invalid>", 
		"<EOR>", 
		"<DOWN>", 
		"<UP>", 
		"COMMENT", 
		"FORM", 
		"LIST", 
		"VECTOR", 
		"UVECTOR", 
		"SEGMENT", 
		"MACRO", 
		"VMACRO", 
		"HASH", 
		"GVAL", 
		"LVAL", 
		"ADECL", 
		"DIGIT", 
		"STRING", 
		"CHAR", 
		"SPACE", 
		"WS", 
		"NATOM", 
		"ATOM_HEAD", 
		"ATOM_TAIL", 
		"ATOM", 
		"NUM", 
		"ATOM_OR_NUM", 
		"SEMI", 
		"COLON", 
		"PERCENT", 
		"LANGLE", 
		"RANGLE", 
		"LPAREN", 
		"RPAREN", 
		"LSQUARE", 
		"RSQUARE", 
		"BANG", 
		"DOT", 
		"COMMA", 
		"APOS"
    };

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
    public const int LIST = 6;
    public const int EOF = -1;
    public const int SPACE = 19;
    public const int SEMI = 27;
    public const int LPAREN = 32;
    public const int NUM = 25;
    public const int COLON = 28;
    public const int ATOM_OR_NUM = 26;
    public const int RPAREN = 33;
    public const int WS = 20;
    public const int VECTOR = 7;
    public const int COMMA = 38;
    public const int LVAL = 14;
    public const int ATOM_HEAD = 22;
    public const int FORM = 5;
    public const int ADECL = 15;
    public const int DIGIT = 16;
    public const int RANGLE = 31;
    public const int DOT = 37;
    public const int COMMENT = 4;
    public const int NATOM = 21;
    public const int MACRO = 10;
    public const int SEGMENT = 9;
    public const int STRING = 17;

    // delegates
    // delegators



        public ZilParser(ITokenStream input)
    		: this(input, new RecognizerSharedState()) {
        }

        public ZilParser(ITokenStream input, RecognizerSharedState state)
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
		get { return ZilParser.tokenNames; }
    }

    override public string GrammarFileName {
		get { return "C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g"; }
    }


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
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:77:1: file : ( comment | noncomment_expr )* ;
    public ZilParser.file_return file() // throws RecognitionException [1]
    {   
        ZilParser.file_return retval = new ZilParser.file_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        ZilParser.comment_return comment1 = default(ZilParser.comment_return);

        ZilParser.noncomment_expr_return noncomment_expr2 = default(ZilParser.noncomment_expr_return);



        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:77:6: ( ( comment | noncomment_expr )* )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:77:8: ( comment | noncomment_expr )*
            {
            	root_0 = (object)adaptor.GetNilNode();

            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:77:8: ( comment | noncomment_expr )*
            	do 
            	{
            	    int alt1 = 3;
            	    int LA1_0 = input.LA(1);

            	    if ( (LA1_0 == SEMI) )
            	    {
            	        alt1 = 1;
            	    }
            	    else if ( (LA1_0 == HASH || (LA1_0 >= STRING && LA1_0 <= CHAR) || (LA1_0 >= ATOM && LA1_0 <= NUM) || (LA1_0 >= PERCENT && LA1_0 <= LANGLE) || LA1_0 == LPAREN || LA1_0 == LSQUARE || (LA1_0 >= BANG && LA1_0 <= APOS)) )
            	    {
            	        alt1 = 2;
            	    }


            	    switch (alt1) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:77:9: comment
            			    {
            			    	PushFollow(FOLLOW_comment_in_file535);
            			    	comment1 = comment();
            			    	state.followingStackPointer--;
            			    	if (state.failed) return retval;
            			    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, comment1.Tree);

            			    }
            			    break;
            			case 2 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:77:19: noncomment_expr
            			    {
            			    	PushFollow(FOLLOW_noncomment_expr_in_file539);
            			    	noncomment_expr2 = noncomment_expr();
            			    	state.followingStackPointer--;
            			    	if (state.failed) return retval;
            			    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, noncomment_expr2.Tree);

            			    }
            			    break;

            			default:
            			    goto loop1;
            	    }
            	} while (true);

            	loop1:
            		;	// Stops C# compiler whining that label 'loop1' has no statements


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
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:80:1: expr : ( comment )* noncomment_expr ;
    public ZilParser.expr_return expr() // throws RecognitionException [1]
    {   
        ZilParser.expr_return retval = new ZilParser.expr_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        ZilParser.comment_return comment3 = default(ZilParser.comment_return);

        ZilParser.noncomment_expr_return noncomment_expr4 = default(ZilParser.noncomment_expr_return);



        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:80:6: ( ( comment )* noncomment_expr )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:80:8: ( comment )* noncomment_expr
            {
            	root_0 = (object)adaptor.GetNilNode();

            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:80:8: ( comment )*
            	do 
            	{
            	    int alt2 = 2;
            	    int LA2_0 = input.LA(1);

            	    if ( (LA2_0 == SEMI) )
            	    {
            	        alt2 = 1;
            	    }


            	    switch (alt2) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:80:8: comment
            			    {
            			    	PushFollow(FOLLOW_comment_in_expr551);
            			    	comment3 = comment();
            			    	state.followingStackPointer--;
            			    	if (state.failed) return retval;
            			    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, comment3.Tree);

            			    }
            			    break;

            			default:
            			    goto loop2;
            	    }
            	} while (true);

            	loop2:
            		;	// Stops C# compiler whining that label 'loop2' has no statements

            	PushFollow(FOLLOW_noncomment_expr_in_expr554);
            	noncomment_expr4 = noncomment_expr();
            	state.followingStackPointer--;
            	if (state.failed) return retval;
            	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, noncomment_expr4.Tree);

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

    public class comment_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "comment"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:83:1: comment : SEMI noncomment_expr -> ^( COMMENT noncomment_expr ) ;
    public ZilParser.comment_return comment() // throws RecognitionException [1]
    {   
        ZilParser.comment_return retval = new ZilParser.comment_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken SEMI5 = null;
        ZilParser.noncomment_expr_return noncomment_expr6 = default(ZilParser.noncomment_expr_return);


        object SEMI5_tree=null;
        RewriteRuleTokenStream stream_SEMI = new RewriteRuleTokenStream(adaptor,"token SEMI");
        RewriteRuleSubtreeStream stream_noncomment_expr = new RewriteRuleSubtreeStream(adaptor,"rule noncomment_expr");
        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:83:9: ( SEMI noncomment_expr -> ^( COMMENT noncomment_expr ) )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:83:11: SEMI noncomment_expr
            {
            	SEMI5=(IToken)Match(input,SEMI,FOLLOW_SEMI_in_comment564); if (state.failed) return retval; 
            	if ( (state.backtracking==0) ) stream_SEMI.Add(SEMI5);

            	PushFollow(FOLLOW_noncomment_expr_in_comment566);
            	noncomment_expr6 = noncomment_expr();
            	state.followingStackPointer--;
            	if (state.failed) return retval;
            	if ( (state.backtracking==0) ) stream_noncomment_expr.Add(noncomment_expr6.Tree);


            	// AST REWRITE
            	// elements:          noncomment_expr
            	// token labels:      
            	// rule labels:       retval
            	// token list labels: 
            	// rule list labels:  
            	// wildcard labels: 
            	if ( (state.backtracking==0) ) {
            	retval.Tree = root_0;
            	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

            	root_0 = (object)adaptor.GetNilNode();
            	// 83:32: -> ^( COMMENT noncomment_expr )
            	{
            	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:83:35: ^( COMMENT noncomment_expr )
            	    {
            	    object root_1 = (object)adaptor.GetNilNode();
            	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(COMMENT, "COMMENT"), root_1);

            	    adaptor.AddChild(root_1, stream_noncomment_expr.NextTree());

            	    adaptor.AddChild(root_0, root_1);
            	    }

            	}

            	retval.Tree = root_0;retval.Tree = root_0;}
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
    // $ANTLR end "comment"

    public class noncomment_expr_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "noncomment_expr"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:86:1: noncomment_expr : ( ATOM | ATOM COLON ATOM -> ^( ADECL ATOM ATOM ) | NUM | CHAR | STRING | form | segment | vector | list | macro | HASH ATOM expr -> ^( HASH ATOM expr ) );
    public ZilParser.noncomment_expr_return noncomment_expr() // throws RecognitionException [1]
    {   
        ZilParser.noncomment_expr_return retval = new ZilParser.noncomment_expr_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken ATOM7 = null;
        IToken ATOM8 = null;
        IToken COLON9 = null;
        IToken ATOM10 = null;
        IToken NUM11 = null;
        IToken CHAR12 = null;
        IToken STRING13 = null;
        IToken HASH19 = null;
        IToken ATOM20 = null;
        ZilParser.form_return form14 = default(ZilParser.form_return);

        ZilParser.segment_return segment15 = default(ZilParser.segment_return);

        ZilParser.vector_return vector16 = default(ZilParser.vector_return);

        ZilParser.list_return list17 = default(ZilParser.list_return);

        ZilParser.macro_return macro18 = default(ZilParser.macro_return);

        ZilParser.expr_return expr21 = default(ZilParser.expr_return);


        object ATOM7_tree=null;
        object ATOM8_tree=null;
        object COLON9_tree=null;
        object ATOM10_tree=null;
        object NUM11_tree=null;
        object CHAR12_tree=null;
        object STRING13_tree=null;
        object HASH19_tree=null;
        object ATOM20_tree=null;
        RewriteRuleTokenStream stream_HASH = new RewriteRuleTokenStream(adaptor,"token HASH");
        RewriteRuleTokenStream stream_COLON = new RewriteRuleTokenStream(adaptor,"token COLON");
        RewriteRuleTokenStream stream_ATOM = new RewriteRuleTokenStream(adaptor,"token ATOM");
        RewriteRuleSubtreeStream stream_expr = new RewriteRuleSubtreeStream(adaptor,"rule expr");
        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:87:2: ( ATOM | ATOM COLON ATOM -> ^( ADECL ATOM ATOM ) | NUM | CHAR | STRING | form | segment | vector | list | macro | HASH ATOM expr -> ^( HASH ATOM expr ) )
            int alt3 = 11;
            alt3 = dfa3.Predict(input);
            switch (alt3) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:87:4: ATOM
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	ATOM7=(IToken)Match(input,ATOM,FOLLOW_ATOM_in_noncomment_expr585); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{ATOM7_tree = (object)adaptor.Create(ATOM7);
                    		adaptor.AddChild(root_0, ATOM7_tree);
                    	}

                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:88:4: ATOM COLON ATOM
                    {
                    	ATOM8=(IToken)Match(input,ATOM,FOLLOW_ATOM_in_noncomment_expr590); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_ATOM.Add(ATOM8);

                    	COLON9=(IToken)Match(input,COLON,FOLLOW_COLON_in_noncomment_expr592); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_COLON.Add(COLON9);

                    	ATOM10=(IToken)Match(input,ATOM,FOLLOW_ATOM_in_noncomment_expr594); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_ATOM.Add(ATOM10);



                    	// AST REWRITE
                    	// elements:          ATOM, ATOM
                    	// token labels:      
                    	// rule labels:       retval
                    	// token list labels: 
                    	// rule list labels:  
                    	// wildcard labels: 
                    	if ( (state.backtracking==0) ) {
                    	retval.Tree = root_0;
                    	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	root_0 = (object)adaptor.GetNilNode();
                    	// 88:21: -> ^( ADECL ATOM ATOM )
                    	{
                    	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:88:24: ^( ADECL ATOM ATOM )
                    	    {
                    	    object root_1 = (object)adaptor.GetNilNode();
                    	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(ADECL, "ADECL"), root_1);

                    	    adaptor.AddChild(root_1, stream_ATOM.NextNode());
                    	    adaptor.AddChild(root_1, stream_ATOM.NextNode());

                    	    adaptor.AddChild(root_0, root_1);
                    	    }

                    	}

                    	retval.Tree = root_0;retval.Tree = root_0;}
                    }
                    break;
                case 3 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:89:4: NUM
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	NUM11=(IToken)Match(input,NUM,FOLLOW_NUM_in_noncomment_expr610); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{NUM11_tree = (object)adaptor.Create(NUM11);
                    		adaptor.AddChild(root_0, NUM11_tree);
                    	}

                    }
                    break;
                case 4 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:90:4: CHAR
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	CHAR12=(IToken)Match(input,CHAR,FOLLOW_CHAR_in_noncomment_expr615); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{CHAR12_tree = (object)adaptor.Create(CHAR12);
                    		adaptor.AddChild(root_0, CHAR12_tree);
                    	}

                    }
                    break;
                case 5 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:91:4: STRING
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	STRING13=(IToken)Match(input,STRING,FOLLOW_STRING_in_noncomment_expr620); if (state.failed) return retval;
                    	if ( state.backtracking == 0 )
                    	{STRING13_tree = (object)adaptor.Create(STRING13);
                    		adaptor.AddChild(root_0, STRING13_tree);
                    	}

                    }
                    break;
                case 6 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:92:4: form
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	PushFollow(FOLLOW_form_in_noncomment_expr625);
                    	form14 = form();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, form14.Tree);

                    }
                    break;
                case 7 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:93:4: segment
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	PushFollow(FOLLOW_segment_in_noncomment_expr630);
                    	segment15 = segment();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, segment15.Tree);

                    }
                    break;
                case 8 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:94:4: vector
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	PushFollow(FOLLOW_vector_in_noncomment_expr635);
                    	vector16 = vector();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, vector16.Tree);

                    }
                    break;
                case 9 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:95:4: list
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	PushFollow(FOLLOW_list_in_noncomment_expr640);
                    	list17 = list();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, list17.Tree);

                    }
                    break;
                case 10 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:96:4: macro
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	PushFollow(FOLLOW_macro_in_noncomment_expr645);
                    	macro18 = macro();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, macro18.Tree);

                    }
                    break;
                case 11 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:97:4: HASH ATOM expr
                    {
                    	HASH19=(IToken)Match(input,HASH,FOLLOW_HASH_in_noncomment_expr650); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_HASH.Add(HASH19);

                    	ATOM20=(IToken)Match(input,ATOM,FOLLOW_ATOM_in_noncomment_expr652); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_ATOM.Add(ATOM20);

                    	PushFollow(FOLLOW_expr_in_noncomment_expr654);
                    	expr21 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( (state.backtracking==0) ) stream_expr.Add(expr21.Tree);


                    	// AST REWRITE
                    	// elements:          expr, ATOM, HASH
                    	// token labels:      
                    	// rule labels:       retval
                    	// token list labels: 
                    	// rule list labels:  
                    	// wildcard labels: 
                    	if ( (state.backtracking==0) ) {
                    	retval.Tree = root_0;
                    	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	root_0 = (object)adaptor.GetNilNode();
                    	// 97:20: -> ^( HASH ATOM expr )
                    	{
                    	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:97:23: ^( HASH ATOM expr )
                    	    {
                    	    object root_1 = (object)adaptor.GetNilNode();
                    	    root_1 = (object)adaptor.BecomeRoot(stream_HASH.NextNode(), root_1);

                    	    adaptor.AddChild(root_1, stream_ATOM.NextNode());
                    	    adaptor.AddChild(root_1, stream_expr.NextTree());

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
        }
        return retval;
    }
    // $ANTLR end "noncomment_expr"

    public class macro_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "macro"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:100:1: macro : PERCENT ( ( PERCENT )=> PERCENT expr -> ^( VMACRO expr ) | expr -> ^( MACRO expr ) ) ;
    public ZilParser.macro_return macro() // throws RecognitionException [1]
    {   
        ZilParser.macro_return retval = new ZilParser.macro_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken PERCENT22 = null;
        IToken PERCENT23 = null;
        ZilParser.expr_return expr24 = default(ZilParser.expr_return);

        ZilParser.expr_return expr25 = default(ZilParser.expr_return);


        object PERCENT22_tree=null;
        object PERCENT23_tree=null;
        RewriteRuleTokenStream stream_PERCENT = new RewriteRuleTokenStream(adaptor,"token PERCENT");
        RewriteRuleSubtreeStream stream_expr = new RewriteRuleSubtreeStream(adaptor,"rule expr");
        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:101:2: ( PERCENT ( ( PERCENT )=> PERCENT expr -> ^( VMACRO expr ) | expr -> ^( MACRO expr ) ) )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:101:4: PERCENT ( ( PERCENT )=> PERCENT expr -> ^( VMACRO expr ) | expr -> ^( MACRO expr ) )
            {
            	PERCENT22=(IToken)Match(input,PERCENT,FOLLOW_PERCENT_in_macro676); if (state.failed) return retval; 
            	if ( (state.backtracking==0) ) stream_PERCENT.Add(PERCENT22);

            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:102:3: ( ( PERCENT )=> PERCENT expr -> ^( VMACRO expr ) | expr -> ^( MACRO expr ) )
            	int alt4 = 2;
            	alt4 = dfa4.Predict(input);
            	switch (alt4) 
            	{
            	    case 1 :
            	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:102:5: ( PERCENT )=> PERCENT expr
            	        {
            	        	PERCENT23=(IToken)Match(input,PERCENT,FOLLOW_PERCENT_in_macro687); if (state.failed) return retval; 
            	        	if ( (state.backtracking==0) ) stream_PERCENT.Add(PERCENT23);

            	        	PushFollow(FOLLOW_expr_in_macro689);
            	        	expr24 = expr();
            	        	state.followingStackPointer--;
            	        	if (state.failed) return retval;
            	        	if ( (state.backtracking==0) ) stream_expr.Add(expr24.Tree);


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
            	        	// 102:30: -> ^( VMACRO expr )
            	        	{
            	        	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:102:33: ^( VMACRO expr )
            	        	    {
            	        	    object root_1 = (object)adaptor.GetNilNode();
            	        	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(VMACRO, "VMACRO"), root_1);

            	        	    adaptor.AddChild(root_1, stream_expr.NextTree());

            	        	    adaptor.AddChild(root_0, root_1);
            	        	    }

            	        	}

            	        	retval.Tree = root_0;retval.Tree = root_0;}
            	        }
            	        break;
            	    case 2 :
            	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:103:5: expr
            	        {
            	        	PushFollow(FOLLOW_expr_in_macro703);
            	        	expr25 = expr();
            	        	state.followingStackPointer--;
            	        	if (state.failed) return retval;
            	        	if ( (state.backtracking==0) ) stream_expr.Add(expr25.Tree);


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
            	        	// 103:13: -> ^( MACRO expr )
            	        	{
            	        	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:103:16: ^( MACRO expr )
            	        	    {
            	        	    object root_1 = (object)adaptor.GetNilNode();
            	        	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(MACRO, "MACRO"), root_1);

            	        	    adaptor.AddChild(root_1, stream_expr.NextTree());

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
    // $ANTLR end "macro"

    public class comment_or_expr_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "comment_or_expr"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:106:1: comment_or_expr : ( comment | noncomment_expr );
    public ZilParser.comment_or_expr_return comment_or_expr() // throws RecognitionException [1]
    {   
        ZilParser.comment_or_expr_return retval = new ZilParser.comment_or_expr_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        ZilParser.comment_return comment26 = default(ZilParser.comment_return);

        ZilParser.noncomment_expr_return noncomment_expr27 = default(ZilParser.noncomment_expr_return);



        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:107:2: ( comment | noncomment_expr )
            int alt5 = 2;
            int LA5_0 = input.LA(1);

            if ( (LA5_0 == SEMI) )
            {
                alt5 = 1;
            }
            else if ( (LA5_0 == HASH || (LA5_0 >= STRING && LA5_0 <= CHAR) || (LA5_0 >= ATOM && LA5_0 <= NUM) || (LA5_0 >= PERCENT && LA5_0 <= LANGLE) || LA5_0 == LPAREN || LA5_0 == LSQUARE || (LA5_0 >= BANG && LA5_0 <= APOS)) )
            {
                alt5 = 2;
            }
            else 
            {
                if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                NoViableAltException nvae_d5s0 =
                    new NoViableAltException("", 5, 0, input);

                throw nvae_d5s0;
            }
            switch (alt5) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:107:4: comment
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	PushFollow(FOLLOW_comment_in_comment_or_expr727);
                    	comment26 = comment();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, comment26.Tree);

                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:108:4: noncomment_expr
                    {
                    	root_0 = (object)adaptor.GetNilNode();

                    	PushFollow(FOLLOW_noncomment_expr_in_comment_or_expr732);
                    	noncomment_expr27 = noncomment_expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( state.backtracking == 0 ) adaptor.AddChild(root_0, noncomment_expr27.Tree);

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
    // $ANTLR end "comment_or_expr"

    public class form_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "form"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:111:1: form : (h= LANGLE ( comment_or_expr )* RANGLE -> ^( FORM[$h] ( comment_or_expr )* ) | h= DOT expr -> ^( FORM[$h] ATOM[\"LVAL\"] expr ) | h= COMMA expr -> ^( FORM[$h] ATOM[\"GVAL\"] expr ) | h= APOS expr -> ^( FORM[$h] ATOM[\"QUOTE\"] expr ) );
    public ZilParser.form_return form() // throws RecognitionException [1]
    {   
        ZilParser.form_return retval = new ZilParser.form_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken h = null;
        IToken RANGLE29 = null;
        ZilParser.comment_or_expr_return comment_or_expr28 = default(ZilParser.comment_or_expr_return);

        ZilParser.expr_return expr30 = default(ZilParser.expr_return);

        ZilParser.expr_return expr31 = default(ZilParser.expr_return);

        ZilParser.expr_return expr32 = default(ZilParser.expr_return);


        object h_tree=null;
        object RANGLE29_tree=null;
        RewriteRuleTokenStream stream_APOS = new RewriteRuleTokenStream(adaptor,"token APOS");
        RewriteRuleTokenStream stream_RANGLE = new RewriteRuleTokenStream(adaptor,"token RANGLE");
        RewriteRuleTokenStream stream_DOT = new RewriteRuleTokenStream(adaptor,"token DOT");
        RewriteRuleTokenStream stream_COMMA = new RewriteRuleTokenStream(adaptor,"token COMMA");
        RewriteRuleTokenStream stream_LANGLE = new RewriteRuleTokenStream(adaptor,"token LANGLE");
        RewriteRuleSubtreeStream stream_comment_or_expr = new RewriteRuleSubtreeStream(adaptor,"rule comment_or_expr");
        RewriteRuleSubtreeStream stream_expr = new RewriteRuleSubtreeStream(adaptor,"rule expr");
        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:111:6: (h= LANGLE ( comment_or_expr )* RANGLE -> ^( FORM[$h] ( comment_or_expr )* ) | h= DOT expr -> ^( FORM[$h] ATOM[\"LVAL\"] expr ) | h= COMMA expr -> ^( FORM[$h] ATOM[\"GVAL\"] expr ) | h= APOS expr -> ^( FORM[$h] ATOM[\"QUOTE\"] expr ) )
            int alt7 = 4;
            switch ( input.LA(1) ) 
            {
            case LANGLE:
            	{
                alt7 = 1;
                }
                break;
            case DOT:
            	{
                alt7 = 2;
                }
                break;
            case COMMA:
            	{
                alt7 = 3;
                }
                break;
            case APOS:
            	{
                alt7 = 4;
                }
                break;
            	default:
            	    if ( state.backtracking > 0 ) {state.failed = true; return retval;}
            	    NoViableAltException nvae_d7s0 =
            	        new NoViableAltException("", 7, 0, input);

            	    throw nvae_d7s0;
            }

            switch (alt7) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:111:8: h= LANGLE ( comment_or_expr )* RANGLE
                    {
                    	h=(IToken)Match(input,LANGLE,FOLLOW_LANGLE_in_form744); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_LANGLE.Add(h);

                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:111:17: ( comment_or_expr )*
                    	do 
                    	{
                    	    int alt6 = 2;
                    	    int LA6_0 = input.LA(1);

                    	    if ( (LA6_0 == HASH || (LA6_0 >= STRING && LA6_0 <= CHAR) || (LA6_0 >= ATOM && LA6_0 <= NUM) || LA6_0 == SEMI || (LA6_0 >= PERCENT && LA6_0 <= LANGLE) || LA6_0 == LPAREN || LA6_0 == LSQUARE || (LA6_0 >= BANG && LA6_0 <= APOS)) )
                    	    {
                    	        alt6 = 1;
                    	    }


                    	    switch (alt6) 
                    		{
                    			case 1 :
                    			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:111:17: comment_or_expr
                    			    {
                    			    	PushFollow(FOLLOW_comment_or_expr_in_form746);
                    			    	comment_or_expr28 = comment_or_expr();
                    			    	state.followingStackPointer--;
                    			    	if (state.failed) return retval;
                    			    	if ( (state.backtracking==0) ) stream_comment_or_expr.Add(comment_or_expr28.Tree);

                    			    }
                    			    break;

                    			default:
                    			    goto loop6;
                    	    }
                    	} while (true);

                    	loop6:
                    		;	// Stops C# compiler whining that label 'loop6' has no statements

                    	RANGLE29=(IToken)Match(input,RANGLE,FOLLOW_RANGLE_in_form749); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_RANGLE.Add(RANGLE29);



                    	// AST REWRITE
                    	// elements:          comment_or_expr
                    	// token labels:      
                    	// rule labels:       retval
                    	// token list labels: 
                    	// rule list labels:  
                    	// wildcard labels: 
                    	if ( (state.backtracking==0) ) {
                    	retval.Tree = root_0;
                    	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	root_0 = (object)adaptor.GetNilNode();
                    	// 112:6: -> ^( FORM[$h] ( comment_or_expr )* )
                    	{
                    	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:112:9: ^( FORM[$h] ( comment_or_expr )* )
                    	    {
                    	    object root_1 = (object)adaptor.GetNilNode();
                    	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(FORM, h), root_1);

                    	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:112:20: ( comment_or_expr )*
                    	    while ( stream_comment_or_expr.HasNext() )
                    	    {
                    	        adaptor.AddChild(root_1, stream_comment_or_expr.NextTree());

                    	    }
                    	    stream_comment_or_expr.Reset();

                    	    adaptor.AddChild(root_0, root_1);
                    	    }

                    	}

                    	retval.Tree = root_0;retval.Tree = root_0;}
                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:113:4: h= DOT expr
                    {
                    	h=(IToken)Match(input,DOT,FOLLOW_DOT_in_form771); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_DOT.Add(h);

                    	PushFollow(FOLLOW_expr_in_form773);
                    	expr30 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( (state.backtracking==0) ) stream_expr.Add(expr30.Tree);


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
                    	// 113:16: -> ^( FORM[$h] ATOM[\"LVAL\"] expr )
                    	{
                    	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:113:19: ^( FORM[$h] ATOM[\"LVAL\"] expr )
                    	    {
                    	    object root_1 = (object)adaptor.GetNilNode();
                    	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(FORM, h), root_1);

                    	    adaptor.AddChild(root_1, (object)adaptor.Create(ATOM, "LVAL"));
                    	    adaptor.AddChild(root_1, stream_expr.NextTree());

                    	    adaptor.AddChild(root_0, root_1);
                    	    }

                    	}

                    	retval.Tree = root_0;retval.Tree = root_0;}
                    }
                    break;
                case 3 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:114:4: h= COMMA expr
                    {
                    	h=(IToken)Match(input,COMMA,FOLLOW_COMMA_in_form793); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_COMMA.Add(h);

                    	PushFollow(FOLLOW_expr_in_form795);
                    	expr31 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( (state.backtracking==0) ) stream_expr.Add(expr31.Tree);


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
                    	// 114:18: -> ^( FORM[$h] ATOM[\"GVAL\"] expr )
                    	{
                    	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:114:21: ^( FORM[$h] ATOM[\"GVAL\"] expr )
                    	    {
                    	    object root_1 = (object)adaptor.GetNilNode();
                    	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(FORM, h), root_1);

                    	    adaptor.AddChild(root_1, (object)adaptor.Create(ATOM, "GVAL"));
                    	    adaptor.AddChild(root_1, stream_expr.NextTree());

                    	    adaptor.AddChild(root_0, root_1);
                    	    }

                    	}

                    	retval.Tree = root_0;retval.Tree = root_0;}
                    }
                    break;
                case 4 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:115:4: h= APOS expr
                    {
                    	h=(IToken)Match(input,APOS,FOLLOW_APOS_in_form815); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_APOS.Add(h);

                    	PushFollow(FOLLOW_expr_in_form817);
                    	expr32 = expr();
                    	state.followingStackPointer--;
                    	if (state.failed) return retval;
                    	if ( (state.backtracking==0) ) stream_expr.Add(expr32.Tree);


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
                    	// 115:17: -> ^( FORM[$h] ATOM[\"QUOTE\"] expr )
                    	{
                    	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:115:20: ^( FORM[$h] ATOM[\"QUOTE\"] expr )
                    	    {
                    	    object root_1 = (object)adaptor.GetNilNode();
                    	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(FORM, h), root_1);

                    	    adaptor.AddChild(root_1, (object)adaptor.Create(ATOM, "QUOTE"));
                    	    adaptor.AddChild(root_1, stream_expr.NextTree());

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
        }
        return retval;
    }
    // $ANTLR end "form"

    public class segment_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "segment"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:118:1: segment : BANG form -> ^( SEGMENT form ) ;
    public ZilParser.segment_return segment() // throws RecognitionException [1]
    {   
        ZilParser.segment_return retval = new ZilParser.segment_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken BANG33 = null;
        ZilParser.form_return form34 = default(ZilParser.form_return);


        object BANG33_tree=null;
        RewriteRuleTokenStream stream_BANG = new RewriteRuleTokenStream(adaptor,"token BANG");
        RewriteRuleSubtreeStream stream_form = new RewriteRuleSubtreeStream(adaptor,"rule form");
        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:118:9: ( BANG form -> ^( SEGMENT form ) )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:118:11: BANG form
            {
            	BANG33=(IToken)Match(input,BANG,FOLLOW_BANG_in_segment840); if (state.failed) return retval; 
            	if ( (state.backtracking==0) ) stream_BANG.Add(BANG33);

            	PushFollow(FOLLOW_form_in_segment842);
            	form34 = form();
            	state.followingStackPointer--;
            	if (state.failed) return retval;
            	if ( (state.backtracking==0) ) stream_form.Add(form34.Tree);


            	// AST REWRITE
            	// elements:          form
            	// token labels:      
            	// rule labels:       retval
            	// token list labels: 
            	// rule list labels:  
            	// wildcard labels: 
            	if ( (state.backtracking==0) ) {
            	retval.Tree = root_0;
            	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

            	root_0 = (object)adaptor.GetNilNode();
            	// 118:22: -> ^( SEGMENT form )
            	{
            	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:118:25: ^( SEGMENT form )
            	    {
            	    object root_1 = (object)adaptor.GetNilNode();
            	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(SEGMENT, "SEGMENT"), root_1);

            	    adaptor.AddChild(root_1, stream_form.NextTree());

            	    adaptor.AddChild(root_0, root_1);
            	    }

            	}

            	retval.Tree = root_0;retval.Tree = root_0;}
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
    // $ANTLR end "segment"

    public class list_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "list"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:121:1: list : LPAREN ( comment_or_expr )* RPAREN -> ^( LIST ( comment_or_expr )* ) ;
    public ZilParser.list_return list() // throws RecognitionException [1]
    {   
        ZilParser.list_return retval = new ZilParser.list_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken LPAREN35 = null;
        IToken RPAREN37 = null;
        ZilParser.comment_or_expr_return comment_or_expr36 = default(ZilParser.comment_or_expr_return);


        object LPAREN35_tree=null;
        object RPAREN37_tree=null;
        RewriteRuleTokenStream stream_RPAREN = new RewriteRuleTokenStream(adaptor,"token RPAREN");
        RewriteRuleTokenStream stream_LPAREN = new RewriteRuleTokenStream(adaptor,"token LPAREN");
        RewriteRuleSubtreeStream stream_comment_or_expr = new RewriteRuleSubtreeStream(adaptor,"rule comment_or_expr");
        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:121:6: ( LPAREN ( comment_or_expr )* RPAREN -> ^( LIST ( comment_or_expr )* ) )
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:121:8: LPAREN ( comment_or_expr )* RPAREN
            {
            	LPAREN35=(IToken)Match(input,LPAREN,FOLLOW_LPAREN_in_list861); if (state.failed) return retval; 
            	if ( (state.backtracking==0) ) stream_LPAREN.Add(LPAREN35);

            	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:121:15: ( comment_or_expr )*
            	do 
            	{
            	    int alt8 = 2;
            	    int LA8_0 = input.LA(1);

            	    if ( (LA8_0 == HASH || (LA8_0 >= STRING && LA8_0 <= CHAR) || (LA8_0 >= ATOM && LA8_0 <= NUM) || LA8_0 == SEMI || (LA8_0 >= PERCENT && LA8_0 <= LANGLE) || LA8_0 == LPAREN || LA8_0 == LSQUARE || (LA8_0 >= BANG && LA8_0 <= APOS)) )
            	    {
            	        alt8 = 1;
            	    }


            	    switch (alt8) 
            		{
            			case 1 :
            			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:121:15: comment_or_expr
            			    {
            			    	PushFollow(FOLLOW_comment_or_expr_in_list863);
            			    	comment_or_expr36 = comment_or_expr();
            			    	state.followingStackPointer--;
            			    	if (state.failed) return retval;
            			    	if ( (state.backtracking==0) ) stream_comment_or_expr.Add(comment_or_expr36.Tree);

            			    }
            			    break;

            			default:
            			    goto loop8;
            	    }
            	} while (true);

            	loop8:
            		;	// Stops C# compiler whining that label 'loop8' has no statements

            	RPAREN37=(IToken)Match(input,RPAREN,FOLLOW_RPAREN_in_list866); if (state.failed) return retval; 
            	if ( (state.backtracking==0) ) stream_RPAREN.Add(RPAREN37);



            	// AST REWRITE
            	// elements:          comment_or_expr
            	// token labels:      
            	// rule labels:       retval
            	// token list labels: 
            	// rule list labels:  
            	// wildcard labels: 
            	if ( (state.backtracking==0) ) {
            	retval.Tree = root_0;
            	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

            	root_0 = (object)adaptor.GetNilNode();
            	// 122:6: -> ^( LIST ( comment_or_expr )* )
            	{
            	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:122:9: ^( LIST ( comment_or_expr )* )
            	    {
            	    object root_1 = (object)adaptor.GetNilNode();
            	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(LIST, "LIST"), root_1);

            	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:122:16: ( comment_or_expr )*
            	    while ( stream_comment_or_expr.HasNext() )
            	    {
            	        adaptor.AddChild(root_1, stream_comment_or_expr.NextTree());

            	    }
            	    stream_comment_or_expr.Reset();

            	    adaptor.AddChild(root_0, root_1);
            	    }

            	}

            	retval.Tree = root_0;retval.Tree = root_0;}
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
    // $ANTLR end "list"

    public class vector_return : ParserRuleReturnScope
    {
        private object tree;
        override public object Tree
        {
        	get { return tree; }
        	set { tree = (object) value; }
        }
    };

    // $ANTLR start "vector"
    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:125:1: vector : ( LSQUARE ( comment_or_expr )* RSQUARE -> ^( VECTOR ( comment_or_expr )* ) | BANG LSQUARE ( comment_or_expr )* ( BANG )? RSQUARE -> ^( UVECTOR ( comment_or_expr )* ) );
    public ZilParser.vector_return vector() // throws RecognitionException [1]
    {   
        ZilParser.vector_return retval = new ZilParser.vector_return();
        retval.Start = input.LT(1);

        object root_0 = null;

        IToken LSQUARE38 = null;
        IToken RSQUARE40 = null;
        IToken BANG41 = null;
        IToken LSQUARE42 = null;
        IToken BANG44 = null;
        IToken RSQUARE45 = null;
        ZilParser.comment_or_expr_return comment_or_expr39 = default(ZilParser.comment_or_expr_return);

        ZilParser.comment_or_expr_return comment_or_expr43 = default(ZilParser.comment_or_expr_return);


        object LSQUARE38_tree=null;
        object RSQUARE40_tree=null;
        object BANG41_tree=null;
        object LSQUARE42_tree=null;
        object BANG44_tree=null;
        object RSQUARE45_tree=null;
        RewriteRuleTokenStream stream_BANG = new RewriteRuleTokenStream(adaptor,"token BANG");
        RewriteRuleTokenStream stream_LSQUARE = new RewriteRuleTokenStream(adaptor,"token LSQUARE");
        RewriteRuleTokenStream stream_RSQUARE = new RewriteRuleTokenStream(adaptor,"token RSQUARE");
        RewriteRuleSubtreeStream stream_comment_or_expr = new RewriteRuleSubtreeStream(adaptor,"rule comment_or_expr");
        try 
    	{
            // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:125:8: ( LSQUARE ( comment_or_expr )* RSQUARE -> ^( VECTOR ( comment_or_expr )* ) | BANG LSQUARE ( comment_or_expr )* ( BANG )? RSQUARE -> ^( UVECTOR ( comment_or_expr )* ) )
            int alt12 = 2;
            int LA12_0 = input.LA(1);

            if ( (LA12_0 == LSQUARE) )
            {
                alt12 = 1;
            }
            else if ( (LA12_0 == BANG) )
            {
                alt12 = 2;
            }
            else 
            {
                if ( state.backtracking > 0 ) {state.failed = true; return retval;}
                NoViableAltException nvae_d12s0 =
                    new NoViableAltException("", 12, 0, input);

                throw nvae_d12s0;
            }
            switch (alt12) 
            {
                case 1 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:125:10: LSQUARE ( comment_or_expr )* RSQUARE
                    {
                    	LSQUARE38=(IToken)Match(input,LSQUARE,FOLLOW_LSQUARE_in_vector890); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_LSQUARE.Add(LSQUARE38);

                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:125:18: ( comment_or_expr )*
                    	do 
                    	{
                    	    int alt9 = 2;
                    	    int LA9_0 = input.LA(1);

                    	    if ( (LA9_0 == HASH || (LA9_0 >= STRING && LA9_0 <= CHAR) || (LA9_0 >= ATOM && LA9_0 <= NUM) || LA9_0 == SEMI || (LA9_0 >= PERCENT && LA9_0 <= LANGLE) || LA9_0 == LPAREN || LA9_0 == LSQUARE || (LA9_0 >= BANG && LA9_0 <= APOS)) )
                    	    {
                    	        alt9 = 1;
                    	    }


                    	    switch (alt9) 
                    		{
                    			case 1 :
                    			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:125:18: comment_or_expr
                    			    {
                    			    	PushFollow(FOLLOW_comment_or_expr_in_vector892);
                    			    	comment_or_expr39 = comment_or_expr();
                    			    	state.followingStackPointer--;
                    			    	if (state.failed) return retval;
                    			    	if ( (state.backtracking==0) ) stream_comment_or_expr.Add(comment_or_expr39.Tree);

                    			    }
                    			    break;

                    			default:
                    			    goto loop9;
                    	    }
                    	} while (true);

                    	loop9:
                    		;	// Stops C# compiler whining that label 'loop9' has no statements

                    	RSQUARE40=(IToken)Match(input,RSQUARE,FOLLOW_RSQUARE_in_vector895); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_RSQUARE.Add(RSQUARE40);



                    	// AST REWRITE
                    	// elements:          comment_or_expr
                    	// token labels:      
                    	// rule labels:       retval
                    	// token list labels: 
                    	// rule list labels:  
                    	// wildcard labels: 
                    	if ( (state.backtracking==0) ) {
                    	retval.Tree = root_0;
                    	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	root_0 = (object)adaptor.GetNilNode();
                    	// 126:6: -> ^( VECTOR ( comment_or_expr )* )
                    	{
                    	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:126:9: ^( VECTOR ( comment_or_expr )* )
                    	    {
                    	    object root_1 = (object)adaptor.GetNilNode();
                    	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(VECTOR, "VECTOR"), root_1);

                    	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:126:18: ( comment_or_expr )*
                    	    while ( stream_comment_or_expr.HasNext() )
                    	    {
                    	        adaptor.AddChild(root_1, stream_comment_or_expr.NextTree());

                    	    }
                    	    stream_comment_or_expr.Reset();

                    	    adaptor.AddChild(root_0, root_1);
                    	    }

                    	}

                    	retval.Tree = root_0;retval.Tree = root_0;}
                    }
                    break;
                case 2 :
                    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:127:4: BANG LSQUARE ( comment_or_expr )* ( BANG )? RSQUARE
                    {
                    	BANG41=(IToken)Match(input,BANG,FOLLOW_BANG_in_vector914); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_BANG.Add(BANG41);

                    	LSQUARE42=(IToken)Match(input,LSQUARE,FOLLOW_LSQUARE_in_vector916); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_LSQUARE.Add(LSQUARE42);

                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:127:17: ( comment_or_expr )*
                    	do 
                    	{
                    	    int alt10 = 2;
                    	    int LA10_0 = input.LA(1);

                    	    if ( (LA10_0 == BANG) )
                    	    {
                    	        int LA10_1 = input.LA(2);

                    	        if ( (LA10_1 == LANGLE || LA10_1 == LSQUARE || (LA10_1 >= DOT && LA10_1 <= APOS)) )
                    	        {
                    	            alt10 = 1;
                    	        }


                    	    }
                    	    else if ( (LA10_0 == HASH || (LA10_0 >= STRING && LA10_0 <= CHAR) || (LA10_0 >= ATOM && LA10_0 <= NUM) || LA10_0 == SEMI || (LA10_0 >= PERCENT && LA10_0 <= LANGLE) || LA10_0 == LPAREN || LA10_0 == LSQUARE || (LA10_0 >= DOT && LA10_0 <= APOS)) )
                    	    {
                    	        alt10 = 1;
                    	    }


                    	    switch (alt10) 
                    		{
                    			case 1 :
                    			    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:127:17: comment_or_expr
                    			    {
                    			    	PushFollow(FOLLOW_comment_or_expr_in_vector918);
                    			    	comment_or_expr43 = comment_or_expr();
                    			    	state.followingStackPointer--;
                    			    	if (state.failed) return retval;
                    			    	if ( (state.backtracking==0) ) stream_comment_or_expr.Add(comment_or_expr43.Tree);

                    			    }
                    			    break;

                    			default:
                    			    goto loop10;
                    	    }
                    	} while (true);

                    	loop10:
                    		;	// Stops C# compiler whining that label 'loop10' has no statements

                    	// C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:127:34: ( BANG )?
                    	int alt11 = 2;
                    	int LA11_0 = input.LA(1);

                    	if ( (LA11_0 == BANG) )
                    	{
                    	    alt11 = 1;
                    	}
                    	switch (alt11) 
                    	{
                    	    case 1 :
                    	        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:127:34: BANG
                    	        {
                    	        	BANG44=(IToken)Match(input,BANG,FOLLOW_BANG_in_vector921); if (state.failed) return retval; 
                    	        	if ( (state.backtracking==0) ) stream_BANG.Add(BANG44);


                    	        }
                    	        break;

                    	}

                    	RSQUARE45=(IToken)Match(input,RSQUARE,FOLLOW_RSQUARE_in_vector924); if (state.failed) return retval; 
                    	if ( (state.backtracking==0) ) stream_RSQUARE.Add(RSQUARE45);



                    	// AST REWRITE
                    	// elements:          comment_or_expr
                    	// token labels:      
                    	// rule labels:       retval
                    	// token list labels: 
                    	// rule list labels:  
                    	// wildcard labels: 
                    	if ( (state.backtracking==0) ) {
                    	retval.Tree = root_0;
                    	RewriteRuleSubtreeStream stream_retval = new RewriteRuleSubtreeStream(adaptor, "rule retval", retval!=null ? retval.Tree : null);

                    	root_0 = (object)adaptor.GetNilNode();
                    	// 128:6: -> ^( UVECTOR ( comment_or_expr )* )
                    	{
                    	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:128:9: ^( UVECTOR ( comment_or_expr )* )
                    	    {
                    	    object root_1 = (object)adaptor.GetNilNode();
                    	    root_1 = (object)adaptor.BecomeRoot((object)adaptor.Create(UVECTOR, "UVECTOR"), root_1);

                    	    // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:128:19: ( comment_or_expr )*
                    	    while ( stream_comment_or_expr.HasNext() )
                    	    {
                    	        adaptor.AddChild(root_1, stream_comment_or_expr.NextTree());

                    	    }
                    	    stream_comment_or_expr.Reset();

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
        }
        return retval;
    }
    // $ANTLR end "vector"

    // $ANTLR start "synpred1_Zil"
    public void synpred1_Zil_fragment() {
        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:102:5: ( PERCENT )
        // C:\\Users\\Jesse\\Documents\\ZIL\\Zilf\\Zilf\\Zil.g:102:6: PERCENT
        {
        	Match(input,PERCENT,FOLLOW_PERCENT_in_synpred1_Zil683); if (state.failed) return ;

        }
    }
    // $ANTLR end "synpred1_Zil"

    // Delegated rules

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


   	protected DFA3 dfa3;
   	protected DFA4 dfa4;
	private void InitializeCyclicDFAs()
	{
    	this.dfa3 = new DFA3(this);
    	this.dfa4 = new DFA4(this);
	    this.dfa4.specialStateTransitionHandler = new DFA.SpecialStateTransitionHandler(DFA4_SpecialStateTransition);
	}

    const string DFA3_eotS =
        "\x0e\uffff";
    const string DFA3_eofS =
        "\x01\uffff\x01\x0c\x0c\uffff";
    const string DFA3_minS =
        "\x02\x0c\x04\uffff\x01\x1e\x07\uffff";
    const string DFA3_maxS =
        "\x02\x27\x04\uffff\x01\x27\x07\uffff";
    const string DFA3_acceptS =
        "\x02\uffff\x01\x03\x01\x04\x01\x05\x01\x06\x01\uffff\x01\x08\x01"+
        "\x09\x01\x0a\x01\x0b\x01\x02\x01\x01\x01\x07";
    const string DFA3_specialS =
        "\x0e\uffff}>";
    static readonly string[] DFA3_transitionS = {
            "\x01\x0a\x04\uffff\x01\x04\x01\x03\x05\uffff\x01\x01\x01\x02"+
            "\x03\uffff\x01\x09\x01\x05\x01\uffff\x01\x08\x01\uffff\x01\x07"+
            "\x01\uffff\x01\x06\x03\x05",
            "\x01\x0c\x04\uffff\x02\x0c\x05\uffff\x02\x0c\x01\uffff\x01"+
            "\x0c\x01\x0b\x0b\x0c",
            "",
            "",
            "",
            "",
            "\x01\x0d\x03\uffff\x01\x07\x02\uffff\x03\x0d",
            "",
            "",
            "",
            "",
            "",
            "",
            ""
    };

    static readonly short[] DFA3_eot = DFA.UnpackEncodedString(DFA3_eotS);
    static readonly short[] DFA3_eof = DFA.UnpackEncodedString(DFA3_eofS);
    static readonly char[] DFA3_min = DFA.UnpackEncodedStringToUnsignedChars(DFA3_minS);
    static readonly char[] DFA3_max = DFA.UnpackEncodedStringToUnsignedChars(DFA3_maxS);
    static readonly short[] DFA3_accept = DFA.UnpackEncodedString(DFA3_acceptS);
    static readonly short[] DFA3_special = DFA.UnpackEncodedString(DFA3_specialS);
    static readonly short[][] DFA3_transition = DFA.UnpackEncodedStringArray(DFA3_transitionS);

    protected class DFA3 : DFA
    {
        public DFA3(BaseRecognizer recognizer)
        {
            this.recognizer = recognizer;
            this.decisionNumber = 3;
            this.eot = DFA3_eot;
            this.eof = DFA3_eof;
            this.min = DFA3_min;
            this.max = DFA3_max;
            this.accept = DFA3_accept;
            this.special = DFA3_special;
            this.transition = DFA3_transition;

        }

        override public string Description
        {
            get { return "86:1: noncomment_expr : ( ATOM | ATOM COLON ATOM -> ^( ADECL ATOM ATOM ) | NUM | CHAR | STRING | form | segment | vector | list | macro | HASH ATOM expr -> ^( HASH ATOM expr ) );"; }
        }

    }

    const string DFA4_eotS =
        "\x10\uffff";
    const string DFA4_eofS =
        "\x10\uffff";
    const string DFA4_minS =
        "\x01\x0c\x01\x00\x0e\uffff";
    const string DFA4_maxS =
        "\x01\x27\x01\x00\x0e\uffff";
    const string DFA4_acceptS =
        "\x02\uffff\x01\x02\x0c\uffff\x01\x01";
    const string DFA4_specialS =
        "\x01\uffff\x01\x00\x0e\uffff}>";
    static readonly string[] DFA4_transitionS = {
            "\x01\x02\x04\uffff\x02\x02\x05\uffff\x02\x02\x01\uffff\x01"+
            "\x02\x01\uffff\x01\x01\x01\x02\x01\uffff\x01\x02\x01\uffff\x01"+
            "\x02\x01\uffff\x04\x02",
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
            "",
            "",
            ""
    };

    static readonly short[] DFA4_eot = DFA.UnpackEncodedString(DFA4_eotS);
    static readonly short[] DFA4_eof = DFA.UnpackEncodedString(DFA4_eofS);
    static readonly char[] DFA4_min = DFA.UnpackEncodedStringToUnsignedChars(DFA4_minS);
    static readonly char[] DFA4_max = DFA.UnpackEncodedStringToUnsignedChars(DFA4_maxS);
    static readonly short[] DFA4_accept = DFA.UnpackEncodedString(DFA4_acceptS);
    static readonly short[] DFA4_special = DFA.UnpackEncodedString(DFA4_specialS);
    static readonly short[][] DFA4_transition = DFA.UnpackEncodedStringArray(DFA4_transitionS);

    protected class DFA4 : DFA
    {
        public DFA4(BaseRecognizer recognizer)
        {
            this.recognizer = recognizer;
            this.decisionNumber = 4;
            this.eot = DFA4_eot;
            this.eof = DFA4_eof;
            this.min = DFA4_min;
            this.max = DFA4_max;
            this.accept = DFA4_accept;
            this.special = DFA4_special;
            this.transition = DFA4_transition;

        }

        override public string Description
        {
            get { return "102:3: ( ( PERCENT )=> PERCENT expr -> ^( VMACRO expr ) | expr -> ^( MACRO expr ) )"; }
        }

    }


    protected internal int DFA4_SpecialStateTransition(DFA dfa, int s, IIntStream _input) //throws NoViableAltException
    {
            ITokenStream input = (ITokenStream)_input;
    	int _s = s;
        switch ( s )
        {
               	case 0 : 
                   	int LA4_1 = input.LA(1);

                   	 
                   	int index4_1 = input.Index();
                   	input.Rewind();
                   	s = -1;
                   	if ( (synpred1_Zil()) ) { s = 15; }

                   	else if ( (true) ) { s = 2; }

                   	 
                   	input.Seek(index4_1);
                   	if ( s >= 0 ) return s;
                   	break;
        }
        if (state.backtracking > 0) {state.failed = true; return -1;}
        NoViableAltException nvae4 =
            new NoViableAltException(dfa.Description, 4, _s, input);
        dfa.Error(nvae4);
        throw nvae4;
    }
 

    public static readonly BitSet FOLLOW_comment_in_file535 = new BitSet(new ulong[]{0x000000F56B061002UL});
    public static readonly BitSet FOLLOW_noncomment_expr_in_file539 = new BitSet(new ulong[]{0x000000F56B061002UL});
    public static readonly BitSet FOLLOW_comment_in_expr551 = new BitSet(new ulong[]{0x000000F56B061000UL});
    public static readonly BitSet FOLLOW_noncomment_expr_in_expr554 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_SEMI_in_comment564 = new BitSet(new ulong[]{0x000000F56B061000UL});
    public static readonly BitSet FOLLOW_noncomment_expr_in_comment566 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_ATOM_in_noncomment_expr585 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_ATOM_in_noncomment_expr590 = new BitSet(new ulong[]{0x0000000010000000UL});
    public static readonly BitSet FOLLOW_COLON_in_noncomment_expr592 = new BitSet(new ulong[]{0x0000000001000000UL});
    public static readonly BitSet FOLLOW_ATOM_in_noncomment_expr594 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_NUM_in_noncomment_expr610 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_CHAR_in_noncomment_expr615 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_STRING_in_noncomment_expr620 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_form_in_noncomment_expr625 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_segment_in_noncomment_expr630 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_vector_in_noncomment_expr635 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_list_in_noncomment_expr640 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_macro_in_noncomment_expr645 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_HASH_in_noncomment_expr650 = new BitSet(new ulong[]{0x0000000001000000UL});
    public static readonly BitSet FOLLOW_ATOM_in_noncomment_expr652 = new BitSet(new ulong[]{0x000000F56B061000UL});
    public static readonly BitSet FOLLOW_expr_in_noncomment_expr654 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_PERCENT_in_macro676 = new BitSet(new ulong[]{0x000000F56B061000UL});
    public static readonly BitSet FOLLOW_PERCENT_in_macro687 = new BitSet(new ulong[]{0x000000F56B061000UL});
    public static readonly BitSet FOLLOW_expr_in_macro689 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_expr_in_macro703 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_comment_in_comment_or_expr727 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_noncomment_expr_in_comment_or_expr732 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_LANGLE_in_form744 = new BitSet(new ulong[]{0x000000F5EB061000UL});
    public static readonly BitSet FOLLOW_comment_or_expr_in_form746 = new BitSet(new ulong[]{0x000000F5EB061000UL});
    public static readonly BitSet FOLLOW_RANGLE_in_form749 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_DOT_in_form771 = new BitSet(new ulong[]{0x000000F56B061000UL});
    public static readonly BitSet FOLLOW_expr_in_form773 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_COMMA_in_form793 = new BitSet(new ulong[]{0x000000F56B061000UL});
    public static readonly BitSet FOLLOW_expr_in_form795 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_APOS_in_form815 = new BitSet(new ulong[]{0x000000F56B061000UL});
    public static readonly BitSet FOLLOW_expr_in_form817 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_BANG_in_segment840 = new BitSet(new ulong[]{0x000000E040000000UL});
    public static readonly BitSet FOLLOW_form_in_segment842 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_LPAREN_in_list861 = new BitSet(new ulong[]{0x000000F76B061000UL});
    public static readonly BitSet FOLLOW_comment_or_expr_in_list863 = new BitSet(new ulong[]{0x000000F76B061000UL});
    public static readonly BitSet FOLLOW_RPAREN_in_list866 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_LSQUARE_in_vector890 = new BitSet(new ulong[]{0x000000FD6B061000UL});
    public static readonly BitSet FOLLOW_comment_or_expr_in_vector892 = new BitSet(new ulong[]{0x000000FD6B061000UL});
    public static readonly BitSet FOLLOW_RSQUARE_in_vector895 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_BANG_in_vector914 = new BitSet(new ulong[]{0x0000000400000000UL});
    public static readonly BitSet FOLLOW_LSQUARE_in_vector916 = new BitSet(new ulong[]{0x000000FD6B061000UL});
    public static readonly BitSet FOLLOW_comment_or_expr_in_vector918 = new BitSet(new ulong[]{0x000000FD6B061000UL});
    public static readonly BitSet FOLLOW_BANG_in_vector921 = new BitSet(new ulong[]{0x0000000800000000UL});
    public static readonly BitSet FOLLOW_RSQUARE_in_vector924 = new BitSet(new ulong[]{0x0000000000000002UL});
    public static readonly BitSet FOLLOW_PERCENT_in_synpred1_Zil683 = new BitSet(new ulong[]{0x0000000000000002UL});

}
}