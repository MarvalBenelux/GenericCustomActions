using System;
using MarvalSoftware.Diagnostics;
using MarvalSoftware.UI.Web;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MarvalSoftware.Servers.CustomAction.PostJSON
{
    public class PostJsonAction : IIntegrationAction
    {
        #region Implementation of IIntegrationAction

        /// <summary>
        /// The integration action name (is being set by the configuration in the MSM Module Loader)
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        ///     Executes the action returning an number of messages.
        /// </summary>
        /// <param name="sessionId">The session Id of the user executing this action.</param>
        /// <param name="integrationMessage">Accepts a string integration message that can be deserialized into the required class.</param>
        public void Execute(string sessionId, string integrationMessage)
        {
            string jsonIntegration = "Unknown JSON Integration";
            string jsonBaseAddress = "<unknown>";
            try
            {
                var message = JObject.Parse(integrationMessage);
                jsonIntegration = (string)message["integration"] ?? "JSON Integration";
                jsonBaseAddress = (string)message["baseaddress"];
                string jsonEndpoint = (string)message["endpoint"];
                var jsonMessage = message["message"];
                JArray jsonHeaders = (JArray)message["headers"];
                var additionalHeaders = jsonHeaders.Select(h => new KeyValuePair<string, string>((string)h["header"], (string)h["value"])).ToDictionary(i => i.Key, i => i.Value);
                var proxyInfoBlock = message["proxy"];
                var restPost = PostRestMessage(new Uri(jsonBaseAddress), jsonEndpoint, (object)jsonMessage, additionalHeaders, ParseWebProxyInfo(proxyInfoBlock));
                restPost.Wait(10000);
                if (restPost.IsFaulted)
                {
                    throw restPost.Exception;
                }
                var result = restPost.Result;
                var debugInfo = new MarvalApplicationException(String.Format("Success: {0} ({1}) - {2}", (int)result.StatusCode, result.StatusCode, result.ReasonPhrase));
                TraceHelper.Verbose(String.Format("{0}: {1}: {2}", this.ActionName, jsonIntegration, debugInfo.Message));
#if DEBUG
                // send a message to the UI
                SendMessage(this, new MessageEventArgs(sessionId, new Message
                    {
                        Heading = jsonIntegration,
                        Text = "Successful postback",
                        Type = Message.MessageTypes.Information
                    }));
#endif
            }
            catch (JsonReaderException e)
            {
                throw new MarvalApplicationException(String.Format("[{0}] {1}: Error while parsing Action Message", this.ActionName, jsonIntegration), e);
            }
            catch (HttpRequestException e)
            {
                throw new MarvalApplicationException(String.Format("[{0}] {1}: Unable to connect with {2}", this.ActionName, jsonIntegration, jsonBaseAddress ?? "<unset>"), e);
            }
            catch (Exception e)
            {
                if ((e is AggregateException) && (e.InnerException != null))
                {
                    throw new MarvalApplicationException(String.Format("[{0}] {1}: Error while communicating with {2}", this.ActionName, jsonIntegration, jsonBaseAddress ?? "<unset>"), e.InnerException);
                }
                else
                {
                    throw new MarvalApplicationException(String.Format("[{0}] {1}: Exception in JSON Integration", this.ActionName, jsonIntegration), e);
                }
            }
        }

        /// <summary>
        /// Public event for sending a message through to the UI
        /// </summary>
        public event EventHandler<MessageEventArgs> SendMessage;

        #endregion

        #region Internals

        private class WebProxyInfo
        {
            public bool DoNotUseProxy { get; set; }
            public IWebProxy Proxy { get; set; }
        }

        private WebProxyInfo ParseWebProxyInfo(JToken proxyInfoBlock)
        {
            bool doNotUseProxy = false;
            IWebProxy proxy = null;

            if (proxyInfoBlock != null)
            {
                bool.TryParse((string)proxyInfoBlock["disabled"] ?? "false", out doNotUseProxy);
                bool proxyPasswordIsEncoded;
                bool.TryParse((string)proxyInfoBlock["password_encoded"] ?? "false", out proxyPasswordIsEncoded);
                string proxyPassword;

                proxyPassword = (string)proxyInfoBlock["password"];
                if (proxyPasswordIsEncoded)
                {
                    try
                    {
                        proxyPassword = Encoding.UTF8.GetString(Convert.FromBase64String(proxyPassword));
                    }
                    catch
                    {
                        proxyPassword = "";
                    }
                }
                if (! doNotUseProxy)
                {
                    proxy = WebRequest.GetSystemWebProxy();
                    proxy.Credentials = new NetworkCredential((string)proxyInfoBlock["username"] ?? "", proxyPassword ?? "");

                }
            }
            return new WebProxyInfo()
            {
                DoNotUseProxy = doNotUseProxy,
                Proxy = proxy
            };
        }

        private async Task<HttpResponseMessage> PostRestMessage(Uri baseAddress, string restRelativeEndpoint, object jsonMessage, Dictionary<string, string> additionalHeaders = null, WebProxyInfo proxyInfo = null)
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
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    if (additionalHeaders != null)
                    {
                        foreach (var additionalHeader in additionalHeaders)
                        {
                            client.DefaultRequestHeaders.Add(additionalHeader.Key, additionalHeader.Value);
                        }
                    }
                    response = await client.PostAsJsonAsync(restRelativeEndpoint, jsonMessage);
                    response.EnsureSuccessStatusCode();
                }
            }
            return response;
        }

        #endregion

    }
}
