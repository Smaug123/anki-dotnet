namespace AnkiStatic

open System.Text

[<RequireQualifiedAccess>]
module internal Base91 =

    // Replicating the Anki algorithm
    let private chars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
        + "0123456789"
        + "!#$%&()*+,-./:;<=>?@[]^_`{|}~"

    let toString (input : uint64) : string =
        let output = StringBuilder ()
        let mutable input = input

        while input > 0uL do
            let modded = int (input % (uint64 chars.Length))
            let div = input / (uint64 chars.Length)
            input <- div
            output.Append chars.[modded] |> ignore

        output.ToString ()
