using System;
using MarvalSoftware.Data;
using MarvalSoftware.ExceptionHandling;
using MarvalSoftware.Extensions;
using Serilog;

namespace MarvalSoftware.Servers.CustomAction.Example
{
    public class SampleAction : IIntegrationAction
    {
        #region Implementation of IIntegrationAction

        /// <summary>
        /// Providing a name for your custom action is Required
        /// </summary>
        public SampleAction()
        {
            this.ActionName = "SampleAction";
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
            try
            {
                MySampleMessageClass myMessageClass = integrationMessage.XmlDeserialize<MySampleMessageClass>();

                if (myMessageClass != null)
                {
                    // this sample custom action does nothing more than send a message to the UI
                    SendMessage(this, new MessageEventArgs(sessionId, new TrayMessage
                    {
                        Heading = Resource.ResourceManager[this.ActionName],
                        Text = string.Format("The sample action executed with action message {0}", myMessageClass.MyMessage),
                        Type = TrayMessage.MessageTypes.Information
                    }));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, this.ActionName);
                ExceptionHandler.Publish(new Exception(string.Format("[{0}] {1}", this.ActionName, ex.Message)));
            }
        }

        /// <summary>
        /// Public event for sending a message through to the UI
        /// </summary>
        public event EventHandler<MessageEventArgs> SendMessage = delegate { };

        #endregion

        #region Internals

        public class MySampleMessageClass
        {
            public string MyMessage { get; set; }
        }

        #endregion
    }
}
