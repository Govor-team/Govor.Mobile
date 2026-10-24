namespace Govor.Mobile.Tests;

public class GalleryMediaPolicyTests
{
    [Test]
    public async Task StreamLimitIsEnforcedWithoutTrustingLength()
    {
        using var stream = new MemoryStream(new byte[101]);
        Assert.ThrowsAsync<InvalidDataException>(() => GalleryMediaPolicy.ReadAsync(stream, 100));
        using var allowed = new MemoryStream(new byte[100]);
        Assert.That(await GalleryMediaPolicy.ReadAsync(allowed, 100), Has.Length.EqualTo(100));
    }

    [Test]
    public void EmptyFilesAndActiveContentAreRejected()
    {
        Assert.ThrowsAsync<InvalidDataException>(() => GalleryMediaPolicy.ReadAsync(Stream.Null));
        Assert.Throws<InvalidDataException>(() => GalleryMediaPolicy.Inspect("<svg onload='evil()'/>"u8.ToArray()));
        Assert.Throws<InvalidDataException>(() => GalleryMediaPolicy.Inspect("<html>not a photo</html>"u8.ToArray()));
    }

    [Test]
    public void IsoContainerForHeicOrAvifIsNotMistakenForVideo()
    {
        Assert.Throws<InvalidDataException>(() => GalleryMediaPolicy.Inspect(Container("heic")));
        Assert.Throws<InvalidDataException>(() => GalleryMediaPolicy.Inspect(Container("avif")));
        Assert.That(GalleryMediaPolicy.Inspect(Container("mp42")).Mime, Is.EqualTo("video/mp4"));
        Assert.That(GalleryMediaPolicy.Inspect(Container("qt  ")).Mime, Is.EqualTo("video/quicktime"));
    }

    [Test]
    public void DetectsImageBytesWithoutTrustingExtension()
    {
        var png = new byte[24];
        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }.CopyTo(png, 0);
        Assert.That(GalleryMediaPolicy.Inspect(png), Is.EqualTo(("image/png", ".png", false)));
    }

    private static byte[] Container(string brand)
    {
        var bytes = new byte[24];
        "ftyp"u8.CopyTo(bytes.AsSpan(4));
        System.Text.Encoding.ASCII.GetBytes(brand).CopyTo(bytes, 8);
        return bytes;
    }
}
