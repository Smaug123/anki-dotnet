namespace AnkiStatic

open System
open System.Collections.Generic

type SerialisedCollection =
    {
        CreationDate : DateTimeOffset
        Configuration : SerialisedCollectionConfiguration
        DefaultModel : DateTimeOffset * SerialisedModel
        NonDefaultModels : Map<DateTimeOffset, SerialisedModel>
        DefaultDeck : SerialisedDeck
        NonDefaultDecks : Map<DateTimeOffset, SerialisedDeck>
        DefaultDeckConfiguration : SerialisedDeckConfiguration
        NonDefaultDeckConfiguration : Map<DateTimeOffset, SerialisedDeckConfiguration>
        Tags : string
    }

type CollectionForSql =
    {
        Decks : Map<DateTimeOffset, Deck>
        DecksInverse : SerialisedDeck -> DateTimeOffset
        Models : Map<DateTimeOffset, ModelConfiguration<DateTimeOffset>>
        ModelsInverse : SerialisedModel -> DateTimeOffset
        Collection : Collection<DateTimeOffset, DateTimeOffset>
    }

[<RequireQualifiedAccess>]
module SerialisedCollection =

    let toSqlite (collection : SerialisedCollection) : CollectionForSql =
        let decks, deckLookup =
            let dict = Dictionary ()

            let decks =
                collection.NonDefaultDecks
                |> Map.add (DateTimeOffset.FromUnixTimeMilliseconds 1) collection.DefaultDeck
                |> Map.map (fun keyTimestamp deck ->
                    let converted = SerialisedDeck.ToDeck deck
                    dict.Add (deck, (keyTimestamp, converted))
                    converted
                )

            let deckLookup (d : SerialisedDeck) : DateTimeOffset * Deck =
                // This could look up on reference equality rather than structural equality, for speed
                match dict.TryGetValue d with
                | true, v -> v
                | false, _ ->
                    failwith
                        $"A model declared that it was attached to a deck, but that deck was not declared in the deck list: %+A{d}"

            decks, deckLookup

        let models, modelLookup, _correctedCurrentDate =
            let dict = Dictionary ()

            let defaultModelDate =
                let mutable currentDate =
                    fst collection.DefaultModel + TimeSpan.FromMilliseconds 1.0

                while collection.NonDefaultModels.ContainsKey currentDate do
                    currentDate <- currentDate + TimeSpan.FromMilliseconds 1.0

                currentDate

            let models =
                collection.NonDefaultModels
                |> Map.add defaultModelDate (snd collection.DefaultModel)
                |> Map.map (fun modelTimestamp v ->
                    let defaultDeckTimestamp, _deck = deckLookup v.DefaultDeck
                    dict.Add (v, modelTimestamp)
                    SerialisedModel.ToModel v defaultDeckTimestamp
                )

            let modelLookup (m : SerialisedModel) : DateTimeOffset =
                match dict.TryGetValue m with
                | true, v -> v
                | false, _ ->
                    failwith
                        $"A note declared that it satisfied a model, but that model was not declared in the model list:\n\nDesired: %+A{m}\n\nAvailable: %+A{dict}"

            models, modelLookup, defaultModelDate

        let defaultDeck, _ = deckLookup collection.DefaultDeck

        let deckConfigurations =
            collection.NonDefaultDeckConfiguration
            |> Map.add (DateTimeOffset.FromUnixTimeMilliseconds 1) collection.DefaultDeckConfiguration
            |> Map.map (fun _ -> SerialisedDeckConfiguration.ToDeckConfiguration)

        {
            Decks = decks
            DecksInverse = fun deck -> fst (deckLookup deck)
            Models = models
            ModelsInverse = modelLookup
            Collection =
                {
                    CreationDate = collection.CreationDate
                    LastModified = collection.CreationDate
                    LastSchemaModification = collection.CreationDate
                    Version = 11
                    Dirty = 0
                    UpdateSequenceNumber = -1
                    LastSync = DateTimeOffset.FromUnixTimeSeconds 0
                    Configuration =
                        collection.Configuration
                        |> SerialisedCollectionConfiguration.ToCollectionConfiguration
                            (Some defaultDeck)
                            // TODO: work out what it means for a deck to be a "descendant" of another
                            [ defaultDeck ]
                            (fst collection.DefaultModel)
                    Models = models
                    Decks = decks
                    DeckConfigurations = deckConfigurations
                    Tags = collection.Tags
                }
        }
