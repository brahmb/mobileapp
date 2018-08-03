using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Helpers;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Ultrawave.ApiClients
{
    internal abstract class BaseApi
    {
        internal delegate void ResponseValidator<T>(IRequest request, IResponse response, T data);
        
        private readonly IApiClient apiClient;
        private readonly IJsonSerializer serializer;

        private readonly Endpoint loggedEndpoint;

        protected HttpHeader AuthHeader { get; }

        protected BaseApi(IApiClient apiClient, IJsonSerializer serializer, Credentials credentials, Endpoint loggedEndpoint)
        {
            Ensure.Argument.IsNotNull(apiClient, nameof(apiClient));
            Ensure.Argument.IsNotNull(serializer, nameof(serializer));
            Ensure.Argument.IsNotNull(credentials, nameof(credentials));
            Ensure.Argument.IsNotNull(loggedEndpoint, nameof(loggedEndpoint));

            this.apiClient = apiClient;
            this.serializer = serializer;
            this.loggedEndpoint = loggedEndpoint;
            AuthHeader = credentials.Header;
        }

        protected IObservable<List<TInterface>> CreateListObservable<TModel, TInterface>(Endpoint endpoint, HttpHeader header, List<TModel> entities, SerializationReason serializationReason, IWorkspaceFeatureCollection features = null)
            where TModel : class, TInterface
        {
            var body = serializer.Serialize(entities, serializationReason, features);
            return CreateListObservable<TModel, TInterface>(endpoint, header, body);
        }

        protected IObservable<List<TInterface>> CreateListObservable<TModel, TInterface>(Endpoint endpoint, HttpHeader header, string body = "")
            where TModel : class, TInterface
            => CreateListObservable<TModel, TInterface>(endpoint, new[] { header }, body);


        protected IObservable<List<TInterface>> CreateListObservable<TModel, TInterface>(Endpoint endpoint, IEnumerable<HttpHeader> headers, string body = "")
            where TModel : class, TInterface
        {
            var observable = CreateObservable<List<TModel>>(endpoint, headers, body);
            return observable.Select(items => items?.ToList<TInterface>());
        }

        protected IObservable<T> CreateObservable<T>(Endpoint endpoint, HttpHeader header, T entity,
            SerializationReason serializationReason, IWorkspaceFeatureCollection features = null,
            ResponseValidator<T> responseValidator = null)
        {
            var body = serializer.Serialize(entity, serializationReason, features);
            return CreateObservable(endpoint, header, body, responseValidator);
        }
        
        protected IObservable<T> CreateObservable<T>(Endpoint endpoint, HttpHeader header, string body = "",
            ResponseValidator<T> responseValidator = null)
        => CreateObservable(endpoint, new[] { header }, body, responseValidator);

        protected IObservable<T> CreateObservable<T>(Endpoint endpoint, IEnumerable<HttpHeader> headers, string body = "",
            ResponseValidator<T> responseValidator = null)
            => createObservable(endpoint, headers, body, async (rawData) =>
                !string.IsNullOrEmpty(rawData)
                    ? await Task.Run(() => serializer.Deserialize<T>(rawData)).ConfigureAwait(false)
                    : default(T),
                responseValidator);

        protected IObservable<string> CreateObservable(Endpoint endpoint, HttpHeader header, string body = "")
            => createObservable(endpoint, new[] { header }, body, Task.FromResult);

        protected IObservable<string> CreateObservable(Endpoint endpoint, IEnumerable<HttpHeader> headers, string body = "")
            => createObservable(endpoint, headers, body, Task.FromResult);

        private IObservable<T> createObservable<T>(Endpoint endpoint,
            IEnumerable<HttpHeader> headers, string body, Func<string, Task<T>> process,
            ResponseValidator<T> responseValidator = null)
        {
            var headerList = headers as IList<HttpHeader> ?? headers.ToList();
            var request = new Request(body, endpoint.Url, headerList, endpoint.Method);
            
            return Observable.Create<T>(async observer =>
            {
                T data;
                try
                {
                    var response = await sendRequest(request).ConfigureAwait(false);

                    await throwIfRequestFailed(request, response, headerList).ConfigureAwait(false);

                    data = await processResponseData(request, response, process).ConfigureAwait(false);

                    validateResponse(request, response, data, responseValidator);
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                    return;
                }

                observer.OnNext(data);
                observer.OnCompleted();
            });
        }

        private async Task<IResponse> sendRequest(IRequest request)
        {
            try
            {
                return await apiClient.Send(request).ConfigureAwait(false);
            }
            catch (HttpRequestException exception)
            {
                throw new OfflineException(exception);
            }
        }

        private async Task throwIfRequestFailed(IRequest request, IResponse response, IEnumerable<HttpHeader> headers)
        {
            if (response.IsSuccess)
                return;

            throw await getExceptionFor(request, response, headers).ConfigureAwait(false);
        }

        private static async Task<T> processResponseData<T>(IRequest request, IResponse response, Func<string, Task<T>> process)
        {
            try
            {
                return await process(response.RawData).ConfigureAwait(false);
            }
            catch
            {
                throw new DeserializationException<T>(request, response, response.RawData);
            }
        }

        private static void validateResponse<T>(IRequest request, IResponse response, T data, ResponseValidator<T> validateResponse)
            => validateResponse?.Invoke(request, response, data);

        private async Task<Exception> getExceptionFor(
            IRequest request,
            IResponse response,
            IEnumerable<HttpHeader> headers)
        {
            try
            {
                if (response.StatusCode == HttpStatusCode.Forbidden && await isLoggedIn(headers).ConfigureAwait(false) == false)
                    return new UnauthorizedException(request, response);
            }
            catch (HttpRequestException)
            {
                return new OfflineException();
            }

            return ApiExceptions.For(request, response);
        }

        private async Task<bool> isLoggedIn(IEnumerable<HttpHeader> headers)
        {
            var request = new Request(String.Empty, loggedEndpoint.Url, headers, loggedEndpoint.Method);
            var response = await apiClient.Send(request).ConfigureAwait(false);
            return !((int)response.StatusCode >= 400 && (int)response.StatusCode < 500);
        }
    }
}
