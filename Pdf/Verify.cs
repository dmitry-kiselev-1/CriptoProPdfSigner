
using System;
using System.Security.Cryptography.X509Certificates;
using System.IO.Packaging;

namespace Skdo.Signer.Pdf
{
    using iTextSharp.text.pdf;
    using System.IO;
    using Org.BouncyCastle.X509;
    using System.Security.Cryptography.Pkcs;
    using System.Collections.Generic;
    using iTextSharp.text.pdf.security;    
    
    /// <summary>
    /// http://www.cryptopro.ru/forum2/Default.aspx?g=posts&t=2846
    /// </summary>
    public class Verify
    {

        [STAThread]
        public static int Main(string[] args)
        {
            // Разбираем аргументы
            if (args.Length < 1)
            {
                Console.WriteLine("Pdf.Verify <document>");
                return 1;
            }
            string document = args[0];

            // Открываем документ
            PdfReader reader = new PdfReader(document);

            // Получаем подписи из документа
            AcroFields af = reader.AcroFields;
            List<string> names = af.GetSignatureNames();
            foreach (string name in names)
            {
                string message = "Signature name: " + name;
                message += "\nSignature covers whole document: " + af.SignatureCoversWholeDocument(name);
                message += "\nDocument revision: " + af.GetRevision(name) + " of " + af.TotalRevisions;
                Console.WriteLine(message);

                // Проверяем подпись
                // szOID_CP_GOST_R3411_12_256	"1.2.643.7.1.1.2.2"	Функция хэширования ГОСТ Р 34.11-2012, длина выхода 256 бит

                PdfPKCS7 pk = af.VerifySignature(name);
                DateTime cal = pk.SignDate;
                Org.BouncyCastle.X509.X509Certificate[] pkc = pk.Certificates;
                message = "Certificate " + pk.SigningCertificate;
                message += "\nDocument modified: " + !pk.Verify();
                message += "\nDate: " + cal.ToShortDateString();

                // Проверим сертификат через CAPI     
                X509Certificate2 cert = new X509Certificate2(pk.SigningCertificate.GetEncoded());
                var isCAPIValid = cert.Verify();
                message += "\nCAPI Validation: " + isCAPIValid.ToString();
                Console.WriteLine(message);
            }

            return 0;
        }
    }
}
