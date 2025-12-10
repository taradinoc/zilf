using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

using FyreVM;
using Zifmia.FyreVM.Service;

namespace WinFyre
{
    public partial class MainForm : Form
    {
        private EngineWrapper wrapper;


        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream file = assembly.GetManifestResourceStream("WinFyre.Games.FyreVMTester.ulx");
            byte[] buffer = new byte[file.Length];
            int result = file.Read(buffer, 0, (int)file.Length);
            MemoryStream fileData = new MemoryStream(buffer);

            wrapper = new EngineWrapper(buffer);

            AddOutput();
        }

        private void AddOutput()
        {
            OutputText.AppendText(wrapper.ToJSON);
            OutputText.AppendText(Environment.NewLine);
            OutputText.AppendText(Environment.NewLine);
        }

        private void GoButton_Click(object sender, EventArgs e)
        {
            wrapper.SendCommand(CommandLine.Text);
            CommandLine.Text = "";
            AddOutput();
            CommandLine.Focus();
        }
    }
}
