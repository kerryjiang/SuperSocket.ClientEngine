# Test

Open `../global.json`, and remove `sdk`, and save like:

```json
{
  "projects": [
    ".", "SuperSocket.ClientEngine", "Test"
  ]
}
```

Launch test by `dotnet test`

```bat
D:\Proj\SuperSocket.ClientEngine\Test>dotnet test
???????????????????????...
???????????

D:\Proj\SuperSocket.ClientEngine\Test\bin\Debug\netcoreapp1.0\Test.dll(.NETCoreApp,Version=v1.0) ??????
Microsoft (R) Test Execution Command Line Tool Version 15.9.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...

Total tests: 14. Passed: 14. Failed: 0. Skipped: 0.
Test Run Successful.
Test execution time: 4.7565 Seconds
```
