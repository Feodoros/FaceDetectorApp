using System;
using System.Drawing;

namespace DetectorHelper
{
    public interface IDetector : IDisposable
    {        
        string DetectorName { get; }

        string ConfigPath { get; }

        bool IsModelExists();

        bool TryInitialize(bool raiseError = false);        

        void DetectFace(ref Bitmap bitmap);
    }
}
