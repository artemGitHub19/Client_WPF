using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for DialogEditItemWindow.xaml
    /// </summary>
    public partial class DialogEditItemWindow : Window
    {
        public MainWindow.MyImage image { get; set; }

        public string fileName { get; set; }

        public DialogEditItemWindow(MainWindow.MyImage imageToEdit)
        {
            InitializeComponent();

            image = imageToEdit;            

            ChooseImage.Click += ChooseImage_Click;
            
            var nameBindingObject = new Binding("Name");            
            nameBindingObject.Mode = BindingMode.TwoWay;
            nameBindingObject.Source = image;          
            BindingOperations.SetBinding(TextBoxName, TextBox.TextProperty, nameBindingObject);

            if (imageToEdit.Content != "")
            {
                byte[] binaryData = Convert.FromBase64String(imageToEdit.Content);

                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = new MemoryStream(binaryData);
                bi.EndInit();

                Image1.Source = bi;
            }
            else
            {
                okButton.Visibility = Visibility.Hidden;
                this.Title = "Create";
                ChooseImage_Click(null, null);                
            }
        }

        private void ChooseImage_Click(object sender, RoutedEventArgs e)
        {           
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.DefaultExt = ".jpg"; 
            fileDialog.Filter = " JPG |*.jpg"; 

            if (fileDialog.ShowDialog() == true)
            {                
                byte[] imageArray = System.IO.File.ReadAllBytes(fileDialog.FileName);

                string base64ImageRepresentation = Convert.ToBase64String(imageArray);

                image.Content = base64ImageRepresentation;

                byte[] binaryData = Convert.FromBase64String(image.Content);

                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = new MemoryStream(binaryData);
                bi.EndInit();

                Image1.Source = bi;

                okButton.Visibility = Visibility.Visible;
            } 
            else
            {
                if (this.Title == "Create")
                {
                    ButtonAutomationPeer peer = new ButtonAutomationPeer(cancelButton);
                    IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                    invokeProv.Invoke();
                }
            }
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            //DialogResult = false;
        }

        private void okButton_Click(object sender, RoutedEventArgs e) =>
            DialogResult = true;

        private void cancelButton_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;
        
    }
}
