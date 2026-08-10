using Domain.Entities;

namespace Application.GradeScales
{
    // A raw GradeScale band lookup gives a flat step value (every percentage inside a band maps
    // to the exact same GradePoint) -- this instead linearly interpolates from the matched band's
    // own GradePoint (at its MinPercent) up towards the next-higher band's GradePoint (at its
    // MaxPercent), the same formula Nepal's NEB/SEE grading system uses:
    // GP = LowerGP + [(percentage - LowerBound) / (UpperBound - LowerBound)] * (UpperGP - LowerGP).
    // The topmost band has no higher neighbor to interpolate towards, so it stays flat (e.g. A+
    // stays a flat 4.0 across 90-100).
    public static class GradePointCalculator
    {
        public static (string Grade, decimal? GradePoint) Resolve(decimal percentage, IReadOnlyList<GradeScale> gradeScalesDescendingByMinPercent)
        {
            GradeScale matched = null;
            GradeScale nextHigher = null;

            for (var i = 0; i < gradeScalesDescendingByMinPercent.Count; i++)
            {
                var band = gradeScalesDescendingByMinPercent[i];
                if (percentage >= band.MinPercent && percentage <= band.MaxPercent)
                {
                    matched = band;
                    nextHigher = i > 0 ? gradeScalesDescendingByMinPercent[i - 1] : null;
                    break;
                }
            }

            if (matched == null)
            {
                return (null, null);
            }

            var bandSpan = matched.MaxPercent - matched.MinPercent;
            if (nextHigher == null || bandSpan <= 0)
            {
                return (matched.Grade, matched.GradePoint);
            }

            var interpolated = matched.GradePoint + (percentage - matched.MinPercent) / bandSpan * (nextHigher.GradePoint - matched.GradePoint);
            return (matched.Grade, Math.Round(interpolated, 2));
        }
    }
}
