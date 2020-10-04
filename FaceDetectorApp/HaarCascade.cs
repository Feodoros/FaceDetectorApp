using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;
using System.IO;

namespace FaceDetectorApp
{
    public sealed class HaarCascade : IDetector
    {
        public string DetectorName => "HaarCascade";

        public string ConfigPath => Config.HaarPath;

        public bool IsModelExists() => File.Exists(ConfigPath);

        public void DetectFace(ref Bitmap bitmap)
        {
            CascadeClassifier classifierFace = new CascadeClassifier(ConfigPath);

            using Image<Gray, byte> emguImage = bitmap.ToImage<Gray, byte>();
            Rectangle[] faces = classifierFace.DetectMultiScale(emguImage, 1.1, 4);

            using Image<Bgr, byte> colorImage = bitmap.ToImage<Bgr, byte>();
            foreach (Rectangle face in faces)
            {
                colorImage.Draw(face, new Bgr(0, 255, 0), 2);
            }
            bitmap = colorImage.AsBitmap();
        }
    }
}
