using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Programming_For_Kinect_Book
{
    /// <summary>
    /// Interaction logic for StartWindow.xaml
    /// </summary>
    public partial class StartWindow : Window
    {
        public StartWindow()
        {
            InitializeComponent();
        }

        private void btn_Explore_Click(object sender, RoutedEventArgs e)
        {
           
            showMainWindow();
            launchEarth();
        }

        public void showMainWindow()
        {
     
                MainWindow main = new MainWindow();
                App.Current.MainWindow = main;
                this.Close();
                main.userName = tb_UserName.Text + " ";
                main.Show();
                
              
       
        }

        public void launchEarth()
        {
            Process.Start(@"C:\Program Files (x86)\Google\Google Earth\client\googleearth.exe"); //may have to change file path
        }
    }
}
