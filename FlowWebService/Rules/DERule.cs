using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FlowWebService.Interface;
using FlowWebService.Models;

namespace FlowWebService.Rules
{
    public class DERule:BaseRule,IReturnToBeforeStep
    {
        FlowDBDataContext db = new FlowDBDataContext();
        string BILLTYPE = "DE";
        public string ReturnTo(string cardNumber, string sysNo, string currentStepName, string returnToStepName, string opinion = "")
        {
            //先验证
            var apply = db.flow_apply.Where(a => a.sys_no == sysNo).FirstOrDefault();
            if (apply == null) throw new Exception("此流水号不存在");
            if (apply.success != null) throw new Exception("此流程已结束");

            var applyEntrys = apply.flow_applyEntry.ToList();
            var currentApplyEntry = applyEntrys.Where(a => a.step_name == currentStepName && a.pass == null).FirstOrDefault();
            var returnToApplyEntry = applyEntrys.Where(a => a.step_name == returnToStepName).OrderByDescending(a => a.step).FirstOrDefault();
            var templateEntrys = apply.flow_template.flow_templateEntry.ToList();
            var currentTemplateEntry = templateEntrys.Where(c => c.step_name == currentStepName).FirstOrDefault();
            var returnToTemplateEntry = templateEntrys.Where(c => c.step_name == returnToStepName).FirstOrDefault();

            if (currentApplyEntry == null) throw new Exception("当前处理节点不是在批状态");
            if (currentTemplateEntry == null) throw new Exception("当前节点不存在于流程模板中");
            if (currentTemplateEntry.countersign == true) throw new Exception("当前属于会签节点，不能返回");
            if (returnToTemplateEntry == null && !"申请人".Equals(returnToStepName)) throw new Exception("返回节点不存在于流程模板中");
            if (returnToApplyEntry == null) {
                if ("申请人".Equals(returnToStepName)) {
                    //返回给申请人，申请人不存在与流程模板中，需要特殊处理
                    returnToApplyEntry = new flow_applyEntry()
                    {
                        step = 0,
                        step_name = "申请人",
                        final_auditor = apply.create_user
                    };
                }
                else {
                    throw new Exception("返回节点不存在于流程中");
                }
            }

            //删除当前的流程队列
            db.flow_applyEntryQueue.DeleteAllOnSubmit(db.flow_applyEntryQueue.Where(f => f.sys_no == sysNo));

            //插入到流程队列
            int stepCount = 1; //统计插入的步数
            foreach (var te in applyEntrys.Where(t => t.step > returnToApplyEntry.step && t.step <= currentApplyEntry.step).OrderBy(t => t.step)) {
                string auditors = "";
                var temEntry = templateEntrys.Where(t => t.step_name == te.step_name).FirstOrDefault();
                //var appEntry = apply.flow_applyEntry.Where(f => f.step_name == te.step_name).OrderByDescending(f => f.step).FirstOrDefault();
                if (temEntry == null) continue;

                if (temEntry.countersign == true) {
                    //会签
                    auditors = string.Join(";", applyEntrys.Where(f => f.step == te.step).Select(f => f.final_auditor ?? f.auditors).ToArray());
                }
                else {
                    auditors = te.final_auditor ?? te.auditors;
                }
                if (!string.IsNullOrEmpty(auditors)) {
                    stepCount++;
                    db.flow_applyEntryQueue.InsertOnSubmit(new flow_applyEntryQueue()
                    {
                        sys_no = sysNo,
                        auditors = auditors,
                        countersign = temEntry.countersign,
                        flow_template_entry_id = te.flow_template_entry_id,
                        step = currentApplyEntry.step + stepCount,
                        step_name = te.step_name
                    });
                }
            }

            //如果插入的步数大于模板中下一节点的步数，那么要更新流程模板的step值
            if (templateEntrys.Where(t => t.step > currentTemplateEntry.step && t.step <= currentApplyEntry.step + stepCount).Count() > 0) {
                foreach (var te in templateEntrys.Where(t => t.step > currentTemplateEntry.step)) {
                    te.step = te.step + stepCount * 10;
                }
            }

            //更新当前节点状态
            currentApplyEntry.pass = true;
            currentApplyEntry.opinion = "返回到节点：" + returnToStepName + (string.IsNullOrEmpty(opinion) ? ("," + opinion) : "");
            currentApplyEntry.audit_time = DateTime.Now;
            currentApplyEntry.final_auditor = cardNumber;

            //插入下一节点
            flow_applyEntry nextEntry = new flow_applyEntry();
            nextEntry.step = currentApplyEntry.step + 1;
            nextEntry.step_name = returnToStepName;
            nextEntry.flow_templateEntry = returnToTemplateEntry;
            nextEntry.apply_id = apply.id;
            nextEntry.auditors = returnToApplyEntry.final_auditor;
            db.flow_applyEntry.InsertOnSubmit(nextEntry);

            db.SubmitChanges();

            return returnToApplyEntry.final_auditor;
        }
    }
}