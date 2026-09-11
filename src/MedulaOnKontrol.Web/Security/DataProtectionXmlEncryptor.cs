using System.Xml.Linq;
using MedulaOnKontrol.Application;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;

namespace MedulaOnKontrol.Web.Security;
public sealed class DataProtectionXmlEncryptor(AesGcmCodec encryptionService) : IXmlEncryptor
{
    public EncryptedXmlInfo Encrypt(XElement plaintextElement) => new(new XElement("encryptedSessionKey", encryptionService.Encrypt(plaintextElement.ToString(SaveOptions.DisableFormatting))), typeof(DataProtectionXmlDecryptor));
}
