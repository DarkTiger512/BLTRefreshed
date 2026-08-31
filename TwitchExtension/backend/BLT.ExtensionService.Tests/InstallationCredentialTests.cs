using BLT.ExtensionService.Security;

namespace BLT.ExtensionService.Tests;

public sealed class InstallationCredentialTests
{
    [Fact]
    public void CredentialsAreRandomAndOnlyHashesArePersistable()
    {
        var first = InstallationCredentialService.Create();
        var second = InstallationCredentialService.Create();

        Assert.NotEqual(first, second);
        Assert.NotEqual(first, InstallationCredentialService.Hash(first));
        Assert.Equal(InstallationCredentialService.Hash(first), InstallationCredentialService.Hash(first));
    }
}
