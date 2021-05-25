using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 设备类申请单流程
    /// </summary>
    public class XBRule:BaseRule
    {
        FlowDBDataContext db = new FlowDBDataContext();
        JObject o;
        string BILLTYPE = "XB";

        //收益提供者
        public string profitOfferAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["profit_confirm_people_num"];
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

            return string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == "XA" && f.relate_name == "部门总经理" && f.relate_text == company + "_" + deptName).Select(f => f.relate_value).ToArray());
        }

        //以下是外卖流程节点：
        private bool isSellProc(string formJson)
        {
            o = JObject.Parse(formJson);
            return "设备外卖".Equals((string)o["deal_type"]);
        }

        //采购接单
        public string s_buyerAuditor(flow_apply apply, string formJson)
        {
            if (isSellProc(formJson)) {
                return "190423005"; //蔡学骏
            }
            return "";
        }

        //申请人上传设备清单
        public string s_applierAuditor(flow_apply apply, string formJson)
        {
            if (isSellProc(formJson)) {
                return apply.create_user; 
            }
            return "";
        }

        //设备科负责人确认
        public string s_equitmentAuditor(flow_apply apply, string formJson)
        {
            if (isSellProc(formJson)) {
                return equitmentAuditor(apply, formJson);
            }
            return "";
        }

        //部门主管确认
        public string s_deptChargerAuditor(flow_apply apply, string formJson)
        {
            if (isSellProc(formJson)) {
                return deptChargerAuditor(apply, formJson);
            }
            return "";
        }

        //部门总经理确认
        public string s_deptManagementAuditor(flow_apply apply, string formJson)
        {
            if (isSellProc(formJson)) {
                return deptManagementAuditor(apply, formJson);
            }
            return "";
        }

        //设备管理部确认
        public string s_equitmentManagerAuditor(flow_apply apply, string formJson)
        {
            if (isSellProc(formJson)) {
                return "131017020"; //李夏衍
            }
            return "";
        }

        //审核部确认
        public string s_checkerAuditor(flow_apply apply, string formJson)
        {
            if (isSellProc(formJson)) {
                return "190522007"; //陈禹健
            }
            return "";
        }

    }
}