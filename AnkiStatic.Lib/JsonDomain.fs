namespace AnkiStatic

open System
open System.Collections.Generic
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Tasks

type private LeechActionJsonConverter () =
    inherit JsonConverter<LeechAction> ()

    override this.Read (reader, _, options) =
        match reader.GetString().ToLowerInvariant () with
        | "suspend" -> LeechAction.Suspend
        | "mark" -> LeechAction.Mark
        | s -> raise (JsonException $"could not deserialise: %s{s}")

    override this.Write (writer, value, options) =
        match value with
        | LeechAction.Mark -> "mark"
        | LeechAction.Suspend -> "suspend"
        |> writer.WriteStringValue

type private NewCardOrderJsonConverter () =
    inherit JsonConverter<NewCardOrder> ()

    override this.Read (reader, _, options) =
        match reader.GetString().ToLowerInvariant () with
        | "random" -> NewCardOrder.Random
        | "due" -> NewCardOrder.Due
        | s -> raise (JsonException $"could not deserialise: %s{s}")

    override this.Write (writer, value, options) =
        match value with
        | NewCardOrder.Random -> "random"
        | NewCardOrder.Due -> "due"
        |> writer.WriteStringValue

type private ModelTypeJsonConverter () =
    inherit JsonConverter<ModelType> ()

    override this.Read (reader, _, options) =
        match reader.GetString().ToLowerInvariant () with
        | "standard" -> ModelType.Standard
        | "cloze" -> ModelType.Cloze
        | s -> raise (JsonException $"could not deserialise: %s{s}")

    override this.Write (writer, value, options) =
        match value with
        | ModelType.Standard -> "standard"
        | ModelType.Cloze -> "cloze"
        |> writer.WriteStringValue

type private NewCardDistributionJsonConverter () =
    inherit JsonConverter<NewCardDistribution> ()

    override this.Read (reader, _, options) =
        match reader.GetString().ToLowerInvariant () with
        | "distribute" -> NewCardDistribution.Distribute
        | "first" -> NewCardDistribution.First
        | "last" -> NewCardDistribution.Last
        | s -> raise (JsonException $"could not deserialise: %s{s}")

    override this.Write (writer, value, options) =
        match value with
        | NewCardDistribution.Distribute -> "distribute"
        | NewCardDistribution.First -> "first"
        | NewCardDistribution.Last -> "last"
        |> writer.WriteStringValue

