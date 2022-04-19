using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FangChain
{
    public interface ICompactor
    {
        ImmutableArray<BlockModel> Compact(ImmutableArray<BlockModel> blockchain, long fromIndex, long toIndex);
    }
}
