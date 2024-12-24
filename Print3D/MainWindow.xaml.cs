using HelixToolkit.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Print3D
{
    public partial class MainWindow : Window
    {
        private Model3DGroup modelGroup = new Model3DGroup();
        private Material previousMaterial = null;
        private GeometryModel3D selectedModel = null; // Текущая выделенная модель

        public MainWindow()
        {
            InitializeComponent();

            // Инициализация группы моделей
            var modelVisual = new ModelVisual3D { Content = modelGroup };
            helixViewport.Children.Add(modelVisual);

            helixViewport.MouseDown += OnViewportMouseDown; // Подключение события для выделения модели
        }

        private void CreateBox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double width = double.Parse(WidthTextBox.Text);
                double height = double.Parse(HeightTextBox.Text);
                double depth = double.Parse(DepthTextBox.Text);

                var meshBuilder = new MeshBuilder();
                meshBuilder.AddBox(new Point3D(0, 0, 0), width, height, depth);

                var material = new DiffuseMaterial(new SolidColorBrush(Colors.Blue));
                var geometry = meshBuilder.ToMesh();

                var model = new GeometryModel3D(geometry, material);
                modelGroup.Children.Add(model);
            }
            catch
            {
                MessageBox.Show("Введите корректные размеры куба.");
            }
        }

        private void CreateSphere_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double radius = double.Parse(RadiusTextBox.Text);

                var meshBuilder = new MeshBuilder();
                meshBuilder.AddSphere(new Point3D(0, 0, 0), radius, 32, 32);

                var material = new DiffuseMaterial(new SolidColorBrush(Colors.Red));
                var geometry = meshBuilder.ToMesh();

                var model = new GeometryModel3D(geometry, material);
                modelGroup.Children.Add(model);
            }
            catch
            {
                MessageBox.Show("Введите корректный радиус.");
            }
        }

        private void MoveObject_Click(object sender, RoutedEventArgs e)
        {
            if (selectedModel != null)
            {
                try
                {
                    double x = double.Parse(MoveXTextBox.Text);
                    double y = double.Parse(MoveYTextBox.Text);
                    double z = double.Parse(MoveZTextBox.Text);

                    var transform = new TranslateTransform3D(x, y, z);
                    selectedModel.Transform = transform;

                    
                }
                catch
                {
                    MessageBox.Show("Введите корректные значения перемещения.");
                }
            }
            else
            {
                MessageBox.Show("Выберите модель для перемещения.");
            }
        }

        private void OnViewportMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Определяем точку клика мыши
            var mousePosition = e.GetPosition(helixViewport);

            // Выполняем тест на попадание
            var hitResult = VisualTreeHelper.HitTest(helixViewport, mousePosition) as RayMeshGeometry3DHitTestResult;

            if (hitResult != null)
            {
                // Проверяем, попали ли в объект типа GeometryModel3D
                if (hitResult.ModelHit is GeometryModel3D geometryModel)
                {
                    // Снимаем выделение с предыдущей модели
                    if (selectedModel != null)
                    {
                        selectedModel.Material = previousMaterial; // Возвращаем старый материал
                    }

                    // Запоминаем текущую модель
                    selectedModel = geometryModel;
                    previousMaterial = geometryModel.Material; // Сохраняем текущий материал
                    geometryModel.Material = new DiffuseMaterial(new SolidColorBrush(Colors.Green)); // Перекрашиваем в зеленый
                }
            }
            else
            {
                // Снимаем выделение, если кликнули по фону
                if (selectedModel != null)
                {
                    selectedModel.Material = previousMaterial; // Возвращаем старый материал
                    selectedModel = null;
                }
            }
        }


        private void SaveModels_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "OBJ Files|*.obj|STL Files|*.stl",
                Title = "Сохранить модель"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string extension = System.IO.Path.GetExtension(saveFileDialog.FileName).ToLower();

                try
                {
                    if (extension == ".obj")
                    {
                        var exporter = new ObjExporter
                        {
                            MaterialsFile = System.IO.Path.ChangeExtension(saveFileDialog.FileName, ".mtl") // Указание пути для файла материалов
                        };

                        using (var stream = File.Create(saveFileDialog.FileName))
                        {
                            exporter.Export(modelGroup, stream);
                        }

                        MessageBox.Show("Модель успешно сохранена в формате OBJ.");
                    }
                    else if (extension == ".stl")
                    {
                        var exporter = new StlExporter();

                        using (var stream = File.Create(saveFileDialog.FileName))
                        {
                            exporter.Export(modelGroup, stream);
                        }

                        MessageBox.Show("Модель успешно сохранена в формате STL.");
                    }
                    else
                    {
                        MessageBox.Show("Неподдерживаемый формат для сохранения.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}");
                }
            }
        }



        private void LoadModels_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "OBJ Files|*.obj|STL Files|*.stl",
                Title = "Загрузить модель"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string extension = System.IO.Path.GetExtension(openFileDialog.FileName).ToLower();

                if (extension == ".obj")
                {
                    var reader = new ObjReader();
                    var modelCollection = reader.Read(openFileDialog.FileName);

                    foreach (Model3D model in modelCollection.Children) // Обходим коллекцию моделей
                    {
                        modelGroup.Children.Add(model);
                    }
                }
                else if (extension == ".stl")
                {
                    var reader = new StLReader();
                    Model3DGroup modelGroupFromFile = reader.Read(openFileDialog.FileName) as Model3DGroup;

                    if (modelGroupFromFile != null)
                    {
                        foreach (Model3D model in modelGroupFromFile.Children) // Обходим модели внутри Model3DGroup
                        {
                            modelGroup.Children.Add(model);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Ошибка загрузки STL модели.");
                    }
                }
                else
                {
                    MessageBox.Show("Неподдерживаемый формат.");
                    return;
                }

                MessageBox.Show("Модель успешно загружена.");
            }
        }


        private void DeleteModel_Click(object sender, RoutedEventArgs e)
        {
            if (selectedModel != null)
            {
                // Удаляем модель из коллекции
                modelGroup.Children.Remove(selectedModel);

                // Снимаем выделение
                selectedModel.Material = previousMaterial; // Возвращаем старый материал
                selectedModel = null;

                
            }
            else
            {
                MessageBox.Show("Нет модели для удаления.");
            }
        }
    }
}
