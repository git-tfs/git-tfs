To clone a tfs repository on visualstudio.com, you should :
 * Enable alternate credentials. Go to "My profile"->"CREDENTIALS"-> Enter data and "Enable alternate credentials"
 * use the command following this pattern (where 'yourLogin' and 'yourPassword' was defined in the previous step)
`git tfs clone https://[user].visualstudio.com/DefaultCollection $/project/folder  --username=yourLogin --password=yourPassword`

To succeed, visualstudio.com needs (for authentication) that you use the TFS2012 version of the TFS dlls. Otherwise, you will see the message :

`TF400813: Resource not available for anonymous access. Client authentication required`

To be sure that git-tfs uses this version, use the command `git tfs --version` and you should see the text `TFS client library 11.0.0.0`, like in:

`git-tfs version 0.17.2.0 (TFS client library 11.0.0.0 (MS)) (64-bit)`
