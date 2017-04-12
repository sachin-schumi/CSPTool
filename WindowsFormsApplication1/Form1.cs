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
        public Form1()
        {
            //InitializeComponent();
            button1_Click(null,null);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CSPGenerator c = new CSPGenerator();
            System.IO.StreamReader sr = new
                   System.IO.StreamReader(@"C:\FSDT\input3.txt");
            String str = sr.ReadToEnd();
            sr.Close();
            bool flag = c.checkInput(str);
            if (flag == true)
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = @"C:\Program Files\Process Analysis Toolkit\Process Analysis Toolkit 3.5.1\PAT 3.exe";
                process.StartInfo.Arguments = @"C:\FSDT\file.csp";
                process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                process.Start();
            }
            else
                MessageBox.Show("Check syntax", "Parser");
           
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
            CSPGenerator c = new CSPGenerator();
            bool flag = c.checkInput(s);
            if(true == flag)
                MessageBox.Show("Valid CSP","Parser");
            else
                MessageBox.Show("Check syntax", "Parser");
        }
    }
}
