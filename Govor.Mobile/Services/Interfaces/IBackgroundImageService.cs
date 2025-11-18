using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Services.Interfaces;

public interface IBackgroundImageService
{
    string GetBackgroundImage();
    void SetUserBackground(string imagePath);
    void ClearUserBackground();
}


