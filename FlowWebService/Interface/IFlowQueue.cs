using FlowWebService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowWebService.Interface
{
    interface IFlowQueue
    {
        /// <summary>
        /// 获取流程预览，只有在申请时就决定所有审核步骤的才可以
        /// </summary>
        /// <param name="formObj"></param>
        /// <returns></returns>
        List<flow_applyEntryQueue> GetFlowQueue(string formObj);
    }
}
