using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceDetectorApp
{
    public class Config
    {
        // Path to congig directory
        private static readonly string configDirectory = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName, "Resources");

        public static string HaarPath => Path.Combine(configDirectory, "haarcascade_frontalface_alt.xml");
    }
}
