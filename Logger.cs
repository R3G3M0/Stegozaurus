using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Steganography
{
    class Logger
    {
        private TextBox _log;

        public Logger(ref TextBox log)
        {
            _log = log;
        }

        public void printToLog(string logMessage)
        {
           _log.Text += logMessage;
        }

        public void informUser(string caption, string message, int messageType)
        {
            MessageBoxImage icon;
            switch (messageType)
            {
                case 0:
                {
                    icon = MessageBoxImage.Information;
                    break;
                }
                case 1:
                {
                    icon = MessageBoxImage.Warning;
                    break;
                }
                case 2:
                {
                    icon = MessageBoxImage.Question;
                    break;
                }
                case 3:
                default:
                    icon = MessageBoxImage.Error;
                    break;
            }
            MessageBox.Show(message, caption, MessageBoxButton.OK, icon);
        }

    }
}