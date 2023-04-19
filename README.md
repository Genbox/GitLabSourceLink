# GitLabSourceLink

[![NuGet](https://img.shields.io/nuget/v/Genbox.GitLabSourceLink.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Genbox.GitLabSourceLink/)
[![License](https://img.shields.io/github/license/Genbox/GitLabSourceLink)](https://github.com/Genbox/GitLabSourceLink/blob/master/LICENSE.txt)

### Description
Enable source linking with self-managed GitLab repositories.

This is not a fully-feature package with all the same bells and whistles as Microsoft's own [source link packages](https://github.com/dotnet/sourcelink).
It is mostly intended as a proof-of-concept to demonstrate that it is possible to get source linking to work without using a proxy or any server-side hacks.

This is not intended for repositories hosted on [gitlab.com](https://gitlab.com). If you need source linking for public repositories on gitlab.com, you should use [Microsoft.SourceLink.GitLab](https://www.nuget.org/packages/Microsoft.SourceLink.GitLab) instead.

### How to use?
It is pretty simple. Just do the following:

1. Your project must be hosted on a self-managed GitLab instance, and the source code must be checked out via git from that instance.
2. Reference the [nuget package](https://www.nuget.org/packages/Genbox.GitLabSourceLink/).

If you then package your project as a NuGet package, it should have source linking information embedded.
You can use [NuGet Package Explorer](https://apps.microsoft.com/store/detail/nuget-package-explorer/9WZDNCRDMDM3) to verify it.

Note that this package automatically use relative paths in the source linking process to support debugging on computers with different source locations.

### Why not just use Microsoft.SourceLink.GitLab?

There are two primary issues
1. It turns out [GitLab don't want to support HTTP Basic authentication](https://gitlab.com/gitlab-org/gitlab/-/issues/19189) on their raw endpoint (Looks like this: `https://mygit.com/Project/-/raw/develop/myfile.cs`), which is the default auth protocol source linking clients use.
2. GitLab return HTTP error code 302 (Found) and redirect to the login page, instead of returning 403 (Forbidden). This makes it impossible for clients to know authentication is required.

The first issue can be solved. Most clients (Visual Studio, Visual Studio Code, Jetbrains Rider) supports [Git Credential Manager](https://github.com/git-ecosystem/git-credential-manager) and should therefore be able to authenticate via OAuth.
But due to the second issue, the client does not know that it should authenticate, and as such it returns the HTML for the login page, rather than the source code.

The second issue could probably easily be solved with some hacking in the GitLab code, but any custom changes are likely to disappear with the next patch update.
To solve the issue, GitLab has an [API to retrieve files](https://docs.gitlab.com/ee/api/repository_files.html#get-raw-file-from-repository) and it returns 403 (Forbidden) when no auth header is provided.

So we could just rewrite the Microsoft.SourceLink.GitLab code to use the API, right? If only it was that easy.
Turns out GitLab API urls needs to be URL encoded, and the URLs provided by the source link client are not encoded, so you'll get 404 on all requests, even if they have authentication headers.

Others have tried to solve the problem:
- [GitLab Source Link Proxy](https://github.com/rgl/gitlab-source-link-proxy)
- [GReWritter](https://github.com/juangburgos/GitlabRewritter)

They all require either that you run a proxy locally on the development computer, or that you change the GitLab server code.
My solution does not require anything but a nuget package be installed, just like Microsoft.SourceLink.GitLab. Checkout the `How does it work` section if you want the details.

### How does it work?
When you reference the package you get a targets file loaded in MsBuild, which uses Microsoft.SourceLink.Common to get some common information such as SourceRoot, untracked files, revision of the latest commit etc.
This information is then feed into an MsBuild task that iterates all the files that was compiled, remove the untracked sources (files in obj mostly) and then generate relative links to each file.

For example, A file like `C:\Source\Project\SubFolder\MyClass.cs` with a git repo root of  `C:\Source\Project\`, and a git remote origin of `https://mygit.com/Project`, and a commit id of `e631b672a7659c0d2f4f1f89de46fd135a7b5286`
should eventually end up as the url:

`https://mygit.com/api/v4/projects/Group%2FProject/repository/files/SubFolder%2FMyClass.cs/raw?ref=e631b672a7659c0d2f4f1f89de46fd135a7b5286`

the `api/v4/projects/` part is the GitLab v4 API which can be used to retrieve files. GitLab require two parts to be URL encoded:
- Project Id
- File Id

The Project Id part is `/Group%2FProject/` above. `%2F` is `/` URL encoded.
The File Id part is the `SubFolder%2FMyClass.cs`. As you can see, it is also URL encoded.

The MsBuild task generate this URL with the correct parts encoded and then it outputs them into a JSON format supported by [source link](https://github.com/dotnet/designs/blob/main/accepted/2020/diagnostics/source-link.md).

It looks like this:
```json
{
    "documents": {
        "C:\\Source\\Project\\SubFolder\\MyClass.cs": "https://mygit.com/api/v4/projects/Group%2FProject/repository/files/SubFolder%2FMyClass.cs/raw?ref=eff9fa13cff546a28f2aacdf733909f03b732c5f"
    }
}
```

This is then read by the compiler if you supply it with the path to the source link JSON file, and then it embed the path and URLs so you can click Go To Source in your IDE and see the source code.