using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Services.Interfaces;

public interface IMediaLoaderService
{
    Task<Result<UploadMediaResponse>> Upload(UploadMediaRequest request);
    Task<Result<DownloadMediaResponse>> Download(Guid id);
}
