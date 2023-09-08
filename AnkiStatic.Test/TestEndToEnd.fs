namespace AnkiStatic.Test

open AnkiStatic
open NUnit.Framework
open System
open System.IO

[<TestFixture>]
module TestEndToEnd =
    type private Dummy =
        class
        end

    [<TestCase "example1.json">]
    let ``End-to-end test of example1.json`` (fileName : string) =
        let assembly = typeof<Dummy>.Assembly
        let json = Utils.readResource assembly fileName

        let collection, notes = JsonCollection.deserialise json |> JsonCollection.toInternal

        let outputFile =
            Path.GetTempFileName ()
            |> fun f -> Path.ChangeExtension (f, ".apkg")
            |> FileInfo

        let collection = SerialisedCollection.toSqlite collection

        Sqlite.writeAll (Random 1) collection notes outputFile |> fun t -> t.Result

        Console.WriteLine $"Written file: %s{outputFile.FullName}"
