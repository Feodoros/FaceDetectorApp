using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;

namespace FaceDetectorApp
{
    class HaarCascade
    {
        public static Bitmap HaarDetect(string imagePath)
        {
            CascadeClassifier classifierFace = new CascadeClassifier(Config.HaarPath);

            using Image<Gray, byte> image = new Image<Gray, byte>(imagePath);

            Rectangle[] faces = classifierFace.DetectMultiScale(image, 1.1, 4);

            using Image<Bgr, byte> newImage = new Image<Bgr, byte>(imagePath);

            foreach (var face in faces)
            {
                newImage.Draw(face, new Bgr(0, 255, 0), 2);
            }
            return newImage.AsBitmap();
        }
    }
}
