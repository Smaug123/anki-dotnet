namespace AnkiStatic

open System

[<RequireQualifiedAccess>]
type CardType =
    | New
    | Learning
    | Review
    | Relearning

    member this.ToInteger () =
        match this with
        | CardType.New -> 0
        | CardType.Learning -> 1
        | CardType.Review -> 2
        | CardType.Relearning -> 3

[<RequireQualifiedAccess>]
type Queue =
    | UserBuried
    | SchedulerBuried
    | Buried
    | Suspended
    | New
    | Learning
    | Review
    | InLearning
    | Preview

    member this.ToInteger () =
        match this with
        | Queue.UserBuried -> -3
        // Yes, there's an overlap. The two scheduling algorithms
        // interpret -2 in a slightly different sense.
        | Queue.SchedulerBuried
        | Queue.Buried -> -2
        | Queue.Suspended -> -1
        | Queue.New -> 0
        | Queue.Learning -> 1
        | Queue.Review -> 2
        | Queue.InLearning -> 3
        | Queue.Preview -> 4

[<RequireQualifiedAccess>]
type Interval =
    | Seconds of int
    | Days of int
    | Unset

    member this.ToInteger () =
        match this with
        | Interval.Unset -> 0
        | Interval.Days d -> d
        | Interval.Seconds s -> -s

/// Ease of 1000 means "no bias".
/// Ease of 2500 means "this is 2.5x easier", so intervals get 2.5xed.
[<Measure>]
type ease

/// We don't model cards in a filtered deck.
type Card<'Note, 'Deck> =
    {
        CreationDate : DateTimeOffset
        NotesId : 'Note
        DeckId : 'Deck
        Ordinal : int
        ModificationDate : DateTimeOffset
        UpdateSequenceNumber : int
        Type : CardType
        Queue : Queue
        Due : int
        Interval : Interval
        EaseFactor : int<ease>
        NumberOfReviews : int
        NumberOfLapses : int
        Left : int
        OriginalDue : int
        /// A client-defined extra bitmask.
        Flags : int
        /// Currently unused.
        Data : string
    }

[<RequireQualifiedAccess>]
type NewCardDistribution =
    /// See new cards mixed in with reviews of old cards
    | Distribute
    /// See new cards after reviewing old cards
    | Last
    /// See new cards before reviewing old cards
    | First

    member this.ToInteger () =
        match this with
        | NewCardDistribution.Distribute -> 0
        | NewCardDistribution.Last -> 1
        | NewCardDistribution.First -> 2
