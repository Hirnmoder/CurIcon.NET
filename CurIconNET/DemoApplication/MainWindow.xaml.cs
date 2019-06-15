using CurIconNET;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DemoApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapSourceCurIcon curicon = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (FileStream fs = new FileStream(tbLoadPath.Text, FileMode.Open, FileAccess.Read))
                {
                    curicon = new BitmapSourceCurIcon(fs, true);
                }
                txtType.Text = $"Type: {curicon.Type.ToString()}, Frame(s): {curicon.FrameCount}";
                if(curicon.FrameCount > 0)
                {
                    img.Source = curicon[0];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonPathLoad_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Icons and Cursors|*.ico;*.cur|All files|*.*";
            ofd.Multiselect = false;
            ofd.Title = "Select an icon or a cursor";
            ofd.FileName = tbLoadPath.Text;
            if (ofd.ShowDialog() == true)
            {
                tbLoadPath.Text = ofd.FileName;
                ButtonLoad_Click(null, null);
            }
        }

        private void ButtonSavePath_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "Icons and Cursors|*.ico;*.cur|All files|*.*";
            sfd.Title = "Select a save destination";
            sfd.FileName = tbSavePath.Text;
            if (sfd.ShowDialog() == true)
            {
                tbSavePath.Text = sfd.FileName;
            }
        }

        private void ButtonSaveIcon_Click(object sender, RoutedEventArgs e)
        {
            Save(FileType.Icon);
        }

        private void ButtonSaveCursor_Click(object sender, RoutedEventArgs e)
        {
            Save(FileType.Cursor);
        }

        private void Save(FileType fileType)
        {
            try
            {
                using (FileStream fs = new FileStream(tbSavePath.Text, FileMode.Create, FileAccess.ReadWrite))
                {
                    curicon?.Save(fs, fileType, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
