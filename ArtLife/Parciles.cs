using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using System.Linq;

namespace ArtLife;

public static class Config
{
    public static bool LINES_VISABILITY = true;
    public static bool LINES_VIEW_VISABILITY = false;
    
    public static bool GRAVITY = false;
    public static bool BOUNCE = false;
    
    public static double FOODS = 700;
    // public static double FOODS = 800;
    public static double CELLS = 70;
    public static double MAX_CELLS = 130;

    public static double CELL_DIVISION_PROP = 0.9;
    public static double CELL_CROSSING_PROP = 1;
    public static double MUTATION_RATE = 0.05;
    
    public static double FOOD_RADIUS = 7;
    public static double FOOD_ENERGY = 70;

    public static double DEFAULT_ENERGY = 17;
    public static double DIVISION_ENERGY_CONSUM = 18;
    public static double LIVING_ENERGY_CONSUM = 0.8;
    public static double ENERGY_MAX = 100;
    public static double CELL_RADIUS = 7;
    
    public static double BOUNCE_EFFECT = 0.7;
    public static double GRAVITY_FORCE = 0.2;
    public static double FRICTION_FORCE = 0.88;

    public static int HIDDEN_LAYER_NEURONS = 18;
    public static int INPUT_LAYER_NEURONS = 18;
    public static int OUTPUT_LAYER_NEURONS = 3;
    
    
    public static SolidColorBrush FOOD_COLOR = (SolidColorBrush)new BrushConverter().ConvertFrom("#26444D");
}

public abstract class Parcile : Dynamic
{
    public abstract double X { get; set; }
    public abstract double Y { get; set; }
    public abstract double Radius { get; set; }
}

public class Cell : Parcile
{
    
    public NeuralNetwork NN;
    public Ellipse ellipse { get; set; }
    public Line line { get; set; }
    public Line line2 { get; set; }
    
    public SolidColorBrush RGBColor;
    
    public double isWall;
    public double isBug;
    public double isFood;
    
    public double SpeedX { get; set; }
    public double SpeedY { get; set; }

    
    private double x;
    public override double X
    {
        get
        {
            return x;
        }
        set
        {
            x = value;
            UpdateXVisual();
        }
    }

    private double y;
    public override double Y
    {
        get
        {
            return y;
        }
        set
        {
            y = value;
            UpdateYVisual();
        }
    }
    
    private double radius;
    public override double Radius 
    {
        get
        {
            return radius;
        }
        set
        {
            radius = value;
            Update();
        }
    }

    private double energy;
    public double Energy
    {
        get
        {
            return energy;
        }
        set
        {
            if (value > Config.ENERGY_MAX)
            {
                energy = Config.ENERGY_MAX;
            }

            Radius = Config.CELL_RADIUS + 4 * (Math.Abs(energy) / Config.ENERGY_MAX);
            energy = value;
        }
    }
        
    private double angle;
    public double Angle
    {
        get
        {
            return angle;
        }
        set
        {
            angle = value;
        }
    }
    
    private double angleView;
    public double AngleView
    {
        get { return angleView; }
        set
        {
            angleView = value;
            UpdateLinesViewVisual();
        }
    }

    public double AngleView2 { get; set; }
    public double isWall2 { get; set; }
    public double isBug2 { get; set; }
    public double isFood2 { get; set; }
    
    public double AngleView3 { get; set; }
    public double isWall3 { get; set; }
    public double isBug3 { get; set; }
    public double isFood3 { get; set; }
    
    public Cell targerCell { get; set; }

    public Cell(Canvas canvas, double x, double y, double r, double angle, double speed, double energy, SolidColorBrush color)
    {
        NN = new NeuralNetwork(Config.INPUT_LAYER_NEURONS, Config.HIDDEN_LAYER_NEURONS, Config.HIDDEN_LAYER_NEURONS, Config.OUTPUT_LAYER_NEURONS);

        ellipse = new Ellipse();
        ellipse.Fill = color;
        UpdateRadiusVisual();
        canvas.Children.Add(ellipse);
                
        line = new Line();
        line.Stroke = Brushes.White;
        line.StrokeThickness = 1;
        canvas.Children.Add(line);
        
                        
        line2 = new Line();
        line2.Stroke = Brushes.White;
        line2.StrokeThickness = 1;
        canvas.Children.Add(line2);

        
        X = x;
        Y = y;
        Radius = r;
        RGBColor = color;
        Angle = angle;
        Energy = energy;
        AngleView = 0;
        
        SpeedX = Math.Cos(Angle)*speed;
        SpeedY = Math.Sin(Angle)*speed;
    }
    
