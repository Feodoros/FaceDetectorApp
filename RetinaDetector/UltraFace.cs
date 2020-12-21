using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using DetectorHelper;
using NcnnDotNet;

namespace RetinaDetector
{
    /// <summary>
    /// Provides the method to find face methods. This class cannot be inherited.
    /// </summary>
    public sealed class UltraFace : DisposableObject, IDetector
    {
        #region Fields

        private const float CenterVariance = 0.1f;
        private const float SizeVariance = 0.2f;
        private const int NumFeatureMap = 4;
        private const float NormalTolerance = 0.8f;

        private readonly float[] _Strides = { 8.0f, 16.0f, 32.0f, 64.0f };
        private readonly float[] _MeanVals = { 127f, 127f, 127f };
        private readonly float[] _NormVals = { (float)(1.0 / 128), (float)(1.0 / 128), (float)(1.0 / 128) };
        private readonly float[][] _MinBoxes =
        {
            new[]{10.0f,  16.0f,  24.0f},
            new[]{32.0f,  48.0f},
            new[]{64.0f,  96.0f},
            new[]{128.0f, 192.0f, 256.0f}
        };

        private int _NumThread;
        private int _InW;
        private int _InH;
        private int _NumAnchors;
        private int _TopK;
        private float _ScoreThreshold;
        private float _IouThreshold;
        private readonly IList<float[]> _FeatureMapSize = new List<float[]>();
        private readonly IList<float[]> _ShrinkageSize = new List<float[]>();
        private readonly IList<float[]> _Priors = new List<float[]>();

        private Net _UltraFace;
        private UltraFaceParameter _ultraFaceParameter;
        private readonly Pen _pen = new Pen(Color.FromArgb(255, 20, 200), 3);

        private int _ImageW;
        private int _ImageH;

        public string DetectorName => "UltraFace";

        public string ConfigPath => Config.RetinaBinPath;

        public Pen DrawPen => _pen;

        #endregion

        #region Methods

        /// <summary>
        /// Returns an enumerable collection of face location correspond to all faces in specified image.
        /// </summary>
        /// <param name="image">The image contains faces. The image can contain multiple faces.</param>
        /// <returns>An enumerable collection of face location correspond to all faces in specified image.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="image"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException"><paramref name="image"/> or this object is disposed.</exception>
        public IEnumerable<FaceInfo> Detect(Mat image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            this.ThrowIfDisposed();
            image.ThrowIfDisposed();

            List<FaceInfo> faceList = new List<FaceInfo>();
            if (Detect(image, faceList) != 0)
            {
                throw new ArgumentException("Image is empty.", nameof(image));
            }

            return faceList.ToArray();
        }

        #region Overrides 

        /// <summary>
        /// Releases all unmanaged resources.
        /// </summary>
        protected override void DisposeUnmanaged()
        {
            base.DisposeUnmanaged();

            this._UltraFace?.Dispose();
        }

        #endregion

        #region Helpers

        private static float Clip(double x, float y)
        {
            return (float)(x < 0 ? 0 : x > y ? y : x);
        }

        private int Detect(Mat image, ICollection<FaceInfo> faceList)
        {
            if (image.IsEmpty)
            {
                Console.WriteLine("image is empty ,please check!");
                return -1;
            }

            this._ImageH = image.H;
            this._ImageW = image.W;

            using (Mat @in = new Mat())
            {
                Ncnn.ResizeBilinear(image, @in, this._InW, this._InH);

                Mat ncnnImg = @in;
                ncnnImg.SubstractMeanNormalize(this._MeanVals, this._NormVals);

                List<FaceInfo> boundingBoxCollection = new List<FaceInfo>();

                using (Extractor ex = this._UltraFace.CreateExtractor())
                {
                    ex.SetNumThreads(this._NumThread);
                    ex.Input("input", ncnnImg);

                    using (Mat scores = new Mat())
                    {
                        using (Mat boxes = new Mat())
                        {
                            ex.Extract("scores", scores);
                            ex.Extract("boxes", boxes);

                            GenerateBBox(boundingBoxCollection, scores, boxes, this._ScoreThreshold, this._NumAnchors);
                            NonMaximumSuppression(boundingBoxCollection, faceList);
                        }
                    }
                }
            }

            return 0;
        }

