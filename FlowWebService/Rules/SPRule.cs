using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 部门寄件/收件流程
    /// </summary>
    public class SPRule:BaseRule
    {
        FlowDBDataContext db = new FlowDBDataContext();
        JObject o;

        /// <summary>
        /// 事业部审批
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetBusDepAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string busName = (string)o["bus_name"];
            var busPlannerAuditors = db.flow_auditorRelation.Where(f => f.bill_type == "SP"
                && f.relate_name == "事业部审批" && f.relate_text == busName).Select(f => f.relate_value).ToArray();
            if (busPlannerAuditors.Count() == 0) return "";

            return string.Join(";", busPlannerAuditors);
        }

        /// <summary>
        /// 退补货需要QA审批
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetQAAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            bool isSend = ((string)o["send_or_receive"] == "寄件");
            bool isProduct = ((string)o["content_type"] == "产品");
            bool isReturn = ((string)o["isReturnBack"] == "是");

            if (isSend && isProduct && isReturn) {
                var auditors = db.flow_auditorRelation.Where(f => f.bill_type == "SP" && f.relate_name == "QA审批").Select(f => f.relate_value).ToArray();
                if (auditors.Count() > 0) return string.Join(";", auditors);

            }
            return "";
        }

        /// <summary>
        /// 行政部审批
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetAdministrationAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            bool isSend = ((string)o["send_or_receive"] == "寄件");
            bool isProduct = ((string)o["content_type"] == "产品");

            if (isSend && isProduct) {
                return string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == "SP" && f.relate_name == "行政部审批").Select(f => f.relate_value).ToArray());
            }
            return "";
        }

        /// <summary>
        /// 物流部审批
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetLGAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            bool isProduct = ((string)o["content_type"] == "产品");
            decimal weight = (decimal)o["total_weight"];

            if (isProduct && weight >= 10) {
                return string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == "SP" && f.relate_name == "物流部审批").Select(f => f.relate_value).ToArray());
            }
            return "";
        }

    }
}