using System;
using System.Collections.Generic;

namespace Steganography
{
    public class CryptoGenerator
    {
        public List<int> generate(string password, int max)
        {
            int hash = makeHash(password);

            Random rand = new Random(hash);
            List<int> randInt = new List<int>();

            for (int i = 0, k = 0; i < (max / 2); i++)
            {
                bool equ = false;
                k = rand.Next(max);
                foreach (int ar in randInt)
                    if (k == ar) equ = true;
                if (!equ)
                    randInt.Add(k);
            }

            return randInt;
        }

        private int makeHash(string password)
        {
            int p = 0;
            int i = 1;
            int p1 = 0;
  
            foreach (char element in password)
            {
                p += i * (int)element;
                i++;
            }

            p1 = p % 1327;
            if (p1 == 0)
                p1 = p % 1361;


            return p1;
        }
    }
}
