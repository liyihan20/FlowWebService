using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using FlowWebService.Interface;

namespace FlowWebService.Rules
{
    //自动化&设备评估部要求的流程：IE立项/结项流程
    public class IERule : BaseRule, IReturnToBeforeStep
    {
        FlowDBDataContext db = new FlowDBDataContext();
        JObject o;

        /// <summary>
        /// 返回之前的节点
        /// </summary>
        /// <param name="sysNo">流水号</param>
        /// <param name="currentStepName">当前节点名称</param>
        /// <param name="returnToStepName">返回节点名称</param>
        /// <returns>下一审核人</returns>
        public string ReturnTo(string cardNumber, string sysNo, string currentStepName, string returnToStepName)
        {
            //先验证
            var apply = db.flow_apply.Where(a => a.sys_no == sysNo).FirstOrDefault();
            if (apply == null) throw new Exception("此流水号不存在");
            if (apply.success != null) throw new Exception("此流程已结束");

            var currentApplyEntry = apply.flow_applyEntry.Where(a => a.step_name == currentStepName && a.pass == null).FirstOrDefault();
            var returnToApplyEntry = apply.flow_applyEntry.Where(a => a.step_name == returnToStepName).OrderByDescending(a => a.step).FirstOrDefault();
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
            foreach (var te in templateEntrys.Where(t => t.step > returnToApplyEntry.step && t.step <= currentApplyEntry.step).OrderBy(t => t.step)) {
                string auditors = "";
                var appEntry = apply.flow_applyEntry.Where(f => f.step_name == te.step_name).OrderByDescending(f => f.step).FirstOrDefault();
                if (appEntry == null) continue;

                if (te.countersign == true) {
                    //会签
                    auditors = string.Join(";", apply.flow_applyEntry.Where(f => f.step == appEntry.step).Select(f => f.final_auditor ?? f.auditors).ToArray());
                }
                else {
                    auditors = appEntry.final_auditor ?? appEntry.auditors;
                }
                if (!string.IsNullOrEmpty(auditors)) {
                    stepCount++;
                    db.flow_applyEntryQueue.InsertOnSubmit(new flow_applyEntryQueue()
                    {
                        sys_no = sysNo,
                        auditors = auditors,
                        countersign = te.countersign,
                        flow_template_entry_id = te.id,
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
            currentApplyEntry.opinion = "返回到节点：" + returnToStepName;
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

        //IE组长
        public string GetIELeader(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string busName = (string)o["bus_name"];
            var auditor = db.ei_ieAuditors.Where(i => i.bus_name == busName).FirstOrDefault();
            if (auditor == null) return "";
            return auditor.ie_leader_no;
        }

        //事业部长
        public string GetBusMinister(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string busName = (string)o["bus_name"];
            var auditor = db.ei_ieAuditors.Where(i => i.bus_name == busName).FirstOrDefault();
            if (auditor == null) return "";
            return auditor.bus_minister_no;
        }

        //IE管理员
        public string GetIEManager(flow_apply apply, string formJson)
        {
            var auditors = db.flow_auditorRelation.Where(f => f.bill_type == "IE" && f.relate_name == "IE管理部").Select(f => f.relate_value).ToArray();
            if (auditors.Count() == 0) return "";
            return string.Join(";", auditors);
        }

        //AH审核人
        public string GetAHAuditor(flow_apply apply, string formJson)
        {
            var auditors = db.flow_auditorRelation.Where(f => f.bill_type == "IE" && f.relate_name == "AH部").Select(f => f.relate_value).ToArray();
            if (auditors.Count() == 0) return "";
            return string.Join(";", auditors);
        }

    }
}