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
using System.Threading;

namespace FaceDetectorWpf
{
    public class ViewModel : INotifyPropertyChanged
    {
        // Detected faces by some of detectors
        public class AllDetectedFaces
        {
            public List<Face> HaarFaces { get; set; }
            public List<Face> SsdFaces { get; set; }
            public List<Face> RetinaFaces { get; set; }
            //List<Face> RetinaFaces { get; set; }
        }

        public Action<Bitmap> SetImage;
        public Func<Bitmap> GetOriginalImage;
        public Func<Bitmap> GetCurrentImage;
        public Func<string> GetSavingPath;
        public Action<string> SetElapsedTime;
        public delegate void ControlHandler();
        public event ControlHandler ClearImageBoxEvent;
        public event PropertyChangedEventHandler PropertyChanged;

        #region Fields
        private ICommand _chooseDetectorCommand;
        private ICommand _analyzeCommand;
        private ICommand _clearCommand;
        private ICommand _saveCommand;
        private ICommand _checkDetectorCommand;
        private IDetector _detector;
        private bool _isPictureSet;
        private bool _isAnalyzingNow;
        private bool _isHaarCheckedToDraw;
        private bool _isSsdCheckedToDraw;
        private bool _isRetinaCheckedToDraw;
        private bool _isCenterCheckedToDraw;
        private List<Face> _currentDetectedFaces;
        private AllDetectedFaces _allDetected;
        private bool _canDrawRetina;
        private bool _canDrawSsd;
        private bool _canDrawHaar;
        #endregion

