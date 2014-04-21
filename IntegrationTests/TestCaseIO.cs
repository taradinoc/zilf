using System;
using System.Collections.Generic;
using System.Text;
using ZLR.VM;
using System.IO;

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
        private readonly Queue<string> inputBuffer = new Queue<string>();
        private readonly Stream inputStream;

        public ReplayIO(Stream prevInputStream)
        {
            this.inputStream = prevInputStream;
        }

        #region Z-machine I/O implementation

        string IZMachineIO.ReadLine(string initial, int time, TimedInputCallback callback,
            byte[] terminatingKeys, out byte terminator)
        {
            terminator = 13;
            return inputBuffer.Dequeue();
        }

        void IZMachineIO.PutCommand(string command)
        {
            // nada
        }

        short IZMachineIO.ReadKey(int time, TimedInputCallback callback, CharTranslator translator)
        {
            string inputLine;
            do { inputLine = inputBuffer.Dequeue(); } while (inputLine.Length == 0);
            return translator(inputLine[0]);
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
            return null;
        }

        Stream IZMachineIO.OpenRestoreFile()
        {
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
            return false;
        }

        #endregion
    }
}
