using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using FyreVM;

namespace SimpleFyre
{
    public partial class MainForm : Form
    {
        private Engine vm;
        private Thread vmThread;
        private Stream gameFile;

        private AutoResetEvent inputReadyEvent = new AutoResetEvent(false);
        private bool lineWanted = false;
        private bool keyWanted = false;
        private string inputLine = null;
        private string[] channelText = new string[18];

        public MainForm()
        {
            InitializeComponent();

            lblPrompt.Text = "";
            txtInput.Bounds = inputPanel.ClientRectangle;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutSimpleFyreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("SimpleFyre\nA FyreVM sample application.");
        }

        private void openGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string filename;

            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Game File";
                dlg.Filter = "Glulx files (*.ulx)|*.ulx";

                if (dlg.ShowDialog() == DialogResult.Cancel)
                    return;

                filename = dlg.FileName;
            }

            if (vmThread != null)
            {
                vmThread.Abort();
                vmThread.Join();
            }

            if (gameFile != null)
                gameFile.Close();

            txtInput.Clear();
            txtLocation.Clear();
            txtTime.Clear();
            txtScore.Clear();

            gameFile = new FileStream(filename, FileMode.Open, FileAccess.Read);

            vm = new Engine(gameFile);
            //vm.OutputFilterEnabled = false;

            vm.LineWanted += new LineWantedEventHandler(vm_LineWanted);
            vm.KeyWanted += new KeyWantedEventHandler(vm_KeyWanted);
            vm.OutputReady += new OutputReadyEventHandler(vm_OutputReady);
            vm.SaveRequested += new SaveRestoreEventHandler(vm_SaveRequested);
            vm.LoadRequested += new SaveRestoreEventHandler(vm_LoadRequested);

            vmThread = new Thread(VMThreadProc);
            vmThread.IsBackground = true;
            vmThread.Start();
        }

        private void RequestLine()
        {
            lineWanted = true;
            keyWanted = false;

            txtInput.Enabled = true;
            txtInput.Focus();
        }

        private void RequestKey()
        {
            lineWanted = false;
            keyWanted = true;

            txtInput.Enabled = true;
            txtInput.Focus();
        }

        private void txtInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (keyWanted)
            {
                GotInput(new string(e.KeyChar, 1));
                e.Handled = true;
            }
            else if (e.KeyChar == '\r' || e.KeyChar == '\n')
            {
                // already handled in KeyDown
                e.Handled = true;
            }
        }

        private void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
            {
                e.Handled = true;

                if (keyWanted)
                {
                    GotInput("\n");
                }
                else if (lineWanted)
                {
                    GotInput(txtInput.Text);
                    //txtOutput.Text += lblPrompt.Text + txtInput.Text + "\r\n";
                    txtInput.Clear();
                }
            }
        }

        private void GotInput(string line)
        {
            txtInput.Enabled = false;
            inputLine = line;
            inputReadyEvent.Set();
        }

        public class Channel
        {
            public Channel(string name, string data)
            {
                this.ChannelName = name;
                this.ChannelData = data;
            }

            public string ChannelName { get; set; }
            public string ChannelData { get; set; }
        }

        private void HandleOutput(Dictionary<string, string> package)
        {
            List<Channel> channels = new List<Channel>();

            foreach (string key in package.Keys)
            {
                channels.Add(new Channel(key, package[key]));
            }

            dataGridView1.DataSource = channels;
        }

        private void ArrangeInput(object sender, EventArgs e)
        {
            txtInput.Left = lblPrompt.Right + 2;
            txtInput.Width = inputPanel.ClientSize.Width - txtInput.Left;
        }

        private Stream RequestSaveStream()
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "Select Save File";
                dlg.Filter = "Quetzal save files (*.sav)|*.sav";

                if (dlg.ShowDialog() == DialogResult.Cancel)
                    return null;
                else
                    return new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write);
            }
        }

        private Stream RequestLoadStream()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Load a Saved Game";
                dlg.Filter = "Quetzal save files (*.sav)|*.sav";

                if (dlg.ShowDialog() == DialogResult.Cancel)
                    return null;
                else
                    return new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read);
            }
        }

        #region VM Thread

        private void VMThreadProc(object dummy)
        {
            try
            {
                vm.Run();
                this.Invoke(new Action(GameFinished));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void vm_LineWanted(object sender, LineWantedEventArgs e)
        {
            this.Invoke(new Action(RequestLine));
            inputReadyEvent.WaitOne();
            e.Line = inputLine;
        }

        private void vm_KeyWanted(object sender, KeyWantedEventArgs e)
        {
            this.Invoke(new Action(RequestKey));
            inputReadyEvent.WaitOne();
            e.Char = inputLine[0];
        }

        private void vm_OutputReady(object sender, OutputReadyEventArgs e)
        {
            this.Invoke(
                new Action<Dictionary<string, string>>(HandleOutput),
                e.Package);
        }

        private void vm_SaveRequested(object sender, SaveRestoreEventArgs e)
        {
            e.Stream = (Stream)this.Invoke(new Func<Stream>(RequestSaveStream));
        }

        private void vm_LoadRequested(object sender, SaveRestoreEventArgs e)
        {
            e.Stream = (Stream)this.Invoke(new Func<Stream>(RequestLoadStream));
        }

        #endregion

        private void GameFinished()
        {
            this.Close();
        }

    }
}
