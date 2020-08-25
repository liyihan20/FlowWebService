using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 电子调休申请流程 SV:Switch Vacation,继承漏刷卡流程，因为流程是一样的
    /// </summary>
    public class SVRule:CRRule
    {
        public override List<flow_applyEntryQueue> GetFlowQueue(string formObj)
        {
            var list = base.GetFlowQueue(formObj);
            var o = JObject.Parse(formObj);
            DateTime dutyDateFrom = (DateTime)o["duty_date_from"];
            DateTime vacationDateFrom = (DateTime)o["vacation_date_from"];
            string depNo = (string)o["dep_no"];

            if (list.Count() == 0) {
                throw new Exception("没有找到任何审核人");
            }

            //电子：如果先调休后值班，需要行政部审批
            if (depNo.StartsWith("104")) {
                if (dutyDateFrom > vacationDateFrom) {
                    ei_department AHDep = GetNearestParentDepByNodeName(depNo, "AH审批", "请假"); //最近的AH审批节点
                    if (AHDep != null) {
                        var auditStep = GetGivenDepAuditor(AHDep, "请假");
                        auditStep.step = list.Count() + 1;
                        auditStep.sys_no = list.First().sys_no;
                        list.Add(auditStep);
                    }
                }
            }
            else if (depNo.StartsWith("106")) {
                //惠州何秀棠审批
                list.Add(new flow_applyEntryQueue()
                {
                    step = list.Count() + 1,
                    step_name = "AH审批",
                    sys_no = list.First().sys_no,
                    auditors = "06101101"
                });
            }
            else if (depNo.StartsWith("4")) {
                //光电仁寿审批人：袁大军101028026
                list.Add(new flow_applyEntryQueue()
                {
                    step = list.Count() + 1,
                    step_name = "AH审批",
                    sys_no = list.First().sys_no,
                    auditors = "101028026"
                });
            }
            else {
                //其它罗继旺
                list.Add(new flow_applyEntryQueue()
                {
                    step = list.Count() + 1,
                    step_name = "AH审批",
                    sys_no = list.First().sys_no,
                    auditors = "05012004"
                });
            }
            return list;
        }
    }
}