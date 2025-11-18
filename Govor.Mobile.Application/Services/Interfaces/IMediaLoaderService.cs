using Govor.Mobile.Application.Models.Requests;
using Govor.Mobile.Application.Models.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Application.Services.Interfaces;

public interface IMediaLoaderService
{
    Task<Result<UploadMediaResponse>> Upload(UploadMediaRequest request);
    Task<Result<DownloadMediaResponse>> Download(Guid id);
}
