using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Utilities
{
    public class Result<T>
    {
        public T Value { get; }
        public string ErrorMessage { get; }
        public bool IsSuccess { get; }

        private Result(T value, string error, bool isSuccess)
        {
            Value = value;
            ErrorMessage = error;
            IsSuccess = isSuccess;
        }

        public static Result<T> Success(T value) => new Result<T>(value, null, true);
        public static Result<T> Failure(string error) => new Result<T>(default, error, false);
    }
}
