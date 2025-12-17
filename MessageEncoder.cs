using System;
using System.Collections.Generic;
using System.Collections;

namespace Steganography
{
    // переписать вообще к херам этот класс. Кодировать нужно байты, а вся эта дрочь с русским и английским языком нафиг не нужна [28.10.2025]
    class MessageEncoder
    {
        private Dictionary<char, BitArray> codeTable; // переменная, в которой будет храниться таблица перекодировки
        // символы из которых составляется codeTable
        private const string englishLetters = "abcdefghijklmnopqrstuvwxyz";
        private const string numbers = "0123456789";
        private const string punctuation = ",;:.?!'-\"";
        private const string engSpecials = "&[@](+=<*$>/)_% ";
        private const string russianLetters = "абвгдеёжзийклмнопрстуфхцчш";
        private const string rusSpecials = "щъ@ы(ьэю*$я/)_% "; 

        // целые массивы магических цифр. Я уже и не помню, что все эти числа обозначают и зачем они здесь вообще [28.10.2025]

        // коды символов
        private byte[] letCodes = { 1, 3, 9, 25, 17, 11, 27, 19,
                                 10, 26, 5, 7, 13, 29, 21, 15,
                                 31, 23, 14, 30, 37, 39, 58, 45,
                                 61, 53 };
        private byte[] numCodes = { 17, 35, 41, 57, 49, 43, 59, 51, 42, 63 };
        private byte[] punctuationCodes = { 2, 6, 18, 50, 34, 60, 4, 36, 8};
        private byte[] specialCodes = { 47, 55, 46, 62, 22, 54, 38, 20, 52, 12, 28, 44, 24, 56, 40, 0};
        private bool _lang;
        private string messageAscii;
        private BitArray messageEncoded;

        public MessageEncoder(bool lang)
        {
            _lang = lang;
            string keys;
            byte[] values = new byte [63];
            Array.Copy(letCodes, values, 26);
            Array.Copy(numCodes, 0, values, 27, 10);
            Array.Copy(punctuationCodes, 0, values, 37, 9);
            Array.Copy(specialCodes, 0, values, 46, 16);
            codeTable = new Dictionary<char, BitArray>();

            if (_lang)
            {
                keys = englishLetters + numbers + punctuation + engSpecials;
            }
            else
            {
                keys = russianLetters + numbers + punctuation + rusSpecials;
            }

            for (int i = 0; i < 61; i++)
            {
                codeTable.Add(keys[i], byteToBit(values[i]));
            }
            
        }
        public BitArray Encode(string input)
        {
            char sym;
            BitArray result = new BitArray(input.Length*6);
            result.SetAll(false);
            BitArray codedSym = new BitArray(6);
            for(int i = 0; i < input.Length; i++)
            {
                sym = input[i];
                codedSym = codeTable[sym];
                for(int j=0; j<6; j++)
                {
                    result.Set((i * 6 + j), codedSym[j]);
                }
            }

            // вычисляем размер поля size(в байтах)
            double size = Math.Log(result.Count, 2) / 8;
            int sizeBits = (int)(Math.Round(size) * 8);

            // добавляем в начало строки размер 
            int mesLength = result.Count;

            int totalLengthInBits = 6 + sizeBits + mesLength;

            //размер сообщения в последовательность бит
            BitArray bitArrayLength = new BitArray(sizeBits);
            String str = Convert.ToString(totalLengthInBits, 2); //размер сообщения в двоичной с/с в текстовом представлении

            //инициализация bitArrayLength
            for (int i = str.Length - 1, j = 0; i >= 0; i--, j++)
            {
                if (str[i] == '0') bitArrayLength[j] = false;
                else bitArrayLength[j] = true;
            }

            BitArray baFlags = new BitArray(6);
            
            // записываем длину поля size в байтах в поле флагов
            String flagsStr = Convert.ToString((int)Math.Round(size), 2);
            while(flagsStr.Length < 6)
            {
                flagsStr = "0" + flagsStr;
            }

            for (int i = flagsStr.Length - 1; i >= 0; i--)
            {
                if (flagsStr[i] == '0') baFlags[i] = false;
                else baFlags[i] = true;
            }

            // самый первый байт - язык, 0 для en, 1 для ru
            if (_lang)
            {
                baFlags.Set(0, false);
            }
            else
            {
                baFlags.Set(0, true);
            }

            //склеиваем массивы бит в один
            BitArray finalArr = new BitArray(totalLengthInBits);

            for (int i = 0; i < baFlags.Length; i++)
            {
                finalArr[i] = baFlags[i];
            }
            for (int i = baFlags.Length; i < baFlags.Length + bitArrayLength.Length; i++)
            {
                finalArr[i] = bitArrayLength[i- baFlags.Length];
            }
            for (int i = baFlags.Length + bitArrayLength.Length; i < finalArr.Length; i++)
            {
                finalArr[i] = result[i - (baFlags.Length + bitArrayLength.Length)];
            }

            return finalArr;
        }

        // где код этого метода потерялся??? и как тогда декодируется вообще?
        public string Decode(BitArray input)
        {

            return messageAscii;
        }

        private BitArray byteToBit(byte b)
        {
            BitArray ret = new BitArray(6);
            int del = 32; // 100000 в двоичном представлении
            int rez = 0; 
            for(int i=0; i<6; i++)
            {
                rez = b & del;
                if(rez == 0)
                {
                    ret.Set(i, false);
                }
                else
                {
                    ret.Set(i, true);
                }
                del = del / 2;
            }

            return ret;
        }
        private byte[] bitToByte(BitArray btA)
        {
            byte[] bt = new byte[btA.Length / 8];
            BitArray bf = new BitArray(8);

            for (int i = 0, j = 0, k = 0; i < btA.Count; i++, j++)
            {
                if (j != 7)
                    bf[j] = btA[i];
                else
                {
                    bf[j] = btA[i];
                    bt[k] = (byte)bitToInt(bf);
                    k++;
                    j = -1;
                }
            }
           
            return bt;
        }
        private int bitToInt(BitArray bitA)
        {
            int result = 0;
            if (bitA.Count > 0)
                for (int i = 0; i < bitA.Count; i++)
                {
                    if (bitA.Get(i))
                        result = result + (int)Math.Pow(2, i);
                }
            return result;
        }
        ~MessageEncoder()
        {
            codeTable.Clear();
        }
    }
}