        private void GenerateBBox(ICollection<FaceInfo> boundingBoxCollection, Mat scores, Mat boxes, float scoreThreshold, int numAnchors)
        {
            using (Mat scoresChannel = scores.Channel(0))
            {
                using (Mat boxesChannel = boxes.Channel(0))
                {
                    for (int i = 0; i < numAnchors; i++)
                        if (scoresChannel[i * 2 + 1] > scoreThreshold)
                        {
                            FaceInfo rects = new FaceInfo();
                            float xCenter = boxesChannel[i * 4] * CenterVariance * this._Priors[i][2] + this._Priors[i][0];
                            float yCenter = boxesChannel[i * 4 + 1] * CenterVariance * this._Priors[i][3] + this._Priors[i][1];
                            double w = Math.Exp(boxesChannel[i * 4 + 2] * SizeVariance) * this._Priors[i][2];
                            double h = Math.Exp(boxesChannel[i * 4 + 3] * SizeVariance) * this._Priors[i][3];

                            rects.X1 = Clip(xCenter - w / 2.0, 1) * this._ImageW;
                            rects.Y1 = Clip(yCenter - h / 2.0, 1) * this._ImageH;
                            rects.X2 = Clip(xCenter + w / 2.0, 1) * this._ImageW;
                            rects.Y2 = Clip(yCenter + h / 2.0, 1) * this._ImageH;
                            rects.Score = Clip(scoresChannel[i * 2 + 1], 1);

                            boundingBoxCollection.Add(rects);
                        }
                }
            }
        }

        private void NonMaximumSuppression(List<FaceInfo> input, ICollection<FaceInfo> output, NonMaximumSuppressionMode type = NonMaximumSuppressionMode.Blending)
        {
            input.Sort((f1, f2) => f1.Score.CompareTo(f2.Score));

            int boxNum = input.Count;

            int[] merged = new int[boxNum];

            for (int i = 0; i < boxNum; i++)
            {
                if (merged[i] > 0)
                {
                    continue;
                }

                List<FaceInfo> buf = new List<FaceInfo>
                {
                    input[i]
                };

                merged[i] = 1;

                float h0 = input[i].Y2 - input[i].Y1 + 1;
                float w0 = input[i].X2 - input[i].X1 + 1;

                float area0 = h0 * w0;

                for (int j = i + 1; j < boxNum; j++)
                {
                    if (merged[j] > 0)
                    {
                        continue;
                    }

                    float innerX0 = input[i].X1 > input[j].X1 ? input[i].X1 : input[j].X1;
                    float innerY0 = input[i].Y1 > input[j].Y1 ? input[i].Y1 : input[j].Y1;

                    float innerX1 = input[i].X2 < input[j].X2 ? input[i].X2 : input[j].X2;
                    float innerY1 = input[i].Y2 < input[j].Y2 ? input[i].Y2 : input[j].Y2;

                    float innerH = innerY1 - innerY0 + 1;
                    float innerW = innerX1 - innerX0 + 1;

                    if (innerH <= 0 || innerW <= 0)
                    {
                        continue;
                    }

                    var innerArea = innerH * innerW;

                    var h1 = input[j].Y2 - input[j].Y1 + 1;
                    var w1 = input[j].X2 - input[j].X1 + 1;

                    var area1 = h1 * w1;

                    var score = innerArea / (area0 + area1 - innerArea);

                    if (score > this._IouThreshold)
                    {
                        merged[j] = 1;
                        buf.Add(input[j]);
                    }
                }

                switch (type)
                {
                    case NonMaximumSuppressionMode.Hard:
                        {
                            output.Add(buf[0]);
                            break;
                        }
                    case NonMaximumSuppressionMode.Blending:
                        {
                            double total = 0d;
                            for (int j = 0; j < buf.Count; j++)
                            {
                                total += Math.Exp(buf[j].Score);
                            }

                            FaceInfo rects = new FaceInfo();
                            for (int j = 0; j < buf.Count; j++)
                            {
                                double rate = Math.Exp(buf[j].Score) / total;
                                rects.X1 += (float)(buf[j].X1 * rate);
                                rects.Y1 += (float)(buf[j].Y1 * rate);
                                rects.X2 += (float)(buf[j].X2 * rate);
                                rects.Y2 += (float)(buf[j].Y2 * rate);
                                rects.Score += (float)(buf[j].Score * rate);
                            }

                            output.Add(rects);
                            break;
                        }
                    default:
                        //{
                        //    Console.WriteLine("wrong type of nms.");
                        //    exit(-1);
                        //}
                        break;
                }
            }

            #endregion

            #endregion
        }

