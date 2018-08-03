using System;

namespace Toggl.Ultrawave.Network
{
    internal struct FeedbackEndpoints
    {
        private readonly Uri baseUrl;

        public FeedbackEndpoints(Uri baseUrl)
        {
            this.baseUrl = baseUrl;
        }

        public Endpoint Post => Endpoint.Post(baseUrl, "mobile/feedback");
    }
}
