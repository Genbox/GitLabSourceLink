# GitLabSourceLink

[![NuGet](https://img.shields.io/nuget/v/Genbox.GitLabSourceLink.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Genbox.GitLabSourceLink/)
[![License](https://img.shields.io/github/license/Genbox/GitLabSourceLink)](https://github.com/Genbox/GitLabSourceLink/blob/master/LICENSE.txt)

## Description
Enable source linking with self-managed GitLab repositories.

This project is mostly intended as a proof-of-concept to demonstrate that [it is possible](https://github.com/dotnet/sourcelink/issues/281) to get source linking to work without using a proxy or any server-side hacks.
If you need source linking for repositories on gitlab.com, you should use [Microsoft.SourceLink.GitLab](https://www.nuget.org/packages/Microsoft.SourceLink.GitLab) instead.

## How to use?
1. Your project must be hosted on a self-managed GitLab instance, and the source code must be checked out via git from that instance.
2. Reference the [nuget package](https://www.nuget.org/packages/Genbox.GitLabSourceLink/).

If you then pack your project as a NuGet package, it should have source linking information embedded.
You can use [NuGet Package Explorer](https://apps.microsoft.com/store/detail/nuget-package-explorer/9WZDNCRDMDM3) to verify it.

## Why not just use Microsoft.SourceLink.GitLab?

There are two primary issues:
1. It turns out [GitLab don't want to support HTTP Basic authentication](https://gitlab.com/gitlab-org/gitlab/-/issues/19189) on their raw endpoint (Looks like this: `https://mygit.com/Project/-/raw/develop/myfile.cs`), which is the default auth protocol source linking clients use.
2. GitLab return HTTP error code 302 (Found) and redirect to the login page, instead of returning 403 (Forbidden). This makes it impossible for clients to detect authentication is required.

The first issue can be easily solved. Most source link clients (Visual Studio, Visual Studio Code, Jetbrains Rider) supports [Git Credential Manager](https://github.com/git-ecosystem/git-credential-manager) and should therefore be able to authenticate via OAuth pr Private Access Tokens.
But due to the second issue, the client does not know that it _should_ authenticate, and as such it returns the HTML for the login page, rather than the source code.

The second issue could probably easily be solved with some hacking in the GitLab code, but any custom changes are likely to disappear with the next patch update.
To solve the issue, GitLab has an [API to retrieve files](https://docs.gitlab.com/ee/api/repository_files.html#get-raw-file-from-repository) and it returns 403 (Forbidden) when no auth header is provided.

So we could just rewrite the Microsoft.SourceLink.GitLab code to use the API, right?
Turns out GitLab API needs certain segments to be URL encoded, and the URLs provided by the source link client are not encoded, so you'll get 404 on all requests.

Others have tried to solve the problem:
- [GitLab Source Link Proxy](https://github.com/rgl/gitlab-source-link-proxy)
- [GReWritter](https://github.com/juangburgos/GitlabRewritter)
- [GitLabProxy](https://gitlab.com/slcon/pub/repo/gitlabproxy)

They all require either that you run a proxy locally on the development computer, or between the client and GitLab, or that you change GitLab's source code.
My solution does not require anything but a nuget package. Checkout the `How does it work` section if you want the details.

## How does it work?
The nuget package contains an MsBuild targets file, which contains an task that uses Microsoft.Build.Tasks.Git to get source control information such as SourceRoot, untracked files, revision of the latest commit etc.
This information is then used to generate a list of all files that are part of the compilation, and generate an URL for each of them.

For example, A file like `C:\Source\Project\SubFolder\MyClass.cs` with a git repo root of  `C:\Source\Project\`, and a git remote of `https://mygit.com/Project`, and a commit id of `e631b672a7659c0d2f4f1f89de46fd135a7b5286`
should eventually end up as the url:

`https://mygit.com/api/v4/projects/Group%2FProject/repository/files/SubFolder%2FMyClass.cs/raw?ref=e631b672a7659c0d2f4f1f89de46fd135a7b5286`

the `api/v4/projects/` part is the GitLab v4 API which can be used to retrieve files. GitLab require two parts to be URL encoded:
- Project Id
- File Id

The Project Id part is `/Group%2FProject/` above. `%2F` is `/` URL encoded.
The File Id part is the `SubFolder%2FMyClass.cs`. As you can see, it is also URL encoded.

The task generate URLs with the correct parts encoded and write them to a [source link JSON file](https://github.com/dotnet/designs/blob/main/accepted/2020/diagnostics/source-link.md).

It looks like this:
```json
{
    "documents": {
        "/_/SubFolder/MyClass.cs": "https://mygit.com/api/v4/projects/Group%2FProject/repository/files/SubFolder%2FMyClass.cs/raw?ref=eff9fa13cff546a28f2aacdf733909f03b732c5f"
    }
}
```

The local path is converted to a relative path prefixed with `/_/`. To get the compiler to generate relative paths in the PDB, it has to have a [PathMap](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/advanced#pathmap), with is autogenerated by the MsBuild task.
When the PathMap and SourceLink variables are suppled to the compiler, it generates a PDB with source link information embedded.

## FAQ

#### Will it work with private GitLab repositories hosted on GitLab.com?
I assume it will, but have not tested it. However, I believe that you can use [Microsoft.SourceLink.GitLab](https://www.nuget.org/packages/Microsoft.SourceLink.GitLab) for the source link, and [GCM](https://github.com/git-ecosystem/git-credential-manager) in your IDE (VS, VS Code, Rider, etc.) for the authentication to gitlab.com.

#### Are there any options? Like setting repository URL, embedding of untracked sources? others?
No. This is a PoC. Everything is setup to work with most configuration.

#### Source link is working, but how do I authenticate?
Authentication is outside the scope of this project. However, I've tested both [Personal Access Tokens](https://docs.gitlab.com/ee/user/profile/personal_access_tokens.html) and [OAuth](https://github.com/git-ecosystem/git-credential-manager/blob/main/docs/gitlab.md). They work just fine.