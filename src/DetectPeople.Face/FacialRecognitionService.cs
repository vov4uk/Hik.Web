using DetectPeople.Face.Helpers;
using FaceRecognitionDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace DetectPeople.Face
{
    public class NoFaceFoundException : Exception
    {
    }

    public class Face
    {
        public string FullPath { get; set; }
        public string Name { get; set; }
        public FaceEncoding Encoding { get; set; }
    }

    public class SearchResult
    {
        public FaceEncoding Encoding { get; set; }
        public Location FaceLocation { get; set; }
        public List<Result> Matches { get; set; }
    }

    public class Result
    {
        public string ImagePath { get; set; }
        public string Name { get; set; }
        public double Confidence { get; set; }
    }

    public class FacialRecognitionService
    {
        private List<Face> _faces;
        private static FaceRecognition _faceRecognition;

        public void Initialize()
        {
            _faceRecognition = FaceRecognition.Create(PathHelper.ModelsFolder());
            EncodeDataSet(PathHelper.ImagesFolder());
            _faces = DeserializeFaces(PathHelper.ImagesFolder()).ToList();
        }

        public List<SearchResult> Search(string filePath)
        {
            return Search(PathHelper.ImagesFolder(), filePath, _faces);
        }

        private static double Curve(double confidence)
        {
            return Math.Sqrt(confidence);
        }

        public void AddFace(Face face)
        {
            if (face != null)
            {
                _faces.Add(face);
                SaveFaceEncoding(face);
            }
            else
            {
                Console.WriteLine($"Face is null {face.FullPath}");
            }
        }

        public static List<SearchResult> Search(string imagesFolder, string path, List<Face> images = null)
        {
            FaceRecognition.InternalEncoding = System.Text.Encoding.UTF8;

            using (Image imageB = FaceRecognition.LoadImageFile(path, Mode.Greyscale))
            {
                if (images == null)
                    images = DeserializeFaces(imagesFolder).Where(i => i.Name != imagesFolder).ToList();

                var faceLocations = _faceRecognition.FaceLocations(imageB, 0, Model.Hog)
                        .OrderByDescending(l => (l.Right - l.Left) * (l.Bottom - l.Top));
                if (faceLocations.Any() == false)
                    throw new NoFaceFoundException();

                var result = new List<SearchResult>();

                var knownFaces = images.Select(i => i.Encoding);

                foreach (var faceLocation in faceLocations)
                {
                    var faceEncoding = _faceRecognition.FaceEncodings(imageB, new[] { faceLocation }).First();

                    var distances = FaceRecognition.FaceDistances(knownFaces, faceEncoding).ToList();

                    //faceEncoding.Dispose();

                    var results = distances.Select((d, i) => new Result
                    {
                        Confidence = Curve(1 - d),
                        ImagePath = images[i].FullPath,
                        Name = images[i].Name
                    }).OrderByDescending(i => i.Confidence).ToList();

                    result.Add(new SearchResult
                    {
                        FaceLocation = faceLocation,
                        Matches = results,
                        Encoding = faceEncoding
                    });
                }

                return result;
            }
        }


        public static void EncodeDataSet(string imagesFolder)
        {
            var images = GetImages(imagesFolder);
            foreach (var image in images)
            {
                SaveFaceEncoding(image);
            }
        }

        public static IEnumerable<Face> DeserializeFaces(string folder)
        {
            var files = Directory.EnumerateFiles(folder, "*.fe", SearchOption.AllDirectories);
            var serializer = new BinaryFormatter();

            foreach (var file in files)
            {
                var parts = file.Split(new char[] { '\\', '/' });
                var dir = parts[parts.Length - 2];

                using (var stream = File.OpenRead(file))
                {
                    yield return new Face
                    {
                        Encoding = (FaceEncoding)serializer.Deserialize(stream),
                        FullPath = file.Replace(".fe", ""),
                        Name = dir
                    };
                }
            }
        }

        public static IEnumerable<Face> GetImages(string folder)
        {
            var files = Directory.EnumerateFiles(folder, "*.png", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var face = GetFace(file);
                if (face != null)
                {
                    yield return face;
                }
            }
        }

        public static Face GetFace(string file)
        {
            var parts = file.Split(new char[] { '\\', '/' });
            var dir = parts[parts.Length - 2];
            var fileDirectory = Path.GetDirectoryName(file);
            var fileName = Path.GetFileName(file);
            var faceEncodingFile = Path.Combine(fileDirectory, fileName + ".fe");
            if (File.Exists(faceEncodingFile))
            {
                return null;
            }

            try
            {
                using (var image = FaceRecognition.LoadImageFile(file, Mode.Greyscale))
                {
                    var locations = _faceRecognition.FaceLocations(image);
                    var face = _faceRecognition.FaceEncodings(image, locations).FirstOrDefault();

                    if (face is null) return null;

                    return new Face
                    {
                        FullPath = file,
                        Encoding = face,
                        Name = dir,
                    };
                }
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public static void SaveFaceEncoding(Face image)
        {
            var name = Path.GetFileName(image.FullPath);
            var path = Path.Combine(Path.GetDirectoryName(image.FullPath), name + ".fe");
            using (var file = File.OpenWrite(path))
            {
                file.SetLength(0);
                var formatter = new BinaryFormatter();
                formatter.Serialize(file, image.Encoding);
            }
        }
    }
}
