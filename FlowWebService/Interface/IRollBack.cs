using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowWebService.Interface
{
    interface IRollBack
    {
        /// <summary>
        /// 收回操作，实现此接口的流程可以收回
        /// </summary>
        /// <param name="sysNo"></param>
        /// <param name="step"></param>
        void RollBack(string sysNo, int step);
    }
}
