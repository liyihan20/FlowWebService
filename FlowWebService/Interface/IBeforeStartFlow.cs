using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowWebService.Interface
{
    interface IBeforeStartFlow
    {
        /// <summary>
        /// 提交之前先验证
        /// </summary>
        /// <param name="formObj"></param>
        /// <param name="createUser"></param>
        void Validate(string formObj, string createUser);

        /// <summary>
        /// 提交之前需要做的事
        /// </summary>
        /// <param name="formObj"></param>
        void DoBeforeFlow(string formObj);
    }
}
