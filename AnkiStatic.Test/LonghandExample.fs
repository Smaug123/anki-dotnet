namespace AnkiStatic.Test

open System
open System.IO
open AnkiStatic
open NUnit.Framework

[<TestFixture>]
module Example =

    let incrementArr (arr : byte[]) =
        let rec go (pos : int) =
            let v = arr.[pos]

            if v < 255uy then
                arr.[pos] <- v + 1uy
            else
                arr.[pos] <- 0uy

                if pos = 0 then
                    failwith "could not increment max guid"

                go (pos - 1)

        go (arr.Length - 1)

    [<Test>]
    let example () =
        let frontField : SerialisedModelField =
            {
                Font = "Arial"
                Name = "Front"
                RightToLeft = false
                FontSize = 20
                Sticky = false
            }

        let backField : SerialisedModelField =
            {
                Font = "Arial"
                Name = "Back"
                RightToLeft = false
                FontSize = 20
                Sticky = false
            }

        let frontTemplate : SerialisedCardTemplate =
            {
                AnswerFormat = "{{FrontSide}}\n\n<hr id=answer>\n\n{{Back}}"
                BrowserAnswerFormat = ""
                BrowserQuestionFormat = ""
                Name = "Card 1"
                QuestionFormat = "{{Front}}"
            }

        let backTemplate : SerialisedCardTemplate =
            {
                AnswerFormat = "{{FrontSide}}\n\n<hr id=answer>\n\n{{Front}}"
                BrowserAnswerFormat = ""
                BrowserQuestionFormat = ""
                Name = "Card 2"
                QuestionFormat = "{{Back}}"
            }

        let deck =
            {
                Name = "Analysis"
                ExtendedReviewLimit = Some 50
                ExtendedNewCardLimit = Some 10
                Collapsed = false
                BrowserCollapsed = false
                Description = ""
            }

        let basicAndReverseModel : SerialisedModel =
            {
                Css =
                    ".card {\n font-family: arial;\n font-size: 20px;\n text-align: center;\n color: black;\n background-color: white;\n}\n"
                AdditionalFields = [ backField ]
                LatexPost = "\end{document}"
                LatexPre =
                    "\\documentclass[12pt]{article}\n\\special{papersize=3in,5in}\n\\usepackage[utf8]{inputenc}\n\\usepackage{amssymb,amsmath}\n\\pagestyle{empty}\n\\setlength{\\parindent}{0in}\n\\begin{document}\n"
                Name = "Basic (and reversed card)"
                SortField = frontField
                Templates = [ frontTemplate ; backTemplate ]
                Type = ModelType.Standard
                DefaultDeck = deck
            }

        let textField : SerialisedModelField =
            {
                Font = "Arial"
                Name = "Text"
                RightToLeft = false
                FontSize = 20
                Sticky = false
            }

        let extraField : SerialisedModelField =
            {
                Font = "Arial"
                Name = "Extra"
                RightToLeft = false
                FontSize = 20
                Sticky = false
            }

        let clozeTemplate : SerialisedCardTemplate =
            {
                AnswerFormat = "{{cloze:Text}}<br>\n{{Extra}}"
                BrowserAnswerFormat = ""
                BrowserQuestionFormat = ""
                Name = "Cloze"
                QuestionFormat = "{{cloze:Text}}"
            }

        let clozeModel : SerialisedModel =
            {
                Css =
                    ".card {\n font-family: arial;\n font-size: 20px;\n text-align: center;\n color: black;\n background-color: white;\n}\n\n.cloze {\n font-weight: bold;\n color: blue;\n}"
                AdditionalFields = [ extraField ]
                LatexPost = "\end{document}"
                LatexPre =
                    "\\documentclass[12pt]{article}\n\\special{papersize=3in,5in}\n\\usepackage[utf8]{inputenc}\n\\usepackage{amssymb,amsmath}\n\\pagestyle{empty}\n\\setlength{\\parindent}{0in}\n\\begin{document}\n"
                Name = "Cloze"
                SortField = textField
                Templates = [ clozeTemplate ]
                Type = ModelType.Cloze
                DefaultDeck = deck
            }

        let example : SerialisedCollection =
            {
                CreationDate = DateTimeOffset (2023, 09, 06, 17, 03, 00, TimeSpan.FromHours 1.0)
                Configuration =
                    {
                        NewSpread = NewCardDistribution.Distribute
                        CollapseTime = 1200
                        TimeLimit = TimeSpan.Zero
                        EstimateTimes = true
                        ShowDueCounts = true
                        SortBackwards = false
                    }
                DefaultDeck = deck
                NonDefaultDecks = Map.empty
                DefaultDeckConfiguration =
                    {
                        AutoPlay = true
                        Lapse =
                            {
                                Delays = [ 10 ]
                                LeechAction = LeechAction.Suspend
                                LeechFails = 8
                                MinInterval = 1
                                Multiplier = 0
                            }
                        Name = "Default"
                        New =
                            {
                                Delays = [ 1 ; 10 ]
                                InitialEase = 2500<ease>
                                Intervals =
                                    {
                                        Good = 1
                                        Easy = 4
                                        Unused = 7
                                    }
                                Order = NewCardOrder.Random
                                MaxNewPerDay = 20
                            }
                        ReplayQuestionAudioWithAnswer = true
                        Review =
                            {
                                EasinessPerEasyReview = 1.3
                                Fuzz = 0.05
                                IntervalFactor = 1
                                MaxInterval = TimeSpan.FromDays 36500.0
                                PerDay = 100
                            }
                        ShowTimer = false
                        MaxTimerTimeout = TimeSpan.FromSeconds 60.0
                    }
                NonDefaultDeckConfiguration = Map.empty
                Tags = "{}"
                DefaultModel = DateTimeOffset.FromUnixTimeMilliseconds 1373473028445L, basicAndReverseModel
                NonDefaultModels =
                    [ DateTimeOffset.FromUnixTimeMilliseconds 1373473028440L, clozeModel ]
                    |> Map.ofList
            }

        let collection = SerialisedCollection.toSqlite example

        let notes : SerialisedNote list =
            [
                {
                    Model = basicAndReverseModel
                    Tags = []
                    ValueOfSortField = "Definition of the logistic function"
                    ValuesOfAdditionalFields = [ @"\(g(z) = \frac{1}{1+e^{-z}}\)" ]
                    CreationDate = DateTimeOffset (2023, 09, 06, 19, 30, 00, TimeSpan.FromHours 1.0)
                    Deck = deck
                }
                {
                    Model = clozeModel
                    Tags = []
                    ValueOfSortField =
                        "The four perspectives of Ithkuil are {{c1::monadic}}, {{c2::unbounded}}, {{c3::nomic}}, {{c4::abstract}}."
                    ValuesOfAdditionalFields = [ "" ]
                    CreationDate = DateTimeOffset (2023, 09, 06, 19, 30, 00, TimeSpan.FromHours 1.0)
                    Deck = deck
                }
            ]

        let outputFile =
            Path.GetTempFileName ()
            |> fun f -> Path.ChangeExtension (f, ".apkg")
            |> FileInfo

        Sqlite.writeAll (Random 1) collection notes outputFile |> fun t -> t.Result

        Console.WriteLine $"Written: %s{outputFile.FullName}"
