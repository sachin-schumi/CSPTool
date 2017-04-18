using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;


namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        String s;
        bool parsed = false;
        public Form1()
        {
            InitializeComponent();
            //button1_Click(null, null);
        }

        private void showError(Error e)
        {
            MessageBox.Show(e.Message, "Parser");
        }

        private Error addAttacker(String filename,String processName)
        {
            int lastDotIndex = filename.LastIndexOf('.');
            string newFile = filename.Substring(0, lastDotIndex) + "_full" + filename.Substring(lastDotIndex);
            CSPModel model = new CSPModel(filename, newFile);
            Error e = model.ToFullModel(processName.Trim());
            if (!(e.IsError))
            {
                e = model.Verify();
            }
            else
            {
                showError(e);
            }
            return e;
        }

        private void button1_Click(object sender, EventArgs e)
        {

            /*
            System.IO.StreamReader sr = new
                   System.IO.StreamReader(@"C:\FSDT\input3.txt");
            s = sr.ReadToEnd();

            sr.Close();
            */

            CSPGenerator c = new CSPGenerator();
            Error ex = new Error(0, false);
            if (String.IsNullOrWhiteSpace(s))
            {
                ex.IsError = true;
                ex.Message = "Select input file";
                showError(ex);
            }
            else
            {
                ex = c.checkInput(s);
                if (ex.IsError == false)
                {
                    ex = addAttacker(@"C:\FSDT\file.csp", "Protocol");
                    if (ex.IsError == false)
                    {
                        System.Diagnostics.Process process = new System.Diagnostics.Process();
                        process.StartInfo.Verb = "runas";
                        process.StartInfo.FileName = @"C:\Program Files\Process Analysis Toolkit\Process Analysis Toolkit 3.5.1\PAT 3.exe";
                        process.StartInfo.Arguments = @"C:\FSDT\file.csp";
                        process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                        process.Start();
                    }
                    else
                        showError(ex);
                }
                else
                {
                    ex.Message = "Check syntax at line " + ex.LineNumber;
                    showError(ex);
                }
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
                System.IO.StreamReader sr = new
                   System.IO.StreamReader(openFileDialog1.FileName);
                s = sr.ReadToEnd();

                sr.Close();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Error ex = new Error(0, false);
            CSPGenerator c = new CSPGenerator();
            if(String.IsNullOrWhiteSpace(s))
            {
                ex.IsError = true;
                ex.Message = "Select input file";
                showError(ex);
            }
            else
            {
                ex = c.checkInput(s);
                if (ex.IsError == true)
                    ex.Message = "Check syntax at line " + ex.LineNumber;
                else
                    ex.Message = "Valid CSP";
                showError(ex);
            }
        }
    }
}