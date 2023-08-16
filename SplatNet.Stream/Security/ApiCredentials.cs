using Newtonsoft.Json;
using SplatNet.Stream.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatNet.Stream.Security
{
    public class ApiCredentials
    {
        private static readonly byte[] s_key = new byte[72]
        {
            0x98,0x92,0xB2,0x65,0x26,0x26,0x64,0x88,0x5E,0xBB,0x83,0x96,
            0x9F,0x6A,0xE0,0x95,0x37,0x0F,0x8A,0x1C,0x7E,0x91,0x98,0x96,
            0x64,0x5B,0xFB,0x0E,0x67,0x2D,0xA7,0x55,0x20,0x2D,0xDA,0x42,
            0x73,0xCE,0x52,0x21,0x11,0xCC,0xCA,0x18,0x75,0x33,0xB6,0x22,
            0xF6,0x40,0x44,0x7D,0x16,0x26,0xC4,0x07,0xF8,0x1B,0x00,0xC9,
            0x29,0x55,0x20,0x30,0x13,0x2D,0x76,0x71,0x8E,0x9E,0x21,0x68
        };

        public string Language { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public string GToken { get; set; } = string.Empty;
        public string BulletToken { get; set; } = string.Empty;

        private readonly string serviceName;

        public ApiCredentials(string serviceName)
        {
            this.serviceName = serviceName;
            string filePath = Path.Combine(IOUtil.AppDirectory, $"{serviceName}-service.json");
            if (!File.Exists(filePath))
            {
                return;
            }

            CredStore credStore = JsonConvert.DeserializeObject<CredStore>(File.ReadAllText(filePath));
            this.Language = credStore.Language;
            this.Country = credStore.Country;
            this.SessionToken = DecryptPassword(credStore.EncryptedSessionToken);
            this.GToken = DecryptPassword(credStore.EncryptedGToken);
            this.BulletToken = DecryptPassword(credStore.EncryptedBulletToken);
        }

        public void Save()
        {
            string filePath = Path.Combine(IOUtil.AppDirectory, $"{this.serviceName}-service.json");
            string jsonText = JsonConvert.SerializeObject(new CredStore
            {
                Language = this.Language,
                Country = this.Country,
                EncryptedSessionToken = EncryptPassword(this.SessionToken),
                EncryptedGToken = EncryptPassword(this.GToken),
                EncryptedBulletToken = EncryptPassword(this.BulletToken)
            });
            File.WriteAllText(filePath, jsonText);
        }

        public void Forget()
        {
            string filePath = Path.Combine(IOUtil.AppDirectory, $"{this.serviceName}-service.json");
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        public bool IsValid()
        {
            return 
                !string.IsNullOrWhiteSpace(this.Language) &&
                !string.IsNullOrWhiteSpace(this.Country) &&
                !string.IsNullOrWhiteSpace(this.SessionToken) && 
                !string.IsNullOrWhiteSpace(this.GToken) && 
                !string.IsNullOrWhiteSpace(this.BulletToken);
        }

        private class CredStore
        {
            public string Language { get; set; }
            public string Country { get; set; }
            public string EncryptedSessionToken { get; set; }
            public string EncryptedGToken { get; set; }
            public string EncryptedBulletToken { get; set; }
        }

        private static char ToHex(byte nibble)
        {
            if (nibble < 10)
            {
                return (char)(nibble + '0');
            }
            else
            {
                return (char)((char)(nibble - 10) + 'a');
            }
        }

        private static byte FromHex(char c)
        {
            if (c >= '0' && c <= '9') return (byte)(c - '0');
            if (c >= 'a' && c <= 'f') return (byte)(c - 'a' + 10);
            if (c >= 'A' && c <= 'F') return (byte)(c - 'A' + 10);
            return 0;
        }

        private static string EncryptPassword(string plainText)
        {
            string cypherText = string.Empty;
            int i = 0;
            foreach (char c in plainText)
            {
                byte x = (byte)((byte)c ^ s_key[i++ % 72]);
                cypherText += ToHex((byte)(x >> 4));
                cypherText += ToHex((byte)(x & 0xF));
            }
            return cypherText;
        }

        private static string DecryptPassword(string cypherText)
        {
            string plainText = string.Empty;
            for (int i = 0; i < cypherText.Length - 1; i += 2)
            {
                byte c = (byte)(FromHex(cypherText[i]) << 4 | FromHex(cypherText[i + 1]));
                plainText += (char)(c ^ s_key[i / 2 % 72]);
            }
            return plainText;
        }
    }
}
