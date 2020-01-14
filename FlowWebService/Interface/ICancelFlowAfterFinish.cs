using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowWebService.Interface
{
    interface ICancelFlowAfterFinish
    {
        void CancelFlow(string sysNo, string cardNumber);
    }
}
