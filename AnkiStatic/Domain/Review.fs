namespace AnkiStatic

open System

type ReviewConfiguration =
    {
        Bury : bool
        EasinessPerEasyReview : float
        Fuzz : float
        IntervalFactor : int
        MaxInterval : TimeSpan
        /// Unused; set to 1
        MinSpace : int
        PerDay : int
    }

    static member toJson (this : ReviewConfiguration) : string =
        $"""{{
    "perDay": %i{this.PerDay},
    "ivlFct": %i{this.IntervalFactor},
    "maxIvl": %i{int this.MaxInterval.TotalDays * 100},
    "minSpace": %i{this.MinSpace},
    "ease4": %f{this.EasinessPerEasyReview},
    "fuzz": %f{this.Fuzz}
}}"""
