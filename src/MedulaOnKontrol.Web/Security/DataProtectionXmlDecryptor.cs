using System.Xml.Linq;
using MedulaOnKontrol.Application;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;

namespace MedulaOnKontrol.Web.Security;
public sealed class DataProtectionXmlDecryptor(IServiceProvider services) : IXmlDecryptor
{
    public XElement Decrypt(XElement encryptedElement) => XElement.Parse(services.GetRequiredService<AesGcmCodec>().Decrypt(encryptedElement.Value));
}