        public bool IsModelExists() => File.Exists(ConfigPath) && File.Exists(Config.RetinaParamPath);

        public bool TryInitialize(bool raiseError = false)
        {
            if (_ultraFaceParameter != null)
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
                // Need to test params for possible better work
                _ultraFaceParameter = new UltraFaceParameter
                {
                    BinFilePath = ConfigPath,
                    ParamFilePath = Config.RetinaParamPath,
                    InputWidth = 320,
                    InputLength = 240,
                    NumThread = 4,
                    ScoreThreshold = NormalTolerance,
                };
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

            try
            {
                _NumThread = _ultraFaceParameter.NumThread;
                _TopK = _ultraFaceParameter.TopK;
                _ScoreThreshold = _ultraFaceParameter.ScoreThreshold;
                _IouThreshold = _ultraFaceParameter.IouThreshold;
                _InW = _ultraFaceParameter.InputWidth;
                _InH = _ultraFaceParameter.InputLength;

                float[] whList = new float[] { _ultraFaceParameter.InputWidth, _ultraFaceParameter.InputLength };

                foreach (var size in whList)
                {
                    float[] featureMapItem = _Strides.Select(stride => (float)Math.Ceiling(size / stride)).ToArray();
                    _FeatureMapSize.Add(featureMapItem);
                }

                foreach (var _ in whList)
                {
                    _ShrinkageSize.Add(_Strides);
                }

                for (int index = 0; index < NumFeatureMap; index++)
                {
                    float scaleW = _InW / _ShrinkageSize[0][index];
                    float scaleH = _InH / _ShrinkageSize[1][index];
                    for (int j = 0; j < _FeatureMapSize[1][index]; j++)
                    {
                        for (int i = 0; i < _FeatureMapSize[0][index]; i++)
                        {
                            float xCenter = (float)((i + 0.5) / scaleW);
                            float yCenter = (float)((j + 0.5) / scaleH);

                            foreach (float k in _MinBoxes[index])
                            {
                                float w = k / _InW;
                                float h = k / _InH;

                                _Priors.Add(
                                    new[]
                                    {
                                Clip(xCenter, 1),
                                Clip(yCenter, 1),
                                Clip(w, 1),
                                Clip(h, 1)
                                    });
                            }
                        }
                    }
                }

                _NumAnchors = _Priors.Count;

                _UltraFace = new Net();
                _UltraFace.LoadParam(_ultraFaceParameter.ParamFilePath);
                _UltraFace.LoadModel(_ultraFaceParameter.BinFilePath);
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

            return true;
        }

        public List<Face> DetectFaces(Bitmap bitmap)
        {
            List<Face> faces = new List<Face>();
            using Image tempImage = new Bitmap(bitmap);
            string tempFilePath = Path.Combine(Environment.CurrentDirectory, "tmp.jpg");
            tempImage.Save(tempFilePath);

            using NcnnDotNet.OpenCV.Mat frame = NcnnDotNet.OpenCV.Cv2.ImRead(tempFilePath);
            using Mat inMat = Mat.FromPixels(frame.Data, PixelType.Bgr2Rgb, frame.Cols, frame.Rows);
            FaceInfo[] faceInfos = Detect(inMat).ToArray();
            File.Delete(tempFilePath);

            foreach (FaceInfo detectedFace in faceInfos)
            {
                faces.Add(new Face((int)detectedFace.X1,
                                   (int)detectedFace.Y1,
                                   (int)detectedFace.X2 - (int)detectedFace.X1,
                                   (int)detectedFace.Y2 - (int)detectedFace.Y1,
                                   _pen));
            }
            return faces;
        }
    }
}
