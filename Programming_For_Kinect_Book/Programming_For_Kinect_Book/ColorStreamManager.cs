using System.Windows.Media.Imaging;
using System.Windows.Media;
using Microsoft.Kinect;
using System.Windows;
using Programming_For_Kinect_Book;

public class ColorStreamManager : Notifier
{
    public WriteableBitmap Bitmap { get; private set; }
    
    public void Update(ColorImageFrame frame)
    {
        var pixelData = new byte[frame.PixelDataLength];
        
        frame.CopyPixelDataTo(pixelData);
        
        if (Bitmap == null)
        {
            Bitmap = new WriteableBitmap(frame.Width, frame.Height,
                                            96, 96, PixelFormats.Bgr32, null);
        }
        
        int stride = Bitmap.PixelWidth * Bitmap.Format.BitsPerPixel / 8;
        Int32Rect dirtyRect = new Int32Rect(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight);
        Bitmap.WritePixels(dirtyRect, pixelData, stride, 0);
        
        RaisePropertyChanged(() => Bitmap);
    }
}