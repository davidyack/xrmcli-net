# xrmcli

Simple command line tool to help upload Web Resources and Plugin Assembly updates

### Connect
xrmcli /Action Connect /Server https://orgname.crm.dynamics.com /User me@orgname.onmicrosoft.com

It will prompt for password which will be stored using Windows Data Encryption https://msdn.microsoft.com/en-us/library/ms995355.aspx in isolated storage for the application

### Clear
xrmcli /Action Clear

Clear saved Connection info

### WhoAmI
xrmcli /Action WhoAmI 

simple action to check if everything is working

### DeployWebResource
xrmcli /Action DeployWebResource /UniqueName dave_mywebresource /DisplayName Test /File mywebresource.htm

Upload a web resource , will update if already exists

### DeployAssembly
xrmcli /Action DeployAssembly /File myplugin.dll

or
xrmcli /Action DeployAssembly /FilePath c:\myplugins

Will update either a single plugin assembly or all plugin assemblies found in a file path
