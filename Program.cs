
// Main
using System;
using System.Reflection;

namespace Skdo.Signer
{
    static class Program
    {
        private const string InvalidCommand = "Неизвестная команда {0}";

        /// <summary>
        /// Консольное приложение для подписи документа PDF
        /// 
        /// Пример запуска:
        /// 
        /// Skdo.Signer.exe sign "C:\temp\test.pdf" "C=RU, S=Moscow, L=Moscow, O=SKDO, OU=IT, CN=Дмитрий Киселёв, E=kd.000.000.1@gmail.com" 0
        /// Skdo.Signer.exe verify "C:\temp\test_signed.pdf"
        /// 
        /// </summary>
        /// <param name="args">
        /// Массив
        /// 0 аргумент: команда, Sign означает подписать, Verify означает проверить подпись
        /// 1 аргумент: путь к pdf-файлу, который необходимо подписать
        /// 2 аргумент: составное имя сертификата (Subject DistinguishedName)
        /// 3 аргумент: пароль на сертификат
        /// </param>
        /// <returns></returns>
        [STAThread]
        static int Main(string[] args)
        {
            // Разбираем аргументы
            switch (args[0].ToLower())
            {
                case "sign":
                    Pdf.Sign.Main(new String[] {args[1], args[2], args[3]});
                    break;

                case "verify":
                    Pdf.Verify.Main(new String[] { args[1] });
                    break;

                default:
                    Console.WriteLine("Skdo.Signer <command> <document> <certificate-dn> [<key-container-password>]");
                    return 1;
            }
            
            return 0;
        }
    }
}
