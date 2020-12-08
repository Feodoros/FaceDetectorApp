using System;
using DetectorHelper;
using Haar;
using RetinaDetector;
using SsdDetector;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Drawing;
using System.Diagnostics;

namespace FaceDetectorWpf
{
    class ViewModel
    {
        public Action<bool> ToggleControlsButtons;
        public Action<bool> ToggleDetectorsButtons;
        public Action<bool> ToggleAllButtons;
        public Action<Bitmap> SetImage;

        public Func<Bitmap> GetImage;

        #region Fields
        private ICommand _chooseDetectorCommand;
        private ICommand _analyzeCommand;
        private IDetector _detector;
        #endregion

        public ICommand ChooseDetectorCommand
        {
            get
            {
                if (_chooseDetectorCommand == null)
                {
                    _chooseDetectorCommand = new RelayCommand(SetDetector, CanExecuteMethod);
                }
                return _chooseDetectorCommand;
            }
        }

        public ICommand AnalyzeCommand
        {
            get
            {
                if (_analyzeCommand == null)
                {
                    _analyzeCommand = new RelayCommand(RunAsync, o => true);
                }
                return _analyzeCommand;
            }
        }

        private void SetDetector(object parameter)
        {
            string name = (string)parameter;
            if (name.Equals(string.Empty))
            {
                return;
            }

            _detector?.Dispose();
            try
            {
                switch (name)
                {
                    case "btnHaar": _detector = new HaarCascade(); break;
                    case "btnSsd": _detector = new SSD(); break;
                    case "btnRetina": _detector = new UltraFace(); break;
                    case "btnCenter": _detector = new HaarCascade(); break;
                    default:
                        throw new Exception("Wrong method name.");
                }
                ToggleControlsButtons?.Invoke(true);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private bool CanExecuteMethod(object parameter)
        {
            if (parameter != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async void RunAsync(object parameter)
        {
            if (!_detector.TryInitialize())
            {
                //MessageBox.Show($"{detector.DetectorName} detector initialization failed.");
                return;
            }

            Stopwatch stopwatch = new Stopwatch();
            Bitmap bitmap = new Bitmap(GetImage?.Invoke());

            try
            {
                stopwatch.Start();
                await Task.Run(() => _detector.DetectFace(ref bitmap));
                stopwatch.Stop();
                //label1.Text = $"Elapsed Time: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} sec";
                SetImage(new Bitmap(bitmap));
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Detector {detector.DetectorName} is failed with error {ex.Message}");
                //label1.Text = "Failed";
                return;
            }
            finally
            {
                bitmap?.Dispose();
                /*allButtons.ForEach(btn =>
                {
                    btn.Enabled = true;
                });*/
            }
        }
    }
}
