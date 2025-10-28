using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Govor.Mobile.Utilities
{
    public class HttpResult<T>
    {
        public T? Value { get; }
        public string? ErrorMessage { get; }
        public bool IsSuccess { get => (int)StatusCode >= 200 && (int)StatusCode < 300; }
        public HttpStatusCode StatusCode { get; }

        public HttpResult(T value, string error, HttpStatusCode statusCode)
        {
            Value = value;
            ErrorMessage = error;
            StatusCode = statusCode;
        }

        public HttpResult(string error, HttpStatusCode statusCode)
        {
            Value = default;
            ErrorMessage = error;
            StatusCode = statusCode;
        }

        public HttpResult(HttpStatusCode statusCode)
        {
            Value = default;
            ErrorMessage = "";
            StatusCode = statusCode;
        }


        public static HttpResult<T> Success(T value) => new HttpResult<T>(value, null, HttpStatusCode.OK);
        public static HttpResult<T> FromException(Exception ex)
            => new(default, ex.Message, HttpStatusCode.InternalServerError);

        public override string ToString()
        {
            return IsSuccess
                ? $"✅ Success ({(int)StatusCode}) - Value: {Value}"
                : $"❌ Error ({(int)StatusCode}) - {ErrorMessage}";
        }
    }
}
