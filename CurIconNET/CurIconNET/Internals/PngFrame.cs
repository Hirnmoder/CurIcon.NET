using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CurIconNET.Internals
{
    public class PngFrame
    {

        private BitmapFrame _BitmapFrame;
        public BitmapFrame BitmapFrame
        {
            get
            {
                if (_BitmapFrame == null && _PngFile != null)
                {
                    CreateFrame();
                }
                return _BitmapFrame;
            }
        }


        private byte[] _PngFile;
        public byte[] PngFile
        {
            get
            {
                if (_PngFile == null && _BitmapFrame != null)
                {
                    CreateBytes();
                }
                return _PngFile;
            }
        }

        public byte Width { get; private set; }
        public byte Height { get; private set; }

        public ushort HLeft { get; private set; }
        public ushort HTop { get; private set; }

        internal uint Offset { get; set; }

        /// <exception cref="NullReferenceException" />
        internal long LongLength => PngFile.LongLength;


        public PngFrame(BitmapFrame bitmapFrame, ushort hLeft, ushort hTop)
        {
            this._BitmapFrame = bitmapFrame;
            this.Width = 0;
            this.Height = 0;
            this.HLeft = hLeft;
            this.HTop = hTop;
            this.Offset = 0;
        }

        public PngFrame(byte[] pngFile, ushort hLeft, ushort hTop, bool autocrop)
        {
            this._PngFile = pngFile;
            CreateFrame();
            this._PngFile = null;

            int width = this.BitmapFrame.PixelWidth;
            int height = this.BitmapFrame.PixelHeight;

            bool needsCrop = false;

            if (width > 256)
            {
                needsCrop = true;
                if (!autocrop) throw new ArgumentException(nameof(width) + " must be less or equal to 256.");
            }
            if (height > 256)
            {
                needsCrop = true;
                if (!autocrop) throw new ArgumentException(nameof(height) + " must be less or equal to 256.");
            }

            if (needsCrop)
            {
                CreateBytes();
                this._BitmapFrame = null;
                CreateFrame();
            }

            width = this.BitmapFrame.PixelWidth;
            height = this.BitmapFrame.PixelHeight;

            this.Width = (byte)width;
            this.Height = (byte)height;

            this.HLeft = hLeft;
            this.HTop = hTop;

            this.Offset = 0;
        }

        /// <summary>
        /// Sets the hotspot point. This is relevant for cursors. Returns true if the action succeeded, otherwise false.
        /// </summary>
        /// <param name="left">X-coordinate of the hotspot, measured in pixels.</param>
        /// <param name="top">Y-coordinate of the hotspot, measured in pixels.</param>
        /// <returns>True if the action succeeded, otherwise false.</returns>
        public bool SetHotspot(ushort left, ushort top)
        {
            if (this._PngFile == null || (left <= this.Width && top <= this.Height))
            {
                this.HLeft = left;
                this.HTop = top;
                return true;
            }
            return false;
        }


        /// <summary>
        /// Rotates this <see cref="PngFrame"/> with a given rotation angle.
        /// </summary>
        /// <param name="rotationDegrees">Rotation angle in degrees.</param>
        /// <param name="scalingMode"><see cref="BitmapScalingMode"/> used for rendering the rotated frame. Defaults to <see cref="BitmapScalingMode.NearestNeighbor"/>.</param>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="ArgumentException" />
        /// <exception cref="NotSupportedException" />
        public void RotateFrame(double rotationDegrees, BitmapScalingMode scalingMode = BitmapScalingMode.NearestNeighbor)
        {
            var rotationRad = rotationDegrees / 180.0 * Math.PI;

            var frame = this.BitmapFrame;

            var transform = new RotateTransform(rotationDegrees);
            var w = frame.PixelWidth;
            var h = frame.PixelHeight;

            var newW = (int)(Math.Abs(w * Math.Sin(rotationRad)) + Math.Abs(h * Math.Cos(rotationRad)));
            var newH = (int)(Math.Abs(w * Math.Cos(rotationRad)) + Math.Abs(h * Math.Sin(rotationRad)));

            if (newW > 256) throw new InvalidOperationException(nameof(this.Width) + " is too big to be rotated.");
            if (newH > 256) throw new InvalidOperationException(nameof(this.Height) + " is too big to be rotated.");

            transform.CenterX = w / 2;
            transform.CenterY = h / 2;

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(transform);
            transformGroup.Children.Add(new TranslateTransform((newW - w) / 2.0, (newH - h) / 2));

            var origHotspot = new Point(this.HLeft, this.HTop);
            var transformedHotspot = transformGroup.Transform(origHotspot);

            var image = new Image()
            {
                Source = frame,
                RenderTransform = transformGroup,
                Width = w,
                Height = h,
                Stretch = Stretch.None,
                UseLayoutRounding = false,
                SnapsToDevicePixels = false,
            };
            RenderOptions.SetBitmapScalingMode(image, scalingMode);
            image.Arrange(new Rect(0, 0, w, h));
            var newRect = new Rect(0, 0, newW, newH);

            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                var vb = new VisualBrush(image);
                vb.TileMode = TileMode.None;
                vb.ViewboxUnits = BrushMappingMode.Absolute;
                vb.Viewbox = newRect;
                ctx.DrawRectangle(vb, null, newRect);
            }

            var resultSource = new RenderTargetBitmap(newW, newH, 96, 96, PixelFormats.Pbgra32);
            resultSource.Render(dv);

            this.HLeft = (ushort)transformedHotspot.X;
            this.HTop = (ushort)transformedHotspot.Y;
            this.Width = (byte)newW;
            this.Height = (byte)newH;

            this._BitmapFrame = BitmapFrame.Create(resultSource);
            this._PngFile = null;
        }

        public bool RotateFrameKeepSize(double rotationDegrees, BitmapScalingMode scalingMode = BitmapScalingMode.NearestNeighbor)
        {
            var clipped = false;

            var rotationRad = rotationDegrees / 180.0 * Math.PI;
            var frame = this.BitmapFrame;
            var w = frame.PixelWidth;
            var h = frame.PixelHeight;

            var imageRect = FindNonTransparentRect(frame);

            var newW = (int)(Math.Abs(imageRect.Width * Math.Sin(rotationRad)) + Math.Abs(imageRect.Height * Math.Cos(rotationRad)));
            var newH = (int)(Math.Abs(imageRect.Width * Math.Cos(rotationRad)) + Math.Abs(imageRect.Height * Math.Sin(rotationRad)));

            if(newW > w || newH > h)
                clipped = true;

            var transformGroup = new TransformGroup();
            var rotation = new RotateTransform(rotationDegrees);
            rotation.CenterX = w / 2.0;
            rotation.CenterY = h / 2.0;
            var translate = new TranslateTransform((w - imageRect.Width) / 2.0 - imageRect.X, (h - imageRect.Height) / 2.0 - imageRect.Y);
            transformGroup.Children.Add(translate);
            transformGroup.Children.Add(rotation);

            var origHotspot = new Point(this.HLeft, this.HTop);
            var transformedHotspot = transformGroup.Transform(origHotspot);

            var image = new Image()
            {
                Source = frame,
                RenderTransform = transformGroup,
                Width = w,
                Height = h,
                Stretch = Stretch.None,
                UseLayoutRounding = false,
                SnapsToDevicePixels = false,
            };
            RenderOptions.SetBitmapScalingMode(image, scalingMode);
            image.Arrange(new Rect(0, 0, w, h));
            var newRect = new Rect(0, 0, w, h);

            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                var vb = new VisualBrush(image);
                vb.TileMode = TileMode.None;
                vb.ViewboxUnits = BrushMappingMode.Absolute;
                vb.Viewbox = newRect;
                ctx.DrawRectangle(vb, null, newRect);
            }

            var resultSource = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            resultSource.Render(dv);

            this.HLeft = (ushort)transformedHotspot.X;
            this.HTop = (ushort)transformedHotspot.Y;
            this.Width = (byte)newW;
            this.Height = (byte)newH;

            this._BitmapFrame = BitmapFrame.Create(resultSource);
            this._PngFile = null;

            return clipped;
        }

        private Int32Rect FindNonTransparentRect(BitmapSource frame)
        {
            if (frame.Format != PixelFormats.Bgra32)
            {
                frame = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 1);
            }

            var width = frame.PixelWidth;
            var height = frame.PixelHeight;

            var bytesPerPixel = (frame.Format.BitsPerPixel + 7) / 8;
            var pixels = new byte[width * height * bytesPerPixel];
            frame.CopyPixels(pixels, bytesPerPixel * width, 0);

            var xMin = width;
            var xMax = 0;
            var yMin = height;
            var yMax = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!isTransparent(x, y))
                    {
                        xMin = Math.Min(xMin, x);
                        xMax = Math.Max(xMax, x);
                        yMin = Math.Min(yMin, y);
                        yMax = Math.Max(yMax, y);
                    }
                }
            }

            return new Int32Rect(xMin, yMin, xMax - xMin, yMax - yMin);

            bool isTransparent(int x, int y)
            {
                return pixels[(y * width + x) * bytesPerPixel + 3] == 0;
            }
        }

        private void CreateBytes()
        {
            if (this._BitmapFrame == null) throw new InvalidOperationException(nameof(this.BitmapFrame) + " is null.");

            var frame = this._BitmapFrame;
            // We need to crop
            if (frame.PixelWidth > 256 || frame.PixelHeight > 256)
            {
                int w = Math.Min(frame.PixelWidth, 256);
                int h = Math.Min(frame.PixelHeight, 256);

                frame = BitmapFrame.Create(new CroppedBitmap(frame, new System.Windows.Int32Rect(0, 0, w, h)));
            }

            this.Width = (byte)frame.PixelWidth;
            this.Height = (byte)frame.PixelHeight;

            var enc = new PngBitmapEncoder();
            using (var ms = new MemoryStream())
            {
                enc.Frames.Clear();
                enc.Frames.Add(frame);
                enc.Save(ms);
                this._PngFile = ms.ToArray();
            }
        }

        private void CreateFrame()
        {
            if (this._PngFile == null) throw new InvalidOperationException(nameof(this.PngFile) + " is null.");

            using (var ms = new MemoryStream(this._PngFile))
            {
                this._BitmapFrame = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
        }

        internal void WriteHeader(BinaryWriter bw, bool withHotspot)
        {
            bw.Write((byte)Width);
            bw.Write((byte)Height);
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((ushort)(withHotspot ? HLeft : 0));
            bw.Write((ushort)(withHotspot ? HTop : 0));
            bw.Write((uint)LongLength);
            bw.Write((uint)Offset);
        }

        internal void WriteBody(BinaryWriter bw)
        {
            bw.Write(PngFile);
        }
    }
}
