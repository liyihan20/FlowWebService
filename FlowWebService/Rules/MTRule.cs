using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlowWebService.Rules
{
    public class MTRule:BaseRule
    {
        JObject o;
        FlowDBDataContext db = new FlowDBDataContext();

        public string GetClassMembers(flow_apply app, string formObj)
        {
            var o = JObject.Parse(formObj);
            int infoId = (int)o["eqInfo_id"];

            var members = (from i in db.ei_mtEqInfo
                           join c in db.ei_mtClass on i.class_id equals c.id
                           where i.id == infoId
                           select c.member_number).FirstOrDefault();

            return members;
        }

        public string GetAccepter(flow_apply app, string formObj)
        {
            var o = JObject.Parse(formObj);

            return (string)o["accept_member_no"];
        }

        public string GetClassLeader(flow_apply app, string formObj)
        {
            var o = JObject.Parse(formObj);
            int infoId = (int)o["eqInfo_id"];

            var leader = (from i in db.ei_mtEqInfo
                           join c in db.ei_mtClass on i.class_id equals c.id
                           where i.id == infoId
                           select c.leader_number).FirstOrDefault();

            return leader;
        }

    }
}