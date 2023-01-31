using System;
using System.Security.Cryptography;

namespace EncryptFile.Models
{
    internal class Encrypt
    {
        // iterations must be at least 1000, we will use 2000;
        static int Iterations = 2000;
        static int len_AesKey = 32;
        static int len_AesIV = 16;
        private static string GenerateRandomString(int letterCount)
        {
            // Get the number of words and letters per word.
            int num_letters = letterCount;
            // Make an array of the letters we will use.
            char[] letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

            // Make a random number generator.
            Random rand = new();

            // Make the words.
            string word = "";
            for (int i = 0; i < num_letters; i++)
            {
                // Pick a random number between 0 and 25
                // to select a letter form the letters array.
                int letter_num = rand.Next(0, letters.Length - 1);

                // Append the letter.
                word += letters[letter_num];
            }
            return word;
        }
        public static Aes GenAes()
        {
            var encAlg = Aes.Create();

            string password = GenerateRandomString(len_AesKey);
            string salt = GenerateRandomString(len_AesIV);  // salt size must be at least 8 bytes, we use 16 bytes.
            var keyGenerator = new Rfc2898DeriveBytes(password, System.Text.Encoding.ASCII.GetBytes(salt), Iterations);
            encAlg.Key = keyGenerator.GetBytes(len_AesKey); // set a 256-bit key
            encAlg.IV = keyGenerator.GetBytes(len_AesIV); //set a 128-bit IV

            return encAlg;
        }
    }
}
