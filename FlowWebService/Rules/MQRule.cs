using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FlowWebService.Interface;
using FlowWebService.Models;
using Newtonsoft.Json.Linq;

namespace FlowWebService.Rules
{
    public class MQRule : BaseRule, IBeforeStartFlow, ICancelFlowAfterFinish
    {
        FlowDBDataContext db = new FlowDBDataContext();
        string BILLTYPE = "MQ";
        JObject o;

        public void Validate(string formObj, string createUser)
        {            
            var existsMQ = from a in db.flow_apply
                           join t in db.flow_template on a.flow_template_id equals t.id
                           where a.create_user == createUser
                           && t.bill_type == BILLTYPE
                           && (a.success == null || a.success == true)
                           select a;
            if (existsMQ.Count() > 0) {
                throw new Exception("你存在未结束或已申请成功的辞职申请，不能再次申请！");
            }
        }

        public void DoBeforeFlow(string formObj)
        {
            
        }

        //组长处理
        public string GetStep1Auditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["group_leader_num"];
        }
        //申请人确认
        public string ApplierConfrim(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            if("挽留成功".Equals((string)o["group_leader_choise"])){
                return apply.create_user;
            }
            else {
                return "";
            }
        }
        //主管处理
        public string GetStep2Auditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["charger_num"];
        }

        //申请人再次确认
        public string ApplierConfrimAgain(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            if ("挽留成功".Equals((string)o["charger_choise"])) {
                return apply.create_user;
            }
            else {
                return "";
            }
        }
        
        //生产部长处理
        public string GetStep3Auditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["produce_minister_num"];
        }

        public void CancelFlow(string sysNo, string cardNumber)
        {
            var apply = db.flow_apply.Where(f => f.sys_no == sysNo).FirstOrDefault();
            if (apply == null) throw new Exception("流水单号不存在");
            if (apply.success == null) throw new Exception("此离职申请还未完结，不能作废");
            if (apply.success == false) throw new Exception("此离职申请已被NG，不能作废");

            apply.finish_date = DateTime.Now;
            apply.success = false;
            db.flow_applyEntry.InsertOnSubmit(new flow_applyEntry()
            {
                flow_apply = apply,
                auditors = cardNumber,
                final_auditor = cardNumber,
                pass = false,
                audit_time = DateTime.Now,
                opinion = "AH部作废",
                step = 100,
                step_name = "AH部"
            });
            db.SubmitChanges();
        }
    }
}