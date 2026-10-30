namespace Govor.Mobile.Tests;

public class AttachmentFilesTests
{
    [TestCase("../../secret.txt", "secret.txt")]
    [TestCase("..\\..\\report.docx", "report.docx")]
    [TestCase("\u0000bad:name?.pdf", "badname.pdf")]
    [TestCase("..", "Вложение")]
    public void RemoteNamesCannotEscapeAttachmentDirectory(string name, string expected) =>
        Assert.That(AttachmentFiles.SafeName(name), Is.EqualTo(expected));

    [TestCase("report.DOCX", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [TestCase("notes.txt", "text/plain")]
    [TestCase("document.unknown", "application/octet-stream")]
    [TestCase("photo.jpg", "application/octet-stream")]
    public void FilesAreNotRenderedAsGalleryImagesBasedOnTheirName(string name, string expected)
    {
        Assert.That(AttachmentFiles.Mime(name), Is.EqualTo(expected));
        Assert.That(AttachmentFiles.IsGallery(expected), Is.False);
    }
}
