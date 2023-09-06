namespace AnkiStatic

open System.IO

module Program =
    [<EntryPoint>]
    let main _ =
        let outputFile = FileInfo "/tmp/media"

        let database = Sqlite.createEmptyPackage outputFile |> fun t -> t.Result
        0
