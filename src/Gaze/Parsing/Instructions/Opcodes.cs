/* Copyright 2010-2025 Tara McGrew
 * 
 * This file is part of ZILF.
 * 
 * ZILF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZILF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZILF.  If not, see <http://www.gnu.org/licenses/>.
 */

namespace Gaze.Parsing.Instructions
{
    public static class Opcodes
    {
        [GOp(/*accelfunc*/ 2)]
        public const int accelfunc = 0x180;
        [GOp(/*accelparam*/ 2)]
        public const int accelparam = 0x181;
        [GOp(/*acos*/ 2)]
        public const int acos = 0x1B4;
        [GOp(/*add*/ 3)]
        public const int add = 0x10;
        [GOp(/*aload*/ 3)]
        public const int aload = 0x48;
        [GOp(/*aloadb*/ 3)]
        public const int aloadb = 0x4A;
        [GOp(/*aloadbit*/ 3)]
        public const int aloadbit = 0x4B;
        [GOp(/*aloads*/ 3)]
        public const int aloads = 0x49;
        [GOp(/*asin*/ 2)]
        public const int asin = 0x1B3;
        [GOp(/*astore*/ 3)]
        public const int astore = 0x4C;
        [GOp(/*astoreb*/ 3)]
        public const int astoreb = 0x4E;
        [GOp(/*astorebit*/ 3)]
        public const int astorebit = 0x4F;
        [GOp(/*astores*/ 3)]
        public const int astores = 0x4D;
        [GOp(/*atan*/ 2)]
        public const int atan = 0x1B5;
        [GOp(/*atan2*/ 3)]
        public const int atan2 = 0x1B6;
        [GOp(/*binarysearch*/ 8)]
        public const int binarysearch = 0x151;
        [GOp(/*bitand*/ 3)]
        public const int bitand = 0x18;
        [GOp(/*bitnot*/ 2)]
        public const int bitnot = 0x1B;
        [GOp(/*bitor*/ 3)]
        public const int bitor = 0x19;
        [GOp(/*bitxor*/ 3)]
        public const int bitxor = 0x1A;
        [GOp(/*call*/ 3)]
        public const int call = 0x30;
        [GOp(/*callf*/ 2)]
        public const int callf = 0x160;
        [GOp(/*callfi*/ 3)]
        public const int callfi = 0x161;
        [GOp(/*callfii*/ 4)]
        public const int callfii = 0x162;
        [GOp(/*callfiii*/ 5)]
        public const int callfiii = 0x163;
        [GOp(/*catch*/ 2)]
        public const int @catch = 0x32;
        [GOp(/*ceil*/ 2)]
        public const int ceil = 0x198;
        [GOp(/*copy*/ 2)]
        public const int copy = 0x40;
        [GOp(/*copyb*/ 2)]
        public const int copyb = 0x42;
        [GOp(/*copys*/ 2)]
        public const int copys = 0x41;
        [GOp(/*cos*/ 2)]
        public const int cos = 0x1B1;
        [GOp(/*dacos*/ 4)]
        public const int dacos = 0x224;
        [GOp(/*dadd*/ 6)]
        public const int dadd = 0x210;
        [GOp(/*dasin*/ 4)]
        public const int dasin = 0x223;
        [GOp(/*datan*/ 4)]
        public const int datan = 0x225;
        [GOp(/*datan2*/ 6)]
        public const int datan2 = 0x226;
        [GOp(/*dceil*/ 4)]
        public const int dceil = 0x208;
        [GOp(/*dcos*/ 4)]
        public const int dcos = 0x221;
        [GOp(/*ddiv*/ 6)]
        public const int ddiv = 0x213;
        [GOp(/*debugtrap*/ 1)]
        public const int debugtrap = 0x101;
        [GOp(/*dexp*/ 4)]
        public const int dexp = 0x219;
        [GOp(/*dfloor*/ 4)]
        public const int dfloor = 0x209;
        [GOp(/*discardundo*/ 0)]
        public const int discardundo = 0x129;
        [GOp(/*div*/ 3)]
        public const int div = 0x13;
        [GOp(/*dlog*/ 4)]
        public const int dlog = 0x21A;
        [GOp(/*dmodq*/ 6)]
        public const int dmodq = 0x215;
        [GOp(/*dmodr*/ 6)]
        public const int dmodr = 0x214;
        [GOp(/*dmul*/ 6)]
        public const int dmul = 0x212;
        [GOp(/*dpow*/ 6)]
        public const int dpow = 0x21B;
        [GOp(/*dsin*/ 4)]
        public const int dsin = 0x220;
        [GOp(/*dsqrt*/ 4)]
        public const int dsqrt = 0x218;
        [GOp(/*dsub*/ 6)]
        public const int dsub = 0x211;
        [GOp(/*dtan*/ 4)]
        public const int dtan = 0x222;
        [GOp(/*dtof*/ 3)]
        public const int dtof = 0x204;
        [GOp(/*dtonumn*/ 3)]
        public const int dtonumn = 0x202;
        [GOp(/*dtonumz*/ 3)]
        public const int dtonumz = 0x201;
        [GOp(/*exp*/ 2)]
        public const int exp = 0x1A9;
        [GOp(/*fadd*/ 3)]
        public const int fadd = 0x1A0;
        [GOp(/*fdiv*/ 3)]
        public const int fdiv = 0x1A3;
        [GOp(/*floor*/ 2)]
        public const int floor = 0x199;
        [GOp(/*fmod*/ 4)]
        public const int fmod = 0x1A4;
        [GOp(/*fmul*/ 3)]
        public const int fmul = 0x1A2;
        [GOp(/*fsub*/ 3)]
        public const int fsub = 0x1A1;
        [GOp(/*ftod*/ 3)]
        public const int ftod = 0x203;
        [GOp(/*ftonumn*/ 2)]
        public const int ftonumn = 0x192;
        [GOp(/*ftonumz*/ 2)]
        public const int ftonumz = 0x191;
        [GOp(/*fyrecall*/ 4)]
        public const int fyrecall = 0x1000;
        [GOp(/*gestalt*/ 3)]
        public const int gestalt = 0x100;
        [GOp(/*getiosys*/ 2)]
        public const int getiosys = 0x148;
        [GOp(/*getmemsize*/ 1)]
        public const int getmemsize = 0x102;
        [GOp(/*getstringtbl*/ 1)]
        public const int getstringtbl = 0x140;
        [GOp(/*glk*/ 3)]
        public const int glk = 0x130;
        [GOp(/*hasundo*/ 1)]
        public const int hasundo = 0x128;
        [GOp(/*jdeq*/ 7)]
        public const int jdeq = 0x230;
        [GOp(/*jdge*/ 5)]
        public const int jdge = 0x235;
        [GOp(/*jdgt*/ 5)]
        public const int jdgt = 0x234;
        [GOp(/*jdisinf*/ 3)]
        public const int jdisinf = 0x239;
        [GOp(/*jdisnan*/ 3)]
        public const int jdisnan = 0x238;
        [GOp(/*jdle*/ 5)]
        public const int jdle = 0x233;
        [GOp(/*jdlt*/ 5)]
        public const int jdlt = 0x232;
        [GOp(/*jdne*/ 7)]
        public const int jdne = 0x231;
        [GOp(/*jeq*/ 3)]
        public const int jeq = 0x24;
        [GOp(/*jfeq*/ 4)]
        public const int jfeq = 0x1C0;
        [GOp(/*jfge*/ 3)]
        public const int jfge = 0x1C5;
        [GOp(/*jfgt*/ 3)]
        public const int jfgt = 0x1C4;
        [GOp(/*jfle*/ 3)]
        public const int jfle = 0x1C3;
        [GOp(/*jflt*/ 3)]
        public const int jflt = 0x1C2;
        [GOp(/*jfne*/ 4)]
        public const int jfne = 0x1C1;
        [GOp(/*jge*/ 3)]
        public const int jge = 0x27;
        [GOp(/*jgeu*/ 3)]
        public const int jgeu = 0x2B;
        [GOp(/*jgt*/ 3)]
        public const int jgt = 0x28;
        [GOp(/*jgtu*/ 3)]
        public const int jgtu = 0x2C;
        [GOp(/*jisinf*/ 2)]
        public const int jisinf = 0x1C9;
        [GOp(/*jisnan*/ 2)]
        public const int jisnan = 0x1C8;
        [GOp(/*jle*/ 3)]
        public const int jle = 0x29;
        [GOp(/*jleu*/ 3)]
        public const int jleu = 0x2D;
        [GOp(/*jlt*/ 3)]
        public const int jlt = 0x26;
        [GOp(/*jltu*/ 3)]
        public const int jltu = 0x2A;
        [GOp(/*jne*/ 3)]
        public const int jne = 0x25;
        [GOp(/*jnz*/ 2)]
        public const int jnz = 0x23;
        [GOp(/*jump*/ 1, GOpFlags.Terminates)]
        public const int jump = 0x20;
        [GOp(/*jumpabs*/ 1, GOpFlags.Terminates)]
        public const int jumpabs = 0x104;
        [GOp(/*jz*/ 2)]
        public const int jz = 0x22;
        [GOp(/*linearsearch*/ 8)]
        public const int linearsearch = 0x150;
        [GOp(/*linkedsearch*/ 7)]
        public const int linkedsearch = 0x152;
        [GOp(/*log*/ 2)]
        public const int log = 0x1AA;
        [GOp(/*malloc*/ 2)]
        public const int malloc = 0x178;
        [GOp(/*mcopy*/ 3)]
        public const int mcopy = 0x171;
        [GOp(/*mfree*/ 1)]
        public const int mfree = 0x179;
        [GOp(/*mod*/ 3)]
        public const int mod = 0x14;
        [GOp(/*mul*/ 3)]
        public const int mul = 0x12;
        [GOp(/*mzero*/ 2)]
        public const int mzero = 0x170;
        [GOp(/*neg*/ 2)]
        public const int neg = 0x15;
        [GOp(/*nop*/ 0)]
        public const int nop = 0x00;
        [GOp(/*numtod*/ 3)]
        public const int numtod = 0x200;
        [GOp(/*numtof*/ 2)]
        public const int numtof = 0x190;
        [GOp(/*pow*/ 3)]
        public const int pow = 0x1AB;
        [GOp(/*protect*/ 2)]
        public const int protect = 0x127;
        [GOp(/*quit*/ 0, GOpFlags.Terminates)]
        public const int quit = 0x120;
        [GOp(/*random*/ 2)]
        public const int random = 0x110;
        [GOp(/*restart*/ 0, GOpFlags.Terminates)]
        public const int restart = 0x122;
        [GOp(/*restore*/ 2)]
        public const int restore = 0x124;
        [GOp(/*restoreundo*/ 1)]
        public const int restoreundo = 0x126;
        [GOp(/*return*/ 1, GOpFlags.Terminates)]
        public const int @return = 0x31;
        [GOp(/*save*/ 2)]
        public const int save = 0x123;
        [GOp(/*saveundo*/ 1)]
        public const int saveundo = 0x125;
        [GOp(/*setiosys*/ 2)]
        public const int setiosys = 0x149;
        [GOp(/*setmemsize*/ 2)]
        public const int setmemsize = 0x103;
        [GOp(/*setrandom*/ 1)]
        public const int setrandom = 0x111;
        [GOp(/*setstringtbl*/ 1)]
        public const int setstringtbl = 0x141;
        [GOp(/*sexb*/ 2)]
        public const int sexb = 0x45;
        [GOp(/*sexs*/ 2)]
        public const int sexs = 0x44;
        [GOp(/*shiftl*/ 3)]
        public const int shiftl = 0x1C;
        [GOp(/*sin*/ 2)]
        public const int sin = 0x1B0;
        [GOp(/*sqrt*/ 2)]
        public const int sqrt = 0x1A8;
        [GOp(/*sshiftr*/ 3)]
        public const int sshiftr = 0x1D;
        [GOp(/*stkcopy*/ 1)]
        public const int stkcopy = 0x54;
        [GOp(/*stkcount*/ 1)]
        public const int stkcount = 0x50;
        [GOp(/*stkpeek*/ 2)]
        public const int stkpeek = 0x51;
        [GOp(/*stkroll*/ 2)]
        public const int stkroll = 0x53;
        [GOp(/*stkswap*/ 0)]
        public const int stkswap = 0x52;
        [GOp(/*streamchar*/ 1)]
        public const int streamchar = 0x70;
        [GOp(/*streamnum*/ 1)]
        public const int streamnum = 0x71;
        [GOp(/*streamstr*/ 1)]
        public const int streamstr = 0x72;
        [GOp(/*streamunichar*/ 1)]
        public const int streamunichar = 0x73;
        [GOp(/*sub*/ 3)]
        public const int sub = 0x11;
        [GOp(/*tailcall*/ 2)]
        public const int tailcall = 0x34;
        [GOp(/*tan*/ 2, GOpFlags.Terminates)]
        public const int tan = 0x1B2;
        [GOp(/*throw*/ 2, GOpFlags.Terminates)]
        public const int @throw = 0x33;
        [GOp(/*ushiftr*/ 3)]
        public const int ushiftr = 0x1E;
        [GOp(/*verify*/ 1)]
        public const int verify = 0x121;
    }
}