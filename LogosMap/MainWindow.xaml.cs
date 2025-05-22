using Microsoft.Win32;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LogosMap.resources.lang;
using LogosMap.actions;

namespace LogosMap
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }

        private SKPoint translate = new(0, 0);
        private float scale = 1.0f;

        public List<Node> selectedNodes = [];

        public Node? movingNode;

        public static List<Node> nodes = [];
        public static Dictionary<int, Node> nodeIds = [];

        private SKPoint clickPosition;
        private SKPoint startDragPosition;

        private bool isEdited;

        private bool isPanning = false;

        public string FileName = Strings.NewFile;
        public string FileDirectory = "";

        private readonly SKFont font = new();

        private SKRect bounds;
        private SKRect selectionBox = new();

        private float targetScale = 1f;
        private const float lerpSpeed = 0.05f;

        public static int lastId;

        private Node? editingNode = null;

        private SKPoint lineEnd;

        public List<Actions> actions = [];

        private int actionPointer;

        private readonly List<Node> selectionBoxNodes = [];

        private SKPoint leftClickPos;

        private bool doubleClick;

        public MainWindow()
        {
            InitializeComponent();

            //Initialize Localized Strings
            FileName = Strings.NewFile;
            FileMenu.Header = "_" + Strings.File;
            NewMenu.Header = "_" + Strings.NewFile;
            SaveMenu.Header = "_" + Strings.Save;
            SaveAsMenu.Header = "_" + Strings.SaveAs;
            LoadMenu.Header = "_" + Strings.Load;

            Instance = this;

            ResetTitle();
            font.Typeface = Util.GetTypeface("GMARKETSANSTTFLIGHT.TTF");

            CompositionTarget.Rendering += OnRendering;

            AddNewNode(new SKPoint(400, 250));
        }

        #region Rendering
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
                EditorBoxTranslate.X = ScreenPos.X - (skCanvas.ActualWidth / 2 - 0);
                EditorBoxTranslate.Y = ScreenPos.Y - (skCanvas.ActualHeight / 2 - 16 * scale);
            }

            skCanvas.InvalidateVisual();
        }

        private void SkCanvas_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;

            canvas.Clear(SKColor.Parse("#20252B"));

            float cx = e.Info.Width / 2f;
            float cy = e.Info.Height / 2f;

            canvas.Save();
            canvas.Translate(cx, cy);
            canvas.Scale(scale);
            canvas.Translate(-cx, -cy);
            canvas.Translate(translate.X, translate.Y);

            if (newNode && prevNode != null)
            {
                canvas.DrawLine(new SKPoint(prevNode.x, prevNode.y), lineEnd, Util.TransparentPaint);
                canvas.DrawCircle(lineEnd.X, lineEnd.Y, 6f, Util.WhitePaint);            
            }

            foreach (var node in nodes)
            {
                foreach (Connection connection in node.connections)
                {
                    canvas.DrawLine(new SKPoint(connection.startNode.x, connection.startNode.y), new SKPoint(connection.endNode.x, connection.endNode.y), Util.TransparentPaint);
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


                canvas.DrawCircle(node.x, node.y, 6f, Util.WhitePaint);
                if (selectedNodes.Contains(node) || selectionBoxNodes.Contains(node))
                {
                    canvas.DrawCircle(node.x, node.y, 6.2f, Util.DarkOrangePaint);
                }
                canvas.DrawText(node.name, new SKPoint(node.x, node.y + 20), SKTextAlign.Center, font, Util.WhitePaint);
            }

            if (MathF.Abs(selectionBox.Width) > 0.1f || MathF.Abs(selectionBox.Height) > 0.1f)
            {
                canvas.DrawRect(selectionBox.Left, selectionBox.Top, selectionBox.Width, selectionBox.Height, Util.TransparentPaint);
            }

            canvas.GetLocalClipBounds(out bounds);

            canvas.Restore();
        }
        #endregion

        #region Mouse Events
        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = GetPosition(e.GetPosition(this));

            var clickedNode = GetNodeAtPosition(position);

            var clickedText = GetTextAtPosition(position);

            if (clickedText != null)//If clicked on node text
            {
                selectedNodes.Clear();
                selectedNodes.Add(clickedText);
                EndEditing();
                ShowEditorBox(clickedText);
            }
            else if (clickedNode != null)//If clicked on node
            {
                if (e.ClickCount >= 2)
                {
                    if (!selectedNodes.Contains(clickedNode)) selectedNodes.Add(clickedNode);
                    GetAllChildren(clickedNode);
                    doubleClick = true;
                }
                else
                {
                    doubleClick = false;
                    movingNode = clickedNode;

                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                    {
                        if (!selectedNodes.Contains(clickedNode))
                        {
                            selectedNodes.Clear();
                            selectedNodes.Add(clickedNode);
                        }
                    }
                    else
                    {
                        if (!selectedNodes.Remove(clickedNode))
                        {
                            selectedNodes.Add(clickedNode);
                        }
                    }

                    isEdited = true;
                    ResetTitle();
                }
                    
                EndEditing();
            }
            else//If clicked on nothing
            {
                skCanvas.CaptureMouse();
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                {
                    selectedNodes.Clear();
                }
                selectionBoxNodes.Clear();
                selectionBox.Top = position.Y; 
                selectionBox.Left = position.X;
                selectionBox.Bottom = position.Y;
                selectionBox.Right = position.X;
                EndEditing();
            }

            leftClickPos = position;
            clickPosition = position;
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var position = GetPosition(e.GetPosition(this));

            if (selectedNodes.Count > 0 && SKPoint.Distance(position, leftClickPos) > 0.1f)
            {
                Actions tempActions = [];

                foreach (Node node in selectedNodes)
                {
                    tempActions.Add(new MoveAction(node, new SKPoint(node.x, node.y) - (position - leftClickPos), new SKPoint(node.x, node.y)));
                }

                if (tempActions.Count > 0)
                {
                    AddAction(tempActions);
                }
            }

            foreach (Node node in selectionBoxNodes)
            {
                if (!selectedNodes.Contains(node))
                {
                    selectedNodes.Add(node);
                }
            }
            selectionBoxNodes.Clear();

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                if (SKPoint.Distance(position, leftClickPos) < 0.1f && !doubleClick)
                {
                    selectedNodes.Clear();
                    if (movingNode != null)
                    {
                        selectedNodes.Add(movingNode);
                    }
                }
            }

            skCanvas.ReleaseMouseCapture();

            selectionBox.Right = selectionBox.Left;
            selectionBox.Bottom = selectionBox.Top;

            if (movingNode != null)
            {
                movingNode = null;
            }
        }

        public bool newNode;
        public Node? prevNode;

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
                    lineEnd = position;
                    newNode = true;
                    prevNode = clickedNode;
                }
                isEdited = true;
                ResetTitle();
            }
            else
            {
                var menu = new ContextMenu();

                var addNode = new MenuItem { Header = "_" + Strings.AddNode };
                addNode.Click += (s, args) =>
                {
                    AddNewNode(position);
                };

                menu.Items.Add(addNode);

                menu.IsOpen = true;
            }
            clickPosition = position;
        }

        private void Canvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var position = GetPosition(e.GetPosition(this));

            if (newNode && prevNode != null)
            {
                var nodeAtPosition = GetNodeAtPosition(position);
                if (nodeAtPosition != null && !prevNode.Equals(nodeAtPosition))
                {
                    ConnectNodes(prevNode, nodeAtPosition);

                    prevNode = null;
                    newNode = false;
                }
                else
                {
                    var node = AddNewNode(position);
                    selectedNodes.Clear();
                    selectedNodes.Add(node);
                    ConnectNodes(prevNode, node);
                    ShowEditorBox(node);

                    prevNode = null;
                    newNode = false;
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            var position = GetPosition(e.GetPosition(this));

            if (newNode && prevNode != null) {
                var node = GetNodeAtPosition(position);
                if (node == null)
                {
                    lineEnd = position;
                }
                else
                {
                    lineEnd = new SKPoint(node.x, node.y);
                }
            }
            if (movingNode != null)
            {
                var offset = position - clickPosition;

                foreach(Node node in selectedNodes)
                {
                    node.Move(node.x + offset.X, node.y + offset.Y);
                }
            }
            else
            {
                if (e.MiddleButton == MouseButtonState.Pressed && isPanning)
                {
                    var currentPosition = new SKPoint((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y);
                    var delta = currentPosition - startDragPosition;
                    translate.X += delta.X * 1 / scale;
                    translate.Y += delta.Y * 1 / scale;
                    startDragPosition = currentPosition;
                }
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    selectionBox.Bottom = position.Y;
                    selectionBox.Right = position.X;

                    foreach (Node node in nodes)
                    {
                        if (IsNodeInSelectionBox(node))
                        {
                            if (!selectionBoxNodes.Contains(node))
                            {
                                selectionBoxNodes.Add(node);
                            }
                        }
                        else selectionBoxNodes.Remove(node);
                    }
                }
            }

            clickPosition = position;
        }

        private void Canvas_MouseMiddleButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                startDragPosition = new SKPoint((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y);
                skCanvas.CaptureMouse();
                isPanning = true;
            }
        }

        private void Canvas_MouseMiddleButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                if (isPanning)
                {
                    skCanvas.ReleaseMouseCapture();
                    isPanning = false;
                }
            }
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

        }

        #endregion

        #region Key Events
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //Save
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                if (FileDirectory == null || FileDirectory == "")
                {
                    SaveFile();
                }
                else
                {
                    isEdited = false;
                    ResetTitle();
                    SaveLoad.SaveMindMap(FileDirectory);
                }
            }

            //Save As
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) != 0 && (Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                SaveAsFile();
            }

            //Delete Node
            if (e.Key == Key.Delete)
            {
                Actions tempActions = [];
                foreach(Node node in selectedNodes)
                {
                    tempActions.Add(new DeleteAction(node));
                    DeleteNode(node);
                }
                if(tempActions.Count > 0)
                {
                    AddAction(tempActions);
                }
                isEdited = true;
                ResetTitle();
            }

            //Undo
            if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                if(actionPointer > 0)
                {
                    actions[actionPointer - 1].Undo();
                    actionPointer = Math.Max(0, actionPointer - 1);
                    isEdited = true;
                    ResetTitle();
                }
            }

            //Redo
            if (e.Key == Key.Y && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                if (actionPointer < actions.Count)
                {
                    actions[actionPointer].Redo();
                    actionPointer = Math.Min(actions.Count, actionPointer + 1);
                    isEdited = true;
                    ResetTitle();
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {

        }
        #endregion

        #region UI Events
        //Create New File
        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            nodes.Clear();
            nodeIds.Clear();
            lastId = 0;

            actions.Clear();
            actionPointer = 0;

            selectedNodes.Clear();

            FileDirectory = "";
            FileName = Strings.NewFile;
            isEdited = false;

            AddNewNode(new SKPoint(400, 250));

            translate = new SKPoint(0, 0);
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveAsFile();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileDirectory == null || FileDirectory == "")
            {
                SaveFile();
            }
            else
            {
                isEdited = false;
                ResetTitle();
                SaveLoad.SaveMindMap(FileDirectory);
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadFile();

            translate = new SKPoint(0, 0);
        }

        private void OpenNodeMenu(Node node)
        {
            var menu = new ContextMenu();

            var addNode = new MenuItem { Header = "_" + Strings.DeleteNode };
            addNode.Click += (s, args) =>
            {
                AddAction([new DeleteAction(node)]);
                DeleteNode(node);
                isEdited = true;
                ResetTitle();
            };

            menu.Items.Add(addNode);

            menu.IsOpen = true;
        }

        //Show Warning On Window Close
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
                                SaveFile();
                            }
                            else
                            {
                                isEdited = false;
                                ResetTitle();
                                SaveLoad.SaveMindMap(FileDirectory);
                            }
                            break;
                        case MessageBoxResult.No:
                            break;
                    }
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }
        #endregion

        #region Editor Box Events
        private void EditorBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Change Editor Box Width Depending on Text Length
            EditorBox.Width = MathF.Max(50, font.MeasureText(EditorBox.Text, Util.WhitePaint) + 10);
        }

        private void EndEditing()
        {
            //Change Node Name According to Editor Box Text
            if (editingNode != null)
            {
                editingNode.name = EditorBox.Text != "" ? EditorBox.Text : Strings.Node;
                EditorBox.Visibility = Visibility.Collapsed;

                isEdited = true;
                ResetTitle();
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

        private void ShowEditorBox(Node parentNode)
        {
            editingNode = parentNode;
            EditorBox.Text = parentNode.name;
            parentNode.name = "";

            EditorBox.BorderThickness = new Thickness(0);
            EditorBox.TextAlignment = TextAlignment.Center;
            EditorBox.Visibility = Visibility.Visible;
            EditorBox.Width = MathF.Max(50, font.MeasureText(EditorBox.Text, Util.WhitePaint) + 10);
            EditorBox.Height = 20;

            EditorBox.Focus();
            EditorBox.SelectAll();
        }
        #endregion

        //Check If Node Name is at a Position 
        private Node? GetTextAtPosition(SKPoint position)
        {
            foreach (Node node in nodes)
            {
                var nodeBounds = new SKRect(node.x - MathF.Max(50, font.MeasureText(node.name)) / 2, node.y + 10, node.x + MathF.Max(50, font.MeasureText(node.name)) / 2, node.y + 25);

                if (nodeBounds.Contains(position))
                {
                    return node;
                }
            }
            return null;
        }

        //Check If Node is at Position
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
        }

        public Node AddNewNode(SKPoint position)
        {
            Node node = new(lastId, position.X, position.Y);

            nodeIds.Add(lastId, node);

            nodes.Add(node);

            lastId++;

            AddAction(new Actions { new AddAction(node) });

            return node;
        }

        public Node AddNode(Node node)
        {
            nodeIds.Add(node.Id, node);

            nodes.Add(node);

            foreach(Connection connection in node.startConnections)
            {
                if (nodeIds.ContainsKey(connection.endNode.Id))
                {
                    connection.endNode.connections.Add(connection);
                }
            }

            foreach (Connection connection in node.connections)
            {
                if (nodeIds.ContainsKey(connection.startNode.Id))
                {
                    connection.startNode.startConnections.Add(connection);
                }
            }

            return node;
        }

        public void DeleteNode(Node node)
        {
            editingNode = null;
            movingNode = null;

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

        //Convert Screen Position to World Position
        private SKPoint GetPosition(Point position)
        {
            float xMultiplier = (float)(position.X / skCanvas.ActualWidth);
            float yMultiplier = (float)((position.Y - 18) / skCanvas.ActualHeight);

            float XPos = (bounds.Right - bounds.Left) * xMultiplier;
            float YPos = (bounds.Bottom - bounds.Top) * yMultiplier;

            return new SKPoint(bounds.Left + XPos, bounds.Top + YPos);
        }

        //Get Screen Position from World Position
        private Point GetScreenPosition(SKPoint position)
        {
            float xMultiplier = (float)((position.X - bounds.Left) / (bounds.Right - bounds.Left));
            float yMultiplier = (float)((position.Y - bounds.Top) / (bounds.Bottom - bounds.Top));

            double XPos = (float)skCanvas.ActualWidth * xMultiplier;
            double YPos = (float)skCanvas.ActualHeight * yMultiplier;

            return new Point(XPos, YPos);
        }

        private bool IsNodeInSelectionBox(Node node)
        {
            bool flag1 = false;
            bool flag2 = false;

            if (selectionBox.Left > selectionBox.Right)
            {
                flag1 = true;
            }
            if (selectionBox.Top > selectionBox.Bottom)
            {
                flag2 = true;
            }

            if(!flag1 && !flag2)
            {
                return (node.x >= selectionBox.Left)
                && (node.x < selectionBox.Right)
                && (node.y >= selectionBox.Top)
                && (node.y < selectionBox.Bottom);
            }
            else if(!flag1 && flag2)
            {
                return (node.x >= selectionBox.Left)
                && (node.x < selectionBox.Right)
                && (node.y < selectionBox.Top)
                && (node.y >= selectionBox.Bottom);
            }
            else if(flag1 && !flag2)
            {
                return (node.x < selectionBox.Left)
                && (node.x >= selectionBox.Right)
                && (node.y >= selectionBox.Top)
                && (node.y < selectionBox.Bottom);
            }
            else
            {
                return (node.x < selectionBox.Left)
                && (node.x >= selectionBox.Right)
                && (node.y < selectionBox.Top)
                && (node.y >= selectionBox.Bottom);
            }
        }

        private void GetAllChildren(Node node)
        {
            if (node.GetChildren().Count == 0)
            {
                return;
            }

            foreach (Node child in node.GetChildren())
            {
                if (!selectedNodes.Contains(child))
                {
                    selectedNodes.Add(child);
                }

                GetAllChildren(child);
            }
        }

        private void AddAction(Actions action)
        {
            if (actions.Count > 1)
            {
                if (actionPointer < actions.Count)
                {
                    actions.RemoveRange(actionPointer, actions.Count - actionPointer);
                }
            }

            if (actionPointer > actions.Count)
            {
                actionPointer = actions.Count;
            }
            
            actions.Add(action);
            actionPointer++;
        }

        public void SaveFile()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = Strings.Save + "...",
                Filter = Strings.JsonFile + " (*.json)|*.json",
                FileName = Strings.NewFile + ".json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                FileName = saveFileDialog.SafeFileName;
                FileDirectory = filePath;

                isEdited = false;
                ResetTitle();
                SaveLoad.SaveMindMap(filePath);
            }
        }

        public void SaveAsFile()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = Strings.SaveAs + "...",
                Filter = Strings.JsonFile + " (*.json)|*.json",
                FileName = FileName
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                FileName = saveFileDialog.SafeFileName;
                FileDirectory = filePath;
                isEdited = false;
                ResetTitle();
                SaveLoad.SaveMindMap(filePath);
            }
        }

        public void LoadFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = Strings.Load + "...",
                Filter = Strings.JsonFile + " (*.json)|*.json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                FileName = openFileDialog.SafeFileName;
                FileDirectory = filePath;
                isEdited = false;
                ResetTitle();
                selectedNodes.Clear();
                actions.Clear();
                actionPointer = 0;
                SaveLoad.LoadMindMap(filePath);
            }
        }

        private void ResetTitle()
        {
            mainWindow.Title = !isEdited ? Strings.LogosMap + "  - " + FileName : Strings.LogosMap + " - " + FileName + "*";
        }
    }
}