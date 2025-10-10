using System;
using System.IO;
using System.IO.Compression;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Processing module that compresses and decompresses data using GZip.
    /// </summary>
    public sealed class GzipProcessingModule : IProcessingModule
    {
        private const int BufferSize = 81920;

        /// <inheritdoc />
        public byte[] Process(byte[] input)
        {
            if (input == null)
            {
                throw new SaveSystemException("GZip processor cannot compress null input.");
            }

            try
            {
                using var output = new MemoryStream();
                using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
                {
                    gzip.Write(input, 0, input.Length);
                }

                return output.ToArray();
            }
            catch (Exception ex)
            {
                throw new SaveSystemException("Failed to compress data using GZip.", ex);
            }
        }

        /// <inheritdoc />
        public byte[] Reverse(byte[] input)
        {
            if (input == null)
            {
                throw new SaveSystemException("GZip processor cannot decompress null input.");
            }

            try
            {
                using var compressed = new MemoryStream(input);
                using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
                using var output = new MemoryStream();

                gzip.CopyTo(output, BufferSize);
                return output.ToArray();
            }
            catch (Exception ex)
            {
                throw new SaveSystemException("Failed to decompress data using GZip.", ex);
            }
        }
    }
}
