/* Copyright 2010, 2015 Jesse McGrew
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using ZLR.VM;

namespace IntegrationTests
{
    abstract class TestCaseIO
    {
        protected readonly StringBuilder outputBuffer = new StringBuilder();

        public string CollectOutput()
        {
            string result = outputBuffer.ToString();
            outputBuffer.Length = 0;
            return result;
        }
    }

    class ReplayIO : TestCaseIO, IZMachineIO
    {
        readonly Stream inputStream;
        readonly bool wantStatusLine;

        MemoryStream saveStream;

        public ReplayIO(Stream prevInputStream, bool wantStatusLine = false)
        {
            this.inputStream = prevInputStream;
            this.wantStatusLine = wantStatusLine;
        }

        #region Z-machine I/O implementation

        string IZMachineIO.ReadLine(string initial, int time, TimedInputCallback callback,
            byte[] terminatingKeys, out byte terminator)
        {
            throw new AssertFailedException("Unexpected line input request");
        }

        void IZMachineIO.PutCommand(string command)
        {
            // nada
        }

        short IZMachineIO.ReadKey(int time, TimedInputCallback callback, CharTranslator translator)
        {
            throw new AssertFailedException("Unexpected character input request");
        }

        void IZMachineIO.PutChar(char ch)
        {
            outputBuffer.Append(ch);
        }

        void IZMachineIO.PutString(string str)
        {
            outputBuffer.Append(str);
        }

        void IZMachineIO.PutTextRectangle(string[] lines)
        {
            foreach (string line in lines)
                outputBuffer.AppendLine(line);
        }

        bool IZMachineIO.Transcripting
        {
            get { return false; }
            set { /* nada */}
        }

        void IZMachineIO.PutTranscriptChar(char ch)
        {
            // nada
        }

        void IZMachineIO.PutTranscriptString(string str)
        {
            // nada
        }

        Stream IZMachineIO.OpenSaveFile(int size)
        {
            saveStream = new MemoryStream();
            return saveStream;
        }

        Stream IZMachineIO.OpenRestoreFile()
        {
            if (saveStream != null)
                return new MemoryStream(saveStream.ToArray());

            return null;
        }

        Stream IZMachineIO.OpenAuxiliaryFile(string name, int size, bool writing)
        {
            return null;
        }

        Stream IZMachineIO.OpenCommandFile(bool writing)
        {
            if (writing)
                return null;

            return inputStream;
        }

        void IZMachineIO.SetTextStyle(TextStyle style)
        {
            // nada
        }

        void IZMachineIO.SplitWindow(short lines)
        {
            // nada
        }

        void IZMachineIO.SelectWindow(short num)
        {
            // nada
        }

        void IZMachineIO.EraseWindow(short num)
        {
            // nada
        }

        void IZMachineIO.EraseLine()
        {
            // nada
        }

        void IZMachineIO.MoveCursor(short x, short y)
        {
            // nada
        }

        void IZMachineIO.GetCursorPos(out short x, out short y)
        {
            x = 1;
            y = 1;
        }

        void IZMachineIO.SetColors(short fg, short bg)
        {
            // nada
        }

        short IZMachineIO.SetFont(short num)
        {
            // not supported
            return 0;
        }

        bool IZMachineIO.GraphicsFontAvailable
        {
            get { return false; }
        }

        bool IZMachineIO.ForceFixedPitch
        {
            get { return true; }
            set { /* nada */ }
        }

        bool IZMachineIO.BoldAvailable
        {
            get { return false; }
        }

        bool IZMachineIO.ItalicAvailable
        {
            get { return false; }
        }

        bool IZMachineIO.FixedPitchAvailable
        {
            get { return false; }
        }

        bool IZMachineIO.VariablePitchAvailable
        {
            get { return false; }
        }

        bool IZMachineIO.ScrollFromBottom
        {
            get { return false; }
            set { /* nada */ }
        }

        bool IZMachineIO.TimedInputAvailable
        {
            get { return false; }
        }

        byte IZMachineIO.WidthChars
        {
            get { return 80; }
        }

        short IZMachineIO.WidthUnits
        {
            get { return 80; }
        }

        byte IZMachineIO.HeightChars
        {
            get { return 25; }
        }

        short IZMachineIO.HeightUnits
        {
            get { return 25; }
        }

        byte IZMachineIO.FontHeight
        {
            get { return 1; }
        }

        byte IZMachineIO.FontWidth
        {
            get { return 1; }
        }

        event EventHandler IZMachineIO.SizeChanged
        {
            add { /* nada */ }
            remove { /* nada */ }
        }

        bool IZMachineIO.ColorsAvailable
        {
            get { return false; }
        }

        byte IZMachineIO.DefaultForeground
        {
            get { return 9; }
        }

        byte IZMachineIO.DefaultBackground
        {
            get { return 2; }
        }

        void IZMachineIO.PlaySoundSample(ushort num, SoundAction action, byte volume, byte repeats,
            SoundFinishedCallback callback)
        {
            // nada
        }

        void IZMachineIO.PlayBeep(bool highPitch)
        {
            // nada
        }

        bool IZMachineIO.SoundSamplesAvailable
        {
            get { return false; }
        }

        bool IZMachineIO.Buffering
        {
            get { return false; }
            set { /* nada */ }
        }

        UnicodeCaps IZMachineIO.CheckUnicode(char ch)
        {
            return UnicodeCaps.CanPrint | UnicodeCaps.CanInput;
        }

        bool IZMachineIO.DrawCustomStatusLine(string location, short hoursOrScore, short minsOrTurns, bool useTime)
        {
            return !wantStatusLine;
        }

        #endregion
    }
}
