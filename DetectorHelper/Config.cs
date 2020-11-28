using System.IO;

namespace DetectorHelper
{
    public class Config
    {
        // Path to congig directory

        private static readonly string configDirectory = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, "Resources");

        public static string HaarPath => Path.Combine(configDirectory, "haarcascade_frontalface_alt.xml");

        public static string SsdPath => Path.Combine(configDirectory, "frozen_inference_graph.pb");
    }
}
