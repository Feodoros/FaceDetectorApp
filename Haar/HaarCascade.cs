using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using DetectorHelper;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Haar
{
    public sealed class HaarCascade : IDetector
    {
        private readonly Pen _pen = new Pen(Color.FromArgb(0, 255, 0), 3);
        private CascadeClassifier classifierFace;

        public string DetectorName => "HaarCascade";

        public string ConfigPath => Config.HaarPath;

        public Pen DrawPen => _pen;

        public bool IsModelExists() => File.Exists(ConfigPath);

        public List<Face> DetectFaces(Bitmap bitmap)
        {
            List<Face> faces = new List<Face>();
            using Image<Gray, byte> emguImage = bitmap.ToImage<Gray, byte>();
            Rectangle[] detectedFaces = classifierFace.DetectMultiScale(emguImage, 1.1, 4);

            foreach (Rectangle face in detectedFaces)
            {
                faces.Add(new Face(face.X, face.Top, face.Width, face.Height, DrawPen));
            }
            return faces;
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
