using System;
using System.IO;
using System.Security.Cryptography;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Processing module that encrypts and decrypts data using AES.
    /// </summary>
    public sealed class AesProcessingModule : IProcessingModule
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        /// <summary>
        /// Initializes a new instance of the <see cref="AesProcessingModule"/> class.
        /// </summary>
        /// <param name="key">AES key (128, 192, or 256 bits).</param>
        /// <param name="iv">Initialization vector (128 bits).</param>
        public AesProcessingModule(byte[] key, byte[] iv)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _iv = iv ?? throw new ArgumentNullException(nameof(iv));

            if (_key.Length != 16 && _key.Length != 24 && _key.Length != 32)
            {
                throw new SaveSystemException("AES key must be 128, 192, or 256 bits long.");
            }

            if (_iv.Length != 16)
            {
                throw new SaveSystemException("AES IV must be 128 bits long.");
            }
        }

        /// <inheritdoc />
        public byte[] Process(byte[] input)
        {
            if (input == null)
            {
                throw new SaveSystemException("AES processor cannot encrypt null input.");
            }

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                return Transform(input, encryptor);
            }
            catch (CryptographicException ex)
            {
                throw new SaveSystemException("Failed to encrypt data using AES.", ex);
            }
        }

        /// <inheritdoc />
        public byte[] Reverse(byte[] input)
        {
            if (input == null)
            {
                throw new SaveSystemException("AES processor cannot decrypt null input.");
            }

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                return Transform(input, decryptor);
            }
            catch (CryptographicException ex)
            {
                throw new SaveSystemException("Failed to decrypt data using AES.", ex);
            }
        }

        /// <summary>
        /// Executes the provided crypto transform against the input bytes.
        /// </summary>
        /// <param name="input">Input payload.</param>
        /// <param name="transform">Cryptographic transform to apply.</param>
        /// <returns>Resulting transformed bytes.</returns>
        private static byte[] Transform(byte[] input, ICryptoTransform transform)
        {
            using var inputStream = new MemoryStream(input);
            using var cryptoStream = new CryptoStream(inputStream, transform, CryptoStreamMode.Read);
            using var outputStream = new MemoryStream();
            cryptoStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
    }
}
