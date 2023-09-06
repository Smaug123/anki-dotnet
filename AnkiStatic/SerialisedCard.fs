namespace AnkiStatic

open System

type SerialisedNote =
    {
        CreationDate : DateTimeOffset
        Model : SerialisedModel
        Tags : string list
        ValueOfSortField : string
        /// These must be in the same order as the fields of the Model.
        /// TODO: type safety to get these to line up.
        ValuesOfAdditionalFields : string list
    }

    static member ToNote<'Model>
        (guid : uint64)
        (model : SerialisedModel -> 'Model)
        (note : SerialisedNote)
        : Note<'Model>
        =
        {
            Guid = guid
            ModelId = model note.Model
            LastModified = note.CreationDate
            UpdateSequenceNumber = -1
            Tags = note.Tags
            Fields = note.ValueOfSortField :: note.ValuesOfAdditionalFields
            SortField = Choice1Of2 note.ValueOfSortField
            Flags = 0
            Data = ""
        }

[<RequireQualifiedAccess>]
module SerialisedNote =
    let buildCards
        (cardCountSoFar : int)
        (deck : SerialisedDeck)
        (easeFactor : int<ease>)
        (interval : Interval)
        (note : SerialisedNote)
        : Card<SerialisedNote, SerialisedDeck> list
        =
        let primaryCard : Card<_, _> =
            {
                CreationDate = note.CreationDate
                NotesId = note
                DeckId = deck
                Interval = interval
                EaseFactor = easeFactor
                Ordinal = 0
                ModificationDate = note.CreationDate + TimeSpan.FromMilliseconds cardCountSoFar
                UpdateSequenceNumber = -1
                Type = CardType.New
                Queue = Queue.New
                Due = cardCountSoFar
                NumberOfReviews = 0
                NumberOfLapses = 0
                Left = 0
                Flags = 0
                Data = ""
                OriginalDue = 0
            }

        let otherCards =
            note.Model.AdditionalFields
            |> List.mapi (fun i _field ->
                {
                    CreationDate = note.CreationDate
                    NotesId = note
                    DeckId = deck
                    Interval = interval
                    EaseFactor = easeFactor
                    Ordinal = i + 1
                    ModificationDate = note.CreationDate + TimeSpan.FromMilliseconds (float (cardCountSoFar + i + 1))
                    UpdateSequenceNumber = -1
                    Type = CardType.New
                    Queue = Queue.New
                    Due = cardCountSoFar + i + 1
                    NumberOfReviews = 0
                    NumberOfLapses = 0
                    Left = 0
                    Flags = 0
                    Data = ""
                    OriginalDue = 0
                }
            )

        primaryCard :: otherCards

[<RequireQualifiedAccess>]
module Card =
    let translate<'note, 'deck>
        (noteLookup : SerialisedNote -> 'note)
        (deckLookup : SerialisedDeck -> 'deck)
        (serialised : Card<SerialisedNote, SerialisedDeck>)
        : Card<'note, 'deck>
        =
        {
            CreationDate = serialised.CreationDate
            NotesId = noteLookup serialised.NotesId
            DeckId = deckLookup serialised.DeckId
            Ordinal = serialised.Ordinal
            ModificationDate = serialised.ModificationDate
            UpdateSequenceNumber = serialised.UpdateSequenceNumber
            Type = serialised.Type
            Queue = serialised.Queue
            Due = serialised.Due
            Interval = serialised.Interval
            EaseFactor = serialised.EaseFactor
            NumberOfReviews = serialised.NumberOfReviews
            NumberOfLapses = serialised.NumberOfLapses
            Left = serialised.Left
            Flags = serialised.Flags
            Data = serialised.Data
            OriginalDue = serialised.OriginalDue
        }
