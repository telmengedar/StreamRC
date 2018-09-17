namespace StreamRC.Cam {
    public class CameraDevice {
        public Platform Platform { get; set; }

        public string Device { get; set; }

        public string Display { get; set; }

        public override string ToString() {
            return $"{Display};{Platform}";
        }
    }
}