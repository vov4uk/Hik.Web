namespace DetectPeople.Face.Retina
{
    public class FaceNetRecognitionResult
    {
        public FaceNetRecognitionResult(FaceRecognitionResult found, FaceRecognitionResult best, double? maxDistance)
        {
            Found = found;
            Best = best;
            MaxDistance = maxDistance;
        }

        public FaceRecognitionResult Best { get; set; }
        public FaceRecognitionResult Found { get; set; }
        public double? MaxDistance { get; set; }
    }
}
