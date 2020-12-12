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
        private readonly Action<bool> _updateDetectorButtonsEnableState;

        private Bitmap OriginalImage { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel viewModel = new ViewModel
            {
                GetImage = () => OriginalImage,
                SetImage = (newImage) => { imageBox.Source = BitmapToImageSource(newImage); },
                SetElapsedTime = (time) => { elapsedTime.Content = time; }
            };
            _updateDetectorButtonsEnableState = viewModel.UpdateDetectorButtonsEnableState;
            DataContext = viewModel;
        }

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                OriginalImage = new Bitmap(System.Drawing.Image.FromFile(openFileDialog.FileName));
                imageBox.Source = BitmapToImageSource(OriginalImage);
                _updateDetectorButtonsEnableState?.Invoke(true);
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
