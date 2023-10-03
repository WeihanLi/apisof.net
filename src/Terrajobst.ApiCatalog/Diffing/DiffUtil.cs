using System.Diagnostics;

namespace Terrajobst.ApiCatalog;

// Note: Taken and modified from Roslyn

internal sealed class DiffUtil
{
    public static IEnumerable<MarkupPartDiff> Diff(Markup oldMarkup, Markup newMarkup)
    {
        return MarkupLCS.Default.Diff(oldMarkup, newMarkup);
    }

    private enum EditKind
    {
        None = 0,
        Update = 1,
        Insert = 2,
        Delete = 3,
    }

    private sealed class MarkupLCS : LongestCommonSubsequence<Markup>
    {
        public static readonly MarkupLCS Default = new();

        protected override bool ItemsEqual(Markup sequenceA, int indexA, Markup sequenceB, int indexB)
        {
            var a = sequenceA.Parts[indexA];
            var b = sequenceB.Parts[indexB];
            return a.Kind == b.Kind &&
                   a.Text == b.Text &&
                   a.Reference == b.Reference;
        }

        public IEnumerable<MarkupPartDiff> Diff(Markup oldMarkup, Markup newMarkup)
        {
            foreach (var edit in GetEdits(oldMarkup, oldMarkup.Parts.Length, newMarkup, newMarkup.Parts.Length).Reverse())
            {
                switch (edit.Kind)
                {
                    case EditKind.Delete:
                        yield return new MarkupPartDiff(DiffKind.Removed, oldMarkup.Parts[edit.IndexA]);
                        break;

                    case EditKind.Insert:
                        yield return new MarkupPartDiff(DiffKind.Added, newMarkup.Parts[edit.IndexB]);
                        break;

                    case EditKind.Update:
                        if (ItemsEqual(oldMarkup, edit.IndexA, newMarkup, edit.IndexB))
                        {
                            yield return new MarkupPartDiff(DiffKind.Unchanged, newMarkup.Parts[edit.IndexB]);
                        }
                        else
                        {
                            yield return new MarkupPartDiff(DiffKind.Removed, oldMarkup.Parts[edit.IndexA]);
                            yield return new MarkupPartDiff(DiffKind.Added, newMarkup.Parts[edit.IndexB]);
                        }
                        break;
                }
            }
        }
    }

    private abstract class LongestCommonSubsequence<TSequence>
    {
        protected readonly struct Edit
        {
            public readonly EditKind Kind;
            public readonly int IndexA;
            public readonly int IndexB;

            internal Edit(EditKind kind, int indexA, int indexB)
            {
                this.Kind = kind;
                this.IndexA = indexA;
                this.IndexB = indexB;
            }
        }

        private const int DeleteCost = 1;
        private const int InsertCost = 1;
        private const int UpdateCost = 2;

        protected abstract bool ItemsEqual(TSequence sequenceA, int indexA, TSequence sequenceB, int indexB);

        protected IEnumerable<Edit> GetEdits(TSequence sequenceA, int lengthA, TSequence sequenceB, int lengthB)
        {
            int[,] d = ComputeCostMatrix(sequenceA, lengthA, sequenceB, lengthB);
            int i = lengthA;
            int j = lengthB;

            while (i != 0 && j != 0)
            {
                if (d[i, j] == d[i - 1, j] + DeleteCost)
                {
                    i--;
                    yield return new Edit(EditKind.Delete, i, -1);
                }
                else if (d[i, j] == d[i, j - 1] + InsertCost)
                {
                    j--;
                    yield return new Edit(EditKind.Insert, -1, j);
                }
                else
                {
                    i--;
                    j--;
                    yield return new Edit(EditKind.Update, i, j);
                }
            }

            while (i > 0)
            {
                i--;
                yield return new Edit(EditKind.Delete, i, -1);
            }

            while (j > 0)
            {
                j--;
                yield return new Edit(EditKind.Insert, -1, j);
            }
        }

        private int[,] ComputeCostMatrix(TSequence sequenceA, int lengthA, TSequence sequenceB, int lengthB)
        {
            var la = lengthA + 1;
            var lb = lengthB + 1;

            var d = new int[la, lb];

            d[0, 0] = 0;
            for (int i = 1; i <= lengthA; i++)
            {
                d[i, 0] = d[i - 1, 0] + DeleteCost;
            }

            for (int j = 1; j <= lengthB; j++)
            {
                d[0, j] = d[0, j - 1] + InsertCost;
            }

            for (int i = 1; i <= lengthA; i++)
            {
                for (int j = 1; j <= lengthB; j++)
                {
                    int m1 = d[i - 1, j - 1] + (ItemsEqual(sequenceA, i - 1, sequenceB, j - 1) ? 0 : UpdateCost);
                    int m2 = d[i - 1, j] + DeleteCost;
                    int m3 = d[i, j - 1] + InsertCost;
                    d[i, j] = Math.Min(Math.Min(m1, m2), m3);
                }
            }

            return d;
        }
    }
}