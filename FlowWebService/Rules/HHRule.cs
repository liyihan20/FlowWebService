using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlowWebService.Rules
{
    public class HHRule:BaseRule
    {
        FlowDBDataContext db = new FlowDBDataContext();
        string BILLTYPE = "HH";
        JObject o;

        //办事处审批 2020-9-9 营业申请改为客服申请，提交后抄送给客服即可，所以不需办事处审批
        public string GetAgencyAuditor(flow_apply apply, string formJson)
        {
            return "";
            //o = JObject.Parse(formJson);
            //string agencyName = (string)o["agency_name"];
            //var auditors = db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE
            //    && f.relate_name == "办事处审批人" && f.relate_text == agencyName).Select(f => f.relate_value).ToArray();
            //if (auditors.Count() == 0) return "";

            //return string.Join(";", auditors);
        }

        //品质经理审批
        public string GetQualityAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["quality_manager_no"];            
        }

        //计划经理审批
        public string GetPlanManager(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string depName = (string)o["return_dep"];
            var auditors = db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE
                && f.relate_name == "计划经理" && f.relate_text == depName).Select(f => f.relate_value).ToArray();
            if (auditors.Count() == 0) return "";

            return string.Join(";", auditors);
        }

        //生产主管审批
        public string GetDepCharger(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string depName = (string)o["return_dep"];
            var auditors = db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE
                && f.relate_name == "生产主管" && f.relate_text == depName).Select(f => f.relate_value).ToArray();
            if (auditors.Count() == 0) return "";

            return string.Join(";", auditors);
        }

        //部门总经理审批
        public string GetDepManger(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string depName = (string)o["return_dep"];
            var auditors = db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE
                && f.relate_name == "部门总经理" && f.relate_text == depName).Select(f => f.relate_value).ToArray();
            if (auditors.Count() == 0) return "";

            return string.Join(";", auditors);
        }

        //物流审批
        public string GetLogisticAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string company = (string)o["company"];
            var auditors = db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE
                && f.relate_name == "物流审批人" && f.relate_text == company).Select(f => f.relate_value).ToArray();
            if (auditors.Count() == 0) return "";

            return string.Join(";", auditors);
        }

        //市场管理部结案
        public string GetMarketAuditor(flow_apply apply, string formJson)
        {
            var auditors = db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE
                && f.relate_name == "市场管理部").Select(f => f.relate_value).ToArray();
            if (auditors.Count() == 0) return "";

            return string.Join(";", auditors);
        }

        //2021-06-08 增加QA审批
        public string GetQAAuditor(flow_apply apply, string formJson)
        {
            var auditors = db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "QA审批").Select(f => f.relate_value).ToArray();
            return string.Join(";", auditors);
        }

    }
}