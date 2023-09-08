namespace AnkiStatic

open System
open System.Text.Json

type Deck =
    {
        // We'll assume newToday, revToday, lrnToday, timeToday are all [0,0]
        Name : string
        ExtendedReviewLimit : int option
        ExtendedNewCardLimit : int option
        UpdateSequenceNumber : int
        Collapsed : bool
        BrowserCollapsed : bool
        Description : string
        LastModified : DateTimeOffset
    }

[<RequireQualifiedAccess>]
module Deck =
    let toJson (id : int64) (model : DateTimeOffset option) (this : Deck) : string =
        let extendRev =
            match this.ExtendedReviewLimit with
            | None -> ""
            | Some rev -> sprintf "\"extendRev\": %i," rev

        let extendNew =
            match this.ExtendedNewCardLimit with
            | None -> ""
            | Some lim -> sprintf "\"extendNew\": %i," lim

        let model =
            match model with
            | None -> ""
            | Some model -> model.ToUnixTimeMilliseconds () |> sprintf "\"mod\": %i,"

        // TODO: what is `conf`?
        $"""{{
    "name": %s{JsonSerializer.Serialize this.Name},
    "desc": %s{JsonSerializer.Serialize this.Description},
    %s{extendRev}
    "usn": %i{this.UpdateSequenceNumber},
    "collapsed": %b{this.Collapsed},
    "newToday": [0,0],
    "timeToday": [0,0],
    "revToday": [0,0],
    "lrnToday": [0,0],
    "dyn": 0,
    %s{model}
    %s{extendNew}
    "conf": 1,
    "id": %i{id},
    "mod": %i{this.LastModified.ToUnixTimeSeconds ()}
}}"""


[<Measure>]
type deck
