namespace AnkiStatic

open System

type Collection<'Model, 'Deck> =
    {
        CreationDate : DateTimeOffset
        LastModified : DateTimeOffset
        LastSchemaModification : DateTimeOffset
        Version : int
        /// Apparently unused and always 0
        Dirty : int
        UpdateSequenceNumber : int
        LastSync : DateTimeOffset
        Configuration : CollectionConfiguration<'Model, 'Deck>
        Models : Map<DateTimeOffset, ModelConfiguration<'Deck>>
        Decks : Map<DateTimeOffset, Deck>
        DeckConfigurations : Map<DateTimeOffset, DeckConfiguration>
        Tags : string
    }

[<RequireQualifiedAccess>]
module Collection =

    let getJsonDeckString (col : Collection<DateTimeOffset, DateTimeOffset>) : string =
        col.Decks
        |> Map.toSeq
        |> Seq.map (fun (dto, deck) ->
            let timestamp = dto.ToUnixTimeMilliseconds ()
            Deck.toJson timestamp None deck |> sprintf "\"%i\": %s" timestamp
        )
        |> String.concat ","
        |> sprintf "{%s}"

    let getDeckConfigurationString (col : Collection<DateTimeOffset, DateTimeOffset>) : string =
        col.DeckConfigurations
        |> Map.toSeq
        |> Seq.map (fun (dto, conf) ->
            let timestamp = dto.ToUnixTimeMilliseconds ()
            DeckConfiguration.toJson timestamp conf |> sprintf "\"%i\": %s" timestamp
        )
        |> String.concat ","
        |> sprintf "{%s}"

    let getJsonModelString (col : Collection<DateTimeOffset, DateTimeOffset>) : string =
        col.Models
        |> Map.toSeq
        |> Seq.map (fun (dto, conf) ->
            let timestamp = dto.ToUnixTimeMilliseconds ()
            ModelConfiguration.toJson timestamp conf |> sprintf "\"%i\": %s" timestamp
        )
        |> String.concat ","
        |> sprintf "{%s}"
