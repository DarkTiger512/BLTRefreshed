using System.Security.Cryptography;
using System.Text;

namespace BLT.ExtensionService.Security;

public static class InstallationCredentialService
{
    public static string Create() => Base64Url(RandomNumberGenerator.GetBytes(48));
    public static string Hash(string credential) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(credential)));
    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