        #region Properties
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
                    _analyzeCommand = new RelayCommand(o => Analyze(), o => true);
                }
                return _analyzeCommand;
            }
        }

        public ICommand ClearCommand
        {
            get
            {
                if (_clearCommand == null)
                {
                    _clearCommand = new RelayCommand(o => Clear(), o => true);
                }
                return _clearCommand;
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new RelayCommand(o => SaveImage(), o => true);
                }
                return _saveCommand;
            }
        }

        public ICommand CheckDetectorCommand
        {
            get
            {
                if (_checkDetectorCommand == null)
                {
                    _checkDetectorCommand = new RelayCommand(o => PickDetector(), CanExecuteMethod);
                }
                return _checkDetectorCommand;
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

        public bool NeedHaarDrawing
        {
            get
            {
                return _isHaarCheckedToDraw;
            }
            set
            {
                _isHaarCheckedToDraw = value;
                RaisePropertyChanged("NeedHaarDrawing");
            }
        }

        public bool NeedSsdDrawing
        {
            get
            {
                return _isSsdCheckedToDraw;
            }
            set
            {
                _isSsdCheckedToDraw = value;
                RaisePropertyChanged("NeedSsdDrawing");
            }
        }

        public bool NeedRetinaDrawing
        {
            get
            {
                return _isRetinaCheckedToDraw;
            }
            set
            {
                _isRetinaCheckedToDraw = value;
                RaisePropertyChanged("NeedRetinaDrawing");
            }
        }

        public bool CanDrawHaar
        {
            get
            {
                return _canDrawHaar;
            }
            set
            {
                _canDrawHaar = value;
                RaisePropertyChanged("CanDrawHaar");
            }
        }

        public bool CanDrawSsd
        {
            get
            {
                return _canDrawSsd;
            }
            set
            {
                _canDrawSsd = value;
                RaisePropertyChanged("CanDrawSsd");
            }
        }

        public bool CanDrawRetina
        {
            get
            {
                return _canDrawRetina;
            }
            set
            {
                _canDrawRetina = value;
                RaisePropertyChanged("CanDrawRetina");
            }
        }
        #endregion        

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

        private void PickDetector()
        {
            using (Bitmap bitmap = new Bitmap(GetOriginalImage?.Invoke()))
            {
                List<Face> faces = new List<Face>();
                if (NeedHaarDrawing)
                {
                    if (_allDetected?.HaarFaces != null)
                    {
                        faces.AddRange(_allDetected?.HaarFaces);
                        CanDrawHaar = true;
                    }
                }
                if (NeedSsdDrawing)
                {
                    if (_allDetected?.SsdFaces != null)
                    {
                        faces.AddRange(_allDetected?.SsdFaces);
                        CanDrawSsd = true;
                    }
                }
                if (NeedRetinaDrawing)
                {
                    if (_allDetected?.RetinaFaces != null)
                    {
                        faces.AddRange(_allDetected?.RetinaFaces);
                        CanDrawRetina = true;
                    }
                }
                DrawFacesOnImage(bitmap, faces);
                SetImage?.Invoke(bitmap);
            }
        }

        private double Detect(Bitmap bitmap)
        {
            double elapsedTime = 0;
            if (_allDetected == null)
            {
                _allDetected = new AllDetectedFaces();
            }

            if (_detector == null)
            {
                return elapsedTime;
            }

            ToggleButtonsEnableState(false);
            if (!_detector.TryInitialize())
            {
                ToggleButtonsEnableState(true);
                return elapsedTime;
            }

            Stopwatch stopwatch = new Stopwatch();
            try
            {
                _currentDetectedFaces = new List<Face>();
                stopwatch.Start();
                _currentDetectedFaces = _detector.DetectFaces(new Bitmap(bitmap));
                stopwatch.Stop();
                elapsedTime = stopwatch.Elapsed.Seconds + stopwatch.Elapsed.Milliseconds / 1000.00;
                switch (_detector.DetectorName)
                {
                    case "HaarCascade": { _allDetected.HaarFaces = _currentDetectedFaces; NeedHaarDrawing = true; } break;
                    case "Single-Shot": { _allDetected.SsdFaces = _currentDetectedFaces; NeedSsdDrawing = true; } break;
                    case "UltraFace": { _allDetected.RetinaFaces = _currentDetectedFaces; NeedRetinaDrawing = true; } break;
                    default: break;
                }
            }
            catch (Exception ex)
            {
                // TaskLogger in future instead Console
                Console.WriteLine(ex.Message);
                return elapsedTime;
            }
            finally
            {
                ToggleButtonsEnableState(true);
            }
            return elapsedTime;
        }

        private async void Analyze()
        {
            using (Bitmap bitmap = new Bitmap(GetOriginalImage?.Invoke()))
            {
                double time = 0;
                string timeMessage = "";

                await Task.Run(() =>
                {
                    time = Detect(bitmap);
                });

                if (time == 0)
                {
                    timeMessage = "Failed";
                }
                else
                {
                    timeMessage = $"Elapsed time: {string.Format("{0:0.00}", time)}";
                }
                SetElapsedTime?.Invoke(timeMessage);

                PickDetector();
            }
        }

        private void SaveImage()
        {
            using (Bitmap bitmap = new Bitmap(GetCurrentImage?.Invoke()))
            {
                string path = GetSavingPath?.Invoke();
                if (bitmap == null || path.Equals(string.Empty))
                {
                    return;
                }
                bitmap.Save(path);
            }
        }

        private void DrawFacesOnImage(Bitmap image, List<Face> faces)
        {
            foreach (Face face in faces)
            {
                Graphics g = Graphics.FromImage(image);
                g.DrawRectangle(face.Pen, face.X1, face.Y1, face.X2, face.Y2);
            }
        }

        public void UpdateDetectorButtonsEnableState(bool isEnabled)
        {
            IsDetectorButtonsEnabled = isEnabled;
        }

        private void ToggleButtonsEnableState(bool isEnabled)
        {
            IsControlButtonsEnabled = isEnabled;
            IsDetectorButtonsEnabled = isEnabled;
        }

        private void Clear()
        {
            ClearImageBoxEvent?.Invoke();
            NeedHaarDrawing = false;
            NeedSsdDrawing = false;
            NeedRetinaDrawing = false;
        }

        /// <summary>
        /// User have chosen new pic
        /// </summary>
        public void ChangePicture()
        {
            NeedHaarDrawing = NeedRetinaDrawing = NeedSsdDrawing = false;
            CanDrawHaar = CanDrawRetina = CanDrawSsd = false;
            SetElapsedTime?.Invoke("Elapsed time:");
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
    }
}
