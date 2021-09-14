using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using FlowWebService.Interface;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 项目单流程
    /// </summary>
    public class XARule:BaseRule,IStepName
    {
        FlowDBDataContext db = new FlowDBDataContext();
        JObject o;
        string BILLTYPE = "XA";

        //收益提供者
        public string profitOfferAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["profit_confirm_people_num"];
        }

        //项目初审，有收益的才要审批，林启名
        public string projectFirstAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            bool hasProfit = (bool)o["has_profit"];
            if (hasProfit) {
                return "140901056";
            }
            else {
                return "";
            }
        }

        //设备部负责人
        public string equitmentAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["equitment_auditor_num"];
        }

        //部门主管
        public string deptChargerAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["dept_charger_num"];
        }

        //部门总经理
        public string deptManagementAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);           
            
            string deptName = (string)o["dept_name"];
            string company = (string)o["company"];
            string managerNo = string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "部门总经理" && f.relate_text == company + "_" + deptName).Select(f => f.relate_value).ToArray());
            
            if (string.IsNullOrEmpty(managerNo)) {
                managerNo = (string)o["dept_manager_num"];  //2021-05-25 在数据表增加此总经理厂牌字段，提交后即写入。可解决在流程中间部门名称被变更或删除后找不到总经理审批人的问题。
            }

            return managerNo;
        }

        //采购部审核
        public string buyerAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string company = (string)o["company"];

            return string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "采购部审批" && f.relate_text == company).Select(f => f.relate_value).ToArray());
        }

        //节省与监督
        public string saverAuditor(flow_apply apply, string formJson)
        {
            return string.Join(";", db.flow_auditorRelation.Where(f=>f.bill_type==BILLTYPE && f.relate_name=="节省与监督").Select(f=>f.relate_value).ToList());
        }

        //管理部会签
        public string managementCountersign(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string deptName = (string)o["dept_name"];
            string company = (string)o["company"];
            string classification = (string)o["classification"];
            bool isShareFee = (bool)o["is_share_fee"];

            var result = (from r in db.flow_auditorRelation
                          where r.bill_type == BILLTYPE
                          && r.relate_name == "项目大类" 
                          && r.relate_text == classification
                          select r.relate_value).ToList();

            //加入部门总经理
            result.AddRange(deptManagementAuditor(apply, formJson).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

            //分摊部门的总经理也要加入会签
            if (isShareFee) {
                result.AddRange(((string)o["share_fee_managers"]).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            //2021-04-14 仁寿的需要加上刘蜀鹏审批
            if (deptName.Contains("仁寿")) {
                result.Add("171026058");
            }

            return string.Join(";", result.Distinct().ToList());
        }

        //项目大类负责人验收
        public string projectAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string classification = (string)o["classification"];

            return string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "项目大类" && f.relate_text == classification).Select(f => f.relate_value).ToArray());
        }

        //项目小类负责人验收
        public string projectTypeAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string projectType = (string)o["project_type"];

            return string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "项目小类" && f.relate_text == projectType).Select(f => f.relate_value).ToArray());
        }

        public string GetSpecStepName(string stepName, string formJson)
        {
            var realStepName = stepName;

            if (stepName.Contains("项目小类")) {
                o = JObject.Parse(formJson);
                string projectType = (string)o["project_type"];
                if (projectType.Contains("监控")) {
                    realStepName = "行政安保部确认";
                }
                else if (projectType.Contains("网络")) {
                    realStepName = "信息管理部确认";
                }
            }

            return realStepName;
        }

        //信息管理部验收
        public string GetIMAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string projectType = (string)o["project_type"];

            if (projectType.Contains("网络等弱电项目")) {
                return string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "项目小类" && f.relate_text == projectType).Select(f => f.relate_value).ToArray());
            }
            else {
                return "";
            }
        }

    }
}