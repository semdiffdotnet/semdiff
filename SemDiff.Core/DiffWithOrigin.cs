namespace SemDiff.Core
{
    internal class DiffWithOrigin
    {
        public static DiffWithOrigin Local(Diff diff) => new DiffWithOrigin { Diff = diff, Origin = OriginEnum.Local };

        public static DiffWithOrigin Remote(Diff diff) => new DiffWithOrigin { Diff = diff, Origin = OriginEnum.Remote };

        public enum OriginEnum
        {
            Local,
            Remote
        }

        public Diff Diff { get; set; }
        public OriginEnum Origin { get; set; }
    }
}