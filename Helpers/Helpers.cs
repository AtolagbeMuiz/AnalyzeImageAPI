using System;

namespace AnalyzeImageAPI.Helpers
{
    public class Helpers
    {
        // Helper method to determine if the path is a URL
        public bool IsUrl(string path)
        {
            return Uri.TryCreate(path, UriKind.Absolute, out Uri uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
