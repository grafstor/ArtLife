using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MathNet.Numerics.LinearAlgebra;

namespace ArtLife;

public abstract class Static
{
    public abstract void Update();
}

public class Text : Static
{
    private TextBlock textBlock;
    private Canvas canvas;

    public double X { get; set; }
    public double Y { get; set; }

    public Text(Canvas canvas, double x, double y, string text)
    {
        X = x;
        Y = y;

        this.canvas = canvas;

        textBlock = new TextBlock();
        textBlock.Text = text;
        textBlock.Opacity = 0.74;
        textBlock.FontSize = 12; // Установите размер шрифта
        textBlock.Foreground = Brushes.White; // Установите цвет текста
        textBlock.FontFamily = new FontFamily("Inter"); // Установите семейство шрифта

        Update();

        canvas.Children.Add(textBlock);
    }

    public override void Update()
    {
        Canvas.SetLeft(textBlock, (X > 0) ? X : X + canvas.ActualWidth); // Установите координату X
        Canvas.SetTop(textBlock, (Y > 0) ? Y : Y + canvas.ActualHeight); // Установите координату Y
    }

    public string GetText()
    {
        return textBlock.Text;
    }

    public void UpdateText(string text)
    {
        textBlock.Text = text;
    }
}

public partial class MainWindow : Window
{
    private double bounceEffect = Config.BOUNCE_EFFECT;
    private double scale = 0.07;

    private List<Cell> cells;
    private List<Food> foods;
    private List<Dynamic> dynamicObjects;
    private List<Static> staticObjects;
    private Border border;

    private List<Cell> cellsCopy;
    private List<Cell> newCells;
    private List<Cell> removedCells;

    private bool isDragging;
    private Point curentMouseDownPoint;
    private double fieldStartPositionX;
    private double fieldStartPositionY;

    private CancellationTokenSource cancellationTokenSource;

    private Text textCells;
    private Text textScale;
    private Text textFps;
    private Text textFood;

    private int frameCount = 0;
    private int foodCount = 0;
    private DateTime lastFrameTime = DateTime.Now;

    private Random random;

