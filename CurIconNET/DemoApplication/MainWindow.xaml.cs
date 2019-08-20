using CurIconNET;
using CurIconNET.Internals;
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
        private BitmapSourceCurIcon lsCI = null;
        private BitmapSourceCurIcon coCI = null;
        private int currentFrame = -1;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            coCI = new BitmapSourceCurIcon(FileType.Icon);
        }

        #region Load and Save
        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (FileStream fs = new FileStream(tbLoadPath.Text, FileMode.Open, FileAccess.Read))
                {
                    lsCI = new BitmapSourceCurIcon(fs, true);
                }
                txtType.Text = $"Type: {lsCI.Type.ToString()}, Frame(s): {lsCI.FrameCount}";
                if (lsCI.FrameCount > 0)
                {
                    img.Source = lsCI[0].BitmapFrame;
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
            Save(lsCI, FileType.Icon, tbSavePath.Text);
        }

        private void ButtonSaveCursor_Click(object sender, RoutedEventArgs e)
        {
            Save(lsCI, FileType.Cursor, tbSavePath.Text);
        }

        private void Save(BitmapSourceCurIcon bsci, FileType fileType, string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                {
                    bsci?.Save(fs, fileType, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonRotate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var degrees = double.Parse(tbRotation.Text);
                for (int i = 0; i < lsCI.FrameCount; i++)
                {
                    var pngFrame = lsCI[i];
                    pngFrame.RotateFrame(degrees, BitmapScalingMode.Linear);
                }
                imgPreview.Source = lsCI[0].BitmapFrame;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Construct

        private void UpdateLabelAndImage()
        {
            if (coCI.FrameCount > 0)
            {
                txtFrame.Text = $"Frame ({currentFrame + 1} of {coCI.FrameCount})";
                imgConstruct.Source = coCI[currentFrame].BitmapFrame;
            }
            else
            {
                txtFrame.Text = $"Frame (0 of 0)";
                imgConstruct.Source = null;
            }
            imgConstruct.InvalidateArrange();
        }

        private void ButtonPrev_Click(object sender, RoutedEventArgs e)
        {
            if (coCI.FrameCount == 0) return;
            if (currentFrame > 0) currentFrame--;
            UpdateLabelAndImage();
        }

        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            if (coCI.FrameCount == 0) return;
            if (currentFrame < coCI.FrameCount - 1) currentFrame++;
            UpdateLabelAndImage();
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Pictures|*.png;*.jpg;*.jpeg;*.gif;*.tif;*.tiff|All files|*.*";
            ofd.Multiselect = false;
            ofd.Title = "Select an image.";
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    coCI.AddPngFrame(new PngFrame(File.ReadAllBytes(ofd.FileName), 0, 0, true));
                    currentFrame = coCI.FrameCount - 1;
                    UpdateLabelAndImage();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            if (coCI.FrameCount == 0) return;
            coCI.RemovePngFrameAt(currentFrame);
            currentFrame = Math.Max(currentFrame - 1, 0);
            UpdateLabelAndImage();
        }

        private void ButtonSaveIcon2_Click(object sender, RoutedEventArgs e)
        {
            Save2(FileType.Icon);
        }

        private void Save2(FileType type)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "Icons and Cursors|*.ico;*.cur|All files|*.*";
            sfd.Title = "Select a save destination";
            if (sfd.ShowDialog() == true)
            {
                Save(coCI, type, sfd.FileName);
            }
        }

        private void ButtonSaveCursor2_Click(object sender, RoutedEventArgs e)
        {
            Save2(FileType.Cursor);
        }

        #endregion


    }
}
