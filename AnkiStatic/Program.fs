namespace AnkiStatic.App

open Argu

module Program =
    let subcommands =
        [|
            "render",
            ("Render an Anki configuration JSON file into a .apkg file", ArgsCrate.make RenderArgs.OfParse Render.run)

            "output-schema",
            ("Output a schema you can use to verify the `render` config file",
             ArgsCrate.make (OutputSchemaArgs.OfParse >> Result.mapError List.singleton) OutputSchema.run)

            "verify",
            ("Verify a `render` configuration file",
             ArgsCrate.make (VerifyArgs.OfParse >> Result.mapError List.singleton) Verify.run)
        |]
        |> Map.ofArray

    [<EntryPoint>]
    let main argv =
        // It looks like Argu doesn't really support the combination of subcommands and read-from-env-vars, so we just
        // roll our own.

        match Array.tryHead argv with
        | None
        | Some "--help" ->
            subcommands.Keys
            |> String.concat ","
            |> eprintfn "Subcommands (try each with `--help`): %s"

            127

        | Some commandName ->

        match Map.tryFind commandName subcommands with
        | None ->
            subcommands.Keys
            |> String.concat ","
            |> eprintfn "Unrecognised command '%s'. Subcommands (try each with `--help`): %s" commandName

            127

        | Some (_help, command) ->

        let argv = Array.tail argv
        let config = ConfigurationReader.FromEnvironmentVariables ()

        { new ArgsEvaluator<_> with
            member _.Eval<'a, 'b when 'b :> IArgParserTemplate>
                (ofParseResult : ParseResults<'b> -> Result<'a, _>)
                run
                =
                let parser = ArgumentParser.Create<'b> ()

                let parsed =
                    try
                        parser.Parse (argv, config, raiseOnUsage = true) |> Some
                    with :? ArguParseException as e ->
                        e.Message.Replace ("AnkiStatic ", sprintf "AnkiStatic %s " commandName)
                        |> eprintfn "%s"

                        None

                match parsed with
                | None -> Error 127
                | Some parsed ->

                match ofParseResult parsed with
                | Error errors ->
                    for e in errors do
                        e.Message.Replace ("AnkiStatic ", sprintf "AnkiStatic %s " commandName)
                        |> eprintfn "%s"

                    Error 127
                | Ok args ->

                run args |> Ok
        }
        |> command.Apply
        |> Result.cata (fun t -> t.Result) id
