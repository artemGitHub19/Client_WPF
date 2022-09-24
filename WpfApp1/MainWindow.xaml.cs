using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Windows.Controls.Image;
using static WpfApp1.MainWindow;
using static System.Net.WebRequestMethods;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Net;
using System.Reflection;
using System.Globalization;
using System.Runtime.Intrinsics.X86;
using System.Diagnostics;
using System.Collections.Specialized;

namespace WpfApp1
{

    public class RelayCommand : ICommand
    {
        private readonly Predicate<object> _canExecute;
        private readonly Action<object> _execute;

        public RelayCommand(Predicate<object> canExecute, Action<object> execute)
        {
            _canExecute = canExecute;
            _execute = execute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }

    public class DateTimeToDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            byte[] binaryData = System.Convert.FromBase64String((string)value);

            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = new MemoryStream(binaryData);
            bi.EndInit();

            return bi;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<MyImage> items { get; set; }

        public ICommand EditButtonCmd { get; }
        public ICommand DeleteButtonCmd { get; }

        List<MyImage> images = new List<MyImage>();
        HttpClient client = new HttpClient();       

        public MainWindow()
        {
            InitializeComponent();  
            setClient();

            AddNewImageButton.Click += AddNewImageButton_Click;

            EditButtonCmd = new RelayCommand( o => true, EditButton_Click);
            DeleteButtonCmd = new RelayCommand( o => true, DeleteButton_Click);

            GetImagesAsync();

            DataContext = this;
        }

        private void EditButton_Click(object obj)
        {
            MyImage myImage = (MyImage)obj;
            var dialog = new DialogEditItemWindow(myImage);
            bool? result = dialog.ShowDialog();

            if (result == true)
            {                
                UpdateImageAsync(dialog.image);
            }
        }

        private void DeleteButton_Click(object obj)
        {
            MyImage myImage = (MyImage)obj;

            if (MessageBox.Show("Are you sure you want to delete the image?",
                    "Confirmation",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question) == MessageBoxResult.OK)
            {  
                DeleteImageAsync(myImage.Id);
            }
        }

        private void AddNewImageButton_Click(object sender, RoutedEventArgs e)
        {   
            MyImage newImage = new MyImage();
            var dialog = new DialogEditItemWindow(newImage);
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                newImage = dialog.image;                
                CreateImageAsync(newImage);
            }            
        }        

        async Task GetImagesAsync()
        {      
            string path = "/api/images";

            HttpResponseMessage response = await client.GetAsync(path);

            string jsonString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Status code: " + response.StatusCode.ToString() + ". Message: " + jsonString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                images = JsonSerializer.Deserialize<List<MyImage>>(jsonString)!;                
                items = new ObservableCollection<MyImage>(images);                

                phonesList.ItemsSource = items;                
            }            
        }        

        async Task CreateImageAsync(MyImage myImage)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/images", myImage);

            if (!response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Status code: " + response.StatusCode.ToString() + ". Message: " + responseData, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                string jsonString = await response.Content.ReadAsStringAsync();

                MyImage? newImage = JsonSerializer.Deserialize<MyImage>(jsonString); 
                
                items.Add(newImage);
            }
        }

        async Task UpdateImageAsync(MyImage myImage)
        {
            HttpResponseMessage response = await client.PutAsJsonAsync($"/api/images/{myImage.Id}", myImage);

            if (!response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Status code: " + response.StatusCode.ToString() + ". Message: " + responseData, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                string jsonString = await response.Content.ReadAsStringAsync();

                MyImage? imageToUpdate = JsonSerializer.Deserialize<MyImage>(jsonString);

                for (int i = 0; i < items.Count; i++)
                {                    

                    if (items[i].Id == imageToUpdate.Id)
                    {
                        items[i].Name = imageToUpdate.Name;
                        items[i].Content = imageToUpdate.Content;

                        phonesList.Items.Refresh();
                        break;
                    }
                }                
            }
        }

        async Task DeleteImageAsync(string id)
        {
            HttpResponseMessage response = await client.DeleteAsync($"api/products/{id}");

            if (!response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Status code: " + response.StatusCode.ToString() + ". Message: " + responseData, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                string imageId = await response.Content.ReadAsStringAsync();

                for (int i = 0; i < items.Count; i++)
                {

                    if (items[i].Id == imageId)
                    {
                        items.Remove(items[i]);
                        break;
                    }
                }
            }
        }  

        private void setClient()
        {
            client.BaseAddress = new Uri("https://localhost:7115/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public class MyImage 
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Content { get; set; } = "";            
        }        
    }
}
