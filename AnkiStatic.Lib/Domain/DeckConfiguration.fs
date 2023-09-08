namespace AnkiStatic

open System
open System.Text.Json

[<RequireQualifiedAccess>]
type LeechAction =
    | Suspend
    | Mark

    member this.ToInteger () =
        match this with
        | LeechAction.Suspend -> 0
        | LeechAction.Mark -> 1

type LapseConfiguration =
    {
        Delays : int list
        LeechAction : LeechAction
        LeechFails : int
        MinInterval : int
        Multiplier : float
    }

    static member toJson (this : LapseConfiguration) : string =
        let delays =
            this.Delays
            |> Seq.map (fun (i : int) -> i.ToString ())
            |> String.concat ","
            |> sprintf "[%s]"

        let mult =
            if this.Multiplier <> 0.0 then
                failwith "can't yet handle this"
            else
                "0"

        $"""{{
    "leechFails": %i{this.LeechFails},
    "minInt": %i{this.MinInterval},
    "delays": %s{delays},
    "leechAction": %i{this.LeechAction.ToInteger ()},
    "mult": %s{mult}
}}"""

type IntervalConfiguration =
    {
        Good : int
        Easy : int
        Unused : int
    }

[<RequireQualifiedAccess>]
type NewCardOrder =
    | Random
    | Due

    member this.ToInteger () =
        match this with
        | NewCardOrder.Random -> 0
        | NewCardOrder.Due -> 1

type NewCardConfiguration =
    {
        Bury : bool
        Delays : int list
        InitialEase : int<ease>
        Intervals : IntervalConfiguration
        Order : NewCardOrder
        MaxNewPerDay : int
        /// Apparently unused; leave this as `true`
        Separate : bool
    }

    static member toJson (this : NewCardConfiguration) : string =
        let ints =
            [ this.Intervals.Good ; this.Intervals.Easy ; this.Intervals.Unused ]
            |> Seq.map (fun (s : int) -> s.ToString ())
            |> String.concat ","
            |> sprintf "[%s]"

        let delays =
            this.Delays
            |> Seq.map (fun (s : int) -> s.ToString ())
            |> String.concat ","
            |> sprintf "[%s]"

        $"""{{
    "perDay": %i{this.MaxNewPerDay},
    "delays": %s{delays},
    "separate": %b{this.Separate},
    "ints": %s{ints},
    "initialFactor": %i{this.InitialEase},
    "order": %i{this.Order.ToInteger ()}
}}"""

type DeckConfiguration =
    {
        AutoPlay : bool
        Lapse : LapseConfiguration
        MaxTaken : TimeSpan
        LastModified : DateTimeOffset
        Name : string
        New : NewCardConfiguration
        ReplayQuestionAudioWithAnswer : bool
        Review : ReviewConfiguration
        ShowTimer : bool
        UpdateSequenceNumber : int
    }

[<RequireQualifiedAccess>]
module DeckConfiguration =

    let toJson (id : int64) (conf : DeckConfiguration) : string =
        $"""{{
    "name": {JsonSerializer.Serialize conf.Name},
    "replayq": %b{conf.ReplayQuestionAudioWithAnswer},
    "lapse": %s{LapseConfiguration.toJson conf.Lapse},
    "rev": %s{ReviewConfiguration.toJson conf.Review},
    "timer": %i{if conf.ShowTimer then 1 else 0},
    "maxTaken": %i{int conf.MaxTaken.TotalSeconds},
    "usn": %i{conf.UpdateSequenceNumber},
    "new": %s{NewCardConfiguration.toJson conf.New},
    "mod": %i{conf.LastModified.ToUnixTimeMilliseconds ()},
    "id": %i{id},
    "autoplay": %b{conf.AutoPlay}
}}"""

[<Measure>]
type deckOption
