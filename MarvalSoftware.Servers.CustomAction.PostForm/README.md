# Generic `PostForm` CustomAction for MSM


## Dependencies

Make sure that the following assemblies are copied into `msmd`'s folder,
besides `CustomActionPostForm.dll` itself:

**N/A**


## Example Action Message:

```xml
    <FormPostMessage integrationName="SMS Integration">
        <BaseUrl>http://192.0.2.1/</BaseUrl>
        <PostEndpoint>api/send-sms</PostEndpoint>
        <Proxy disable="false">
            <Username>svc-msm-proxy_usr</Username>
            <Password encoded="true">QmFzZTY0X2VuY29kZWRfcGFzc3dvcmQ="</Password>
        </Proxy>
        <Headers>
            <Header key="Pragma">no-cache</Header>
            <Header key="X-MSM-Version">@(Model.Assignee.GetType().Assembly.GetName().Version.ToString(3))</Header>
        </Headers>
        <FormValues>
            <FormValue key="message_text">Major incident: @Model.FullRequestNumber Description: @Model.Description</FormValue>
            <FormValue key="receiver">@Model.Contact.TelephoneNumber</FormValue>
        </FormValues>
    </FormPostMessage>
```

### Notes

- The `integrationName` attribute is optional
- `BaseURL` is required for the CustomAction to function
- `PostEndpoint` can be left empty if `BaseURL` already contains the full URL.
- `Proxy` can be an empty (self-closing) tag
- `Headers` can be an empty (self-closing) tag
- `FormValues` can be an empty (self-closing) tag in the odd case that no actual form data needs to be sent over.
