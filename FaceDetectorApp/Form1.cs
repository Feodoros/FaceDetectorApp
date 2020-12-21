using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DetectorHelper;
using Haar;
using RetinaDetector;
using SsdDetector;

namespace FaceDetectorApp
{
    public partial class Form1 : Form
    {
        private readonly List<RadioButton> detectorsButtons;
        private readonly List<Button> controlButtons;
        private readonly List<ButtonBase> allButtons;
        private IDetector detector;
        private Bitmap originalImage;

        public Form1()
        {
            InitializeComponent();

            btnAnalyze.Enabled = btnSave.Enabled = btnClear.Enabled = false;
            btnHaar.Enabled = btnSsd.Enabled = btnUltra.Enabled = btnCenter.Enabled = false;
            detectorsButtons = splitContainer1.Panel2.Controls.OfType<RadioButton>().ToList();
            controlButtons = splitContainer1.Panel2.Controls.OfType<Button>().ToList();
            allButtons = detectorsButtons.OfType<ButtonBase>().Concat(controlButtons.OfType<ButtonBase>()).ToList();
        }

        private void Btn_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = (RadioButton)sender;
            if (radioButton.Checked)
            {
                detector?.Dispose();
                try
                {
                    detector =
                        radioButton.Text switch
                        {
                            "Haar Cascades" => new HaarCascade(),
                            "SSD-MobileNet" => new SSD(),
                            /*"CenterFace" => HaarCascade.HaarDetect,*/
                            "UltraFace" => new UltraFace(),
                            _ => throw new Exception("Wrong method name."),
                        };

                    controlButtons.ForEach(btn =>
                    {
                        btn.Enabled = true;
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    controlButtons.ForEach(btn =>
                    {
                        btn.Enabled = false;
                    });

                    return;
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                originalImage = new Bitmap(Image.FromFile(openFileDialog1.FileName));
                pictureBox1.Image = originalImage;
            }
            btnHaar.Enabled = btnSsd.Enabled = btnUltra.Enabled = btnCenter.Enabled = true;
        }

        private void btnAnalyze_Click(object sender, EventArgs e)
        {
            allButtons.ForEach(btn =>
            {
                btn.Enabled = false;
            });

            Run();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = originalImage;
        }

        private async void Run()
        {
            if (!detector.TryInitialize())
            {
                MessageBox.Show($"{detector.DetectorName} detector initialization failed.");
                return;
            }

            Stopwatch stopwatch = new Stopwatch();
            Bitmap bitmap = new Bitmap(originalImage ?? pictureBox1.Image);

            try
            {
                stopwatch.Start();
                //await Task.Run(() => detector.DetectFace(ref bitmap));
                stopwatch.Stop();
                label1.Text = $"Elapsed Time: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} sec";
                pictureBox1.Image = new Bitmap(bitmap);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Detector {detector.DetectorName} is failed with error {ex.Message}");
                label1.Text = "Failed";                
                return;
            }
            finally
            {
                bitmap?.Dispose();
                allButtons.ForEach(btn =>
                {
                    btn.Enabled = true;
                });
            }
        }


        private void btnColorHaar_Click(object sender, EventArgs e)
        {
            ColorDialog MyDialog = new ColorDialog
            {
                // Keeps the user from selecting a custom color.
                AllowFullOpen = false,
                // Allows the user to get help. (The default is false.)
                CustomColors = new int[]{Convert.ToInt32($"{new Random().Next(0, 255)}{new Random().Next(0, 255)}{new Random().Next(0, 255)}"), 15195440, 16107657, 1836924,
   3758726, 12566463, 7526079, 7405793, 6945974, 241502, 2296476, 5130294,
   3102017, 7324121, 14993507, 11730944,},
                FullOpen = false,
                ShowHelp = false,
                SolidColorOnly = true
            };
            // Sets the initial color select to the current text color.
            //MyDialog.Color = textBox1.ForeColor;

            // Update the text box color if the user clicks OK 
            if (MyDialog.ShowDialog() == DialogResult.OK)
                btnColorHaar.BackColor = MyDialog.Color;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {

        }
    }
}
