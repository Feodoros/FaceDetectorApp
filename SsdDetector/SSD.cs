using System.Drawing;
using System.IO;
using System;
using Tensorflow;
using static Tensorflow.Binding;
using NumSharp;
using FaceDetectorApp;

namespace SsdDetector
{
    public sealed class SSD : IDetector
    {
        private Graph Graph;
        private Graph PreprocessingGraph;
        private Session Session;
        private Session PreprocessingSession;

        private Tensor[] OutTensorArr;
        private Tensor PreprocessInput;
        private Tensor PreprocessOutput;
        private Tensor ImgTensor;

        private readonly double tolerance = 0.5;

        public string DetectorName => "Single-Shot";

        public string ConfigPath => Config.SsdPath;

        private bool Initialize()
        {
            Graph = LoadModelGraph();

            Tensor tensorNum = Graph.OperationByName("num_detections");
            Tensor tensorBoxes = Graph.OperationByName("detection_boxes");
            Tensor tensorScores = Graph.OperationByName("detection_scores");
            Tensor tensorClasses = Graph.OperationByName("detection_classes");
            ImgTensor = Graph.OperationByName("image_tensor");
            OutTensorArr = new Tensor[] { tensorNum, tensorBoxes, tensorScores, tensorClasses };


            PreprocessingGraph = BuildPreprocGraph();

            PreprocessInput = PreprocessingGraph.get_operation_by_name("input");
            PreprocessOutput = PreprocessingGraph.get_operation_by_name("output");

            Session = tf.Session(Graph);
            PreprocessingSession = tf.Session(PreprocessingGraph);

            return (Graph != null) && (PreprocessingGraph != null);
        }

        private Graph LoadModelGraph()
        {
            Graph graph = new Graph().as_default();
            graph.Import(ConfigPath);

            return graph;
        }

        private Graph BuildPreprocGraph()
        {
            Tensor name_placeholder = tf.placeholder(TF_DataType.TF_STRING, name: "input");
            Tensor file_reader = tf.read_file(name_placeholder, "file_reader");
            Tensor decodeJpeg = tf.image.decode_jpeg(file_reader, channels: 3, name: "DecodeJpeg");
            Tensor casted = tf.cast(decodeJpeg, TF_DataType.TF_UINT8);
            Tensor dims_expander = tf.expand_dims(casted, 0, name: "output");

            return dims_expander.graph;
        }

        public bool IsModelExists() => File.Exists(ConfigPath);

        public void DetectFace(ref Bitmap bitmap)
        {
            Initialize();

            int cols = bitmap.Width;
            int rows = bitmap.Height;

            PreprocessingGraph.as_default();
            using Image tempImage = new Bitmap(bitmap);
            string tempFilePath = Path.Combine(Environment.CurrentDirectory, "tmp.jpg");
            tempImage.Save(tempFilePath);
            FeedItem feed = new FeedItem(PreprocessInput, tempFilePath);
            NDArray arrayFromImage = PreprocessingSession.run(PreprocessOutput, feed);

            File.Delete(tempFilePath);

            Graph.as_default();
            NDArray[] results = Session.run(OutTensorArr, new FeedItem(ImgTensor, arrayFromImage));

            var scores = results[2].AsIterator<float>();
            var boxes = results[1].GetData<float>();
            var id = np.squeeze(results[3]).GetData<float>();

            using Graphics g = Graphics.FromImage(bitmap);
            Pen pen = new Pen(Color.FromArgb(0, 255, 255), 2);

            for (int i = 0; i < scores.size; i++)
            {
                float score = scores.MoveNext();
                if (score > tolerance)
                {
                    Rectangle rectangle = Rectangle.FromLTRB(Convert.ToInt32(boxes[i * 4 + 1] * cols), Convert.ToInt32(boxes[i * 4] * rows),
                                                             Convert.ToInt32(boxes[i * 4 + 3] * cols), Convert.ToInt32(boxes[i * 4 + 2] * rows));
                    g.DrawRectangle(pen, rectangle);
                }
            }
        }

        private void Dispose()
        {
            Graph?.Dispose();
            Graph = null;

            PreprocessingGraph?.Dispose();
            PreprocessingGraph = null;

            Session?.close();
            Session?.Dispose();
            Session = null;

            PreprocessingSession?.close();
            PreprocessingSession?.Dispose();
            PreprocessingSession = null;

            ImgTensor?.Dispose();
            ImgTensor = null;

            PreprocessInput?.Dispose();
            PreprocessInput = null;

            PreprocessOutput?.Dispose();
            PreprocessOutput = null;

            OutTensorArr = null;
        }

        public bool TryInitialize()
        {
            throw new NotImplementedException();
        }        

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
