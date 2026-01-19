using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace Steganography
{
    // TODO:
    // Переписать этот класс так, чтобы он вызывал generator, encoder и stAnalyzer, а не делал всё сам.
    // Слишком огромный класс, получился таким из-за того, что переносил всю логику из класса окна.
    
    class BusinessLogic
    {
        private double mse = 0;

        // пока так передаётся, потом надо нормальный DI сделать
        public BitmapSource bSource { get; set; }
        public int maxLengthMessage { get; set; } // максимальная возможная длина сообщения для данного изображения
        public int lengthSize { get; set; }

        public double MSE = 0; // Mean Squared Error
        public double PSNR = 0; // Peak Signal to Noise Ratio

        // не знаю, на кой хер мне эта функция, но пусть лучше будеть здесь, чем в классе окна.
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        // TODO: сейчас всегда возвращает true, планировалось видимо чтобы при ошибке встраивания вернуло false
        public bool insertMessage(string message, int hashSeed)
        {
            double MSE1 = 0;
            double PSNR1 = 0;
            double sub = 0;

            BitArray bitArray = getBitArray(message);
            WriteableBitmap bitmap = new WriteableBitmap(bSource);
            int step = bitmap.Format.BitsPerPixel / 8;

            List<int> randInt = generateListOfIndexes(hashSeed, bitArray.Length);

            bitmap.Lock();
            unsafe
            {
                int pBackBuffer = (int)bitmap.BackBuffer;
                int k = 0;
                foreach (int ar in randInt)
                {
                    int ptr = ar * step;
                    int color = *((int*)(pBackBuffer + ptr)); //получили цвет пикселя в int (4 byte)

                    int color1 = color;
                    byte[] cB1 = BitConverter.GetBytes(color1);

                    color = sm2lsb(color, bitArray.Get(k), bitArray.Get(k + 1));
                    byte[] cB2 = BitConverter.GetBytes(color);

                    sub += System.Math.Pow((cB1[0] - cB2[0]), 2) + System.Math.Pow((cB1[1] - cB2[1]), 2) + System.Math.Pow((cB1[2] - cB2[2]), 2);

                    k += 2;

                    *((int*)(pBackBuffer + ptr)) = color;
                }
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            bitmap.Unlock();

            // подсчёт MSE и PSNR нужно вынести в StegoAnalyzer [16.12.2025]
            MSE = sub;
            MSE = MSE / (bitmap.PixelHeight * bitmap.PixelWidth * 3);

            PSNR = 10 * Math.Log10(Math.Pow(255, 2) / MSE);

            MSE1 = mse / (bitmap.PixelHeight * bitmap.PixelWidth * 3);
            PSNR1 = 10 * Math.Log10(Math.Pow(255, 2) / MSE1);

            bSource = bitmap;

            return true;
        }

        private BitArray getBitArray(string message)
        {
            //Encoding encoding = Encoding.Unicode; //тип кодировки ASCII
            Encoding encoding = Encoding.ASCII;
            byte[] byteString = encoding.GetBytes(message); //кодируем строку в массив байт

            int sizeBit = ((int)byteString.Length * 8); //длина сообщения в битах

            if (sizeBit >= maxLengthMessage)
            {
                //сообщение слишком длинное
                string exceptionMessage = "Длина сообщения слишком велика для данного изображения.";
                throw new Exception(exceptionMessage);
                //   return bitEr;

            }
            //размер сообщения в последовательность бит
            BitArray bitArrayLength = new BitArray(lengthSize);
            String stri = Convert.ToString(sizeBit, 2); //размер сообщения в двоичной с/сч в текстовом представлении
            //инициализация bitArrayLength
            for (int i = stri.Length - 1, j = 0; i >= 0; i--, j++)
            {
                if (stri[i] == '0')
                    bitArrayLength[j] = false;
                else
                    bitArrayLength[j] = true;
            }

            // склеивание 2 битовых массивов

            BitArray bitArrayMessage = new BitArray(byteString);

            BitArray bitArrayCod = new BitArray(bitArrayLength.Length + bitArrayMessage.Length);

            for (int i = 0; i < bitArrayLength.Length; i++)
                bitArrayCod[i] = bitArrayLength[i];

            for (int i = bitArrayLength.Length, j = 0; i < bitArrayCod.Length; i++, j++)
                bitArrayCod[i] = bitArrayMessage[j];

            return bitArrayCod;
        }

        public String extractMessage(int hashSeed)
        {
            String message = "0";
            BitArray exBits = new BitArray(lengthSize);

            //извлекаем размер сообщения
            exBits = extractBit(0, lengthSize, hashSeed);
            int exSize = bitToInt(exBits);

            if (exSize <= maxLengthMessage)
            {
                // извлекаем само сообщение
                exBits = extractBit(lengthSize, lengthSize + exSize, hashSeed);
                byte[] exByte = new byte[exSize / 8];
                exByte = bitToByte(exBits);
                message = System.Text.Encoding.ASCII.GetString(exByte);
            }

            return message;
        }

        private BitArray extractBit(int start, int end, int hashSeed)
        {
            int size = end - start;
            BitArray exBitAr = new BitArray(size);
            BitArray exBufLSB = new BitArray(3);
            BitArray exBufLSB2 = new BitArray(2);
            byte[] exColor = { 0, 0, 0, 0 };
            int step = bSource.Format.BitsPerPixel / 8;
            int k2 = 0;

            WriteableBitmap bitmap = new WriteableBitmap(bSource);

            // инициализация псевдослучайного массива int
            List<int> randInt = generateListOfIndexes(hashSeed, end);

            bitmap.Lock();
            unsafe
            {
                int pbackbuffer = (int)bitmap.BackBuffer;
                int k = 0;
                foreach (int ar in randInt)
                {
                    if (k >= start)
                    {
                        int ptr = ar * step;

                        int color = *((int*)(pbackbuffer + ptr));

                        byte[] cByte = BitConverter.GetBytes(color);
                        BitArray exBuf = new BitArray(cByte);
                        exBufLSB[0] = exBuf[0];
                        exBufLSB[1] = exBuf[8];
                        exBufLSB[2] = exBuf[16];

                        exBufLSB2 = exSM2LSB(exBufLSB);

                        exBitAr.Set(k2, exBufLSB2[0]);
                        exBitAr.Set((k2 + 1), exBufLSB2[1]);
                        k2 += 2;
                    }
                    k += 2;
                }
            }
            // bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            bitmap.Unlock();

            return exBitAr;
        }

        private List<int> generateListOfIndexes(int hashSeed, int end)
        {
            Random rand = new Random(hashSeed);
            List<int> randInt = new List<int>();
            int max = bSource.PixelHeight * bSource.PixelWidth - 1;

            for (int i = 0, k = 0; i < (end / 2); i++)
            {
                bool equ = false;
                k = rand.Next(max);
                foreach (int ar in randInt)
                {
                    if (k == ar) equ = true;
                }

                if (!equ)
                {
                    randInt.Add(k);
                }
            }

            return randInt;
        }

        private BitArray exSM2LSB(BitArray b1)
        {
            BitArray b2 = new BitArray(2);
            bool i = b1[2];

            if (i)
            {
                b2.Set(1, !(b1[1]));
                b2.Set(0, b1[0]);
            }
            else
            {
                b2.Set(1, b1[1]);
                b2.Set(0, !(b1[0]));
            }

            return b2;
        }

        // как отдельный метод - не нужен, вставить inline
        private byte setLSB(byte bt, bool bl)
        {
            if ((bt % 2 == 1) & (!bl)) bt--;
            if ((bt % 2 == 0) & (bl)) bt++;

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

        private int sm2lsb(int colors, bool bit1, bool bit2)
        {
            byte[] cByte = BitConverter.GetBytes(colors);
            bool[] match = { false, false };
            BitArray lsb = new BitArray(3);
            BitArray buf; //= new BitArray(8);

            buf = new BitArray(cByte);
            lsb[0] = buf[0];
            lsb[1] = buf[8];
            lsb[2] = buf[16];

            if (lsb[0] == bit1) match[0] = true;
            else match[0] = false;

            if (lsb[1] == bit2) match[1] = true;
            else match[1] = false;

            int n = boolToInt(lsb[2]);
            bool bb = !(lsb[2]);
            if ((match[0] == true) && (match[1] == true)) //2 бита совпадают
            {
                lsb[n] = !(lsb[n]);
            }
            if ((match[0] == false) && (match[1] == false)) // 2 бита не совпадают
            {
                n = boolToInt(bb);
                lsb[n] = !(lsb[n]);
                mse += 2;
            }
            if (match[0] != match[1]) // 1  бит совпадает, 1 - нет
            {
                if (match[n] == true)
                    lsb[2] = !(lsb[2]);
                mse++;

            }
            for (int i = 0; i < lsb.Length; i++)
            {
                cByte[i] = setLSB(cByte[i], lsb[i]);
            }

            colors = BitConverter.ToInt32(cByte, 0);
            return colors;
        }

        private int boolToInt(bool b)
        {
            if (b) return 1;
            else return 0;
        }

        private ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }
    }
}