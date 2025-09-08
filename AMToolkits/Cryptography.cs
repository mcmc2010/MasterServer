

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

    #region RSA
    public class RSA
    {

        public static bool RSA2SignData(string text, string private_key,
                                out byte[]? output_buffer)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(text);

            // 1. 加载私钥到 RSA 实例
            System.Security.Cryptography.RSA rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportFromPem(private_key.ToCharArray()); // .NET 6+ 直接支持 PEM 导入
            return RSA2SignData(buffer, rsa, out output_buffer);
        }

        public static bool RSA2SignData(string text, System.Security.Cryptography.RSA? rsa,
                                out byte[]? output_buffer)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(text);
            return RSA2SignData(buffer, rsa, out output_buffer);
        }

        public static bool RSA2SignData(byte[] buffer, System.Security.Cryptography.RSA? rsa,
                                out byte[]? output_buffer)
        {
            output_buffer = null;
            if (rsa == null)
            {
                return false;
            }

            try
            {
                byte[] signature_data = rsa.SignData(
                    buffer,
                    System.Security.Cryptography.HashAlgorithmName.SHA256,
                    System.Security.Cryptography.RSASignaturePadding.Pkcs1 // 支付宝要求 PKCS#1 填充
                );

                output_buffer = signature_data;
                return true;
            }
            catch (Exception e)
            {
                output_buffer = null;
                System.Console.WriteLine(e.Message);
                return false;
            }
        }

        public static bool RSA2VerifyData(string key, string text, byte[] sign_data)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(text);
            // 1. 加载私钥到 RSA 实例
            System.Security.Cryptography.RSA rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportFromPem(key.ToCharArray()); // .NET 6+ 直接支持 PEM 导入
            return RSA2VerifyData(rsa, buffer, sign_data);
        }


        public static bool RSA2VerifyData(System.Security.Cryptography.RSA? rsa, string text, byte[] sign_data)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(text);
            return RSA2VerifyData(rsa, buffer, sign_data);
        }

        public static bool RSA2VerifyData(System.Security.Cryptography.RSA? rsa, byte[] data, byte[] sign_data)
        {
            if (rsa == null)
            {
                return false;
            }

            try
            {
                // 计算数据的 SHA256 哈希值
                System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
                byte[] hash = sha256.ComputeHash(data);

                sha256.Dispose();

                // 使用公钥验证签名
                return rsa.VerifyHash(
                    hash,
                    sign_data,
                    System.Security.Cryptography.HashAlgorithmName.SHA256,
                    System.Security.Cryptography.RSASignaturePadding.Pkcs1
                );
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
                return false;
            }
        }
    }
    #endregion
}