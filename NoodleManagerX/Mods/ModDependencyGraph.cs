using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Mods
{
    public class ModDependencyGraph
    {
        public enum ResolvedState
        {
            UNRESOLVED,
            RESOLVED,
            ERROR
        }

        public ResolvedState State { get; private set; } = ResolvedState.UNRESOLVED;

        public void Resolve()
        {
            State = ResolvedState.RESOLVED;
        }
    }
}
