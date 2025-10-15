using System;
using Microsoft.AspNetCore.Http.Features;

namespace Zapto.AspNetCore.Http.Features;

public class HttpsCompressionFeatureImpl : IHttpsCompressionFeature
{
    private AspNetResponse _response = null!;

    public void SetHttpResponse(AspNetResponse response)
    {
        _response = response;
    }

    public void Reset()
    {
        _response = null!;
    }

    public HttpsCompressionMode Mode
    {
        get
        {
            // Use the header "Content-Encoding" to tell IIS compression module to compress or not compress
            var contentEncoding = _response.Headers["Content-Encoding"];

            if (string.IsNullOrEmpty(contentEncoding))
            {
                return HttpsCompressionMode.Default;
            }

            if (string.Equals(contentEncoding, "identity", StringComparison.OrdinalIgnoreCase))
            {
                return HttpsCompressionMode.DoNotCompress;
            }

            return HttpsCompressionMode.Compress;
        }
        set
        {
            switch (value)
            {
                case HttpsCompressionMode.Default:
                    _response.Headers.Remove("Content-Encoding");
                    break;
                case HttpsCompressionMode.DoNotCompress:
                    _response.Headers["Content-Encoding"] = "identity";
                    break;
                case HttpsCompressionMode.Compress:
                    // Do nothing, let the compression middleware set the appropriate header
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }
}
