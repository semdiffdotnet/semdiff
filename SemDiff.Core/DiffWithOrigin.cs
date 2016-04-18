// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
namespace SemDiff.Core
{
    /// <summary>
    /// A wrapper around Diff that adds a property that contains if the Diff was from comparing the
    /// Local or Remote file
    /// </summary>
    internal class DiffWithOrigin
    {
        public static DiffWithOrigin Local(Diff diff) =>
            new DiffWithOrigin
            {
                Diff = diff,
                Origin = OriginEnum.Local
            };

        public static DiffWithOrigin Remote(Diff diff) =>
            new DiffWithOrigin
            {
                Diff = diff,
                Origin = OriginEnum.Remote
            };

        public enum OriginEnum
        {
            Local,
            Remote
        }

        public Diff Diff { get; set; }
        public OriginEnum Origin { get; set; }
    }
}