[<RequireQualifiedAccess>]
module JsonCollection =
    type JsonTemplate =
        {
            AnswerFormat : string
            QuestionFormat : string
            Name : string
            BrowserAnswerFormat : string option
            BrowserQuestionFormat : string option
        }

        static member ToInternal (this : JsonTemplate) : SerialisedCardTemplate =
            {
                AnswerFormat = this.AnswerFormat
                QuestionFormat = this.QuestionFormat
                Name = this.Name
                BrowserAnswerFormat = this.BrowserAnswerFormat |> Option.defaultValue ""
                BrowserQuestionFormat = this.BrowserQuestionFormat |> Option.defaultValue ""
            }

    type JsonMetadata =
        {
            // [<JsonRequired>]
            CreationDate : DateTimeOffset
            // [<JsonRequired>]
            DefaultDeck : string
            // [<JsonRequired>]
            DefaultDeckConfiguration : SerialisedDeckConfiguration
            /// Map into the string deck names
            NonDefaultDecks : IReadOnlyDictionary<DateTimeOffset, string>
            NonDefaultDeckConfigurations : IReadOnlyDictionary<DateTimeOffset, SerialisedDeckConfiguration>
            Tags : string
            // [<JsonRequired>]
            DefaultModelName : string
            SortBackwards : bool option
            ShowDueCounts : bool option
            NewSpread : NewCardDistribution
            EstimateTimes : bool option
            TimeLimitSeconds : int option
            CollapseTimeSeconds : int option
        }

    type JsonNote =
        {
            Tags : string list option
            // [<JsonRequired>]
            SortFieldValue : string
            // [<JsonRequired>]
            AdditionalFieldValues : string list
            CreationDate : DateTimeOffset option
            // [<JsonRequired>]
            Model : string
        }

        static member internal ToInternal
            (deck : SerialisedDeck)
            (models : Map<string, SerialisedModel>)
            (this : JsonNote)
            : SerialisedNote
            =
            {
                Deck = deck
                CreationDate =
                    this.CreationDate
                    |> Option.defaultValue (DateTimeOffset.UnixEpoch + TimeSpan.FromSeconds 1.0)
                Model = models.[this.Model]
                Tags = this.Tags |> Option.defaultValue []
                ValueOfSortField = this.SortFieldValue
                ValuesOfAdditionalFields = this.AdditionalFieldValues
            }

    type JsonDeck =
        {
            ExtendedReviewLimit : int option
            ExtendedNewCardLimit : int option
            Collapsed : bool option
            BrowserCollapsed : bool option
            // [<JsonRequired>]
            Description : string
            // [<JsonRequired>]
            Notes : JsonNote list
        }

        static member internal ToInternal (name : string) (deck : JsonDeck) : SerialisedDeck =
            {
                Name = name
                ExtendedReviewLimit = deck.ExtendedReviewLimit
                ExtendedNewCardLimit = deck.ExtendedNewCardLimit
                Collapsed = deck.Collapsed |> Option.defaultValue false
                BrowserCollapsed = deck.BrowserCollapsed |> Option.defaultValue false
                Description = deck.Description
            }

    type JsonField =
        {
            // [<JsonRequired>]
            DisplayName : string
            Font : string option
            RightToLeft : bool option
            FontSize : int option
            Sticky : bool option
        }

        static member internal ToInternal (field : JsonField) : SerialisedModelField =
            {
                Font = field.Font |> Option.defaultValue "Arial"
                Name = field.DisplayName
                RightToLeft = field.RightToLeft |> Option.defaultValue false
                FontSize = field.FontSize |> Option.defaultValue 20
                Sticky = field.Sticky |> Option.defaultValue false
            }

    type JsonModel =
        {
            Css : string option
            /// Name of a field
            // [<JsonRequired>]
            SortField : string
            // [<JsonRequired>]
            AdditionalFields : string list
            LatexPost : string option
            LatexPre : string option
            // TODO: is this required?
            // [<JsonRequired>]
            Name : string
            // [<JsonRequired>]
            Templates : string list
            // [<JsonRequired>]
            Type : ModelType
            DefaultDeck : string option
            ModificationTime : DateTimeOffset
        }

        static member internal ToInternal
            (defaultDeck : SerialisedDeck)
            (standardTemplates : IReadOnlyDictionary<string, SerialisedCardTemplate>)
            (clozeTemplates : IReadOnlyDictionary<string, SerialisedCardTemplate>)
            (decks : Map<string, SerialisedDeck>)
            (fields : Map<string, SerialisedModelField>)
            (this : JsonModel)
            : SerialisedModel
            =
            {
                Css =
                    this.Css
                    |> Option.defaultValue (
                        JsonSerializer.Serialize
                            ".card {\n font-family: arial;\n font-size: 20px;\n text-align: center;\n color: black;\n background-color: white;\n}\n"
                    )
                AdditionalFields = this.AdditionalFields |> List.map (fun field -> Map.find field fields)
                LatexPost =
                    this.LatexPost
                    |> Option.defaultValue (JsonSerializer.Serialize @"\end{document}")
                LatexPre =
                    this.LatexPre
                    |> Option.defaultValue (
                        JsonSerializer.Serialize
                            "\\documentclass[12pt]{article}\n\\special{papersize=3in,5in}\n\\usepackage[utf8]{inputenc}\n\\usepackage{amssymb,amsmath}\n\\pagestyle{empty}\n\\setlength{\\parindent}{0in}\n\\begin{document}\n"
                    )
                Name = this.Name
                SortField = fields.[this.SortField]
                Templates =
                    match this.Type with
                    | ModelType.Cloze -> this.Templates |> List.map (fun t -> clozeTemplates.[t])
                    | ModelType.Standard -> this.Templates |> List.map (fun t -> standardTemplates.[t])
                Type = this.Type
                DefaultDeck =
                    match this.DefaultDeck with
                    | None -> defaultDeck
                    | Some deck -> decks.[deck]
            }

    type JsonCollection =
        {
            // [<JsonRequired>]
            Metadata : JsonMetadata
            // [<JsonRequired>]
            StandardTemplates : IReadOnlyDictionary<string, JsonTemplate>
            // [<JsonRequired>]
            ClozeTemplates : IReadOnlyDictionary<string, JsonTemplate>
            /// Map of name to deck
            // [<JsonRequired>]
            Decks : IReadOnlyDictionary<string, JsonDeck>
            // [<JsonRequired>]
            Fields : IReadOnlyDictionary<string, JsonField>
            // [<JsonRequired>]
            Models : IReadOnlyDictionary<string, JsonModel>
        }

    let private options =
        let opts = JsonSerializerOptions ()
        opts.Converters.Add (LeechActionJsonConverter ())
        opts.Converters.Add (NewCardDistributionJsonConverter ())
        opts.Converters.Add (NewCardOrderJsonConverter ())
        opts.Converters.Add (ModelTypeJsonConverter ())
        opts.PropertyNameCaseInsensitive <- true
        opts

    let internal deserialiseString (s : string) : JsonCollection = JsonSerializer.Deserialize (s, options)

    let deserialise (utf8Json : Stream) : ValueTask<JsonCollection> =
        JsonSerializer.DeserializeAsync (utf8Json, options)

    let toInternal (collection : JsonCollection) : SerialisedCollection * SerialisedNote list =
        let decks =
            collection.Decks
            |> Seq.map (fun (KeyValue (deckName, deck)) -> deckName, JsonDeck.ToInternal deckName deck)
            |> Map.ofSeq

        let fields =
            collection.Fields
            |> Seq.map (fun (KeyValue (fieldName, field)) -> fieldName, JsonField.ToInternal field)
            |> Map.ofSeq

        let standardTemplates =
            collection.StandardTemplates
            |> Seq.map (fun (KeyValue (key, template)) -> key, JsonTemplate.ToInternal template)
            |> Map.ofSeq

        let clozeTemplates =
            collection.ClozeTemplates
            |> Seq.map (fun (KeyValue (key, template)) -> key, JsonTemplate.ToInternal template)
            |> Map.ofSeq

        let defaultDeck = decks.[collection.Metadata.DefaultDeck]

        let models : Map<string, SerialisedModel> =
            collection.Models
            |> Seq.map (fun (KeyValue (modelName, model)) ->
                let model =
                    JsonModel.ToInternal defaultDeck standardTemplates clozeTemplates decks fields model

                modelName, model
            )
            |> Map.ofSeq

        let notes =
            collection.Decks
            |> Seq.map (fun (KeyValue (deckName, deck)) ->
                deck.Notes |> Seq.map (JsonNote.ToInternal decks.[deckName] models)
            )
            |> Seq.concat
            |> List.ofSeq

        let collection =
            {
                CreationDate = collection.Metadata.CreationDate
                Configuration =
                    {
                        NewSpread = collection.Metadata.NewSpread
                        CollapseTime = collection.Metadata.CollapseTimeSeconds |> Option.defaultValue 1200
                        TimeLimit =
                            collection.Metadata.TimeLimitSeconds
                            |> Option.defaultValue 0
                            |> float<int>
                            |> TimeSpan.FromSeconds
                        EstimateTimes = collection.Metadata.EstimateTimes |> Option.defaultValue false
                        ShowDueCounts = collection.Metadata.ShowDueCounts |> Option.defaultValue true
                        SortBackwards = collection.Metadata.SortBackwards |> Option.defaultValue false
                    }
                DefaultModel =
                    let model = models.[collection.Metadata.DefaultModelName]
                    collection.Models.[collection.Metadata.DefaultModelName].ModificationTime, model
                NonDefaultModels =
                    collection.Models
                    |> Seq.choose (fun (KeyValue (modelKey, _model)) ->
                        if modelKey <> collection.Metadata.DefaultModelName then
                            let time = collection.Models.[modelKey].ModificationTime
                            Some (time, models.[modelKey])
                        else
                            None
                    )
                    |> Map.ofSeq
                DefaultDeck = defaultDeck
                NonDefaultDecks =
                    collection.Metadata.NonDefaultDecks
                    |> Seq.map (fun (KeyValue (time, deck)) -> time, decks.[deck])
                    |> Map.ofSeq
                DefaultDeckConfiguration = collection.Metadata.DefaultDeckConfiguration
                NonDefaultDeckConfiguration =
                    collection.Metadata.NonDefaultDeckConfigurations
                    |> Seq.map (fun kvp -> kvp.Key, kvp.Value)
                    |> Map.ofSeq
                Tags = collection.Metadata.Tags
            }

        collection, notes
