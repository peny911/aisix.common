using System.Net;

namespace Aisix.Common.Exceptions
{
    public class WebApiException : Exception
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        public WebApiStatusCode WebApiStatusCode { get; set; }
        public object AdditionalData { get; set; }

        public WebApiException()
            : this(WebApiStatusCode.InternalServerError)
        {
        }

        public WebApiException(WebApiStatusCode statusCode)
           : this(statusCode, null)
        {
        }

        public WebApiException(string message)
            : this(WebApiStatusCode.InternalServerError, message)
        {
        }

        public WebApiException(WebApiStatusCode statusCode, string message)
            : this(statusCode, message, HttpStatusCode.InternalServerError)
        {
        }

        public WebApiException(string message, object additionalData)
            : this(WebApiStatusCode.InternalServerError, message, additionalData)
        {
        }

        public WebApiException(WebApiStatusCode statusCode, object additionalData)
            : this(statusCode, null, additionalData)
        {
        }

        public WebApiException(WebApiStatusCode statusCode, string message, object additionalData)
            : this(statusCode, message, HttpStatusCode.InternalServerError, additionalData)
        {
        }

        public WebApiException(WebApiStatusCode statusCode, string message, HttpStatusCode httpStatusCode)
            : this(statusCode, message, httpStatusCode, null)
        {
        }

        public WebApiException(WebApiStatusCode statusCode, string message, HttpStatusCode httpStatusCode, object additionalData)
            : this(statusCode, message, httpStatusCode, null, additionalData)
        {
        }

        public WebApiException(string message, Exception exception)
            : this(WebApiStatusCode.InternalServerError, message, exception)
        {
        }

        public WebApiException(string message, Exception exception, object additionalData)
            : this(WebApiStatusCode.InternalServerError, message, exception, additionalData)
        {
        }

        public WebApiException(WebApiStatusCode statusCode, string message, Exception exception)
            : this(statusCode, message, HttpStatusCode.InternalServerError, exception)
        {
        }

        public WebApiException(WebApiStatusCode statusCode, string message, Exception exception, object additionalData)
            : this(statusCode, message, HttpStatusCode.InternalServerError, exception, additionalData)
        {
        }

        public WebApiException(WebApiStatusCode statusCode, string message, HttpStatusCode httpStatusCode, Exception exception)
            : this(statusCode, message, httpStatusCode, exception, null)
        {
        }

        public WebApiException(WebApiStatusCode apiStatusCode, string message, HttpStatusCode httpStatusCode, Exception exception, object additionalData)
            : base(message, exception)
        {
            WebApiStatusCode = apiStatusCode;
            HttpStatusCode = httpStatusCode;
            AdditionalData = additionalData;
        }

        //public WebApiException(WebApiStatusCode errorCode, string errorMsg)
        //    : base(errorMsg)
        //{
        //    this.errorCode = errorCode;
        //}
    }
}
