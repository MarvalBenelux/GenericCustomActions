# Generic CustomActions for MSM


## Building

- Make sure that the correct versions of the original MSM assemblies have
been placed in `Binary References`.
- Restore NuGet packages for the entire solution. 
- Rebuild the entire solution.


## Installation

Any CustomAction can be installed by simply placing its assembly and
any other assemblies it depends on into the `Services` folder (in the 
`MSM` installation folder) where the MSM Background service 
(`msmd.exe`) can be found. The `readme.md` files of the respective
CustomActions describe which dependencies are needed for it to work.


## Usage

In MSM's `Request Actions`, use **Execute Custom Action** in the 
Then-actions and manually write the Custom Action name (like 
SampleAction, PostJSON, etc.) in the next field. Now, also select an
action message in the correct format to send over to the Custom Action.

## Currently included generic CustomActions:

- [SampleAction](MarvalSoftware.Servers.CustomAction.Example/)
  The example as distributed by Marval Software Ltd. This is the 
  version as distributed with v14.4 up to at least v14.10 and probably 
  much higher.

- [PostJSON](MarvalSoftware.Servers.CustomAction.PostJSON/)
  A generic CustomAction for the MSM Background Service that can POST 
  JSON to any HTTP(S) REST endpoint. Proxy authentication is supported.
  Action Messages are expected to be in the JSON format themselves.

- [PostForm](MarvalSoftware.Servers.CustomAction.PostForm/)
  A generic CustomAction for the MSM Background Service that can POST 
  `x-form-urlencoded` data via HTTP(S) to any website / web application.
  Action Messages are expected to be in XML format.

- [ExecuteAgent](MarvalSoftware.Servers.CustomAction.ExecuteAgent/)
  A generic CustomAction for the MSM Background Service that can 
  execute executables, batch scripts, Powershell scripts, etc.
  It runs these 'agents' on the server itself and parameters / 
  arguments can be passed in.
  Action Messages are expected to be in XML format.


## Contributing

