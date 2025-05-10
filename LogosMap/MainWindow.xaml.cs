using Microsoft.Win32;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LogosMap
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }

        private SKPoint translate = new(0, 0);
        private float scale = 1.0f;

        public List<Node> selectedNodes = [];

        public Node? selectedNode;

        public static List<Node> nodes = [];
        public static Dictionary<int, Node> nodeIds = new();

        private SKPoint clickPosition;
        private SKPoint startDragPosition;

        private bool isEdited;

        private bool isPanning = false;

        private readonly SKPaint FillPaint;
        private readonly SKPaint StrokePaint;

        public string FileName = "새 마인드맵";
        public string FileDirectory = "";

        private readonly SKFont font = new();

        private SKRect bounds;

        private float targetScale = 1f;
        private const float lerpSpeed = 0.05f;

        public static int lastId;

        private Node? editingNode = null;

        public MainWindow()
        {
            InitializeComponent();

            Instance = this;

            mainWindow.Title = !isEdited ? "로고스맵 - " + FileName : "로고스맵 - " + FileName + "*";
            font.Typeface = Util.GetTypeface("GMARKETSANSTTFLIGHT.TTF");

            FillPaint = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
            };

            StrokePaint = new SKPaint
            {
                Color = SKColor.Parse("#4FFFFFFF"),
                StrokeWidth = 0.6f,
                IsAntialias = true
            };

            CompositionTarget.Rendering += OnRendering;

            AddNewNode(new SKPoint(400, 250));
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            var args = (RenderingEventArgs)e;
            float dt = (float)args.RenderingTime.TotalSeconds;

            scale = scale + (targetScale - scale) * Math.Min(1, lerpSpeed * dt);

            if (editingNode != null)
            {
                SKPoint BoxPos = new(editingNode.x, editingNode.y);

                Point ScreenPos = GetScreenPosition(BoxPos);

                TextBoxScale.ScaleX = scale * 0.8f;
                TextBoxScale.ScaleY = scale * 0.8f;
                EditorBox.RenderTransform = new TranslateTransform(ScreenPos.X - (skCanvas.ActualWidth / 2 - 0), ScreenPos.Y - (skCanvas.ActualHeight / 2 - 16 * scale));
            }

            skCanvas.InvalidateVisual();
        }

        private void SkCanvas_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;

            canvas.Clear(SKColor.Parse("#20252B"));

            // 3) 중앙 좌표 계산
            float cx = e.Info.Width / 2f;
            float cy = e.Info.Height / 2f;

            canvas.Save();
            canvas.Translate(cx, cy);
            canvas.Scale(scale);
            canvas.Translate(-cx, -cy);
            canvas.Translate(translate.X, translate.Y);

            foreach (var node in nodes)
            {
                foreach (Connection connection in node.connections)
                {
                    canvas.DrawLine(new SKPoint(connection.startNode.x, connection.startNode.y), new SKPoint(connection.endNode.x, connection.endNode.y), StrokePaint);
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

                canvas.DrawCircle(node.x, node.y, 6f, FillPaint);
                canvas.DrawText(node.name, new SKPoint(node.x, node.y + 20), SKTextAlign.Center, font, FillPaint);
            }

            canvas.GetLocalClipBounds(out bounds);

            canvas.Restore();
        }

        #region Mouse Events

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = GetPosition(e.GetPosition(this));

            var clickedNode = GetNodeAtPosition(position);

            var clickedText = GetTextAtPosition(position);

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

                EditorBox.Focus();
                EditorBox.SelectAll();
            }
            else if (clickedNode != null)
            {
                selectedNode = clickedNode;

                isEdited = true;
                mainWindow.Title = !isEdited ? "로고스맵 - " + FileName : "로고스맵 - " + FileName + "*";

                EndEditing();
            }
            else
            {
                startDragPosition = new SKPoint((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y - 18);
                skCanvas.CaptureMouse();
                EndEditing();
                isPanning = true;
            }

            clickPosition = position;
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isPanning)
            {
                skCanvas.ReleaseMouseCapture();
                isPanning = false;

            }
            if (selectedNode != null)
            {
                selectedNode = null;
            }
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
                    selectedNode = AddNewNode(position);
                    ConnectNodes(clickedNode, selectedNode);
                }
                isEdited = true;
                mainWindow.Title = !isEdited ? "로고스맵 - " + FileName : "로고스맵 - " + FileName + "*";
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

        private void Canvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (selectedNode != null)
            {
                editingNode = selectedNode;
                EditorBox.Text = selectedNode.name;
                selectedNode.name = "";

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

                EditorBox.Focus();
                EditorBox.SelectAll();

                selectedNode = null;
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            var position = GetPosition(e.GetPosition(this));

            if (selectedNode != null)
            {
                var offset = position - clickPosition;

                selectedNode.Move(selectedNode.x + offset.X, selectedNode.y + offset.Y);
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

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                targetScale *= 1.1f;
            }
            else
            {
                targetScale /= 1.1f;
            }

            skCanvas.InvalidateVisual();
        }

        #endregion

        #region Key Events
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                if (FileDirectory == null || FileDirectory == "")
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        Title = "저장...",
                        Filter = "JSON 파일 (*.json)|*.json",
                        FileName = "새 마인드맵.json"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string filePath = saveFileDialog.FileName;
                        FileName = saveFileDialog.SafeFileName;
                        FileDirectory = filePath;

                        isEdited = false;
                        mainWindow.Title = !isEdited ? "로고스맵 - " + FileName : "로고스맵 - " + FileName + "*";

                        SaveLoad.SaveMindMap(filePath);
                    }
                }
                else
                {
                    isEdited = false;
                    mainWindow.Title = !isEdited ? "로고스맵 - " + FileName : "로고스맵 - " + FileName + "*";

                    SaveLoad.SaveMindMap(FileDirectory);
                }
            }

            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) != 0 && (Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "다른 이름으로 저장...",
                    Filter = "JSON 파일 (*.json)|*.json",
                    FileName = FileName
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    FileName = saveFileDialog.SafeFileName;
                    FileDirectory = filePath;

                    isEdited = false;
                    mainWindow.Title = !isEdited ? "로고스맵 - " + FileName : "로고스맵 - " + FileName + "*";

                    SaveLoad.SaveMindMap(filePath);
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {

        }
        #endregion

        #region UI Events
        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            nodes.Clear();
            nodeIds.Clear();
            lastId = 0;

            FileDirectory = "";
            FileName = "새 마인드맵";
            isEdited = false;

            AddNewNode(new SKPoint(400, 250));

            translate = new SKPoint(0, 0);
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "다른 이름으로 저장...",
                Filter = "JSON 파일 (*.json)|*.json",
                FileName = FileName
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                FileName = saveFileDialog.SafeFileName;
                FileDirectory = filePath;
                isEdited = false;
                mainWindow.Title = !isEdited ? "로고스맵 - " + FileName : "로고스맵 - " + FileName + "*";
                SaveLoad.SaveMindMap(filePath);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileDirectory == null || FileDirectory == "")
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "저장...",
                    Filter = "JSON 파일 (*.json)|*.json",
                    FileName = "새 마인드맵.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    FileName = saveFileDialog.SafeFileName;
                    FileDirectory = filePath;

                    isEdited = false;
                    mainWindow.Title = !isEdited ? "로고스맵 - " + FileName : "로고스맵 - " + FileName + "*";

                    SaveLoad.SaveMindMap(filePath);
                }
            }
            else
            {
                isEdited = false;
                mainWindow.Title = !isEdited ? "로고스맵 - " + FileName : "로고스맵 - " + FileName + "*";

                SaveLoad.SaveMindMap(FileDirectory);
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "불러오기...",
                Filter = "JSON 파일 (*.json)|*.json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                FileName = openFileDialog.SafeFileName;
                FileDirectory = filePath;
                isEdited = false;
                mainWindow.Title = !isEdited ? "로고스맵 - " + FileName : "로고스맵 - " + FileName + "*";
                SaveLoad.LoadMindMap(filePath);
            }

            translate = new SKPoint(0, 0);
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (isEdited)
            {
                var dlg = new SaveConfirmWindow
                {
                    Owner = this
                };

                bool? dialogResult = dlg.ShowDialog();
                if (dialogResult == true)
                {
                    switch (dlg.Result)
                    {
                        case MessageBoxResult.Yes:
                            if (FileDirectory == null || FileDirectory == "")
                            {
                                var saveFileDialog = new SaveFileDialog
                                {
                                    Title = "저장...",
                                    Filter = "JSON 파일 (*.json)|*.json",
                                    FileName = "새 마인드맵.json"
                                };

                                if (saveFileDialog.ShowDialog() == true)
                                {
                                    string filePath = saveFileDialog.FileName;
                                    FileName = saveFileDialog.SafeFileName;
                                    FileDirectory = filePath;

                                    isEdited = false;
                                    mainWindow.Title = !isEdited ? "로고스맵 - " + FileName : "로고스맵 - " + FileName + "*";

                                    SaveLoad.SaveMindMap(filePath);
                                }
                            }
                            else
                            {
                                isEdited = false;
                                mainWindow.Title = !isEdited ? "로고스맵 - " + FileName : "로고스맵 - " + FileName + "*";

                                SaveLoad.SaveMindMap(FileDirectory);
                            }
                            break;
                        case MessageBoxResult.No:
                            // 저장 없이 종료
                            break;
                    }
                }
                else
                {
                    // 취소 눌렀으면 창 닫기 중단
                    e.Cancel = true;
                }
            }
        }
        #endregion

        #region Editor Box Events
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

        private void EndEditing()
        {
            if (editingNode != null)
            {
                editingNode.name = EditorBox.Text != "" ? EditorBox.Text : "노드";
                EditorBox.Visibility = Visibility.Collapsed;
                skCanvas.InvalidateVisual();

                isEdited = true;
                mainWindow.Title = !isEdited ? "로고스맵 - " + FileName : "로고스맵 - " + FileName + "*";

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
        #endregion

        private void OpenNodeMenu(Node node)
        {
            var menu = new ContextMenu();

            var addNode = new MenuItem { Header = "노드 삭제" };
            addNode.Click += (s, args) =>
            {
                DeleteNode(node);
                skCanvas.InvalidateVisual();
                isEdited = true;
                mainWindow.Title = !isEdited ? "로고스맵 - " + FileName : "로고스맵 - " + FileName + "*";
            };

            menu.Items.Add(addNode);

            menu.IsOpen = true;
        }

        private Node? GetTextAtPosition(SKPoint position)
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

        private static Node? GetNodeAtPosition(SKPoint position)
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

        private void ConnectNodes(Node node1, Node node2)
        {
            Connection connection = new(node1, node2);

            node2.connections.Add(connection);
            node1.startConnections.Add(connection);

            skCanvas.InvalidateVisual();
        }

        private Node AddNewNode(SKPoint position)
        {
            Node node = new(lastId, position.X, position.Y);

            nodeIds.Add(lastId, node);

            nodes.Add(node);

            skCanvas.InvalidateVisual();

            lastId++;

            return node;
        }

        private void DeleteNode(Node node)
        {
            editingNode = null;
            selectedNode = null;

            foreach(var connection in node.startConnections)
            {
                connection.endNode.connections.Remove(connection);
            }
            foreach (var connection in node.connections)
            {
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
    }
}