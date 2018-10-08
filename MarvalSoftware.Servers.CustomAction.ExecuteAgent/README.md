# Generic `ExecuteAgent` CustomAction for MSM


## Dependencies

Make sure that the following assemblies are copied into `msmd`'s folder,
besides `CustomActionPostJSON.dll` itself:

**N/A**


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
