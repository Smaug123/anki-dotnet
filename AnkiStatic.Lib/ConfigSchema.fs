namespace AnkiStatic

open System.IO

[<RequireQualifiedAccess>]
module AnkiStatic =

    let getSchema () : Stream =
        let resource = "AnkiStatic.Lib.anki.schema.json"
        let assembly = System.Reflection.Assembly.GetExecutingAssembly ()
        let stream = assembly.GetManifestResourceStream resource

        match stream with
        | null -> failwithf "The resource %s was not found. This is a bug in the tool." resource
        | stream -> stream
