using CommunityToolkit.Mvvm.ComponentModel;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics; // если требуется по проекту — при необходимости убрать

namespace Govor.Mobile.PageModels.ContentViewsModel
{
    public partial class UserIdQRCodePopupModel : ObservableObject
    {
        [ObservableProperty] private Guid userId;
        [ObservableProperty] private AvatarViewModel avatar;
        [ObservableProperty] private string username;
        [ObservableProperty] private ImageSource qrCodeImage;
        public const string QrContentPrefix = "govor://u/";

        public UserIdQRCodePopupModel(Guid userId, string username, AvatarViewModel avatar)
        {
            UserId = userId;
            Username = username;
            Avatar = avatar;

            _ = InitQrCodeAsync();
        }

        private async Task InitQrCodeAsync()
        {
            try
            {
                var qrContent = $"{QrContentPrefix}{UserId}";

                // Генерируем QR
                using var qrCodeData = QRCodeGenerator.GenerateQrCode(
                    qrContent,
                    QRCodeGenerator.ECCLevel.M);

                using var qrCode = new PngByteQRCode(qrCodeData);

                var bytes = qrCode.GetGraphic(20);

                // ImageSource создаём на UI thread
                var imageSource = await MainThread.InvokeOnMainThreadAsync(
                    () => ImageSource.FromStream(
                        () => new MemoryStream(bytes)));

                QrCodeImage = imageSource;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"QR generation error: {ex}");
            }
        }
    }
}