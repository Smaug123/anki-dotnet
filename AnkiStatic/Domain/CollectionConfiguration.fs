namespace AnkiStatic

open System
open System.Text.Json

type CollectionConfiguration<'Model, 'Deck> =
    {
        CurrentDeck : 'Deck option
        ActiveDecks : 'Deck list
        NewSpread : NewCardDistribution
        CollapseTime : int
        TimeLimit : TimeSpan
        EstimateTimes : bool
        ShowDueCounts : bool
        CurrentModel : 'Model
        NextPosition : int
        /// This has some specifically allowed values, but :shrug:
        SortType : string
        SortBackwards : bool
        /// Value of "when adding, default to current deck"
        AddToCurrent : bool
    }

[<RequireQualifiedAccess>]
module CollectionConfiguration =
    let toJsonString (this : CollectionConfiguration<DateTimeOffset, DateTimeOffset>) : string =
        let currentDeckString =
            match this.CurrentDeck with
            | None -> ""
            | Some d -> sprintf "\"curDeck\": %i," (d.ToUnixTimeMilliseconds ())

        let activeDecks =
            this.ActiveDecks
            |> List.map (fun dto -> dto.ToUnixTimeSeconds().ToString ())
            |> String.concat ","

        $"""{{
    "nextPos": %i{this.NextPosition},
    "estTimes": %b{this.EstimateTimes},
    "activeDecks": [%s{activeDecks}],
    "sortType": %s{JsonSerializer.Serialize this.SortType},
    "timeLim": %i{int this.TimeLimit.TotalSeconds},
    "sortBackwards": %b{this.SortBackwards},
    "addToCur": %b{this.AddToCurrent},
     %s{currentDeckString}
     "newSpread": %i{this.NewSpread.ToInteger ()},
    "dueCounts": %b{this.ShowDueCounts},
    "curModel": "%i{this.CurrentModel.ToUnixTimeMilliseconds ()}",
    "collapseTime": %i{this.CollapseTime}
}}"""
