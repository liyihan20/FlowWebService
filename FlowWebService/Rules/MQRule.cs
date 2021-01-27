using FlowWebService.Interface;
using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace FlowWebService.Rules
{
    public class MQRule : BaseRule, IBeforeStartFlow, ICancelFlowAfterFinish
    {
        FlowDBDataContext db = new FlowDBDataContext();
        string BILLTYPE = "MQ";
        JObject o;

        public void Validate(string formObj, string createUser)
        {
            o = JObject.Parse(formObj);
            string cardNumber = (string)o["card_number"];

            var existsMQ = from a in db.flow_apply
                           join t in db.flow_template on a.flow_template_id equals t.id
                           where t.bill_type == BILLTYPE 
                           && a.create_user == createUser
                           && (a.success == null || a.success == true)
                           select a.sys_no;
            if (existsMQ.Count() > 0) {
                throw new Exception("你存在未结束或已申请成功的辞职申请，不能再次申请！申请单号："+existsMQ.First());
            }

            var existsJQ = from j in db.ei_jqApply
                           join a in db.flow_apply on j.sys_no equals a.sys_no
                           where j.card_number == cardNumber
                           && j.apply_time>DateTime.Now.AddMonths(-2) //只取最近2个月的请假记录，避免数据太多影响速度
                           && (a.success == null || a.success == true)
                           select a.sys_no;
            if (existsJQ.Count() > 0) {
                throw new Exception("你存在未结束或已申请成功的辞职申请，不能再次申请！申请单号：" + existsJQ.First());
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
            //2020-11-30 还在职的情况下才需要申请人确认
            if (db.GetHREmpInfo(apply.create_user).Count() < 1) {
                return "";
            }

            //220-11-17 不管主管选了什么处理方式，都要经过申请人确认
            return apply.create_user;

            //o = JObject.Parse(formJson);
            //if ("挽留成功".Equals((string)o["charger_choise"])) {
            //    return apply.create_user;
            //}
            //else {
            //    return "";
            //}
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
            //if (apply.success == null) throw new Exception("此离职申请还未完结，不能作废");
            if (apply.success == false) throw new Exception("此离职申请已被NG，不能作废");

            apply.finish_date = DateTime.Now;
            apply.success = false;

            db.flow_applyEntry.DeleteAllOnSubmit(apply.flow_applyEntry.Where(a => a.pass == null).ToList());

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