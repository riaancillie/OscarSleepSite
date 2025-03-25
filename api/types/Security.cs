using System.IO;
using System.Security.Cryptography;
using System.Text;


namespace oscarsleep
{
    public class SymmetricEncryption
    {
        public static string Encrypt(string plainText, string passphrase)
        {
            byte[] key, iv;
            DeriveKeyAndIV(passphrase, out key, out iv);
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new())
                {
                    using (CryptoStream cryptoStream = new((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new((Stream)cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);
        }

        public static string Decrypt(string cipherText, string passphrase)
        {
            byte[] key, iv;
            DeriveKeyAndIV(passphrase, out key, out iv);

            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new(buffer))
                {
                    using (CryptoStream cryptoStream = new((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new((Stream)cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }


        }

        private static void DeriveKeyAndIV(string passphrase, out byte[] key, out byte[] iv)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(passphrase));
                key = new byte[32];
                iv = new byte[16];
                Array.Copy(hash, key, Math.Min(hash.Length, key.Length));
                Array.Copy(hash, iv, Math.Min(hash.Length, iv.Length));
            }
        }        
    }

    
}