﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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

            btnAnalyze.Enabled = btnSave.Enabled = false;
            btnHaar.Enabled = btnSsd.Enabled = btnUltra.Enabled = btnCenter.Enabled = false;
        }
        
        private IDetector detector;

        private void Btn_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = (RadioButton)sender;
            if (radioButton.Checked)
            {
                try
                {
                    detector =
                        radioButton.Text switch
                        {
                            "Haar Cascades" => new HaarCascade(),
                            /*"SSD-MobileNet" => HaarCascade.HaarDetect,
                            "CenterFace" => HaarCascade.HaarDetect,
                            "UltraFace" => HaarCascade.HaarDetect,*/
                            _ => throw new Exception("Wrong method name."),
                        };

                    btnAnalyze.Enabled = true;
                    btnSave.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    btnAnalyze.Enabled = btnSave.Enabled = false;
                    return;
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pictureBox1.Image = Image.FromFile(openFileDialog1.FileName);
            }
            btnHaar.Enabled = btnSsd.Enabled = btnUltra.Enabled = btnCenter.Enabled = true;
        }

        private void btnAnalyze_Click(object sender, EventArgs e)
        {
            Run();
        }

        private async void Run()
        {
            if (!detector.IsModelExists())
            {
                MessageBox.Show($"{detector.DetectorName} detector's model is not found.");
                return;
            }

            Stopwatch stopwatch = new Stopwatch();
            Bitmap bitmap = new Bitmap(pictureBox1.Image);

            try
            {
                stopwatch.Start();
                await Task.Run(() => detector.DetectFace(ref bitmap));
                stopwatch.Stop();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Detector {detector.DetectorName} is failed with error {ex.Message}");
                return;
            }
            finally
            {
                bitmap?.Dispose();
            }

            label1.Text = $"Elapsed Time: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} sec";
            pictureBox1.Image = bitmap;
        }
    }
}
