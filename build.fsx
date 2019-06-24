#r "paket: groupref netcorebuild //"
#load ".fake/build.fsx/intellisense.fsx"
#if !FAKE
#r "Facades/netstandard"
#r "netstandard"
#endif

#nowarn "52"

open System.Text.RegularExpressions
open Fake.Core
open Fake.Tools
open Fake.Api

let root = __SOURCE_DIRECTORY__

let gitOwner = "MangelMaxime"
let repoName = "FAKE-issue-2328"

////////////////////////////////////
// The next functions are a "static" version of the one used in Thoth.Json.Net repo
// I keep it similar to have the same structure of code for "Release" target
let getLastVersion () = "1.0.0"

let isPreRelease (version : string) =
    let regex = Regex(".*(alpha|beta|rc).*", RegexOptions.IgnoreCase)
    regex.IsMatch(version)

let getNotes (version : string) =
    [ "### Added"
      ""
      "* Initial release" ]

////////////////////////////////////

Target.create "Release" (fun _ ->
    let version = getLastVersion()

    match Git.Information.getBranchName root with
    | "master" ->
        Git.Staging.stageAll root
        let commitMsg = sprintf "Release version %s" version
        Git.Commit.exec root commitMsg
        Git.Branches.push root

        let token =
            match Environment.environVarOrDefault "GITHUB_TOKEN" "" with
            | s when not (System.String.IsNullOrWhiteSpace s) -> s
            | _ -> failwith "The Github token must be set in a GITHUB_TOKEN environmental variable"

        GitHub.createClientWithToken token
        |> GitHub.draftNewRelease gitOwner repoName version (isPreRelease version) (getNotes version)
        |> GitHub.uploadFile "./build.fsx"
        |> GitHub.publishDraft
        |> Async.RunSynchronously

    | _ -> failwith "You need to be on the master branch in order to create a Github Release"

)

Target.runOrDefault "Release"
