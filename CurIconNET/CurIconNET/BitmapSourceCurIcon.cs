using CurIconNET.Internals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CurIconNET
{
    public class BitmapSourceCurIcon : CurIcon
    {
        private const string ERR_FILETYPE_NOT_RECOGNIZED = "FileType cannot be recognized. Maybe the given stream is not an icon and not a cursor.";
        private const string ERR_FIRST_TWO_BYTES_NOT_ZERO = "First two bytes must be zero.";
        private const string ERR_NUMBER_OF_FRAMES_MISMATCH = "Number of frames (bytes at position 5 and 6) do not match the decoder's count of frames.";
        private const string ERR_STREAM_NOT_WRITEABLE = "Given stream is not writeable.";
        private const string ERR_FILETYPE_UNDEFINED = "desiredFileType must not be FileType.Undefined";
        private const string ERR_TOO_MANY_FRAMES = "There are too many frames to save. Maximum is 65535.";

        private const uint HEADER_SIZE = 6;
        private const uint DIR_ENTRY_SIZE = 16;

        private List<PngFrame> _Frames;


        /// <summary>
        /// Creates a <see cref="BitmapSourceCurIcon"/> from a given stream.
        /// </summary>
        /// <param name="constructionStream">Construction stream.</param>
        /// <param name="throwOnFramecountMismatch">Set this to true if an Exception should be raised if the expected count of frames and the decoder's count of frames mismatch.</param>
        /// <exception cref="IOException" />
        /// <exception cref="NotSupportedException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="FormatException" />
        /// <exception cref="ArgumentNullException" />
        public BitmapSourceCurIcon(Stream constructionStream, bool throwOnFramecountMismatch = false) : base(FileType.Undefined)
        {
            Construct(constructionStream, throwOnFramecountMismatch);
        }

        /// <summary>
        /// Creates a <see cref="BitmapSourceCurIcon"/> from a given byte array.
        /// </summary>
        /// <param name="constructionBytes">Byte array used for construction.</param>
        /// <param name="throwOnFramecountMismatch">Set this to true if an Exception should be raised if the expected count of frames and the decoder's count of frames mismatch.</param>
        /// <exception cref="IOException" />
        /// <exception cref="NotSupportedException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="FormatException" />
        /// <exception cref="ArgumentNullException" />
        public BitmapSourceCurIcon(byte[] constructionBytes, bool throwOnFramecountMismatch = false) : base(FileType.Undefined)
        {
            using(var ms = new MemoryStream(constructionBytes))
            {
                Construct(ms);
            }
        }


        /// <summary>
        /// Creates a BitmapSource from a given stream.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="throwOnFramecountMismatch"></param>
        /// <exception cref="IOException" />
        /// <exception cref="NotSupportedException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="FormatException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        private void Construct(Stream s, bool throwOnFramecountMismatch = false)
        {
            long initPos = s.Position;

            int a = s.ReadByte();
            if (a != 0)
                throw new FormatException(ERR_FIRST_TWO_BYTES_NOT_ZERO);
            a = s.ReadByte();
            if (a != 0)
                throw new FormatException(ERR_FIRST_TWO_BYTES_NOT_ZERO);

            s.Position = initPos + 2;
            int typeFlag = s.ReadByte();
            switch (typeFlag)
            {
                case 1:
                    this.Type = FileType.Icon;
                    break;
                case 2:
                    this.Type = FileType.Cursor;
                    break;
                default:
                    throw new FormatException(ERR_FILETYPE_NOT_RECOGNIZED);
            }

            a = s.ReadByte();
            if (a != 0)
                throw new FormatException(ERR_FILETYPE_NOT_RECOGNIZED);

            s.Position = initPos;

            var decoder = BitmapDecoder.Create(s, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

            if(this.Type == FileType.Icon)
            {
                this._Frames = decoder.Frames.Select(f => new PngFrame(f, 0, 0)).ToList();
            }
            else if(this.Type == FileType.Cursor)
            {
                this._Frames = new List<PngFrame>();

                s.Position = initPos + 4;
                int loC = s.ReadByte();
                int hiC = s.ReadByte();

                if (loC == -1 || hiC == -1) throw new FormatException(ERR_NUMBER_OF_FRAMES_MISMATCH);

                uint expectedFrameCount = ((uint)hiC >> 8) + (uint)loC;
                if (expectedFrameCount != decoder.Frames.Count && throwOnFramecountMismatch) throw new InvalidOperationException(ERR_NUMBER_OF_FRAMES_MISMATCH);

                uint frameCount = Math.Min((uint)decoder.Frames.Count, expectedFrameCount);

                for (int i = 0; i < frameCount; i++)
                {
                    uint offset = HEADER_SIZE + (uint)i * DIR_ENTRY_SIZE + 4;
                    s.Position = initPos + offset;
                    int loL = s.ReadByte();
                    int hiL = s.ReadByte();
                    int loT = s.ReadByte();
                    int hiT = s.ReadByte();

                    ushort left = 0;
                    ushort top = 0;

                    if (loL != -1 && hiL != -1 && loT != -1 && hiT != -1)
                    {
                        left = (ushort)((hiL >> 8) + loL);
                        top = (ushort)((hiT >> 8) + loT);
                    }

                    this._Frames.Add(new PngFrame(decoder.Frames[i], left, top));
                }
            }
        }


        /// <summary>
        /// Saves this instance of <see cref="BitmapSourceCurIcon"/> with the given <see cref="FileType"/>.
        /// </summary>
        /// <remarks><see cref="BitmapFrame"/>s that are too big (greater than 256x256 Pixels) are cropped.</remarks>
        /// <param name="outStream">The output stream.</param>
        /// <param name="desiredFileType">The desired <see cref="FileType"/>.</param>
        /// <param name="throwOnTooManyFrames">Set this to true if an Exception should be raised if there are more than 255 frames to save.</param>
        /// <exception cref="NotSupportedException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="ArgumentException" />
        public override void Save(Stream outStream, FileType desiredFileType, bool throwOnTooManyFrames = false)
        {
            if (outStream == null) throw new ArgumentNullException(nameof(outStream));
            if (!outStream.CanWrite) throw new NotSupportedException(ERR_STREAM_NOT_WRITEABLE);
            if (desiredFileType == FileType.Undefined) throw new ArgumentException(ERR_FILETYPE_UNDEFINED);
            if ((desiredFileType == FileType.Cursor || desiredFileType == FileType.Icon) && this._Frames.Count > ushort.MaxValue && throwOnTooManyFrames) throw new InvalidOperationException(ERR_TOO_MANY_FRAMES);

            int frameCount = Math.Min(this._Frames.Count, ushort.MaxValue);

            ushort fileTypeFlag = (ushort)(desiredFileType == FileType.Icon ? 1 : 2);

            for(int i = 0; i < frameCount; i++)
            {
                this._Frames[i].CreateBytes();
            }

            uint addedSizes = 0;

            // Calculate Offsets
            for (int i = 0; i < frameCount; i++)
            {
                uint offset = HEADER_SIZE + (uint)frameCount * DIR_ENTRY_SIZE + addedSizes;
                addedSizes += (uint)_Frames[i].LongLength;
                _Frames[i].Offset = offset;
            }

            // Little-Endian
            using (var bw = new BinaryWriter(outStream, Encoding.UTF8, true))
            {
                bw.Write((ushort)0);
                bw.Write((ushort)fileTypeFlag);
                bw.Write((ushort)frameCount);
                for (int i = 0; i < frameCount; i++)
                {
                    _Frames[i].WriteHeader(bw, desiredFileType == FileType.Cursor);
                }
                for (int i = 0; i < frameCount; i++)
                {
                    _Frames[i].WriteBody(bw);
                }
            }
        }

        public int FrameCount => this._Frames.Count;

        public BitmapSource this[int index]
        {
            get
            {
                return GetFrame(index);
            }
        }



        public BitmapSource GetFrame(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (index >= FrameCount) throw new ArgumentOutOfRangeException(nameof(index));
            return this._Frames[index].BitmapFrame;
        }

        public PngFrame GetPngFrame(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (index >= FrameCount) throw new ArgumentOutOfRangeException(nameof(index));
            return this._Frames[index];
        }
    }
}
