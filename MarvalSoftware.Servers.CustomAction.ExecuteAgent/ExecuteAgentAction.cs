using System;
using MarvalSoftware.Data;
using MarvalSoftware.Diagnostics;
using MarvalSoftware.ExceptionHandling;
using MarvalSoftware.Extensions;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MarvalSoftware.Servers.CustomAction.ExecuteAgent
{
    public class ExecuteAgentAction : IIntegrationAction
    {
        #region Implementation of IIntegrationAction

        /// <summary>
        /// The integration action name (is being set by the configuration in the MSM Module Loader)
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        ///     Executes the action, executing an agent with the supplied parameters.
        /// </summary>
        /// <param name="sessionId">The session Id of the user executing this action.</param>
        /// <param name="integrationMessage">Accepts a string integration message that can be deserialized into the required class.</param>
        public void Execute(string sessionId, string integrationMessage)
        {
            string agentIntegration = "Unknown Agent Integration";
            string agentFullPath = "<unknown>";
            try
            {
                AgentActionMessage executeAgentMessage = integrationMessage.XmlDeserialize<AgentActionMessage>();
                agentIntegration = executeAgentMessage.IntegrationName ?? "Agent Integration";
                agentFullPath = executeAgentMessage.AgentFullPath;
                if (! File.Exists(agentFullPath))
                {
#if DEBUG
                    // send a message to the UI
                    SendMessage(this, new MessageEventArgs(sessionId, new TrayMessage
                    {
                        Heading = agentIntegration,
                        Text = String.Format("Agent executable not found: {0}", agentFullPath),
                        Type = TrayMessage.MessageTypes.Alert
                    }));
#endif
                    var logException = new MarvalApplicationException(String.Format(" Agent executable not found: {0} ", agentFullPath));
                    TraceHelper.Error(String.Format("{0}: {1}: {2}", this.ActionName, agentIntegration, logException.Message));
                    return;
                }
                var agentExecution = ExecuteAgent(agentFullPath, executeAgentMessage.Parameters.Select(p => p.Value).ToArray());
                agentExecution.Wait(10000);
                if (agentExecution.IsFaulted)
                {
                    throw agentExecution.Exception;
                }
                var result = agentExecution.Result;
                var debugInfo = new MarvalApplicationException(String.Format("Success: {0} ", result));
                TraceHelper.Verbose(String.Format("{0}: {1}: {2}", this.ActionName, agentIntegration, debugInfo.Message));
#if DEBUG
                // send a message to the UI
                SendMessage(this, new MessageEventArgs(sessionId, new TrayMessage
                {
                    Heading = agentIntegration,
                    Text = "Successful agent execution",
                    Type = TrayMessage.MessageTypes.Information
                }));
#endif
            }
            catch (Exception e)
            {
                TraceHelper.Error(String.Format("{0}: {1}: {2}", this.ActionName, agentIntegration, (e.InnerException ?? e).Message));
                if ((e is AggregateException) && (e.InnerException != null))
                {
                    ExceptionHandler.Publish(new MarvalApplicationException(String.Format("[{0}] {1}: Error while executing {2}", this.ActionName, agentIntegration, agentFullPath ?? "<unset>"), e.InnerException));
                }
                else
                {
                    ExceptionHandler.Publish(new MarvalApplicationException(String.Format("[{0}] {1}: Exception in agent integration", this.ActionName, agentIntegration), e));
                }
            }
        }

        /// <summary>
        /// Public event for sending a message through to the UI
        /// </summary>
        public event EventHandler<MessageEventArgs> SendMessage;

        #endregion

        #region Public implementation details

        public struct Parameter
        {
            [XmlText]
            public string Value { get; set; }
        }
        public class AgentActionMessage
        {
            [XmlAttribute(AttributeName = "integrationName")]
            public string IntegrationName { get; set; }
            public string AgentFullPath { get; set; }
            public Parameter[] Parameters { get; set; }
        }

        #endregion

        #region Internals

        private async Task<int> ExecuteAgent(string agentFullPath, string[] parameters)
        {
            return await RunProcessAsync(agentFullPath, String.Join(" ", parameters)).ConfigureAwait(false);
        }

        private static async Task<int> RunProcessAsync(string fileName, string args, bool redirectOutputToConsole = false)
        {
            using (var process = new Process
            {
                StartInfo =
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = !redirectOutputToConsole,
                    RedirectStandardOutput = redirectOutputToConsole,
                    RedirectStandardError = redirectOutputToConsole,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            })
            {
                return await RunProcessAsync(process, redirectOutputToConsole).ConfigureAwait(false);
            }
        }
        private static Task<int> RunProcessAsync(Process process, bool redirectOutputToConsole = false)
        {
            var tcs = new TaskCompletionSource<int>();

            process.Exited += (s, ea) => tcs.SetResult(process.ExitCode);
            if (redirectOutputToConsole)
            {
                process.OutputDataReceived += (s, ea) => Console.WriteLine(ea.Data);
                process.ErrorDataReceived += (s, ea) => Console.WriteLine("ERR: " + ea.Data);
            }

            bool started = process.Start();
            //if (!started)
            //{
            //    throw new InvalidOperationException("Could not start process: " + process);
            //}

            if (redirectOutputToConsole)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            return tcs.Task;
        }

        #endregion
    }

}
