# Example `SampleAction` CustomAction for MSM


## Dependencies

Make sure that the following assemblies are copied into `msmd`'s folder,
besides `CustomActionExample.dll` itself:

**N/A**


## Example Action Message:

```xml
    <MySampleMessageClass>
        <MyMessage>Test Message for @Model.FullRequestNumber</MyMessage>
    </MySampleMessageClass>
```

### Notes

- All this example custom action does, is return the message to the
  user via a UI message (popup).