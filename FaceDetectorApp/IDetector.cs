using System.Drawing;

namespace FaceDetectorApp
{
    public interface IDetector
    {
        public string DetectorName { get; }

        public string ConfigPath { get; }

        public bool IsModelExists();

        public void DetectFace(ref Bitmap bitmap);                
    }
}
