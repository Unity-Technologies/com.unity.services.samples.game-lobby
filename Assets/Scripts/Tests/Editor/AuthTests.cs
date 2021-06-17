using LobbyRooms.Auth;
using NUnit.Framework;

public class AuthTests
{
    [Test]
    public void IdentityBasicSubidentity()
    {
        string value = null;
        int count = 0;
        SubIdentity testIdentity = new SubIdentity();
        testIdentity.onChanged += (si) => { value = si.GetContent("key1"); count++; };

        testIdentity.SetContent("key1", "newValue1");
        Assert.AreEqual(1, count, "Content changed once.");
        Assert.AreEqual("newValue1", value, "Should not have to do anything to receive updated content from a set.");
        testIdentity.SetContent("key2", "newValue2");
        Assert.AreEqual(2, count, "Content changed twice.");
        Assert.AreEqual("newValue1", value, "Contents should not affect different keys.");
    }
}