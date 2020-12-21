using System;
using System.Collections.Generic;
using System.Drawing;

namespace DetectorHelper
{
    public interface IDetector : IDisposable
    {
        string DetectorName { get; }

        string ConfigPath { get; }

        Pen DrawPen { get; }

        bool IsModelExists();

        bool TryInitialize(bool raiseError = false);

        List<Face> DetectFaces(Bitmap bitmap);
    }
}
