namespace AnkiStatic

open System
open System.Collections.Generic
open System.IO
open System.IO.Compression
open Microsoft.Data.Sqlite
open System.Threading.Tasks

type EmptyPackage = private | EmptyPackage of FileInfo

type Package =
    private
    | Package of FileInfo

    member this.GetFileInfo () =
        match this with
        | Package p -> p

[<RequireQualifiedAccess>]
module Sqlite =
    let private executeCreateStatement (conn : SqliteConnection) (statement : string) =
        task {
            use cmd = conn.CreateCommand ()
            cmd.CommandText <- statement
            let! result = cmd.ExecuteNonQueryAsync ()

            if result <> 0 then
                return failwith "unexpectedly created a row in cards creation"
        }

    let createEmptyPackage (path : FileInfo) : Task<EmptyPackage> =
        if path.FullName.Contains ';' then
            failwith "Path contained connection string metacharacter ';', so aborting."

        task {
            // Connect to the SQLite database; create if not exists
            let connectionString = $"Data Source=%s{path.FullName};"
            use connection = new SqliteConnection (connectionString)
            connection.Open ()

            do!
                executeCreateStatement
                    connection
                    """
CREATE TABLE cards (
    id              integer primary key,
    nid             integer not null,
    did             integer not null,
    ord             integer not null,
    mod             integer not null,
    usn             integer not null,
    type            integer not null,
    queue           integer not null,
    due             integer not null,
    ivl             integer not null,
    factor          integer not null,
    reps            integer not null,
    lapses          integer not null,
    left            integer not null,
    odue            integer not null,
    odid            integer not null,
    flags           integer not null,
    data            text not null
)"""

            do!
                executeCreateStatement
                    connection
                    """
CREATE TABLE col (
    id              integer primary key,
    crt             integer not null,
    mod             integer not null,
    scm             integer not null,
    ver             integer not null,
    dty             integer not null,
    usn             integer not null,
    ls              integer not null,
    conf            text not null,
    models          text not null,
    decks           text not null,
    dconf           text not null,
    tags            text not null
)"""

            do!
                executeCreateStatement
                    connection
                    """
CREATE TABLE graves (
    usn             integer not null,
    oid             integer not null,
    type            integer not null
)"""

            do!
                executeCreateStatement
                    connection
                    """
CREATE TABLE notes (
    id              integer primary key,
    guid            text not null,
    mid             integer not null,
    mod             integer not null,
    usn             integer not null,
    tags            text not null,
    flds            text not null,
    sfld            integer not null,
    csum            integer not null,
    flags           integer not null,
    data            text not null
)"""

            do!
                executeCreateStatement
                    connection
                    """
CREATE TABLE revlog (
    id              integer primary key,
    cid             integer not null,
    usn             integer not null,
    ease            integer not null,
    ivl             integer not null,
    lastIvl         integer not null,
    factor          integer not null,
    time            integer not null,
    type            integer not null
)"""

            do!
                executeCreateStatement
                    connection
                    """
CREATE INDEX ix_cards_nid on cards (nid);
CREATE INDEX ix_cards_sched on cards (did, queue, due);
CREATE INDEX ix_cards_usn on cards (usn);
CREATE INDEX ix_notes_csum on notes (csum);
CREATE INDEX ix_notes_usn on notes (usn);
CREATE INDEX ix_revlog_cid on revlog (cid);
CREATE INDEX ix_revlog_usn on revlog (usn)
"""

            return EmptyPackage path
        }

    let createDecks (EmptyPackage sqliteDb) (collection : Collection<DateTimeOffset, DateTimeOffset>) : Task<Package> =
        task {
            let connectionString = $"Data Source=%s{sqliteDb.FullName};"
            use connection = new SqliteConnection (connectionString)
            connection.Open ()

            let cmd = connection.CreateCommand ()
            cmd.Connection <- connection

            cmd.CommandText <-
                """
INSERT INTO col
(crt, mod, scm, ver, dty, usn, ls, conf, models, decks, dconf, tags)
VALUES ($crt, $mod, $scm, $ver, $dty, $usn, $ls, $conf, $models, $decks, $dconf, $tags)
"""

            cmd.Parameters.AddWithValue ("crt", collection.CreationDate.ToUnixTimeSeconds ())
            |> ignore

            cmd.Parameters.AddWithValue ("mod", collection.LastModified.ToUnixTimeSeconds ())
            |> ignore

            cmd.Parameters.AddWithValue ("scm", collection.LastSchemaModification.ToUnixTimeSeconds ())
            |> ignore

            cmd.Parameters.AddWithValue ("ver", collection.Version) |> ignore
            cmd.Parameters.AddWithValue ("dty", collection.Dirty) |> ignore
            cmd.Parameters.AddWithValue ("usn", collection.UpdateSequenceNumber) |> ignore

            cmd.Parameters.AddWithValue ("ls", collection.LastSync.ToUnixTimeSeconds ())
            |> ignore

            cmd.Parameters.AddWithValue ("conf", collection.Configuration |> CollectionConfiguration.toJsonString)
            |> ignore

            cmd.Parameters.AddWithValue ("models", Collection.getJsonModelString collection)
            |> ignore

            cmd.Parameters.AddWithValue ("decks", Collection.getJsonDeckString collection)
            |> ignore

            cmd.Parameters.AddWithValue ("dconf", Collection.getDeckConfigurationString collection)
            |> ignore

            cmd.Parameters.AddWithValue ("tags", collection.Tags) |> ignore

            let! rows = cmd.ExecuteNonQueryAsync ()

            if rows <> 1 then
                return failwith $"Failed to insert collection row (got: %i{rows})"
            else

            return Package sqliteDb
        }

    /// Returns the note ID for each input note, in order.
    let createNotes (Package sqliteDb) (notes : Note<DateTimeOffset> list) : int64 IReadOnlyList Task =
        // The Anki model is *absolutely* insane and uses time of creation of a note
        // as a unique key.
        // Work around this madness.

        let notes =
            let seenSoFar = HashSet ()
            let mutable maxSoFar = DateTimeOffset.MinValue

            notes
            |> List.map (fun node ->
                maxSoFar <- max maxSoFar node.LastModified

                if not (seenSoFar.Add node.LastModified) then
                    maxSoFar <- maxSoFar + TimeSpan.FromMilliseconds 1.0

                    if not (seenSoFar.Add maxSoFar) then
                        failwith "logic has failed me"

                    { node with
                        LastModified = maxSoFar
                    }
                else
                    node
            )

        task {
            let connectionString = $"Data Source=%s{sqliteDb.FullName};"
            use connection = new SqliteConnection (connectionString)
            connection.Open ()

            let cmd = connection.CreateCommand ()

            cmd.CommandText <-
                """
INSERT INTO notes
(id, guid, mid, mod, usn, tags, flds, sfld, csum, flags, data)
VALUES ($id, $guid, $mid, $mod, $usn, $tags, $flds, $sfld, $csum, $flags, $data)
"""

            cmd.Parameters.Add ("id", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("guid", SqliteType.Text) |> ignore
            cmd.Parameters.Add ("mid", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("mod", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("usn", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("tags", SqliteType.Text) |> ignore
            cmd.Parameters.Add ("flds", SqliteType.Text) |> ignore
            cmd.Parameters.Add ("sfld", SqliteType.Text) |> ignore
            cmd.Parameters.Add ("csum", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("flags", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("data", SqliteType.Text) |> ignore
            do! cmd.PrepareAsync ()

            let result = ResizeArray ()

            for note in notes do
                cmd.Parameters.["id"].Value <- note.LastModified.ToUnixTimeMilliseconds ()
                cmd.Parameters.["guid"].Value <- note.Guid |> Base91.toString
                cmd.Parameters.["mid"].Value <- note.ModelId.ToUnixTimeMilliseconds ()
                cmd.Parameters.["mod"].Value <- note.LastModified.ToUnixTimeSeconds ()
                cmd.Parameters.["usn"].Value <- note.UpdateSequenceNumber

                cmd.Parameters.["tags"].Value <-
                    match note.Tags with
                    | [] -> ""
                    | tags -> String.concat " " tags |> sprintf " %s "

                cmd.Parameters.["flds"].Value <- note.Fields |> String.concat "\u001f"

                cmd.Parameters.["sfld"].Value <-
                    match note.SortField with
                    | Choice1Of2 s -> s
                    | Choice2Of2 i -> i.ToString ()

                cmd.Parameters.["csum"].Value <- note.Checksum
                cmd.Parameters.["flags"].Value <- note.Flags
                cmd.Parameters.["data"].Value <- note.Data

                let! count = cmd.ExecuteNonQueryAsync ()

                if count <> 1 then
                    failwithf "failed to insert note, got count: %i" count

                let id = note.LastModified.ToUnixTimeMilliseconds ()
                result.Add id

            return result :> IReadOnlyList<_>
        }

    let createCards (Package sqliteDb) (cards : Card<int64, DateTimeOffset> list) =
        task {
            let connectionString = $"Data Source=%s{sqliteDb.FullName};"
            use connection = new SqliteConnection (connectionString)
            connection.Open ()

            let cmd = connection.CreateCommand ()

            cmd.CommandText <-
                """
INSERT INTO cards
(id, nid, did, ord, mod, usn, type, queue, due, ivl, factor, reps, lapses, left, odue, odid, flags, data)
VALUES (@id, @nid, @did, @ord, @mod, @usn, @type, @queue, @due, @ivl, @factor, @reps, @lapses, @left, @odue, @odid, @flags, @data)
"""

            cmd.Parameters.Add ("id", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("nid", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("did", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("ord", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("mod", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("usn", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("type", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("queue", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("due", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("ivl", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("factor", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("reps", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("lapses", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("left", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("odue", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("odid", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("flags", SqliteType.Integer) |> ignore
            cmd.Parameters.Add ("data", SqliteType.Text) |> ignore
            do! cmd.PrepareAsync ()

            for card in cards do
                cmd.Parameters.["id"].Value <- card.ModificationDate.ToUnixTimeMilliseconds ()
                cmd.Parameters.["nid"].Value <- card.NotesId
                cmd.Parameters.["did"].Value <- card.DeckId.ToUnixTimeMilliseconds ()
                cmd.Parameters.["ord"].Value <- card.Ordinal
                cmd.Parameters.["mod"].Value <- card.ModificationDate.ToUnixTimeSeconds ()
                cmd.Parameters.["usn"].Value <- card.UpdateSequenceNumber
                cmd.Parameters.["type"].Value <- card.Type.ToInteger ()
                cmd.Parameters.["queue"].Value <- card.Queue.ToInteger ()
                cmd.Parameters.["due"].Value <- card.Due
                cmd.Parameters.["ivl"].Value <- card.Interval.ToInteger ()
                cmd.Parameters.["factor"].Value <- card.EaseFactor
                cmd.Parameters.["reps"].Value <- card.NumberOfReviews
                cmd.Parameters.["lapses"].Value <- card.NumberOfLapses
                cmd.Parameters.["left"].Value <- card.Left
                cmd.Parameters.["odue"].Value <- card.OriginalDue
                cmd.Parameters.["odid"].Value <- 0
                cmd.Parameters.["flags"].Value <- card.Flags
                cmd.Parameters.["data"].Value <- card.Data

                let! result = cmd.ExecuteNonQueryAsync ()

                if result <> 1 then
                    failwith $"Did not get exactly 1 row back from insertion: %i{result}"

            return ()
        }

    let writeAll
        (rng : Random)
        (collection : CollectionForSql)
        (notes : SerialisedNote list)
        (outputFile : FileInfo)
        : Task<unit>
        =
        let renderedNotes, lookupNote =
            let dict = Dictionary ()

            let buffer = BitConverter.GetBytes (uint64 0)

            let result =
                notes
                |> List.mapi (fun i note ->
                    rng.NextBytes buffer
                    let guid = BitConverter.ToUInt64 (buffer, 0)
                    dict.Add (note, i)
                    SerialisedNote.ToNote guid collection.ModelsInverse note
                )

            let lookupNote (note : SerialisedNote) : int =
                match dict.TryGetValue note with
                | true, v -> v
                | false, _ ->
                    failwith
                        $"A card declared that it was associated with a note, but that note was not inserted.\nDesired: %+A{note}\nAvailable:\n%+A{dict}"

            result, lookupNote

        let tempFile = Path.GetTempFileName () |> FileInfo

        task {
            let! package = createEmptyPackage tempFile

            let! written = collection.Collection |> createDecks package

            let! noteIds = createNotes written renderedNotes

            let _, _, cards =
                ((0, 0, []), notes)
                ||> List.fold (fun (count, iter, cards) note ->
                    let built =
                        SerialisedNote.buildCards count note.Deck 1000<ease> Interval.Unset note
                        |> List.map (Card.translate (fun note -> noteIds.[lookupNote note]) collection.DecksInverse)

                    built.Length + count, iter + 1, built @ cards
                )

            do! createCards written cards

            use outputStream = outputFile.OpenWrite ()
            use archive = new ZipArchive (outputStream, ZipArchiveMode.Create, true)

            let entry = archive.CreateEntry "collection.anki2"
            use entryStream = entry.Open ()
            use contents = tempFile.OpenRead ()
            do! contents.CopyToAsync entryStream

            return ()
        }
