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
    /// 请假流程
    /// 这个没有固定的流程模板，大部分的审批节点只能在代码中动态生成，连第一级审批都是未知的
    /// </summary>
    public class ALRule:BaseRule,IBeforeStartFlow,IFinishFlow
    {
        JObject o;
        FlowDBDataContext db = new FlowDBDataContext();
        private const string PROCESSNAME = "请假";

        public void Validate(string formObj, string createUser)
        {
            if (db.flow_apply.Where(a => a.create_user == createUser && a.success == null).Count() > 0) {
                throw new Exception("存在未完成的请假申请流程，结束之前不能再次申请");
            }
        }

        public void DoBeforeFlow(string formObj)
        {
            //流程开始之前，先将审核人队列准备好
            var queue = GetALAuditQueue(formObj);
            db.flow_applyEntryQueue.InsertAllOnSubmit(queue);
            db.SubmitChanges();
        }

        public void DoAfterFlowSucceed(string formObj,flow_apply apply)
        {
            o = JObject.Parse(formObj);
            string cardNo = (string)o["applier_num"];
            string agentNumber = (string)o["agent_man"];
            DateTime beginTime = (DateTime)o["from_date"];
            DateTime endTime = (DateTime)o["to_date"];

            //如果申请了撤销，最后审批成功，需要将apply的success状态改为false，表示此请假流程是无效的
            if (apply.user_abort == true) {
                apply.success = false;
            }

            //如果有代理人，插入代理表
            if (!string.IsNullOrEmpty(agentNumber)) {
                ei_workAgent agent = new ei_workAgent();
                agent.agent_number = agentNumber;
                agent.emp_number = cardNo;
                agent.begin_time = beginTime;
                agent.end_time = endTime;
                db.ei_workAgent.InsertOnSubmit(agent);
                db.SubmitChanges();
            }            

        }

        public void DoAfterFlowFailed(string formObj, flow_apply apply)
        {
            //如果申请了撤销，最后审批失败，需要将apply的success状态改为true，表示此请假流程是有效的
            if (apply.user_abort == true) {
                apply.success = true;
            }
        }

        public List<flow_applyEntryQueue> GetALAuditQueue(string formObj)
        {
            List<flow_applyEntryQueue> list = new List<flow_applyEntryQueue>();

            o = JObject.Parse(formObj);
            string sysNo = (string)o["sys_no"];
            string cardNo=(string)o["applier_num"];
            string depNo = (string)o["dep_no"];
            int empLevel = (int)o["emp_level"];
            bool isDirectCharge = ((bool?)o["is_direct_charge"]) ?? false;
            string leaveType = (string)o["leave_type"];
            int workDays = (int)o["work_days"];
            decimal workHours = (decimal)o["work_hours"];

            //走集团流程的部门
            bool isCopFlow = true;
            string[] copDepNames = new string[] { "信利半导体有限公司", "总裁办", "信利电子有限公司", "信利光电股份有限公司" };
            foreach (var d in db.ei_department.Where(d => copDepNames.Contains(d.FName)).ToList()) {
                if (depNo.StartsWith(d.FNumber)) {
                    isCopFlow = false;
                    break;
                }
            }

            ei_department AHDep = GetNearestParentDepByNodeName(depNo, "AH审批", PROCESSNAME); //最近的AH审批节点
            int stepNum = 1;

            if (isCopFlow) {
                //集团流程
                string presidentNo;
                try {
                    presidentNo = db.ei_department.Where(d => d.FName == "总裁办").First().FNumber; //总裁办
                }
                catch {
                    throw new Exception("[总裁办]部门不存在");
                }
                
                if (empLevel < 1) {
                    //组长以下，至少需要本部门和上一级部门负责人审批 
                    var n0 = GetParentDepAuditor(depNo, PROCESSNAME, 0, false);
                    n0.sys_no = sysNo;
                    n0.step = stepNum++;
                    list.Add(n0);

                    var n1 = GetParentDepAuditor(depNo, PROCESSNAME, 1);
                    if (n1 != null) {
                        n1.sys_no = sysNo;
                        n1.step = stepNum++;
                        list.Add(n1);
                    }
                   
                    if (workDays >= 2)  {
                        //大于2天，AH，上上级部门审批
                        if (AHDep != null) {
                            var n2 = GetGivenDepAuditor(AHDep, PROCESSNAME);
                            n2.sys_no = sysNo;
                            n2.step = stepNum++;
                            list.Add(n2);
                        }

                        var n3 = GetParentDepAuditor(depNo, PROCESSNAME, 2);
                        if (n3 != null) {
                            n3.sys_no = sysNo;
                            n3.step = stepNum++;
                            list.Add(n3);
                        }                        
                    }
                }
                else if (empLevel < 7) {
                    //组长以上，经理/主管以下，至少需要本部门负责人和行政/HR审批
                    var n0 = GetParentDepAuditor(depNo, PROCESSNAME, 0, false);
                    n0.sys_no = sysNo;
                    n0.step = stepNum++;
                    list.Add(n0);

                    if (AHDep != null) {
                        var n1 = GetGivenDepAuditor(AHDep, PROCESSNAME);
                        n1.sys_no = sysNo;
                        n1.step = stepNum++;                        
                        list.Add(n1);
                    }                    

                    if (workDays >= 0) {
                        //需要上上级部门负责人审批
                        var n2 = GetParentDepAuditor(depNo, PROCESSNAME, 1);
                        if (n2 != null) {
                            n2.step = stepNum++;
                            n2.sys_no = sysNo;
                            list.Add(n2);
                        }
                    }
                }
                else {
                    //经理/主管以上，总裁办审批
                    var n0 = GetGivenDepAuditor(presidentNo, PROCESSNAME);
                    n0.sys_no = sysNo;
                    n0.step = stepNum++;
                    list.Add(n0);
                }

            }
            else {
                //光电和半导体的请假流程
                var ceoDep = GetNearestDep(depNo, "董事办公室");
                if (ceoDep == null) {
                    ceoDep = GetNearestDep(depNo, "总经理办公室"); //没有董事办公室，看有没有总经理办公室
                }

                if (empLevel < 1) {
                    //组长以下，必须本部门和上级部门审批
                    var n0 = GetParentDepAuditor(depNo, PROCESSNAME, 0,false);
                    n0.sys_no = sysNo;
                    n0.step = stepNum++;
                    list.Add(n0);

                    var n1 = GetParentDepAuditor(depNo, PROCESSNAME, 1);
                    if (n1 != null) {
                        n1.sys_no = sysNo;
                        n1.step = stepNum++;
                        list.Add(n1);
                    }                    

                    if (workDays >= 10) {
                        //10天以上需要上上级审批
                        var n2 = GetParentDepAuditor(depNo, PROCESSNAME, 2);
                        if (n2 != null) {
                            n2.sys_no = sysNo;
                            n2.step = stepNum++;
                            list.Add(n2);
                        }
                    }
                    if (workDays >= 15) {
                        //15天以上需要AH部审批
                        if (AHDep != null) {
                            var n3 = GetGivenDepAuditor(AHDep, PROCESSNAME);
                            n3.sys_no = sysNo;
                            n3.step = stepNum++;
                            list.Add(n3);
                        }
                    }
                }
                else if (empLevel < 7) {
                    //组长以上，经理/主管以下，至少需要本部门负责人审批
                    var n0 = GetParentDepAuditor(depNo, PROCESSNAME, 0,false);
                    n0.sys_no = sysNo;
                    n0.step = stepNum++;
                    list.Add(n0);

                    if (workDays >= 3) { 
                        //3天以上需要上一级负责人审批
                        var n1 = GetParentDepAuditor(depNo, PROCESSNAME, 1);
                        if (n1 != null) {
                            n1.sys_no = sysNo;
                            n1.step = stepNum++;
                            list.Add(n1);
                        }
                    }
                    if (workDays >= 10) {
                        //10天以上需要AH部审批
                        if (AHDep != null) {
                            var n2 = GetGivenDepAuditor(AHDep, PROCESSNAME);
                            n2.sys_no = sysNo;
                            n2.step = stepNum++;
                            list.Add(n2);
                        }
                    }
                }
                else {
                    //经理/主管以上
                    if (isDirectCharge) {
                        //直管
                        if (ceoDep == null) {
                            throw new Exception("[董事办公室]审核人未设置");
                        }
                        var n0 = GetGivenDepAuditor(ceoDep, PROCESSNAME);
                        n0.sys_no = sysNo;
                        n0.step = stepNum++;
                        list.Add(n0);
                    }
                    else {
                        //不是直管，都要上一级部门负责人审批
                        var n0 = GetParentDepAuditor(depNo, PROCESSNAME, 1,false);
                        n0.step = stepNum++;
                        n0.sys_no = sysNo;
                        list.Add(n0);

                        if (workDays > 3) {
                            //3天以上需要AH部审批
                            if (AHDep != null) {
                                var n1 = GetGivenDepAuditor(AHDep, PROCESSNAME);
                                n1.sys_no = sysNo;
                                n1.step = stepNum++;
                                list.Add(n1);
                            }
                        }
                        if (workDays >= 10) {
                            //10天以上需要董事总经理审批
                            if (ceoDep != null) {
                                var n2 = GetGivenDepAuditor(ceoDep, PROCESSNAME);                                
                                    n2.sys_no = sysNo;
                                    n2.step = stepNum++;
                                    list.Add(n2);                                
                            }
                        }
                    }
                }
            }

            //病假60天内累计请假大于30天，或除了产假、病假之外，其它假60天内累计大于15天,需要行政部审批
            DateTime twoMonthsAgo=DateTime.Now.AddMonths(-2);
            string administrationNo = "1"; //集团,行政审批
            if ("病假".Equals(leaveType)) {
                int sickDaysInTwoMonths = workDays;
                var sickRecord = db.vw_leaving_days.Where(v => v.applier_num == cardNo && v.leave_type == "病假" && v.from_date >= twoMonthsAgo).ToList();
                if (sickRecord.Count() > 0) {
                    sickDaysInTwoMonths += sickRecord.Sum(s => s.work_days) ?? 0 + (int)Math.Floor((sickRecord.Sum(s => s.work_hours) ?? 0) / 8);
                }
                if (sickDaysInTwoMonths >= 30) {
                    var ad = GetGivenDepAuditor(administrationNo, PROCESSNAME);
                    ad.sys_no = sysNo;
                    ad.step = stepNum++;
                    list.Add(ad);
                }
            }else if (!"产假".Equals("leaveType")) {
                int leaveDays = workDays;
                var leavRecord = db.vw_leaving_days.Where(v => v.applier_num == cardNo && v.leave_type != "病假" && v.leave_type != "产假" && v.from_date >= twoMonthsAgo).ToList();
                if (leavRecord.Count() > 0) {
                    leaveDays += leavRecord.Sum(l => l.work_days) ?? 0 + (int)Math.Floor((leavRecord.Sum(s => s.work_hours) ?? 0) / 8);
                }
                if (leaveDays >= 15) {
                    var ad = GetGivenDepAuditor(administrationNo, PROCESSNAME);
                    ad.sys_no = sysNo;
                    ad.step = stepNum++;
                    list.Add(ad);
                }
            }

            return list;
        }

        public FlowResultModel AbortAfterFinish(string sysNo, string reason)
        {
            var apps = db.flow_apply.Where(s => s.sys_no == sysNo).ToList();
            if (apps.Count() == 0) {
                return new FlowResultModel(false,"流水号不存在");
            }

            var app = apps.First();
            if (app.success == null) {
                return new FlowResultModel(false,"流程未结束，不能走撤销流程");
            }
            if (app.success == false) {
                return new FlowResultModel(false, "流程已被拒绝，不能走撤销流程");
            }
            if(app.user_abort==true){
                return new FlowResultModel(false,"撤销流程只能走一次");
            }

            //修改表头
            app.success = null;
            app.user_abort = true;
            app.finish_date = null;
            
            int maxStep=(int)app.flow_applyEntry.Max(f=>f.step);

            //插入表体记录
            maxStep++;
            flow_applyEntry ae = new flow_applyEntry();
            ae.apply_id = app.id;
            ae.auditors = app.create_user;
            ae.final_auditor = app.create_user;
            ae.opinion = reason;
            ae.pass = true;
            ae.step = maxStep;
            ae.step_name = "申请撤销";
            ae.audit_time = DateTime.Now;
            db.flow_applyEntry.InsertOnSubmit(ae);

            //第一步的审核人
            var s1 = GetPreStep(app, 1);
            maxStep++;
            foreach (var s in s1) {
                flow_applyEntry ae1 = new flow_applyEntry();
                ae1.apply_id = s.apply_id;
                ae1.auditors = s.auditors;
                ae1.flow_template_entry_id = s.flow_template_entry_id;
                ae1.step = maxStep;
                ae1.step_name = s.step_name;
                db.flow_applyEntry.InsertOnSubmit(ae1);
            }

            db.SubmitChanges();
            return new FlowResultModel(true, "撤销申请提交成功", string.Join(";", s1.Select(s => s.auditors).ToArray()));

        }
        

    }
}