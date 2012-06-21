using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;

namespace RunnersPal
{
    public class Secure
    {
        static Secure()
        {
            try
            {
                var secureSettings = MassiveDB.Current.FindSettings("Secure");
                algorithm = new RijndaelManaged();

                if (secureSettings.Count() != 2)
                {
                    MassiveDB.Current.RemoveDomainSettings("Secure");

                    algorithm.GenerateIV();
                    algorithm.GenerateKey();

                    MassiveDB.Current.InsertDomainSetting("Secure", "IV", Convert.ToBase64String(algorithm.IV));
                    MassiveDB.Current.InsertDomainSetting("Secure", "Key", Convert.ToBase64String(algorithm.Key));
                }
                else
                {
                    algorithm.IV = Convert.FromBase64String(secureSettings.Single(s => s.Identifier == "IV").SettingValue);
                    algorithm.Key = Convert.FromBase64String(secureSettings.Single(s => s.Identifier == "Key").SettingValue);
                }
            }
            catch (Exception ex)
            {
                algorithm = null;
                initException = ex;
            }
        }

        private static SymmetricAlgorithm algorithm;
        private static Exception initException;

        public static string DecryptValue(string encryptedValue, string defaultIfCannotDecrypt, bool rethrowExceptionIfCannotDecrypt = false)
        {
            if (algorithm == null || initException != null)
                throw new InvalidOperationException("An error occured during initialisation: ", initException);

            try
            {
                var inBytes = Convert.FromBase64String(encryptedValue);
                var xfrm = algorithm.CreateDecryptor();
                var outBlock = xfrm.TransformFinalBlock(inBytes, 0, inBytes.Length);
                return Encoding.Unicode.GetString(outBlock);
            }
            catch (Exception ex)
            {
                if (rethrowExceptionIfCannotDecrypt) throw;
                Trace.TraceWarning("Cannot decrypt value - possibly been tampered with...returning default", ex);
                return defaultIfCannotDecrypt;
            }
        }

        public static string EncryptValue(string value)
        {
            if (algorithm == null || initException != null)
                throw new InvalidOperationException("An error occured during initialisation: ", initException);

            var xfrm = algorithm.CreateEncryptor();
            var inBlock = Encoding.Unicode.GetBytes(value);
            var outBlock = xfrm.TransformFinalBlock(inBlock, 0, inBlock.Length);
            return Convert.ToBase64String(outBlock);
        }
    }
}
