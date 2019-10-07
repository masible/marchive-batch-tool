using System;
using System.Collections.Generic;
using System.Text;

namespace MArchiveBatchTool.Psb.Writing
{
    public class StandardKeyNamesEncoder : IKeyNamesEncoder
    {
        bool[] usedRangeMap;

        public void Process(RegularNameNode root, int totalNodes)
        {
            // Rules
            // #. Character index must be high enough so valueOffset is at least 1.
            //    That means the index is at least char value + 1
            // #. <range rules here>
            // #. Terminating node takes the first available index
            // #. If a node has a terminating child, any non-terminating children are
            //    rebased on the current node's index

            // TODO: find whether current files contain unfilled gaps

            usedRangeMap = new bool[totalNodes];

            throw new NotImplementedException();
        }

    }
}
