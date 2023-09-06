namespace AnkiStatic

open System
open System.Security.Cryptography
open System.Text

type Note<'Model> =
    {
        Guid : uint64
        ModelId : 'Model
        LastModified : DateTimeOffset
        UpdateSequenceNumber : int
        /// Serialised space-separated as a string, with a space at the start and end.
        Tags : string list
        /// Serialised as a string separated by the 0x1f character
        Fields : string list
        /// In the Sqlite table, this is an int field.
        /// Sqlite is dynamically typed and accepts strings in an int field.
        /// But it will sort "correctly" in the sense that integers are compared as integers
        /// for the purpose of sorting in this way.
        SortField : Choice<string, int>
        /// Unused
        Flags : int
        /// Unused
        Data : string
    }

    member this.Checksum : uint =
        let fromBase256 (firstCount : int) (bytes : byte[]) : uint =
            let mutable answer = 0u

            for b = 0 to firstCount - 1 do
                answer <- answer * 256u
                answer <- answer + uint bytes.[b]

            answer

        use sha1 = SHA1.Create ()
        // TODO: in the wild, this actually strips HTML first
        this.Fields.[0] |> Encoding.UTF8.GetBytes |> sha1.ComputeHash |> fromBase256 4

[<Measure>]
type note
