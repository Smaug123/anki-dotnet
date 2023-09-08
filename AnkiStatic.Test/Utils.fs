namespace AnkiStatic.Test

open System
open System.IO
open System.Reflection

[<RequireQualifiedAccess>]
module Utils =

    let readResource (assembly : Assembly) (name : string) : string =
        let names =
            assembly.GetManifestResourceNames ()
            |> Seq.filter (fun s -> s.EndsWith (name, StringComparison.Ordinal))

        use stream =
            names
            |> Seq.exactlyOne
            |> assembly.GetManifestResourceStream
            |> fun s -> new StreamReader (s)

        stream.ReadToEnd ()
