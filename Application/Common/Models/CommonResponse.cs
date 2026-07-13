namespace Application.Common.Models
{
    public class CommonResponse<T>
    {
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public T Data { get; set; }

        public CommonResponse()
        {
        }

        public CommonResponse(string responseCode, string responseMessage, T data = default)
        {
            ResponseCode = responseCode;
            ResponseMessage = responseMessage;
            Data = data;
        }

        public static CommonResponse<T> Success(T data, string message = "Request processed successfully.")
        {
            return new CommonResponse<T>(ResponseCodes.Success, message, data);
        }

        public static CommonResponse<T> Fail(string responseCode, string message)
        {
            return new CommonResponse<T>(responseCode, message, default);
        }
    }
}
