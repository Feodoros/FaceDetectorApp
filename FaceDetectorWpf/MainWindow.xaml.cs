using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.UI.Xaml.Controls.Primitives;

namespace FaceDetectorWpf
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ViewModel _viewModel;
        private readonly List<RadioButton> _detectorsButtons = new List<RadioButton>() { };
        private readonly List<Button> _controlButtons = new List<Button>() { };
        private List<ButtonBase> _allButtons = new List<ButtonBase>() { };

        private Bitmap OriginalImage { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            UniteButtons();
            ToggleControlsButtons(false);

            _viewModel = new ViewModel
            {
                ToggleControlsButtons = ToggleControlsButtons,
                ToggleDetectorsButtons = ToggleDetectorsButtons,
                ToggleAllButtons = ToggleAllButtons,
                GetImage = () => OriginalImage,
                SetImage = (newImage) => { imageBox.Source = BitmapToImageSource(newImage); }
            };
            DataContext = _viewModel;
        }

        #region Buttons
        private void UniteButtons()
        {
            _detectorsButtons.Add(btnHaar);
            _detectorsButtons.Add(btnSsd);
            _detectorsButtons.Add(btnRetina);
            _detectorsButtons.Add(btnCenter);

            _controlButtons.Add(btnAnalyze);
            _controlButtons.Add(btnSave);
            _controlButtons.Add(btnClear);

            _allButtons = _detectorsButtons.OfType<ButtonBase>().Concat(_controlButtons.OfType<ButtonBase>()).ToList();
        }

        private void ToggleDetectorsButtons(bool enabled)
        {
            foreach (RadioButton button in _detectorsButtons)
            {
                button.IsEnabled = enabled;
            }
        }

        private void ToggleControlsButtons(bool enabled)
        {
            foreach (Button button in _controlButtons)
            {
                button.IsEnabled = enabled;
            }
        }

        private void ToggleAllButtons(bool enabled)
        {
            foreach (ButtonBase button in _allButtons)
            {
                button.IsEnabled = enabled;
            }
        }
        #endregion

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                OriginalImage = new Bitmap(System.Drawing.Image.FromFile(openFileDialog.FileName));
                imageBox.Source = BitmapToImageSource(OriginalImage);
            }
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
    }
}