We welcome all feedback including feature requests and bug reports.
Please raise these as issues on [GitHub](https://github.com/MarvalBenelux/GenericCustomActions).
If you would like to contribute to the project please fork the
repository and issue a pull request.


## Advanced setups

### Including Razor code in Action Messages

It is very much possible to include extra code in front of the rest of
the Action Messages using the proper Razor syntax.

**Example 1 for ExecuteAgent**

        @{

            string monitoringId = "";
            foreach (var item in Model.Attributes)
            {
                if (item.Type.Identifier == 1252)
                {
                    monitoringId = item.Value;
                }
            }

            string requestNumber = @Model.RequestNumber;

        }<AgentActionMessage integrationName="MonitoringTool - Event closed">
            <AgentFullPath>D:\Interfaces\MonitoringTool\report_event_closed.bat</AgentFullPath>
            <Parameters>
                <Parameter>/id="@monitoringId"</Parameter>
                <Parameter>/message="Event @requestNumber has been closed in MSM."</Parameter>
            </Parameters>
        </AgentActionMessage>

**Example 2 for PostJSON**

    @using MarvalSoftware
    @using System.Globalization
    @using System.Text
    @using System.Text.RegularExpressions
    @{
        var FormatSlackDateTime = new Func<DateTime, string, string>( (DateTime datetimeUtc, string slackFormat) =>
        //string FormatSlackDateTime(DateTime datetimeUtc, string slackFormat = "{date_pretty} {time}")
        {
            if (String.IsNullOrEmpty(slackFormat))
            {
                slackFormat = @"{date_pretty} {time}";
            }
            string dtFallback = TimezoneHelper.ToLocalTime(datetimeUtc).ToString(new CultureInfo("nl-NL"));
            int epochTimestamp = (int)(datetimeUtc - new DateTime(1970, 1, 1)).TotalSeconds;
            return String.Format("<!date^{0}^{1}|{2}>", epochTimestamp, slackFormat, dtFallback);
        });
        
        var CleanHtml = new Func<string, string>( (string dirtyString) =>
        // string CleanHtml(string dirtyString)
        {
            return Regex.Replace(System.Net.WebUtility.HtmlDecode(dirtyString).Replace("<br>", "\n"), "<.*?>", String.Empty).Trim();
        }
        );

        var JsonString = new Func<string, string>( (string plainText) =>
        // string JsonString(string plainText)
        {
            //return  JsonConvert.ToString(plainText);
            // Very poor man's quoting while we're not able to @using Newtonsoft.JSON from within ActionMessages:
            var output = new StringBuilder(plainText.Length);
            foreach (var c in plainText)
            {
                switch (c)
                {
                    case '\"':
                    case '\\':
                        output.AppendFormat("{0}{1}", '\\', c);
                        break;
                    case '\b':
                        output.Append("\\b");
                        break;
                    case '\f':
                        output.Append("\\f");
                        break;
                    case '\t':
                        output.Append("\\t");
                        break;
                    case '\n':
                        output.Append("\\n");
                        break;
                    case '\r':
                        output.Append("\\r");
                        break;
                    default:
                        output.Append(c.ToString());
                        break;
                }
            }
            return String.Format("\"{0}\"", output.ToString());
        }
        );

        /* Get details of the last note for the request (if available) */
        var lastNote      = (Model.Notes.Count < 1) ? null : Model.Notes[Model.Notes.Count-1];
        var noteAuthor    = lastNote == null ? "" : lastNote.Author.Name;
        var noteDate      = lastNote == null ? "" : FormatSlackDateTime(lastNote.Created, @"{date_long_pretty} {time}");
        var noteType      = lastNote == null ? "" : lastNote.Type;
        var noteContents  = lastNote == null ? "" : CleanHtml(lastNote.Content).Replace(": :",":\n\t");
        var markedUpNote  = lastNote == null ? "_No notes_" : String.Format("{0}  |  {1}  |  {2}\n```{3}```", noteAuthor, noteDate, noteType, noteContents);

        /* Encode Slack details */
        string endpoint   = JsonString("SUPER/SECRET/SlackApiKeyForYourOrganisation");
        string footer     = JsonString("MSM " + (Model.Assignee.GetType().Assembly.GetName().Version.ToString(3)) + " > Notes added notifier");
        string footerIcon = JsonString("https://servicedesk.example.com/MSM/Assets/Skins/Marval%20Gold/icons/notes_32.png");
        string linkName   = JsonString(String.Format("{0}", Model.FullRequestNumber));
        string linkUrl    = JsonString(String.Format("https://servicedesk.example.com/MSM/RFP/Forms/Request.aspx?id={0}", Model.Identifier));
        string linkIcon   = JsonString(String.Format("https://servicedesk.example.com/MSM/Assets/Skins/Marval%20Gold/icons/{0}_16.png", Model.RequestType.BaseType.ToString().Replace("ChangeRequest","Change")));
        string pretext    = JsonString(String.Format("*New notes* have been added to {0} {1} by {2}", Model.RequestType.Name, Model.FullRequestNumber, Model.Notes[Model.Notes.Count-1].Author.Name));
        string title      = JsonString(String.Format("{0}", Model.Description));
        string text       = JsonString(markedUpNote);
        string fallback   = JsonString(String.Format("{0}: New notes have been added to this {1} by {2}", Model.FullRequestNumber, Model.RequestType.Name, Model.Notes[Model.Notes.Count-1].Author.Name));

    }{
        "integration": "Slack",
        "baseaddress": "https://hooks.slack.com/services/",
        "endpoint": @endpoint,
        "headers" : [
            {
                "header": "X-MSM-Version",
                "value": "@(Model.Assignee.GetType().Assembly.GetName().Version.ToString(3))"
            }
        ],
        "message": {
            "channel": "#sd-all-updates",
            "attachments": [
                {
                    "mrkdwn_in": ["text", "pretext"],
                    "fallback": @fallback,
                    "color": "warning",
                    "pretext": @pretext,
                    "author_name": @linkName,
                    "author_link": @linkUrl,
                    "author_icon": @linkIcon,
                    "title": @title,
                    "text": @text,
                    "fields": [
                        {
                            "title": "Priority",
                            "value": "@Model.Priority.Value",
                            "short": true
                        },
                        {
                            "title": "Status",
                            "value": "@Model.CurrentStatus.Status.Status.Name",
                            "short": true
                        },
                        {
                            "title": "Customer",
                            "value": "@Model.Customer.Represents.Name (@Model.Contact.Name)",
                            "short": true
                        },
                        {
                            "title": "Occurred Date",
                            "value": "@(FormatSlackDateTime(Model.DateOccurred, @"{date_long_pretty} {time}"))",
                            "short": true
                        }
                    ],
                    "footer": @footer,
                    "footer_icon": @footerIcon,
                    "ts": @((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds)
                }
            ]
        }
    }
