namespace AnkiStatic

open System

type SerialisedDeck =
    {
        Name : string
        ExtendedReviewLimit : int option
        ExtendedNewCardLimit : int option
        Collapsed : bool
        BrowserCollapsed : bool
        Description : string
    }

    static member ToDeck (deck : SerialisedDeck) : Deck =
        {
            Name = deck.Name
            ExtendedReviewLimit = deck.ExtendedReviewLimit
            ExtendedNewCardLimit = deck.ExtendedNewCardLimit
            UpdateSequenceNumber = -1
            Collapsed = deck.Collapsed
            BrowserCollapsed = deck.BrowserCollapsed
            Description = deck.Description
            LastModified = DateTimeOffset.FromUnixTimeSeconds 0
        }

type SerialisedModelField =
    {
        /// E.g. "Arial"
        Font : string
        Name : string
        /// Whether text should display right-to-left
        RightToLeft : bool
        FontSize : int
        Sticky : bool
    }

    static member ToModelField (counter : int) (field : SerialisedModelField) : ModelField =
        {
            Font = field.Font
            FontSize = field.FontSize
            Media = []
            Name = field.Name
            Ord = counter
            RightToLeft = field.RightToLeft
            Sticky = field.Sticky
        }

type SerialisedCardTemplate =
    {
        AnswerFormat : string
        BrowserAnswerFormat : string
        BrowserQuestionFormat : string
        Name : string
        QuestionFormat : string
    }

    static member ToCardTemplate<'Deck>
        (deck : 'Deck option)
        (counter : int)
        (template : SerialisedCardTemplate)
        : CardTemplate<'Deck>
        =
        {
            AnswerFormat = template.AnswerFormat
            BrowserAnswerFormat = template.BrowserAnswerFormat
            BrowserQuestionFormat = template.BrowserQuestionFormat
            Name = template.Name
            QuestionFormat = template.QuestionFormat
            DeckOverride = deck
            Ord = counter
        }

type SerialisedModel =
    {
        Css : string
        /// Any extra fields which are not the sort field
        AdditionalFields : SerialisedModelField list
        /// String which is added to terminate LaTeX expressions
        LatexPost : string
        LatexPre : string
        Name : string
        /// Which field the browser uses to sort by
        SortField : SerialisedModelField
        /// An invariant which is not maintained at construction:
        /// if the Type is Cloze, then this templates list must have length exactly 1.
        Templates : SerialisedCardTemplate list
        Type : ModelType
        DefaultDeck : SerialisedDeck
    }

    static member ToModel<'Deck> (s : SerialisedModel) (deck : 'Deck) : ModelConfiguration<'Deck> =
        match s.Type, s.Templates with
        | ModelType.Cloze, [] -> failwith $"A cloze model must have exactly one template, but got 0: %+A{s}"
        | ModelType.Cloze, _ :: _ :: _ ->
            failwith $"A cloze model must have exactly one template, but got at least 2: %+A{s}"
        | _, _ -> ()

        {
            Css = s.Css
            DefaultDeckId = deck
            Fields =
                (s.SortField :: s.AdditionalFields)
                |> List.mapi SerialisedModelField.ToModelField
            LatexPost = s.LatexPost
            LatexPre = s.LatexPre
            LastModification = DateTimeOffset.FromUnixTimeSeconds 0
            Name = s.Name
            SortField = 0
            Tags = []
            Templates = s.Templates |> List.mapi (SerialisedCardTemplate.ToCardTemplate None)
            Type = s.Type
            UpdateSequenceNumber = -1
            Version = []
        }

type SerialisedNewCardConfiguration =
    {
        Delays : int list
        InitialEase : int<ease>
        Intervals : IntervalConfiguration
        Order : NewCardOrder
        MaxNewPerDay : int
    }

    static member ToNewCardConfiguration (conf : SerialisedNewCardConfiguration) : NewCardConfiguration =
        {
            Bury = true
            Delays = conf.Delays
            InitialEase = conf.InitialEase
            Intervals = conf.Intervals
            Order = conf.Order
            MaxNewPerDay = conf.MaxNewPerDay
            Separate = true
        }

type SerialisedReviewConfiguration =
    {
        EasinessPerEasyReview : float
        Fuzz : float
        IntervalFactor : int
        MaxInterval : TimeSpan
        PerDay : int
    }

    static member ToReviewConfiguration (conf : SerialisedReviewConfiguration) : ReviewConfiguration =
        {
            Bury = true
            EasinessPerEasyReview = conf.EasinessPerEasyReview
            Fuzz = conf.Fuzz
            IntervalFactor = conf.IntervalFactor
            MaxInterval = conf.MaxInterval
            MinSpace = 1
            PerDay = conf.PerDay
        }


type SerialisedDeckConfiguration =
    {
        AutoPlay : bool
        Lapse : LapseConfiguration
        Name : string
        New : SerialisedNewCardConfiguration
        ReplayQuestionAudioWithAnswer : bool
        Review : SerialisedReviewConfiguration
        ShowTimer : bool
        MaxTimerTimeout : TimeSpan
    }

    static member ToDeckConfiguration (conf : SerialisedDeckConfiguration) : DeckConfiguration =
        {
            AutoPlay = conf.AutoPlay
            Lapse = conf.Lapse
            MaxTaken = conf.MaxTimerTimeout
            LastModified = DateTimeOffset.FromUnixTimeSeconds 0
            Name = conf.Name
            New = conf.New |> SerialisedNewCardConfiguration.ToNewCardConfiguration
            ReplayQuestionAudioWithAnswer = conf.ReplayQuestionAudioWithAnswer
            Review = conf.Review |> SerialisedReviewConfiguration.ToReviewConfiguration
            ShowTimer = conf.ShowTimer
            UpdateSequenceNumber = -1
        }

type SerialisedCollectionConfiguration =
    {
        NewSpread : NewCardDistribution
        CollapseTime : int
        TimeLimit : TimeSpan
        EstimateTimes : bool
        ShowDueCounts : bool
        SortBackwards : bool
    }

    static member ToCollectionConfiguration
        (currentDeck : 'Deck option)
        (activeDecks : 'Deck list)
        (currentModel : 'Model)
        (conf : SerialisedCollectionConfiguration)
        : CollectionConfiguration<'Model, 'Deck>
        =
        {
            CurrentDeck = currentDeck
            ActiveDecks = activeDecks
            NewSpread = conf.NewSpread
            CollapseTime = conf.CollapseTime
            TimeLimit = conf.TimeLimit
            EstimateTimes = conf.EstimateTimes
            ShowDueCounts = conf.ShowDueCounts
            CurrentModel = currentModel
            NextPosition =
                // TODO: get this to pick up the incrementing counter
                4
            SortType =
                // TODO: generalise this
                "noteFld"
            SortBackwards = conf.SortBackwards
            AddToCurrent = true
        }
