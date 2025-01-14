// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    [Obsolete(Obsoletions.DerivedCryptographicTypesMessage, DiagnosticId = Obsoletions.DerivedCryptographicTypesDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class DESCryptoServiceProvider : DES
    {
        private const int BitsPerByte = 8;

        public DESCryptoServiceProvider()
        {
            FeedbackSizeValue = 8;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "This is the implementation of DES")]
        public override void GenerateKey()
        {
            var key = new byte[8];
            RandomNumberGenerator.Fill(key);
            // Never hand back a weak or semi-weak key
            while (IsWeakKey(key) || IsSemiWeakKey(key))
            {
                RandomNumberGenerator.Fill(key);
            }
            KeyValue = key;
        }

        public override void GenerateIV()
        {
            IVValue = RandomNumberGenerator.GetBytes(8);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "This is the implementation of DES")]
        public override ICryptoTransform CreateDecryptor()
        {
            return CreateTransform(Key, IV, encrypting: false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "This is the implementation of DES")]
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV == null ? null : rgbIV.CloneByteArray(), encrypting: false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "This is the implementation of DES")]
        public override ICryptoTransform CreateEncryptor()
        {
            return CreateTransform(Key, IV, encrypting: true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "This is the implementation of DES")]
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV == null ? null : rgbIV.CloneByteArray(), encrypting: true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "This is the implementation of DES")]
        private ICryptoTransform CreateTransform(byte[] rgbKey, byte[]? rgbIV, bool encrypting)
        {
            // note: rgbIV is guaranteed to be cloned before this method, so no need to clone it again

            if (rgbKey == null)
                throw new ArgumentNullException(nameof(rgbKey));

            long keySize = rgbKey.Length * (long)BitsPerByte;
            if (keySize > int.MaxValue || !((int)keySize).IsLegalSize(LegalKeySizes))
                throw new ArgumentException(SR.Cryptography_InvalidKeySize, nameof(rgbKey));

            if (IsWeakKey(rgbKey))
                throw new CryptographicException(SR.Cryptography_InvalidKey_Weak, "DES");
            if (IsSemiWeakKey(rgbKey))
                throw new CryptographicException(SR.Cryptography_InvalidKey_SemiWeak, "DES");

            if (rgbIV == null)
            {
                if (Mode.UsesIv())
                {
                    rgbIV = RandomNumberGenerator.GetBytes(8);
                }
            }
            else
            {

                // We truncate IV's that are longer than the block size to 8 bytes : this is
                // done to maintain backward .NET Framework compatibility with the behavior shipped in V1.x.
                // The call to set the IV in CryptoAPI will ignore any bytes after the first 8
                // bytes. We'll still reject IV's that are shorter than the block size though.
                if (rgbIV.Length < 8)
                    throw new CryptographicException(SR.Cryptography_InvalidIVSize);
            }

            BasicSymmetricCipher cipher = new BasicSymmetricCipherCsp(
                CapiHelper.CALG_DES,
                Mode,
                BlockSize / BitsPerByte,
                rgbKey,
                addNoSaltFlag: false,
                rgbIV,
                encrypting,
                FeedbackSize,
                this.GetPaddingSize(Mode, FeedbackSize));
            return UniversalCryptoTransform.Create(Padding, cipher, encrypting);
        }
    }
}
