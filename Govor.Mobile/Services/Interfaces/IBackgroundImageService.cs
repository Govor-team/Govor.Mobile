using System;
using System.Collections.Generic;
using System.Text;
using Govor.Mobile.Models;
using FileResult = Microsoft.Maui.Storage.FileResult;

namespace Govor.Mobile.Services.Interfaces;

public interface IBackgroundImageService
{
    List<BackgroundItem> GetAvailableBackgrounds();
    Task<BackgroundItem> AddBackgroundFromGallery(FileResult file);
    Task<bool> RemoveBackgroundAsync(string path);
    void ApplyBackground(string path);
    BackgroundItem LoadCurrent();
}


