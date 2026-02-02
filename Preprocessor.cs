using System.Text.RegularExpressions;

namespace Steganography
{
    
    class Preprocessor
    {
        public int GetMaxMessageLength(int width, int height)
        {
            const double embedingPersent = 0.1; //процент встраиваемой информации от размеров изображения
            int pixelCount = width * height;
            int bitsInsertedInPix = 2; // для 1 метода 2 бита информации в пиксель
                                       // для 2 метода 6 бит в пиксель

            //максимальная длина сообщения (в битах)
            int maxMesLength = (int)(pixelCount * embedingPersent * bitsInsertedInPix);

            // logger.WriteLog();

            int maxMesLengthInSymb = maxMesLength / 6;

            // logger.InformUser();

            return maxMesLengthInSymb; 
        }

        // Возвращает lengthSize - длину поля, которое записывается перед основным сообщением

        public int GetLengthSize(int width, int height)
        {
            int size = (width * height * 2);

            int lengthSize = (int)System.Math.Log((double)size, (double)2);
            //проверка четности. если нечетно +1
            if (lengthSize % 2 != 0)
                lengthSize++;

            return lengthSize;
        }

        public bool CheckLang(string mes, int flag)
        {
            string pattern;
            
            // по умолчанию 0 - англ язык
            if(flag == 0)
            {
                pattern = @"^[a-zA-Z0-9]*";
            }
            else
            {
                pattern = @"^[а-яА-Я0-9]*";
            }
            Regex regex = new Regex(pattern);

            if(!regex.IsMatch(mes))
            {
                
            }

            return true;
        }

        public void displayResults()
        {

        }
    }
}