namespace AnkiStatic.App

open System.Threading.Tasks
open Argu

type ArgsEvaluator<'ret> =
    abstract Eval<'a, 'b when 'b :> IArgParserTemplate> :
        (ParseResults<'b> -> Result<'a, ArguParseException list>) -> ('a -> Task<int>) -> 'ret

type ArgsCrate =
    abstract Apply<'ret> : ArgsEvaluator<'ret> -> 'ret

[<RequireQualifiedAccess>]
module ArgsCrate =
    let make<'a, 'b when 'b :> IArgParserTemplate>
        (ofResult : ParseResults<'b> -> Result<'a, ArguParseException list>)
        (run : 'a -> Task<int>)
        =
        { new ArgsCrate with
            member _.Apply e = e.Eval ofResult run
        }
