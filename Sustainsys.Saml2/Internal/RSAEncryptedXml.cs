﻿using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace Sustainsys.Saml2.Internal
{
    internal class RSAEncryptedXml : EncryptedXml
    {
        private readonly RSA privateKey;

        public RSAEncryptedXml(XmlDocument document, RSA rsaKey)
            : base(document)
        {
            privateKey = rsaKey;
        }

        // Try to decrypt the EncryptedKey by the given key
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "CipherReference"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "CipherData")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public override byte[] DecryptEncryptedKey(EncryptedKey encryptedKey)
        {
            if (encryptedKey == null)
            {
                throw new ArgumentNullException(nameof(encryptedKey));
            }

            if (encryptedKey.CipherData.CipherValue == null)
            {
                throw new NotImplementedException("Unable to decode CipherData of type \"CipherReference\".");
            }

            // use the key info
            if (privateKey == null)
            {
                return base.DecryptEncryptedKey(encryptedKey);
            }

            var useOaep = (encryptedKey.EncryptionMethod != null &&
                         encryptedKey.EncryptionMethod.KeyAlgorithm == XmlEncRSAOAEPUrl);

            return DecryptKey(encryptedKey.CipherData.CipherValue, privateKey, useOaep);
        }
        
        internal const string AesGcm128Identifier = "http://www.w3.org/2009/xmlenc11#aes128-gcm";

        /// <summary>
        /// AES-GCM Nonce size defined in https://www.w3.org/TR/xmlenc-core1/#sec-AES-GCM
        /// </summary>
        internal const int AesGcm128NonceSizeInBits = 96;
    
        public override byte[] GetDecryptionIV(EncryptedData encryptedData, string symmetricAlgorithmUri)
        {
          //adapted from https://github.com/dotnet/runtime/blob/a5192d4963531579166d7f43df2a1ed44a96900f/src/libraries/System.Security.Cryptography.Xml/src/System/Security/Cryptography/Xml/EncryptedXml.cs#L267
          if (symmetricAlgorithmUri == null && encryptedData.EncryptionMethod != null)
          {
              symmetricAlgorithmUri = encryptedData.EncryptionMethod.KeyAlgorithm;
          }

          if (symmetricAlgorithmUri == AesGcm128Identifier)
          {
              const int initBytesSize = AesGcm128NonceSizeInBits / 8;
              var iv = new byte[initBytesSize];
              var cipherValue = encryptedData.CipherData.CipherValue;
              Buffer.BlockCopy(cipherValue, 0, iv, 0, iv.Length);
              return iv;
          }
          return base.GetDecryptionIV(encryptedData, symmetricAlgorithmUri);
        }
    }
}
