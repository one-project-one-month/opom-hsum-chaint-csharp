using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Domain
{
    public class ApplicationCommonResponseModel<T> where T : class
    {
        public bool? IsSuccess { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public T? ListData { get; set; }
    }
}
