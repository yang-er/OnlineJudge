namespace Microsoft.AspNetCore.Mvc
{
    public class RequestFormLimits2Attribute : RequestFormLimitsAttribute
    {
        public RequestFormLimits2Attribute(int bytes)
        {
            MultipartBodyLengthLimit = bytes;
            KeyLengthLimit = bytes;
            MultipartBoundaryLengthLimit = bytes;
            MultipartHeadersCountLimit = bytes;
            MultipartHeadersLengthLimit = bytes;
            BufferBodyLengthLimit = bytes;
            ValueCountLimit = bytes;
            ValueLengthLimit = bytes;
        }
    }
}
