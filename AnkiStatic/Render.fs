namespace AnkiStatic.App

open System
open System.IO
open System.Threading.Tasks
open Argu
open AnkiStatic

type RenderArgsFragment =
    | [<MainCommand>] Input of string
    | Output of string

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | RenderArgsFragment.Input _ ->
                "path to the JSON file to be rendered as an Anki deck, or the literal '-' to read from stdin"
            | RenderArgsFragment.Output _ -> "Output file path"

type InputSource =
    | File of FileInfo
    | Stdin

type RenderArgs =
    {
        Input : InputSource
        Output : FileInfo
    }

    static member OfParse (parsed : ParseResults<RenderArgsFragment>) : Result<RenderArgs, ArguParseException list> =
        let input =
            try
                parsed.GetResult RenderArgsFragment.Input |> Ok
            with :? ArguParseException as e ->
                Error e

        let output =
            try
                parsed.GetResult RenderArgsFragment.Output |> Ok
            with :? ArguParseException as e ->
                Error e

        match input, output with
        | Error e, Ok _
        | Ok _, Error e -> Error [ e ]
        | Error e1, Error e2 -> Error [ e1 ; e2 ]
        | Ok input, Ok output ->

        let input =
            if input = "-" then
                InputSource.Stdin
            else
                InputSource.File (FileInfo input)

        let output = FileInfo output

        {
            Input = input
            Output = output
        }
        |> Ok

module Render =
    let run (args : RenderArgs) : Task<int> =
        task {
            let rng = Random ()

            use s =
                match args.Input with
                | InputSource.Stdin -> Console.OpenStandardInput ()
                | InputSource.File f -> f.OpenRead () :> Stream

            let! json = JsonCollection.deserialise s
            let collection, notes = json |> JsonCollection.toInternal

            let outputFile =
                Path.GetTempFileName ()
                |> fun f -> Path.ChangeExtension (f, ".apkg")
                |> FileInfo

            let collection = SerialisedCollection.toSqlite collection

            do! Sqlite.writeAll rng collection notes outputFile

            outputFile.MoveTo args.Output.FullName

            return 0
        }
