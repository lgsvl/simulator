using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

#if !NETSTANDARD1_6
using System.Security;
#endif

namespace Docker.DotNet.X509
{
    public static class RSAUtil
    {
        private const byte Padding = 0x00;

        public static X509Certificate2 GetCertFromPFX(string pfxFilePath, string password)
        {
            return new X509Certificate2(pfxFilePath, password);
        }

#if !NETSTANDARD1_6
        public static X509Certificate2 GetCertFromPFXSecure(string pfxFilePath, SecureString password)
        {
            return new X509Certificate2(pfxFilePath, password);
        }

        public static X509Certificate2 GetCertFromPEMFiles(string certFilePath, string keyFilePath)
        {
            var cert = new X509Certificate2(certFilePath);
            cert.PrivateKey = RSAUtil.ReadFromPemFile(keyFilePath);
            return cert;
        }
#endif

        private static RSACryptoServiceProvider ReadFromPemFile(string pemFilePath)
        {
            var allBytes = File.ReadAllBytes(pemFilePath);
            var mem = new MemoryStream(allBytes);
            var startIndex = 0;
            var endIndex = 0;

            using (var rdr = new BinaryReader(mem))
            {
                if (!TryReadUntil(rdr, "-----BEGIN RSA PRIVATE KEY-----"))
                {
                    throw new Exception("Invalid file format expected. No begin tag.");
                }

                startIndex = (int)(mem.Position);

                const string endTag = "-----END RSA PRIVATE KEY-----";
                if (!TryReadUntil(rdr, endTag))
                {
                    throw new Exception("Invalid file format expected. No end tag.");
                }

                endIndex = (int)(mem.Position - endTag.Length - 2);
            }

            // Convert the bytes from base64;
            var convertedBytes = Convert.FromBase64String(Encoding.UTF8.GetString(allBytes, startIndex, endIndex - startIndex));
            mem = new MemoryStream(convertedBytes);
            using (var rdr = new BinaryReader(mem))
            {
                var val = rdr.ReadUInt16();
                if (val != 0x8230)
                {
                    throw new Exception("Invalid byte ordering.");
                }

                // Discard the next bits of the version.
                rdr.ReadUInt32();
                if (rdr.ReadByte() != Padding)
                {
                    throw new InvalidDataException("Invalid ASN.1 format.");
                }

                var rsa = new RSAParameters()
                {
                    Modulus = rdr.ReadBytes(ReadIntegerCount(rdr)),
                    Exponent = rdr.ReadBytes(ReadIntegerCount(rdr)),
                    D = rdr.ReadBytes(ReadIntegerCount(rdr)),
                    P = rdr.ReadBytes(ReadIntegerCount(rdr)),
                    Q = rdr.ReadBytes(ReadIntegerCount(rdr)),
                    DP = rdr.ReadBytes(ReadIntegerCount(rdr)),
                    DQ = rdr.ReadBytes(ReadIntegerCount(rdr)),
                    InverseQ = rdr.ReadBytes(ReadIntegerCount(rdr))
                };

                // Use "1" to indicate RSA.
                var csp = new CspParameters(1)
                {

                    // Set the KeyContainerName so that native code that looks up the private key
                    // can find it. This produces a keyset file on disk as a side effect.
                    KeyContainerName = pemFilePath
                };
                var rsaProvider = new RSACryptoServiceProvider(csp)
                {

                    // Setting to false makes sure the keystore file will be cleaned up
                    // when the current process exits.
                    PersistKeyInCsp = false
                };

                // Import the private key into the keyset.
                rsaProvider.ImportParameters(rsa);

                return rsaProvider;
            }
        }

        /// <summary>
        /// Reads an integer count encoding in DER ASN.1 format.
        /// <summary>
        private static int ReadIntegerCount(BinaryReader rdr)
        {
            const byte highBitOctet = 0x80;
            const byte ASN1_INTEGER = 0x02;

            if (rdr.ReadByte() != ASN1_INTEGER)
            {
                throw new Exception("Integer tag expected.");
            }

            int count = 0;
            var val = rdr.ReadByte();
            if ((val & highBitOctet) == highBitOctet)
            {
                byte numOfOctets = (byte)(val - highBitOctet);
                if (numOfOctets > 4)
                {
                    throw new InvalidDataException("Too many octets.");
                }

                for (var i = 0; i < numOfOctets; i++)
                {
                    count <<= 8;
                    count += rdr.ReadByte();
                }
            }
            else
            {
                count = val;
            }

            while (rdr.ReadByte() == Padding)
            {
                count--;
            }

            // The last read was a valid byte. Go back here.
            rdr.BaseStream.Seek(-1, SeekOrigin.Current);

            return count;
        }

        /// <summary>
        /// Reads until the matching PEM tag is found.
        /// <summary>
        private static bool TryReadUntil(BinaryReader rdr, string tag)
        {
            char delim = '\n';
            char c;
            char[] line = new char[64];
            int index;

            try
            {
                do
                {
                    index = 0;
                    while ((c = rdr.ReadChar()) != delim)
                    {
                        if(c == '\r')
                        {
                            continue;
                        }
                        line[index] = c;
                        index++;
                    }
                } while (new string(line, 0, index) != tag);

                return true;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }
    }
}