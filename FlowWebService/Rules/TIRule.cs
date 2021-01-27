using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FlowWebService.Interface;
using FlowWebService.Models;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 物流车辆放行申请，只需要固定一级审核人
    /// </summary>
    public class TIRule:BaseRule,IFinishFlow,ICancelFlowAfterFinish
    {
        
        public void DoAfterFlowSucceed(string formObj, Models.flow_apply app)
        {
            FlowDBDataContext db = new FlowDBDataContext();
            db.DoAfterFinishTI(app.sys_no);
        }

        public void DoAfterFlowFailed(string formObj, Models.flow_apply app)
        {
            
        }
        public void CancelFlow(string sysNo, string cardNumber)
        {
            FlowDBDataContext db = new FlowDBDataContext();
            var apply = db.flow_apply.Where(f => f.sys_no == sysNo).FirstOrDefault();
            if (apply == null) throw new Exception("流水单号不存在");
            if (apply.success == null) throw new Exception("此申请还未完结，不能作废");
            if (apply.success == false) throw new Exception("此申请已被NG，不能作废");

            apply.finish_date = DateTime.Now;
            apply.success = false;
            db.flow_applyEntry.InsertOnSubmit(new flow_applyEntry()
            {
                flow_apply = apply,
                auditors = cardNumber,
                final_auditor = cardNumber,
                pass = false,
                audit_time = DateTime.Now,
                opinion = "作废流程",
                step = 100,
                step_name = "申请人"
            });
            db.SubmitChanges();
        }
    }
}