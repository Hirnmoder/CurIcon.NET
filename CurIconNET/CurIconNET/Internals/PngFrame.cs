using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                if(_BitmapFrame == null && _PngFile != null)
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
                if(_PngFile == null && _BitmapFrame != null)
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

        public PngFrame(byte[] pngFile, int width, int height, ushort hLeft, ushort hTop)
        {
            this._PngFile = pngFile;

            if (width > 256) throw new ArgumentException(nameof(width) + " must be less or equal to 256.");
            if (height > 256) throw new ArgumentException(nameof(height) + " must be less or equal to 256.");
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
            if(this._PngFile == null || (left <= this.Width && top <= this.Height))
            {
                this.HLeft = left;
                this.HTop = top;
                return true;
            }
            return false;
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
                this._BitmapFrame =  BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
        }

        internal void WriteHeader(BinaryWriter bw, bool withHotspot)
        {
            bw.Write((byte)Width);
            bw.Write((byte)Height);
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((ushort)(withHotspot ? HLeft : 0));
            bw.Write((ushort)(withHotspot ? HTop: 0));
            bw.Write((uint)LongLength);
            bw.Write((uint)Offset);
        }

        internal void WriteBody(BinaryWriter bw)
        {
            bw.Write(PngFile);
        }
    }
}
