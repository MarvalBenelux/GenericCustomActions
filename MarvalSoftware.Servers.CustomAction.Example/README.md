# Example `SampleAction` CustomAction for MSM


## Dependencies

Make sure that the following assemblies / dependencies are copied into
the `Services\Integration` folder:

**N/A**


## Example of `Actions` config to add

     <add ActionName="SampleAction" ActionType="MarvalSoftware.Servers.CustomAction.Example.SampleAction"
           ActionPath="C:\Program Files\Marval Software\MSM\Services\Integration\CustomActionExample.dll" />


## Example Action Message:

```xml
    <MySampleMessageClass>
        <MyMessage>Test Message for @Model.FullRequestNumber</MyMessage>
    </MySampleMessageClass>
```

### Notes

- All this example custom action does, is return the message to the
  user via a UI message (popup).