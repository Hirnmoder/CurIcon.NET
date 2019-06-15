using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurIconNET
{
    public enum FileType
    {
        Undefined = 0,
        Icon = 1,
        Cursor = 2,
    }


    public abstract class CurIcon
    {
        /// <summary>
        /// States whether this instance is an icon or a cursor.
        /// </summary>
        public FileType Type { get; protected set; }


        /// <summary>
        /// Creates an empty <see cref="CurIcon"/> object with the given <see cref="FileType"/>.
        /// </summary>
        /// <param name="fileType">The desired <see cref="FileType"/>.</param>
        public CurIcon(FileType fileType)
        {
            this.Type = fileType;
        }

        /// <summary>
        /// Saves this instance of <see cref="CurIcon"/> with the <see cref="FileType"/> specified in the <see cref="Type"/> property.
        /// </summary>
        /// <param name="outStream">The output stream.</param>
        public void Save(Stream outStream)
        {
            Save(outStream, this.Type);
        }

        /// <summary>
        /// Saves this instance of <see cref="CurIcon"/> with the given <see cref="FileType"/>.
        /// </summary>
        /// <param name="outStream">The output stream.</param>
        /// <param name="desiredFileType">The desired <see cref="FileType"/>.</param>
        /// <param name="throwOnTooManyFrames">Set this to true if an Exception should be raised if there are more than 255 frames to save.</param>
        public abstract void Save(Stream outStream, FileType desiredFileType, bool throwOnTooManyFrames = false);

    }
}
