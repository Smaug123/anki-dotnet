namespace AnkiStatic

open System
open System.Text.Json

type CardTemplate<'Deck> =
    {
        AnswerFormat : string
        BrowserAnswerFormat : string
        BrowserQuestionFormat : string
        DeckOverride : 'Deck option
        Name : string
        Ord : int
        QuestionFormat : string
    }

[<RequireQualifiedAccess>]
module CardTemplate =
    let toJson (this : CardTemplate<DateTimeOffset>) : string =
        let did =
            match this.DeckOverride with
            | None -> "null"
            | Some did -> sprintf "%i" (did.ToUnixTimeMilliseconds ())

        $"""{{
    "afmt": %s{JsonSerializer.Serialize this.AnswerFormat},
    "name": %s{JsonSerializer.Serialize this.Name},
    "qfmt": %s{JsonSerializer.Serialize this.QuestionFormat},
    "did": %s{did},
    "ord": %i{this.Ord},
    "bafmt": %s{JsonSerializer.Serialize this.BrowserAnswerFormat},
    "bqfmt": %s{JsonSerializer.Serialize this.BrowserAnswerFormat}
}}"""

type ModelField =
    {
        /// E.g. "Arial"
        Font : string
        /// Docs suggest this is unused
        Media : string list
        Name : string
        /// For some reason a ModelField is intended to be stored in an
        /// array, but *also* tagged with its index in that array :shrug:
        Ord : int
        /// Whether text should display right-to-left
        RightToLeft : bool
        FontSize : int
        Sticky : bool
    }

    static member toJson (this : ModelField) : string =
        let media =
            this.Media
            |> Seq.map JsonSerializer.Serialize
            |> String.concat ","
            |> sprintf "[%s]"

        $"""{{
    "size": %i{this.FontSize},
    "name": %s{JsonSerializer.Serialize this.Name},
    "media": %s{media},
    "rtl": %b{this.RightToLeft},
    "ord": %i{this.Ord},
    "font": %s{JsonSerializer.Serialize this.Font},
    "sticky": %b{this.Sticky}
}}"""


type ModelType =
    | Standard
    | Cloze

    member this.ToInteger () =
        match this with
        | ModelType.Standard -> 0
        | ModelType.Cloze -> 1

type ModelConfiguration<'Deck> =
    {
        Css : string
        DeckId : 'Deck
        Fields : ModelField list
        /// String which is added to terminate LaTeX expressions
        LatexPost : string
        LatexPre : string
        LastModification : DateTimeOffset
        Name : string
        // I've omitted `req` which is unused in modern clients
        /// Which field the browser uses to sort by
        SortField : int
        /// Unused, should always be empty
        Tags : string list
        Templates : CardTemplate<'Deck> list
        Type : ModelType
        UpdateSequenceNumber : int
        /// Unused, should always be empty
        Version : string list
    }

[<RequireQualifiedAccess>]
module ModelConfiguration =
    let toJson (id : int64) (this : ModelConfiguration<DateTimeOffset>) : string =
        let vers =
            this.Version
            |> Seq.map JsonSerializer.Serialize
            |> String.concat ","
            |> sprintf "[%s]"

        let tags =
            this.Tags
            |> Seq.map JsonSerializer.Serialize
            |> String.concat ","
            |> sprintf "[%s]"

        let flds =
            this.Fields |> Seq.map ModelField.toJson |> String.concat "," |> sprintf "[%s]"

        let tmpls =
            this.Templates
            |> Seq.map CardTemplate.toJson
            |> String.concat ","
            |> sprintf "[%s]"

        $"""{{
    "vers": %s{vers},
    "name": %s{JsonSerializer.Serialize this.Name},
    "tags": %s{tags},
    "did": %i{this.DeckId.ToUnixTimeMilliseconds ()},
    "usn": %i{this.UpdateSequenceNumber},
    "flds": %s{flds},
    "sortf": %i{this.SortField},
    "tmpls": %s{tmpls},
    "latexPre": %s{JsonSerializer.Serialize this.LatexPre},
    "latexPost": %s{JsonSerializer.Serialize this.LatexPost},
    "type": %i{this.Type.ToInteger ()},
    "id": %i{id},
    "css": %s{JsonSerializer.Serialize this.Css},
    "mod": %i{this.LastModification.ToUnixTimeSeconds ()}
}}"""

/// Identifies a type of note (e.g. "Cloze").
[<Measure>]
type model
