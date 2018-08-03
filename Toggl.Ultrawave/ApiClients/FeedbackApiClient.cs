using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Newtonsoft.Json;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;
using Toggl.Ultrawave.Serialization.Converters;

namespace Toggl.Ultrawave.ApiClients
{
    internal class FeedbackApiClient : BaseApi, IFeedbackApi
    {
        private readonly FeedbackEndpoints endPoints;

        private readonly IJsonSerializer serializer;

        public FeedbackApiClient(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer, Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.Feedback;
            this.serializer = serializer;
        }

        public IObservable<Unit> Send(Email email, string message, IDictionary<string, string> data)
        {
            Ensure.Argument.IsValidEmail(email, nameof(email));
            Ensure.Argument.IsNotNullOrWhiteSpaceString(message, nameof(message));

            var feedback = new Feedback
            {
                Email = email,
                Message = message,
                Data = data?.Select(CommonFunctions.Identity) ?? Enumerable.Empty<KeyValuePair<string, string>>()
            };

            var json = serializer.Serialize(feedback, SerializationReason.Post, null);

            return CreateObservable(endPoints.Post, AuthHeader, json).SelectUnit();
        }

        [Preserve(AllMembers = true)]
        private class Feedback
        {
            [JsonConverter(typeof(EmailConverter))]
            public Email Email { get; set; }

            public string Message { get; set; }

            public IEnumerable<KeyValuePair<string, string>> Data { get; set; }
        }
    }
}
