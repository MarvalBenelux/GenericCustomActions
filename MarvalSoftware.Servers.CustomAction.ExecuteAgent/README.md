# Generic `ExecuteAgent` CustomAction for MSM


## Dependencies

Make sure that the following assemblies / dependencies are copied into
the `Services\Integration` folder:

**N/A**


## Example of `Actions` config to add

     <add ActionName="ExecuteAgent" ActionType="MarvalSoftware.Servers.CustomAction.ExecuteAgent.ExecuteAgentAction"
           ActionPath="C:\Program Files\Marval Software\MSM\Services\Integration\CustomActionExecuteAgent.dll" />


## Example Action Message:

```xml
    <AgentActionMessage integrationName="MonitoringTool - Event closed">
        <AgentFullPath>D:\Interfaces\MonitoringTool\report_event_closed.bat</AgentFullPath>
        <Parameters>
            <Parameter>/message="Event @Model.RequestNumber has been closed in MSM."</Parameter>
        </Parameters>
    </AgentActionMessage>
```

### Notes

- The `integrationName` attribute is optional
- `AgentFullPtah` is required for the CustomAction to function
- `Parameters` can be an empty (self-closing) tag
