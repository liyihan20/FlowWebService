using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FlowWebService.Models;

namespace FlowWebService.Interface
{
    public interface IFinishFlow
    {
        /// <summary>
        /// 流程成功完结需要做的事情
        /// </summary>
        /// <param name="formObj"></param>
        void DoAfterFlowSucceed(string formObj,flow_apply app);

        /// <summary>
        /// 流程NG完结后需要做的事情
        /// </summary>
        /// <param name="formObj"></param>
        void DoAfterFlowFailed(string formObj, flow_apply app);
    }
}