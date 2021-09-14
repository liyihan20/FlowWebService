using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FlowWebService.Models;
using FlowWebService.Interface;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace FlowWebService.Rules
{
    public class FXRule:BaseRule,IFlowQueue,IBeforeStartFlow,IAfterStepAudited
    {
        JObject o;
        string BILLTYPE = "FX";
        FlowDBDataContext db = new FlowDBDataContext();



        public void Validate(string formObj, string createUser)
        {
            
        }

        public void DoBeforeFlow(string formObj)
        {
            var queueList = GetFlowQueue(formObj);

            db.flow_applyEntryQueue.InsertAllOnSubmit(queueList);
            db.SubmitChanges();
        }


        /// <summary>
        /// 自提流程申请时未上传附件的，部门负责人同意后，需上传附件，插入可以上传的处理节点
        /// </summary>
        /// <param name="stepName"></param>
        /// <param name="formJson"></param>
        public void AfterStepSucceedAudited(int? step, string stepName, string formJson)
        {
            if (!stepName.Contains("部门负责人")) {
                return;
            }

            o = JObject.Parse(formJson);
            bool isSelfTaken = ((string)o["fx_type_no"]).StartsWith("2"); //是否自提流程
            bool hasAttach = (bool)o["has_attachment"];

            if (isSelfTaken && !hasAttach) {
                string sysNo = (string)o["sys_no"];
                string applierNum = (string)o["applier_num"];
                db.flow_applyEntryQueue.InsertOnSubmit(new flow_applyEntryQueue()
                {
                    sys_no = sysNo,
                    step = step + 1,
                    step_name = "上传物品图片",
                    auditors = applierNum,
                    countersign = false
                });
                db.SubmitChanges();
            }

        }

        /// <summary>
        /// 根据业务类型和流程字段动态生成放行流程到队列表
        /// </summary>
        /// <param name="formObj"></param>
        /// <returns></returns>
        public List<flow_applyEntryQueue> GetFlowQueue(string formObj)
        {
            o = JObject.Parse(formObj);
            string fxTypeNo = (string)o["fx_type_no"];
            string auditorSegs = (string)o["auditor_segs"];
            string sysNo = (string)o["sys_no"];

            var fxType = db.ei_fxType.Where(f => f.type_no == fxTypeNo).FirstOrDefault();

            if (fxType == null) {
                throw new Exception("当前业务类型不存在");
            }
            if (fxType.is_deleted) {
                throw new Exception("当前业务类型已被禁用");
            }
            if (string.IsNullOrEmpty(fxType.type_process)) {
                throw new Exception("当前业务类型没有对应的审批流程");
            }

            List<flow_applyEntryQueue> queueList = new List<flow_applyEntryQueue>();
            var processInfo = JObject.Parse(fxType.process_info);
            var userInput = ((string)processInfo["user_input"]).Split(new char[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries);
            var userSelect = ((string)processInfo["user_select"]).Split(new char[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries);
            List<AuditorSeg> formAuditors = JsonConvert.DeserializeObject<List<AuditorSeg>>(auditorSegs);
            var processNames = fxType.type_process.Split(new char[] { '>' }, StringSplitOptions.RemoveEmptyEntries);
            int step = 10;
            foreach (var pn in processNames) {
                string auditor = "";
                if (userInput.Contains(pn) || userSelect.Contains(pn)) {
                    //从申请表单获取审核人
                    auditor = formAuditors.Where(f => f.stepName == pn).Select(f => f.auditor).FirstOrDefault();
                }
                else {
                    //从系统设置获取审核人
                    auditor = db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == pn).Select(f => f.relate_value).FirstOrDefault();
                }

                if (string.IsNullOrEmpty(auditor)) {
                    throw new Exception("找不到此审批环节的处理人：" + pn);
                }

                queueList.Add(new flow_applyEntryQueue()
                {
                    sys_no = sysNo,
                    auditors = auditor,
                    step_name = pn,
                    step = step,
                    countersign = false
                });
                step = step + 10;
            }

            return queueList;
        }
    }

    public class AuditorSeg
    {
        public string stepName { get; set; }
        public string auditor { get; set; }
    }

}