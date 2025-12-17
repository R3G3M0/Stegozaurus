using System;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Collections;
using System.Drawing;
using System.Runtime.InteropServices;

// ПЕРЕНЕСТИ ВСЮ БИЗНЕС ЛОГИКУ ИЗ ЭТОГО ФАЙЛА!!!   [28.10.2025]
// перенёс ExtractMessage [15.12.2025]
// перенёс insertBitsToBitmap [16.12.2025]

namespace Steganography
{
    /// <summary>
    /// Interaction logic for MainWnd.xaml
    /// </summary>
    /// 
     
    public partial class Stego : Window
    {
        private CryptoGenerator generator;
        private MessageEncoder encoder;
        private Preprocessor preprocessor;
        private StegoAnalyzer stAnalyzer;
        private Logger logger;
        private BusinessLogic businessLogic;
        private int lengthSize = 0;
        private int maxLengthMessage = 0;
        private BitmapSource bSource;

        public Stego()
        {
            InitializeComponent();

            generator = new CryptoGenerator();
            preprocessor = new Preprocessor();
            stAnalyzer = new StegoAnalyzer();
            businessLogic = new BusinessLogic();

            //encoder = new MessageEncoder(true);
            logger = new Logger(ref txtLog);

        }
            
        private void btnOpenClick(object sender, RoutedEventArgs e)
        {
            txtMessage.Clear();
            txtLog.Clear();
            txtPassword.Clear();
            textBoxMSE.Clear();
            textBoxPSNR.Clear();

            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".png"; // Default file extension
            dlg.Filter = "Portable Network Graphics (.png)|*.png"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;

                // Open a Stream and decode a PNG image
                Stream imageStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                PngBitmapDecoder decoder = new PngBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                
                BitmapSource bitmapSource = decoder.Frames[0];
                bSource = bitmapSource;

                // Draw the Image
                imageBox.Source = bSource;
                imageBox.Stretch = Stretch.Uniform;

                // рассчет максимальной длины сообщения
                int maxLength = preprocessor.GetLength(bSource.PixelWidth, bSource.PixelHeight);

                GetLength();
                
                logger.informUser("Информация", "Максимальная длина сообщения(в символах) = " + maxLength / 6, 0);
                logger.printToLog("\nИзображенние контейнер успешно открыто.");
            }
           
        }

        private void btnSaveClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Enc"; // Default file name
            dlg.DefaultExt = ".png"; // Default file extension
            dlg.Filter = "Portable Network Graphics (.png)|*.png";

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                string filename = dlg.FileName;
             
                FileStream stream = new FileStream(filename, FileMode.Create);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                
                encoder.Interlace = PngInterlaceOption.Off;
                //encoder.Interlace = PngInterlaceOption.On;

                encoder.Frames.Add(BitmapFrame.Create(bSource)); 
                encoder.Save(stream);
                stream.Close();

                logger.printToLog("\nИзображение-контейнер успешно сохранено.");
            }
        }

        private void btnInsertClick(object sender, RoutedEventArgs e)
        {
            
                /*
            String message = txtMessage.Text;

            preprocessor.CheckLang(message, 0);

            if(cbRussian.IsChecked == true)
            {
                encoder = new MessageEncoder(false);
            }
            else
            {
                encoder = new MessageEncoder(true);
            }
            
            BitArray messageEncoded = encoder.Encode(message);

            String pass = txtPassword.Password;
            if (pass.Length < 4)
            {
                MessageBox.Show("Минимальная длина пароля - 4 символа!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<int> indexes = generator.generate(pass, messageEncoded.Length);

            bool result = false;

            result = insertBits(bSource, messageEncoded, indexes);

            */

            int pas = GetPasswordFromUser();
            bool result = false;

            if (pas > 0)
            {
                businessLogic.bSource = bSource;
                result = businessLogic.insertBitToBitmap(getBitArray(), pas);
            }


            if (result == true)
            {
                imageBox.Source = businessLogic.bSource;
                imageBox.Stretch = Stretch.Uniform;
                textBoxMSE.Text = businessLogic.MSE.ToString();
                textBoxPSNR.Text = businessLogic.PSNR.ToString();
                MessageBox.Show("Операция встраивания завершилась успешно!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                logger.printToLog("\nОперация встраивания завершилась успешно!\n");
            }
            else
            {
                logger.printToLog("\nОперация встраивания завершилась успешно!\n");
                logger.informUser("Ошибка", "Произошла ошибка при встраивании информации!", 3);
            }
                
        }

        private void btnExtractClick(object sender, RoutedEventArgs e)
        {
            // где блять проверка того, что введён пароль? [15.12.2025]

            // сделал пока так, потом сделаю нормальный DI [15.12.2025]
            businessLogic.bSource = bSource;
            txtMessage.Text = businessLogic.extractMessage(GetPasswordFromUser(), lengthSize, maxLengthMessage);
            
            logger.printToLog("\nИзвлечение сообщения прошло успешно!\n");
        }

        private void GetLength()
        {
            int size;

            size = (bSource.PixelHeight * bSource.PixelWidth * 2);

            lengthSize = (int)System.Math.Log((double)size, (double)2);
            //проверка четности. если нечетно +1
            if (lengthSize % 2 != 0)
                lengthSize++;

            maxLengthMessage = size - lengthSize;

        }

        //перенести в MessageEncoder
        private BitArray getBitArray()
        {

            String message = txtMessage.Text;

            Encoding encoding = Encoding.Unicode; //тип кодировки ASCII
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
            String stri = Convert.ToString(sizeBit, 2); //размер сообщения в двоичной с/с в текстовом представлении
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

        private int GetPasswordFromUser()
        {
            int p = 0;
            string pass = txtPassword.Password;
            int i = 1;
            int p1 = 0;

            if (pass.Length < 4)
            {
                MessageBox.Show("Минимальная длина пароля 4 символа!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                foreach (char element in pass)
                {
                    p += i * (int)element;
                    i++;
                }

                p1 = p % 1327;
                if (p1 == 0)
                    p1 = p % 1361;

            }
            return p1;
        }


        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        private ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        private void CbEnglish_Checked(object sender, RoutedEventArgs e)
        {
            if(cbRussian != null)
                cbRussian.IsChecked = false;
        }

        private void CbRussian_Checked(object sender, RoutedEventArgs e)
        {
            cbEnglish.IsChecked = false;
        }

        private void CbRussian_Unchecked(object sender, RoutedEventArgs e)
        {
            cbEnglish.IsChecked = true;
        }

        private void CbEnglish_Unchecked(object sender, RoutedEventArgs e)
        {
            cbRussian.IsChecked = true;
        }
    }

}
