using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FlowWebService.Interface;
using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 员工调动申请流程
    /// </summary>
    public class SJRule : BaseRule, IBeforeStartFlow, IFlowQueue
    {
        
        FlowDBDataContext db = new FlowDBDataContext();
        string BILLTYPE = "SJ";
        public List<flow_applyEntryQueue> GetFlowQueue(string formObj)
        {
            var o = JObject.Parse(formObj);
            List<flow_applyEntryQueue> list = new List<flow_applyEntryQueue>();
            string sysNo=(string)o["sys_no"];
            string salaryType = (string)o["salary_type"];
            int step = 1;
            string AHAuditor = string.Join(",", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "AH审批" && f.relate_text == salaryType).Select(f => f.relate_value).ToArray());
            if (string.IsNullOrEmpty(AHAuditor)) throw new Exception("找不到AH审批处理人");

            //string inClerkNum = (string)o["in_clerk_num"];
            string inManagerNum = (string)o["in_manager_num"];
            string outManagerNum = (string)o["out_manager_num"];

            if ("计件".Equals(salaryType)) {

                //1. 调入文员
                //list.Add(new flow_applyEntryQueue()
                //{
                //    step = step++,
                //    auditors = inClerkNum,
                //    step_name = "调入部门文员审批",
                //    sys_no = sysNo,
                //    countersign = false
                //});

                //2. 调入主管
                list.Add(new flow_applyEntryQueue()
                {
                    step = step++,
                    auditors = inManagerNum,
                    step_name = "调入部门主管/经理审批",
                    sys_no = sysNo,
                    countersign = false
                });

                //3. 调出主管
                list.Add(new flow_applyEntryQueue()
                {
                    step = step++,
                    auditors = outManagerNum,
                    step_name = "调出部门主管/经理审批",
                    sys_no = sysNo,
                    countersign = false
                });

            }
            else {
                //包括月薪、计转月、月转计
                string inMinisterNum = (string)o["in_minister_num"];
                string outMinisterNum = (string)o["out_minister_num"];

                //1. 调入文员
                //list.Add(new flow_applyEntryQueue()
                //{
                //    step = step++,
                //    auditors = inClerkNum,
                //    step_name = "调入部门文员审批",
                //    sys_no = sysNo,
                //    countersign = false
                //});

                //2. 调入主管
                list.Add(new flow_applyEntryQueue()
                {
                    step = step++,
                    auditors = inManagerNum,
                    step_name = "调入部门主管/经理审批",
                    sys_no = sysNo,
                    countersign = false
                });

                //3. 调入部长
                list.Add(new flow_applyEntryQueue()
                {
                    step = step++,
                    auditors = inMinisterNum,
                    step_name = "调入部门部长/助理审批",
                    sys_no = sysNo,
                    countersign = false
                });

                //4. 调出主管
                list.Add(new flow_applyEntryQueue()
                {
                    step = step++,
                    auditors = outManagerNum,
                    step_name = "调出部门主管/经理审批",
                    sys_no = sysNo,
                    countersign = false
                });

                //5. 调出部长/助理
                list.Add(new flow_applyEntryQueue()
                {
                    step = step++,
                    auditors = outMinisterNum,
                    step_name = "调出部门部长/助理审批",
                    sys_no = sysNo,
                    countersign = false
                });
            }

            //AH审批
            list.Add(new flow_applyEntryQueue()
            {
                step = step++,
                auditors = AHAuditor,
                step_name = "AH审批",
                sys_no = sysNo,
                countersign = false
            });

            return list;
        }

        public void Validate(string formObj, string createUser)
        {
           
        }

        public void DoBeforeFlow(string formObj)
        {
            var queue = GetFlowQueue(formObj);
            db.flow_applyEntryQueue.InsertAllOnSubmit(queue);
            db.SubmitChanges();
        }
    }
}