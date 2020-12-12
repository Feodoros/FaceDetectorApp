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
    class ViewModel : INotifyPropertyChanged
    {
        public Action<Bitmap> SetImage;
        public Func<Bitmap> GetImage;
        public Action<string> SetElapsedTime;
        public event PropertyChangedEventHandler PropertyChanged;

        #region Fields
        private ICommand _chooseDetectorCommand;
        private ICommand _analyzeCommand;
        private IDetector _detector;
        private bool _isPictureSet;
        private bool _isAnalyzingNow;

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

        public bool IsDetectorButtonsEnabled
        {
            get
            {
                return _isPictureSet;
            }
            private set
            {
                _isPictureSet = value;
                RaisePropertyChanged("IsDetectorButtonsEnabled");
            }
        }

        public bool IsControlButtonsEnabled
        {
            get
            {
                return _isAnalyzingNow;
            }
            private set
            {
                _isAnalyzingNow = value;
                RaisePropertyChanged("IsControlButtonsEnabled");
            }
        }

        public void UpdateDetectorButtonsEnableState(bool isEnabled)
        {
            IsDetectorButtonsEnabled = isEnabled;
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
                IsControlButtonsEnabled = true;
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

        private void RaisePropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        private async void RunAsync(object parameter)
        {
            if (_detector == null)
            {
                return;
            }

            ToggleButtonsEnableState(false);
            if (!_detector.TryInitialize())
            {
                ToggleButtonsEnableState(true);
                return;
            }

            Stopwatch stopwatch = new Stopwatch();
            Bitmap bitmap = new Bitmap(GetImage?.Invoke());

            try
            {
                stopwatch.Start();
                await Task.Run(() => _detector.DetectFace(ref bitmap));
                stopwatch.Stop();
                SetElapsedTime?.Invoke($"Elapsed time: {string.Format("{0:0.00}", stopwatch.Elapsed.Seconds + stopwatch.Elapsed.Milliseconds / 1000.00)}");
                SetImage?.Invoke(new Bitmap(bitmap));
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
                ToggleButtonsEnableState(true);
                /*allButtons.ForEach(btn =>
                {
                    btn.Enabled = true;
                });*/
            }
        }

        private void ToggleButtonsEnableState(bool isEnabled)
        {
            IsControlButtonsEnabled = isEnabled;
            IsDetectorButtonsEnabled = isEnabled;
        }
    }
}
