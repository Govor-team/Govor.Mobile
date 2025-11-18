using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Application.Services.Interfaces;

public interface IBackgroundImageService
{
    string GetBackgroundImage();
    void SetUserBackground(string imagePath);
    void ClearUserBackground();
}


