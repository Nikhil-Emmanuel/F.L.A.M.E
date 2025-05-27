using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace F.L.A.M.E
{
    public partial class GunStatusView : UserControl
    {
        public GunStatusView(int GunCount)
        {
            InitializeComponent();
            CreateGunBoxes(GunCount);

            // Subscribe to PLC data updates
            PlcReader.SharedInstance.OnGunDataUpdated += HandlePlcDataUpdate;
        }

        private void HandlePlcDataUpdate(object? sender, GunDataEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var thermometer = FindName($"Thermometer{e.GunIndex}") as ProgressBar;
                var tempText = FindName($"GunLabel{e.GunIndex}") as TextBlock;
                var flowText = FindName($"GunFlow{e.GunIndex}") as TextBlock;

                UpdateThermometer(e.GunIndex, e.Temperature);
                if (tempText != null) tempText.Text = $"GUN {e.GunIndex}\n\nTemp:    {e.Temperature:F1} °C";
                if (flowText != null) flowText.Text = $"Flow:    {e.Flow:F1} L / min";


                UpdateFlowNeedle(e.GunIndex, e.Flow);
            });
        }

        private Dictionary<int, double> _currentTemperatures = new();

        public void UpdateThermometer(int gunIndex, double temperature)
        {
            if (FindName($"Thermometer{gunIndex}") is ProgressBar thermometer)
            {
                SolidColorBrush fillColor;
                if (temperature >= 35)
                    fillColor = Brushes.Red;
                else if (temperature >= 27)
                    fillColor = Brushes.Goldenrod; // Yellowish color
                else
                    fillColor = Brushes.Green;

                // Apply color
                thermometer.Foreground = fillColor;
                double currentTemp = _currentTemperatures.ContainsKey(gunIndex) ? _currentTemperatures[gunIndex] : 0;

                var anim = new DoubleAnimation
                {
                    From = currentTemp,
                    To = temperature,
                    Duration = TimeSpan.FromMilliseconds(900),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                thermometer.BeginAnimation(ProgressBar.ValueProperty, anim);

                _currentTemperatures[gunIndex] = temperature;
            }
        }

        private void CreateGunBoxes(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var border = new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10),
                    Margin = new Thickness(5),
                    CornerRadius = new CornerRadius(10)
                };

                var stackPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

                var thermometerGrid = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                thermometerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // scale
                thermometerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // thermometer

                // Create thermometer
                var thermometer = new ProgressBar
                {
                    Name = $"Thermometer{i}",
                    Width = 20,
                    Height = 150,
                    Maximum = 50,
                    Margin = new Thickness(0, 12, 0, 0),
                    Orientation = Orientation.Vertical,
                    BorderThickness = new Thickness(2.5),
                    Value = 0
                };
                this.RegisterName(thermometer.Name, thermometer);

                // Add thermometer to column 1
                Grid.SetColumn(thermometer, 1);
                thermometerGrid.Children.Add(thermometer);

                // Create scale (0 to 50°C)
                var scaleStack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Height = 160,
                    Margin = new Thickness(0, 10.1, 0, 0)
                };
                for (int t = 50; t >= 0; t -= 10)
                {
                    scaleStack.Children.Add(new TextBlock
                    {
                        Text = $"{t} °C - ",
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 16.2),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Right
                    });
                }
                Grid.SetColumn(scaleStack, 0);
                thermometerGrid.Children.Add(scaleStack);

                // ✅ THIS LINE was missing
                stackPanel.Children.Add(thermometerGrid);

                var flowMeterArc = CreateFlowMeterArc(i);

                var labelTemp = new TextBlock
                {
                    Name = $"GunLabel{i}",
                    Text = $"Temp: 0°C",
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 15, 0, 5),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                this.RegisterName(labelTemp.Name, labelTemp);

                var labelFlow = new TextBlock
                {
                    Name = $"GunFlow{i}",
                    Text = $"Flow: 0 L/min",
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 5),
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                this.RegisterName(labelFlow.Name, labelFlow);

                stackPanel.Children.Add(flowMeterArc);
                stackPanel.Children.Add(labelTemp);
                stackPanel.Children.Add(labelFlow);

                border.Child = stackPanel;
                GunGrid.Children.Add(border);
            }
        }

        private Grid CreateFlowMeterArc(int gunIndex)
        {
            double radius = 50;
            var canvas = new Canvas
            {
                Margin = new Thickness(0, 35, 0, 2),
                Width = radius * 2 + 20,
                Height = radius + 20
            };

            // Helper function to create arc Path
            Path CreateArc(double startAngle, double sweepAngle, Brush strokeBrush)
            {
                // Convert degrees to radians
                double startRadians = (startAngle - 180) * Math.PI / 180; // -90 to start from bottom left
                double endRadians = (startAngle + sweepAngle - 90) * Math.PI / 180;

                Point startPoint = new Point(radius + 10 + radius * Math.Cos(startRadians),
                                             radius + 10 + radius * Math.Sin(startRadians));
                Point endPoint = new Point(radius + 10 + radius * Math.Cos(endRadians),
                                           radius + 10 + radius * Math.Sin(endRadians));

                bool isLargeArc = sweepAngle > 180;

                var figure = new PathFigure
                {
                    StartPoint = startPoint,
                    Segments = new PathSegmentCollection
                    {
                        new ArcSegment
                        {
                            Point = endPoint,
                            Size = new Size(radius, radius),
                            IsLargeArc = isLargeArc,
                            SweepDirection = SweepDirection.Clockwise
                        }
                    }
                };

                var geometry = new PathGeometry(new[] { figure });

                return new Path
                {
                    Stroke = strokeBrush,
                    StrokeThickness = 15,
                    Data = geometry,
                    StrokeStartLineCap = PenLineCap.Flat,
                    StrokeEndLineCap = PenLineCap.Flat
                };
            }

            // Red arc: 0 to 8 (out of 30)
            // Calculate corresponding sweep angle (180 degrees total for max flow 30)
            double maxFlow = 30.0;
            double totalAngle = 180;
            double redEndAngle = (6 / maxFlow) * totalAngle;   // ~48 degrees
            double yellowEndAngle = (12 / maxFlow) * totalAngle; // ~72 degrees
            double greenStartAngle = (15 / maxFlow) * totalAngle; // start of green arc (flow=12)
            double greenEndAngle = (15 / maxFlow) * totalAngle;
            var redArc = CreateArc(0, redEndAngle, Brushes.Red);
            var yellowArc = CreateArc(redEndAngle, yellowEndAngle - redEndAngle, Brushes.Yellow);
            Path greenArc = CreateArc(greenStartAngle, greenEndAngle - greenStartAngle, Brushes.LightGreen);

            canvas.Children.Add(redArc);
            canvas.Children.Add(yellowArc);
            canvas.Children.Add(greenArc);

            var needle = new Line
            {
                Name = $"FlowNeedle{gunIndex}",
                X1 = radius + 10,
                Y1 = radius + 10,
                X2 = radius + 10,
                Y2 = 10,
                Stroke = Brushes.Black, // You can keep Red or Black for needle
                StrokeThickness = 4,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Triangle
            };
            for (int f = 0; f <= 30; f += 5)
            {
                double angle = (f / 30.0) * 180;
                double angleRadians = angle * Math.PI / 180;
                double labelRadius = radius + 15;
                double centerX = radius + 10;
                double centerY = radius + 10;

                double x = centerX + labelRadius * Math.Cos(angleRadians - Math.PI);
                double y = centerY + labelRadius * Math.Sin(angleRadians - Math.PI);

                var label = new TextBlock
                {
                    Text = f.ToString(),
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black
                };

                Canvas.SetLeft(label, x - 4);
                Canvas.SetTop(label, y - 8);
                canvas.Children.Add(label);
            }


            this.RegisterName(needle.Name, needle);

            canvas.Children.Add(needle);

            var grid = new Grid
            {
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            grid.Children.Add(canvas);

            return grid;
        }

        private Dictionary<int, double> _currentAngles = new();


        public void UpdateFlowNeedle(int gunIndex, double flowValue)
        {
            double maxFlow = 30.0;
            double radius = 50;
            double centerX = radius + 10;
            double centerY = radius + 10;

            // Calculate target angle in degrees and radians
            double targetAngle = (flowValue / maxFlow) * 180;
            double targetAngleRadians = targetAngle * Math.PI / 180;

            // Calculate target needle endpoint coordinates
            double targetX = centerX + radius * Math.Cos(targetAngleRadians - Math.PI);
            double targetY = centerY + radius * Math.Sin(targetAngleRadians - Math.PI);

            // Get current angle or default to 0
            double currentAngle = _currentAngles.ContainsKey(gunIndex) ? _currentAngles[gunIndex] : 0;
            double currentAngleRadians = currentAngle * Math.PI / 180;

            // Calculate current needle endpoint coordinates
            double currentX = centerX + radius * Math.Cos(currentAngleRadians - Math.PI);
            double currentY = centerY + radius * Math.Sin(currentAngleRadians - Math.PI);

            if (FindName($"FlowNeedle{gunIndex}") is Line needle)
            {
                // Animate X2 from currentX to targetX
                var animX = new DoubleAnimation
                {
                    From = currentX,
                    To = targetX,
                    Duration = TimeSpan.FromMilliseconds(900),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                needle.BeginAnimation(Line.X2Property, animX);

                // Animate Y2 from currentY to targetY
                var animY = new DoubleAnimation
                {
                    From = currentY,
                    To = targetY,
                    Duration = TimeSpan.FromMilliseconds(800),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                needle.BeginAnimation(Line.Y2Property, animY);
            }

            // Update stored current angle for next animation
            _currentAngles[gunIndex] = targetAngle;
        }
    }
}
