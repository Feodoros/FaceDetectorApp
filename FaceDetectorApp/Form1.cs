using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FaceDetectorApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            btnAnalyze.Enabled = false;
            btnSave.Enabled = false;

        }

        private void Btn_CheckedChanged(object sender, EventArgs e)
        {            
            RadioButton radioButton = (RadioButton)sender;
            if (radioButton.Checked)
            {
                MessageBox.Show("Вы выбрали " + radioButton.Text);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pictureBox1.Image = Image.FromFile(openFileDialog1.FileName);
            }
            btnAnalyze.Enabled = true;
            btnSave.Enabled = true;
        }
    }
}
