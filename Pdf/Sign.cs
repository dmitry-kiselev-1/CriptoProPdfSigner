
using System;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security;

namespace Skdo.Signer.Pdf
{
    using iTextSharp.text.pdf;
    using System.IO;
    using Org.BouncyCastle.X509;
    using System.Security.Cryptography.Pkcs;
    using CryptoPro.Sharpei;
    using iTextSharp.text.pdf.security;

    /// <summary>
    /// http://www.cryptopro.ru/forum2/Default.aspx?g=posts&t=2846
    /// </summary>
    public class Sign
    {        
        [STAThread]
        public static int Main(string[] args)
        {
            // Разбираем аргументы
            if (args.Length < 2)
            {
                Console.WriteLine("Pdf.Sign <document> <certificate-dn> [<key-container-password>]");
                return 1;
            }
            string document = args[0];
            string certificate_dn = args[1];

            /*
            // Извлечение клиентского сертификата из хранилища:
            X509Store x509Store = new X509Store(StoreLocation.CurrentUser);
            x509Store.Open(OpenFlags.ReadOnly);
            var x509Certificate = x509Store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName,
                //"E=Rostelecom@s3bank.ru, CN=Sozonjuk Aleksandr Vasil'evich, OU=Department of information technologies, O=s3bank.ru, L=Moscow, S=Moscow, C=RU",
                "E=rostelecom@s3bank.ru, CN=S3Bank-Rostelecom Service, OU=IT Department, O=s3bank.ru, L=Moscow, S=Moscow, C=RU",
                false)[0];
             */

            // Находим секретный ключ по сертификату в хранилище
            X509Store x509Store = new X509Store("My", StoreLocation.CurrentUser);
            x509Store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
            X509Certificate2Collection found = x509Store.Certificates.Find(
                //X509FindType.FindByThumbprint, certificate_dn, true);
                X509FindType.FindBySubjectDistinguishedName, certificate_dn, true);

            if (found.Count == 0)
            {
                Console.WriteLine("Секретный ключ не найден.");
                return 1;
            }
            if (found.Count > 1)
            {
                Console.WriteLine("Найдено более одного секретного ключа.");
                return 1;
            }
            X509Certificate2 certificate = found[0];

            if (args.Length > 2)
            {
                //set password. Пароль "0"
                //var cert_key = certificate.PrivateKey as Gost3410_2012_256CryptoServiceProvider; //Gost3410CryptoServiceProvider;
                var cert_key = certificate.PrivateKey as Gost3410CryptoServiceProvider;
                if (null != cert_key)
                {
                    var cspParameters = new CspParameters();
                    //копируем параметры csp из исходного контекста сертификата
                    cspParameters.KeyContainerName = cert_key.CspKeyContainerInfo.KeyContainerName;
                    cspParameters.ProviderType = cert_key.CspKeyContainerInfo.ProviderType;
                    cspParameters.ProviderName = cert_key.CspKeyContainerInfo.ProviderName;
                    cspParameters.Flags = cert_key.CspKeyContainerInfo.MachineKeyStore
                                      ? (CspProviderFlags.UseExistingKey|CspProviderFlags.UseMachineKeyStore)
                                      : (CspProviderFlags.UseExistingKey);
                    cspParameters.KeyPassword = new SecureString();
                    foreach (var c in args[2])
                    {
                        cspParameters.KeyPassword.AppendChar(c);
                    }
                    //создаем новый контекст сертификат, поскольку исходный открыт readonly
                    certificate = new X509Certificate2(certificate.RawData);
                    
                    //задаем криптопровайдер с установленным паролем
                    //certificate.PrivateKey = new Gost3410_2012_256CryptoServiceProvider(cspParameters);
                    certificate.PrivateKey = new Gost3410CryptoServiceProvider(cspParameters);
                }
            }

            PdfReader reader = new PdfReader(document);
            PdfStamper st = PdfStamper.CreateSignature(reader, new FileStream(document.Replace(".pdf", "") + "_signed.pdf", FileMode.Create, FileAccess.Write), '\0');                        
            PdfSignatureAppearance sap = st.SignatureAppearance;

            // Загружаем сертификат в объект iTextSharp
            X509CertificateParser parser = new X509CertificateParser();
            Org.BouncyCastle.X509.X509Certificate[] chain = new Org.BouncyCastle.X509.X509Certificate[] { 
                parser.ReadCertificate(certificate.RawData)
            };

            sap.Certificate = parser.ReadCertificate(certificate.RawData);
            sap.Reason = "Первый сценарий";
            sap.Location = "Universe";
            sap.Acro6Layers = true;
            
            //sap.Render = PdfSignatureAppearance.SignatureRender.NameAndDescription;
            sap.SignDate = DateTime.Now;

            // Выбираем подходящий тип фильтра
            PdfName filterName = new PdfName("CryptoPro PDF");

            // Создаем подпись
            PdfSignature dic = new PdfSignature(filterName, PdfName.ADBE_PKCS7_DETACHED);
            dic.Date = new PdfDate(sap.SignDate);
            dic.Name = "PdfPKCS7 signature";
            if (sap.Reason != null)
                dic.Reason = sap.Reason;
            if (sap.Location != null)
                dic.Location = sap.Location;
            sap.CryptoDictionary = dic;

            int intCSize = 4000;
            Dictionary<PdfName, int> hashtable = new Dictionary<PdfName, int>();
            hashtable[PdfName.CONTENTS] = intCSize * 2 + 2;
            sap.PreClose(hashtable);
            Stream s = sap.GetRangeStream();
            MemoryStream ss = new MemoryStream();
            int read = 0;
            byte[] buff = new byte[8192];
            while ((read = s.Read(buff, 0, 8192)) > 0)
            {
                ss.Write(buff, 0, read);
            }

            // Вычисляем подпись
            ContentInfo contentInfo = new ContentInfo(ss.ToArray());
            SignedCms signedCms = new SignedCms(contentInfo, true);
            CmsSigner cmsSigner = new CmsSigner(certificate);
            signedCms.ComputeSignature(cmsSigner, false);
            byte[] pk = signedCms.Encode();

            // Помещаем подпись в документ
            byte[] outc = new byte[intCSize];
            PdfDictionary dic2 = new PdfDictionary();
            Array.Copy(pk, 0, outc, 0, pk.Length);
            dic2.Put(PdfName.CONTENTS, new PdfString(outc).SetHexWriting(true));
            sap.Close(dic2);

            Console.WriteLine("Документ {0} успешно подписан на ключе {1} => {2}.",
                document, certificate.Subject, document + "_signed.pdf");
            return 0;
        }
    }
}
