using System;
using System.Collections.Generic;
using System.Text;

namespace MArchiveBatchTool.Psb.Writing
{
    public interface IKeyNamesEncoder
    {
        void Process(RegularNameNode root, int totalNodes);
    }
}
