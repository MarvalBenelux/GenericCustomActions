using System;
using MarvalSoftware.Data;
using MarvalSoftware.ExceptionHandling;
using MarvalSoftware.Extensions;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace MarvalSoftware.Servers.CustomAction.PostForm
{
    public class PostFormAction : IIntegrationAction
    {
        #region Implementation of IIntegrationAction

        /// <summary>
        /// Providing a name for a custom action is Required
        /// </summary>
        public PostFormAction()
        {
            this.ActionName = "PostForm";
        }

        /// <summary>
        /// The integration action name
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        ///     Executes the action returning an number of messages.
        /// </summary>
        /// <param name="sessionId">The session Id of the user executing this action.</param>
        /// <param name="integrationMessage">Accepts a string integration message that can be deserialized into the required class.</param>
        public void Execute(string sessionId, string integrationMessage)
        {
            string postIntegration = "Unknown Post Integration";
            string postBaseAddress = "<unknown>";
            try
            {
                FormPostMessage postIntegrationMessage = integrationMessage.XmlDeserialize<FormPostMessage>();
                postIntegration = postIntegrationMessage.IntegrationName ?? "Post Integration";
                postBaseAddress = postIntegrationMessage.BaseUrl;
                var formValues = postIntegrationMessage.FormValues.ToDictionary(fv => fv.Key, fv => fv.Value);
                var additionalHeaders = postIntegrationMessage.Headers.ToDictionary(h => h.Key, h => h.Value);
                var formPost = PostForm(new Uri(postBaseAddress), postIntegrationMessage.PostEndpoint, formValues, additionalHeaders, ParseWebProxyInfo(postIntegrationMessage.Proxy));
                formPost.Wait(10000);
                if (formPost.IsFaulted)
                {
                    throw formPost.Exception;
                }
                var result = formPost.Result;
                var debugInfo = new MarvalApplicationException(String.Format("Success: {0} ({1}) - {2}", (int)result.StatusCode, result.StatusCode, result.ReasonPhrase));
                Log.Debug(debugInfo, String.Format("{0}: {1}: {2}", this.ActionName, postIntegration, debugInfo.Message));
#if DEBUG
                // send a message to the UI
                SendMessage(this, new MessageEventArgs(sessionId, new TrayMessage
                {
                    Heading = postIntegration,
                    Text = "Successful postback",
                    Type = TrayMessage.MessageTypes.Information
                }));
#endif
            }
            catch (HttpRequestException e)
            {
                Log.Error(e, String.Format("{0}: {1}: {2}", this.ActionName, postIntegration, e.Message));
                ExceptionHandler.Publish(new MarvalApplicationException(String.Format("[{0}] {1}: Unable to connect with {2}", this.ActionName, postIntegration, postBaseAddress ?? "<unset>"), e));
            }
            catch (Exception e)
            {
                Log.Error(e, String.Format("{0}: {1}: {2}", this.ActionName, postIntegration, (e.InnerException ?? e).Message));
                if ((e is AggregateException) && (e.InnerException != null))
                {
                    ExceptionHandler.Publish(new MarvalApplicationException(String.Format("[{0}] {1}: Error while communicating with {2}", this.ActionName, postIntegration, postBaseAddress ?? "<unset>"), e.InnerException));
                }
                else
                {
                    ExceptionHandler.Publish(new MarvalApplicationException(String.Format("[{0}] {1}: Exception in Form Post Integration", this.ActionName, postIntegration), e));
                }
            }
        }

        /// <summary>
        /// Public event for sending a message through to the UI
        /// </summary>
        public event EventHandler<MessageEventArgs> SendMessage;

        #endregion

        #region Public implementation details

        public struct Header
        {
            [XmlAttribute(AttributeName = "key")]
            public string Key { get; set; }
            [XmlText]
            public string Value { get; set; }
        }

        public struct FormValue
        {
            [XmlAttribute(AttributeName = "key")]
            public string Key { get; set; }
            [XmlText]
            public string Value { get; set; }
        }

        public struct ProxyUsername
        {
            [XmlText]
            public string Value { get; set; }
        }
        public struct ProxyPassword
        {
            private string password;
            [XmlText]
            public string Value
            {
                get
                {
                    if (IsEncoded)
                    {
                        try
                        {
                            return Encoding.UTF8.GetString(Convert.FromBase64String(password));
                        }
                        catch
                        {
                            return "";
                        }
                    }
                    else
                    {
                        return password;
                    }
                }
                set
                {
                    if (IsEncoded)
                    {
                        try
                        {
                            password = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
                        }
                        catch
                        {
                            password = "";
                        }
                    }
                    else
                    {
                        password = value;
                    }
                }
            }
            [XmlAttribute(AttributeName = "encoded")]
            public bool IsEncoded { get; set; }
        }
        public struct ProxyInfo
        {
            [XmlAttribute(AttributeName = "disable")]
            public bool Disabled { get; set; }
            public ProxyUsername Username;
            public ProxyPassword Password;
        }

        public class FormPostMessage
        {
            [XmlAttribute(AttributeName = "integrationName")]
            public string IntegrationName { get; set; }
            public string BaseUrl { get; set; }
            public string PostEndpoint { get; set; }
            public Header[] Headers { get; set; }
            public ProxyInfo Proxy { get; set; }
            public FormValue[] FormValues { get; set; }
        }

        #endregion

        #region Internals

        private class WebProxyInfo
        {
            public bool DoNotUseProxy { get; set; }
            public IWebProxy Proxy { get; set; }
        }

        private WebProxyInfo ParseWebProxyInfo(ProxyInfo? proxyInfo)
        {
            bool doNotUseProxy = false;
            IWebProxy proxy = null;

            if (proxyInfo != null)
            {
                doNotUseProxy = (proxyInfo?.Disabled ?? false);
                if (! doNotUseProxy)
                {
                    proxy = WebRequest.GetSystemWebProxy();
                    proxy.Credentials = new NetworkCredential(proxyInfo?.Username.Value ?? "", proxyInfo?.Password.Value ?? "");

                }
            }
            return new WebProxyInfo()
            {
                DoNotUseProxy = doNotUseProxy,
                Proxy = proxy
            };
        }

        private async Task<HttpResponseMessage> PostForm(Uri baseAddress, string postEndpointUrl, Dictionary<string, string> formValues, Dictionary<string, string> additionalHeaders = null, WebProxyInfo proxyInfo = null)
        {
            HttpResponseMessage response;

            using (var handler = new HttpClientHandler())
            {
                if (proxyInfo?.DoNotUseProxy ?? false)
                {
                    handler.UseProxy = false;
                }
                else if ((proxyInfo?.Proxy ?? null) != null)
                {
                    handler.Proxy = proxyInfo.Proxy;
                }

                using (var client = new HttpClient(handler))
                {
                    client.BaseAddress = baseAddress;
                    if (additionalHeaders != null)
                    {
                        foreach (var additionalHeader in additionalHeaders)
                        {
                            client.DefaultRequestHeaders.Add(additionalHeader.Key, additionalHeader.Value);
                        }
                    }

                    response = await client.PostAsync(postEndpointUrl, new FormUrlEncodedContent(formValues));
                    response.EnsureSuccessStatusCode();
                }
            }
            return response;
        }

        #endregion

    }
}
