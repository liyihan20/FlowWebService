using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FlowWebService.Interface;
using FlowWebService.Models;
using Newtonsoft.Json.Linq;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 电脑故障报修流程
    /// </summary>
    public class ITRule:BaseRule,IBeforeStartFlow
    {
        FlowDBDataContext db = new FlowDBDataContext();
        string BILLTYPE = "IT";
        JObject o;

        public void Validate(string formObj, string createUser)
        {
            var existsJQ = from a in db.flow_apply
                           where a.create_user == createUser
                           && a.success == null 
                           && a.flow_template.bill_type == BILLTYPE
                           select a;
            if (existsJQ.Count() > 0) {
                throw new Exception("存在未结束的电脑故障报修申请，在流程结束之前不能再次申请！");
            }
        }

        public void DoBeforeFlow(string formObj)
        {
            
        }

        //部门主管
        public string GetDepManager(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["dep_charger_no"];
        }

        // 信息管理部经理
        public string GetICManager(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            int priority = (int)o["priority"];
            //总裁办优先级为5，部长级别为4，经理级别为3，其他为1
            if (priority > 1) {
                return string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "信息管理部经理").Select(f => f.relate_value).ToArray()); 
            }
            return "";
        }

        //IT部接单人
        public string GetITRepairer(flow_apply apply, string formJson)
        {
            return string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "IT部接单").Select(f => f.relate_value).ToArray()); 
        }

        //现场维修的，需要申请人搬电脑到IT部处理
        public string GetApplierIfNeed(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string applierNumber = (string)o["applier_num"];
            string repairWay = (string)o["repair_way"];

            if ("现场维修".Equals(repairWay)) {
                return applierNumber;
            }
            return "";
        }

    }
}