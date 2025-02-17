﻿using Graph.Core;
using Graph.Core.Algorithms.SearchAlgo;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Laba5_Algo.ViewModels
{
    public class MainWindowVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private FileDialogService openFileDialog;

        public ObservableCollection<IGraphSearch> SearchAlgorithms { get; }

        private IGraphSearch selectedSearchAlgo;
        public IGraphSearch SelectedSearchAlgo
        {
            get { return selectedSearchAlgo; }
            set { selectedSearchAlgo = value; OnPropertyChanged(nameof(SelectedSearchAlgo)); }
        }

        public MainWindowVM()
        {
            SearchAlgorithms = new ObservableCollection<IGraphSearch>()
            {
                new BFS(),
                new DFS()
            };
            SelectedSearchAlgo = SearchAlgorithms[0];

            VertexRadius = 30;
            Vertices = new ObservableCollection<VertexVM>();
            Edges = new ObservableCollection<EdgeVM>();

            Clear = new RelayCommand(clear);
            Save = new RelayCommand(save);
            Load = new RelayCommand(load);
            StartSearchAlgo = new RelayCommandAsync(startSearchAlgo);
            StartMaxFlowAlgo = new RelayCommandAsync(startMaxFlowAlgo);
            StartMinTreeAlgo = new RelayCommandAsync(startMinTreeAlgo);
            StartMinPathAlgo = new RelayCommandAsync(startMinPath);

            openFileDialog = new FileDialogService();

            isDragging = false;
            IsConnecting = false;
            IsOriented = false;
            Visible = Visibility.Collapsed;

        }

        private bool isDragging;
        private int vertexRadius;
        private int vertexCount;

        public int VertexRadius
        {
            get { return vertexRadius; }
            set
            {
                vertexRadius = value;
                OnPropertyChanged(nameof(VertexRadius));
            }
        }

        public ICommand Clear { get; set; }
        public ICommand Save { get; set; }
        public ICommand Load { get; set; }
        public ICommand StartSearchAlgo { get; set; }
        public ICommand StartMaxFlowAlgo { get; set; }
        public ICommand StartMinPathAlgo { get; set; }
        public ICommand StartMinTreeAlgo { get; set; }

        private ObservableCollection<VertexVM> vertices;
        public ObservableCollection<VertexVM> Vertices
        {
            get { return vertices; }
            set
            {
                vertices = value;
                OnPropertyChanged(nameof(Vertices));
            }
        }

        private ObservableCollection<EdgeVM> edges;
        public ObservableCollection<EdgeVM> Edges
        {
            get { return edges; }
            set
            {
                edges = value;
                OnPropertyChanged(nameof(Edges));
            }
        }

        private VertexVM selectedVertex;
        public VertexVM SelectedVertex
        {
            get { return selectedVertex; }
            set
            {
                selectedVertex = value;
                OnPropertyChanged(nameof(SelectedVertex));
            }
        }

        private VertexVM fromVertex;
        public VertexVM FromVertex
        {
            get { return fromVertex; }
            set
            {
                fromVertex = value;
                OnPropertyChanged(nameof(FromVertex));
            }
        }

        private VertexVM toVertex;
        public VertexVM ToVertex
        {
            get { return toVertex; }
            set
            {
                toVertex = value;
                OnPropertyChanged(nameof(ToVertex));
            }
        }

        private bool isOriented;
        public bool IsOriented
        {
            get { return isOriented; }
            set {
                clear(null);
                isOriented = value; 
                OnPropertyChanged(nameof(IsOriented));
            }
        }

        private bool isConnecting;
        public bool IsConnecting
        {
            get { return isConnecting; }
            set
            {
                isConnecting = value;
                OnPropertyChanged(nameof(IsConnecting));
                Visible = IsConnecting ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private Visibility visible;
        public Visibility Visible
        {
            get { return visible; }
            set
            {
                visible = value;
                OnPropertyChanged(nameof(Visible));
            }
        }

        private int mousePointX;
        public int MousePointX
        {
            get { return mousePointX; }
            set
            {
                mousePointX = value;
                OnPropertyChanged(nameof(MousePointX));
            }
        }

        private int mousePointY;
        public int MousePointY
        {
            get { return mousePointY; }
            set
            {
                mousePointY = value;
                OnPropertyChanged(nameof(MousePointY));
            }
        }

        private void clear(object obj)
        {
            Vertices.Clear();
            Edges.Clear();
            vertexCount = 0;
        }

        private void load(object obj)
        {
            Vertices.Clear();
            Edges.Clear();
      
            if(!openFileDialog.OpenFileDialog())
                return;

            string path = openFileDialog.FilePath;
            try
            {
                var res = GraphFileConverter.Load(path);

                IsOriented = res.Item3;
                Vertices = new ObservableCollection<VertexVM>(res.Item1);
                Edges = new ObservableCollection<EdgeVM>(res.Item2);
            } 
            catch
            {
                MessageBox.Show("Неваллидный файл");
            }
        }

        private void save(object obj)
        {
            if (!openFileDialog.SaveFileDialog())
                return;

            string path = openFileDialog.FilePath;
            GraphFileConverter.Save(Vertices.ToList(), Edges.ToList(), IsOriented, path);
        }

        public void MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition((UIElement)sender);
            MousePointX = (int)p.X - 15;
            MousePointY = (int)p.Y - 15;

            if(isDragging && p.X > VertexRadius / 2 && selectedVertex != null)
            {
                SelectedVertex.X = MousePointX; 
                SelectedVertex.Y = MousePointY;

                var edgesToRemove = GetConnectedEdge(SelectedVertex);
                edgesToRemove.ForEach(edge => { Edges.Remove(edge); });
                edgesToRemove.ForEach(edge => { Edges.Add(edge); });
            }
        }

        public void CanvasClick(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition((UIElement)sender);
            Point newVertexPoint = new Point(p.X - VertexRadius / 2, p.Y - VertexRadius / 2);

            if (GetVertexFromPoint(newVertexPoint) != null)
                return;

            Vertices.Add(new VertexVM((int)newVertexPoint.X, (int)newVertexPoint.Y, vertexCount++.ToString()));
        }

        public void RemoveEdge(object sender, RoutedEventArgs e)
        {
            Edges.Remove((sender as MenuItem).Tag as EdgeVM);
        }

        public void EditEdge(object sender, RoutedEventArgs e)
        {
            var edge = (sender as MenuItem).Tag as EdgeVM;

            int weight = new EditEdgeVM(edge.Weight).ShowDialog();
            edge.Weight = weight == -1 ? edge.Weight : weight;
        }

        public void RemoveVertex(object sender, RoutedEventArgs e)
        {
            var vertex = (sender as MenuItem).Tag as VertexVM;

            var edgesToRemove = GetConnectedEdge(vertex);
            edgesToRemove.ForEach(edge => { Edges.Remove(edge); });

            Vertices.Remove(vertex);
        }

        public void ConnectVertex(object sender, RoutedEventArgs e)
        {
            var vertex = (sender as MenuItem).Tag as VertexVM;
            if(vertex == null) 
                throw new NullReferenceException(nameof(vertex));

            SelectedVertex = vertex;
            IsConnecting = true;
        }

        public void VertexClick(object sender, MouseButtonEventArgs e)
        {
            Point clickedPoint = e.GetPosition((UIElement)sender);
            var vertex = GetVertexFromPoint(clickedPoint);
            if (vertex == null)
                return;

            if (IsConnecting)
            {
                int weight = new EditEdgeVM().ShowDialog();
                if (weight != -1)
                {
                    Edges.Add(new EdgeVM(SelectedVertex, vertex, VertexRadius, weight, IsOriented));
                }

                IsConnecting = false;
            }
            else
            {
                SelectedVertex = vertex;
                isDragging = true;
            }
        }

        public void Vertex_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
        }

        private VertexVM? GetVertexFromPoint(Point point)
        {
            foreach (var vertex in Vertices)
            {
                if (IsPointInsideEllipse(point, vertex, VertexRadius))
                {
                    return vertex;
                }
            }
            return null;
        }

        private List<EdgeVM> GetConnectedEdge(VertexVM vertex)
        {
            var edges = new List<EdgeVM>();
            foreach (var edge in Edges)
            {
                if (edge.From == vertex || edge.To == vertex)
                {
                    edges.Add(edge);
                }
            }
            return edges;
        }

        private bool IsPointInsideEllipse(Point point, VertexVM vertex, double radius)
        {
            double dx = point.X - vertex.X;
            double dy = point.Y - vertex.Y;

            return (dx * dx) / (radius * radius) + (dy * dy) / (radius * radius) <= 1;
        }

        private async Task startSearchAlgo()
        {
            var graph = GraphVMConverter.ToModel(Vertices.ToList(), Edges.ToList(), IsOriented);
            var nodes = SelectedSearchAlgo.Traversal(graph);

            foreach (var node in nodes)
            {
                Vertices.Where(n => n.Name == node.Name).First().SetGreen();
                await Task.Delay(500);
            }
            MessageBox.Show("Выполнено");

            foreach(var vertex in Vertices)
            {
                vertex.SetDefaultColor();
            }
        }

        private async Task startMinPath()
        {
            throw new NotImplementedException();
        }

        private async Task startMinTreeAlgo()
        {
            throw new NotImplementedException();
        }

        private async Task startMaxFlowAlgo()
        {
            throw new NotImplementedException();
        }
    }
}
