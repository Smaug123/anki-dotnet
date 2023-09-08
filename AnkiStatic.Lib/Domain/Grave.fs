namespace AnkiStatic

type GraveType =
    | Card
    | Note
    | Deck

    member this.ToInteger () =
        match this with
        | GraveType.Card -> 0
        | GraveType.Note -> 1
        | GraveType.Deck -> 2

type Grave =
    {
        UpdateSequenceNumber : int
        ObjectId : int
        Type : GraveType
    }
