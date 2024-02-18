/* Copyright 2010-2023 Tara McGrew
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

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZLR.VM;

namespace Zilf.Tests.Integration
{
    abstract class TestCaseIO
    {
        protected readonly StringBuilder outputBuffer = new();

        public string CollectOutput()
        {
            string result = outputBuffer.ToString();
            outputBuffer.Length = 0;
            return result;
        }
    }

    sealed class ReplayIO(Stream prevInputStream, bool wantStatusLine = false) : TestCaseIO, IAsyncZMachineIO, IDisposable
    {
        MemoryStream? saveStream;

        #region Z-machine I/O implementation

        ReadLineResult IZMachineIO.ReadLine(string initial, int time, TimedInputCallback callback, byte[] terminatingKeys, bool allowDebuggerBreak)
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
            get => false;
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

        Stream? IZMachineIO.OpenRestoreFile()
        {
            return saveStream != null ? new MemoryStream(saveStream.ToArray()) : null;
        }

        Stream? IZMachineIO.OpenAuxiliaryFile(string name, int size, bool writing)
        {
            return null;
        }

        Stream? IZMachineIO.OpenCommandFile(bool writing)
        {
            return writing ? null : prevInputStream;
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

        bool IZMachineIO.GraphicsFontAvailable => false;

        bool IZMachineIO.ForceFixedPitch
        {
            get => true;
            set { /* nada */ }
        }

        bool IZMachineIO.BoldAvailable => false;

        bool IZMachineIO.ItalicAvailable => false;

        bool IZMachineIO.FixedPitchAvailable => false;

        bool IZMachineIO.VariablePitchAvailable => false;

        bool IZMachineIO.ScrollFromBottom
        {
            get => false;
            set { /* nada */ }
        }

        bool IZMachineIO.TimedInputAvailable => false;

        byte IZMachineIO.WidthChars => 80;

        short IZMachineIO.WidthUnits => 80;

        byte IZMachineIO.HeightChars => 25;

        short IZMachineIO.HeightUnits => 25;

        byte IZMachineIO.FontHeight => 1;

        byte IZMachineIO.FontWidth => 1;

        event EventHandler IZMachineIO.SizeChanged
        {
            add { /* nada */ }
            remove { /* nada */ }
        }

        bool IZMachineIO.ColorsAvailable => false;

        byte IZMachineIO.DefaultForeground => 9;

        byte IZMachineIO.DefaultBackground => 2;

        void IZMachineIO.PlaySoundSample(ushort num, SoundAction action, byte volume, byte repeats,
            SoundFinishedCallback callback)
        {
            // nada
        }

        void IZMachineIO.PlayBeep(bool highPitch)
        {
            // nada
        }

        bool IZMachineIO.SoundSamplesAvailable => false;

        bool IZMachineIO.Buffering
        {
            get => false;
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

        Task<ReadLineResult> IAsyncZMachineIO.ReadLineAsync(string initial, byte[] terminatingKeys, bool allowDebuggerBreak, CancellationToken cancellationToken)
        {
            return Task.FromException<ReadLineResult>(new AssertFailedException("Unexpected line input request"));
        }

        Task<short> IAsyncZMachineIO.ReadKeyAsync(CharTranslator translator, CancellationToken cancellationToken)
        {
            return Task.FromException<short>(new AssertFailedException("Unexpected character input request"));
        }

        Task<Stream?> IAsyncZMachineIO.OpenSaveFileAsync(int size, CancellationToken cancellationToken)
        {
            saveStream = new MemoryStream();
            return Task.FromResult<Stream?>(saveStream);
        }

        Task<Stream?> IAsyncZMachineIO.OpenRestoreFileAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream?>(saveStream != null ? new MemoryStream(saveStream.ToArray()) : null);
        }

        Task<Stream?> IAsyncZMachineIO.OpenAuxiliaryFileAsync(string name, int size, bool writing, CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream?>(null);
        }

        Task<Stream?> IAsyncZMachineIO.OpenCommandFileAsync(bool writing, CancellationToken cancellationToken)
        {
            return Task.FromResult(writing ? null : prevInputStream);
        }

        #endregion

        public void Dispose()
        {
            prevInputStream.Dispose();
            saveStream?.Dispose();
        }
    }
}
