using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

// ПЕРЕНЕСТИ ВСЮ БИЗНЕС ЛОГИКУ ИЗ ЭТОГО ФАЙЛА!!! [28.10.2025]
// перенёс ExtractMessage [15.12.2025]
// перенёс insertBitsToBitmap [16.12.2025]
// перенёс getBitArray и прочие вспомогательные функции [04.01.2026]

namespace Steganography
{
     
    public partial class Stego : Window
    {
        private CryptoGenerator generator; // TODO: generator должен создаваться в классе businessLogic, он только там и используется
        private MessageEncoder encoder;
        private Preprocessor preprocessor;
        private StegoAnalyzer stAnalyzer;
        private Logger logger;
        private BusinessLogic businessLogic;
        private BitmapSource bSource; // пока что пусть будет частью этого класса, потом нужно вынести в BusinessLogic

        public Stego()
        {
            InitializeComponent();

            generator = new CryptoGenerator();
            preprocessor = new Preprocessor();
            stAnalyzer = new StegoAnalyzer();
            businessLogic = new BusinessLogic();
            logger = new Logger(ref txtLog);
            encoder = new MessageEncoder(true);
            generator = new CryptoGenerator();
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

                // рассчет максимальной длины сообщения в символах
                int maxLength = preprocessor.GetMaxMessageLength(bSource.PixelWidth, bSource.PixelHeight);
                businessLogic.maxLengthMessage = maxLength;
                businessLogic.lengthSize = preprocessor.GetLengthSize(bSource.PixelWidth, bSource.PixelHeight);

                logger.informUser("Информация", "Максимальная длина сообщения(в символах) = " + maxLength / 6, 0);
                logger.printToLog("\nИзображенние контейнер успешно открыто.");
            }
           
        }
        private void btnInsertClick(object sender, RoutedEventArgs e)
        {
            int hash = GetPasswordFromUser();
            bool result = false;

            if (hash > 0)
            {
                businessLogic.bSource = bSource;
                String message = txtMessage.Text;
                businessLogic.maxLengthMessage = preprocessor.GetMaxMessageLength(bSource.PixelWidth, bSource.PixelHeight);
                result = businessLogic.insertMessage(message, hash);
                bSource = businessLogic.bSource;
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
            txtMessage.Text = businessLogic.extractMessage(GetPasswordFromUser()); 

            logger.printToLog("\nИзвлечение сообщения прошло успешно!\n");
        }

        private void btnSaveClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Encripted"; // Default file name
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
                encoder.Frames.Add(BitmapFrame.Create(bSource)); 
                encoder.Save(stream);

                stream.Close();

                logger.printToLog("\nИзображение-контейнер успешно сохранено.");
            }
        }

        // TODO: переписать эту функцию, чтобы не было вызова generator-a
        // Его должен вызывать класс bussinesLogic
        private int GetPasswordFromUser()
        {
            string pass = txtPassword.Password;

            if (pass.Length < 4)
            {
                MessageBox.Show("Минимальная длина пароля 4 символа!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return -1;
            }
            else
            {
                return generator.makeHash(pass); 
            }
            
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