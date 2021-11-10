namespace DetectPeople.Face.Retina
{
    public class HeadPoseDetectorResult
    {
        public HeadPoseDetectorResult(int[][] axis, float yaw, float pitch, float roll)
        {
            Axis = axis;
            this.Yaw = yaw;
            this.Pitch = pitch;
            this.Roll = roll;
        }

        public int[][] Axis { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public float Roll { get; set; }
    }
}
