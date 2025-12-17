using System.Text.RegularExpressions;

namespace Steganography
{
    
    class Preprocessor
    {
        private int messageLength;
        private int imageSize;
        public int GetLength(int width, int height)
        {
            const double embedingPersent = 0.1; //процент встраиваемой информации от размеров изображения
            int maxMesLength = 0;
            int pixelCount = width * height;
            int bitsInsertedInPix = 2; // для 1 метода 2 бита информации в пиксель
            // для 2 метода 6 бит в пиксель

            //максимальная длина сообщения (в битах)
            maxMesLength = (int)(pixelCount * embedingPersent * bitsInsertedInPix);

            // logger.WriteLog();

            int maxMesLengthInSymb = maxMesLength / 6;

            // logger.InformUser();

            return maxMesLengthInSymb; 
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