    public double[] GetInput()
    {
        double av = AngleView;
        if (av == -1)
        {
            av = 10000;
        }
        if (av == 0){
            av = 1;
        }
        av = Math.Log(av) / 4;

        double av2 = AngleView2;
        if (av2 == -1)
        {
            av2 = 10000;
        }
        if (av2 == 0){
            av2 = 1;
        }
        av2 = Math.Log(av2) / 4;
        
        double av3 = AngleView3;
        if (av3 == -1)
        {
            av3 = 10000;
        }
        if (av3 == 0){
            av3 = 1;
        }
        av3 = Math.Log(av3) / 4;
        
        double[] input = {
            isWall,
            isBug,
            isFood,
            isWall2,
            isBug2,
            isFood2,
            isWall3,
            isBug3,
            isFood3,
            Angle/(Math.PI*2),
            Math.Sqrt(SpeedX * SpeedX + SpeedY * SpeedY)/30,
            av,
            av2,
            av3,
            Energy/100
        }; 
        // double[] input = { (X+2000)/4000, (Y+2000)/4000, Math.Sqrt(SpeedX * SpeedX + SpeedY * SpeedY)/30, Angle/(Math.PI*2), av}; 
        // double[] input = { X/2000, Y/2000, Math.Sqrt(SpeedX * SpeedX + SpeedY * SpeedY)/30, Angle/(Math.PI*2), AngleView/4000 }; 
        
        return input.Concat(NN.GetPastMove()).ToArray();
    }

    private void UpdateLinesViewVisual()
    {
        if (Config.LINES_VIEW_VISABILITY)
        {
            line.X1 = CConverter.FrameToPixel(x, CConverter.FieldFramePositionX);
            line.X2 = line.X1+CConverter.FramesToPixels(Math.Cos(GetSpeedAngle())*angleView);
            
            line.Y1 = CConverter.FrameToPixel(y, CConverter.FieldFramePositionY);
            line.Y2 = line.Y1+CConverter.FramesToPixels(Math.Sin(GetSpeedAngle())*angleView);
        }        
    }

    public double GetSpeedAngle()
    {
        return Math.Atan2(SpeedY, SpeedX);;
    }
    private void UpdateXVisual()
    {
        if (Config.LINES_VISABILITY)
        {
            line2.X1 = CConverter.FrameToPixel(x, CConverter.FieldFramePositionX);
            line2.X2 = line2.X1 + CConverter.FramesToPixels(SpeedX);
        }

        Canvas.SetLeft(ellipse, CConverter.FrameToPixel(x - Radius, CConverter.FieldFramePositionX));
    }
    private void UpdateYVisual()
    {
        if (Config.LINES_VISABILITY)
        {
            line2.Y1 = CConverter.FrameToPixel(y, CConverter.FieldFramePositionY); 
            line2.Y2 = line2.Y1+CConverter.FramesToPixels(SpeedY);
        }
        Canvas.SetTop(ellipse, CConverter.FrameToPixel(y - Radius, CConverter.FieldFramePositionY));
    }

    private void UpdateRadiusVisual()
    {
        ellipse.Width = CConverter.FramesToPixels(radius * 2);
        ellipse.Height = CConverter.FramesToPixels(radius * 2);
    }
    public override void Update()
    {
        UpdateRadiusVisual();
        UpdateXVisual();
        UpdateYVisual();
    }

    public (double, double, double, double) GetAngleView(double cellAngle, Cell cell, List<Parcile> parciles)
    {
        foreach (var otherParciles in parciles)
        {
            if (cell != otherParciles)
            {
                double angle = Math.Atan2(otherParciles.Y - cell.Y, otherParciles.X - cell.X);

                
                double angleDifference = Math.Abs(cellAngle - angle);
                if (angleDifference > Math.PI)
                {
                    angleDifference = 2 * Math.PI - angleDifference;
                }

                double distance = Math.Sqrt(Math.Pow(otherParciles.X - cell.X, 2) +
                                            Math.Pow(otherParciles.Y - cell.Y, 2));
                double Pdistance = otherParciles.Radius / (2 * Math.Sin(angleDifference / 2));

                if (distance < Pdistance)
                {
                    if (otherParciles is Cell)
                    {
                        targerCell = (Cell)otherParciles;
                        return (distance, 0, 1, 0);

                    }
                    if (otherParciles is Food)
                    {
                        return (distance, 0, 0, 1);
                    }
                }
            }
        }
        return (-1, 1, 0, 0);
    }
}
public class Food : Parcile
{
    public (byte, byte, byte) Color;
    public Ellipse ellipse { get; set; }
    
    private double x;
    public override double X
    {
        get
        {
            return x;
        }
        set
        {
            x = value;
            UpdateXVisual();
        }
    }

    private double y;
    public override double Y
    {
        get
        {
            return y;
        }
        set
        {
            y = value;
            UpdateYVisual();
        }
    }

    private double radius;
    public override double Radius 
    {
        get
        {
            return radius;
        }
        set
        {
            radius = value;
            Update();
        }
    }
    public Food(Canvas canvas, double x, double y, double r, SolidColorBrush color)
    {

        ellipse = new Ellipse();
        ellipse.Fill = color;
        UpdateRadiusVisual();
        canvas.Children.Add(ellipse);
        
        X = x;
        Y = y;
        Radius = r;
    }

