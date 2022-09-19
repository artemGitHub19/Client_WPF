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

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {       
        List<MyImage> images = new List<MyImage>();
        HttpClient client = new HttpClient();

        BitmapImage editIcon;
        BitmapImage deleteIcon;

        public MainWindow()
        {
            InitializeComponent();

            client.BaseAddress = new Uri("https://localhost:7115/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));            

            ButtonAddNewImage.Click += ButtonAddNewImage_Click;            

            editIcon = CreateBitmapImage("data/editIcon.png");
            deleteIcon = CreateBitmapImage("data/deleteIcon.png");

            GetImagesAsync();
        }   

        private void ButtonAddNewImage_Click(object sender, RoutedEventArgs e)
        {

            MyImage newImage = new MyImage();

            var dialog = new DialogEditItemWindow(newImage);

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                newImage = dialog.image;
                if (String.IsNullOrEmpty(newImage.Name))
                {
                    newImage.Name = "Without a name";
                }
                CreateImageAsync(newImage);
            }            
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            string stringId = button.Name.ToString().Split("_")[1];
            int id = int.Parse(stringId);

            MyImage? image = images.FirstOrDefault((item) => item.Id == id);

            var dialog = new DialogEditItemWindow(image);            
            bool? result = dialog.ShowDialog();            

            if (result == true)
            {               
                image = dialog.image;
                if (String.IsNullOrEmpty(image.Name))
                {
                    image.Name = "Without a name";
                }
                UpdateImageAsync(image);
            }            
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {            
            if (MessageBox.Show("Are you sure you want to delete the image?",
                    "Confirmation",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                Button button = (Button)sender;
                string id = button.Name.ToString().Split("_")[1];

                DeleteImageAsync(id);
            }           
        }

        async Task<string> GetImagesAsync()
        {      
            string path = "/api/images";
            string jsonString = null;

            HttpResponseMessage response = await client.GetAsync(path);

            jsonString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Status code: " + response.StatusCode.ToString() + ". Message: " + jsonString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return jsonString;
            }

            images = JsonSerializer.Deserialize<List<MyImage>>(jsonString)!;

            wrapPanel1.Children.Clear();

            for (int i = 0; i < images.Count; i++)
            {

                byte[] binaryData = Convert.FromBase64String(images[i].Content);

                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = new MemoryStream(binaryData);
                bi.EndInit();

                WrapPanel wp = new WrapPanel() { Orientation = Orientation.Vertical, Margin = new Thickness(20, 20, 20, 0) };

                WrapPanel wp1 = new WrapPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5), HorizontalAlignment = HorizontalAlignment.Right };

                Button editButton = new Button() { Content = "Edit", Height = 20, Width = 20, Background = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0) };
                Button deleteButton = new Button() { Content = "Delete", Height = 20, Width = 20, Margin = new Thickness(10, 0, 0, 0), Background = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0) };

                editButton.Content = new Image() { Source = editIcon, Height = 20, Width = 20, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                deleteButton.Content = new Image () { Source = deleteIcon, Height = 20, Width = 20,  VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

                editButton.Name = "name_" + images[i].Id;
                deleteButton.Name = "name_" + images[i].Id;

                editButton.Click += EditButton_Click;
                deleteButton.Click += DeleteButton_Click;

                wp1.Children.Add(editButton);
                wp1.Children.Add(deleteButton);

                wp.Children.Add(wp1);

                Border border = new Border() { BorderThickness = new Thickness(1), BorderBrush = System.Windows.Media.Brushes.White };
                border.Child = new Image() { Source = bi, Height = 150, Width = 200, Stretch = Stretch.Fill };
                wp.Children.Add(border);

                wp.Children.Add(new Label() { Content = images[i].Name });

                
                wrapPanel1.Children.Add(wp);
            }

            return jsonString;
        }        

        async Task CreateImageAsync(MyImage myImage)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/images", myImage);

            HandleResponse(response);
        }

        async Task UpdateImageAsync(MyImage myImage)
        {
            HttpResponseMessage response = await client.PutAsJsonAsync($"/api/images/{myImage.Id}", myImage);

            HandleResponse(response);
        }

        async Task DeleteImageAsync(string id)
        {
            HttpResponseMessage response = await client.DeleteAsync($"api/products/{id}");

            HandleResponse(response);
        }

        private async void HandleResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Status code: " + response.StatusCode.ToString() + ". Message: " + responseData, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                GetImagesAsync();
            }
        }

        private BitmapImage CreateBitmapImage(string path)
        {
            byte[] imageArray = System.IO.File.ReadAllBytes(path);
            string base64ImageRepresentation = Convert.ToBase64String(imageArray);
            byte[] binaryData = Convert.FromBase64String(base64ImageRepresentation);

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(binaryData);
            bitmapImage.EndInit();

            return bitmapImage;
        }

        public class MyImage
        {
            public int Id { get; set; } = 0;
            public string Name { get; set; } = "";
            public string Content { get; set; } = "";
        }        
    }
}
