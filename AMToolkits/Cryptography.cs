using System.Security.Cryptography;

namespace AMToolkits
{
    #region Hash
    public static class Hash
    {
        public static string MD5String(string text)
        {
            MD5 md5 = MD5.Create();
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(text);
            byte[] digest = md5.ComputeHash(buffer);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < digest.Length; i++)
            {
                sb.Append(digest[i].ToString("x2"));
            }
            return sb.ToString().ToUpper();
        }


        public static string SHA256String(string text)
        {
            SHA256 sha = SHA256.Create();
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(text);
            byte[] digest = sha.ComputeHash(buffer);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < digest.Length; i++)
            {
                sb.Append(digest[i].ToString("x2"));
            }
            return sb.ToString().ToUpper();
        }
    }
    #endregion

    #region AES
    public static class AESCrypto
    {
        public const string IV_NORMAL = "0123456789ABCDEF";
        public static byte[]? Encrypt(byte[] input, byte[] key, PaddingMode padding = PaddingMode.Zeros, byte[]? iv = null)
        {
            // 验证输入数据长度（仅对None填充需要）
            if (padding == PaddingMode.None && input.Length % 16 != 0)
            {
                //throw new ArgumentException("For None padding, input must be multiple of 16 bytes");
                return null;
            }

            byte[] vkey = new byte[32];
            Buffer.BlockCopy(key, 0, vkey, 0, System.Math.Min(key.Length, vkey.Length));

            // 创建AES加密器
            var aes = Aes.Create();
            aes.Key = vkey;
            aes.Padding = padding;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;

            try
            {
                //
                if (iv == null)
                {
                    iv = System.Text.Encoding.UTF8.GetBytes(IV_NORMAL);
                }
                aes.IV = iv;
                var encryptor = aes.CreateEncryptor();

                var ms = new MemoryStream();
                var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
                cs.Write(input, 0, input.Length);
                cs.FlushFinalBlock();

                byte[] output = ms.ToArray();
                return output;
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
                return null;
            }
            finally
            {
                if (aes != null)
                {
                    aes.Dispose();
                    aes = null;
                }
            }

        }

        public static byte[]? Decrypt(byte[] input, byte[] key, PaddingMode padding = PaddingMode.Zeros, byte[]? iv = null)
        {
#pragma warning disable CS0168
            // 验证输入数据长度（仅对None填充需要）
            if (padding == PaddingMode.None && input.Length % 16 != 0)
            {
                //throw new ArgumentException("For None padding, input must be multiple of 16 bytes");
                return null;
            }

            byte[] vkey = new byte[32];
            Buffer.BlockCopy(key, 0, vkey, 0, System.Math.Min(key.Length, vkey.Length));

            // 创建AES加密器
            var aes = Aes.Create();
            aes.Key = vkey;
            aes.Padding = padding;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;

            try
            {
                //
                if (iv == null)
                {
                    iv = System.Text.Encoding.UTF8.GetBytes(IV_NORMAL);
                }
                aes.IV = iv;
                var decryptor = aes.CreateDecryptor();

                var ms = new MemoryStream(input, 0, input.Length);
                var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                var rs = new MemoryStream();
                cs.CopyTo(rs);

                byte[] output = rs.ToArray();
                return output;
            }
            catch (Exception e)
            {
                //System.Console.WriteLine(e.Message);
                return null;
            }
            finally
            {
                if (aes != null)
                {
                    aes.Dispose();
                    aes = null;
                }
            }
#pragma warning restore CS0168
        }

        public static byte[]? Encrypt(string input, string key, PaddingMode padding = PaddingMode.Zeros,
                bool key_is_binary = false)
        {
            byte[]? key_buffer = System.Text.Encoding.UTF8.GetBytes(key);
            if (key_is_binary)
            {
                key_buffer = AMToolkits.Utils.ToHexBinary(key) ?? null;
            }
            if (key_buffer == null)
            {
                return null;
            }
            return Encrypt(System.Text.Encoding.UTF8.GetBytes(input), key_buffer, padding);
        }

        public static byte[]? Decrypt(byte[] input, string key, PaddingMode padding = PaddingMode.Zeros,
                bool key_is_binary = false)
        {
            byte[]? key_buffer = System.Text.Encoding.UTF8.GetBytes(key);
            if (key_is_binary)
            {
                key_buffer = AMToolkits.Utils.ToHexBinary(key) ?? null;
            }
            if (key_buffer == null)
            {
                return null;
            }
            return Decrypt(input, key_buffer, padding);
        }
    }
    #endregion
}