    private void UpdateXVisual()
    {
        Canvas.SetLeft(ellipse, CConverter.FrameToPixel(x - Radius, CConverter.FieldFramePositionX));
    }
    private void UpdateYVisual()
    {
        Canvas.SetTop(ellipse, CConverter.FrameToPixel(y - Radius, CConverter.FieldFramePositionY));
    }

    private void UpdateRadiusVisual()
    {
        ellipse.Width = CConverter.FramesToPixels(radius * 2);
        ellipse.Height = CConverter.FramesToPixels(radius * 2);
    }
    public override void Update()
    {
        UpdateRadiusVisual();
        UpdateXVisual();
        UpdateYVisual();
    }
}

public class Border : Dynamic
{
    public Rectangle rectangle;
    
    private double x;
    public double X
    {
        get
        {
            return x;
        }
        set
        {
            x = value;
            Canvas.SetLeft(rectangle, CConverter.FrameToPixel(value-width/2, CConverter.FieldFramePositionX));
        }
    }

    private double y;
    public double Y
    {
        get
        {
            return y;
        }
        set
        {
            y = value;
            Canvas.SetTop(rectangle, CConverter.FrameToPixel(value-height/2, CConverter.FieldFramePositionY));
        }
    }
    private double width;
    public double Width
    {
        get
        {
            return width;
        }
        set
        {
            width = value;
            rectangle.Width = CConverter.FramesToPixels(width);
            Update();
        }
    }
    
    private double height;
    public double Height
    {
        get
        {
            return height;
        }
        set
        {
            height = value;
            rectangle.Height = CConverter.FramesToPixels(height);
            Update();
        }
    }

    private double thickness;
    public double Thickness
    {
        get
        {
            return thickness;
        }
        set
        {
            thickness = value;
            rectangle.StrokeThickness = CConverter.FramesToPixels(thickness);
        }
    }

    public Border(Canvas canvas, double x, double y, double width, double height)
    {
        rectangle = new Rectangle();

        rectangle.Fill = Brushes.Transparent;
        rectangle.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#152327");
        
        canvas.Children.Add(rectangle);

        X = x;
        Y = y;
        
        Thickness = 40;
        Width = width;
        Height = height;
    }
    public override void Update()
    {
        rectangle.Width = CConverter.FramesToPixels(width);
        rectangle.Height = CConverter.FramesToPixels(height);
        rectangle.StrokeThickness = CConverter.FramesToPixels(thickness);

        Canvas.SetLeft(rectangle, CConverter.FrameToPixel(x-width/2, CConverter.FieldFramePositionX));
        Canvas.SetTop(rectangle, CConverter.FrameToPixel(y-height/2, CConverter.FieldFramePositionY));
    }

    public bool isLeftBorder(double cx, double cy)
    {
        if (X - width / 2 + Thickness > cx)
        {
            return true;
        }

        return false;
    }
    public bool isRightBorder(double cx, double cy)
    {
        if (X + width / 2 - Thickness < cx)
        {
            return true;
        }

        return false;
    }
    public bool isTopBorder(double cx, double cy)
    {
        if (Y + height / 2 - Thickness < cy)
        {
            return true;
        }

        return false;
    }
    public bool isBottomBorder(double cx, double cy)
    {
        if (Y - height / 2 + Thickness > cy)
        {
            return true;
        }

        return false;
    }

}

public class CellDeath : Parcile
{
    public Ellipse ellipse { get; set; }
    
    private double x;

    public override double X
    {
        get { return x; }
        set
        {
            x = value;
            UpdateXVisual();
        }
    }

    private double y;

    public override double Y
    {
        get { return y; }
        set
        {
            y = value;
            UpdateYVisual();
        }
    }

    private double radius;

    public override double Radius
    {
        get { return radius; }
        set
        {
            radius = value;
            UpdateRadiusVisual();
        }
    }

    private Canvas canvas;
    private List<Dynamic> dynamicObjects;

    public CellDeath(Canvas canvas, List<Dynamic> dynamicObjects, double x, double y, SolidColorBrush color)
    {
        this.dynamicObjects = dynamicObjects;
        this.canvas = canvas;
        ellipse = new Ellipse();
        ellipse.Fill = color;
        UpdateRadiusVisual();
        canvas.Children.Add(ellipse);
        
        X = x;
        Y = y;
        Radius = Config.CELL_RADIUS;
    }

    private void UpdateXVisual()
    {
        Canvas.SetLeft(ellipse, CConverter.FrameToPixel(x - Radius, CConverter.FieldFramePositionX));
    }

    private void UpdateYVisual()
    {
        Canvas.SetTop(ellipse, CConverter.FrameToPixel(y - Radius, CConverter.FieldFramePositionY));
    }

    private void UpdateRadiusVisual()
    {
        ellipse.Width = CConverter.FramesToPixels(radius * 2);
        ellipse.Height = CConverter.FramesToPixels(radius * 2);
    }

    public override void Update()
    {
        Radius *= 2;
        UpdateXVisual();
        UpdateYVisual();
        if (Radius <= 0)
        {
            canvas.Children.Remove(ellipse);
            dynamicObjects.Remove(this);
        }
    }
}