    public MainWindow()
    {
        InitializeComponent();
        cells = new List<Cell>();
        foods = new List<Food>();

        random = new Random();

        dynamicObjects = new List<Dynamic>();
        staticObjects = new List<Static>();

        cellsCopy = new List<Cell>();
        newCells = new List<Cell>();
        removedCells = new List<Cell>();

        Loaded += Canvas_Loaded;
        canvas.PreviewMouseWheel += OnCavasMouseWheel;
        canvas.MouseDown += OnCanvasMouseDown;
        canvas.MouseUp += OnCanvasMouseUp;
        canvas.MouseMove += OnCanvasMouseMove;
        canvas.SizeChanged += delegate { CConverter.CanvasWidth = canvas.ActualWidth; };

        cancellationTokenSource = new CancellationTokenSource();
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (staticObjects.Count > 0)
        {
            foreach (var staticObject in staticObjects)
            {
                staticObject.Update();
            }
        }
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        var canvas = (Canvas)sender;
        var position = e.GetPosition(canvas);

        if (isDragging)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                var deltaX = position.X - curentMouseDownPoint.X;
                CConverter.FieldFramePositionX = fieldStartPositionX - CConverter.PixelsToFrames(deltaX);

                var deltaY = position.Y - curentMouseDownPoint.Y;
                CConverter.FieldFramePositionY = fieldStartPositionY - CConverter.PixelsToFrames(deltaY);
            }
        }

        foreach (var dynamicObject in dynamicObjects)
        {
            dynamicObject.Update();
        }
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        var canvas = (Canvas)sender;
        isDragging = false;
        canvas.Cursor = Cursors.Arrow;
        e.Handled = true;
    }

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        isDragging = true;

        curentMouseDownPoint = e.GetPosition(canvas);

        if (!Keyboard.IsKeyDown(Key.LeftCtrl))
        {
            fieldStartPositionX = CConverter.FieldFramePositionX;
            fieldStartPositionY = CConverter.FieldFramePositionY;
            canvas.Cursor = Cursors.Hand;
        }

        e.Handled = true;
    }

    private void OnCavasMouseWheel(object sender, MouseWheelEventArgs e)
    {
        double newScale = e.Delta > 0 ? 1.3 : 1 / 1.3;

        scale *= newScale;
        CConverter.FieldFramePositionX += CConverter.FieldFrameWidth * (1 - newScale) / 2;
        CConverter.FieldFramePositionY += CConverter.FieldFrameWidth * (1 - newScale) / 4;
        CConverter.FieldFrameWidth *= newScale;

        foreach (var dynamicObject in dynamicObjects)
        {
            dynamicObject.Update();
        }

        textScale.UpdateText($"{Math.Round(1 / scale, 2)}x");
    }

    private void Canvas_Loaded(object sender, RoutedEventArgs e)
    {
        DrawBorders();
        DrawCells();
        DrawFood();

        DrawText();
        StartGame();
    }

    private void StartGame()
    {
        DispatcherTimer gameTimer = new DispatcherTimer();
        gameTimer.Interval = TimeSpan.FromMilliseconds(1); // 60 FPS
        gameTimer.Tick += UpdateGame;
        cellsCopy = new List<Cell>(cells);
        gameTimer.Start();
    }

    private (double, double) CalculateGravity(Cell cell, List<Cell> cellsCopy)
    {
        double dx = 0;
        double dy = 0;

        foreach (Cell otherCell in cellsCopy)
        {
            if (cell != otherCell)
            {
                double distance = Math.Sqrt(Math.Pow(otherCell.X - cell.X, 2) +
                                            Math.Pow(otherCell.Y - cell.Y, 2));

                double force = Config.GRAVITY_FORCE / (distance * distance + 1);

                dx += (otherCell.X - cell.X) * force;
                dy += (otherCell.Y - cell.Y) * force;
            }
        }

        return (dx, dy);
    }

    private void RemoveCell(Cell cell)
    {
        canvas.Children.Remove(cell.ellipse);
        canvas.Children.Remove(cell.line);
        canvas.Children.Remove(cell.line2);

        cells.Remove(cell);
        dynamicObjects.Remove(cell);

        if (removedCells.Count > 50)
        {
            removedCells.RemoveAt(0);
        }

        removedCells.Add(cell);
    }

    private Matrix<double> CrossMatrix(Matrix<double> matrix1, Matrix<double> matrix2)
    {
        if (matrix1.RowCount != matrix2.RowCount || matrix1.ColumnCount != matrix2.ColumnCount)
        {
            throw new ArgumentException("Матрицы должны иметь одинаковые размеры.");
        }

        Matrix<double> crossedMatrix = Matrix<double>.Build.Dense(matrix1.RowCount, matrix1.ColumnCount);

        for (int i = 0; i < matrix1.RowCount; i++)
        {
            for (int j = 0; j < matrix1.ColumnCount; j++)
            {
                // Генерируем случайное число от 0 до 1 и сравниваем с заданной вероятностью rate
                if (random.NextDouble() < 0.5)
                {
                    // Если случайное число меньше rate, берем значение из matrix1
                    crossedMatrix[i, j] = matrix1[i, j];
                }
                else
                {
                    // Иначе берем значение из matrix2
                    crossedMatrix[i, j] = matrix2[i, j];
                }
            }
        }

        return crossedMatrix;
    }


    private (Matrix<double>, Matrix<double>, Matrix<double>) GenomeCrosing(Cell cell, Cell otherCell)
    {
        var otherCellGenom = otherCell.NN.GetGenom();
        var cellGenom = cell.NN.GetGenom();

        Matrix<double> crossMatrix1 = CrossMatrix(cellGenom.Item1, otherCellGenom.Item1);
        Matrix<double> crossMatrix2 = CrossMatrix(cellGenom.Item2, otherCellGenom.Item2);
        Matrix<double> crossMatrix3 = CrossMatrix(cellGenom.Item3, otherCellGenom.Item3);

        return (crossMatrix1, crossMatrix2, crossMatrix3);
    }

    private void UpdateGame(object sender, EventArgs e)
    {
        UpdateFps();

        if (cells.Count <= 0)
        {
            DrawCellsBy(removedCells);
            textFood.UpdateText($"{foodCount} foods eaten");
            foodCount = 0;
            cellsCopy.Clear();
            cellsCopy.AddRange(cells);
        }

        cellsCopy.Clear();
        cellsCopy.AddRange(cells);

        foreach (var cell in cellsCopy)
        {
            if (cell.Energy < 1)
            {
                RemoveCell(cell);
            }

            if (Config.GRAVITY)
            {
                (double, double) dxy = CalculateGravity(cell, cellsCopy);
                cell.SpeedX += dxy.Item1;
                cell.SpeedY += dxy.Item2;
            }

            cell.X += cell.SpeedX;
            cell.Y += cell.SpeedY;

            if (border.isLeftBorder(cell.X, cell.Y) || border.isRightBorder(cell.X, cell.Y))
            {
                if (Config.BOUNCE)
                {
                    cell.SpeedX = -cell.SpeedX * Config.BOUNCE_EFFECT;
                    cell.X += cell.SpeedX;
                }
                else
                {
                    RemoveCell(cell);
                    continue;
                }
            }

            if (border.isTopBorder(cell.X, cell.Y) || border.isBottomBorder(cell.X, cell.Y))
            {
                if (Config.BOUNCE)
                {
                    cell.SpeedY = -cell.SpeedY * bounceEffect;
                    cell.Y += cell.SpeedY;
                }
                else
                {
                    RemoveCell(cell);
                    continue;
                }
            }

            double x;
            double y;

            List<Food> foodsCopy = new List<Food>(foods);
            foreach (var food in foodsCopy)
            {
                double distance = Math.Sqrt(Math.Pow(food.X - cell.X, 2) +
                                            Math.Pow(food.Y - cell.Y, 2));

                if (distance < (cell.Radius + food.Radius))
                {
                    cell.Energy += Config.FOOD_ENERGY;
                    foodCount++;

                    x = (random.NextDouble() * 2 - 1) * (1900);
                    y = (random.NextDouble() * 2 - 1) * (1900);

                    double radius = Config.FOOD_RADIUS;
                    SolidColorBrush color = Config.FOOD_COLOR;

                    Food newFood = new Food(canvas, x, y, radius, color);
                    foods.Add(newFood);
                    dynamicObjects.Add(newFood);

                    foods.Remove(food);
                    dynamicObjects.Remove(food);
                    canvas.Children.Remove(food.ellipse);
                }
            }


            if (cell.Energy >= Config.DIVISION_ENERGY_CONSUM && random.NextDouble() > Config.CELL_DIVISION_PROP &&
                cells.Count < Config.MAX_CELLS)
            {
                Cell newCell = new Cell(canvas, cell.X, cell.Y, cell.Radius, random.NextDouble() * (Math.PI * 2),
                    random.NextDouble() * 10, Config.DEFAULT_ENERGY, cell.RGBColor);


                if (random.NextDouble() < Config.CELL_CROSSING_PROP)
                {
                    Cell otherCell = cellsCopy[random.Next(0, cellsCopy.Count)];
                    var averageGenom = GenomeCrosing(cell, otherCell);
                    newCell.NN.NewGenom(averageGenom);
                }
                else
                {
                    newCell.NN.NewGenom(newCell.NN.Mutation());
                }

                cell.Energy -= Config.DIVISION_ENERGY_CONSUM;

                // newCells.Add(newCell);
                cells.Add(newCell);
                dynamicObjects.Add(newCell);

                textCells.UpdateText($"{cells.Count} частиц");
            }


            List<Parcile> partCells = new List<Parcile>(cells);
            foreach (var food in foods)
            {
                partCells.Add((Parcile)food);
            }

            (cell.AngleView2, cell.isWall2, cell.isBug2, cell.isFood2) =
                cell.GetAngleView(cell.GetSpeedAngle() - 0.1, cell, partCells);
            (cell.AngleView3, cell.isWall3, cell.isBug3, cell.isFood3) =
                cell.GetAngleView(cell.GetSpeedAngle() + 0.1, cell, partCells);
            (cell.AngleView, cell.isWall, cell.isBug, cell.isFood) =
                cell.GetAngleView(cell.GetSpeedAngle(), cell, partCells);

            double[] predition = cell.NN.FeedForward(cell.GetInput());


            double preictedAngle = predition[0];
            double predictedSpeed = Math.Abs(10*predition[1]);
            double predictedFire = predition[2];
            
            // if (predictedFire >= 0)
            // {
            //     if (cell.AngleView < 0.5 && cell.targerCell != null && cell.isBug == 1)
            //     {
            //         //new CellDeath(canvas, dynamicObjects, cell.targerCell.X, cell.targerCell.Y, cell.targerCell.RGBColor);
            //         RemoveCell(cell.targerCell);
            //         cell.Energy += 100;
            //         cell.line.X1 = CConverter.FrameToPixel(cell.X, CConverter.FieldFramePositionX);
            //         cell.line.X2 = cell.line.X1+CConverter.FramesToPixels(Math.Cos(cell.GetSpeedAngle())*(Math.Exp(cell.AngleView * 4)));
            //
            //         cell.line.Y1 = CConverter.FrameToPixel(cell.Y, CConverter.FieldFramePositionY);
            //         cell.line.Y2 = cell.line.Y1+CConverter.FramesToPixels(Math.Sin(cell.GetSpeedAngle())*(Math.Exp(cell.AngleView * 4)));
            //         continue;
            //     }
            // }

            cell.Angle += preictedAngle*0.5;

            
            cell.SpeedX = Math.Cos(cell.Angle) * predictedSpeed;
            cell.SpeedY = Math.Sin(cell.Angle) * predictedSpeed;

            // cell.SpeedX *= Config.FRICTION_FORCE;
            // cell.SpeedY *= Config.FRICTION_FORCE;
            
            cell.Energy -= Math.Abs(predictedSpeed*0.01);
            cell.Energy -= Config.LIVING_ENERGY_CONSUM;

            if (cell.Energy < 1)
            {
                RemoveCell(cell);
            }
        }
    }

    private void UpdateFps()
    {
        frameCount++;

        DateTime now = DateTime.Now;
        TimeSpan elapsed = now - lastFrameTime;
        if (elapsed.TotalSeconds >= 1)
        {
            double fps = frameCount / elapsed.TotalSeconds;

            textFps.UpdateText($"{Math.Round(fps)} fps");
            // Сбрасываем счетчик и время
            frameCount = 0;
            lastFrameTime = now;
        }
    }

    private void DrawText()
    {
        textCells = new Text(canvas, 13, -24, "cells");
        textScale = new Text(canvas, -45, -24, "3x");
        textFps = new Text(canvas, 13, 9, "fps");
        textFood  = new Text(canvas, 13, 27, "fps");
        staticObjects.Add(textCells);
        staticObjects.Add(textScale);
        staticObjects.Add(textFps);
        staticObjects.Add(textFood);
    }

    private void DrawBorders()
    {
        border = new Border(canvas, 0, 0, 4000, 4000);
        dynamicObjects.Add(border);
    }

    private void DrawCells()
    {
        for (int i = 0; i < Config.CELLS; i++)
        {
            double x = (random.NextDouble() * 2 - 1) * 1900;
            double y = (random.NextDouble() * 2 - 1) * 1900;
            double radius = Config.CELL_RADIUS;
            double angle = 2 * Math.PI * random.NextDouble();
            double speed = 4;
            SolidColorBrush color = PickCellColor();

            Cell cell = new Cell(canvas, x, y, radius, angle, speed, Config.DEFAULT_ENERGY, color);
            cells.Add(cell);
            dynamicObjects.Add(cell);
        }
    }

    private void DrawCellsBy(List<Cell> cellsBy)
    {
        for (int i = 0; i < Config.CELLS; i++)
        {
            double x = (random.NextDouble() * 2 - 1) * 1900;
            double y = (random.NextDouble() * 2 - 1) * 1900;
            double radius = Config.CELL_RADIUS;
            double angle = 2 * Math.PI * random.NextDouble();
            double speed = 4;
            SolidColorBrush color = PickCellColor();

            Cell cell = new Cell(canvas, x, y, radius, angle, speed, Config.DEFAULT_ENERGY,
                cellsBy[i % cellsBy.Count].RGBColor);
            cell.NN.NewGenom(cellsBy[i % cellsBy.Count].NN.Mutation());
            cells.Add(cell);
            dynamicObjects.Add(cell);
        }
    }

    private SolidColorBrush PickCellColor()
    {
        byte RC;
        byte GC;
        byte BC;

        while (true)
        {
            RC = (byte)random.Next(255);
            GC = (byte)random.Next(255);
            BC = (byte)random.Next(255);
            if (RC + GC + BC >= 255 && RC + GC + BC <= 255 * 2)
            {
                break;
            }
        }

        return new SolidColorBrush(Color.FromRgb(RC, GC, BC));
    }

    private void DrawFood()
    {
        for (int i = 0; i < Config.FOODS; i++)
        {
            double x = (random.NextDouble() * 2 - 1) * 1900;
            double y = (random.NextDouble() * 2 - 1) * 1900;
            double radius = Config.FOOD_RADIUS;
            SolidColorBrush color = Config.FOOD_COLOR;

            Food food = new Food(canvas, x, y, radius, color);
            foods.Add(food);
            dynamicObjects.Add(food);
        }
    }
}