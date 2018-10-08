# Generic `PostJSON` CustomAction for MSM


## Dependencies

Make sure that the following assemblies are copied into `msmd`'s folder,
besides `CustomActionPostJSON.dll` itself:

- System.Net.Http.Formatting.dll


## Example Action Message:

```json
    {
        "integration": "IntegrationName",
        "baseaddress": "https://example.com/restapi/",
        "endpoint": "interestingmodule/resource",
        "proxy": {
            "username": "svc-msm-proxy_usr",
            "password": "QmFzZTY0X2VuY29kZWRfcGFzc3dvcmQ=",
            "password_encoded": true
        },
        "headers" : [
            {
                "header": "Authorization",
                "value": "Bearer 1234567890"
            },
            {
                "header": "X-MSM-Version",
                "value": "@(Model.Assignee.GetType().Assembly.GetName().Version.ToString(3))"
            }
        ],
        "message": {
        }
    }
```

### Notes

- `integration` is optional
- `baseaddress` is needed to have a functioning integration; `endpoint` can be left empty if `baseaddress` already contains the full URL
- `proxy` is optional
- `headers` can be an empty array
- `message` contains the actual (JSON) message being sent over
