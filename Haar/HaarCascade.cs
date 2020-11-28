using DetectorHelper;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.IO;

namespace Haar
{
    public sealed class HaarCascade : IDetector
    {
        private CascadeClassifier classifierFace;        

        public string DetectorName => "HaarCascade";

        public string ConfigPath => Config.HaarPath;

        public bool IsModelExists() => File.Exists(ConfigPath);

        public void DetectFace(ref Bitmap bitmap)
        {
            using Image<Gray, byte> emguImage = bitmap.ToImage<Gray, byte>();
            Rectangle[] faces = classifierFace.DetectMultiScale(emguImage, 1.1, 4);

            using Image<Bgr, byte> colorImage = bitmap.ToImage<Bgr, byte>();
            foreach (Rectangle face in faces)
            {
                colorImage.Draw(face, new Bgr(0, 255, 0), 2);
            }
            bitmap = colorImage.AsBitmap();
        }

        public bool TryInitialize(bool raiseError = false)
        {
            if (classifierFace != null)
            {
                return true;
            }

            if (!IsModelExists())
            {
                string message = $"Could not find the model of {DetectorName} detector";

                if (raiseError)
                {
                    throw new Exception(message);
                }
                // TaskLogger in future instead Console
                Console.WriteLine(message);
                return false;
            }

            try
            {
                classifierFace = new CascadeClassifier(ConfigPath);
                return true;
            }
            catch (Exception ex)
            {
                string message = $"{DetectorName} detector initialization failed with Exception {ex.Message}";

                if (raiseError)
                {
                    throw new Exception(message);
                }
                // TaskLogger in future instead Console
                Console.WriteLine(message);
                return false;
            }
        }

        public void Dispose()
        {
            classifierFace?.Dispose();
        }
    }
}
