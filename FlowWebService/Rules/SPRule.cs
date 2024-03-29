﻿using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 部门寄件/收件流程
    /// </summary>
    public class SPRule:BaseRule
    {
        FlowDBDataContext db = new FlowDBDataContext();
        string BILLTYPE = "SP";
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
            var busPlannerAuditors = db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE
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
            // 2021-06-09 是否退换货改为是否信利产品
            o = JObject.Parse(formJson);
            bool isSend = ((string)o["send_or_receive"] == "寄件");
            bool isProduct = ((string)o["content_type"] == "产品");
            bool isReturn = ((string)o["isReturnBack"] == "是");

            if (isSend && isProduct && isReturn) {
                var auditors = db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "QA审批").Select(f => f.relate_value).ToArray();
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
            //o = JObject.Parse(formJson);
            //bool isSend = ((string)o["send_or_receive"] == "寄件");
            //bool isProduct = ((string)o["content_type"] == "产品" || (string)o["content_type"] == "原材料");

            //if (isSend && isProduct) {
            //    return string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "行政部审批").Select(f => f.relate_value).ToArray());
            //}
            return ""; //2021-1-21 锡标说不需要经过行政部
        }

        /// <summary>
        /// 物流部审批
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetLGAuditor(flow_apply apply, string formJson)
        {
            //2021-1-22 物流也说不需要审批,2021-6-03 又说要开始审批了，10KG以下也要。
            o = JObject.Parse(formJson);
            bool isProduct = ((string)o["content_type"] != "文件");
            //decimal weight = (decimal)o["total_weight"];

            if (isProduct) {
                return string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "物流部审批").Select(f => f.relate_value).ToArray());
            }
            return "";
        }

        //品质审核
        public string GetQualityAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string contentType = (string)o["content_type"];
            string quality_audior_no = (string)o["quality_audior_no"];

            if ("原材料".Equals(contentType) && !string.IsNullOrEmpty(quality_audior_no)) {
                return quality_audior_no;
            }
            return "";
        }

        //仓管审核，根据仓库地址获取
        public string GetStockAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string contentType = (string)o["content_type"];
            string stockAddr = (string)o["stock_addr"];

            if ("原材料".Equals(contentType) && !string.IsNullOrEmpty(stockAddr)) {
                return string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "仓管审批" && f.relate_text == stockAddr).Select(f => f.relate_value).ToArray());
            }
            return "";
        }

        /// <summary>
        /// 2021-07-28 增加委外物品类，需营运部佐键审批
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetRunningAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            bool isSend = ((string)o["send_or_receive"] == "寄件");
            bool isOutStuff = ((string)o["content_type"] == "委外物品");

            if (isSend && isOutStuff) {
                var auditors = db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "营运审批").Select(f => f.relate_value).ToArray();
                if (auditors.Count() > 0) return string.Join(";", auditors);

            }
            return "";
        }
        
    }
}