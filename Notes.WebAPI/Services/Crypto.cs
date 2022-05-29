using System;
using System.IO;
using System.Security.Cryptography;
using Amazon.DynamoDBv2.DataModel;

namespace Notes.WebAPI.Services
{
    public class Crypto
    {
        private readonly IDynamoDBContext _context;

        public Crypto(IDynamoDBContext context)
        {
            _context = context;
        }

        public string Encode(byte[] bytes) => Convert.ToBase64String(bytes);
        public byte[] Decode(string data) => Convert.FromBase64String(data);

        public string Hash(params string[] data)
        {
            var hash = SHA256.Create();

            using var ms = new MemoryStream();
            using (var sw = new StreamWriter(ms))
            {
                foreach (var datum in data)
                {
                    sw.Write(datum);
                }
            }

            var bytes = ms.ToArray();

            var hashed = hash.ComputeHash(bytes);

            return Encode(hashed);
        }

        public string Encrypt(string key, out string iv, string data)
        {
            using var aes = Aes.Create();
            var aesKey = Decode(key);
            aes.Key = aesKey;
            iv = Encode(aes.IV);

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                using var sw = new StreamWriter(cs);
                sw.Write(data);
            }

            return Encode(ms.ToArray());
        }

        public string Decrypt(string key, string iv, string data)
        {
            using var aes = Aes.Create();
            aes.Key = Decode(key);
            aes.IV = Decode(iv);

            using var ms = new MemoryStream(Decode(data));
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            
            return sr.ReadToEnd();
        }
    }
}