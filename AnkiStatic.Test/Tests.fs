namespace AnkiStatic.Test

open System
open AnkiStatic
open FsUnitTyped
open NUnit.Framework
open System.Text
open System.Security.Cryptography

[<TestFixture>]
module Tests =

    [<TestCase("A continuous function on a closed bounded interval is bounded and attains its bounds.", 375454972u)>]
    let ``Checksum matches`` (str : string, expected : uint32) =
        let data : Note<unit> =
            {
                Guid = 0uL
                ModelId = ()
                LastModified = DateTimeOffset.UnixEpoch
                UpdateSequenceNumber = 0
                Tags = []
                Fields = [ str ]
                SortField = Choice2Of2 0
                Flags = 0
                Data = ""
            }

        // Obtained from reading an example in the wild
        data.Checksum |> shouldEqual expected
