using Microsoft.Win32;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LogosMap
{
    public partial class MainWindow : Window
    {
        private SKPoint translate = new(0, 0);
        private float scale = 1.0f;

        public Node? moveNode;
        public static List<Node> nodes = [];
        public static Dictionary<int, Node> nodeIds = new();
        private SKPoint clickPosition;
        private SKPoint startDragPosition;

        public List<Node> selectedNode = [];

        private bool isPanning = false;

        public static MainWindow? Instance { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            AddNewNode(new SKPoint(400, 250));
        }

        private SKRect bounds;

        public static SKTypeface? GetTypeface(string fullFontName)
        {
            SKTypeface result;

            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("LogosMap.Fonts." + fullFontName);
            if (stream == null)
                return null;

            result = SKTypeface.FromStream(stream);
            return result;
        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "저장...",
                    Filter = "JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*",
                    FileName = "NewMap.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    SaveLoad.SaveMindMap(filePath);
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            
        }

        private SKFont font = new();

        private void SkCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;

            canvas.Clear(SKColor.Parse("#20252B"));

            canvas.Save();
            canvas.Translate(translate.X, translate.Y);
            canvas.Scale(scale);
            var paint = new SKPaint
            {
                Color = SKColor.FromHsl(255, 255, 255, 255),
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
            };
            var selectedPaint = new SKPaint
            {
                Color = SKColor.FromHsl(255, 255, 255, 255),
                StrokeWidth = 1,
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
            };
            var selectedPaintStroke = new SKPaint
            {
                Color = SKColor.Parse("#00FF00"),
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
            };
            font.Typeface = GetTypeface("GMARKETSANSTTFLIGHT.TTF");

            var paint2 = new SKPaint
            {
                Color = SKColors.Gray,
                StrokeWidth = 1,
                IsAntialias = true
            };
            foreach (var node in nodes)
            {
                foreach (Connection connection in node.connections)
                {
                    canvas.DrawLine(new SKPoint(connection.startNode.x, connection.startNode.y), new SKPoint(connection.endNode.x, connection.endNode.y), paint2);
                }
            }
            foreach (var node in nodes)
            {
                var nodeRect = new SKRect(
                    node.x - 3,
                    node.y - 3,
                    node.x + 3,
                    node.y + 3
                );

                if (!bounds.IntersectsWith(nodeRect)) continue;
                if(selectedNode.Contains(node))
                {
                    canvas.DrawCircle(node.x, node.y, 6f, selectedPaint);
                    canvas.DrawCircle(node.x, node.y, 6f, selectedPaintStroke);
                }
                else
                {
                    canvas.DrawCircle(node.x, node.y, 6f, paint);
                }

                canvas.DrawText(node.name, new SKPoint(node.x, node.y + 20), SKTextAlign.Center, font, paint);
            }

            canvas.GetLocalClipBounds(out bounds);

            canvas.Restore();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = GetPosition(e.GetPosition(this));

            var clickedNode = GetNodeAtPosition(position);

            var clickedText = IsTextAtPosition(position);

            if (clickedText != null)
            {
                editingNode = clickedText;
                EditorBox.Text = clickedText.name;
                clickedText.name = "";

                skCanvas.InvalidateVisual();

                EditorBox.BorderThickness = new Thickness(0);
                EditorBox.TextAlignment = TextAlignment.Center;
                EditorBox.Visibility = Visibility.Visible;
                var paint = new SKPaint
                {
                    Color = SKColor.FromHsl(255, 255, 255),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };
                EditorBox.Width = MathF.Max(50, font.MeasureText(EditorBox.Text, paint) + 10);
                EditorBox.Height = 20;

                SKPoint BoxPos = new(clickedText.x, clickedText.y);

                Point ScreenPos = GetScreenPosition(BoxPos);

                EditorBox.RenderTransform = new TranslateTransform(ScreenPos.X - (skCanvas.ActualWidth/2 - 0), ScreenPos.Y - (skCanvas.ActualHeight / 2 - 20));
                EditorBox.Focus();
                EditorBox.SelectAll();
            }
            else if (clickedNode != null)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                {
                    selectedNode.Add(clickedNode);
                }
                else
                {
                    selectedNode.Clear();
                    selectedNode.Add(clickedNode);
                }

                skCanvas.InvalidateVisual();
                moveNode = clickedNode;
                EndEditing();
            }
            else
            {
                startDragPosition = new SKPoint((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y - 18);
                skCanvas.CaptureMouse();
                selectedNode.Clear();
                skCanvas.InvalidateVisual();
                EndEditing();
                isPanning = true;
            }

            clickPosition = position;
        }

        private void EditorBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var paint = new SKPaint
            {
                Color = SKColor.FromHsl(255, 255, 255),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            EditorBox.Width = MathF.Max(50, font.MeasureText(EditorBox.Text, paint) + 10);
        }

        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = GetPosition(e.GetPosition(this));

            var clickedNode = GetNodeAtPosition(position);
            EndEditing();
            if (clickedNode != null)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                {
                    OpenNodeMenu(clickedNode);
                }
                else
                {
                    Node newNode = AddNewNode(position);
                    selectedNode.Clear();
                    selectedNode.Add(newNode);
                    moveNode = newNode;
                    ConnectNodes(clickedNode, newNode);
                }
                    
            }
            else
            {
                var menu = new ContextMenu();

                var addNode = new MenuItem { Header = "노드 추가" };
                addNode.Click += (s, args) =>
                {
                    AddNewNode(position);
                    skCanvas.InvalidateVisual();
                };

                menu.Items.Add(addNode);

                menu.IsOpen = true;
            }
            clickPosition = position;
        }

        private void OpenNodeMenu(Node node)
        {
            var menu = new ContextMenu();

            var addNode = new MenuItem { Header = "노드 삭제" };
            addNode.Click += (s, args) =>
            {
                DeleteNode(node);
                skCanvas.InvalidateVisual();
            };

            menu.Items.Add(addNode);

            menu.IsOpen = true;
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isPanning)
            {
                skCanvas.ReleaseMouseCapture();
                isPanning = false;

            }
            if (moveNode != null)
            {
                moveNode = null;
            }
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            nodes.Clear();
            nodeIds.Clear();
            lastId = 0;

            AddNewNode(new SKPoint(400, 250));

            translate = new SKPoint(0, 0);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "저장...",
                Filter = "JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*",
                FileName = "NewMap.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                SaveLoad.SaveMindMap(filePath);
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "불러오기...",
                Filter = "JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                SaveLoad.LoadMindMap(filePath);
            }

            translate = new SKPoint(0, 0);
        }

        private void Canvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (moveNode != null)
            {
                editingNode = moveNode;
                EditorBox.Text = moveNode.name;
                moveNode.name = "";

                skCanvas.InvalidateVisual();

                EditorBox.BorderThickness = new Thickness(0);
                EditorBox.TextAlignment = TextAlignment.Center;
                EditorBox.Visibility = Visibility.Visible;
                var paint = new SKPaint
                {
                    Color = SKColor.FromHsl(255, 255, 255),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };
                EditorBox.Width = MathF.Max(50, font.MeasureText(EditorBox.Text, paint) + 10);
                EditorBox.Height = 20;

                SKPoint BoxPos = new(moveNode.x, moveNode.y);

                Point ScreenPos = GetScreenPosition(BoxPos);

                EditorBox.RenderTransform = new TranslateTransform(ScreenPos.X - (skCanvas.ActualWidth / 2 - 0), ScreenPos.Y - (skCanvas.ActualHeight / 2 - 20));
                EditorBox.Focus();
                EditorBox.SelectAll();

                moveNode = null;
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            var position = GetPosition(e.GetPosition(this));

            if (moveNode != null)
            {
                var offset = position - clickPosition;

                SetNodePosition(moveNode, moveNode.x + offset.X, moveNode.y + offset.Y);
            }
            else
            {
                if (e.LeftButton == MouseButtonState.Pressed && isPanning)
                {
                    var currentPosition = new SKPoint((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y - 18);
                    var delta = currentPosition - startDragPosition;
                    translate.X += delta.X;
                    translate.Y += delta.Y;
                    startDragPosition = currentPosition;
                }
            }

            skCanvas.InvalidateVisual();
            clickPosition = position;
        }

        public void DrawLine(SKCanvas canvas)
        {
            SKPoint start = new SKPoint(100, 100);
            SKPoint end = new SKPoint(400, 400);

            var paint = new SKPaint
            {
                Color = SKColors.Gray,
                StrokeWidth = 1,
                IsAntialias = true
            };

            canvas.DrawLine(start, end, paint);
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                scale *= 1.1f;
            }
            else
            {
                scale /= 1.1f;
            }

            skCanvas.InvalidateVisual();
        }

        private Node? IsTextAtPosition(SKPoint position)
        {
            foreach (Node node in nodes)
            {
                var nodeBounds = new SKRect(node.x - font.MeasureText(node.name) / 2, node.y + 10, node.x + font.MeasureText(node.name) / 2, node.y + 25);

                if (nodeBounds.Contains(position))
                {
                    return node;
                }
            }
            return null;
        }

        private Node? GetNodeAtPosition(SKPoint position)
        {
            foreach (Node node in nodes)
            {
                var nodeBounds = new SKRect(node.x - 8, node.y - 8, node.x + 8, node.y + 8);

                if (nodeBounds.Contains(position))
                {
                    return node;
                }
            }
            return null;
        }

        public static int lastId;

        private Node AddNewNode(SKPoint position)
        {
            Node node = new(lastId, position.X, position.Y);

            nodeIds.Add(lastId, node);

            nodes.Add(node);

            skCanvas.InvalidateVisual();

            lastId++;

            return node;
        }

        private void SetNodePosition(Node node, float x, float y)
        {
            node.Move(x, y);
        }

        private void ConnectNodes(Node node1, Node node2)
        {
            Connection connection = new(node1, node2);

            node2.connections.Add(connection);
            node1.startConnections.Add(connection);

            skCanvas.InvalidateVisual();
        }

        private void DeleteNode(Node node)
        {
            editingNode = null;
            moveNode = null;

            foreach(var connection in node.startConnections)
            {
                connection.endNode.connections.Remove(connection);
                connection.startNode.startConnections.Remove(connection);
            }
            nodeIds.Remove(node.Id);
            nodes.Remove(node);
        }

        private SKPoint GetPosition(Point position)
        {
            float xMultiplier = (float)(position.X / skCanvas.ActualWidth);
            float yMultiplier = (float)((position.Y - 18) / skCanvas.ActualHeight);

            float XPos = (bounds.Right - bounds.Left) * xMultiplier;
            float YPos = (bounds.Bottom - bounds.Top) * yMultiplier;

            return new SKPoint(bounds.Left + XPos, bounds.Top + YPos);
        }

        private Point GetScreenPosition(SKPoint position)
        {
            float xMultiplier = (float)((position.X - bounds.Left) / (bounds.Right - bounds.Left));
            float yMultiplier = (float)((position.Y - bounds.Top) / (bounds.Bottom - bounds.Top));

            double XPos = (float)skCanvas.ActualWidth * xMultiplier;
            double YPos = (float)skCanvas.ActualHeight * yMultiplier;

            return new Point(XPos, YPos);
        }

        private Node? editingNode = null;

        private void EndEditing()
        {
            if (editingNode != null)
            {
                editingNode.name = EditorBox.Text != "" ? EditorBox.Text : "노드";
                EditorBox.Visibility = Visibility.Collapsed;
                skCanvas.InvalidateVisual();
                editingNode = null;
            }
        }

        private void EditorBox_LostFocus(object sender, RoutedEventArgs e) => EndEditing();

        private void EditorBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EndEditing();
            }
        }
    }
}