namespace AnkiStatic.Test

open NUnit.Framework
open AnkiStatic
open FsUnitTyped

[<TestFixture>]
module TestJson =

    type private Dummy =
        class
        end

    [<Test>]
    let ``Can read example`` () =
        let assembly = typeof<Dummy>.Assembly
        let json = Utils.readResource assembly "example1.json"

        let collection, notes = JsonCollection.deserialise json |> JsonCollection.toInternal
        ()
