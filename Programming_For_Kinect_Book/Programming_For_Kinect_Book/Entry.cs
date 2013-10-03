using System;
using System.Windows.Shapes;

namespace Programming_For_Kinect_Book
{
    public class Entry
    {
        public DateTime Time { get; set; }
        public Vector3 Position { get; set; }
        public Ellipse DisplayEllipse { get; set; }
    }
}
