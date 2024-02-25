namespace ArtLife;

public static class CConverter
{
    public static double FieldFrameWidth = 6000;
    public static double FieldFramePositionX = -3000;
    public static double FieldFramePositionY = -2000;
    public static double CanvasWidth = 1;

    public static double FramesToPixels(double frames)
    {
        var framesPerPixel = FieldFrameWidth / CanvasWidth;

        return frames / framesPerPixel;
    }

    public static double FrameToPixel(double frame, double position)
    {
        var frames = frame - position;

        var framesPerPixel = FieldFrameWidth / CanvasWidth;

        return frames / framesPerPixel;
    }

    public static double PixelToFrame(double pixel, double position)
    {
        var framesPerPixel = FieldFrameWidth / CanvasWidth;

        return position + pixel * framesPerPixel;
    }

    public static double PixelsToFrames(double pixels)
    {
        var framesPerPixel = FieldFrameWidth / CanvasWidth;

        return pixels * framesPerPixel;
    }
}

