using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Utilities;

public class FileResult
{
    public Stream Stream;
    public string FileName;
    public string MimeType;

    public FileResult(Stream stream, string fileName, string mimeType)
    {
        Stream = stream;
        FileName = fileName;
        MimeType = mimeType;
    }
}
