using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace F.L.A.M.E
{
    public partial class ShopOverviewView : UserControl
    {
        private readonly int _sensorCount;
        // private readonly MockGunDataProvider _provider = new();
        private readonly Dictionary<int, TextBlock> _tempLabels = new();
        private readonly Dictionary<int, TextBlock> _flowLabels = new();
        private readonly Dictionary<int, Border> _sensorBoxes = new();
        private readonly Dictionary<int, DateTime> _lastUpdateTimes = new();
        private readonly DispatcherTimer _statusTimer = new DispatcherTimer();

        private const double BoxSize = 70;
        private const double VerticalSpacing = 120;
        private const double HorizontalSpacing = 300;
        private const int SensorsPerColumn = 6;
        private const double LeftOffset = 130;


        public ShopOverviewView(int sensorCount)
        {
            InitializeComponent();
            _sensorCount = sensorCount;
            Loaded += ShopOverviewView_Loaded;
        }

        private void ShopOverviewView_Loaded(object sender, RoutedEventArgs e)
        {
            GenerateLayout();
            //_provider.OnGunDataUpdated += Provider_OnGunDataUpdated;
            PlcReader.SharedInstance.OnGunDataUpdated += Provider_OnGunDataUpdated; // ✅ Subscribe to real PLC reader

            //_provider.Start();

            // Timer setup
            _statusTimer.Interval = TimeSpan.FromSeconds(1);
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();
        }

        private void Provider_OnGunDataUpdated(object? sender, GunDataEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_tempLabels.ContainsKey(e.GunIndex))
                {
                    _tempLabels[e.GunIndex].Text = $"Temp: {e.Temperature:F1} °C";
                }

                if (_flowLabels.ContainsKey(e.GunIndex))
                {
                    _flowLabels[e.GunIndex].Text = $"Flow: {e.Flow:F1} L / min";
                }

                if (_sensorBoxes.ContainsKey(e.GunIndex))
                {
                    _sensorBoxes[e.GunIndex].Background = Brushes.LightGreen;
                    _lastUpdateTimes[e.GunIndex] = DateTime.Now;
                }
            });
        }

        private void StatusTimer_Tick(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            foreach (var pair in _sensorBoxes)
            {
                int index = pair.Key;
                Border box = pair.Value;

                if (!_lastUpdateTimes.ContainsKey(index) || (now - _lastUpdateTimes[index]).TotalSeconds > 2)
                {
                    box.Background = Brushes.Red;
                }
            }
        }

        private void GenerateLayout()
        {
            Polyline pipe = new Polyline
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 5,
                StrokeDashArray = new DoubleCollection { 3, 3 },
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            PipeCanvas.Children.Add(pipe);

            double bendExtension = 70;
            int column = 0;
            bool isFirst = true;

            for (int i = 0; i < _sensorCount; i++)
            {
                int indexInColumn = i % SensorsPerColumn;
                if (indexInColumn == 0 && i > 0)
                {
                    column++;
                }

                double x = column * HorizontalSpacing + LeftOffset;

                double y;

                if (column % 2 == 0)
                {
                    y = indexInColumn * VerticalSpacing + 40;
                }
                else
                {
                    y = (SensorsPerColumn - 1 - indexInColumn) * VerticalSpacing + 40;
                }

                // Sensor Box
                var box = new Border
                {
                    Width = BoxSize,
                    Height = BoxSize,
                    Background = Brushes.Red,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Child = new TextBlock
                    {
                        Text = $"Gun {i}",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontWeight = FontWeights.Bold
                    }
                };
                PipeCanvas.Children.Add(box);
                Canvas.SetLeft(box, x);
                Canvas.SetTop(box, y);

                // Temp label
                var tempText = new TextBlock
                {
                    Text = $"Temp: 0°C",
                    Margin = new Thickness(5, 0, 0, 0),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold
                };
                PipeCanvas.Children.Add(tempText);
                Canvas.SetLeft(tempText, x - BoxSize - 50);
                Canvas.SetTop(tempText, y + 35);

                // Flow label
                var flowText = new TextBlock
                {
                    Text = $"Flow: 0 L/min",
                    Margin = new Thickness(5, 0, 0, 0),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold
                };
                PipeCanvas.Children.Add(flowText);
                Canvas.SetLeft(flowText, x - BoxSize - 50);
                Canvas.SetTop(flowText, y + 15);

                _tempLabels[i] = tempText;
                _flowLabels[i] = flowText;
                _sensorBoxes[i] = box;

                // Pipe point
                double pipeX = x + BoxSize / 2;
                double pipeY = y + BoxSize / 2;

                if (isFirst)
                {
                    pipe.Points.Add(new Point(pipeX, pipeY));
                    isFirst = false;
                }
                else
                {
                    pipe.Points.Add(new Point(pipeX, pipeY));
                }

                bool isLastInColumn = indexInColumn == SensorsPerColumn - 1;
                if (isLastInColumn && (i + 1) < _sensorCount)
                {
                    // Extend downward
                    pipe.Points.Add(new Point(pipeX, pipeY + bendExtension));

                    // Move right to next column base
                    double nextX = (column + 1) * HorizontalSpacing + LeftOffset + BoxSize / 2;
                    pipe.Points.Add(new Point(nextX, pipeY + bendExtension));

                    // Move to start of next column direction
                    double nextY;
                    if ((column + 1) % 2 == 0)
                    {
                        nextY = 40 + BoxSize / 2;
                    }
                    else
                    {
                        nextY = 40 + (SensorsPerColumn - 1) * VerticalSpacing + BoxSize / 2;
                    }
                    pipe.Points.Add(new Point(nextX, nextY));
                }
            }

            PipeCanvas.Width = (_sensorCount / SensorsPerColumn + 1) * HorizontalSpacing + 100;
            PipeCanvas.Height = SensorsPerColumn * VerticalSpacing + 100;

            // Animate water flow
            var dashAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 8,
                To = 0,
                Duration = TimeSpan.FromSeconds(1),
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
            };

            pipe.BeginAnimation(Shape.StrokeDashOffsetProperty, dashAnimation);
        }
    }
}
